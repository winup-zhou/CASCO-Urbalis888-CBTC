using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TGMTAts {
    public static class StationManager {

        public class Station {
            public int StopPosition = (int)Config.LessInf;
            public int RouteOpenTime = 0;
            public int DepartureTime = 0;
            public int DoorOpenType = 0;
            public bool Pass = false;
            public int Stationtype = 0;//下一站类别 0:正线车站 1:进库转换轨 2:出库转换轨 3:折返线/存车线
            public int NextstationNumber = 0;
            public int ATBtype = 0;//下一站ATB类别 0:不可用 1:不可按压但可用 2:可按压且可用
            public bool OpenLeftDoors { get { return DoorOpenType == 1 || DoorOpenType == 3; } }
            public bool OpenRightDoors { get { return DoorOpenType == 2 || DoorOpenType == 3; } }
        }

        public static Station NextStation = new Station();

        public static bool Stopped;

        public static bool Arrived;

        public static void SetBeacon(TGMTAts.AtsBeaconData data) {
            switch (data.Type) {
                case 96820:
                    NextStation.StopPosition = Math.Abs(data.Optional);
                    NextStation.Pass = data.Optional < 0;
                    TGMTAts.Log("车站停车位置 " + NextStation.StopPosition.ToString());
                    break;
                case 96821:
                    NextStation.DoorOpenType = data.Optional;
                    break;
                case 96822:
                    NextStation.RouteOpenTime = TGMTAts.ConvertTime(data.Optional) * 1000;
                    break;
                case 96823:
                    NextStation.DepartureTime = TGMTAts.ConvertTime(data.Optional) * 1000;
                    break;
                case 96824:
                    NextStation.NextstationNumber = TGMTAts.ConvertTime(data.Optional);
                    break;
                case 96827:
                    NextStation.Stationtype = data.Optional;
                    break;
            }
        }

        public static void Update(TGMTAts.AtsVehicleState state, bool doorState) {
            if(NextStation.Stationtype == 0)
            {
                if (!NextStation.Pass)
                {
                    if (state.Speed == 0 && state.Location > NextStation.StopPosition - Config.StationStartDistance)
                    {
                        if (!Stopped) TGMTAts.Log("已在站内停稳");
                        Stopped = true;
                    }
                    if (doorState)
                    {
                        if (!Arrived) TGMTAts.Log("已开门");
                        Arrived = true;
                    }
                    if (state.Location > NextStation.StopPosition + Config.StationEndDistance)
                    {
                        NextStation = new Station();
                        Stopped = false;
                        Arrived = false;
                        TGMTAts.Log("已出站");
                    }
                }
                else
                {
                    if (state.Location > NextStation.StopPosition + Config.StationEndDistance)
                    {
                        NextStation = new Station();
                        Stopped = false;
                        Arrived = false;
                        TGMTAts.Log("已出站");
                    }
                }
            }
            else if(NextStation.Stationtype == 1)
            {
                //进库转换轨
                if (state.Location > NextStation.StopPosition + Config.StationEndDistance)
                {
                    TGMTAts.localised = false;
                }
            }
            else if (NextStation.Stationtype == 2)
            {
                //出库转换轨
            }
            
        }

        public static SpeedLimit RecommendCurve()
        {
            if (NextStation.Pass) {
                return SpeedLimit.inf;
            }
            else
            {
                if (NextStation.StopPosition >= (int)Config.LessInf)
                {
                    return SpeedLimit.inf;
                }
                else if (Arrived)
                {
                    return SpeedLimit.inf;
                }
                else if (Stopped)
                {
                    return SpeedLimit.inf;
                }
                else
                {
                    return new SpeedLimit(0, NextStation.StopPosition);
                }
            }
        }

        public static SpeedLimit CTCEndpoint() {
            if (NextStation.Stationtype == 0)
            {
                if (TGMTAts.time > NextStation.RouteOpenTime)
                {
                    return SpeedLimit.inf;
                }
                else
                {
                    return new SpeedLimit(0, NextStation.StopPosition + Config.StationMotionEndpoint);
                }
            }
            else
            {
                if (NextStation.Stationtype == 2) return new SpeedLimit(0, NextStation.StopPosition + Config.StationMotionEndpoint);
                else return SpeedLimit.inf;
            }
        }
    }
}
