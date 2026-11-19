using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace BtBattery.App;

/// <summary>
/// 配对设备的关联终结点。经典接口看不到的 BLE 鼠标、键盘通常在这里，
/// 同时也能拿到 Appearance 和 IsConnected。属性变化用来触发下一次扫描，扫描本身仍以完整枚举为准。
/// </summary>
internal sealed class WinRtBluetoothScanner : IDisposable
{
    private static readonly string[] RequestedProperties =
    {
        "System.Devices.Aep.IsConnected",
        "System.Devices.Aep.DeviceAddress",
        "System.Devices.Aep.Category",
        "System.Devices.Aep.Bluetooth.Le.Appearance",
        "System.Devices.Aep.ContainerId",
        "System.Devices.ContainerId",
        "System.ItemNameDisplay",
        BluetoothProperty.BatteryLevelKey,
        BluetoothProperty.ClassOfDeviceKey,
        BluetoothProperty.DeviceAddressKey,
    };

    private readonly List<DeviceWatcher> _watchers = new();
    private Action? _onChanged;

    public void Start(Action onChanged)
    {
        _onChanged = onChanged;
        TryWatch(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
        TryWatch(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
    }

    public async Task<ScanLayerResult> ReadAsync(CancellationToken cancellationToken)
    {
        var observations = new List<DeviceObservation>();
        var warnings = new List<string>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        try
        {
            await CollectAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true), observations, timeout.Token)
                .ConfigureAwait(false);
            await CollectAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true), observations, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            warnings.Add("读取 WinRT 蓝牙设备超时，这一轮只用经典接口和即插即用。");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            warnings.Add("WinRT 蓝牙枚举被拒绝。若设置里的蓝牙隐私开关关掉了桌面应用，打开后再试。");
            AppLog.Error("WinRT 蓝牙枚举被拒绝。", ex);
        }

        return new ScanLayerResult(observations, warnings);
    }

    public static async Task<string?> RadioWarningAsync(CancellationToken cancellationToken)
    {
        try
        {
            var radios = await Radio.GetRadiosAsync().AsTask(cancellationToken).ConfigureAwait(false);
            foreach (var radio in radios)
            {
                if (radio.Kind == RadioKind.Bluetooth && radio.State == RadioState.Off)
                    return "系统蓝牙已关闭。";
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException or OperationCanceledException)
        {
            AppLog.Error("无法读取蓝牙无线电状态。", ex);
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
                    watcher.Stop();
            }
            catch (Exception ex)
            {
                AppLog.Error("停止设备监视失败。", ex);
            }
        }

        _watchers.Clear();
    }

    private void TryWatch(string selector)
    {
        try
        {
            var watcher = DeviceInformation.CreateWatcher(selector, RequestedProperties, DeviceInformationKind.AssociationEndpoint);
            watcher.Added += (_, _) => _onChanged?.Invoke();
            watcher.Updated += (_, _) => _onChanged?.Invoke();
            watcher.Removed += (_, _) => _onChanged?.Invoke();
            watcher.Start();
            _watchers.Add(watcher);
        }
        catch (Exception ex)
        {
            AppLog.Error("无法监视蓝牙设备变化，将只按间隔刷新。", ex);
        }
    }

    private static async Task CollectAsync(string selector, List<DeviceObservation> sink, CancellationToken cancellationToken)
    {
        var devices = await DeviceInformation.FindAllAsync(selector, RequestedProperties, DeviceInformationKind.AssociationEndpoint)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        foreach (var device in devices)
            sink.Add(ToObservation(device));
    }

    private static DeviceObservation ToObservation(DeviceInformation device)
    {
        var name = FirstString(device, "System.ItemNameDisplay") ?? device.Name;
        var address = BluetoothAddress.Normalize(FirstString(device, "System.Devices.Aep.DeviceAddress"))
            ?? BluetoothAddress.Normalize(FirstString(device, BluetoothProperty.DeviceAddressKey))
            ?? BluetoothAddress.ExtractFromInstanceId(device.Id);
        var percent = PropertyValues.AsPercent(FirstValue(device, BluetoothProperty.BatteryLevelKey));
        var battery = percent is int level ? BatteryReading.Known(level, BatterySource.DeviceProperty) : null;

        return new DeviceObservation
        {
            Address = address ?? "",
            Source = ObservationSource.WinRt,
            Name = name,
            NameRank = 80,
            IsConnected = PropertyValues.AsBool(FirstValue(device, "System.Devices.Aep.IsConnected")),
            Signals = new ClassificationInput
            {
                Name = name,
                Appearance = PropertyValues.AsUInt16(FirstValue(device, "System.Devices.Aep.Bluetooth.Le.Appearance")),
                ClassOfDevice = PropertyValues.AsUInt32(FirstValue(device, BluetoothProperty.ClassOfDeviceKey)),
                Categories = PropertyValues.AsStrings(FirstValue(device, "System.Devices.Aep.Category")),
            },
            Battery = battery,
            ContainerId = PropertyValues.AsGuid(FirstValue(device, "System.Devices.Aep.ContainerId"))
                ?? PropertyValues.AsGuid(FirstValue(device, "System.Devices.ContainerId")),
            Note = battery is null ? null : $"WinRT 设备属性电量：{battery.Percent}%",
        };
    }

    private static object? FirstValue(DeviceInformation device, string key)
    {
        if (device.Properties.TryGetValue(key, out var value))
            return value;

        foreach (var pair in device.Properties)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    private static string? FirstString(DeviceInformation device, string key)
    {
        var value = FirstValue(device, key);
        return value as string;
    }
}
