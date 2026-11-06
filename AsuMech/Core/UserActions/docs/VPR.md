# Viper (VPR)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Steel Fangs | 34606 | ✅ | Honed Reavers; clears Honed Steel |
| Hunter's Sting | 34608 | ✅ | Hunter's Instinct |
| Reaving Fangs | 34607 | ✅ | Honed Steel; clears Honed Reavers |
| Writhing Snap | 34632 | ➖ | Ranged filler |
| Swiftskin's Sting | 34609 | ✅ | Swiftscaled |
| Steel Maw | 34614 | ✅ | Honed Reavers (AoE) |
| Flanksting Strike | 34610 | ✅ | +10 SO; grants Hindstung, consumes Flankstung |
| Flanksbane Fang | 34611 | ✅ | +10 SO; grants Hindsbane, consumes Flanksbane |
| Hindsting Strike | 34612 | ✅ | +10 SO; grants Flanksbane, consumes Hindstung |
| Hindsbane Fang | 34613 | ✅ | +10 SO; grants Flankstung, consumes Hindsbane |
| Reaving Maw | 34615 | ✅ | Honed Steel (AoE) |
| Slither | 34646 | ➖ | Native gap-closer/charges |
| Hunter's Bite | 34616 | ✅ | Hunter's Instinct (AoE) |
| Swiftskin's Bite | 34617 | ✅ | Swiftscaled (AoE) |
| Jagged Maw | 34618 | ✅ | +10 SO; grants Grimskin's, consumes Grimhunter's |
| Bloodied Maw | 34619 | ✅ | +10 SO; grants Grimhunter's, consumes Grimskin's |
| Serpent's Tail | 35920 | ✅ | Swap → Death Rattle / Last Lash / Legacy, driven by SerpentComboState (handler) |
| Death Rattle | 34634 | ➖ | Pure-damage oGCD; combo-state clear handled |
| Last Lash | 34635 | ➖ | Pure-damage AoE oGCD; combo-state clear handled |
| Vicewinder | 34620 | ✅ | +1 Rattling Coil; opens dread route |
| Hunter's Coil | 34621 | ✅ | +5 SO; Hunter's Instinct; Hunter's Venom |
| Swiftskin's Coil | 34622 | ✅ | +5 SO; Swiftscaled; Swiftskin's Venom |
| Vicepit | 34623 | ✅ | +1 Rattling Coil (AoE) |
| Hunter's Den | 34624 | ✅ | +5 SO; Hunter's Instinct; Fellhunter's Venom |
| Swiftskin's Den | 34625 | ✅ | +5 SO; Swiftscaled; Fellskin's Venom |
| Twinfang | 35921 | ✅ | Swap → Bite/Thresh, driven by DreadCombo (handler); Uncoiled route via Poised status |
| Twinblood | 35922 | ✅ | Swap → Bite/Thresh, driven by DreadCombo (handler) |
| Twinfang Bite | 34636 | ⚠️ | Consumes Hunter's Venom; reciprocal Swiftskin's Venom grant not modelled |
| Twinblood Bite | 34637 | ⚠️ | Consumes Swiftskin's Venom; reciprocal Hunter's Venom grant not modelled |
| Twinfang Thresh | 34638 | ⚠️ | Consumes Fellhunter's Venom; reciprocal Fellskin's Venom grant not modelled |
| Twinblood Thresh | 34639 | ⚠️ | Consumes Fellskin's Venom; reciprocal Fellhunter's Venom grant not modelled |
| Uncoiled Fury | 34633 | ✅ | Poised for Twinfang; −1 Rattling Coil (cost 87) |
| Serpent's Ire | 34647 | ✅ | +1 Rattling Coil; Ready to Reawaken |
| Reawaken | 34626 | ⚠️ | +5 Anguine Tribute; Reawakened; −50 SO. Ready-to-Reawaken free cast still charges 50 SO |
| First Generation | 34627 | ✅ | Arms First Legacy (handler); −1 Anguine Tribute (cost 89) |
| Second Generation | 34628 | ✅ | Arms Second Legacy; −1 Anguine Tribute (cost 89) |
| Third Generation | 34629 | ✅ | Arms Third Legacy; −1 Anguine Tribute (cost 89) |
| Fourth Generation | 34630 | ✅ | Arms Fourth Legacy; −1 Anguine Tribute (cost 89) |
| Uncoiled Twinfang | 34644 | ✅ | Poised for Twinblood; consumes Poised for Twinfang |
| Uncoiled Twinblood | 34645 | ✅ | Consumes Poised for Twinblood |
| Ouroboros | 34631 | ✅ | Expires Reawakened; −1 Anguine Tribute (cost 90) |
| First Legacy | 34640 | ➖ | Pure-damage oGCD; combo-state clear handled |
| Second Legacy | 34641 | ➖ | Pure-damage oGCD |
| Third Legacy | 34642 | ➖ | Pure-damage oGCD |
| Fourth Legacy | 34643 | ➖ | Pure-damage oGCD |

## Traits
All potency, native button-swaps, or gauge-max traits already reflected in gauge Max → no ⚠️/❌.

## Role actions
Second Wind, Leg Sweep, Bloodbath, Feint, Arm's Length, True North — all ➖.

## Not simulated
- **Reciprocal venom grants on Twinfang/Twinblood Bite/Thresh (34636–34639) not modelled.** Each consumes its venom, but the combo-conditional grant of the sibling venom (Swiftskin's/Hunter's/Fellskin's/Fellhunter's) isn't written.
- **Ready to Reawaken (3671) free cast not honoured.** Reawaken still spends 50 Serpent Offering via `ApplyCost` even when the status should waive the cost.
- **Combo-state swap packing unconfirmed in-game.** `ViperStateHandler` writes `SerpentComboState` (0x10, stored as `enum << 2`) and `DreadCombo` (0x0B) to drive the Serpent's Tail and Twinfang/Twinblood swaps; the exact bit layout (low 2 bits of SerpentComboState assumed unused) needs in-game verification.
