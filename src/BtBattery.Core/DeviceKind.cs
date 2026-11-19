namespace BtBattery.Core;

/// <summary>
/// 托盘要区分的配件类型。手机、电脑、音箱等归入 <see cref="Excluded"/>，默认不显示。
/// </summary>
public enum DeviceKind
{
    Unknown = 0,
    Mouse,
    Keyboard,
    Combo,
    Headset,
    Gamepad,
    Excluded,
}

public static class DeviceKindLabels
{
    public static string For(DeviceKind kind) => kind switch
    {
        DeviceKind.Mouse => "鼠标",
        DeviceKind.Keyboard => "键盘",
        DeviceKind.Combo => "键鼠",
        DeviceKind.Headset => "耳机",
        DeviceKind.Gamepad => "手柄",
        DeviceKind.Excluded => "已忽略",
        _ => "未知",
    };

    /// <summary>没有可用产品名时的托盘名称。</summary>
    public static string FallbackName(DeviceKind kind) => kind switch
    {
        DeviceKind.Mouse => "蓝牙鼠标",
        DeviceKind.Keyboard => "蓝牙键盘",
        DeviceKind.Combo => "蓝牙键鼠",
        DeviceKind.Headset => "蓝牙耳机",
        DeviceKind.Gamepad => "蓝牙手柄",
        _ => "蓝牙设备",
    };

    public static bool IsAccessory(DeviceKind kind) => kind is
        DeviceKind.Mouse or DeviceKind.Keyboard or DeviceKind.Combo or DeviceKind.Headset or DeviceKind.Gamepad;
}
