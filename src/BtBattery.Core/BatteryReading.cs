namespace BtBattery.Core;

/// <summary>电量是从哪一层 Windows 接口得到的。</summary>
public enum BatterySource
{
    None = 0,

    /// <summary>
    /// 即插即用属性 DEVPKEY_Bluetooth_BatteryLevel。
    /// Windows 蓝牙协议栈把 GATT、HID 电量报告和免提 HFP 电量都写到这里，设置应用读的也是它。
    /// </summary>
    DeviceProperty,

    /// <summary>Windows.Devices.Power.Battery，有时还能看出是否在充电。</summary>
    WindowsPowerApi,

    /// <summary>BLE GATT 电池服务 0x180F / 电量特征 0x2A19。</summary>
    GattBatteryService,
}

public enum BatteryBand
{
    Unknown = 0,
    Low,
    Mid,
    Healthy,
}

/// <summary>一次电量读数。未知时 <see cref="Percent"/> 为 null，设备仍然保留。</summary>
public sealed record BatteryReading(int? Percent, BatterySource Source, bool? IsCharging)
{
    public static BatteryReading Unknown { get; } = new(null, BatterySource.None, null);

    public bool IsKnown => Percent is >= 0 and <= 100;

    public static BatteryReading Known(int percent, BatterySource source, bool? isCharging = null) =>
        new(percent, source, isCharging);
}

public static class BatteryMath
{
    public const int LowAtOrBelow = 20;
    public const int MidAtOrBelow = 50;

    /// <summary>0–100 为有效百分比。255 等哨兵值视为未知。</summary>
    public static int? NormalizeStoredLevel(int value) =>
        value is >= 0 and <= 100 ? value : null;

    /// <summary>
    /// 把 Windows.Devices.Power 的剩余容量 / 充满容量换成百分比。
    /// 蓝牙设备上这两个字段经常直接就是「百分比 / 100」，不一定是毫瓦时。
    /// </summary>
    public static int? FromCapacities(int? remaining, int? full)
    {
        if (remaining is null or < 0)
            return null;

        if (full is null or 0)
            return NormalizeStoredLevel(remaining.Value);

        if (full == 100 && remaining <= 100)
            return remaining;

        if (remaining > full)
            return NormalizeStoredLevel(remaining.Value);

        var pct = (int)Math.Round(100.0 * remaining.Value / full.Value);
        return NormalizeStoredLevel(pct);
    }

    public static BatteryBand Band(int? percent)
    {
        var value = percent is int raw ? NormalizeStoredLevel(raw) : null;
        return value switch
        {
            null => BatteryBand.Unknown,
            <= LowAtOrBelow => BatteryBand.Low,
            <= MidAtOrBelow => BatteryBand.Mid,
            _ => BatteryBand.Healthy,
        };
    }

    /// <summary>
    /// 多块电池（例如左右耳）时，用最低的一块来画填充，方便先看到该充电的那只。
    /// </summary>
    public static BatteryReading Choose(IEnumerable<BatteryReading> readings)
    {
        BatteryReading? best = null;
        foreach (var reading in readings)
        {
            if (!reading.IsKnown)
                continue;
            if (best is null || reading.Percent < best.Percent)
                best = reading;
        }

        return best ?? BatteryReading.Unknown;
    }
}

/// <summary>电量提示色。低电量用红色，其余跟随界面主题，不再使用高饱和彩色角标。</summary>
public readonly record struct BadgePalette(string BackgroundHex, string ForegroundHex, string OutlineHex)
{
    public static BadgePalette For(BatteryBand band) => band switch
    {
        BatteryBand.Healthy => new("#16820F", "#FFFFFF", "#0E5A0A"),
        BatteryBand.Mid => new("#F0B429", "#1B1B1B", "#7A5B00"),
        BatteryBand.Low => new("#D13438", "#FFFFFF", "#8E1B1F"),
        _ => new("#4A4A4A", "#FFFFFF", "#2A2A2A"),
    };
}
