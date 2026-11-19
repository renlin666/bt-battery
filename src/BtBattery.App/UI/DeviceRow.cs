using System.Windows;
using System.Windows.Media;

namespace BtBattery.App;

internal sealed class DeviceRow
{
    public required string Name { get; init; }
    public required string KindLabel { get; init; }
    public required string BatteryText { get; init; }
    public required int Percent { get; init; }
    public required ImageSource Icon { get; init; }
    public required Brush BarBrush { get; init; }
    public GridLength Fill => new(Percent, GridUnitType.Star);
    public GridLength Rest => new(Math.Max(0, 100 - Percent), GridUnitType.Star);
}

public interface IAppController
{
    AppSettings Settings { get; }
    MonitorSnapshot Current { get; }
    bool IsDemo { get; }
    event Action? SnapshotChanged;
    void UpdateSettings(AppSettings settings);
    void Refresh();
    void ShowFlyout();
    void ShowSettings();
    void Exit();
}
