namespace BtBattery.Core;

/// <summary>程序对外显示的名称和版本。界面与诊断都从这里取。</summary>
public static class AppInfo
{
    public const string Name = "蓝牙电量";
    public const string Version = "1.0.0";
    public const string MutexName = @"Local\BtBattery.Tray.SingleInstance";
    public const string ShowEventName = @"Local\BtBattery.Tray.Show";
}
