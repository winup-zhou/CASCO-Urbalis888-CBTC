using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TGMTAts {

    public static class TGMTPainter {

        public static Bitmap bmp800, bmp1024, bmptdt;
        public static Graphics g800, g1024, gtdt;

        public static void Initialize() {
            var imgDir = Config.ImageAssetPath;

            bmp800 = new Bitmap(760, 600, PixelFormat.Format32bppRgb);
            g800 = Graphics.FromImage(bmp800);
            bmp1024 = new Bitmap(1024, 1024, PixelFormat.Format32bppRgb);
            g1024 = Graphics.FromImage(bmp1024);
            GDI32.BindGraphics(g800);

            bmptdt = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
            gtdt = Graphics.FromImage(bmptdt);

            hmi = GDI32.LoadImage(Path.Combine(imgDir, "hmi.png"));
            ackcmd = GDI32.LoadImage(Path.Combine(imgDir, "ackcmd.png"));
            atoctrl = GDI32.LoadImage(Path.Combine(imgDir, "atoctrl.png"));
            dormode = GDI32.LoadImage(Path.Combine(imgDir, "dormode.png"));
            dorrel = GDI32.LoadImage(Path.Combine(imgDir, "dorrel.png"));
            drvmode = GDI32.LoadImage(Path.Combine(imgDir, "drvmode.png"));
            emergency = GDI32.LoadImage(Path.Combine(imgDir, "emergency.png"));
            fault = GDI32.LoadImage(Path.Combine(imgDir, "fault.png"));
            selmode = GDI32.LoadImage(Path.Combine(imgDir, "selmode.png"));
            sigmode = GDI32.LoadImage(Path.Combine(imgDir, "sigmode.png"));
            special = GDI32.LoadImage(Path.Combine(imgDir, "special.png"));
            stopsig = GDI32.LoadImage(Path.Combine(imgDir, "stopsig.png"));
            departure = GDI32.LoadImage(Path.Combine(imgDir, "departure.png"));
            menu = GDI32.LoadImage(Path.Combine(imgDir, "menu.png"));

            num0 = GDI32.LoadImage(Path.Combine(imgDir, "num0.png"));
            numn0 = GDI32.LoadImage(Path.Combine(imgDir, "num-0.png"));
            num1 = GDI32.LoadImage(Path.Combine(imgDir, "num1.png"));
            numn1 = GDI32.LoadImage(Path.Combine(imgDir, "num-1.png"));
            colon = GDI32.LoadImage(Path.Combine(imgDir, "colon.png"));
            alive = GDI32.LoadImage(Path.Combine(imgDir, "alive.png"));
        }

        public static void Dispose() {
            GDI32.FreeImage();
            GDI32.FreeGraphics(g800);
        }
        
        public static Bitmap PaintHMI(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) { 
            GDI32.DrawImage(hmi, 0, 0, panel[43] * 600, 730);

            GDI32.DrawImage(menu, 665, 60, panel[23] * 60, 60);
            GDI32.DrawImage(drvmode, 519, 133, panel[24] * 50, 50);
            GDI32.DrawImage(sigmode, 646, 133, panel[25] * 50, 50);
            GDI32.DrawImage(stopsig, 646, 200, panel[26] * 50, 50);
            GDI32.DrawImage(dorrel, 519, 267, panel[27] * 50, 50);
            GDI32.DrawImage(dormode, 519, 337, panel[28] * 50, 50);
            GDI32.DrawImage(departure, 646, 267, panel[32] * 50, 50);
            GDI32.DrawImage(emergency, 646, 337, panel[29] * 50, 50);
            GDI32.DrawImage(fault, 519, 405, panel[30] * 50, 50);
            GDI32.DrawImage(special, 646, 405, panel[31] * 50, 50);
            GDI32.DrawImage(ackcmd, 440, 472, panel[35] * 100, 100);
            GDI32.DrawImage(atoctrl, 32, 395, panel[21] * 50, 50);
            GDI32.DrawImage(selmode, 150, 395, panel[22] * 50, 50);

            GDI32.DrawImage(alive, 20, 550, panel[42] * 45, 45);

            if (panel[18] == 0) {
                GDI32.DrawImage(num0, 64, 110, D(panel[17], 0) * 18, 18);
                GDI32.DrawImage(numn0, 50, 110, D(panel[17], 1) * 18, 18);
                GDI32.DrawImage(numn0, 36, 110, D(panel[17], 2) * 18, 18);
            }

            GDI32.DrawImage(num0, 282, 229, D((int)state.Speed, 0) * 18, 18);
            GDI32.DrawImage(numn0, 268, 229, D((int)state.Speed, 1) * 18, 18);

            
            string strYMD = System.Text.RegularExpressions.Regex.Replace(DateTime.Now.ToString("yyyyMMdd"),"[^\\d]","");
            GDI32.DrawImage(num1, 93, 563, (strYMD[0] - '0') * 13, 13);
            GDI32.DrawImage(num1, 103, 563, (strYMD[1] - '0') * 13, 13);
            GDI32.DrawImage(num1, 113, 563, (strYMD[2] - '0') * 13, 13);
            GDI32.DrawImage(num1, 123, 563, (strYMD[3] - '0') * 13, 13);
            GDI32.DrawImage(num1, 142, 563, (strYMD[4] - '0') * 13, 13);
            GDI32.DrawImage(num1, 152, 563, (strYMD[5] - '0') * 13, 13);
            GDI32.DrawImage(num1, 171, 563, (strYMD[6] - '0') * 13, 13);
            GDI32.DrawImage(num1, 181, 563, (strYMD[7] - '0') * 13, 13);

            var sec = state.Time / 1000 % 60;
            var min = state.Time / 1000 / 60 % 60;
            var hrs = state.Time / 1000 / 3600 % 60;
            GDI32.DrawImage(num1, 207, 563, D(hrs, 1) * 13, 13);
            GDI32.DrawImage(num1, 217, 563, D(hrs, 0) * 13, 13);
            GDI32.DrawImage(num1, 232, 563, D(min, 1) * 13, 13);
            GDI32.DrawImage(num1, 242, 563, D(min, 0) * 13, 13);
            GDI32.DrawImage(num1, 256, 563, D(sec, 1) * 13, 13);
            GDI32.DrawImage(num1, 266, 563, D(sec, 0) * 13, 13);


            g800.FillRectangle(overspeed[panel[10]], new Rectangle(15, 13, 75, 73));
            g800.FillRectangle(targetColor[panel[13] * 1 + panel[14] * 2], new Rectangle(68, 354 - panel[11], 25, panel[11]));
            if (panel[36] != 0 && TGMTAts.time % 1000 < 500) {
                g800.DrawRectangle(ackPen, new Rectangle(438, 470, 280, 100));
            }

            var tSpeed = ((double)panel[1] / 100 * 288 - 144) / 180 *Math.PI;
            if (panel[10] == 2)
            {
                g800.DrawEllipse(circlePenRed, new Rectangle(253, 210, 56, 56));
                g800.DrawLine(needlePenRed, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                g800.FillPolygon(Brushes.Red, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
            }
            else if (panel[10] == 1)
            {
                g800.DrawEllipse(circlePenOrangeRed, new Rectangle(253, 210, 56, 56));
                g800.DrawLine(needlePenOrangeRed, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                g800.FillPolygon(Brushes.OrangeRed, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
            }
            else
            {
                g800.DrawEllipse(circlePenWhite, new Rectangle(253, 210, 56, 56));
                g800.DrawLine(needlePenWhite, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                g800.FillPolygon(Brushes.White, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
            }
            
            if (panel[15] >= 0) {
                var tRecommend = ((double)panel[15] / 100 * 288 - 144) / 180 * Math.PI;
                g800.FillPolygon(Brushes.Yellow, new Point[] {
                    Poc(281, 238, 155, 0, tRecommend), Poc(281, 238, 175, -9, tRecommend), Poc(281, 238, 175, 9, tRecommend)
                });
            }
            if (panel[16] >= 0) {
                var tLimit = ((double)panel[16] / 100 * 288 - 144) / 180 * Math.PI;
                g800.FillPolygon(Brushes.Red, new Point[] {
                    Poc(281, 238, 155, 0, tLimit), Poc(281, 238, 175, -9, tLimit), Poc(281, 238, 175, 9, tLimit)
                });
            }
            /*//°´Å¥½»»¥Ô¤Áô
             
             */
            g800.FillRectangle(targetspeedshow[panel[44]], new Rectangle(0, 85, 95 ,275));

            g1024.DrawImageUnscaled(bmp800, 0, 0);
            return bmp1024;
        }

        public static Bitmap PaintTDT(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) {
            gtdt.Clear(Color.White);
            double dist = Math.Abs(StationManager.NextStation.StopPosition - state.Location);
            string str;
            if (dist > 100) {
                str = "---";
            } else if (dist > 1) {
                str = Math.Round(dist).ToString() + "m";
            } else {
                str = Math.Round(dist * 100).ToString() + "cm";
            }
            gtdt.DrawString(str, new Font("Arial", 12, GraphicsUnit.Pixel), Brushes.Red, 0, 0);
            return bmptdt;
        }

        static int[] pow10 = new int[] { 1, 10, 100, 1000 };

        static int D(int src, int digit) {
            if (pow10[digit] > src) {
                return 10;
            } else if (digit == 0 && src == 0) {
                return 0;
            } else {
                return src / pow10[digit] % 10;
            }
        }

        static Point Poc(int cx, int cy, int dr, int dt, double theta) {
            return new Point(
                (int)(cx + dr * Math.Sin(theta) + dt * Math.Cos(theta)),
                (int)(cy - dr * Math.Cos(theta) + dt * Math.Sin(theta))
            );
        }

        static Pen needlePenWhite = new Pen(Color.White, 10);
        static Pen needlePenOrangeRed = new Pen(Color.OrangeRed, 10);
        static Pen needlePenRed = new Pen(Color.Red, 10);
        static Pen circlePenWhite = new Pen(Color.White, 5);
        static Pen circlePenOrangeRed = new Pen(Color.OrangeRed, 5);
        static Pen circlePenRed = new Pen(Color.Red, 5);
        static Pen ackPen = new Pen(Color.Yellow, 4);
        static Brush[] targetColor = new Brush[] { new SolidBrush(Color.Red), new SolidBrush(Color.OrangeRed), new SolidBrush(Color.Green) };
        static Brush[] overspeed = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.OrangeRed), new SolidBrush(Color.Red)};
        static Brush[] targetspeedshow = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(2, 17, 33)) };
        static int hmi, ackcmd, atoctrl, dormode, dorrel, drvmode, emergency, fault, departure, menu,
            selmode, sigmode, special, stopsig, num0, numn0, colon, alive, numn1, num1;
    }
}
