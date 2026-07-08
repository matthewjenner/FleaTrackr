# FleaTrackr

Windows Avalonia (.NET 10) desktop app for tracking Escape from Tarkov flea-market data: search
items and prices, keep a watchlist with per-item refresh and price alerts, compute barter/craft
profit, and find flip/arbitrage opportunities. There is **no official EFT flea market API**
(Battlestate withholds one to deter bots), so FleaTrackr uses the community **tarkov.dev GraphQL
API** (`the-hideout/tarkov-api`) at `https://api.tarkov.dev/graphql` - free, no API key. The
project template (layout, versioning, Velopack auto-update, release CI) is lifted from the user's
PlexTool/Klakr apps - keep the three broadly in step.

See `Docs/DESIGN.md` for product detail, `Docs/TECHARCH.md` for architecture, and
`Docs/workplan.md` for the living build tracker (current phase, what is done, decisions log).
This file is the quick orientation - read it first.

## Stack

- .NET 10, C#
- Avalonia 12 (UI) - pinned to the 12.x line (currently 12.0.5); the whole Avalonia package
  family (Desktop, Fluent, Inter, DiagnosticsSupport) must move together.
- CommunityToolkit.Mvvm (MVVM source generators)
- Velopack (in-app update check + release packaging via the `vpk` CLI in CI)
- System.Text.Json (settings / watchlist / session serialization)
- xUnit + FluentAssertions (tests; FluentAssertions pinned to 7.x - see Gotchas)

## Layout

```
Src/FleaTrackr.Core/     Pure logic, no UI/HTTP/IO. Fully unit-tested.
   Models/               GameMode, Item, PriceSnapshot, VendorPrice, ItemStack, Barter, Craft,
                         HistoricalPricePoint; ITarkovApi (network contract as an interface)
   Pricing/              PriceFormat, ProfitCalculator/TradeCost, FlipFinder/FlipOpportunity
   Watchlist/            AlertRule/AlertKind, AlertEvaluator, RefreshPolicy, WatchedItemConfig
Src/FleaTrackr.App/      Avalonia app + all I/O.
   Services/             AppHost (composition root), AppPaths, AppSettings, SettingsStore,
                         AtomicJson (crash-safe temp+move writes), TarkovApiClient + DTOs,
                         ItemCache, ImageLoader, WatchlistStore, WatchlistService, RefreshScheduler,
                         SessionStore/SessionState, UpdateService, AppVersion
   Controls/             Sparkline (custom-drawn price history)
   Converters/           SignToBrushConverter, AlertHighlightConverter
   ViewModels/           MainWindow + one per tab, row VMs, ViewModelBase
   Views/                MainWindow + one View per tab
Tests/FleaTrackr.Core.Tests/   xUnit (v2) for Core, against an in-memory ITarkovApi fake.
Tests/FleaTrackr.App.Tests/    Headless Avalonia (xunit v3) - VM/client/store/scheduler tests.
Docs/                    DESIGN.md, TECHARCH.md, workplan.md.
Scripts/                 run.sh (clean+build+run), bump-version.sh.
.github/workflows/       release.yml - reads Directory.Build.props, ships via Velopack.
Directory.Build.props    Single source of truth for the app version.
```

## Build & run

```bash
dotnet restore
dotnet build
dotnet run --project Src/FleaTrackr.App
dotnet test

./Scripts/run.sh            # clean + build + run (optional: Debug|Release arg)
```

## Core principle: Core stays pure, App does I/O

`FleaTrackr.Core` computes and decides but never touches the network or disk. It defines
`ITarkovApi` as an interface only; `FleaTrackr.App` provides the real `TarkovApiClient` (GraphQL
over HttpClient) and executes against it. Profit math, flip ranking, alert evaluation, and
refresh scheduling logic live in Core so they are testable with an in-memory `ITarkovApi` fake and
no live API. Do not pull Avalonia, HttpClient, or filesystem calls into Core.

## The tabs (Search | Watchlist | Barters & Crafts | Flip Finder)

A Settings tab is intentionally absent until there are user-editable settings to host (see
`Docs/polish-backlog.md`); today's settings use sensible defaults. `TabCount` in
`MainWindowViewModel` must stay in step with the tabs and the restored-index clamp.

