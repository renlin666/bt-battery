using System.Windows;
using System.Windows.Controls;

namespace BtBattery.App;

public partial class SettingsWindow : Window
{
    private readonly IAppController _app;
    private bool _loading = true;

    public SettingsWindow(IAppController app)
    {
        InitializeComponent();
        _app = app;
        UiTheme.Apply(this);
        SourceInitialized += (_, _) => UiTheme.Round(this);
        LoadFrom(app.Settings);
        _loading = false;
        StatusText.Text = "版本 " + AppInfo.Version;
    }

    private void LoadFrom(AppSettings settings)
    {
        StartWithWindowsBox.IsChecked = settings.StartWithWindows;
        ShowMouseBox.IsChecked = settings.ShowMouse;
        ShowKeyboardBox.IsChecked = settings.ShowKeyboard;
        ShowHeadsetBox.IsChecked = settings.ShowHeadset;
        ShowGamepadBox.IsChecked = settings.ShowGamepad;
        UnknownShowRadio.IsChecked = settings.ShowWhenBatteryUnknown;
        UnknownHideRadio.IsChecked = !settings.ShowWhenBatteryUnknown;
        ThemeFollowRadio.IsChecked = settings.IconTheme == IconThemePreference.FollowSystem;
        ThemeLightRadio.IsChecked = settings.IconTheme == IconThemePreference.LightGlyphs;
        ThemeDarkRadio.IsChecked = settings.IconTheme == IconThemePreference.DarkGlyphs;

        var choices = new List<int> { 15, 30, 60, 120 };
        if (!choices.Contains(settings.PollIntervalSeconds))
            choices.Add(settings.PollIntervalSeconds);
        var items = choices.Select(seconds => new IntervalChoice(seconds)).ToList();
        IntervalBox.ItemsSource = items;
        IntervalBox.SelectedItem = items.First(item => item.Seconds == settings.PollIntervalSeconds);
    }

    private void OnIntervalChanged(object sender, SelectionChangedEventArgs e) => OnChanged(sender, e);

    private void OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading || !IsLoaded)
            return;

        try
        {
            _app.UpdateSettings(Collect());
            StatusText.Text = "已保存";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private AppSettings Collect()
    {
        var seconds = IntervalBox.SelectedItem is IntervalChoice choice
            ? choice.Seconds
            : _app.Settings.PollIntervalSeconds;
        return new AppSettings
        {
            StartWithWindows = StartWithWindowsBox.IsChecked == true,
            ShowMouse = ShowMouseBox.IsChecked == true,
            ShowKeyboard = ShowKeyboardBox.IsChecked == true,
            ShowHeadset = ShowHeadsetBox.IsChecked == true,
            ShowGamepad = ShowGamepadBox.IsChecked == true,
            ShowWhenBatteryUnknown = UnknownHideRadio.IsChecked != true,
            PollIntervalSeconds = seconds,
            IconTheme = ThemeLightRadio.IsChecked == true
                ? IconThemePreference.LightGlyphs
                : ThemeDarkRadio.IsChecked == true
                    ? IconThemePreference.DarkGlyphs
                    : IconThemePreference.FollowSystem,
        };
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _app.Refresh();
        StatusText.Text = _app.IsDemo ? "演示模式不会读取本机蓝牙" : "正在刷新";
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(DiagnosticReport.Build(_app.Current, _app.Settings, _app.IsDemo));
            StatusText.Text = "诊断信息已复制到剪贴板";
        }
        catch (Exception ex)
        {
            StatusText.Text = "剪贴板正忙：" + ex.Message;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private sealed class IntervalChoice
    {
        public IntervalChoice(int seconds) => Seconds = seconds;

        public int Seconds { get; }

        public override string ToString() => Seconds + " 秒";

        public override bool Equals(object? obj) => obj is IntervalChoice other && other.Seconds == Seconds;

        public override int GetHashCode() => Seconds;
    }
}
