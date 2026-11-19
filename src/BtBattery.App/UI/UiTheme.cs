using System.Windows;
using System.Windows.Media;

namespace BtBattery.App;

internal static class UiTheme
{
    public static void Apply(Window window, bool acrylicFlyout = false)
    {
        var light = TaskbarTheme.AppsUseLightTheme();
        if (acrylicFlyout)
        {
            // 窗口先铺一层半透明浅色（或深色）衬底，亚克力在底下磨砂。
            // 不能把背景做成完全透明，否则衬底没生效时会露出黑底。
            Set(window, "WindowBrush", light ? "#E6F3F3F3" : "#E6202020");
            Set(window, "CardBrush", light ? "#F2FFFFFF" : "#E62C2C2C");
            Set(window, "TextBrush", light ? "#1A1A1A" : "#F5F5F5");
            Set(window, "MutedBrush", light ? "#5E5E5E" : "#C8C8C8");
            Set(window, "StrokeBrush", light ? "#26000000" : "#28FFFFFF");
            Set(window, "AccentBrush", light ? "#0067C0" : "#60CDFF");
            Set(window, "ButtonBrush", light ? "#14000000" : "#18FFFFFF");
            Set(window, "ButtonHoverBrush", light ? "#22000000" : "#28FFFFFF");
            return;
        }

        Set(window, "WindowBrush", light ? "#F3F3F3" : "#202020");
        Set(window, "CardBrush", light ? "#FFFFFF" : "#2C2C2C");
        Set(window, "TextBrush", light ? "#1A1A1A" : "#F5F5F5");
        Set(window, "MutedBrush", light ? "#5E5E5E" : "#C8C8C8");
        Set(window, "StrokeBrush", light ? "#E5E5E5" : "#3D3D3D");
        Set(window, "AccentBrush", light ? "#0067C0" : "#60CDFF");
    }

    public static Color Parse(string hex)
    {
        hex = hex.TrimStart('#');
        var value = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (hex.Length <= 6)
            return Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);

        return Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    public static void Round(Window window)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var corner = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
    }

    /// <summary>按 Windows 11 快速设置那样：圆角、亚克力背景、贴在工作区右下角。</summary>
    public static void ApplyFlyoutChrome(Window window)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dark = TaskbarTheme.AppsUseLightTheme() ? 0 : 1;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

        var corner = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        var backdrop = NativeMethods.DWMSBT_TRANSIENTWINDOW;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

        var border = NativeMethods.DWMWA_COLOR_NONE;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_BORDER_COLOR, ref border, sizeof(int));

        var margins = new MARGINS
        {
            cxLeftWidth = -1,
            cxRightWidth = -1,
            cyTopHeight = -1,
            cyBottomHeight = -1,
        };
        NativeMethods.DwmExtendFrameIntoClientArea(handle, ref margins);
    }

    private static void Set(Window window, string key, string hex)
    {
        if (window.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            brush.Color = Parse(hex);
    }
}
