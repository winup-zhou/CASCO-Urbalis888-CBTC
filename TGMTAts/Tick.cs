using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TGMTAts {

    public static partial class TGMTAts {
        
        internal static SpeedLimit nextLimit;

        public static SpeedLimit movementEndpoint = SpeedLimit.inf;

        private const int speedMultiplier = 1;

        static int lastDrawTime = 0;
        static int MsglastShowTime = 0;

        static double lastAcc, lastSpeed, lastlocation=0, lastJudgeTime = 0;

        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState state, IntPtr hPanel, IntPtr hSound) {
            var panel = new AtsIoArray(hPanel);
            var sound = new AtsIoArray(hSound);
            if (!pluginReady)
            {
                sound[0] = -10000;
                Messages.Clear();
                Messages.Add(new Tuple<int, int, int>(state.Time, 12, 0));
                Messages.Add(new Tuple<int, int, int>(state.Time, 13, 0));
            }
            pluginReady = true;
            ackMessage = 0;
            location = state.Location;
            time = state.Time;

            var handles = new AtsHandles { Power = pPower, Brake = pBrake,
                Reverser = pReverser, ConstantSpeed = AtsCscInstruction.Continue };

            double ebSpeed = 0, recommendSpeed = 0, targetSpeed = 0, targetDistance = 0;
            double recommendSpeed_on_dmi = 0;
            trackLimit.Update(location);
            StationManager.Update(state, doorOpen);

            CalculatedLimit maximumCurve = null, targetCurve = null, recommendCurve = null;

            switch (signalMode) {
                case 0:
                    ebSpeed = Config.RMSpeed;
                    recommendSpeed = -10;
                    targetDistance = -10;
                    targetSpeed = -10;
                    driveMode = 2;
                    break;
                case 1:
                    // ITC
                    if (selectedMode > 0 && driveMode == 2) driveMode = 1;
                    maximumCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, Config.RecommendSpeedOffset, movementEndpoint, trackLimit);
                    targetCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, 0, movementEndpoint, trackLimit);
                    recommendCurve = CalculatedLimit.Calculate(location,
                        Config.RecommendDeceleration, 0, StationManager.RecommendCurve(), movementEndpoint, trackLimit);
                    // 释放速度
                    /*if (movementEndpoint.Location - location < Config.ReleaseSpeedDistance 
                        && movementEndpoint.Location > location
                        && state.Speed < Config.ReleaseSpeed && !releaseSpeed) {
                        ackMessage = 2;
                    }*/
                    break;
                case 2:
                    // CTC
                    releaseSpeed = false;
                    movementEndpoint = StationManager.CTCEndpoint();
                    if (selectedMode > 0 && driveMode == 2) driveMode = 1;
                    maximumCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, Config.RecommendSpeedOffset, movementEndpoint,
                        PreTrainManager.GetEndpoint(), trackLimit);
                    targetCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, 0, movementEndpoint, 
                        PreTrainManager.GetEndpoint(), trackLimit);
                    recommendCurve = CalculatedLimit.Calculate(location,
                        Config.RecommendDeceleration, 0, StationManager.RecommendCurve(), 
                        PreTrainManager.GetEndpoint(), movementEndpoint, trackLimit);
                    break;
                default:
                    // fallback
                    ebSpeed = Config.MaxSpeed;
                    recommendSpeed = -10;
                    targetSpeed = 0;
                    targetDistance = -10;
                    break;
            }
            if (maximumCurve != null) {
                // ITC/CTC 有速度曲线
                ebSpeed = Math.Min(Config.MaxSpeed, Math.Max(0, maximumCurve.CurrentTarget));
                recommendSpeed = Math.Min(ebSpeed - Config.RecommendSpeedOffset, 
                    Math.Max(0, recommendCurve.CurrentTarget));
                recommendSpeed_on_dmi = recommendCurve.NextLimit.Location == StationManager.NextStation.StopPosition ? Math.Max(recommendCurve.CurrentTarget, ebSpeed - Config.RecommendSpeedOffset) : Math.Min(ebSpeed - Config.RecommendSpeedOffset,
                    Math.Max(0, recommendCurve.CurrentTarget));
                nextLimit = targetCurve.NextLimit;
                targetDistance = targetCurve.NextLimit.Location - location;
                targetSpeed = targetCurve.NextLimit.Limit;
                if (location > movementEndpoint.Location) {
                    // 如果已冲出移动授权终点，释放速度无效
                    if (releaseSpeed) Log("超出了移动授权终点, 释放速度无效");
                    recommendSpeed = 0;
                    ebSpeed = 0;
                    
                }
            }

            nowSpeed = state.Speed;

            // 显示速度、预选模式、驾驶模式、控制级别、车门模式
            panel_[31] = 3;
            panel_[1] = Convert.ToInt32(Math.Floor(Math.Abs(state.Speed * speedMultiplier)));
            panel_[22] = BMstatus ? 2 : 1;
            panel_[24] = driveMode;
            panel_[25] = signalMode;
            panel_[28] = (driveMode < 2) ? (doorMode - 1) : 4;


            // 显示临时预选模式
            /*if (state.Speed != 0 || time > selectModeStartTime + Config.ModeSelectTimeout * 1000) {
                selectingMode = -1;
                selectModeStartTime = 0;
            }*/
            if (BMsel) {
                panel_[22] = time % 1000 < 500 ? (BMstatus ? 2 : 1) : 0;
            }
            if (!BMstatus && signalMode == 1)
            {
                panel_[22] = time % 1000 < 500 ? 2 : 0;
            }

            // 显示目标速度、建议速度、干预速度
            if (doorOpen) {
                targetDistance = 0;
                targetSpeed = -10;
            }
            panel_[11] = distanceToPixel(targetDistance);
            if (driveMode == 1&& targetDistance > 0 && targetSpeed < ebSpeed - Config.RecommendSpeedOffset)
            {
                    panel_[44] = targetDistance > Config.TargetSpeedShowDistance ? 0 : 1;
            }
            else panel_[44] = 0;
            panel_[19] = (int)targetDistance;
 
            distanceToColor(targetSpeed, targetDistance, panel);
            targetSpeed = Math.Min(targetSpeed, Config.MaxSpeed);
            panel_[17] = (int)targetSpeed;
            panel_[18] = (targetSpeed < 0) ? 1 : 0;
            panel_[29] = 4;

            if (ModesAvailable)
            {
                if (Messages[Messages.Count - 1].Item3 > 0)
                {
                    Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                    Messages.RemoveAt(Messages.Count - 2);
                }
                Messages.Add(new Tuple<int, int, int>(state.Time, 1, 2));
                sound[1] = 1;
                MsglastShowTime = state.Time;
                ModesAvailable = false;
            }

            // 显示出发与屏蔽门信息
            if (signalMode > 1 && state.Speed == 0) {
                if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.DoorEnableWindow
                    && time > StationManager.NextStation.DepartureTime - Config.DepartRequestTime * 1000 && !doorOpen && StationManager.Arrived) {
                    panel_[32] = 1;
                } else if (doorOpen && time - doorOpenTime >= Config.CloseRequestShowTime * 1000) {
                    panel_[32] = 0;
                } else if(StationManager.Arrived&&state.Time < StationManager.NextStation.RouteOpenTime)
                {
                    panel_[32] = 2;
                    if (state.Speed > 0)
                    {
                        if (Messages[Messages.Count - 1].Item3 > 0)
                        {
                            Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                            Messages.RemoveAt(Messages.Count - 2);
                        }
                        Messages.Add(new Tuple<int, int, int>(state.Time, 4, 1));
                        ebState = 1;
                        sound[0] = 1;
                        MsglastShowTime = state.Time;
                    }
                } 
                else {
                    panel_[32] = 3;
                }
            } else {
                panel_[32] = 3;
            }
            if (signalMode >= 2 && state.Speed == 0) {
                if (doorOpen) {
                    if (time - doorOpenTime >= 1000) {
                        panel_[29] = 2;
                    } else {
                        panel_[29] = 4;
                    }
                } else {
                    if (time - doorCloseTime >= 1000) {
                        panel_[29] = 4;
                    } else {
                        panel_[29] = 2;
                    }
                }
            }

            // 如果没有无线电，显示无线电故障
            panel_[23] = driveMode == 2 ? 1 : 0;
            panel_[30] = deviceCapability != 2 ? 0 : 1;

            // ATO
            panel[40] = 0;
            Ato.UpdateAccel(state.Speed, recommendSpeed);
            if (signalMode > 0) {
                if (handles.Power != 0 || handles.Brake != 0 || handles.Reverser != 1) {
                    driveMode = 1;
                }
                if (recommendSpeed == 0 && state.Speed == 0) {
                    driveMode = 1;
                }
                if (doorOpen) driveMode = 1;
                if (driveMode == 0) {
                    panel[40] = 1;
                    var notch = Ato.GetCmdNotch(state.Speed, recommendSpeed, ebSpeed);
                    if (notch < 0) {
                        handles.Power = 0;
                        handles.Brake = -notch;
                        panel_[21] = 1;
                    } else if (notch > 0) {
                        handles.Power = notch;
                        handles.Brake = 0;
                        panel_[21] = 0;
                    } else {
                        handles.Power = 0;
                        handles.Brake = 0;
                        panel_[21] = 2;
                    }
                } else {
                    panel_[21] = 4;
                    if (Ato.IsAvailable()) {
                        // 闪烁
                        panel[40] = time % 1000 < 500 ? 1 : 0;
                    }
                }
            }

            // ATP 制动干预部分
            if (ebSpeed > 0) {
                // 有移动授权
                if (state.Speed == 0 && handles.Brake == vehicleSpec.BrakeNotches + 1) {
                    // 低于制动缓解速度
                    if (ebState > 0) {
                        if (location > movementEndpoint.Location) {
                            // 冲出移动授权终点，要求RM
                            handles.Brake = vehicleSpec.BrakeNotches + 1;
                            recommendSpeed_on_dmi = 0;
                            recommendSpeed = -10;
                            ebSpeed = 0;
                            targetSpeed = 0;
                            targetDistance = -10;
                        } else {
                            handles.Brake = 0;
                            ebState = 0;
                        }
                    }
                    panel_[10] = 0;
                } else if (state.Speed > ebSpeed + 1) {
                    // 超出制动干预速度
                    if (ebState == 0)
                    {
                        if (Messages[Messages.Count - 1].Item3 > 0)
                        {
                            Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                            Messages.RemoveAt(Messages.Count - 2);
                        }
                        Messages.Add(new Tuple<int, int, int>(state.Time, 5, 1));
                        MsglastShowTime = state.Time;
                    }
                    ebState = 2;
                    if (driveMode < 1) driveMode = 1;
                    panel_[10] = 2;
                    panel_[29] = 0;
                    sound[0] = sound[0] == 0 ? -10000 : 1;
                    handles.Brake = vehicleSpec.BrakeNotches + 1;
                } else {
                    if (ebState > 0)
                    {
                        // 刚刚触发紧急制动，继续制动
                        if (ebState == 2) panel_[10] = 2;
                        panel_[29] = 0;
                        sound[0] = sound[0] == 0 ? -10000 : 1;
                        handles.Brake = vehicleSpec.BrakeNotches + 1;
                    }
                    else if (driveMode == 1 && state.Speed > recommendSpeed_on_dmi + 1.5)
                    {
                        // 超出建议速度，显示警告
                        panel_[10] = 1;
                        sound[0] = 0;
                    }
                    else if (driveMode == 2 && state.Speed > 21.5)
                    {
                        // 超出建议速度，显示警告
                        panel_[10] = 1;
                        sound[0] = 0;
                    }
                    else
                    {
                        panel_[10] = 0;
                        sound[0] = -10000;
                    }
                }
            } else if (signalMode == 1) {
                // ITC下冲出移动授权终点。
                if (state.Speed == 0) {
                    // 停稳后降级到RM模式。等待确认。
                    handles.Brake = vehicleSpec.BrakeNotches + 1;
                    recommendSpeed_on_dmi = 0;
                    recommendSpeed = -10;
                    ebSpeed = 0;
                    targetSpeed = 0;
                    targetDistance = -10;
                }
                if (ebState == 0)
                {
                    if (Messages[Messages.Count - 1].Item3 > 0)
                    {
                        Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                        Messages.RemoveAt(Messages.Count - 2);
                    }
                    Messages.Add(new Tuple<int, int, int>(state.Time, 9, 1));
                    MsglastShowTime = state.Time;
                }
                ebState = 2;
                // 显示紧急制动、目标距离0、速度0
                panel_[10] = 2;
                panel_[29] = 0;
                panel_[11] = 0;
                panel_[19] = 0;
                panel_[17] = 0;
                handles.Brake = vehicleSpec.BrakeNotches + 1;
            }

            // 防溜、车门零速保护
            if (state.Speed < 0.5 && handles.Power < 1 && handles.Brake < 1) {
                handles.Brake = 1;
            }
            if (doorOpen) {
                panel_[15] = -10 * speedMultiplier;
                panel_[16] = driveMode == 2 ? -10 : 0;
                if (handles.Brake < 4) handles.Brake = 4;
            }

            // 后退监督: 每1m一次紧制 (先这么做着, 有些地区似乎是先1m之后每次0.5m)
            if (handles.Reverser == -1) {
                if (location > reverseStartLocation) reverseStartLocation = location;
                if (location < reverseStartLocation - Config.ReverseStepDistance) {
                    if (state.Speed == 0 && handles.Power == 0) {
                        reverseStartLocation = location;
                    } else {
                        handles.Brake = vehicleSpec.BrakeNotches + 1;
                    }
                }
            } else if (state.Speed >= 0) {
                reverseStartLocation = Config.LessInf;
            }

            if (state.Speed > 0 && state.Location < StationManager.NextStation.StopPosition + Config.StationEndDistance)
            {
                if (doorOpen)
                {
                    if (ebState == 0)
                    {
                        if (Messages[Messages.Count - 1].Item3 > 0)
                        {
                            Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                            Messages.RemoveAt(Messages.Count - 2);
                        }
                        Messages.Add(new Tuple<int, int, int>(state.Time, 2, 1));
                        MsglastShowTime = state.Time;
                    }
                    ebState = 2;
                }
                else if (doorOpen && panel_[29] == 2)
                {
                    if (ebState == 0)
                    {
                        if (Messages[Messages.Count - 1].Item3 > 0)
                        {
                            Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                            Messages.RemoveAt(Messages.Count - 2);
                        }
                        Messages.Add(new Tuple<int, int, int>(state.Time, 3, 1));
                        MsglastShowTime = state.Time;
                    }
                    ebState = 2;
                }
            }

            // 显示释放速度、确认消息
           /* if (releaseSpeed) panel_[31] = 3;
            if (ackMessage > 0) {
                panel_[35] = ackMessage;
                //panel_[36] = ((state.Time / 1000) % 1 < 0.5) ? 1 : 0;
            } else {
                panel_[35] = 0;
            }*/

            // 显示TDT、车门使能，车门零速保护
            if (StationManager.NextStation != null) {
                int sectogo = Convert.ToInt32((state.Time - StationManager.NextStation.DepartureTime) / 1000);
                if (StationManager.Arrived) {
                    // 已停稳，可开始显示TDT
                    if (location - StationManager.NextStation.StopPosition < Config.TDTFreezeDistance) {
                        // 未发车
                        // 这里先要求至少100m的移动授权
                        if (movementEndpoint.Location - location > 100) {
                            // 出站信号绿灯
                            if (sectogo < 0) {
                                // 未到发车时间
                                panel_[102] = -1;
                            } else {
                                panel_[102] = 1;
                            }
                        } else {
                            // 出站信号红灯
                            panel_[102] = -1;
                        }
                        if (sectogo < 0) {
                            // 未到发车时间
                            panel_[102] = -1;
                        } else {
                            panel_[102] = 1;
                        }
                        panel_[101] = Math.Min(Math.Abs(sectogo), 999);
                    } else {
                        // 已发车
                        panel_[102] = -1;
                    }
                } else {
                    panel_[102] = 0;
                    panel_[101] = 0;
                }
                if (StationManager.NextStation.DepartureTime < 0.1) panel_[102] = 0;
                if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.StationStartDistance) {
                    // 在车站范围内
                    if (signalMode == 2&&!StationManager.NextStation.Pass && StationManager.NextStation.StopPosition - location > Config.StationStartDistance - 50) panel_[29] = 3;
                    if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.DoorEnableWindow) {
                        // 在停车窗口内
                        panel_[26] = StationManager.NextStation.Pass ? 2 : 1;
                        if ((StationManager.NextStation.OpenLeftDoors || StationManager.NextStation.OpenRightDoors)&&state.Speed == 0) panel[27] = 1;
                        else panel[27] = 0;
                        if (state.Speed == 0) {
                            // 停稳, 可以解锁车门, 解锁对应方向车门
                            if (StationManager.NextStation.OpenLeftDoors)
                            {
                                panel_[27] = time % 1000 < 500 ? 0 : 3;
                            }
                            else if (StationManager.NextStation.OpenRightDoors)
                            {
                                panel_[27] = time % 1000 < 500 ? 1 : 3;
                            }
                            else if (StationManager.NextStation.OpenRightDoors && StationManager.NextStation.OpenLeftDoors)
                            {
                                panel[27] = time % 1000 > 500 ? 2 : 3;
                            }
                            else
                            {
                                panel_[27] = 3;
                            }
                            if (doorOpen) {
                                if (StationManager.NextStation.OpenLeftDoors)
                                {
                                    panel_[27] = 0;
                                }
                                else if (StationManager.NextStation.OpenRightDoors)
                                {
                                    panel_[27] = 1;
                                }
                                else if (StationManager.NextStation.OpenRightDoors&&StationManager.NextStation.OpenLeftDoors)
                                {
                                    panel_[27] = 2;
                                } // 切换成已开门图像
                            }
                        } else {
                            panel_[27] = 3;
                        }
                    } else {
                        // 不在停车窗口内
                        panel_[26] = 2;
                        panel_[27] = 3;
                        panel[27] = 0;
                    }
                } else {
                    // 不在车站范围内
                    panel_[26] = 0;
                    panel_[27] = 3;
                    panel[27] = 0;
                }
                if (signalMode == 0) {
                    // RM-IXL, 门要是开了就当它按了门允许, 没有车门使能和停车窗口指示
                    panel_[26] = 0;
                    panel_[27] = doorOpen ? 4 : 3;
                    panel[27] = 0;
                    if (doorOpen&&localised) panel_[29] = deviceCapability != 2 ? 4 : 2;
                }
            }

            if (localised == false || deviceCapability == 0)
            {
                if (driveMode < 2)
                {
                    panel_[26] = 0;
                    panel_[32] = 3;
                    panel_[27] = doorOpen ? 4 : 3;
                    handles.Brake = vehicleSpec.BrakeNotches + 1;
                    recommendSpeed_on_dmi = 0;
                    recommendSpeed = -10;
                    ebSpeed = 0;
                    targetSpeed = 0;
                    targetDistance = -10;
                }
                panel_[31] = 0;
            }

            if (doorOpen)
            {
                recommendSpeed_on_dmi = 0;
                recommendSpeed = -10;
                ebSpeed = 0;
                targetSpeed = 0;
                targetDistance = -10;
            }

            if (driveMode < 2)
            {
                panel_[16] = (int)(ebSpeed * speedMultiplier);
            }
            else
            {
                panel_[16] = -1;
            }
            if (driveMode > 0)
            {
                panel_[15] = driveMode == 2 ? -1 : (int)(recommendSpeed_on_dmi * speedMultiplier);
            }
            else
            {
                panel_[15] = -1;
            }

            if (StationManager.NextStation.Pass && Math.Abs(StationManager.NextStation.StopPosition - location) < Config.StationStartDistance + 200) panel_[31] = 2;

            //手动EB,只能停车后缓解
            if (handles.Brake == vehicleSpec.BrakeNotches + 1 && state.Speed != 0) ebState = 1;
            else if (handles.Brake == vehicleSpec.BrakeNotches + 1 && state.Speed == 0) { panel_[29] = 0; sound[0] = 1; }
            else if (handles.Brake != vehicleSpec.BrakeNotches + 1 && !wheelslip&&!(driveMode == 1 && state.Speed > recommendSpeed_on_dmi + 1.5)&&!(driveMode == 2 && state.Speed > 21.5) &&ebState == 0) sound[0] = -10000;
            //if (handles.Brake == vehicleSpec.BrakeNotches + 1 && state.Speed == 0) panel_[29] = 2;

            //各种按钮和指示灯
            panel[28] = signalMode == 2 ? 1 : 0;//CBTC指示
            if ((driveMode != 2 || RMstatus) && state.Speed < 1) panel[29] = 1;
            else panel[29] = 0;

            if (driveMode==2)
            {
                panel_[43] = 1;
            }
            else
            {
                panel_[43] = 0;
            }

            if (driveMode != 0) { 
                if (handles.Brake > 0)
                {
                    panel_[21] = 1;
                }
                else if (handles.Power > 0)
                {
                    panel_[21] = 0;
                }
                else if (handles.Power == 0 && handles.Brake == 0)
                {
                    panel_[21] = 2;
                } 
            }

            // 信号灯
           /* if (signalMode >= 2) {
                panel_[41] = 2;
            } else {
                if (doorOpen) {
                    if (time - doorOpenTime >= 1000) {
                        panel_[41] = 1;
                    } else {
                        panel_[41] = 0;
                    }
                } else {
                    if (time - doorCloseTime >= 1000) {
                        panel_[41] = 0;
                    } else {
                        panel_[41] = 1;
                    }
                }
            }*/

            if ((BMsel || RMsel) && state.Speed > 0 && ebState == 0)
            {
                if (Messages[Messages.Count - 1].Item3 > 0)
                {
                    Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                    Messages.RemoveAt(Messages.Count - 2);
                }
                Messages.Add(new Tuple<int, int, int>(state.Time, 11, 1));
                ebState = 1;
                sound[0] = 1;
                MsglastShowTime = state.Time;
            }

            if (state.Speed < 0 && handles.Reverser == 1 && ebState == 0)
            {
                if (Messages[Messages.Count - 1].Item3 > 0)
                {
                    Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                    Messages.RemoveAt(Messages.Count - 2);
                }
                Messages.Add(new Tuple<int, int, int>(state.Time, 10, 1));
                ebState = 1;
                sound[0] = 1;
                MsglastShowTime = state.Time;
            }

            if ((StationManager.Arrived&&doorOpen) || state.Location > StationManager.NextStation.StopPosition + Config.StationEndDistance)
            {
                nowNextStationNumber = StationManager.NextStation.NextstationNumber;
            }

            if (msgpos > Messages.Count - 1) msgpos = Messages.Count - 1;
            if (msgpos < 2) msgpos = 2;

            //信息提示栏
            if(Messages[Messages.Count - 1].Item3 > 0)
            {
                if (Messages[Messages.Count - 1].Item3 == 1)
                {
                    upbuttonClickable = false;
                    downbuttonClickable = false;
                    if (Messages.Count == 1)
                    {
                        panel_[59] = 0;
                        panel_[54] = 0;
                        panel_[49] = Messages[0].Item2;
                    }
                    else if (Messages.Count == 2)
                    {
                        panel_[59] = 0;
                        panel_[54] = Messages[0].Item2;
                        panel_[49] = Messages[1].Item2;
                    }
                    else if (Messages.Count == 3)
                    {
                        panel_[59] = Messages[0].Item2;
                        panel_[54] = Messages[1].Item2;
                        panel_[49] = Messages[2].Item2;
                    }

                    else
                    {
                        msgpos = Messages.Count - 1;
                        panel_[59] = Messages[msgpos - 2].Item2;
                        panel_[54] = Messages[msgpos - 1].Item2;
                        panel_[49] = Messages[msgpos].Item2;
                    }
                    panel_[60] = 1;
                    if (state.Time - MsglastShowTime > 5000)
                    {
                        Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                        Messages.RemoveAt(Messages.Count - 2);
                        sound[1] = -10000;
                        panel_[60] = 0;
                    }

                }
                else if (Messages[Messages.Count - 1].Item3 == 2)
                {
                    panel_[60] = 0;
                    upbuttonClickable = false;
                    downbuttonClickable = false;
                    panel_[36] = 1;
                    panel_[59] = 0;
                    panel_[54] = Messages[Messages.Count - 1].Item2;
                    panel_[49] = 0;
                    if (state.Time - MsglastShowTime > 5000)
                    {
                        Messages.Add(new Tuple<int, int, int>(Messages[Messages.Count - 1].Item1, Messages[Messages.Count - 1].Item2, 0));
                        Messages.RemoveAt(Messages.Count - 2);
                        panel_[36] = 0;
                        if (Messages.Count > 3) msgpos = Messages.Count - 1;
                        sound[1] = -10000;
                    }
                }
            }
            else
            {
                if (Messages.Count < 3)
                {
                    upbuttonClickable = false;
                    downbuttonClickable = false;
                }
                else
                {
                    if (msgpos >= Messages.Count - 1) upbuttonClickable = false;
                    else upbuttonClickable = true;
                    if (msgpos <= 2) downbuttonClickable = false;
                    else downbuttonClickable = true;
                }
                if (Messages.Count == 1)
                {
                    panel_[59] = 0;
                    panel_[54] = 0;
                    panel_[49] = Messages[0].Item2;
                }
                else if (Messages.Count == 2)
                {
                    panel_[59] = 0;
                    panel_[54] = Messages[0].Item2;
                    panel_[49] = Messages[1].Item2;
                }
                else if (Messages.Count == 3)
                {
                    panel_[59] = Messages[0].Item2;
                    panel_[54] = Messages[1].Item2;
                    panel_[49] = Messages[2].Item2;
                }

                else
                {
                    panel_[36] = 0;
                    panel_[60] = 0;
                    panel_[59] = Messages[msgpos - 2].Item2;
                    panel_[54] = Messages[msgpos - 1].Item2;
                    panel_[49] = Messages[msgpos].Item2;
                }
            }

            if (state.Time - lastJudgeTime > 100)
            {
                var nowAcc = (state.Speed * state.Speed - lastSpeed * lastSpeed) / state.Location - lastlocation / 2;
                var wheelSpeed = (state.Speed + lastSpeed) / 2;
                var normalSpeed = ((state.Location - lastlocation) / ((state.Time - lastJudgeTime) / 1000)) * 3.6;
                if (panel_[29] != 0 && lastAcc != 0 && lastlocation != 0 && lastJudgeTime != 0)
                {
                    if (Math.Abs(wheelSpeed - normalSpeed) > 3 && Math.Abs(wheelSpeed - normalSpeed) <= 6)
                    {
                        panel_[29] = state.Time % 1000 < 500 ? 1 : 4;
                    }
                    else if (Math.Abs(wheelSpeed - normalSpeed) > 6)
                    {
                        panel_[29] = 1;
                        wheelslip = true;
                    }
                    else
                    {
                        wheelslip = false;
                    }
                }
                lastAcc = nowAcc;
                lastlocation = state.Location;
                lastJudgeTime = state.Time;
                lastSpeed = state.Speed;
            }

            if (wheelslip) sound[0] = 0;

            // 刷新HMI, TDT, 信号机材质，为了减少对FPS影响把它限制到最多一秒10次
            if (lastDrawTime > state.Time) lastDrawTime = 0;
            if (state.Time - lastDrawTime > 200) {
                lastDrawTime = state.Time;
                panel_[42] += 1;
                panel_[42] %= 6;
                TGMTPainter.PaintHMI(panel, state);
                TGMTPainter.PaintTDT(panel, state);
                //TextureManager.UpdateTexture(TextureManager.HmiTexture, TGMTPainter.PaintHMI(panel, state));
                //TextureManager.UpdateTexture(TextureManager.TdtTexture, TGMTPainter.PaintTDT(panel, state));
            }
            if (Touch) { sound[2] = 1; Touch = false; }
            else { sound[2] = -10000; }
            
            return handles;
        }

        

        // 把目标距离折算成距离条上的像素数量。
        private static int distanceToPixel(double targetdistance) {
            int tgpixel = -10;
                if (targetdistance < 1)
                {
                    tgpixel = 0;
                }
                else if (targetdistance < 2)
                {
                    tgpixel = Convert.ToInt32(0 + (targetdistance - 1) / 1 * 20);
                }
                else if (targetdistance < 5)
                {
                    tgpixel = Convert.ToInt32(20 + (targetdistance - 2) / 3 * 25);
                }
                else if (targetdistance < 10)
                {
                    tgpixel = Convert.ToInt32(50 + (targetdistance - 5) / 5 * 20);
                }
                else if (targetdistance < 20)
                {
                    tgpixel = Convert.ToInt32(70 + (targetdistance - 10) / 10 * 20);
                }
                else if (targetdistance < 50)
                {
                    tgpixel = Convert.ToInt32(90 + (targetdistance - 20) / 30 * 25);
                }
                else if (targetdistance < 100)
                {
                    tgpixel = Convert.ToInt32(118 + (targetdistance - 50) / 50 * 20);
                }
                else if (targetdistance < 200)
                {
                    tgpixel = Convert.ToInt32(138 + (targetdistance - 100) / 100 * 20);
                }
                else if (targetdistance < 500)
                {
                    tgpixel = Convert.ToInt32(158 + (targetdistance - 200) / 300 * 25);
                }
                else if (targetdistance < 1000)
                {
                    tgpixel = Convert.ToInt32(185 + (targetdistance - 500) / 500 * 15);
                }
                else
                {
                    tgpixel = 200;
                }
            return tgpixel;
        }

        // 根据把目标距离设定距离条的颜色。
        private static void distanceToColor(double targetspeed, double targetdistance, AtsIoArray panel) {
            if (targetspeed < 0) {
                panel_[12] = 0; panel_[13] = 0; panel_[14] = 0;
            } else if (targetspeed == 0) {
                if (targetdistance < 150) {
                    panel_[12] = 1; panel_[13] = 0; panel_[14] = 0;
                } else if (targetdistance < 300) {
                    panel_[12] = 0; panel_[13] = 1; panel_[14] = 0;
                } else {
                    panel_[12] = 0; panel_[13] = 0; panel_[14] = 1;
                }
            } else if (targetspeed <= 25) {
                if (targetdistance < 300) {
                    panel_[12] = 0; panel_[13] = 1; panel_[14] = 0;
                } else {
                    panel_[12] = 0; panel_[13] = 0; panel_[14] = 1;
                }
            } else if (targetspeed <= 60) {
                if (targetdistance < 150) {
                    panel_[12] = 0; panel_[13] = 1; panel_[14] = 0;
                } else {
                    panel_[12] = 0; panel_[13] = 0; panel_[14] = 1;
                }
            } else {
                panel_[12] = 0; panel_[13] = 0; panel_[14] = 1;
            }
        }

        private static int[] pow10 = new int[] { 1, 10, 100, 1000 };

        private static int D(int src, int digit)
        {
            if (pow10[digit] > src)
            {
                return 10;
            }
            else if (digit == 0 && src == 0)
            {
                return 0;
            }
            else
            {
                return src / pow10[digit] % 10;
            }
        }
    }
}