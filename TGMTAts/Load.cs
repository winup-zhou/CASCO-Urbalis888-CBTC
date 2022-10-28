using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;

namespace TGMTAts {
	public static partial class TGMTAts {

        public static int[] panel = new int[256];
        public static bool doorOpen;
        public static AtsVehicleSpec vehicleSpec;
        public static double location = -114514;

        public static List<string> debugMessages = new List<string>();
        // 卡斯柯特有的信息提示栏 类型：时间 信息
        public static List<Tuple<int, int, int>> Messages = new List<Tuple<int, int, int>>();
        
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

        public static int ebState = 0;
        public static bool releaseSpeed = false;
        //public static int ackMessage = 0;

        public static double reverseStartLocation = Config.LessInf;
        
        public static TrackLimit trackLimit = new TrackLimit();

        public static Form debugWindow;
        public static bool pluginReady = false;

        public static HarmonyLib.Harmony harmony;



        static TGMTAts() {
            Config.Load(Path.Combine(Config.PluginDir, "TGMTConfig.txt"));
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        const string ExpectedHash = "9758E6EA853B042ED49582081371764F43BC8E4DC7955C2B6D949015B984C8E2";

        [DllExport(CallingConvention.StdCall)]
        public static void Load(){
            Zbx1425.DXDynamicTexture.TextureManager.Initialize();
            if (FolderHash.Calculate(Config.ImageAssetPath) != ExpectedHash) {
                throw new InvalidDataException("TGMT Image data is not original!");
            }

            if (Config.Debug) {
                new Thread(() => {
                    debugWindow = new DebugWindow();
                    Application.Run(debugWindow);
                }).Start();
            }

            harmony = new HarmonyLib.Harmony("cn.zbx1425.bve.trainguardmt");
            try {
                TextureManager.ApplyPatch();
                TGMTPainter.Initialize();
            } catch (Exception ex) {
               MessageBox.Show(ex.ToString());
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.Contains("Harmony")) {
                return Assembly.LoadFile(Config.HarmonyPath);
            }
            if (args.Name.Contains("DXDynamicTexture"))
            {
                var libPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    Config.DXDynamicTexturePath
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

            if (signalMode > 0 && driveMode == 0) driveMode = 1; // 有信号就至少是SM
            if (signalMode == 0 && driveMode > 0) driveMode = 0; // 没信号就得是RM
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
        }

        public static void Log(string msg) {
            time /= 1000;
            var hrs = time / 3600 % 60;
            var min = time / 60 % 60;
            var sec = time % 60;
            debugMessages.Add(string.Format("{0:D2}:{1:D2}:{2:D2} {3}", hrs, min, sec, msg));
        }
	}
}