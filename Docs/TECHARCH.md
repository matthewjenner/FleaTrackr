# FleaTrackr - Technical Architecture

## Shape

Two source projects mirroring the PlexTool/Klakr split:

- **FleaTrackr.Core** - pure, deterministic, fully unit-tested. Holds domain models, the network
  contract `ITarkovApi` (interface only), and all decision logic: profit math (`ProfitCalculator`),
  flip ranking (`FlipFinder`), alert evaluation (`AlertEvaluator`), and refresh scheduling policy
  (`RefreshPolicy`). No Avalonia, no HttpClient, no filesystem.
- **FleaTrackr.App** - the Avalonia UI plus all I/O: `TarkovApiClient` (the real `ITarkovApi` over
  HttpClient + GraphQL), the JSON stores, the refresh scheduler, alert surfacing, and Velopack.

This keeps every non-trivial rule testable against an in-memory `ITarkovApi` fake with no live API.

## API layer (P1)

`TarkovApiClient` posts GraphQL to `https://api.tarkov.dev/graphql` over a single pooled
`HttpClient`, `System.Text.Json` for (de)serialization, `gameMode` on every price query. It maps
the wire shape to Core models:

- **Item / PriceSnapshot** from `avg24hPrice, lastLowPrice, low24hPrice, high24hPrice,
  changeLast48hPercent, lastOfferCount, updated, basePrice` plus `buyFor`/`sellFor`
  (`{ price, currency, priceRUB, vendor { name } }`) - the "Flea Market" vendor entry is the flea
  price, others are traders.
- **Barter / Craft** from `barters`/`crafts` (`requiredItems`/`rewardItems`, trader/station, level).
- **HistoricalPricePoint** from `historicalItemPrices(id, days)` for the sparkline.

Resilience: 429 -> exponential backoff retry; a short-TTL `ItemCache` de-dupes repeated polls so a
busy watchlist stays under the ~60 req/min soft limit.

## Non-blocking refresh (P3)

`RefreshScheduler` (App) owns one lightweight `PeriodicTimer`-driven loop per watchlist item, all
off the UI thread. On tick it asks `RefreshPolicy` (Core, pure) which items are due, coalesces
them into batched GraphQL calls, then marshals results back via `Dispatcher.UIThread.Post(...)`
before raising `PropertyChanged`. `AlertEvaluator` (Core, pure) converts each new snapshot into
zero or more triggered alerts; `AlertService` (App) surfaces them (in-app highlight/banner in v1).

## Persistence & crash-safe state

Three plain-JSON stores under `%APPDATA%\FleaTrackr\`, all writing via `AtomicJson` (serialize to
`*.tmp`, then `File.Move` overwrite - atomic on one volume):

- `settings.json` (`SettingsStore`) - non-secret defaults.
- `watchlist.json` (`WatchlistStore`) - pinned items, per-item intervals, alert rules.
- `session.json` (`SessionStore`) - UI state (active tab, window geometry, game mode, last search +
  selection). Writes are **debounced (~1s after the last change)** rather than only on close, so a
  hard crash still leaves the last committed state on disk. Startup rehydrates from it; a
  missing/invalid file falls back to defaults and is logged, never fatal.

No secrets store: the API is unauthenticated, so nothing sensitive is ever persisted.

## UI

MVVM via CommunityToolkit.Mvvm source generators, compiled bindings on. Shell = `DockPanel`
(update banner, app header with the PVP/PVE toggle bound to `AppHost.GameMode`, `TabControl`).
`AppHost` is the composition root; ViewModels take it and never touch stores or the API client
directly beyond what `AppHost` exposes.

## Build, test, release

.NET 10, `Directory.Build.props` single `<VersionPrefix>`. `dotnet test` runs xUnit v2 (Core) and
headless Avalonia xunit v3 (App). CI (`release.yml`, P6) reads the version on push to `main`,
publishes win-x64 self-contained, packs with Velopack (`vpk`), and cuts a GitHub release, skipping
if the tag already exists. The in-app updater uses an unauthenticated Velopack `GithubSource`, so
the repo must be public.
