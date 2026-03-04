# GalacticTrader Feature Codex: TradeWars Gold Realtime Mapping

Date: 2026-03-04

## Scope

This codex compares classic TradeWars Gold feature expectations to current GalacticTrader capabilities.

Note: the classic product is generally documented as TradeWars 2002 Gold (not "2000"). This document keeps your requested framing (TradeWars Gold), while using feature definitions from archived v3 documentation and related historical references.

Turn-based mechanics are intentionally excluded from parity requirements. Every mapped feature below is framed for realtime gameplay.

## Realtime adaptation model

- Replace turns/day with realtime action budgets, cooldowns, or timed jobs.
- Replace offline "daily cycle only" events with scheduled background processing and event streams.
- Keep strategic depth, logistics pressure, and social/economic conflict loops.

## Current parity matrix

Status legend:
- Implemented: shipped and usable now.
- Partial: capability exists but not at TradeWars Gold-equivalent depth.
- Missing: no equivalent gameplay system currently shipped.

| TradeWars Gold Feature | Realtime equivalent in GalacticTrader | Status | Current evidence |
|---|---|---|---|
| Sector graph universe and warp lanes | Directed route graph with path planning and active travel sessions | Implemented | `src/API/Program.cs` navigation + autopilot endpoints |
| Port commodity trade loop | Commodity/market transaction APIs with history | Implemented | `src/API/Program.cs` economy + market endpoints |
| Good vs evil alignment progression | Alignment updates and reputation pathways | Partial | `src/API/Program.cs` reputation endpoints |
| Federation-law progression and commissions | Admin/faction legal progression and lawful unlock chain | Missing | No federal commission or lawful unlock system in API |
| Terra colonist source economy | Dedicated colonist sourcing world and colonist logistics | Missing | No Terra-specific or colonist entity flow |
| Genesis torpedo planet creation | Player-triggered planet generation in realtime world | Missing | No planet creation endpoint/model |
| Planet classes and production specialization | Timed per-planet production profiles (ore/organics/equipment/fighters) | Missing | No planet production model |
| Citadel construction and level upgrades | Timed build queue with defense/economy unlock tiers | Missing | No citadel model or service |
| Planetary Combat Control Computer defenses | Automated defense logic tied to citadel progression | Missing | No citadel/planet defense subsystem |
| Planetary Trade Agreement bulk transfer | Contracted port-planet bulk transfer automation | Missing | No bulk transfer/planet-port contract feature |
| Port class matrix (Class 1-9 behavior) | Explicit port taxonomy and differentiated trading semantics | Missing | No class-based port economy model |
| Class 0 + StarDock (Class 9) hub economy | Specialized hub station with shipyard/hardware/bank/police/tavern | Missing | No Stardock domain surface |
| Shipyard with canonical ship classes | Realtime ship catalog with gated progression and role classes | Partial | Fleet templates exist, no TW-style gated shipyard flow |
| Imperial StarShip unlock path | Endgame lawful ship unlock via reputation/legal milestones | Missing | No ISS-style progression path |
| Hardware emporium systems | Purchasable tactical devices/scanners and utility modules | Partial | General modules exist; TW device suite missing |
| Density/holo/planet scanner gameplay loops | Tactical recon devices with richer scan intelligence | Missing | No dedicated scanner hardware mechanics |
| Photon missiles and mine disruptors | Range-limited tactical disruption munitions | Missing | No photon/mine-disruptor equivalents |
| Ether probe scouting | Remote disposable probe with sector-by-sector telemetry | Missing | No probe entity or endpoint loop |
| Marker beacons | Persistent sector beacons with player messages | Missing | No beacon model/endpoint |
| Tractor beam towing | Ship towing with risk and movement constraints | Missing | No tow mechanics in fleet/navigation APIs |
| Transporter pad ship transfer | In-range ship-to-ship player transfer | Missing | No transporter pad system |
| Interdictor control | Sector lock mechanic preventing escape during combat | Missing | No interdictor/warp lock system |
| Fighter deployment modes (off/def/toll) | Configurable fighter AI modes and toll extraction | Missing | No fighter deployment mode system |
| Personal vs corporate fighters | Ownership-scoped autonomous defenses | Missing | No deployed fighter ownership mechanic |
| Limpet/Armid mine warfare | Deployed mine types + triggered/attached states | Missing | No mine warfare subsystem |
| Starport construction and upgrades | Player-funded timed starport builds and upgrades | Missing | No starport build/upgrade flow |
| Starport destruction | Strategic denial by destroying hostile infrastructure | Missing | No port destruction path |
| Corporation creation/governance | Persistent orgs with treasury/assets/ranks/shared defenses | Missing | No corporation model/service |
| Global/subspace/hailing/mail comm semantics | Mixed realtime channels + async mail fallback | Partial | Realtime channels exist; no mail fallback model |
| Tavern social systems (gambling, wall, notices) | Social venue features with economic mini-loops | Missing | No tavern subsystem |
| Banked credit layers (ship/citadel/global) | Multi-ledger banking with protected storage and transfers | Missing | No Stardock/bank ledger system |
| Ferrengi raid behavior | Opportunistic NPC raiders with targeted strike logic | Missing | Generic NPC exists; no Ferrengi raid model |
| Alien trader ecosystem | Distinct non-human trader economy behaviors | Missing | No alien-specific trading behavior surface |
| Federation patrol law enforcement | NPC legal enforcement that reacts to player alignment/crimes | Missing | No Fed patrol/crime enforcement loop |
| Corporate war strategic layer | War declarations/intensity metrics | Implemented | `src/API/Program.cs` strategic corporate-war endpoints |
| Territory dominance and strategic telemetry | Dominance recalculation + dashboard feed | Implemented | `src/API/Program.cs` strategic endpoints + realtime snapshot |

## Gap summary

- Implemented: foundational navigation/trading/combat/reputation/strategic telemetry loops.
- Partial: alignment depth, ship/hardware depth, communication semantics.
- Missing: most classic late-game socio-economic warfare loops (planets/citadels/corps/stardock/tactical devices/fighter-mine warfare).

## Source references used for this codex

- Trade Wars 2002 v3 Documentation Text (TradeWars Museum): https://wiki.classictw.com/index.php/Trade_Wars_2002_v3_Documentation_Text
- TradeWars 2002 Bible v1.1 excerpt (TradeWars Museum): https://wiki.classictw.com/index.php/TradeWars_2002_Bible_v1.1
- TradeWars gameplay/historical context (Sonic.net interview/article): https://archives.sonic.net/tw2002/articles/96-12/1372.html
- TradeWars trademark ownership listing (Justia owner page): https://trademarks.justia.com/owners/pritchett-john-1274373/
- Galactic Trader trademark status page (Justia): https://trademarks.justia.com/852/54/galactic-trader-85254060.html
