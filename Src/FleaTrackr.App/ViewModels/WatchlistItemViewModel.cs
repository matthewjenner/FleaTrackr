using System.Globalization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// One watchlist row: live price/trend from the latest snapshot, an editable refresh cadence, an
/// inline alert editor, and per-row remove / refresh-now actions. All edits are pushed through the
/// <see cref="WatchlistService"/>, which persists them and updates the scheduler.
/// </summary>
public sealed partial class WatchlistItemViewModel : ViewModelBase
{
    private readonly WatchlistService _service;
    private bool _suppressIntervalPush;

    public WatchlistItemViewModel(WatchedItemConfig config, WatchlistService service)
    {
        _service = service;
        Config = config;
        _selectedRefresh = RefreshOption.ForSeconds(config.RefreshSeconds);
        LoadRulesIntoEditor(config.Rules);
        _ = LoadIconAsync();
    }

    public WatchedItemConfig Config { get; private set; }

    public string ItemId => Config.ItemId;
    public string Name => Config.Name;
    public string ShortName => Config.ShortName;

    [ObservableProperty]
    private Bitmap? _icon;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceText))]
    [NotifyPropertyChangedFor(nameof(ChangeText))]
    [NotifyPropertyChangedFor(nameof(ChangeValue))]
    [NotifyPropertyChangedFor(nameof(TrendArrow))]
    private PriceSnapshot? _snapshot;

    /// <summary>Live reference price (flea sell, or best trader when unlisted).</summary>
    public string PriceText => PriceFormat.Rub(Snapshot?.ReferencePrice);
    public string ChangeText => PriceFormat.Percent(Snapshot?.ChangeLast48hPercent);
    public double ChangeValue => Snapshot?.ChangeLast48hPercent ?? 0;

    /// <summary>ASCII trend glyph for the change direction.</summary>
    public string TrendArrow => ChangeValue > 0 ? "^" : ChangeValue < 0 ? "v" : "-";

    [ObservableProperty]
    private string _lastRefreshedText = "Waiting for first refresh...";

    /// <summary>Set true briefly when an alert fires, to flash the row.</summary>
    [ObservableProperty]
    private bool _isAlerting;

    private DateTimeOffset? _lastRefreshedAt;

    // ---- Refresh cadence ----

    public IReadOnlyList<RefreshOption> RefreshOptions => RefreshOption.All;

    [ObservableProperty]
    private RefreshOption _selectedRefresh;

    partial void OnSelectedRefreshChanged(RefreshOption value)
    {
        if (_suppressIntervalPush || value.Seconds == Config.RefreshSeconds) return;
        _service.SetInterval(ItemId, value.Seconds);
    }

    // ---- Alert editor (blank field = no rule of that kind) ----

    [ObservableProperty] private string _priceBelow = "";
    [ObservableProperty] private string _priceAbove = "";
    [ObservableProperty] private string _percentMove = "";

    /// <summary>One-line summary of the configured rules, shown on the collapsed row.</summary>
    public string AlertSummary
    {
        get
        {
            if (Config.Rules.Count == 0) return "No alerts";
            var parts = new List<string>();
            foreach (AlertRule r in Config.Rules)
            {
                parts.Add(r.Kind switch
                {
                    AlertKind.PriceDropsBelow => $"below {PriceFormat.Rub((int)r.Threshold)}",
                    AlertKind.PriceRisesAbove => $"above {PriceFormat.Rub((int)r.Threshold)}",
                    AlertKind.PercentDropExceeds => $"-{r.Threshold:0.#}%",
                    AlertKind.PercentRiseExceeds => $"+{r.Threshold:0.#}%",
                    _ => "",
                });
            }
            // Collapse the symmetric +/-N% move pair into one "move N%".
            return string.Join(", ", parts.Distinct());
        }
    }

    [RelayCommand]
    private void ApplyAlerts()
    {
        var rules = new List<AlertRule>();
        if (TryParsePrice(PriceBelow, out int below)) rules.Add(new AlertRule(AlertKind.PriceDropsBelow, below));
        if (TryParsePrice(PriceAbove, out int above)) rules.Add(new AlertRule(AlertKind.PriceRisesAbove, above));
        if (TryParsePercent(PercentMove, out double pct) && pct > 0)
        {
            rules.Add(new AlertRule(AlertKind.PercentDropExceeds, pct));
            rules.Add(new AlertRule(AlertKind.PercentRiseExceeds, pct));
        }
        _service.SetRules(ItemId, rules);
    }

    [RelayCommand]
    private void ClearAlerts()
    {
        PriceBelow = PriceAbove = PercentMove = "";
        _service.SetRules(ItemId, []);
    }

    [RelayCommand]
    private void Remove() => _service.Remove(ItemId);

    [RelayCommand]
    private void RefreshNow() => _service.RefreshNow(ItemId);

    // ---- Called by WatchlistViewModel (on the UI thread) ----

    /// <summary>Applies a fresh live snapshot and stamps the refresh time.</summary>
    public void ApplySnapshot(PriceSnapshot snapshot, DateTimeOffset now)
    {
        Snapshot = snapshot;
        _lastRefreshedAt = now;
        RecomputeRelativeTime(now);
    }

    /// <summary>Re-applies config after an edit (interval/rules) so the row reflects persisted state.</summary>
    public void UpdateConfig(WatchedItemConfig config)
    {
        Config = config;
        _suppressIntervalPush = true;
        SelectedRefresh = RefreshOption.ForSeconds(config.RefreshSeconds);
        _suppressIntervalPush = false;
        LoadRulesIntoEditor(config.Rules);
        OnPropertyChanged(nameof(AlertSummary));
    }

    /// <summary>Flags the row as recently alerted (the view flashes it).</summary>
    public void MarkAlerting() => IsAlerting = true;

    /// <summary>Clears the alert flash and refreshes the "Ns ago" label; driven by a 1s UI timer.</summary>
    public void Tick(DateTimeOffset now)
    {
        IsAlerting = false;
        RecomputeRelativeTime(now);
    }

    private void RecomputeRelativeTime(DateTimeOffset now)
    {
        if (_lastRefreshedAt is not { } at) return;
        int seconds = (int)Math.Max(0, (now - at).TotalSeconds);
        LastRefreshedText = seconds < 2 ? "Refreshed just now"
            : seconds < 60 ? $"Refreshed {seconds}s ago"
            : $"Refreshed {seconds / 60}m ago";
    }

    private void LoadRulesIntoEditor(IReadOnlyList<AlertRule> rules)
    {
        PriceBelow = rules.FirstOrDefault(r => r.Kind == AlertKind.PriceDropsBelow)?.Threshold is { } b
            ? ((int)b).ToString(CultureInfo.InvariantCulture) : "";
        PriceAbove = rules.FirstOrDefault(r => r.Kind == AlertKind.PriceRisesAbove)?.Threshold is { } a
            ? ((int)a).ToString(CultureInfo.InvariantCulture) : "";
        PercentMove = rules.FirstOrDefault(r => r.Kind is AlertKind.PercentDropExceeds or AlertKind.PercentRiseExceeds)?.Threshold is { } p
            ? p.ToString("0.#", CultureInfo.InvariantCulture) : "";
    }

    private async Task LoadIconAsync() => Icon = await ImageLoader.LoadAsync(Config.IconLink);

    private static bool TryParsePrice(string text, out int value)
    {
        value = 0;
        string cleaned = text.Replace(",", "").Replace(" ", "").Trim();
        return int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    private static bool TryParsePercent(string text, out double value) =>
        double.TryParse(text.Replace("%", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
