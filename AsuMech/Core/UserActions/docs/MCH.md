# Machinist (MCH)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Split Shot → Heated Split Shot | 7411 | ✅ | Heat +5. |
| Slug Shot → Heated Slug Shot | 7412 | ✅ | Heat +5 (combo). |
| Hot Shot → Air Anchor | 16500 | ✅ | Battery +20. |
| Reassemble | 2876 | ✅ | Reassembled (851), 5s; cleared by any WS. |
| Gauss Round → Double Check | 36979 | ➖ | |
| Spread Shot → Scattergun | 25786 | ✅ | Heat +10. |
| Clean Shot → Heated Clean Shot | 7413 | ✅ | Heat +5, Battery +10 (combo). |
| Hypercharge | 17209 | ⚠️ | Overheated (2688) ×5 correct, but always drains 50 Heat even when Hypercharged makes it free. |
| Heat Blast → Blazing Shot | 36978 | ✅ | Consumes Overheated. |
| Rook Autoturret → Automaton Queen | 16501 | ➖ | Battery −50 handled; pet not modelled. |
| Rook Overdrive → Queen Overdrive | 16502 | ➖ | |
| Wildfire | 2878 | ✅ | Grants Wildfire (1946, 10s), driving the Detonator swap. |
| Detonator | 16766 | ✅ | Consumes Wildfire (1946). |
| Ricochet → Checkmate | 36980 | ➖ | |
| Auto Crossbow | 16497 | ✅ | Consumes Overheated. |
| Tactician | 16889 | ➖ | |
| Drill | 16498 | ➖ | |
| Dismantle | 2887 | ➖ | |
| Barrel Stabilizer | 7414 | ✅ | Hypercharged (3864) + Full Metal Machinist (3866), 30s each. |
| Blazing Shot | 36978 | ✅ | Consumes Overheated. |
| Flamethrower | 7418 | ❌ | Channel self-status (1205) not applied. |
| Bioblaster | 16499 | ➖ | |
| Air Anchor | 16500 | ✅ | Battery +20. |
| Automaton Queen | 16501 | ➖ | Battery −50 handled; pet not modelled. |
| Queen Overdrive | 16502 | ➖ | |
| Pile Bunker / Crowned Collider / Arm Punch / Roller Dash | — | ➖ | Pet actions, not hotbarable. |
| Scattergun | 25786 | ✅ | Heat +10. |
| Chain Saw | 25788 | ✅ | Battery +20 + Excavator Ready (3865); consumed by Excavator. |
| Double Check | 36979 | ➖ | |
| Checkmate | 36980 | ➖ | |
| Excavator | 36981 | ✅ | Battery +20; consumes Excavator Ready. |
| Full Metal Field | 36982 | ⚠️ | Consumes Full Metal Machinist correctly, but any-WS clear also wrongly consumes Reassembled. |

## Traits
All potency or native button-swaps → ➖.

## Role actions
Leg Graze, Second Wind, Foot Graze, Peloton, Head Graze, Arm's Length — all ➖.

## Not simulated
- **Flamethrower channel status (1205)** not applied — cosmetic channel buff icon.
- **Hypercharge over-drains Heat.** `ApplyCost` subtracts 50 Heat every cast; when Hypercharged (3864) is active the real cost is 0.
- **Full Metal Field clears Reassembled.** `StatusClearedOnAction[851] = AnyWeaponskill()` matches FMF (36982), but FMF is not affected by Reassemble and shouldn't consume it.
- **Automaton Queen / Rook pet** not modelled (out of scope — damage entity). Player-side Battery spend via `CostGauges[62]` is handled; the pet, its `SummonTimeRemaining` timer, and `LastSummonBatteryPower` scaling are not.
