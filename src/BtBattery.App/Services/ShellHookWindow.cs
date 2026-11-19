using System.Windows.Forms;

namespace BtBattery.App;

/// <summary>
/// 一个隐藏的消息窗口。设备树变化和 DPI 变化从这里进来，托盘图标自己收不到这些消息。
/// </summary>
internal sealed class ShellHookWindow : NativeWindow, IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVNODES_CHANGED = 0x0007;
    private const int WM_DPICHANGED = 0x02E0;

    public ShellHookWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = AppInfo.Name,
            Parent = new IntPtr(-3),
        });
    }

    public event EventHandler? DevicesChanged;

    public event EventHandler? DpiChanged;

    public int Dpi
    {
        get
        {
            var dpi = NativeMethods.GetDpiForWindow(Handle);
            return dpi == 0 ? 96 : (int)dpi;
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WM_DEVICECHANGE && message.WParam.ToInt32() == DBT_DEVNODES_CHANGED)
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        else if (message.Msg == WM_DPICHANGED)
            DpiChanged?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref message);
    }

    public void Dispose() => DestroyHandle();
}
