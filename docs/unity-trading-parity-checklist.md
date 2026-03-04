# Unity Trading Parity Checklist

Story: `#268` Unity Migration: migrate Trading gameplay module

## Scope

- Market listings load in Unity from `/api/market/listings`.
- Transaction history loads in Unity from `/api/market/transactions/{playerId}`.
- Price preview flow uses `/api/economy/price-preview`.
- Trade execution flow uses `/api/market/trade`.

## Parity Checks

- [x] Shared trading module service exists in `GalacticTrader.ClientSdk.Trading`.
- [x] Listings + transactions aggregate into a Unity-ready module state.
- [x] Spread and estimated fee summaries are derived for listing cards/rows.
- [x] Preview summary includes spread, spread percent, and estimated fee amount.
- [x] Execute trade path maps backend failures to user-friendly failure states.
- [x] Unity module controller scaffold can refresh, preview, and execute actions.
- [x] Regression tests cover load, preview, success, and insufficient-credit failures.

## Remaining UI Work

- Unity scene/prefab implementation for visual listing cards, preview panel, and action controls.
- Input flow polish for ship selection, quantity controls, and optimistic action feedback.
- UX tuning against action-first standards from `#277`.
