# Reaper (RPR)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Slice | 24373 | ✅ | Soul +10 |
| Waxing Slice | 24374 | ✅ | combo, Soul +10 |
| Shadow of Death | 24378 | ⚠️ | Soul +10 unconditional; sheet grants it only on target KO → should be 0 on a normal cast |
| Harpe | 24386 | ✅ | Soul +10; consumes Enhanced Harpe |
| Hell's Ingress | 24401 | ✅ | Enhanced Harpe + Threshold |
| Hell's Egress | 24402 | ✅ | Enhanced Harpe + Threshold |
| Spinning Scythe | 24376 | ✅ | Soul +10 |
| Infernal Slice | 24375 | ✅ | combo, Soul +10 |
| Whorl of Death | 24379 | ⚠️ | same KO-gated Soul as Shadow of Death |
| Arcane Crest | 24404 | ➖ | barrier/heal, gates nothing |
| Nightmare Scythe | 24377 | ✅ | combo, Soul +10 |
| Blood Stalk | 24389 | ✅ | Soul Reaver; −50 Soul (CostGauges) |
| Grim Swathe | 24392 | ✅ | Soul Reaver; −50 Soul |
| Soul Slice | 24380 | ✅ | Soul +50 |
| Soul Scythe | 24381 | ✅ | Soul +50 |
| Gibbet | 24382 | ✅ | Shroud +10, +Enhanced Gallows; consumes Soul Reaver + Enhanced Gibbet |
| Gallows | 24383 | ✅ | Shroud +10, +Enhanced Gibbet; consumes Soul Reaver + Enhanced Gallows |
| Guillotine | 24384 | ✅ | Shroud +10; consumes Soul Reaver |
| Unveiled Gibbet | 24390 | ✅ | Soul Reaver; −50 Soul (Blood Stalk swap) |
| Unveiled Gallows | 24391 | ✅ | Soul Reaver; −50 Soul |
| Arcane Circle | 24405 | ⚠️ | missing Circle of Sacrifice self-grant + Immortal Sacrifice generation |
| Regress | 24403 | ✅ | consumes Threshold |
| Gluttony | 24393 | ✅ | Executioner (2 stk); −50 Soul |
| Enshroud | 24394 | ✅ | Enshrouded + Oblatio; +5 Lemure Shroud; −50 Shroud |
| Void Reaping | 24395 | ✅ | Enhanced Cross Reaping; Void Shroud +1; −1 Lemure |
| Cross Reaping | 24396 | ✅ | Enhanced Void Reaping; Void Shroud +1; −1 Lemure |
| Grim Reaping | 24397 | ✅ | Void Shroud +1; −1 Lemure (no Enhanced status) |
| Soulsow | 24387 | ✅ | no-expiry status; consumed by Harvest Moon |
| Harvest Moon | 24388 | ✅ | Soul +10; consumes Soulsow |
| Lemure's Slice | 24399 | ✅ | −2 Void Shroud (CostGauges) |
| Lemure's Scythe | 24400 | ✅ | −2 Void Shroud (CostGauges) |
| Plentiful Harvest | 24385 | ⚠️ | Ideal Host + Perfectio Occulta correct, but native gate (Immortal Sacrifice) never produced solo → unreachable in-sim |
| Communio | 24398 | ✅ | Perfectio Parata; clears Enshrouded + Perfectio Occulta |
| Sacrificium | 36969 | ✅ | consumes Oblatio |
| Executioner's Gibbet | 36970 | ✅ | Shroud +10, +Enhanced Gallows; −1 Executioner |
| Executioner's Gallows | 36971 | ✅ | Shroud +10, +Enhanced Gibbet; −1 Executioner |
| Executioner's Guillotine | 36972 | ✅ | Shroud +10; −1 Executioner |
| Perfectio | 36973 | ✅ | consumes Perfectio Parata |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Void Soul | ✅ | Void/Cross/Grim Reaping each grant +1 Void Shroud. |
| Enhanced Arcane Circle | ⚠️ | Immortal Sacrifice generation (Plentiful Harvest gate) not produced solo |

## Role actions
Second Wind, Leg Sweep, Bloodbath, Feint, Arm's Length, True North — all ➖.

## Not simulated
- **Shadow of Death / Whorl of Death over-generate Soul.** Both grant Soul +10 unconditionally; the sheet grants it only if the target is KO'd before Death's Design expires (0 on a normal cast).
- **Plentiful Harvest is unreachable solo.** Arcane Circle grants no Circle of Sacrifice to self and nothing produces Immortal Sacrifice, so the native button-swap gate is a party mechanic that is never met; Plentiful Harvest's own statuses are correct but it can't appear in-sim.
