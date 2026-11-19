namespace BtBattery.Core;

/// <summary>
/// Windows 蓝牙协议栈使用的设备属性。GUID 与 PID 来自公开的 bthguid 定义
/// （Wine 头文件、fastfetch、以及 PowerShell 的 Get-PnpDeviceProperty 都使用同一组键）。
/// </summary>
public static class BluetoothProperty
{
    /// <summary>DEVPKEY_Bluetooth_BatteryLevel，类型 BYTE，0–100。</summary>
    public static readonly Guid BatteryLevelFormat = new("104EA319-6EE2-4701-BD47-8DDBF425BBE5");

    public const uint BatteryLevelPid = 2;

    public const string BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    /// <summary>DEVPKEY_Bluetooth_* 的格式 ID。PID 1 是地址字符串，PID 10 是 Class of Device。</summary>
    public static readonly Guid BluetoothInfoFormat = new("2BD67D8B-8BEB-48D5-87E0-6CDA3428040A");

    public const uint DeviceAddressPid = 1;
    public const uint ClassOfDevicePid = 10;

    public const string DeviceAddressKey = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 1";
    public const string ClassOfDeviceKey = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 10";

    public static readonly Guid DeviceContainerFormat = new("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C");
    public const uint ContainerIdPid = 2;

    public static readonly Guid DevicePropertyFormat = new("A45C254E-DF1C-4EFD-8020-67D146A850E0");
    public const uint DeviceDescPid = 2;
    public const uint CompatibleIdsPid = 4;
    public const uint DeviceClassPid = 9;
    public const uint FriendlyNamePid = 14;
}

/// <summary>把 WinRT / 注册表里装箱后的属性值还原成托管类型。</summary>
public static class PropertyValues
{
    public static int? AsPercent(object? value) => value switch
    {
        byte number => BatteryMath.NormalizeStoredLevel(number),
        sbyte number => BatteryMath.NormalizeStoredLevel(number),
        short number => BatteryMath.NormalizeStoredLevel(number),
        ushort number => BatteryMath.NormalizeStoredLevel(number),
        int number => BatteryMath.NormalizeStoredLevel(number),
        uint number when number <= int.MaxValue => BatteryMath.NormalizeStoredLevel((int)number),
        long number when number is >= 0 and <= int.MaxValue => BatteryMath.NormalizeStoredLevel((int)number),
        byte[] { Length: 1 } bytes => BatteryMath.NormalizeStoredLevel(bytes[0]),
        string text when int.TryParse(text, out var number) => BatteryMath.NormalizeStoredLevel(number),
        _ => null,
    };

    public static bool? AsBool(object? value) => value switch
    {
        bool flag => flag,
        int number => number != 0,
        uint number => number != 0,
        short number => number != 0,
        byte number => number != 0,
        string text when bool.TryParse(text, out var flag) => flag,
        _ => null,
    };

    public static uint? AsUInt32(object? value) => value switch
    {
        uint number => number,
        int number when number >= 0 => (uint)number,
        ushort number => number,
        short number when number >= 0 => (uint)number,
        byte number => number,
        long number when number is >= 0 and <= uint.MaxValue => (uint)number,
        _ => null,
    };

    public static ushort? AsUInt16(object? value)
    {
        var number = AsUInt32(value);
        return number is <= ushort.MaxValue ? (ushort)number.Value : null;
    }

    public static Guid? AsGuid(object? value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var guid) => guid,
        _ => null,
    };

    public static IReadOnlyList<string> AsStrings(object? value) => value switch
    {
        string text => new[] { text },
        string[] items => items,
        IEnumerable<string> items => items.ToArray(),
        _ => Array.Empty<string>(),
    };
}
