# Monk (MNK)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Bootshine | 53 | ✅ | → Raptor Form (108). |
| True Strike | 54 | ✅ | → Coeurl Form (109). |
| Snap Punch | 56 | ✅ | → Opo-opo Form (107). |
| Steeled Meditation | 36940 | ✅ | +1 Chakra. |
| Steel Peak | 25761 | ✅ | Spends 5 Chakra (CostGauges). |
| Twin Snakes | 61 | ⚠️ | → Coeurl Form (109); Raptor's Fury gauge stack not written. |
| Arm of the Destroyer | 62 | ✅ | → Raptor Form (108). |
| Demolish | 66 | ⚠️ | → Opo-opo Form (107); Coeurl's Fury gauge stacks not written. |
| Rockbreaker | 70 | ✅ | → Opo-opo Form (107). |
| Thunderclap | 25762 | ➖ | Gap closer. |
| Inspirited Meditation | 36941 | ✅ | +1 Chakra. |
| Howling Fist | 25763 | ✅ | Spends 5 Chakra (CostGauges). |
| Mantra | 65 | ➖ | Party heal buff. |
| Four-point Fury | 16473 | ✅ | → Coeurl Form (109). |
| Dragon Kick | 74 | ⚠️ | → Raptor Form (108); Opo-opo's Fury gauge stack not written. |
| Perfect Balance | 69 | ✅ | Status 110 (3 stk) + opens the Beast Chakra build window (handler). |
| Form Shift | 4262 | ✅ | Formless Fist (2513); consumed by any WS. |
| Forbidden Meditation | 36942 | ✅ | +1 Chakra. |
| The Forbidden Chakra | 3547 | ✅ | Spends 5 Chakra (CostGauges). |
| Masterful Blitz | 25764 | ✅ | Formless Fist (2513); handler opens Nadi + clears Beast Chakra. |
| Tornado Kick | 3543 | ➖ | Pre-upgrade of Phantom Rush (native swap). |
| Elixir Field | 3545 | ➖ | Pre-upgrade of Elixir Burst (native swap). |
| Celestial Revolution | 25765 | ✅ | Formless Fist (2513); handler opens Nadi + clears Beast Chakra. |
| Flint Strike | 25882 | ➖ | Pre-upgrade of Rising Phoenix (native swap). |
| Riddle of Earth | 7394 | ✅ | Grants Earth's Rumination (3841), gating Earth's Reply. |
| Earth's Reply | 36944 | ➖ | Heal; consumes Earth's Rumination (3841). |
| Riddle of Fire | 7395 | ✅ | Riddle of Fire (1181) + Fire's Rumination (3843). |
| Brotherhood | 7396 | ✅ | Brotherhood (1185) + Meditative Brotherhood (1182). |
| Riddle of Wind | 25766 | ✅ | Riddle of Wind (2687) + Wind's Rumination (3842). |
| Enlightened Meditation | 36943 | ✅ | +1 Chakra. |
| Enlightenment | 16474 | ✅ | Spends 5 Chakra (CostGauges). |
| Six-sided Star | 16476 | ➖ | Speed-down self-buff gates nothing; Chakra close (cost 99) unmapped, moot. |
| Shadow of the Destroyer | 25767 | ✅ | → Raptor Form (108). |
| Rising Phoenix | 25768 | ✅ | Formless Fist (2513); handler opens Nadi + clears Beast Chakra. |
| Phantom Rush | 25769 | ✅ | Formless Fist (2513); handler opens/consumes Nadi + clears Beast Chakra. |
| Leaping Opo | 36945 | ✅ | → Raptor Form (108). |
| Rising Raptor | 36946 | ✅ | → Coeurl Form (109). |
| Pouncing Coeurl | 36947 | ✅ | → Opo-opo Form (107). |
| Elixir Burst | 36948 | ✅ | Formless Fist (2513); handler opens Nadi + clears Beast Chakra. |
| Wind's Reply | 36949 | ✅ | Consumes Wind's Rumination (3842). |
| Fire's Reply | 36950 | ✅ | Consumes Fire's Rumination (3843); grants Formless Fist (2513). |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Deep Meditation (+ II) | ❌ | Chakra-on-crit is reactive, not simulable (the Meditation actions do generate Chakra). |
| Enhanced Brotherhood | ❌ | Guaranteed Chakra open under Meditative Brotherhood is reactive — not simulated. |

All other traits are potency or native button-swaps → ➖ (including Enhanced Perfect Balance, whose Beast Chakra generation is now handled).

## Role actions
Second Wind, Leg Sweep, Bloodbath, Feint, Arm's Length, True North — all ➖.

## Not simulated
- **Beast Chakra / Nadi / blitz-window encoding unconfirmed.** `MonkStateHandler` opens Beast Chakra during Perfect Balance and opens/consumes the Nadi on the finishers, but the PB-window length, Phantom Rush's both-Nadi consume, and whether unspent Beast Chakra survives timer expiry aren't verified in-game.
- **Fury gauge not written.** Dragon Kick / Twin Snakes / Demolish grant Opo-opo's/Raptor's/Coeurl's Fury and Leaping Opo / Rising Raptor / Pouncing Coeurl consume them; these are gauge stacks (no status) and are never written. Gauge-display / damage-modifier only, gates nothing.
