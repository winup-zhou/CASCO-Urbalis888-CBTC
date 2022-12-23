using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using Zbx1425.DXDynamicTexture;

namespace TGMTAts {
	public static partial class TGMTAts {

        public static TouchTextureHandle HmiTexture;
        public static TextureHandle TdtTexture;

        public static int[] panel_ = new int[256];
        public static bool doorOpen;
        public static AtsVehicleSpec vehicleSpec;
        public static double location = -114514;

        public static List<string> debugMessages = new List<string>();

        // 卡斯柯特有的信息提示栏 类型：时间 信息 优先级
        public static List<Tuple<int, int, int>> Messages = new List<Tuple<int, int, int>>();
        public static int msgpos = 2;
        public static int nowNextStationNumber = 0;
        /*
          
        
        // 0: (未选择); 1: AM-BM; 2: AM-C;
        public static int selectedMode = 2;
        // 0: RM; 1: CM; 2: AM; 3: EUM
        public static int driveMode = 1;
        // 0: IXL; 1: BM; 2: CBTC
        public static int signalMode = 2;
        // 1: MM; 2: AM; 3: AA
        public static int doorMode = 1;
        // 0: 无定位区段; 1: CBTC不可用; 2: 正常
        public static int deviceCapability = 2;

        // 暂时的预选速度，-1表示没有在预选
        // 卡斯柯仅有两种预选 AM-BM AM-CBTC
        public static int selectingMode = -1;
        public static int selectModeStartTime = 2;
         */

        // 0: RM; 1: SM-I; 2: SM-C; 3: AM-I; 4: AM-C; 5: XAM
        public static int selectedMode = 4;
        // 0: AM; 1: CM; 2: RM; 3: EUM
        public static int driveMode = 1;
        // 0: IXL; 1: ITC; 2: CTC
        public static int signalMode = 2;
        // 1: MM; 2: AM; 3: AA
        public static int doorMode = 1;
        public static bool ModesAvailable = false;
        public static bool localised = false;
        // 0: 没有CTC,ITC; 1: 没有CTC; 2: 正常
        public static int deviceCapability = 2;

        // 暂时的预选速度，-1表示没有在预选
        public static int selectingMode = -1;
        public static int selectModeStartTime = 0;

        //按钮是否可以被点按？
        public static bool upbuttonClickable = false;
        public static bool downbuttonClickable = false;

        public static int ebState = 0;
        public static bool releaseSpeed = false;
        public static int ackMessage = 0;
        public static bool RMstatus = false;
        public static bool BMstatus = false;
        public static bool RMsel = false;
        public static bool BMsel = false;
        public static bool Touch = false;
        public static bool wheelslip = false;
        public static int destination = 0;
        public static int trainNumber = 0000;
        public static bool CREWIDsel = false;
        public static List<int> CrewID = new List<int>();
        public static List<int> lastCrewID = new List<int>();
        public static float nowSpeed = 0;

        public static double reverseStartLocation = Config.LessInf;
        
        public static TrackLimit trackLimit = new TrackLimit();

        public static Form debugWindow;
        public static bool pluginReady = false;

        public static HarmonyLib.Harmony harmony;

