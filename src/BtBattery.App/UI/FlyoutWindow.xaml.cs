using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BtBattery.App;

public partial class FlyoutWindow : Window
{
    private readonly IAppController _app;
    private readonly DateTime _openedUtc = DateTime.UtcNow;
    private LightDismissWatcher? _lightDismiss;

    public FlyoutWindow(IAppController app)
    {
        InitializeComponent();
        _app = app;
        UiTheme.Apply(this, acrylicFlyout: true);
        _app.SnapshotChanged += Refresh;
        Closed += (_, _) =>
        {
            _app.SnapshotChanged -= Refresh;
            _lightDismiss?.Dispose();
            _lightDismiss = null;
        };
        Loaded += (_, _) =>
        {
            Refresh();
            PlaceAboveTaskbar();
            TryFocus();
        };
        SizeChanged += (_, _) => PlaceAboveTaskbar();
        ContentRendered += (_, _) => PlaceAboveTaskbar();
        SourceInitialized += (_, _) =>
        {
            UiTheme.ApplyFlyoutChrome(this);
            TryFocus();
            _lightDismiss = new LightDismissWatcher(this, _openedUtc);
        };
        Deactivated += (_, _) =>
        {
            if (DateTime.UtcNow - _openedUtc > TimeSpan.FromMilliseconds(300))
                Close();
        };
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
                Close();
        };
    }

    private void Refresh()
    {
        var listed = TrayPolicy.SelectForList(_app.Current.Devices, _app.Settings);
        var hiddenFromTray = listed.Count(device => !_app.Settings.ShowWhenBatteryUnknown && !device.Battery.IsKnown);
        SubtitleText.Text = _app.IsDemo
            ? "演示模式，不是这台电脑上的设备"
            : listed.Count == 0
                ? "当前没有要显示的配件"
                : $"共 {listed.Count} 个" + (hiddenFromTray > 0 ? $"，其中 {hiddenFromTray} 个因电量未知未放入托盘" : "");

        EmptyState.Visibility = listed.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Text = _app.Current.Warning is { Length: > 0 } warning
            ? warning + "\n\n如果图标被收进任务栏时钟左边的「^」，打开它并把图标拖出来。"
            : "没有已连接的鼠标、键盘、耳机或手柄。\n\n新图标可能在任务栏的「^」里面。打开它，把图标拖到时钟旁边。";

        var lightGlyph = TaskbarTheme.UseLightGlyphs(_app.Settings.IconTheme);
        DeviceList.ItemsSource = listed.Select(device =>
        {
            var band = BatteryMath.Band(device.Battery.Percent);
            var light = TaskbarTheme.AppsUseLightTheme();
            var bar = band switch
            {
                BatteryBand.Low => light ? "#C42B1C" : "#FF6F6F",
                BatteryBand.Unknown => light ? "#C8C8C8" : "#5E5E5E",
                _ => light ? "#1A1A1A" : "#F5F5F5",
            };
            return new DeviceRow
            {
                Name = device.Name,
                KindLabel = device.KindLabel,
                BatteryText = DeviceText.BatteryLine(device),
                Percent = device.Battery.Percent ?? 0,
                Icon = IconFactory.CreateBitmapSource(device.Kind, device.Battery.IsKnown ? device.Battery.Percent : null, lightGlyph, 32),
                BarBrush = new SolidColorBrush(UiTheme.Parse(bar)),
            };
        }).ToList();
    }

    /// <summary>贴在当前显示器工作区右下角，和快速设置一样留出 12 像素边距，刚好在任务栏上方。</summary>
    private void PlaceAboveTaskbar()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var cursor = System.Windows.Forms.Cursor.Position;
        var area = System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;
        const double margin = 12;

        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice;
        Point workBottomRight;
        Point workTopLeft;
        if (transform is { } matrix)
        {
            workTopLeft = matrix.Transform(new Point(area.Left, area.Top));
            workBottomRight = matrix.Transform(new Point(area.Right, area.Bottom));
        }
        else
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var scaleX = dpi.PixelsPerInchX <= 0 ? 1 : dpi.PixelsPerInchX / 96.0;
            var scaleY = dpi.PixelsPerInchY <= 0 ? 1 : dpi.PixelsPerInchY / 96.0;
            workTopLeft = new Point(area.Left / scaleX, area.Top / scaleY);
            workBottomRight = new Point(area.Right / scaleX, area.Bottom / scaleY);
        }

        var x = workBottomRight.X - ActualWidth - margin;
        var y = workBottomRight.Y - ActualHeight - margin;
        Left = Math.Max(workTopLeft.X + margin, x);
        Top = Math.Max(workTopLeft.Y + margin, y);
    }

    private void TryFocus()
    {
        Activate();
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            NativeMethods.SetForegroundWindow(hwnd);
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => _app.Refresh();

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        _app.ShowSettings();
        Close();
    }
}
