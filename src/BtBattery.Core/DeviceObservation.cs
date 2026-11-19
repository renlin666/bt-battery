namespace BtBattery.Core;

public enum ObservationSource
{
    ClassicApi = 0,
    WinRt = 1,
    Pnp = 2,
    WindowsPowerApi = 3,
    Gatt = 4,
    Demo = 5,
}

/// <summary>
/// 某一层 API 看到的一台设备或一个附属节点。
/// 同一蓝牙地址的多条观察会在 <see cref="DeviceMerger"/> 里合成一个托盘项。
/// </summary>
public sealed class DeviceObservation
{
    public string Address { get; init; } = "";

    public ObservationSource Source { get; init; }

    public string? Name { get; init; }

    /// <summary>越大越像用户在系统里看到的产品名。经典蓝牙 API 的名字优先于服务节点。</summary>
    public int NameRank { get; init; }

    /// <summary>null 表示这一层不知道连接状态。</summary>
    public bool? IsConnected { get; init; }

    public ClassificationInput Signals { get; init; }

    public BatteryReading? Battery { get; init; }

    public string? InstanceId { get; init; }

    public Guid? ContainerId { get; init; }

    /// <summary>写进诊断报告的一行说明。</summary>
    public string? Note { get; init; }
}

public sealed class TrackedDevice
{
    public required string Address { get; init; }
    public required string Name { get; init; }
    public required DeviceKind Kind { get; init; }
    public required string KindLabel { get; init; }
    public required string ClassificationSource { get; init; }
    public required bool IsConnected { get; init; }
    public required BatteryReading Battery { get; init; }
    public IReadOnlyList<int> KnownPercents { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InstanceIds { get; init; } = Array.Empty<string>();
    public Guid? ContainerId { get; init; }

    public bool HasMultipleBatteries => KnownPercents.Count > 1;
}

public sealed class MonitorSnapshot
{
    public static MonitorSnapshot Empty { get; } = new(Array.Empty<TrackedDevice>(), null);

    public MonitorSnapshot(IReadOnlyList<TrackedDevice> devices, string? warning)
    {
        Devices = devices;
        Warning = warning;
    }

    public IReadOnlyList<TrackedDevice> Devices { get; }

    public string? Warning { get; }
}
