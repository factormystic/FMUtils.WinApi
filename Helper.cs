using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FMUtils.WinApi
{
    public static class Helper
    {
        public enum WindowLocation { UpperLeft, UpperRight, LowerLeft, LowerRight, Center };
        public enum VisualStyles { Classic, Basic, Aero };
        public enum OperatingSystems { Unknown, WinXP, WinVista, Win7, Win8 };

        public static VisualStyles VisualStyle
        {
            get
            {
                //XP doesn't have dwmapi.dll, so attempting this call on that platform results in a DllNotFound exception for the P/Invoke
                //We can avoid all other calls to that dll if isGlassEnabled is false
                if (Environment.OSVersion.Version.Major < 6)
                {
                    return Application.RenderWithVisualStyles ? VisualStyles.Basic : VisualStyles.Classic;
                }
                else
                {
                    if (!Application.RenderWithVisualStyles)
                    {
                        return VisualStyles.Classic;
                    }
                    else
                    {
                        bool composition = false;
                        DWM.DwmIsCompositionEnabled(ref composition);
                        return composition ? VisualStyles.Aero : VisualStyles.Basic;
                    }
                }
            }
        }

        public static OperatingSystems OperatingSystem
        {
            get
            {
                if (Environment.OSVersion.Version.CompareTo(new Version(6, 2)) > 0)
                    return OperatingSystems.Win8;

                if (Environment.OSVersion.Version.CompareTo(new Version(6, 1)) > 0)
                    return OperatingSystems.Win7;

                if (Environment.OSVersion.Version.CompareTo(new Version(6, 0)) > 0)
                    return OperatingSystems.WinVista;

                if (Environment.OSVersion.Version.CompareTo(new Version(5, 0)) > 0)
                    return OperatingSystems.WinXP;

                return OperatingSystems.Unknown;
            }
        }

        public static string GetWindowText(IntPtr Handle, bool sanitized)
        {
            string WindowText = null;

            StringBuilder sb = new StringBuilder(255);
            if (Windowing.GetWindowText(Handle, sb, sb.Capacity) > 0)
            {
                WindowText = sb.ToString();

                if (sanitized)
                    foreach (char c in Path.GetInvalidFileNameChars())
                        WindowText = WindowText.Replace(c, '-');

                return WindowText;
            }
            else
            {
                //Win32Exception Win32Ex = new Win32Exception(Marshal.GetLastWin32Error());
                //if (!(Win32Ex.NativeErrorCode == 0 || Win32Ex.NativeErrorCode == 1400))
                //    throw Win32Ex;
            }

            if (string.IsNullOrEmpty(WindowText))
            {
                int pid = 0;
                Windowing.GetWindowThreadProcessId(new HandleRef(null, Handle), out pid);
                try
                {
                    WindowText = Process.GetProcessById(pid).ProcessName;
                }
                catch
                {
                    //don't care
                    WindowText = "(screenshot)";
                }
            }

            return WindowText;
        }

        public static bool HasNonClientBorder(IntPtr Handle)
        {
            return (Windowing.GetWindowLong(Handle, Windowing.GWL_STYLE) & Windowing.WS_BORDER) != 0;
        }

        public static bool IsWindowMazimized(IntPtr Handle)
        {
            Windowing.WINDOWPLACEMENT placement = new Windowing.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            Windowing.GetWindowPlacement(Handle, out placement);

            return placement.showCmd == 3;

            //Rectangle WorkingAreaRect = Screen.FromHandle(Handle).WorkingArea;
            //Rectangle WindowRect = GetWindowRectangle(Handle);

            //return WindowRect.Equals(WorkingAreaRect);

            ////unreliable, for example, with Zune
            ////return (Unmanaged.GetWindowLong(Handle, Unmanaged.GWL_STYLE) & Unmanaged.WS_MAXIMIZE) != 0;
        }

        public static void ShowBehind(Form frm, IntPtr targetWindow)
        {
            Windowing.ShowWindow(frm.Handle, Windowing.SW_SHOWNOACTIVATE);
            Windowing.SetWindowPos(frm.Handle.ToInt32(), Windowing.HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, 0);//Windowing.SWP_NOACTIVATE);
        }

        public static bool IsSquareWindowEdge(IntPtr Handle)
        {
            bool isMaximized = IsWindowMazimized(Handle);
            if (isMaximized)
                return true;

            //Toolwindow = Gimp toolbox
            //Zune, Steam seem to have both Group and Tabstop
            int ExStyle = Windowing.GetWindowLong(Handle, Windowing.GWL_EXSTYLE);
            int Style = Windowing.GetWindowLong(Handle, Windowing.GWL_STYLE);

            bool toolwin = (ExStyle & Windowing.WS_EX_TOOLWINDOW) != 0;
            bool windowedge = (ExStyle & Windowing.WS_EX_WINDOWEDGE) != 0;
            bool popup = (Style & Windowing.WS_POPUP) != 0;
            bool uix = GetClassName(Handle) == "UIX Render Window";

            if (((toolwin || popup) && !windowedge) || uix)
                return true;
            else
                return false;

            //((Unmanaged.GetWindowLong(Handle, Unmanaged.GWL_STYLE) & Unmanaged.WS_GROUP) != 0 && (Unmanaged.GetWindowLong(Handle, Unmanaged.GWL_STYLE) & Unmanaged.WS_TABSTOP) != 0);
        }

        public static string GetClassName(IntPtr Handle)
        {
            StringBuilder sb = new StringBuilder(255);
            WinApi.Windowing.GetClassName(Handle, sb, sb.Capacity);
            return sb.ToString();
        }

        public static Rectangle GetWindowRectangle(IntPtr Handle)
        {
            Windowing.RECT area = new Windowing.RECT();
            Windowing.GetWindowRect(Handle, out area);

            //if (isDialogWindow(Handle))
            //{
            //    area.Left -= 5;
            //    area.Right += 5;
            //    area.Top -= 5;
            //    area.Bottom += 5;
            //}

            return new Rectangle(area.Left, area.Top, area.Width, area.Height);
        }

        public static bool isDialogWindow(IntPtr Handle)
        {
            return (Windowing.GetWindowLong(Handle, Windowing.GWL_EXSTYLE) & Windowing.WS_EX_DLGMODALFRAME) != 0;
            //||

            //    //Also check for dialog frame (but since that will also be true if mystery stype is true, check that that isn't there)
            //    ((Windowing.GetWindowLong(Handle, Windowing.GWL_STYLE) & Windowing.WS_DLGFRAME) != 0 &&
            //    !((Windowing.GetWindowLong(Handle, Windowing.GWL_STYLE) & 0xC000) != 0));
        }

        public static bool isCompositionEnabled
        {
            get
            {
                bool pfEnabled = false;
                DWM.DwmIsCompositionEnabled(ref pfEnabled);
                return pfEnabled;
            }
        }

        public static DWM.DWM_COLORIZATION_PARAMS GetColorization()
        {
            DWM.DWM_COLORIZATION_PARAMS colorization = new DWM.DWM_COLORIZATION_PARAMS();
            DWM.DwmGetColorizationParameters(out colorization);
            return colorization;
        }

        public static DockStyle GetTaskbarEdge()
        {
            IntPtr taskBarWnd = Windowing.FindWindow("Shell_TrayWnd", null);

            Windowing.APPBARDATA abd = new Windowing.APPBARDATA();
            abd.hWnd = taskBarWnd;
            Windowing.SHAppBarMessage(Windowing.ABM_GETTASKBARPOS, ref abd);

            if (abd.rc.Top == abd.rc.Left && abd.rc.Bottom > abd.rc.Right)
            {
                return DockStyle.Left;
            }
            else if (abd.rc.Top == abd.rc.Left && abd.rc.Bottom < abd.rc.Right)
            {
                return DockStyle.Top;
            }
            else if (abd.rc.Top > abd.rc.Left)
            {
                return DockStyle.Bottom;
            }
            else
            {
                return DockStyle.Right;
            }
        }

        public static bool IsTargetProcessDPIAware(IntPtr h)
        {
            try
            {
                int pid;
                Windowing.GetWindowThreadProcessId(new HandleRef(null, h), out pid);
                string appCompatFlags = (string)Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers").GetValue(Process.GetProcessById(pid).MainModule.FileName, string.Empty);

                return appCompatFlags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Contains("HIGHDPIAWARE");
            }
            catch (Exception e)
            {
                //literally do not care
                return false;
            }
        }

        public static double GetDPIScaleFactor()
        {
            double DPI = (int)Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop").GetValue("LogPixels", 96);
            return DPI / 96.0;
        }

        public static Rectangle DPIExpand(this Rectangle original)
        {
            var ScaleFactor = GetDPIScaleFactor();
            var ScaledRect = new Rectangle((int)(original.X * ScaleFactor), (int)(original.Y * ScaleFactor), (int)(original.Width * ScaleFactor), (int)(original.Height * ScaleFactor));

            Trace.WriteLine(string.Format("DPI adjusted '{0}' -> '{1}'", original, ScaledRect), string.Format("Helper.DPIAdjust [{0}]", System.Threading.Thread.CurrentThread.Name));

            return ScaledRect;
        }

        public static Rectangle SafeDPIShrink(this Rectangle original, IntPtr h)
        {
            bool isTargetDPIAware = IsTargetProcessDPIAware(h);
            bool isSelfDPIAware = Windowing.IsProcessDPIAware();

            if (isSelfDPIAware && isTargetDPIAware)
            {
                return original;
            }
            else
            {
                var ScaleFactor = GetDPIScaleFactor();
                var ScaledRect = new Rectangle((int)(original.X / ScaleFactor), (int)(original.Y / ScaleFactor), (int)(original.Width / ScaleFactor), (int)(original.Height / ScaleFactor));

                Trace.WriteLine(string.Format("DPI adjusted '{0}' -> '{1}'", original, ScaledRect), string.Format("Helper.SafeDPIShrink [{0}]", System.Threading.Thread.CurrentThread.Name));

                return ScaledRect;
            }
        }

        public static Rectangle GetWindowRectangleDPI(IntPtr handle)
        {
            var isMaximized = Helper.IsWindowMazimized(handle);
            var CurrentScreen = Screen.FromHandle(handle);

            Rectangle TargetWindowRect = isMaximized ? CurrentScreen.WorkingArea : Helper.GetWindowRectangle(handle);

            //cf. http://stackoverflow.com/questions/8060280/getting-an-dpi-aware-correct-rect-from-getwindowrect-from-a-external-window
            Windowing.RECT DPITargetRect = new Windowing.RECT();
            DWM.DwmGetWindowAttribute(handle, DWM.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out DPITargetRect, (uint)Marshal.SizeOf(DPITargetRect));
            DPITargetRect = DPITargetRect.ToRectangle();

            if (TargetWindowRect == DPITargetRect)
            {
                Trace.WriteLine("Scaling modes are the same, no adjustment necessary.", string.Format("Helper.GetWindowRectangleDPI[{0}]", System.Threading.Thread.CurrentThread.Name));
            }
            else if (TargetWindowRect.Width > DPITargetRect.Width && TargetWindowRect.Height > DPITargetRect.Height)
            {
                Trace.WriteLine("Target larger than DPI assessment", string.Format("Helper.GetWindowRectangleDPI[{0}]", System.Threading.Thread.CurrentThread.Name));
            }
            else if (TargetWindowRect.Width < DPITargetRect.Width && TargetWindowRect.Height < DPITargetRect.Height)
            {
                Trace.WriteLine("Target smaller than DPI assessment", string.Format("Helper.GetWindowRectangleDPI[{0}]", System.Threading.Thread.CurrentThread.Name));
                TargetWindowRect = DPITargetRect;
            }

            return TargetWindowRect;
        }

        public static bool isShiftKeyDown()
        {
            return Convert.ToBoolean(Windowing.GetKeyState(Keys.LShiftKey) & Windowing.KEY_PRESSED) || Convert.ToBoolean(Windowing.GetKeyState(Keys.RShiftKey) & Windowing.KEY_PRESSED);
        }

        public static double GetWindowBorderWidth()
        {
            double BorderWidth = Helper.OperatingSystem == OperatingSystems.Win8 ? 2.0 : 1.0;

            try
            {
                string BorderWidthValue = (string)Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics").GetValue("BorderWidth");
                if (double.TryParse(BorderWidthValue, out BorderWidth))
                {
                    switch (Helper.OperatingSystem)
                    {
                        //Win8:
                        //-20 => 2
                        //-500 => 50
                        case OperatingSystems.Win8:
                            BorderWidth = Math.Abs(BorderWidth) / 10;
                            break;

                        //Win7:
                        //-15 => 1
                        //-750 => 50
                        default:
                            BorderWidth = Math.Abs(BorderWidth) / 15;
                            break;
                    }
                }
            }
            catch
            {
                //don't care
            }

            return BorderWidth;
        }
    }
}