namespace BtBattery.App;

internal static class AppPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppInfo.Name);

    public static string SettingsPath => Path.Combine(Root, "settings.json");

    public static string LogPath => Path.Combine(Root, "log.txt");
}

internal static class AppLog
{
    private static readonly object Gate = new();

    public static void Info(string message) => Write("信息", message);

    public static void Error(string message, Exception? exception = null)
    {
        var detail = exception is null
            ? message
            : message + " " + exception.GetType().Name + ": " + exception.Message;
        Write("错误", detail);
    }

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} {level} {message}{Environment.NewLine}";
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.Root);
                File.AppendAllText(AppPaths.LogPath, line);
                var info = new FileInfo(AppPaths.LogPath);
                if (info.Length <= 256 * 1024)
                    return;

                var text = File.ReadAllText(AppPaths.LogPath);
                var keep = text.Length > 64 * 1024 ? text[^ (64 * 1024)..] : text;
                File.WriteAllText(AppPaths.LogPath, keep);
            }
        }
        catch (IOException)
        {
            // 写不了日志时托盘仍要继续工作。
        }
    }
}
