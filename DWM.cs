using System;
using System.Runtime.InteropServices;
using FMUtils.WinApi;

namespace FMUtils
{
    public static class DWM
    {
        [DllImport("dwmapi.dll")]
        public static extern void DwmIsCompositionEnabled(ref bool pfEnabled);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);

        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
        public static extern void DwmGetColorizationParameters(out DWM_COLORIZATION_PARAMS parameters);

        [DllImport("dwmapi.dll", EntryPoint = "#131", PreserveSig = false)]
        public static extern void DwmSetColorizationParameters(ref DWM_COLORIZATION_PARAMS parameters, uint u);

        [DllImport("dwmapi.dll")]
        public static extern void DwmGetColorizationColor(out uint ColorizationColor, out bool ColorizationOpaqueBlend);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out Windowing.RECT pvAttribute, uint cbAttribute);

        public struct DWM_COLORIZATION_PARAMS
        {
            public UInt32 Color;
            public UInt32 Afterglow;
            public UInt32 ColorBalance;
            public UInt32 AfterglowBalance;
            public UInt32 BlurBalance;
            public UInt32 GlassReflectionIntensity;
            public UInt32 OpaqueBlend;

            public DWM_COLORIZATION_PARAMS(UInt32 color, UInt32 afterglow, UInt32 colorbalance, UInt32 afterglowbalance, UInt32 blurbalance, UInt32 glassreflectionintensity, UInt32 opaqueblend)
            {
                Color = color;
                Afterglow = afterglow;
                ColorBalance = colorbalance;
                AfterglowBalance = afterglow;
                BlurBalance = blurbalance;
                GlassReflectionIntensity = glassreflectionintensity;
                OpaqueBlend = opaqueblend;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSIZE
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Windowing.RECT rcDestination;
            public Windowing.RECT rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        /*
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle toRectangle()
            {
                return new Rectangle(Left, Top, Right - Left, Bottom - Top);
            }
        }
        */

        public static int DWM_TNP_VISIBLE = 0x8;
        public static int DWM_TNP_OPACITY = 0x4;
        public static int DWM_TNP_RECTDESTINATION = 0x1;

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa969530(v=vs.85).aspx
        public enum DWMWINDOWATTRIBUTE
        {
            /// <summary>
            /// [get] Is non-client rendering enabled/disabled
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,

            /// <summary>
            /// [set] Non-client rendering policy
            /// </summary>
            DWMWA_NCRENDERING_POLICY,

            /// <summary>
            /// [set] Potentially enable/forcibly disable transitions
            /// </summary>
            DWMWA_TRANSITIONS_FORCEDISABLED,

            /// <summary>
            /// [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
            /// </summary>
            DWMWA_ALLOW_NCPAINT,

            /// <summary>
            /// [get] Bounds of the caption button area in window-relative space.
            /// </summary>
            DWMWA_CAPTION_BUTTON_BOUNDS,

            /// <summary>
            /// [set] Is non-client content RTL mirrored 
            /// </summary>
            DWMWA_NONCLIENT_RTL_LAYOUT,

            /// <summary>
            /// [set] Force this window to display iconic thumbnails.
            /// </summary>
            DWMWA_FORCE_ICONIC_REPRESENTATION,

            /// <summary>
            /// [set] Designates how Flip3D will treat the window.
            /// </summary>
            DWMWA_FLIP3D_POLICY,

            /// <summary>
            /// [get] Gets the extended frame bounds rectangle in screen space.
            /// Additional arguements are RECT and sizeof(RECT)
            /// </summary>
            DWMWA_EXTENDED_FRAME_BOUNDS,

            DWMWA_LAST
        };
    }
}
