# FleaTrackr - Workplan (living build tracker)

The single source of truth for build progress: current phase, what is done, what is next, and the
decisions log. Update it as each phase lands.

## Current status

- **Phase:** All phases (P0-P6) complete. Feature-complete v1.
- **Builds/tests:** `dotnet build` and `dotnet test` green, 0 warnings (47 tests: 24 Core, 23 App).
  All four feature tabs wired; app launches clean. Live search/barter/craft/flip all verified
  against the API.
- **Avalonia 12 API notes learned:** `TextBox.Watermark` is obsolete -> use `PlaceholderText`;
  `IsVisible` does not auto-coerce an int Count to bool (use an explicit bool property).
- **Build gotcha:** kill any stray `FleaTrackr.App.exe` before rebuilding (it locks the output DLL).
- **Remaining before first release:** the GitHub repo must be **public** and have at least one
  release for the in-app updater to work; push to `main` triggers `release.yml` (after a version
  bump). Optional future polish: OS toast notifications, flea-fee-net flip profit, richer charts.

## Phases

- [x] **P0 - Repo + skeleton.** `git init` + `origin` remote, `.gitignore`, `FleaTrackr.slnx`,
  4 projects (App, Core, Core.Tests, App.Tests), `Directory.Build.props` versioning,
  `AtomicJson` + `SettingsStore`/`AppSettings`/`AppPaths`/`AppVersion`, `AppHost` with the PVP/PVE
  game-mode state, and a runnable Avalonia shell (`MainWindow` with the economy toggle, update
  banner, and five stubbed tabs). Scripts `run.sh` + `bump-version.sh`. Headless shell test.
- [x] **P1 - API layer.** `ITarkovApi` + models (`Item`, `PriceSnapshot`, `VendorPrice`, `ItemStack`,
  `Barter`, `Craft`, `HistoricalPricePoint`) in Core; `TarkovApiClient` (GraphQL over HttpClient,
  typed `gameMode` variable, 429/5xx exponential backoff, short-TTL `ItemCache`) + wire DTOs in App,
  wired into `AppHost.Api`. Tests: `FakeTarkovApi` in-memory fake, Item/PriceSnapshot price logic
  (Core), and offline client mapping/mode/retry via a stub HttpMessageHandler (App).
- [x] **P2 - Search tab.** Debounced search box -> results list (name, flea sell, colored 48h
  change) -> detail pane (icon via `ImageLoader`, 7-day `Sparkline`, full price breakdown, min flea
  level, wiki link). Economy toggle re-queries + repriced detail. `PriceFormat` (Core),
  `SignToBrushConverter`, `ItemRowViewModel`. Tests drive the VM through the real client + stub HTTP.
  (Add-to-Watchlist button deferred to P3, where the watchlist model lands.)
- [x] **P3 - Watchlist + non-blocking refresh + alerts + session restore.** Core: `AlertRule`/
  `AlertKind`, edge-triggered `AlertEvaluator`, pure `RefreshPolicy`, `WatchedItemConfig`,
  `TriggeredAlert`. App: `RefreshScheduler` (one background loop, batched due-item fetches off the
  UI thread, `TickAsync` testable), `WatchlistService` (persist + scheduler + events),
  `WatchlistStore`, `SessionStore` (debounced, atomic, crash-safe) + `SessionState`. UI: Watchlist
  tab (per-row live price/trend, editable cadence dropdown, inline alert editor, alert feed),
  Add-to-Watchlist on Search, window-geometry + tab + query/selection + mode restore wired through
  `MainWindow`. Tests: RefreshPolicy, AlertEvaluator, RefreshScheduler ticks, WatchlistService
  persistence, SessionStore/WatchlistStore round-trips.
- [x] **P4 - Barters & Crafts.** Core `ProfitCalculator` + `TradeCost` (input = cheapest acquisition
  across flea/traders, output = flea sell then best trader; null if any leg unpriced). Tab: item
  picker -> profit-ranked barter and craft lists (`TradeRowViewModel` with cost/value/profit/ROI,
  crafts add duration + profit/hour). Tests: ProfitCalculator (Core), trade load + ranking (App).
- [x] **P5 - Flip Finder.** Core `FlipFinder` + `FlipOpportunity` (trader-buy < flea-sell, and flea
  < trader-sell), ranked by profit with a min-profit floor. API `GetItemsPageAsync` (paged). Tab: a
  bounded (`MaxItems` 2000, `PageSize` 200), cancellable, progress-reporting scan with a min-profit
  input; results table (item, direction, buy, sell, profit, ROI) flags flea-sale rows as gross of
  the market fee. Tests: FlipFinder ranking/min (Core), scan + ranking (App).
- [x] **P6 - Polish.** Real app icon (built from the user's PNG via ImageMagick, multi-resolution).
  Velopack `UpdateService` (hourly GitHub poll, skip/dismiss/install) wired into `AppHost` +
  the main-window banner (Install/Skip/Later). `.github/workflows/release.yml` (version-gated,
  Velopack `vpk pack`, idempotent). README written; CLAUDE.md finalized.

## Decisions log

- **No official EFT API exists** -> use the community tarkov.dev GraphQL API
  (`https://api.tarkov.dev/graphql`), unauthenticated, ~60 req/min soft limit, Cloudflare-cached,
  PVP/PVE via `gameMode` ("regular"/"pve").
- **No secrets store / DPAPI** (unlike PlexTool): the API needs no credentials, so FleaTrackr
  persists only plain JSON. All three stores write via `AtomicJson` (temp + move) for crash safety.
- **Avalonia pinned to 12.0.5** (latest stable, verified July 2026); whole Avalonia family moves
  together. **FluentAssertions pinned 7.x** (8.x is a paid license).
- **Game mode** is session state on `AppHost` (toggle without rewriting settings each flip), with
  the persisted default in `AppSettings.GameMode`.
- **Placeholder icon** copied from PlexTool for P0; replaced with a FleaTrackr icon in P6.
