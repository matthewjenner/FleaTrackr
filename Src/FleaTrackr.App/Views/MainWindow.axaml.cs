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

        if (s.WindowX is { } x && s.WindowY is { } y)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(x, y);
        }

        if (s.Maximized)
            WindowState = WindowState.Maximized;

        _restored = true;
    }

    private void PersistGeometry()
    {
        if (!_restored || Vm is not { } vm) return;

        bool maximized = WindowState == WindowState.Maximized;

        // Only capture size/position while in the normal state, so a maximized window does not
        // overwrite the remembered restore bounds.
        if (maximized)
            vm.PersistWindow(vm.Session.WindowWidth, vm.Session.WindowHeight,
                vm.Session.WindowX, vm.Session.WindowY, maximized: true);
        else
            vm.PersistWindow(Width, Height, Position.X, Position.Y, maximized: false);
    }
}
