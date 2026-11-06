# Astrologian (AST)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Malefic | 3596 | ➖ | |
| Benefic | 3594 | ✅ | 15% chance to grant Enhanced Benefic II (815). |
| Combust | 3599 | ➖ | |
| Lightspeed | 3606 | ✅ | |
| Helios | 3600 | ➖ | |
| Ascend | 3603 | ➖ | |
| Essential Dignity | 3614 | ➖ | |
| Benefic II | 3610 | ✅ | Consumes Enhanced Benefic II (815). |
| Astral Draw | 37017 | ✅ | Packs Balance/Arrow/Spire+Lord, CurrentDraw→Umbral. |
| Umbral Draw | 37018 | ✅ | Packs Spear/Bole/Ewer+Lady, CurrentDraw→Astral. |
| Play I | 37019 | ✅ | Clears card slot 0 (base + swapped 37023/37026). |
| Play II | 37020 | ✅ | Clears card slot 1 (base + swapped 37024/37027). |
| Play III | 37021 | ✅ | Clears card slot 2 (base + swapped 37025/37028). |
| Aspected Benefic | 3595 | ➖ | |
| Aspected Helios | 3601 | ➖ | |
| Gravity | 3615 | ➖ | |
| Combust II | 3608 | ➖ | |
| Synastry | 3612 | ❌ | Self Synastry (845) icon not granted; no gate. |
| Divination | 16552 | ✅ | +Divining (3893). |
| Malefic II | 3598 | ➖ | |
| Collective Unconscious | 3613 | ❌ | Wheel of Fortune (956/1206) self-buff icons not granted; no gate. |
| Celestial Opposition | 16553 | ➖ | |
| Earthly Star | 7439 | ✅ | Grants Earthly Dominance (1224), enabling Stellar Detonation. |
| Stellar Detonation | 8324 | ✅ | Consumes Earthly Dominance (1224). |
| Malefic III | 7442 | ➖ | |
| Minor Arcana | 37022 | ✅ | Clears arcana slot (base + swapped Lord/Lady of Crowns 7444/7445). |
| Combust III | 16554 | ➖ | |
| Malefic IV | 16555 | ➖ | |
| Celestial Intersection | 16556 | ➖ | |
| Horoscope | 16557 | ❌ | Self Horoscope (1890/1891) + re-press behaviour not modelled. |
| Neutral Sect | 16559 | ✅ | Grants Suntouched (3895), enabling Sun Sign. |
| Fall Malefic | 25871 | ➖ | |
| Gravity II | 25872 | ➖ | |
| Exaltation | 25873 | ➖ | |
| Macrocosmos | 25874 | ✅ | Grants Macrocosmos (2718), enabling Microcosmos. |
| Microcosmos | 25875 | ✅ | Consumes Macrocosmos (2718). |
| Oracle | 37029 | ✅ | Consumes Divining (3893). |
| Helios Conjunction | 37030 | ➖ | |
| Sun Sign | 37031 | ✅ | Consumes Suntouched (3895). |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Enhanced Benefic | ✅ | Benefic has a 15% chance to grant Enhanced Benefic II (815). |
| Enhanced Neutral Sect | ✅ | Neutral Sect grants Suntouched (3895). |

## Role actions
Repose, Esuna, Rescue, Surecast, Lucid Dreaming — ➖. Swiftcast ✅ (167 → next spell instant).

## Not simulated
- **Earthly Star post-10s stage unverified** — Earthly Star grants Earthly Dominance (1224) to gate Stellar Detonation, but the Giant Dominance (1248) transition after 10s and the exact gating mechanic aren't verified in-game.
- **Card deck encoding unconfirmed in-game** — the handler packs `AstrologianGauge.Cards` + `CurrentDraw`, but the exact card set each draw grants and the `CurrentDraw` direction are not verified against the live game.
- **Minor self-buff icons** (no follow-up gate): Synastry (845), Collective Unconscious Wheel of Fortune (956/1206), Horoscope (1890/1891).
