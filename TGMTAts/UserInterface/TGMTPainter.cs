using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Zbx1425.DXDynamicTexture;

namespace TGMTAts {

    public static class TGMTPainter {
        private static Pen needlePenWhite = new Pen(Color.White, 10);
        private static Pen needlePenOrangeRed = new Pen(Color.OrangeRed, 10);
        private static Pen needlePenRed = new Pen(Color.Red, 10);
        private static Pen circlePenWhite = new Pen(Color.White, 5);
        private static Pen circlePenOrangeRed = new Pen(Color.OrangeRed, 5);
        private static Pen circlePenRed = new Pen(Color.Red, 5);
        private static Pen ackPen = new Pen(Color.Yellow, 4);
        private static Pen msgPenYellow = new Pen(Color.Yellow, 8);
        private static Pen msgPenBlack = new Pen(Color.Black, 8);
        private static Pen msgPenNull = new Pen(Color.FromArgb(191, 190, 190), 8);
        private static Brush[] targetColor = new Brush[] { new SolidBrush(Color.Red), new SolidBrush(Color.Orange), new SolidBrush(Color.Green) };
        private static Brush[] overspeed = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.OrangeRed), new SolidBrush(Color.Red) };
        private static Brush[] targetspeedshow = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(2, 17, 33)) };
        private static Brush[] colonshow = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty),
            new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty),
            new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(2, 17, 33))};
        private static Bitmap hmi, ackcmd, atoctrl, dormode, dorrel, drvmode, emergency, fault, departure, menu,
            selmode, sigmode, special, stopsig, num0, numn0, latestmsg, alive, numn1, num1, message, upbutton, downbutton ,tdt_dmi,bmsel,rmsel;
        private static Bitmap bmp800, bmp1024, bmptdt;
        private static GDIHelper g800, g1024, gtdt;

        public static void Initialize() {
            var imgDir = Config.ImageAssetPath;

            bmp800 = new Bitmap(760, 600, PixelFormat.Format32bppRgb);
            g800 = new GDIHelper(760, 600);
            bmp1024 = new Bitmap(1024, 1024, PixelFormat.Format32bppRgb);
            g1024 = new GDIHelper(1024, 1024);

            bmptdt = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
            gtdt = new GDIHelper(32, 32);

            hmi = new Bitmap(Path.Combine(imgDir, "hmi.png"));
           // ackcmd = new Bitmap(Path.Combine(imgDir, "ackcmd.png"));
            atoctrl = new Bitmap(Path.Combine(imgDir, "atoctrl.png"));
            dormode = new Bitmap(Path.Combine(imgDir, "dormode.png"));
            dorrel = new Bitmap(Path.Combine(imgDir, "dorrel.png"));
            drvmode = new Bitmap(Path.Combine(imgDir, "drvmode.png"));
            emergency = new Bitmap(Path.Combine(imgDir, "emergency.png"));
            fault = new Bitmap(Path.Combine(imgDir, "fault.png"));
            selmode = new Bitmap(Path.Combine(imgDir, "selmode.png"));
            sigmode = new Bitmap(Path.Combine(imgDir, "sigmode.png"));
            special = new Bitmap(Path.Combine(imgDir, "special.png"));
            stopsig = new Bitmap(Path.Combine(imgDir, "stopsig.png"));
            departure = new Bitmap(Path.Combine(imgDir, "departure.png"));
            menu = new Bitmap(Path.Combine(imgDir, "menu.png"));

            num0 = new Bitmap(Path.Combine(imgDir, "num0.png"));
            numn0 = new Bitmap(Path.Combine(imgDir, "num-0.png"));
            num1 = new Bitmap(Path.Combine(imgDir, "num1.png"));
            numn1 = new Bitmap(Path.Combine(imgDir, "num-1.png"));
            latestmsg = new Bitmap(Path.Combine(imgDir, "latestmsg.png"));
            alive = new Bitmap(Path.Combine(imgDir, "alive.png"));
            message = new Bitmap(Path.Combine(imgDir, "message.png"));

            upbutton = new Bitmap(Path.Combine(imgDir, "upbutton_for_click.png"));
            downbutton = new Bitmap(Path.Combine(imgDir, "downbutton_for_click.png"));

            tdt_dmi = new Bitmap(Path.Combine(imgDir, "TDT_DMI.png"));
            bmsel = new Bitmap(Path.Combine(imgDir, "BM_sel_button.png"));
            rmsel = new Bitmap(Path.Combine(imgDir, "RM_sel_button.png"));
        }

        public static void Dispose() {
            bmp800.Dispose();
            bmp1024.Dispose();
            bmptdt.Dispose();
            g800.Dispose();
            g1024.Dispose();
            gtdt.Dispose();
        }
        
        public static void PaintHMI(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) {
            if (TGMTAts.HmiTexture.IsCreated)
            {
                g800.BeginGDI();
                g800.DrawImage(hmi, 0, 0, panel[43] * 600, 730);

                g800.DrawImage(menu, 526, 522, panel[23] * 61, 61);


                if (Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && TGMTAts.signalMode > 0 && state.Speed < 1)
                {
                    int sectogo = Convert.ToInt32((state.Time - StationManager.NextStation.DepartureTime) / 1000);
                    g800.DrawImage(tdt_dmi, 519, 72, 1 * 50, 50);
                    if (sectogo < -99)
                    {
                        g800.DrawImage(numn0, 690, 87, (state.Time % 1000 < 500 ? 9 : 10) * 18, 18);
                        g800.DrawImage(numn0, 676, 87, (state.Time % 1000 < 500 ? 9 : 10) * 18, 18);
                    }
                    else if (sectogo > -1)
                    {
                        g800.DrawImage(numn0, 690, 87, (state.Time % 1000 < 500 ? 0 : 10) * 18, 18);
                        g800.DrawImage(numn0, 676, 87, 10 * 18, 18);
                    }
                    else
                    {
                        g800.DrawImage(numn0, 690, 87, D(-sectogo, 0) * 18, 18);
                        g800.DrawImage(numn0, 676, 87, D(-sectogo, 1) * 18, 18);
                    }

                }
                else
                {
                    g800.DrawImage(tdt_dmi, 519, 72, 0 * 50, 50);
                    g800.DrawImage(numn0, 690, 87, 10 * 18, 18);
                    g800.DrawImage(numn0, 676, 87, 10 * 18, 18);
                }

                //列车信息

                //站信息

                g800.DrawImage(drvmode, 519, 129, panel[24] * 50, 50);
                g800.DrawImage(sigmode, 646, 129, panel[25] * 50, 50);
                g800.DrawImage(stopsig, 646, 194, panel[26] * 50, 50);
                g800.DrawImage(dorrel, 519, 260, panel[27] * 50, 50);
                g800.DrawImage(dormode, 519, 329, panel[28] * 50, 50);
                g800.DrawImage(departure, 646, 260, panel[32] * 50, 50);
                g800.DrawImage(emergency, 646, 329, panel[29] * 50, 50);
                g800.DrawImage(fault, 519, 396, panel[30] * 50, 50);
                g800.DrawImage(special, 646, 396, panel[31] * 50, 50);
                //g800.DrawImage(ackcmd, 472, 453, panel[35] * 100, 100);

                g800.DrawImage(latestmsg, 52, 465, panel[60] * 18, 18);
                g800.DrawImage(message, 138, 467, panel[49] * 18, 18);
                g800.DrawImage(message, 138, 493, panel[54] * 18, 18);
                g800.DrawImage(message, 138, 519, panel[59] * 18, 18);
                g800.DrawImage(num1, 86, 469, panel[45] * 13, 13);
                g800.DrawImage(num1, 96, 469, panel[46] * 13, 13);
                g800.DrawImage(num1, 111, 469, panel[47] * 13, 13);
                g800.DrawImage(num1, 121, 469, panel[48] * 13, 13);
                g800.DrawImage(num1, 86, 495, panel[50] * 13, 13);
                g800.DrawImage(num1, 96, 495, panel[51] * 13, 13);
                g800.DrawImage(num1, 111, 495, panel[52] * 13, 13);
                g800.DrawImage(num1, 121, 495, panel[53] * 13, 13);
                g800.DrawImage(num1, 86, 521, panel[55] * 13, 13);
                g800.DrawImage(num1, 96, 521, panel[56] * 13, 13);
                g800.DrawImage(num1, 111, 521, panel[57] * 13, 13);
                g800.DrawImage(num1, 121, 521, panel[58] * 13, 13);

                g800.DrawImage(atoctrl, 55, 395, panel[21] * 50, 50);
                g800.DrawImage(selmode, 153, 395, panel[22] * 50, 50);

                g800.DrawImage(alive, 20, 550, panel[42] * 45, 45);

                if (panel[18] == 0)
                {
                    g800.DrawImage(num0, 64, 110, D(panel[17], 0) * 18, 18);
                    g800.DrawImage(numn0, 50, 110, D(panel[17], 1) * 18, 18);
                    g800.DrawImage(numn0, 36, 110, D(panel[17], 2) * 18, 18);
                }

                g800.DrawImage(num0, 282, 229, D((int)state.Speed, 0) * 18, 18);
                g800.DrawImage(numn0, 268, 229, D((int)state.Speed, 1) * 18, 18);



                g800.DrawImage(upbutton, 470, 453, Convert.ToInt32(TGMTAts.upbuttonClickable) * 61, 61);
                g800.DrawImage(downbutton, 470, 521, Convert.ToInt32(TGMTAts.downbuttonClickable) * 61, 61);

                if(TGMTAts.BMsel) g800.DrawImage(bmsel, 470, 452);
                if(TGMTAts.RMsel) g800.DrawImage(rmsel, 470, 452);


                string strYMD = System.Text.RegularExpressions.Regex.Replace(DateTime.Now.ToString("yyyyMMdd"), "[^\\d]", "");
                g800.DrawImage(num1, 93, 563, (strYMD[0] - '0') * 13, 13);
                g800.DrawImage(num1, 103, 563, (strYMD[1] - '0') * 13, 13);
                g800.DrawImage(num1, 113, 563, (strYMD[2] - '0') * 13, 13);
                g800.DrawImage(num1, 123, 563, (strYMD[3] - '0') * 13, 13);
                g800.DrawImage(num1, 142, 563, (strYMD[4] - '0') * 13, 13);
                g800.DrawImage(num1, 152, 563, (strYMD[5] - '0') * 13, 13);
                g800.DrawImage(num1, 170, 563, (strYMD[6] - '0') * 13, 13);
                g800.DrawImage(num1, 180, 563, (strYMD[7] - '0') * 13, 13);

                var sec = state.Time / 1000 % 60;
                var min = state.Time / 1000 / 60 % 60;
                var hrs = state.Time / 1000 / 3600 % 60;
                g800.DrawImage(num1, 206, 563, D(hrs, 1) * 13, 13);
                g800.DrawImage(num1, 216, 563, D(hrs, 0) * 13, 13);
                g800.DrawImage(num1, 231, 563, D(min, 1) * 13, 13);
                g800.DrawImage(num1, 241, 563, D(min, 0) * 13, 13);
                g800.DrawImage(num1, 255, 563, D(sec, 1) * 13, 13);
                g800.DrawImage(num1, 265, 563, D(sec, 0) * 13, 13);
                g800.EndGDI();

                if ((TGMTAts.RMsel || TGMTAts.BMsel) && state.Time % 1000 < 500) g800.Graphics.DrawRectangle(ackPen, new Rectangle(470, 452, 280, 125));
                g800.Graphics.FillRectangle(colonshow[panel[46]], new Rectangle(106, 470, 5, 13));
                g800.Graphics.FillRectangle(colonshow[panel[51]], new Rectangle(106, 496, 5, 13));
                g800.Graphics.FillRectangle(colonshow[panel[56]], new Rectangle(106, 522, 5, 13));

                g800.Graphics.FillRectangle(overspeed[panel[10]], new Rectangle(15, 13, 75, 73));
                g800.Graphics.FillRectangle(targetColor[panel[13] * 1 + panel[14] * 2], new Rectangle(68, 354 - panel[11], 25, panel[11]));
                if (panel[36] != 0)
                {
                    if (TGMTAts.time % 1000 < 500) g800.Graphics.DrawRectangle(msgPenYellow, new Rectangle(75, 457, 385, 90));
                    else g800.Graphics.DrawRectangle(msgPenBlack, new Rectangle(75, 457, 385, 90));
                }
                else
                {
                    g800.Graphics.DrawRectangle(msgPenNull, new Rectangle(75, 457, 385, 90));
                }

                var tSpeed = ((double)panel[1] / 100 * 288 - 144) / 180 * Math.PI;
                if (panel[10] == 2)
                {
                    g800.Graphics.DrawEllipse(circlePenRed, new Rectangle(253, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenRed, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(Brushes.Red, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
                }
                else if (panel[10] == 1)
                {
                    g800.Graphics.DrawEllipse(circlePenOrangeRed, new Rectangle(253, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenOrangeRed, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(Brushes.OrangeRed, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
                }
                else
                {
                    g800.Graphics.DrawEllipse(circlePenWhite, new Rectangle(253, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenWhite, Poc(281, 238, 28, 0, tSpeed), Poc(281, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(Brushes.White, new Point[] {
                Poc(281, 238, 145, 0, tSpeed), Poc(281, 238, 108, -5, tSpeed), Poc(281, 238, 108, 5, tSpeed)
                });
                }

                if (panel[15] >= 0)
                {
                    var tRecommend = ((double)panel[15] / 100 * 288 - 144) / 180 * Math.PI;
                    g800.Graphics.FillPolygon(Brushes.Yellow, new Point[] {
                    Poc(281, 238, 155, 0, tRecommend), Poc(281, 238, 175, -9, tRecommend), Poc(281, 238, 175, 9, tRecommend)
                });
                }
                if (panel[16] >= 0)
                {
                    var tLimit = ((double)panel[16] / 100 * 288 - 144) / 180 * Math.PI;
                    g800.Graphics.FillPolygon(Brushes.Red, new Point[] {
                    Poc(281, 238, 155, 0, tLimit), Poc(281, 238, 175, -9, tLimit), Poc(281, 238, 175, 9, tLimit)
                });
                }
                g800.Graphics.FillRectangle(targetspeedshow[panel[44]], new Rectangle(0, 85, 95, 275));
                g1024.Graphics.DrawImageUnscaled(g800.Bitmap, 0, 0);
                TGMTAts.HmiTexture.Update(g1024);
                return;
            }
        }

        public static void PaintTDT(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) {
            if (TGMTAts.TdtTexture.IsCreated)
            {
                gtdt.Graphics.Clear(Color.White);
                double dist = Math.Abs(StationManager.NextStation.StopPosition - state.Location);
                string str;
                if (dist > 100)
                {
                    str = "---";
                }
                else if (dist > 1)
                {
                    str = Math.Round(dist).ToString() + "m";
                }
                else
                {
                    str = Math.Round(dist * 100).ToString() + "cm";
                }
                gtdt.Graphics.DrawString(str, new Font("Arial", 12, GraphicsUnit.Pixel), Brushes.Red, 0, 0);
                TGMTAts.TdtTexture.Update(gtdt);
            }
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

    }
}