        static TGMTAts() {
            Config.Load(Path.Combine(Config.PluginDir, "UrbalisConfig.txt"));
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        //const string ExpectedHash = "9758E6EA853B042ED49582081371764F43BC8E4DC7955C2B6D949015B984C8E2";

        [DllExport(CallingConvention.StdCall)]
        public static void Load(){
            /*if (FolderHash.Calculate(Config.ImageAssetPath) != ExpectedHash) {
                throw new InvalidDataException("TGMT Image data is not original!");
            }*/

            if (Config.Debug) {
                new Thread(() => {
                    debugWindow = new DebugWindow();
                    Application.Run(debugWindow);
                }).Start();
            }

            TextureManager.Initialize();

            harmony = new HarmonyLib.Harmony("cn.zbx1425.bve.trainguardmt");
            try {
                HmiTexture = TouchManager.Register(Config.HMIImageSuffix,1024,1024);
                TdtTexture = TextureManager.Register(Config.TDTImageSuffix,32,32);
                var imgDir = Config.ImageAssetPath;
                TouchManager.EnableEvent(MouseButtons.Left, TouchManager.EventType.Down);
                HmiTexture.SetClickableArea(450, 0, 370, 600);
                /*upbutton_ = TouchManager.Register(Path.Combine(imgDir, "upbutton_for_click.png"), 64, 64);
                upbutton_.SetClickableArea(0, 0, 50, 61);*/
                HmiTexture.MouseDown += HmiTex_MouseDown;
                /*downbutton_ = TouchManager.Register(Path.Combine(imgDir, "downbutton_for_click.png"), 64, 64);*/
                

                //TextureManager.ApplyPatch();
                TGMTPainter.Initialize();
            } catch (Exception ex) {
               MessageBox.Show(ex.ToString());
            }

        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.Contains("Harmony"))
            {
                var libPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    Config.DTLibPath
                ));
                var fileName = (Environment.Version.Major >= 4) ?
                    "Harmony-net48.dll" : "Harmony-net35.dll";
                return Assembly.LoadFile(Path.Combine(libPath, fileName));
            }
            if (args.Name.Contains("DXDynamicTexture"))
            {
                var libPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    Config.DTLibPath
                ));
                var fileName = (Environment.Version.Major >= 4) ?
                    "Zbx1425.DXDynamicTexture-net48.dll" : "Zbx1425.DXDynamicTexture-net35.dll";
                return Assembly.LoadFile(Path.Combine(libPath, fileName));
            }
            return null;
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetVehicleSpec(AtsVehicleSpec spec) {
            vehicleSpec = spec;
        }

        static void FixIncompatibleModes() {
            if (selectedMode == 0) signalMode = 0; // 预选了IXL
            if (selectedMode == 1 && signalMode > 1) signalMode = 1; // 预选了ITC
            if (selectedMode == 3 && signalMode > 1) signalMode = 1; // 预选了ITC

            if (deviceCapability == 0) signalMode = 0; // 没有TGMT设备
            if (deviceCapability == 1 && signalMode > 1) signalMode = 1; // 没有无线电信号

            if (signalMode > 0 && driveMode == 2) driveMode = 1; // 有信号就至少是SM
            if (signalMode == 0 && driveMode < 3) driveMode = 3; // 没信号就得是RM
        }

        public static int ConvertTime(int human) {
            var hrs = human / 10000;
            var min = human / 100 % 100;
            var sec = human % 100;
            return hrs * 3600 + min * 60 + sec;
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetSignal(int signal){

		}

        [DllExport(CallingConvention.StdCall)]
        public static void Dispose() {
            if (debugWindow != null) debugWindow.Close();
            TGMTPainter.Dispose();
            TextureManager.Dispose();
            Messages.Clear();
        }

        public static void Log(string msg)
        {
            time /= 1000;
            var hrs = time / 3600 % 60;
            var min = time / 60 % 60;
            var sec = time % 60;
            debugMessages.Add(string.Format("{0:D2}:{1:D2}:{2:D2} {3}", hrs, min, sec, msg));
        }
        private static void HmiTex_MouseDown(object sender, TouchEventArgs e)
        {
            if (BMsel)
            {
                if (e.Y >= 513 && e.Y <= 575)
                {
                    if (e.X >= 51 && e.X <= 157)
                    {
                        Touch = true;
                        BMstatus = true;
                        selectedMode = 3;
                        FixIncompatibleModes();
                        BMsel = false;
                    }
                    else if (e.X >= 199 && e.X <= 297)
                    {
                        Touch = true;
                        BMstatus = false;
                        selectedMode = 3;
                        FixIncompatibleModes();
                        selectedMode = 4;
                        BMsel = false;
                    }
                }
            }
            else if (CREWIDsel)
            {
                Touch = false;
                if (e.X >= 62 && e.X <= 291 && e.Y >= 188 && e.Y <= 240)
                {
                    Touch = true;
                    CrewID.Clear();
                }
                else if (e.X >= 62 && e.X <= 114)
                {
                    if (e.Y >= 263 && e.Y <= 315)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(7);
                    }
                    else if (e.Y >= 336 && e.Y <= 390)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(4);
                    }
                    else if (e.Y >= 411 && e.Y <= 465)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(1);
                    }
                    else if (e.Y >= 487 && e.Y <= 538)
                    {
                        Touch = true;
                        CrewID = lastCrewID;
                        CREWIDsel = false;
                    }
                }
                else if (e.X >= 152 && e.X <= 206)
                {
                    if (e.Y >= 263 && e.Y <= 315)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(8);
                    }
                    else if (e.Y >= 336 && e.Y <= 390)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(5);
                    }
                    else if (e.Y >= 411 && e.Y <= 465)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(2);
                    }
                    else if (e.Y >= 487 && e.Y <= 538)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(0);
                    }
                }
                else if (e.X >= 239 && e.X <= 294)
                {
                    Touch = true;
                    if (e.Y >= 263 && e.Y <= 315)
                    {
                        if (CrewID.Count < 3)
                            CrewID.Add(9);
                    }
                    else if (e.Y >= 336 && e.Y <= 390)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(6);
                    }
                    else if (e.Y >= 411 && e.Y <= 465)
                    {
                        Touch = true;
                        if (CrewID.Count < 3)
                            CrewID.Add(3);
                    }
                    else if (e.Y >= 487 && e.Y <= 538)
                    {
                        Touch = true;
                        if (CrewID.Count != 3) CrewID = lastCrewID;
                        CREWIDsel = false;
                    }
                }
            }
            else
            {
                if (e.X >= 5 && e.X <= 62)
                {
                    if (e.Y >= 470 && e.Y <= 527)
                    {
                        if (upbuttonClickable) Touch = true;
                        msgpos += 1;
                    }
                    else if (e.Y >= 538 && e.Y <= 592)
                    {
                        if (downbuttonClickable) Touch = true;
                        msgpos -= 1;
                    }
                }
                else if (e.X >= 246 && e.X <= 336 && e.Y >= 7 && e.Y <= 52)
                {
                    Touch = true;
                    lastCrewID = CrewID;
                    CREWIDsel = true;
                }
            }
            //MessageBox.Show(String.Format("X: {0}, Y: {1}", e.X, e.Y));
        }

    }
}
/*
 UP 5 62 470 527
 DW 5 62 538 592
 ACK 51 157 513 575
 UNACK 199 297 513 575
 CLEAR 62 291 188 240
 7 62 114 263 315
 8 152 206 263 315
 9 239 294 263 315
 4  *   *  336 390
 5  *   *  336 390
 6  *   *  336 390
 3  *   *  411 465
 2  *   *  411 465
 1  *   *  411 465
 N  *   *  487 538
 0  *   *  487 538
 Y  *   *  487 538

 ID  
*/

/*
 if (RMsel)
            {
                if (e.Y >= 505 && e.Y <= 564)
                {
                    if (e.X >= 39 && e.X <= 141)
                    {
                        Touch = true;
                        RMstatus = true;
                        selectedMode = 0;
                        ebState = 0;
                        signalMode = 0;
                        FixIncompatibleModes();
                        RMsel = false;
                    }
                    else if (e.X >= 171 && e.X <= 271)
                    {
                        Touch = true;
                        RMstatus = false;
                        selectedMode = BMstatus ? 3 : 4;
                        ebState = 0;
                        signalMode = 0;
                        FixIncompatibleModes();
                        RMsel = false;
                    }
                }
            }
            else 
 */