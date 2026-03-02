# Galactic Trader -- User Interface Codex

Version: 1.0\
Scope: Complete UI System Definition\
Audience: Product, UX, Frontend Engineering, Systems Design

------------------------------------------------------------------------

# 1. UI Philosophy

## 1.1 Core Design Principles

-   Strategy-first presentation (economic intelligence \> combat
    spectacle)
-   Data-dense but readable
-   Modular dashboard architecture
-   Clear separation between:
    -   Tactical (battles, routes)
    -   Economic (trade, finance, reputation)
    -   Strategic (alliances, territory, services)
-   Route-based navigation rather than open-world manual piloting
-   Low friction, high information clarity

## 1.2 Visual Identity

Theme: Industrial Sci‑Fi Strategic Console\
Mood: Corporate galactic infrastructure\
Primary Tone: Dark UI with high-contrast data overlays\
Accent Colors: - Economic: Cyan - Combat: Red - Reputation: Gold -
Territory: Purple - Services/NPC: Teal

------------------------------------------------------------------------

# 2. Core Application Layout

## 2.1 Global Layout Structure

  --------------------------------------------------
  Top Status Bar

  Left Nav \| Main View Area \| Right Panel Sidebar
  \| (Context Screen) \| Info/Details

  Bottom Log / Event Feed
  --------------------------------------------------

------------------------------------------------------------------------

# 3. Top Status Bar

Displays real-time strategic metrics:

-   Player Name
-   Reputation Score
-   Financial Net Worth
-   Liquid Credits
-   Fleet Strength Rating
-   Protection Status
-   Active Routes
-   Alerts
-   Global Economic Index

Hover Tooltips: - Breakdown of each metric - Historical trend (7-day
graph mini-sparkline)

------------------------------------------------------------------------

# 4. Left Navigation Sidebar

Primary Navigation Modules:

1.  Dashboard
2.  Trade
3.  Routes
4.  Fleet
5.  Battles
6.  Services (NPC Services)
7.  Reputation
8.  Territory
9.  Market Intelligence
10. Analytics
11. Settings

Navigation Behavior: - Context-aware submenus - Breadcrumb tracking -
Hotkey support

------------------------------------------------------------------------

# 5. Dashboard Screen

Purpose: Strategic Overview

Sections:

## 5.1 Wealth Overview

-   Net worth
-   Cash flow graph
-   Asset allocation pie chart

## 5.2 Fleet Overview

-   Ships active
-   Protection level
-   Risk exposure

## 5.3 Reputation Summary

-   Faction standing
-   Trade reliability score
-   Public influence index

## 5.4 Active Routes Summary

-   Revenue per route
-   Risk rating
-   Interference probability

## 5.5 Global Metrics

-   Total Users Signed Up
-   Active Players (24h)
-   Avg Battles per Hour
-   Economic Stability Index
-   Top Reputation Player
-   Top Financial Player

------------------------------------------------------------------------

# 6. Trade Screen

Layout:

Left Panel: - Commodity list - Market filters - Price movement
indicators

Center: - Buy/Sell interface - Quantity sliders - Margin preview -
Tariff calculation - Smuggling risk indicator

Right Panel: - Market heatmap - Supply/demand curve - NPC competitor
presence

------------------------------------------------------------------------

# 7. Route Planning Screen

Core Concept: Route-based autopilot travel

## 7.1 Map View

-   Node-based star system network
-   Color-coded route risk
-   Economic density overlay
-   Pirate presence probability

## 7.2 Route Builder

-   Origin
-   Destination
-   Waypoints
-   Autopilot mode:
    -   Safe Route
    -   Balanced Route
    -   High Profit Route
    -   Smuggler Route

## 7.3 Risk Simulation Panel

-   Probability of interception
-   Expected revenue
-   Expected loss
-   Protection cost estimate

------------------------------------------------------------------------

# 8. Fleet Screen

Ship List View: - Ship Name - Cargo Capacity - Defense Rating -
Insurance Status - Assigned Route

Ship Detail View: - Upgrade modules - Crew skill weighting - Economic
efficiency score - Route performance history

------------------------------------------------------------------------

# 9. Battle Screen

Combat is stat-driven, not manual.

Displayed Metrics: - X Rating vs Y Rating - Environmental modifiers -
Protection modifiers - Reputation penalties - Economic impact projection

Battle Outcome Screen: - Resource change - Reputation change - Damage
report - Insurance payout

------------------------------------------------------------------------

# 10. Services (NPC Players)

NPC Services behave like weighted AI economic agents.

Types: - Corporate Protector (economic focus) - Smuggler Network
(risk-weighted) - Trade Conglomerate - Pirate Syndicate - Reputation
Broker

UI Shows: - Strategy bias distribution - Aggression index - Wealth
accumulation model - Public standing - Interaction contracts

------------------------------------------------------------------------

# 11. Reputation Screen

Components: - Faction standing matrix - Influence zones - Sanctions or
bonuses - Leaderboard (Top Reputation)

Reputation Impacts: - Trade margins - Protection costs - Smuggling
success rate - Alliance access

------------------------------------------------------------------------

# 12. Territory Screen

Displays: - Controlled systems - Protection zones - Economic output per
system - Conflict heatmap

Interactive: - Assign protection fleets - Adjust taxation - Offer trade
incentives

------------------------------------------------------------------------

# 13. Market Intelligence Screen

Advanced analytics:

-   Price volatility index
-   Regional commodity heatmaps
-   Trade flow diagrams
-   Top traders
-   Emerging smuggling corridors

------------------------------------------------------------------------

# 14. Analytics Screen

Personal & Global Metrics:

-   Revenue per hour
-   ROI per ship
-   Risk-adjusted return
-   Battle-to-profit ratio
-   Market share %
-   System influence %

------------------------------------------------------------------------

# 15. Event Feed

Bottom log area shows:

-   Trade completions
-   Interceptions
-   Reputation changes
-   Market shocks
-   Service contract events
-   Territory conflicts

Filterable and exportable.

------------------------------------------------------------------------

# 16. UX Mechanics

-   Real-time updates via websocket
-   No full page reloads
-   Progressive disclosure for deep stats
-   Keyboard shortcuts for pro players
-   Data export (CSV)

------------------------------------------------------------------------

# 17. UI Technical Stack Recommendation

Frontend: - React or Svelte - WebGL map rendering - D3.js for charts

State Management: - Redux or Zustand

Realtime: - WebSocket or gRPC stream

------------------------------------------------------------------------

# 18. UI Scaling Strategy

Phase 1: Core dashboard & trade\
Phase 2: Advanced analytics\
Phase 3: Territory & faction overlays\
Phase 4: AI NPC behavioral visualizers

------------------------------------------------------------------------

End of UI Codex
