# Gunbreaker (GNB)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Keen Edge | 16137 | ✅ | Resets Gnashing route step; linear combo native. |
| No Mercy | 16138 | ✅ | Dmg buff (1831) + Ready to Break (3886). |
| Brutal Shell | 16139 | ✅ | Resets Gnashing route step; barrier is mitigation. |
| Camouflage | 16140 | ➖ | Mitigation. |
| Demon Slice | 16141 | ✅ | Resets Gnashing route step. |
| Royal Guard | 16142 | ➖ | Tank stance. |
| Lightning Shot | 16143 | ➖ | |
| Danger Zone | 16144 | ➖ | |
| Solid Barrel | 16145 | ✅ | +1 cartridge (combo finish). |
| Burst Strike | 16162 | ✅ | Spends 1 cartridge; Ready to Blast (2686). |
| Nebula | 16148 | ➖ | Mitigation. |
| Demon Slaughter | 16149 | ✅ | +1 cartridge (AoE combo finish). |
| Aurora | 16151 | ➖ | Heal. |
| Superbolide | 16152 | ➖ | Invuln; mitigation. |
| Sonic Break | 16153 | ✅ | Consumes Ready to Break (3886). |
| Trajectory | 36934 | ➖ | Gap closer. |
| Gnashing Fang | 16146 | ✅ | Spends 1 cartridge; route step +1; Ready to Rip (1842). |
| Savage Claw | 16147 | ✅ | Route step +1; Ready to Tear (1843). |
| Wicked Talon | 16150 | ✅ | Ends route; Ready to Gouge (1844). |
| Bow Shock | 16159 | ➖ | DoT. |
| Heart of Light | 16160 | ➖ | Party mitigation. |
| Heart of Stone | 16161 | ➖ | Mitigation. |
| Continuation | 16155 | ➖ | Native swap to the Ready-to consumer; the driving statuses are set on their source actions. |
| Jugular Rip | 16156 | ✅ | Consumes Ready to Rip (1842). |
| Abdomen Tear | 16157 | ✅ | Consumes Ready to Tear (1843). |
| Eye Gouge | 16158 | ✅ | Consumes Ready to Gouge (1844). |
| Fated Circle | 16163 | ✅ | Spends 1 cartridge; Ready to Raze (3839). |
| Bloodfest | 16164 | ⚠️ | +3 cartridges + Ready to Reign (3840); cap→6 for 30s not modelled (Max fixed 3). |
| Blasting Zone | 16165 | ➖ | |
| Heart of Corundum | 25758 | ➖ | Mitigation/heal. |
| Hypervelocity | 25759 | ✅ | Consumes Ready to Blast (2686). |
| Double Down | 25760 | ✅ | Spends 2 cartridges (CostGauges). |
| Great Nebula | 36935 | ➖ | Mitigation. |
| Fated Brand | 36936 | ✅ | Consumes Ready to Raze (3839). |
| Reign of Beasts | 36937 | ✅ | Consumes Ready to Reign (3840); resets Gnashing route step. |
| Noble Blood | 36938 | ✅ | Resets Gnashing route step; Reign combo native. |
| Lion Heart | 36939 | ✅ | Resets Gnashing route step; Reign combo native. |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Cartridge Charge II | ⚠️ | Baseline cartridge cap 3 modelled; Bloodfest's temporary →6 window is not. |

## Role actions
Rampart, Low Blow, Provoke, Interject, Reprisal, Arm's Length, Shirk — all ➖.

## Not simulated
- **Bloodfest cartridge cap→6.** `GnbCartridge.Max` is fixed at 3; the 30s window that raises the cap to 6 isn't modelled. Bloodfest grants exactly +3, so only banking above 3 inside the window is lost.
- **Gnashing route timer ring.** `GnbComboStep` auto-resets after 30s (TimedGauges) but never writes `GunbreakerGauge.MaxTimerDuration`, so the gauge's countdown ring doesn't animate; the step reset itself works.
- **Gnashing route reset coverage.** Burst Strike, Double Down and Sonic Break don't reset `GnbComboStep`, so weaving one mid-route would leave the button swap advanced — unreachable in a normal back-to-back 3-GCD route.
