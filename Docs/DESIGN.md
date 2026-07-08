# FleaTrackr - Design

## What it is

A Windows desktop companion for the Escape from Tarkov flea market. It answers "what is this
worth right now, where should I buy/sell it, and what can I profit on" using live community
market data, and it watches items for you and alerts when a price crosses a threshold.

## Why a third-party API

Battlestate Games ships **no official flea-market API** - deliberately, to stop bots snapping up
high-value listings. The community fills the gap. FleaTrackr uses **tarkov.dev's GraphQL API**
(`the-hideout/tarkov-api`): free, unauthenticated, broad coverage (items, flea + trader prices,
barters, crafts, price history), separate PVP and PVE economies. Prices are community-sourced and
refresh every few minutes server-side, so FleaTrackr treats them as near-real-time, not tick data.

## Audience

Tarkov players managing inventory value and hideout crafting, plus flippers hunting arbitrage.
Both PVP and PVE players are served via an in-app economy toggle.

## Features (v1)

1. **Search + price detail** - find any item; see flea avg/low/high over 24h, 48h change, best
   trader buy/sell, base price, min flea level, an icon, a price-history sparkline, and a wiki link.
2. **Watchlist + alerts** - pin items; each has its **own adjustable refresh interval** (e.g.
   30s / 1m / 5m / manual) and **alert rules** (price above/below a threshold, or a % move).
   Alerts fire without blocking the app; fast-moving items can refresh quickly while everything
   else stays responsive. UX is the priority.
3. **Barters & Crafts** - for an item, list barter trades and hideout crafts with computed profit
   (input cost vs output flea value), sorted best-first.
4. **Flip Finder** - scan for arbitrage: trader buy price below flea sell value (and the reverse),
   ranked by profit / ROI. Bounded and paged to respect the API rate limit.

## Product principles

- **Near-real-time, honestly labelled** - always show when a price was last updated; never imply
  precision the source does not have. Nulls (item not on flea, fresh wipe) are shown plainly.
- **Never lock the UI** - all network and timer work is off the UI thread.
- **Remember where I was** - the app restores its last state on reopen, even after a crash.
- **Be a good API citizen** - cache, batch, and back off; do not hammer a free community service.

## Deferred (post-v1)

OS toast notifications, multi-item comparison, offline history persistence, price charts beyond a
sparkline, and quest/hideout requirement tracking.
