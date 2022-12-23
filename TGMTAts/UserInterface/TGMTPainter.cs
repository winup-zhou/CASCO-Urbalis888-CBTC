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
        private static Pen needlePenWhite = new Pen(Color.FromArgb(191, 190, 190), 16);
        private static Pen needlePenOrangeRed = new Pen(Color.OrangeRed, 16);
        private static Pen needlePenRed = new Pen(Color.Red, 16);
        private static Pen circlePenWhite = new Pen(Color.FromArgb(191, 190, 190), 5);
        private static Pen circlePenOrangeRed = new Pen(Color.OrangeRed, 5);
        private static Pen circlePenRed = new Pen(Color.Red, 5);
        private static Pen ackPen = new Pen(Color.Yellow, 4);
        private static Pen msgPenYellow = new Pen(Color.Yellow, 4);
        private static Pen msgPenBlack = new Pen(Color.Black, 4);
        private static Pen msgPenNull = new Pen(Color.FromArgb(191, 190, 190), 4);
        private static Brush[] targetColor = new Brush[] { new SolidBrush(Color.Red), new SolidBrush(Color.Orange), new SolidBrush(Color.Green) };
        private static Brush[] overspeed = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.OrangeRed), new SolidBrush(Color.Red) };
        //private static Brush[] targetspeedshow = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(2, 17, 33)) };
        //private static Brush[] colonshow = new Brush[] { new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty),new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty),new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(2, 17, 33))};
        private static Bitmap hmi, atoctrl, dormode, dorrel, drvmode, emergency, fault, departure, menu,
            selmode, sigmode, special, stopsig, latestmsg, alive, message, upbutton, downbutton, tdt_dmi, bmsel, staname, ARsig, crewid,tgspeed,activate;
        private static Bitmap bmp800, bmp1024, bmptdt;
        private static GDIHelper g800, g1024, gtdt;
        private static StringFormat TDTstringFormat = new StringFormat();
        private static StringFormat targetspeedFormat = new StringFormat();
        public static void Initialize() {
            var imgDir = Config.ImageAssetPath;

            TDTstringFormat.Alignment = StringAlignment.Center;
            targetspeedFormat.Alignment = StringAlignment.Far;
            bmp800 = new Bitmap(800, 600, PixelFormat.Format32bppRgb);
            g800 = new GDIHelper(800, 600);
            bmp1024 = new Bitmap(1024, 1024, PixelFormat.Format32bppRgb);
            g1024 = new GDIHelper(1024, 1024);

            bmptdt = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
            gtdt = new GDIHelper(32, 32);

            hmi = new Bitmap(Path.Combine(imgDir, "DMI.png"));
            atoctrl = new Bitmap(Path.Combine(imgDir, "C1.png"));
            dormode = new Bitmap(Path.Combine(imgDir, "M7.png"));
            dorrel = new Bitmap(Path.Combine(imgDir, "M5.png"));
            drvmode = new Bitmap(Path.Combine(imgDir, "M1.png"));
            emergency = new Bitmap(Path.Combine(imgDir, "M8.png"));//
            fault = new Bitmap(Path.Combine(imgDir, "M9.png"));
            selmode = new Bitmap(Path.Combine(imgDir, "C2-C3.png"));
            sigmode = new Bitmap(Path.Combine(imgDir, "M2.png"));
            special = new Bitmap(Path.Combine(imgDir, "M10.png"));//
            stopsig = new Bitmap(Path.Combine(imgDir, "M4.png"));
            departure = new Bitmap(Path.Combine(imgDir, "M6.png"));//
            menu = new Bitmap(Path.Combine(imgDir, "menu.png"));

            latestmsg = new Bitmap(Path.Combine(imgDir, "NM.png"));
            alive = new Bitmap(Path.Combine(imgDir, "Life.png"));
            message = new Bitmap(Path.Combine(imgDir, "message.png"));

            upbutton = new Bitmap(Path.Combine(imgDir, "UP.png"));
            downbutton = new Bitmap(Path.Combine(imgDir, "DW.png"));
            tgspeed = new Bitmap(Path.Combine(imgDir, "A2.png"));
            tdt_dmi = new Bitmap(Path.Combine(imgDir, "N.png"));
            bmsel = new Bitmap(Path.Combine(imgDir, "F.png"));
            //rmsel = new Bitmap(Path.Combine(imgDir, "RM_sel_button.png"));
            staname = new Bitmap(Path.Combine(imgDir, "stanames.png"));
            ARsig = new Bitmap(Path.Combine(imgDir, "M3.png"));
            crewid = new Bitmap(Path.Combine(imgDir, "CREWID.png"));
            activate = new Bitmap(Path.Combine(imgDir, "C5.png"));
        }

        public static void Dispose() {
            TGMTAts.HmiTexture.Dispose();
            TGMTAts.TdtTexture.Dispose();
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
                g800.DrawImage(hmi, 0, 0, TGMTAts.panel_[43] * 600, 730);

                if (TGMTAts.panel_[23] > 0) g800.DrawImage(menu, 522, 536);

                if (Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && TGMTAts.signalMode > 0 && state.Speed < 1 &&TGMTAts.localised)
                {
                    g800.DrawImage(tdt_dmi, 479, 65, 1 * 64, 64);
                }
                else
                {
                    g800.DrawImage(tdt_dmi, 479, 65, 0 * 64, 64);
                }

                if (TGMTAts.panel_[44] == 1) g800.DrawImage(tgspeed, 1, 148);

                //列车信息
                g800.DrawImage(staname, 570, 11, TGMTAts.destination * 18, 18);
                /*g800.DrawImage(num1, 480, 12, D(TGMTAts.trainNumber, 3) * 13, 13);
                g800.DrawImage(num1, 490, 12, D(TGMTAts.trainNumber, 2) * 13, 13);
                g800.DrawImage(num1, 500, 12, D(TGMTAts.trainNumber, 1) * 13, 13);
                g800.DrawImage(num1, 510, 12, D(TGMTAts.trainNumber, 0) * 13, 13);*/
                
                //站信息
                g800.DrawImage(staname, 115, 37, TGMTAts.nowNextStationNumber * 18, 18);
                g800.DrawImage(staname, 230, 37, TGMTAts.destination * 18, 18);
                /*if (StationManager.NextStation.DepartureTime != 0 && Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && state.Speed < 1)
                {
                    var sec_ = StationManager.NextStation.DepartureTime / 1000 % 60;
                    var min_ = StationManager.NextStation.DepartureTime / 1000 / 60 % 60;
                    var hrs_ = StationManager.NextStation.DepartureTime / 1000 / 3600 % 60;
                    g800.DrawImage(num1, 368, 39, D(hrs_, 1) * 13, 13);
                    g800.DrawImage(num1, 378, 39, D(hrs_, 0) * 13, 13);
                    g800.DrawImage(num1, 393, 39, D(min_, 1) * 13, 13);
                    g800.DrawImage(num1, 403, 39, D(min_, 0) * 13, 13);
                    g800.DrawImage(num1, 418, 39, D(sec_, 1) * 13, 13);
                    g800.DrawImage(num1, 428, 39, D(sec_, 0) * 13, 13);
                }*/

                //司机号
                /*if (TGMTAts.CrewID.Count == 3){
                    g800.DrawImage(numn0b, 720, 20, TGMTAts.CrewID[0] * 18, 18);
                    g800.DrawImage(numn0b, 735, 20, TGMTAts.CrewID[1] * 18, 18);
                    g800.DrawImage(numn0b, 750, 20, TGMTAts.CrewID[2] * 18, 18);
                }
                else
                {
                    g800.DrawImage(numn0b, 720, 20, 10 * 18, 18);
                    g800.DrawImage(numn0b, 735, 20, 10 * 18, 18);
                    g800.DrawImage(numn0b, 750, 20, 10 * 18, 18);
                }*/

                g800.DrawImage(drvmode, 479, 132, TGMTAts.panel_[24] * 64, 64);
                g800.DrawImage(sigmode, 652, 132, TGMTAts.panel_[25] * 64, 64);
                g800.DrawImage(stopsig, 652, 199, TGMTAts.panel_[26] * 64, 64);
                //g800.DrawImage(ARsig, 497, 199, 1 * 64, 64);
                g800.DrawImage(dorrel, 479, 266, TGMTAts.panel_[27] * 64, 64);
                g800.DrawImage(dormode, 479, 333, TGMTAts.panel_[28] * 64, 64);
                g800.DrawImage(departure, 652, 266, TGMTAts.panel_[32] * 64, 64);
                g800.DrawImage(emergency, 652, 333, TGMTAts.panel_[29] * 64, 64);
                g800.DrawImage(fault, 497, 400, TGMTAts.panel_[30] * 64, 64);
                g800.DrawImage(activate, 455, 400, 2 * 64, 64);
                g800.DrawImage(special, 652, 400, TGMTAts.panel_[31] * 64, 64);

                if (TGMTAts.panel_[60] == 1) g800.DrawImage(latestmsg, 41, 478);
                g800.DrawImage(message, 136, 483, TGMTAts.panel_[49] * 18, 18);
                g800.DrawImage(message, 136, 509, TGMTAts.panel_[54] * 18, 18);
                g800.DrawImage(message, 136, 535, TGMTAts.panel_[59] * 18, 18);

                g800.DrawImage(atoctrl, 25, 400, TGMTAts.panel_[21] * 64, 64);
                g800.DrawImage(selmode, 115, 400, TGMTAts.panel_[22] * 64, 64);

                g800.DrawImage(alive, 7, 558, TGMTAts.panel_[42] * 40, 40);

                /*if (TGMTAts.panel_[18] == 0)
                {
                    g800.DrawImage(num0, 64, 110, D(TGMTAts.panel_[17], 0) * 18, 18);
                    g800.DrawImage(numn0, 50, 110, D(TGMTAts.panel_[17], 1) * 18, 18);
                    g800.DrawImage(numn0, 36, 110, D(TGMTAts.panel_[17], 2) * 18, 18);
                }*/

                //g800.DrawImage(num0, 282, 229, D((int)Math.Abs(state.Speed), 0) * 18, 18);
                //g800.DrawImage(numn0, 268, 229, D((int)Math.Abs(state.Speed), 1) * 18, 18);



                if(TGMTAts.upbuttonClickable) g800.DrawImage(upbutton, 454, 469);
                if(TGMTAts.downbuttonClickable) g800.DrawImage(downbutton, 454, 536);

                if(TGMTAts.BMsel) g800.DrawImage(bmsel, 454, 468);
                //if(TGMTAts.RMsel) g800.DrawImage(rmsel, 470, 452);

                //g800.DrawImage(crewid, 453, 1);
                if (TGMTAts.CREWIDsel) {
                    g800.DrawImage(crewid, 453, 1);
                }
                g800.EndGDI();

                if (TGMTAts.Messages[TGMTAts.Messages.Count - 1].Item3 != 2)
                {
                    if (TGMTAts.Messages.Count == 1)
                    {
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[0].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[0].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 480);
                    }
                    else if (TGMTAts.Messages.Count == 2)
                    {
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[0].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[0].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 506);
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[1].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[1].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 480);
                    }
                    else if (TGMTAts.Messages.Count >= 3)
                    {
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[0].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[TGMTAts.msgpos - 2].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 531);
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[1].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[TGMTAts.msgpos - 1].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 506);
                        g800.Graphics.DrawString(Convert.ToString(TGMTAts.Messages[2].Item1 / 1000 / 3600 % 60).PadLeft(2, '0') + ":" + Convert.ToString(TGMTAts.Messages[TGMTAts.msgpos].Item1 / 1000 / 60 % 60).PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 79, 480);
                    }
                }

                g800.Graphics.DrawString(Convert.ToString(Math.Floor(Math.Abs(state.Speed))), new Font("Arial", 26, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 279, 223, TDTstringFormat);
                if(!TGMTAts.CREWIDsel) g800.Graphics.DrawString(Convert.ToString(TGMTAts.trainNumber).PadLeft(4, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 500, 8);

                var strYMD = DateTime.Now.ToString("MM/dd/yyyy");
                var sec = state.Time / 1000 % 60;
                var min = state.Time / 1000 / 60 % 60;
                var hrs = state.Time / 1000 / 3600 % 60;
                g800.Graphics.DrawString(strYMD + "    " + hrs.ToString().PadLeft(2, '0') +":"+ min.ToString().PadLeft(2, '0') + ":" + sec.ToString().PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.White, 93, 565);

                if (!TGMTAts.CREWIDsel&&Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && TGMTAts.signalMode > 0 && state.Speed < 1 && TGMTAts.localised) {
                    int sectogo = Convert.ToInt32((state.Time - StationManager.NextStation.DepartureTime) / 1000);
                    string str = Convert.ToString(-sectogo);
                    if (sectogo < -99)
                    {
                        g800.Graphics.DrawString((state.Time % 1000 < 500 ? "  " : "99"), new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb(191, 190, 190)), 720, 75, TDTstringFormat);
                    }
                    else if (sectogo > -1)
                    {
                        g800.Graphics.DrawString("0", new Font("Arial", 36, FontStyle.Bold ,GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb(191, 190, 190)), 720, 75, TDTstringFormat);
                    }
                    else
                    {
                        g800.Graphics.DrawString(str, new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb(191, 190, 190)), 720, 75, TDTstringFormat);
                    }
                }
                    
                //if (!(StationManager.NextStation.DepartureTime != 0 && Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && state.Speed < 1)) g800.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(2, 17, 33)), new Rectangle(388, 39, 60, 13));
                if ((TGMTAts.RMsel || TGMTAts.BMsel) && state.Time % 1000 < 500) g800.Graphics.DrawRectangle(ackPen, new Rectangle(455, 467, 336, 130));

                if (TGMTAts.CREWIDsel)
                {
                    g800.Graphics.DrawRectangle(ackPen, new Rectangle(453, 2, 345, 596));
                   if (TGMTAts.CrewID.Count > 0)
                    {
                        if (TGMTAts.CrewID.Count == 3)
                        {
                            g800.Graphics.DrawString(Convert.ToString(TGMTAts.CrewID[0]) + Convert.ToString(TGMTAts.CrewID[1]) + Convert.ToString(TGMTAts.CrewID[2]), new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Black, 750, 115, targetspeedFormat);
                        }
                        else if (TGMTAts.CrewID.Count == 2)
                        {
                            g800.Graphics.DrawString(Convert.ToString(TGMTAts.CrewID[0]) + Convert.ToString(TGMTAts.CrewID[1]), new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Black, 750, 115, targetspeedFormat);
                        }
                        else if (TGMTAts.CrewID.Count == 1)
                        {
                            g800.Graphics.DrawString(Convert.ToString(TGMTAts.CrewID[0]), new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Black, 750, 115, targetspeedFormat);
                        }
                    }
                }
                else
                {
                    if (TGMTAts.CrewID.Count == 3) g800.Graphics.DrawString(Convert.ToString(TGMTAts.CrewID[0]) + Convert.ToString(TGMTAts.CrewID[1]) + Convert.ToString(TGMTAts.CrewID[2]), new Font("Arial", 28, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, 740, 13, TDTstringFormat);
                    else g800.Graphics.DrawString("---", new Font("Arial", 28, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, 740, 13,TDTstringFormat);
                }
                /*g800.Graphics.FillRectangle(colonshow[TGMTAts.panel_[46]], new Rectangle(106, 483, 5, 13));
                g800.Graphics.FillRectangle(colonshow[TGMTAts.panel_[51]], new Rectangle(106, 509, 5, 13));
                g800.Graphics.FillRectangle(colonshow[TGMTAts.panel_[56]], new Rectangle(106, 535, 5, 13));*/
                if (StationManager.NextStation.DepartureTime != 0 && Math.Abs(StationManager.NextStation.StopPosition - state.Location) < Config.DoorEnableWindow && state.Speed < 1)
                {
                    var sec_ = StationManager.NextStation.DepartureTime / 1000 % 60;
                    var min_ = StationManager.NextStation.DepartureTime / 1000 / 60 % 60;
                    var hrs_ = StationManager.NextStation.DepartureTime / 1000 / 3600 % 60;
                    g800.Graphics.DrawString(hrs_.ToString().PadLeft(2, '0') + ":" + min_.ToString().PadLeft(2, '0') + ":" + sec_.ToString().PadLeft(2, '0'), new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel),Brushes.White , 350, 34);
                }
                g800.Graphics.FillRectangle(overspeed[TGMTAts.panel_[10]], new Rectangle(7, 2, 64, 64));
                if (TGMTAts.panel_[44] == 1)
                {
                    g800.Graphics.DrawString(Convert.ToString(TGMTAts.panel_[17]), new Font("Arial", 36, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb(191, 190, 190)), 75, 90, targetspeedFormat);
                    g800.Graphics.FillRectangle(targetColor[TGMTAts.panel_[13] * 1 + TGMTAts.panel_[14] * 2], new Rectangle(45, 368 - TGMTAts.panel_[11], 25, TGMTAts.panel_[11]));
                }

                if (TGMTAts.panel_[36] != 0)
                {
                    if (TGMTAts.time % 1000 < 500) g800.Graphics.DrawRectangle(msgPenYellow, new Rectangle(75, 475, 375, 87));
                    else g800.Graphics.DrawRectangle(msgPenBlack, new Rectangle(75, 475, 375, 87));
                }
                else
                {
                    g800.Graphics.DrawRectangle(msgPenNull, new Rectangle(75, 475, 375, 87));
                }

                var tSpeed = ((double)TGMTAts.panel_[1] / 100 * 288 - 144) / 180 * Math.PI;
                if (TGMTAts.panel_[10] == 2)
                {
                    g800.Graphics.DrawEllipse(circlePenRed, new Rectangle(251, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenRed, Poc(279, 238, 28, 0, tSpeed), Poc(279, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(Brushes.Red, new Point[] {
                Poc(279, 238, 145, 0, tSpeed), Poc(279, 238, 108, -4, tSpeed), Poc(279, 238, 108, 4, tSpeed)
                });
                    g800.Graphics.FillPolygon(Brushes.Red, new Point[] {
                Poc(279, 238, 125, 0, tSpeed), Poc(279, 238, 108, -8, tSpeed), Poc(279, 238, 108, 8, tSpeed)
                });
                }
                else if (TGMTAts.panel_[10] == 1)
                {
                    g800.Graphics.DrawEllipse(circlePenOrangeRed, new Rectangle(251, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenOrangeRed, Poc(279, 238, 28, 0, tSpeed), Poc(279, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(Brushes.OrangeRed, new Point[] {
                Poc(279, 238, 145, 0, tSpeed), Poc(279, 238, 108, -4, tSpeed), Poc(279, 238, 108, 4, tSpeed)
                });
                    g800.Graphics.FillPolygon(Brushes.OrangeRed, new Point[] {
                Poc(279, 238, 125, 0, tSpeed), Poc(279, 238, 108, -8, tSpeed), Poc(279, 238, 108, 8, tSpeed)
                });
                }
                else
                {
                    g800.Graphics.DrawEllipse(circlePenWhite, new Rectangle(251, 210, 56, 56));
                    g800.Graphics.DrawLine(needlePenWhite, Poc(281, 238, 28, 0, tSpeed), Poc(279, 238, 110, 0, tSpeed));
                    g800.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(191, 190, 190)), new Point[] {
                Poc(279, 238, 145, 0, tSpeed), Poc(279, 238, 108, -4, tSpeed), Poc(279, 238, 108, 4, tSpeed)
                });
                    g800.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(191, 190, 190)), new Point[] {
                Poc(279, 238, 125, 0, tSpeed), Poc(279, 238, 108, -8, tSpeed), Poc(279, 238, 108, 8, tSpeed)
                });
                }

                if (TGMTAts.panel_[15] >= 0)
                {
                    var tRecommend = ((double)TGMTAts.panel_[15] / 100 * 288 - 144) / 180 * Math.PI;
                    g800.Graphics.FillPolygon(Brushes.Yellow, new Point[] {
                    Poc(279, 238, 155, 0, tRecommend), Poc(279, 238, 175, -9, tRecommend), Poc(279, 238, 175, 9, tRecommend)
                });
                }
                if (TGMTAts.panel_[16] >= 0)
                {
                    var tLimit = ((double)TGMTAts.panel_[16] / 100 * 288 - 144) / 180 * Math.PI;
                    g800.Graphics.FillPolygon(Brushes.Red, new Point[] {
                    Poc(279, 238, 155, 0, tLimit), Poc(279, 238, 175, -9, tLimit), Poc(279, 238, 175, 9, tLimit)
                });
                }
                //g800.Graphics.FillRectangle(targetspeedshow[TGMTAts.panel_[44]], new Rectangle(0, 85, 95, 275));
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
