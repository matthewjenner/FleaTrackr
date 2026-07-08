# FleaTrackr

A Windows desktop companion for the **Escape from Tarkov** flea market. Search live prices, keep a
watchlist that refreshes on its own cadence and alerts you on price moves, work out barter and
craft profit, and hunt trader/flea arbitrage - all in a fast native app.

Built with [Avalonia](https://avaloniaui.net/) on .NET 10.

> **Prices come from the community [tarkov.dev](https://tarkov.dev/) API**
> ([the-hideout/tarkov-api](https://github.com/the-hideout/tarkov-api)). Battlestate Games ships no
> official flea-market API - deliberately, to deter bots - so FleaTrackr relies on tarkov.dev's
> free, community-run GraphQL API. Prices are community-sourced and refresh every few minutes;
> treat them as near-real-time, not tick data. Please be kind to a free service - FleaTrackr caches
> and rate-limits its requests, and you should not hammer it either.

## Features

- **Search + price detail** - find any item and see flea average / low / high over 24h, the 48h
  change, best trader buy/sell, base price, minimum flea level, a 7-day price sparkline, and a wiki
  link.
- **Watchlist + alerts** - pin items and track them live. Each item has its **own refresh cadence**
  (30s / 1m / 5m / 15m / manual) and **alert rules** (price below/above a threshold, or a 48h % move).
  The refresh is fully non-blocking, so fast-moving items update quickly without freezing the rest
  of the app. A row flashes and an alert feed logs each trigger.
- **Barters & Crafts** - for any item, list every trader barter and hideout craft that produces it,
  with **profit** computed against current prices (cheapest input acquisition vs flea sale value),
  ranked best-first. Crafts also show duration and profit/hour.
- **Flip Finder** - a bounded, cancellable scan of the market that ranks **trader/flea arbitrage**
  opportunities by profit and ROI (buy from a trader and resell on flea, or the reverse).
- **PVP / PVE toggle** - switch economies in the header; every price re-queries for the selected mode.
- **Remembers where you were** - the active tab, window size/position, economy, and last search are
  restored on reopen, even after a crash (state is saved continuously, not just on close).

## Install

Grab the latest installer from the [Releases](https://github.com/matthewjenner/FleaTrackr/releases)
page and run it. FleaTrackr updates itself: when a new release is published, an in-app banner offers
to install it. Windows 10/11, 64-bit.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet build
dotnet run --project Src/FleaTrackr.App

# or: clean + build + run
./Scripts/run.sh

dotnet test
```

## Where your data lives

Everything is plain JSON under `%APPDATA%\FleaTrackr\` (no accounts, no credentials - the API needs
none):

- `settings.json` - default economy and refresh/alert defaults.
- `watchlist.json` - your pinned items, per-item cadences, and alert rules.
- `session.json` - UI state for restore-on-reopen.

Delete any of these to reset that part of the app; each is rewritten with defaults on next launch.

## Architecture

- **`FleaTrackr.Core`** - pure, fully unit-tested domain logic: models, the `ITarkovApi` contract,
  profit/flip math, and alert/refresh policy. No UI, no network, no disk.
- **`FleaTrackr.App`** - the Avalonia UI and all I/O: the GraphQL API client, the JSON stores, the
  non-blocking refresh scheduler, and Velopack auto-update.

See [`Docs/`](Docs/) for the design, technical architecture, and build tracker, and
[`CLAUDE.md`](CLAUDE.md) for a quick orientation.

## Credits

- Market data by [tarkov.dev](https://tarkov.dev/) and the
  [the-hideout](https://github.com/the-hideout) community. FleaTrackr is not affiliated with them or
  with Battlestate Games.
- Escape from Tarkov is a trademark of Battlestate Games. This is an unofficial fan-made tool.
