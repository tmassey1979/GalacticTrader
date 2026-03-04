# GALACTIC TRADER

## Full Strategic Codex

### Version 2.0 (Current-State Aligned)

## 1. Core Philosophy

Galactic Trader is a real-time, backend-authoritative economic strategy game.

Core pillars:

- Strategy over reflex
- Logistics over raw combat volume
- Economy over isolated kill count
- Reputation and alignment as progression gates
- Server authority over client authority

## 2. Universe Model

The universe is modeled as a directed weighted graph.

Primary sector attributes include:

- Id, name, coordinates
- Security and hazard ratings
- Economic modifiers
- Faction control and stability indicators

Routes encode travel time, fuel cost, baseline risk, and legal context.

## 3. Navigation and Autopilot

Manual piloting is presentation; autopilot state is canonical.

Travel modes include standard, high-burn, stealth, convoy, and escort-oriented profiles.

Encounter scoring model:

`EncounterScore = BaseRouteRisk + SectorHazard + CargoValueFactor + PlayerNotoriety - EscortStrength - FactionProtection - StealthModifier`

## 4. Strategic Combat

Combat is deterministic and tick-based.

Representative attack model:

`EffectiveAttack = (WeaponTier * CrewSkill * EnergyAllocation) + DoctrineBonus + PositioningBonus - TargetShield - CountermeasureReduction`

Damage is resolved across shields, hull, and selected subsystems.

## 5. Economy and Market Dynamics

Representative pricing model:

`Price = BasePrice * DemandMultiplier * RiskPremium * ScarcityModifier`

Price movement is influenced by supply, demand, faction stability, pirate pressure, and player actions.

## 6. Reputation and Alignment

Lawful and dirty paths drive access control and economic multipliers.

Impacted systems include:

- Sector/service access
- Trade terms and scan frequency
- Insurance behavior and pricing

## 7. Ship and Crew Systems

Ship model exposes core tactical/economic stats (hull, shields, cargo, signatures, hardpoints, crew slots).

Crew model tracks role/skill progression and modifies combat/navigation outcomes.

## 8. NPC Agent Model

NPCs are modeled as autonomous actors with archetype-weighted behavior (merchant, pirate, industrialist, etc.).

Decision flow:

1. Goal evaluation
2. Opportunity scan
3. Action selection
4. Route planning
5. Combat/trade execution

## 9. Metrics and Telemetry

Key operational metrics include API latency, combat tick timing, route planning timing, strategic maintenance counters, and economic/state gauges.

Leaderboards track wealth, reputation, combat, and trade dimensions.

## 10. Backend Architecture State

### Current State (Implemented)

- .NET 9 API host with in-process modular service boundaries (`src/API` + `src/Services`).
- EF Core 9 + PostgreSQL relational persistence.
- Redis cache integration.
- Keycloak integration is explicit-config opt-in for credential login.
- Gateway available as separate edge service.

### Target State (Roadmap)

- Optional service extraction for simulation/communication workloads.
- Optional Kubernetes-first production topology.

Roadmap items are future-state and not assumed active unless implemented in code and deployment manifests.

## 11. Communication Systems

Communication stack includes persistent text channels, websocket delivery, and voice signaling/spatial mix workflows.

## 12. Admin and Balance Controls

Admin controls cover tax rates, pirate intensity, liquidity adjustments, and correction events.

Legacy `X-Admin-Key` auth is deprecated and on a dated removal plan; bearer-role auth is the target long-term path.

## 13. Strategic Systems

Implemented strategic domains include:

- Sector volatility cycles
- Corporate wars
- Infrastructure ownership
- Territory dominance
- Insurance policies/claims
- Intelligence networks/reports

## 14. Core Loop

1. Analyze market and risk
2. Select cargo and route strategy
3. Execute movement/trade/combat decisions
4. Respond to dynamic events
5. Grow wealth, influence, and strategic control
