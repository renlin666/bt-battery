using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace BtBattery.App;

/// <summary>
/// 面板打开后监听全局鼠标按下。点在窗口外面就关闭，和快速设置的轻关闭一样。
/// 托盘图标点击往往拿不到前台焦点，只靠 Deactivated 关不掉。
/// </summary>
internal sealed class LightDismissWatcher : IDisposable
{
    private readonly Window _window;
    private readonly DateTime _openedUtc;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private IntPtr _hook;
    private bool _disposed;

    public LightDismissWatcher(Window window, DateTime openedUtc)
    {
        _window = window;
        _openedUtc = openedUtc;
        _proc = OnMouse;
        var module = NativeMethods.GetModuleHandle(null);
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, module, 0);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    private IntPtr OnMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsButtonDown(wParam) && DateTime.UtcNow - _openedUtc > TimeSpan.FromMilliseconds(250))
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if (!Contains(data.pt.X, data.pt.Y))
                Dismiss();
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static bool IsButtonDown(IntPtr wParam)
    {
        var message = wParam.ToInt32();
        return message is NativeMethods.WM_LBUTTONDOWN
            or NativeMethods.WM_RBUTTONDOWN
            or NativeMethods.WM_MBUTTONDOWN
            or NativeMethods.WM_XBUTTONDOWN;
    }

    private bool Contains(int x, int y)
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero)
            return true;
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
            return true;
        return rect.Contains(x, y);
    }

    private void Dismiss()
    {
        if (_disposed)
            return;

        var dispatcher = _window.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            if (_window.IsVisible)
                _window.Close();
            return;
        }

        dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (!_disposed && _window.IsVisible)
                _window.Close();
        }));
    }
}
