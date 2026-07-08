using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleaTrackr.App.Services;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the Settings tab. Each field edits <see cref="AppSettings"/> and persists immediately via
/// <see cref="AppHost.UpdateSettings"/>; the values feed defaults and the profit/flip calculations
/// (player flea level, estimated-fee reduction). A load guard stops the initial field assignment
/// from triggering a redundant save.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly AppHost _host;
    private readonly bool _loaded;

    public SettingsViewModel(AppHost host)
    {
        _host = host;
        AppSettings s = host.Settings;

        _defaultRefresh = RefreshOption.ForSeconds(s.DefaultRefreshSeconds);
        _defaultMinProfit = s.DefaultMinProfit;
        _playerFleaLevel = s.PlayerFleaLevel;
        _fleaFeeReductionPercent = s.FleaFeeReductionPercent;

        _loaded = true;
    }

    public IReadOnlyList<RefreshOption> RefreshOptions => RefreshOption.All;

    [ObservableProperty] private RefreshOption _defaultRefresh;
    [ObservableProperty] private int _defaultMinProfit;
    [ObservableProperty] private int _playerFleaLevel;
    [ObservableProperty] private double _fleaFeeReductionPercent;

    /// <summary>Where the app's config/data files live, shown for reference.</summary>
    public string ConfigFolderPath => AppPaths.BaseDirectory;

    partial void OnDefaultRefreshChanged(RefreshOption value) => Save();
    partial void OnDefaultMinProfitChanged(int value) => Save();
    partial void OnPlayerFleaLevelChanged(int value) => Save();
    partial void OnFleaFeeReductionPercentChanged(double value) => Save();

    private void Save()
    {
        if (!_loaded) return;
        _host.UpdateSettings(_host.Settings with
        {
            DefaultRefreshSeconds = DefaultRefresh.Seconds,
            DefaultMinProfit = Math.Max(0, DefaultMinProfit),
            PlayerFleaLevel = Math.Clamp(PlayerFleaLevel, 1, 79),
            FleaFeeReductionPercent = Math.Clamp(FleaFeeReductionPercent, 0, 100),
        });
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            Directory.CreateDirectory(ConfigFolderPath);
            Process.Start(new ProcessStartInfo(ConfigFolderPath) { UseShellExecute = true });
        }
        catch
        {
            // Opening a folder is a convenience - failure is not worth interrupting the user.
        }
    }
}
