namespace BtBattery.App;

internal static class DiagnosticReport
{
    public static string Build(MonitorSnapshot snapshot, AppSettings settings, bool demo)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{AppInfo.Name} 诊断");
        builder.AppendLine($"版本：{AppInfo.Version}");
        builder.AppendLine($"时间：{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"系统：{Environment.OSVersion.VersionString}");
        builder.AppendLine($"刷新间隔：{settings.PollIntervalSeconds} 秒");
        builder.AppendLine($"电量未知时仍显示托盘图标：{(settings.ShowWhenBatteryUnknown ? "是" : "否")}");
        builder.AppendLine($"日志：{AppPaths.LogPath}");
        if (demo)
            builder.AppendLine("模式：演示。下面不是这台电脑上的设备。");
        if (!string.IsNullOrWhiteSpace(snapshot.Warning))
            builder.AppendLine("提示：" + snapshot.Warning);
        builder.AppendLine();

        if (snapshot.Devices.Count == 0)
        {
            builder.AppendLine("没有发现已连接的蓝牙设备。");
        }

        foreach (var device in snapshot.Devices)
        {
            builder.AppendLine("- " + device.Name);
            builder.AppendLine("  地址：" + device.Address);
            builder.AppendLine($"  类型：{device.KindLabel}（{device.ClassificationSource}）");
            builder.AppendLine("  连接：" + (device.IsConnected ? "已连接" : "未连接"));
            builder.AppendLine(device.Battery.IsKnown
                ? $"  电量：{device.Battery.Percent}%（{DeviceText.SourceLabel(device.Battery.Source)}）"
                : "  电量：未知");
            if (device.HasMultipleBatteries)
                builder.AppendLine("  多块电池：" + string.Join(" / ", device.KnownPercents) + "%，角标用最低值");
            foreach (var note in device.Notes)
                builder.AppendLine("  · " + note);
            foreach (var instanceId in device.InstanceIds)
                builder.AppendLine("  实例：" + instanceId);
            if (!settings.Allows(device.Kind))
                builder.AppendLine("  托盘：已按设置隐藏（类型不在显示范围内）。");
            else if (!device.Battery.IsKnown && !settings.ShowWhenBatteryUnknown)
                builder.AppendLine("  托盘：已按设置隐藏（电量未知）。设备列表里仍然显示。");
            builder.AppendLine();
        }

        builder.AppendLine("电量属性键：{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2");
        builder.AppendLine("这是 Windows 蓝牙协议栈的 DEVPKEY_Bluetooth_BatteryLevel，设置里的电量也来自这里。");
        return builder.ToString();
    }
}

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showEvent;
    private readonly CancellationTokenSource _cts = new();
    private readonly SynchronizationContext _ui;

    public SingleInstance(SynchronizationContext ui)
    {
        _ui = ui;
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, AppInfo.ShowEventName);
        _mutex = new Mutex(false, AppInfo.MutexName);
        try
        {
            IsFirst = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            IsFirst = true;
        }
    }

    public bool IsFirst { get; }

    public void Signal() => _showEvent.Set();

    public void Listen(Action show)
    {
        Task.Run(() =>
        {
            while (!_cts.IsCancellationRequested)
            {
                bool signaled;
                try
                {
                    signaled = _showEvent.WaitOne(TimeSpan.FromSeconds(1));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (signaled)
                    _ui.Post(_ => show(), null);
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (IsFirst)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // 没有持有互斥量时释放会失败，退出不必再报。
            }
        }

        _mutex.Dispose();
        _showEvent.Dispose();
        _cts.Dispose();
    }
}
