namespace BtBattery.App;

internal sealed class ScanLayerResult
{
    public static ScanLayerResult Empty { get; } = new(Array.Empty<DeviceObservation>(), Array.Empty<string>());

    public ScanLayerResult(IReadOnlyList<DeviceObservation> observations, IReadOnlyList<string> warnings)
    {
        Observations = observations;
        Warnings = warnings;
    }

    public IReadOnlyList<DeviceObservation> Observations { get; }

    public IReadOnlyList<string> Warnings { get; }
}

/// <summary>
/// 经典蓝牙 API。只询问“当前已连接”的设备，不会让无线电进入查询扫描。
/// 很多纯 BLE 鼠标不会出现在这里，那些交给 WinRT 和即插即用。
/// </summary>
internal static class ClassicBluetoothScanner
{
    public static ScanLayerResult Read()
    {
        var observations = new List<DeviceObservation>();
        var warnings = new List<string>();
        var info = new BLUETOOTH_DEVICE_INFO { dwSize = Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };
        var search = new BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            dwSize = Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
            ReturnConnected = 1,
        };

        var find = NativeMethods.BluetoothFindFirstDevice(ref search, ref info);
        if (find == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            if (error is not (0 or NativeMethods.ERROR_NO_MORE_ITEMS))
            {
                warnings.Add(error == NativeMethods.ERROR_INVALID_PARAMETER
                    ? "经典蓝牙接口拒绝了查询参数。已改用即插即用枚举。"
                    : $"经典蓝牙接口没有返回设备（Win32 {error}）。");
            }

            return new ScanLayerResult(observations, warnings);
        }

        try
        {
            do
            {
                Add(observations, info);
                info = new BLUETOOTH_DEVICE_INFO { dwSize = Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };
            }
            while (NativeMethods.BluetoothFindNextDevice(find, ref info));
        }
        finally
        {
            NativeMethods.BluetoothFindDeviceClose(find);
        }

        return new ScanLayerResult(observations, warnings);
    }

    private static void Add(List<DeviceObservation> observations, BLUETOOTH_DEVICE_INFO info)
    {
        var address = BluetoothAddress.Format(info.Address);
        if (address is null || info.Connected == 0)
            return;

        var name = string.IsNullOrWhiteSpace(info.Name) ? null : info.Name.Trim();
        observations.Add(new DeviceObservation
        {
            Address = address,
            Source = ObservationSource.ClassicApi,
            Name = name,
            NameRank = 100,
            IsConnected = true,
            Signals = new ClassificationInput
            {
                Name = name,
                ClassOfDevice = info.ClassOfDevice == 0 ? null : info.ClassOfDevice,
            },
            Note = $"经典蓝牙接口：已连接，设备类 0x{info.ClassOfDevice:X}",
        });
    }
}
