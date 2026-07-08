# FleaTrackr - Workplan (living build tracker)

The single source of truth for build progress: current phase, what is done, what is next, and the
decisions log. Update it as each phase lands.

## Current status

- **Phase:** P4 complete; P5 next.
- **Builds/tests:** `dotnet build` and `dotnet test` green, 0 warnings (41 tests: 20 Core, 21 App).
  App launches with no binding errors; Search + Watchlist + Barters/Crafts wired. Live barter/craft
  profit verified against the API.
- **Avalonia 12 API notes learned:** `TextBox.Watermark` is obsolete -> use `PlaceholderText`;
  `IsVisible` does not auto-coerce an int Count to bool (use an explicit bool property).

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
- [ ] **P5 - Flip Finder.** `FlipFinder` bounded/paged arbitrage scan ranked by profit/ROI.
- [ ] **P6 - Polish.** Real app icon, Velopack `UpdateService` + banner wiring, `.github/workflows/
  release.yml`, README + CLAUDE.md finalization.

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
