# FleaTrackr - Polish backlog (post-v1.0)

The core app is feature-complete and shipping as v1.0.0. This is the prioritized list of polish and
refinement work, grouped by value. Each item notes **what**, **why it matters**, **where** it lands,
and a rough **effort** (S = a sitting, M = half a day, L = a day+). Nothing here blocks release;
they make a good app better. Ordered so the highest-leverage items come first.

## Tier 1 - Real gaps worth closing first

### 1. Add a real Settings tab  (effort: M)
- **What:** The empty placeholder Settings tab was removed in v1.0. Add a real one when there are
  controls to host: default economy (PVP/PVE), default watchlist refresh cadence, default Flip
  Finder min-profit, player flea-market level, currency display, theme (System/Light/Dark), "open
  config folder", and the update controls.
- **Why:** The app already reads `AppSettings.DefaultRefreshSeconds` and a default economy, but there
  is no UI to change them. Several Tier 2/3 items (theme, currency, player level) also live here.
- **Where:** new `SettingsViewModel` + `SettingsView`; extend `AppSettings`; add the `TabItem` back
  and bump `MainWindowViewModel.TabCount` to 5. Persist via existing `AppHost.UpdateSettings`.

### 2. Net flea-market profit (subtract the sales fee)  (effort: M)
- **What:** Flip Finder (trader->flea) and Barters/Crafts reward values are currently **gross** of the
  flea sales fee. Implement the BSG fee formula and show net profit (keep gross available too).
- **Why:** The fee is significant on high-value items, so gross profit overstates real returns - the
  single biggest accuracy win. Doable from data we already fetch (`basePrice` + listing price).
- **Where:** new pure `FleaFee` calculator in `Core/Pricing` (fully unit-testable); fold into
  `ProfitCalculator` and `FlipFinder`/`FlipOpportunity`. Formula:
  `fee = V0*Ti*4^P0 + VR*Tr*4^PR` with `V0 = basePrice`, `VR = sale price`,
  `P0 = log10(V0/VR)` (raised to ^1.08 when VR<V0), `PR = log10(VR/V0)` (^1.08 when VR>=V0),
  `Ti = Tr = 0.03` (0.03 default; the Intelligence Center reduces it - expose as a setting later).

### 3. Player flea-level awareness  (effort: S)
- **What:** With the player level from Settings (#1), flag items whose `MinLevelForFlea` exceeds it -
  dim them and exclude from flip results (or mark "locked").
- **Why:** A flip you cannot list yet is not actionable; today the finder can suggest them.
- **Where:** `FlipFinder`/`FlipFinderViewModel` filter; a badge in Search/Watchlist rows.

## Tier 2 - Meaningful UX improvements

### 4. OS toast notifications + minimize-to-tray  (effort: M-L)
- **What:** Raise a Windows toast when a watchlist alert fires, and allow minimizing to the system
  tray so alerts keep working while the window is hidden.
- **Why:** Alerts are only useful if you see them; today they need the app focused. This makes the
  watchlist genuinely a background monitor - directly in the spirit of the original request.
- **Where:** `AlertService` (new) subscribing to `WatchlistService.AlertTriggered`; a tray icon in
  `App`/`MainWindow`. Consider `DesktopNotifications` or Windows `AppNotification` interop.

### 5. Richer price-history chart + range selector  (effort: M)
- **What:** Upgrade the sparkline to a small chart with min/max/last labels and a hover tooltip, and
  a 7 / 30 / 90-day selector (the API's `historicalItemPrices` takes `days`).
- **Why:** More decision-useful than a bare line; the data is already one call away.
- **Where:** extend `Sparkline` (or a new `PriceChart` control) + `SearchViewModel`.

### 6. Sorting & filtering across tabs  (effort: M)
- **What:** Sort Search results (price / 48h change / name) and Flip Finder (profit / ROI / direction),
  plus a direction filter and an optional category scope for the flip scan (scan one category faster).
- **Why:** Scales the tabs from "a list" to "a tool"; category-scoped scans also cut API load.
- **Where:** the three list view models; `items(categoryNames:)` for scoped scans in `TarkovApiClient`.

### 7. Finish session restore for selection  (effort: S)
- **What:** `SessionState.SelectedWatchlistItemId` is persisted but never re-applied; the Flip Finder's
  scan params are not remembered either. Wire both.
- **Why:** Completes the "returns exactly where you left off" promise.
- **Where:** `WatchlistViewModel` / `FlipFinderViewModel` + `MainWindowViewModel` restore path.

### 8. Barter/craft depth  (effort: M)
- **What:** Per-required-item price breakdown (hover/expand), trader-loyalty and quest-unlock gating,
  and a "used in" view (`bartersUsing`/`craftsUsing`) so you can see where an item is consumed.
- **Why:** Turns the tab from "can I profit" into "should I hoard this".
- **Where:** `ITarkovApi` additions + `BartersCraftsViewModel`/`TradeRowViewModel`.

## Tier 3 - Nice to have / ongoing

### 9. Theme toggle & currency display  (effort: S each)
- Explicit Light/Dark override in Settings (today follows system); show USD/EUR equivalents alongside
  roubles. Both are small once Settings (#1) exists.

### 10. Scan caching & rate-limit visibility  (effort: S-M)
- Cache the last flip scan with a timestamp (avoid a full re-page on quick revisits); show a small
  "requests this minute" indicator so power users can see headroom under the ~60/min limit.

### 11. Icon disk cache  (effort: S)
- `ImageLoader` caches decoded icons in memory only; persist to `%APPDATA%\FleaTrackr\icons\` so they
  survive restarts and cut cold-start network.

### 12. Accessibility & onboarding  (effort: M, ongoing)
- `AutomationProperties` labels, full keyboard navigation and focus order, and a first-run hint on the
  empty Search/Watchlist tabs.

### 13. Diagnostics logging  (effort: S)
- A rolling log file under `%APPDATA%\FleaTrackr\logs\` for API errors and update failures (today they
  are silently swallowed), behind a Settings toggle.

### 14. UI interaction tests  (effort: M, ongoing)
- Headless Avalonia tests that actually drive the tabs (type a search, add to watchlist, run a scan),
  plus a `FleaFee` unit-test suite once #2 lands.

## Suggested order

1 -> 2 -> 3 (Settings unlocks #3 and parts of #9/#13), then 4 and 5 for watchlist/chart UX, then
6/7/8 as time allows. Tier 3 is opportunistic. Each Tier 1/2 item is independently shippable and
should come with tests and a `Scripts/bump-version.sh` patch bump.
