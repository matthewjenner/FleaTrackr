using Avalonia;
using Avalonia.Controls;
using FleaTrackr.App.Services;
using FleaTrackr.App.ViewModels;

namespace FleaTrackr.App.Views;

public partial class MainWindow : Window
{
    private bool _restored;

    public MainWindow()
    {
        InitializeComponent();

        // Restore geometry once the window opens (DataContext is set by then), then persist any
        // later move/resize/maximize through the debounced, crash-safe session store.
        Opened += (_, _) => RestoreGeometry();
        PositionChanged += (_, _) => PersistGeometry();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ClientSizeProperty || e.Property == WindowStateProperty)
                PersistGeometry();
        };
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private void RestoreGeometry()
    {
        if (Vm is not { } vm) return;
        SessionState s = vm.Session;

        Width = s.WindowWidth;
        Height = s.WindowHeight;

        // Only honour a saved position if it still lands on a connected screen. This guards against
        // a stale position from a now-disconnected monitor and the (-32000,-32000) sentinel Windows
        // reports for a minimized window; otherwise center and scrub the bad coordinates so they
        // do not persist into the next launch.
        if (s.WindowX is { } x && s.WindowY is { } y)
        {
            if (IsOnAnyScreen(new PixelPoint(x, y)))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint(x, y);
            }
            else
            {
                vm.PersistWindow(s.WindowWidth, s.WindowHeight, null, null, s.Maximized);
            }
        }

        if (s.Maximized)
            WindowState = WindowState.Maximized;

        _restored = true;
    }

    private void PersistGeometry()
    {
        if (!_restored || Vm is not { } vm) return;

        // Never persist geometry while minimized: a minimized window reports its position as
        // (-32000,-32000), which would otherwise be saved and restored off-screen next launch.
        if (WindowState == WindowState.Minimized) return;

        if (WindowState == WindowState.Maximized)
            // Keep the remembered normal bounds; only record that we were maximized.
            vm.PersistWindow(vm.Session.WindowWidth, vm.Session.WindowHeight,
                vm.Session.WindowX, vm.Session.WindowY, maximized: true);
        else
            vm.PersistWindow(Width, Height, Position.X, Position.Y, maximized: false);
    }

    /// <summary>True if <paramref name="point"/> falls within the bounds of any connected screen.</summary>
    private bool IsOnAnyScreen(PixelPoint point)
    {
        IReadOnlyList<Avalonia.Platform.Screen>? all = Screens?.All;
        if (all is null || all.Count == 0) return true; // can't verify - trust the saved value
        foreach (Avalonia.Platform.Screen screen in all)
            if (screen.Bounds.Contains(point))
                return true;
        return false;
    }
}
