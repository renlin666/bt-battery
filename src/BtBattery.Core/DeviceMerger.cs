namespace BtBattery.Core;

/// <summary>
/// 把经典蓝牙、WinRT、即插即用和电量接口看到的碎片合成「一台设备」。
/// 连接状态的优先级是：经典 API 说已连接最可信；WinRT 明确说未连接时，忽略可能残留的即插即用节点。
/// </summary>
public static class DeviceMerger
{
    public static IReadOnlyList<TrackedDevice> Merge(
        IEnumerable<DeviceObservation> observations,
        bool connectedOnly = true)
    {
        var list = observations.ToList();
        var groups = list
            .Where(item => !string.IsNullOrEmpty(item.Address))
            .GroupBy(item => item.Address, StringComparer.OrdinalIgnoreCase);

        var devices = new List<TrackedDevice>();
        foreach (var group in groups)
        {
            var rows = group.ToList();
            AttachContainerBatteries(rows, list);
            var device = Build(group.Key, rows);
            if (!connectedOnly || device.IsConnected)
                devices.Add(device);
        }

        return devices
            .OrderBy(item => KindOrder(item.Kind))
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void AttachContainerBatteries(List<DeviceObservation> rows, List<DeviceObservation> all)
    {
        var container = rows.Select(item => item.ContainerId).FirstOrDefault(item => item.HasValue);
        if (container is null)
            return;

        foreach (var extra in all)
        {
            if (!string.IsNullOrEmpty(extra.Address))
                continue;
            if (extra.ContainerId != container)
                continue;
            if (extra.Battery is not { IsKnown: true })
                continue;
            rows.Add(extra);
        }
    }

    private static TrackedDevice Build(string address, List<DeviceObservation> rows)
    {
        var classified = rows
            .Select(row => DeviceClassifier.Classify(row.Signals))
            .Where(item => item.Confidence > 0)
            .ToList();
        var classification = classified.Count == 0
            ? ClassificationResult.Unknown
            : DeviceClassifier.Pick(classified);

        var readings = rows
            .Select(row => row.Battery)
            .OfType<BatteryReading>()
            .ToList();
        var battery = BatteryMath.Choose(readings);
        var percents = readings
            .Where(item => item.IsKnown)
            .Select(item => item.Percent!.Value)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        var notes = rows
            .Select(row => row.Note)
            .Where(note => !string.IsNullOrWhiteSpace(note))
            .Select(note => note!.Trim())
            .Distinct()
            .ToList();

        var instances = rows
            .Select(row => row.InstanceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TrackedDevice
        {
            Address = address,
            Name = DeviceNames.Choose(classification.Kind, rows.Select(row => (row.Name, row.NameRank))),
            Kind = classification.Kind,
            KindLabel = string.IsNullOrEmpty(classification.Label)
                ? DeviceKindLabels.For(classification.Kind)
                : classification.Label,
            ClassificationSource = classification.Source,
            IsConnected = ResolveConnected(rows),
            Battery = battery,
            KnownPercents = percents,
            Notes = notes,
            InstanceIds = instances,
            ContainerId = rows.Select(row => row.ContainerId).FirstOrDefault(item => item.HasValue),
        };
    }

    /// <summary>
    /// 经典接口说已连接就显示。WinRT 明确说未连接、且经典接口没有肯定答复时，
    /// 不采用可能已经过期的即插即用“已启动”状态。两边都没有记录时，才退回即插即用。
    /// </summary>
    public static bool ResolveConnected(IReadOnlyList<DeviceObservation> rows)
    {
        if (rows.Any(row => row.Source == ObservationSource.ClassicApi && row.IsConnected == true))
            return true;

        if (rows.Any(row => row.Source == ObservationSource.WinRt && row.IsConnected == true))
            return true;

        if (rows.Any(row => row.Source == ObservationSource.WinRt && row.IsConnected == false))
            return false;

        if (rows.Any(row => row.Source == ObservationSource.ClassicApi && row.IsConnected == false))
            return false;

        if (rows.Any(row => row.Source == ObservationSource.Pnp && row.IsConnected == true))
            return true;

        if (rows.Any(row => row.Source == ObservationSource.Demo && row.IsConnected == true))
            return true;

        return false;
    }

    private static int KindOrder(DeviceKind kind) => kind switch
    {
        DeviceKind.Mouse => 0,
        DeviceKind.Keyboard => 1,
        DeviceKind.Combo => 2,
        DeviceKind.Headset => 3,
        DeviceKind.Gamepad => 4,
        DeviceKind.Unknown => 5,
        _ => 6,
    };
}
