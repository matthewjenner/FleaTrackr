using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using FleaTrackr.App.Services;
using FleaTrackr.App.ViewModels;
using FleaTrackr.App.Views;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App;

public partial class App : Application
{
    private AppHost? _host;
    private Window? _mainWindow;
    private TrayIcon? _tray;
    private WindowNotificationManager? _notifications;
    private bool _quitting;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _host = new AppHost();

            _mainWindow = new MainWindow { DataContext = new MainWindowViewModel(_host) };
            desktop.MainWindow = _mainWindow;

            // Default shutdown mode (on last window close) is what we want: with close-to-tray on we
            // only *hide* the window (it stays open, so the app lives on); Quit shuts down explicitly.

            _notifications = new WindowNotificationManager(_mainWindow)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3,
            };

            SetUpTray();
            _mainWindow.Closing += OnMainWindowClosing;
            _host.SettingsChanged += _ => UpdateTrayVisibility();
            _host.Watchlist.AlertTriggered += OnAlertTriggered;

            desktop.ShutdownRequested += (_, _) => _host?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetUpTray()
    {
        var open = new NativeMenuItem("Open FleaTrackr");
        open.Click += (_, _) => ShowMainWindow();
        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => QuitApp();

        var menu = new NativeMenu();
        menu.Add(open);
        menu.Add(quit);

        _tray = new TrayIcon
        {
            ToolTipText = "FleaTrackr",
            Menu = menu,
            Icon = TryLoadIcon(),
        };
        _tray.Clicked += (_, _) => ShowMainWindow();

        TrayIcon.SetIcons(this, [_tray]);
        UpdateTrayVisibility();
    }

    private static WindowIcon? TryLoadIcon()
    {
        try
        {
            using System.IO.Stream stream = AssetLoader.Open(new Uri("avares://FleaTrackr.App/Assets/icon.ico"));
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateTrayVisibility()
    {
        // The tray icon only appears when "close to tray" is enabled - otherwise it would be a
        // surprising, unused icon.
        if (_tray is not null && _host is not null)
            _tray.IsVisible = _host.Settings.CloseToTray;
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // With close-to-tray on, the X button hides to the tray rather than exiting.
        if (_quitting || _host is null || !_host.Settings.CloseToTray) return;
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void QuitApp()
    {
        _quitting = true;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void OnAlertTriggered(TriggeredAlert alert)
    {
        // Fired off the UI thread by the scheduler - marshal before touching UI.
        Dispatcher.UIThread.Post(() =>
        {
            if (_host?.Settings.NotifyOnAlerts != true) return;

            // Tray tooltip reflects the latest alert even when the window is hidden.
            if (_tray is not null)
                _tray.ToolTipText = $"FleaTrackr - {alert.Message}";

            // In-window toast (visible when the window is shown).
            _notifications?.Show(new Notification("Price alert", alert.Message, NotificationType.Warning));
        });
    }
}
