using IsoCore.App.Services;
using IsoCore.App.Services.Auth;
using IsoCore.App.Services.Users;
using IsoCore.App.State;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;

namespace IsoCore.App;

public partial class App : Application
{
    private Window? _window;
    public static IAppStateService AppState { get; } = CreateAppState();
    public static IRoleService RoleService { get; } = new RoleService();
    public static IUserAuthService UserAuthService { get; } = new JsonUserAuthService();
    public static IUserDirectoryService UserDirectoryService { get; } = new UserDirectoryService();
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (_window == null)
        {
            _window = new Window();
            MainWindow = _window;

            var frame = new Frame();
            frame.Navigate(typeof(Views.MainPage));
            _window.Content = frame;
        }

        _window.Activate();
    }

    private static IAppStateService CreateAppState()
    {
        ProjectStorageManager.Initialize();
        var registry = new ProjectRegistry();
        return new AppStateService(registry);
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            e.Handled = false; // do NOT swallow it yet, just log it

            var ex = e.Exception;
            var message = ex?.ToString() ?? "Unknown exception";

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(localAppData, "IsoCoreBridge", "logs");
            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, "unhandled.log");
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            File.AppendAllText(logFile, logLine);

            Debug.WriteLine("UNHANDLED EXCEPTION: " + message);
        }
        catch
        {
            // Best-effort logging only; never throw from here
        }
    }
}
