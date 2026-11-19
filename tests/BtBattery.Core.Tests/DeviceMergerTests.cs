using BtBattery.Core;

namespace BtBattery.Core.Tests;

public class DeviceMergerTests
{
    [Fact]
    public void Same_address_keeps_the_classic_name_and_the_pnp_battery()
    {
        var devices = DeviceMerger.Merge(new[]
        {
            Observed("AA:BB:CC:DD:EE:01", ObservationSource.ClassicApi, "WH-1000XM5", 100, true, cod: 0x240404),
            Observed(
                "AA:BB:CC:DD:EE:01",
                ObservationSource.Pnp,
                "Headset",
                40,
                true,
                battery: 72,
                instanceId: @"BTHHFENUM\BthHFAudio\1"),
        });

        var device = Assert.Single(devices);
        Assert.Equal("WH-1000XM5", device.Name);
        Assert.Equal(DeviceKind.Headset, device.Kind);
        Assert.Equal(72, device.Battery.Percent);
        Assert.Equal(BatterySource.DeviceProperty, device.Battery.Source);
        Assert.Contains(@"BTHHFENUM\BthHFAudio\1", device.InstanceIds);
    }

    [Fact]
    public void Classic_connected_wins_over_a_stale_winrt_false()
    {
        var rows = new[]
        {
            Observed("AA:BB:CC:DD:EE:01", ObservationSource.ClassicApi, "鼠标", 100, true, cod: 0x580),
            Observed("AA:BB:CC:DD:EE:01", ObservationSource.WinRt, "鼠标", 80, false),
        };

        Assert.True(DeviceMerger.ResolveConnected(rows));
        Assert.Single(DeviceMerger.Merge(rows));
    }

    [Fact]
    public void Winrt_false_hides_a_phantom_pnp_node()
    {
        var rows = new[]
        {
            Observed("AA:BB:CC:DD:EE:01", ObservationSource.WinRt, "MX Master 3S", 80, false),
            Observed("AA:BB:CC:DD:EE:01", ObservationSource.Pnp, "MX Master 3S", 50, true, cod: 0x580),
        };

        Assert.False(DeviceMerger.ResolveConnected(rows));
        Assert.Empty(DeviceMerger.Merge(rows));
    }

    [Fact]
    public void Pnp_started_is_used_when_no_other_source_knows_the_device()
    {
        var devices = DeviceMerger.Merge(new[]
        {
            Observed("AA:BB:CC:DD:EE:02", ObservationSource.Pnp, "Keychron K3", 50, true, cod: 0x540, battery: 40),
        });

        var device = Assert.Single(devices);
        Assert.Equal(DeviceKind.Keyboard, device.Kind);
        Assert.Equal(40, device.Battery.Percent);
    }

    [Fact]
    public void Container_battery_is_attached_when_the_node_has_no_address()
    {
        var container = Guid.NewGuid();
        var devices = DeviceMerger.Merge(new[]
        {
            Observed("AA:BB:CC:DD:EE:03", ObservationSource.ClassicApi, "FreeBuds", 100, true, cod: 0x404, container: container),
            new DeviceObservation
            {
                Address = "",
                Source = ObservationSource.WindowsPowerApi,
                ContainerId = container,
                Battery = BatteryReading.Known(61, BatterySource.WindowsPowerApi, true),
                Note = "Windows 电池接口：61%，充电中",
            },
        });

        var device = Assert.Single(devices);
        Assert.Equal(61, device.Battery.Percent);
        Assert.True(device.Battery.IsCharging);
        Assert.Contains("Windows 电池接口：61%，充电中", device.Notes);
    }

    [Fact]
    public void Lowest_of_several_percents_is_kept_and_recorded()
    {
        var devices = DeviceMerger.Merge(new[]
        {
            Observed("AA:BB:CC:DD:EE:04", ObservationSource.ClassicApi, "AirPods Pro", 100, true, appearance: 0x0941, battery: 80),
            Observed("AA:BB:CC:DD:EE:04", ObservationSource.Pnp, "AirPods Pro", 40, true, battery: 55),
        });

        var device = Assert.Single(devices);
        Assert.Equal(55, device.Battery.Percent);
        Assert.Equal(new[] { 55, 80 }, device.KnownPercents);
        Assert.Contains("最低", DeviceText.BatteryLine(device));
    }

    private static DeviceObservation Observed(
        string address,
        ObservationSource source,
        string name,
        int nameRank,
        bool connected,
        uint? cod = null,
        ushort? appearance = null,
        int? battery = null,
        string? instanceId = null,
        Guid? container = null)
    {
        return new DeviceObservation
        {
            Address = address,
            Source = source,
            Name = name,
            NameRank = nameRank,
            IsConnected = connected,
            Signals = new ClassificationInput
            {
                Name = name,
                ClassOfDevice = cod,
                Appearance = appearance,
            },
            Battery = battery is int percent ? BatteryReading.Known(percent, BatterySource.DeviceProperty) : null,
            InstanceId = instanceId,
            ContainerId = container,
            Note = battery is int value ? $"设备属性电量：{value}%" : null,
        };
    }
}
