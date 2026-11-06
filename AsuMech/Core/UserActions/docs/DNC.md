# Dancer (DNC)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Cascade | 15989 | ✅ | 50% Silken Symmetry (2693); +5 Esprit. |
| Fountain | 15990 | ✅ | Combo bonus: 50% Silken Flow (2694); +5 Esprit. |
| Windmill | 15993 | ✅ | 50% Silken Symmetry (2693); +5 Esprit. |
| Standard Step | 15997 | ✅ | Standard Step (1818, 15s); seeds 2-step dance. |
| Standard Finish | 16003 | ✅ | Standard Finish (1821, 60s) + Last Dance Ready (3867, 30s); clears dance. |
| Reverse Cascade | 15991 | ✅ | 50% feather + Esprit; consumes Silken/Flourishing Symmetry. |
| Bladeshower | 15994 | ✅ | Combo bonus: 50% Silken Flow (2694); +5 Esprit. |
| Fan Dance | 16007 | ✅ | 50% Threefold Fan Dance (1820); −1 feather (native). |
| Rising Windmill | 15995 | ✅ | 50% feather + Esprit; consumes Symmetry. |
| Fountainfall | 15992 | ✅ | 50% feather + Esprit; consumes Flow. |
| Bloodshower | 15996 | ✅ | 50% feather + Esprit; consumes Flow. |
| Fan Dance II | 16008 | ✅ | 50% Threefold Fan Dance (1820); −1 feather. |
| En Avant | 16010 | ➖ | Native dash/charges. |
| Curing Waltz | 16015 | ➖ | Heal. |
| Shield Samba | 16012 | ➖ | Mitigation + self-icon; no rotational gate. |
| Closed Position | 16006 | ➖ | Partnership utility; no rotational gate. |
| Ending | 18073 | ➖ | Ends partnership. |
| Devilment | 16011 | ✅ | Devilment (1825, 20s) + Flourishing Starfall (2700, 20s). |
| Fan Dance III | 16009 | ✅ | Consumes Threefold Fan Dance (1820). |
| Technical Step | 15998 | ✅ | Technical Step (1819, 15s); seeds 4-step dance. |
| Technical Finish | 16004 | ✅ | Technical Finish (1822, 20s) + Flourishing Finish (2698, 30s) + Dance of the Dawn Ready (3869, 30s); clears dance. |
| Flourish | 16013 | ✅ | Flourishing Symmetry (3017) + Flow (3018) + Threefold (1820) + Fourfold Fan Dance (2699) + Finishing Move Ready (3868), all 30s. |
| Saber Dance | 16005 | ✅ | Spends 50 Esprit. |
| Improvisation | 16014 | ❌ | Rising Rhythm (2696) self-status (stacks every 3s) not granted. |
| Improvised Finish | 25789 | ➖ | Barrier (damage-model); Improvisation pair unmodelled. |
| Tillana | 25790 | ✅ | +50 Esprit; consumes Flourishing Finish (2698). |
| Fan Dance IV | 25791 | ✅ | Consumes Fourfold Fan Dance (2699). |
| Starfall Dance | 25792 | ✅ | Consumes Flourishing Starfall (2700). |
| Last Dance | 36983 | ✅ | Consumes Last Dance Ready (3867). |
| Finishing Move | 36984 | ✅ | Standard Finish (1821, 60s) + Last Dance Ready (3867, 30s); consumes Finishing Move Ready (3868). |
| Dance of the Dawn | 36985 | ✅ | Spends 50 Esprit; consumes Dance of the Dawn Ready (3869). |
| Emboite | 15999 | ✅ | Advances dance StepIndex. |
| Entrechat | 16000 | ✅ | Advances dance StepIndex. |
| Jete | 16001 | ✅ | Advances dance StepIndex. |
| Pirouette | 16002 | ✅ | Advances dance StepIndex. |

## Traits
All potency, native swaps, or handled via the action rows above → nothing to flag. (Esprit gen is +5 flat, not +10 at the Enhanced Esprit trait.)

## Role actions
Leg Graze, Second Wind, Foot Graze, Peloton, Head Graze, Arm's Length — all ➖.

## Not simulated
- **Improvisation (16014) → Rising Rhythm (2696).** The stacking self-status (a stack every 3s, max 4) and the Improvisation/Improvised Finish pair are unmodelled.
- **Dance-step HUD write unconfirmed.** `DancerStateHandler` seeds and advances the dance-step sequence in the gauge, but whether the `DanceSteps[]` writes reach the on-screen Step Gauge display is unverified in-game.
