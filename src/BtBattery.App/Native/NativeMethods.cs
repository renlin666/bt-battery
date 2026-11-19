namespace BtBattery.App;

/// <summary>cfgmgr32、bluetoothapis 和托盘窗口用到的 Win32 声明。</summary>
internal static class NativeMethods
{
    public const int CR_SUCCESS = 0;
    public const int CR_BUFFER_SMALL = 26;

    public const uint CM_LOCATE_DEVNODE_NORMAL = 0;
    public const uint CM_GETIDLIST_FILTER_PRESENT = 0x00000100;
    public const uint CM_GETIDLIST_FILTER_CLASS = 0x00000200;

    public const uint DN_STARTED = 0x00000008;
    public const uint DN_HAS_PROBLEM = 0x00000400;

    public const uint DEVPROP_TYPE_BYTE = 0x00000003;
    public const uint DEVPROP_TYPE_UINT32 = 0x00000007;
    public const uint DEVPROP_TYPE_GUID = 0x0000000D;
    public const uint DEVPROP_TYPE_STRING = 0x00000012;
    public const uint DEVPROP_TYPE_STRING_LIST = 0x00002012;

    public const int ERROR_NO_MORE_ITEMS = 259;
    public const int ERROR_INVALID_PARAMETER = 87;

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_ID_List_SizeW(out uint length, string? filter, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_ID_ListW(string? filter, char[] buffer, uint bufferLength, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_DevNode_PropertyW(
        uint devInst,
        ref DEVPROPKEY key,
        out uint propertyType,
        byte[]? buffer,
        ref uint bufferSize,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    public static extern int CM_Get_DevNode_Status(out uint status, out uint problem, uint devInst, uint flags);

    [DllImport("bluetoothapis.dll", SetLastError = true)]
    public static extern IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS search, ref BLUETOOTH_DEVICE_INFO info);

    [DllImport("bluetoothapis.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindNextDevice(IntPtr find, ref BLUETOOTH_DEVICE_INFO info);

    [DllImport("bluetoothapis.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindDeviceClose(IntPtr find);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr handle);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr handle);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_BORDER_COLOR = 34;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    public const int DWMWCP_ROUND = 2;
    public const int DWMSBT_TRANSIENTWINDOW = 3;
    public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_XBUTTONDOWN = 0x020B;

    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
}

[StructLayout(LayoutKind.Sequential)]
internal struct MARGINS
{
    public int cxLeftWidth;
    public int cxRightWidth;
    public int cyTopHeight;
    public int cyBottomHeight;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DEVPROPKEY
{
    public Guid FormatId;
    public uint Pid;

    public DEVPROPKEY(Guid formatId, uint pid)
    {
        FormatId = formatId;
        Pid = pid;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEMTIME
{
    public ushort Year;
    public ushort Month;
    public ushort DayOfWeek;
    public ushort Day;
    public ushort Hour;
    public ushort Minute;
    public ushort Second;
    public ushort Milliseconds;
}

/// <summary>
/// 与 bluetoothapis.h 的默认对齐一致。dwSize 必须等于托管封送后的大小，
/// 否则 API 返回 ERROR_INVALID_PARAMETER，程序会退回另外两层枚举。
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct BLUETOOTH_DEVICE_INFO
{
    public int dwSize;
    public ulong Address;
    public uint ClassOfDevice;
    public int Connected;
    public int Remembered;
    public int Authenticated;
    public SYSTEMTIME LastSeen;
    public SYSTEMTIME LastUsed;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
    public string Name;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BLUETOOTH_DEVICE_SEARCH_PARAMS
{
    public int dwSize;
    public int ReturnAuthenticated;
    public int ReturnRemembered;
    public int ReturnUnknown;
    public int ReturnConnected;
    public int IssueInquiry;
    public byte TimeoutMultiplier;
    public IntPtr Radio;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSLLHOOKSTRUCT
{
    public POINT pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}