The shell is a `DockPanel`: update banner docked top, an app header with the **PVP/PVE economy
toggle** (bound to `AppHost.GameMode` - changing it re-queries the active tab), then a `TabControl`.
New tab = wiring points: a `ViewModel`, a `View` (+`.axaml.cs`), a property on
`MainWindowViewModel`, and a `<TabItem>` in `MainWindow.axaml`.

## API integration (tarkov.dev GraphQL)

- Endpoint `https://api.tarkov.dev/graphql`, POST GraphQL, no auth. Pass `gameMode: regular|pve`
  on every price query (see `GameMode.ToApiValue`).
- Flea vs trader prices come from `buyFor`/`sellFor` arrays: the entry whose `vendor.name` is
  **"Flea Market"** is the flea price; the rest are traders/Fence. Use `priceRUB` to compare.
- Soft rate limit ~60 req/min, Cloudflare-cached. Use `ItemCache` so repeated watchlist polls
  reuse a recent snapshot; retry 429s with exponential backoff. Never hammer the API.

## Non-blocking refresh (core UX requirement)

Each watchlist item refreshes on its **own adjustable cadence** via `RefreshScheduler`
(`PeriodicTimer` + `Task`, off the UI thread); results marshal back with
`Dispatcher.UIThread.Post(...)` before raising `PropertyChanged`. A fast 30s item must never block
a slow one or the rest of the app. `RefreshPolicy` (Core, pure) decides when an item is due and
batches due items; `AlertEvaluator` (Core, pure) turns each snapshot into triggered alerts.

## Persistence & crash-safe state (hard requirement)

All under `%APPDATA%\FleaTrackr\`, every write via `AtomicJson` (temp file + move) so a crash
mid-write never corrupts a file:
- `settings.json` (`SettingsStore`) - game mode default, refresh/alert defaults.
- `watchlist.json` (`WatchlistStore`) - pinned items + per-item interval + alert rules.
- `session.json` (`SessionStore`) - UI state for restore-on-reopen: active tab, window
  geometry, PVP/PVE mode, last search + selection. **Written debounced on change, not only on
  close**, so a hard crash still restores the last state. A missing/invalid file falls back to
  defaults and is never fatal.
- No secrets/DPAPI store - FleaTrackr holds no credentials.

## Conventions

- **MVVM**: ViewModels never reference Views. Use CommunityToolkit.Mvvm `[ObservableProperty]` /
  `[RelayCommand]` source generators, not hand-rolled `INotifyPropertyChanged`.
- **ASCII punctuation only** in all UI text, code, comments, and docs. No em-dashes, en-dashes,
  or unicode ellipsis - write "-" and "...". The user notices AI-artifact punctuation.
- **Compiled bindings** on (`AvaloniaUseCompiledBindingsByDefault`); give each `x:DataType`.
- **Threading**: API calls and timers run off the UI thread; marshal back via `Dispatcher.UIThread`
  before raising `PropertyChanged`.
- **Naming**: ViewModels end in `ViewModel`. Views end in `Window` or `View`.

## Releasing / versioning (identical to PlexTool)

- Version lives in `Directory.Build.props` as a single `<VersionPrefix>`.
- `./Scripts/bump-version.sh` (default Patch; pass `Minor`/`Major`) bumps it. Do this whenever a
  feature or behavior change is complete and will ship, ideally in the same commit. Do NOT bump for
  docs, comments, or refactors with no user-visible effect.
- Push to `main`: `.github/workflows/release.yml` reads the version, skips if the `vX.Y.Z`
  release already exists, else tests + publishes win-x64 self-contained + `vpk pack --packId
  FleaTrackr --mainExe FleaTrackr.App.exe` + creates the GitHub release. CI never commits.
- The repo MUST be public for the in-app update check (unauthenticated `GithubSource`) to work.
- The user handles all git adds/commits. `origin` is
  `https://github.com/matthewjenner/FleaTrackr.git`.

## Gotchas

- **FluentAssertions pinned to 7.x**: 8.x moved to a paid commercial license. Do not bump without
  the user accepting that license.
- **Respect the API rate limit**: cache, batch, and back off. A watchlist of many fast-refresh
  items must still stay under ~60 req/min - coalesce due items into batched queries.
- **Flea price is not a top-level field**: `avg24hPrice`/`lastLowPrice` exist, but the actual
  flea buy/sell lives in `buyFor`/`sellFor` under the "Flea Market" vendor. Read both.
- **Prices can be null** (item not currently on flea, or fresh wipe) - handle nulls in the UI.
