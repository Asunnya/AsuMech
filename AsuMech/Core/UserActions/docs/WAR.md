# Warrior (WAR)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Heavy Swing | 31 | ➖ | Combo starter. |
| Maim | 37 | ✅ | Combo +10 Beast. |
| Berserk | 38 | ➖ | Native upgrade to Inner Release at max level. |
| Overpower | 41 | ➖ | AoE combo starter. |
| Defiance | 48 | ❌ | Tank-stance status (91) icon never applied. |
| Release Defiance | 32066 | ➖ | Cancels stance. |
| Tomahawk | 46 | ➖ | |
| Storm's Path | 42 | ✅ | Combo +20 Beast. |
| Thrill of Battle | 40 | ➖ | Self max-HP buff. |
| Inner Beast → Fell Cleave | 49 / 3549 | ✅ | Spends 50 Beast; consumes 1 Inner Release stk. |
| Vengeance → Damnation | 44 / 36923 | ➖ | Mitigation. |
| Mythril Tempest | 16462 | ✅ | AoE combo: +20 Beast + Surging Tempest. |
| Holmgang | 43 | ➖ | Invuln. |
| Steel Cyclone → Decimate | 51 / 3550 | ✅ | Spends 50 Beast; consumes 1 Inner Release stk. |
| Storm's Eye | 45 | ✅ | Combo +10 Beast + Surging Tempest. |
| Infuriate | 52 | ✅ | +50 Beast + Nascent Chaos; drives Fell Cleave→Inner Chaos / Decimate→Chaotic Cyclone swap. |
| Fell Cleave | 3549 | ✅ | Spends 50 Beast; consumes Inner Release stk. |
| Raw Intuition → Bloodwhetting | 3551 / 25751 | ➖ | Mitigation. |
| Equilibrium | 3552 | ➖ | Self heal/HoT. |
| Decimate | 3550 | ✅ | Spends 50 Beast; consumes Inner Release stk. |
| Onslaught | 7386 | ➖ | Gap-closer, charges native. |
| Upheaval | 7387 | ➖ | |
| Shake It Off | 7388 | ➖ | Party barrier + self HoT. |
| Inner Release | 7389 | ⚠️ | Grants Inner Release (3 stk) + Primal Rend Ready; missing Burgeoning Fury→Wrathful→Primal Wrath chain + Inner Strength icon. |
| Chaotic Cyclone | 16463 | ✅ | Spends 50 Beast; consumes Nascent Chaos. |
| Nascent Flash | 16464 | ➖ | Party heal/mit. |
| Inner Chaos | 16465 | ✅ | Spends 50 Beast; consumes Nascent Chaos. |
| Orogeny | 25752 | ➖ | AoE damage. |
| Primal Rend | 25753 | ✅ | Consumes Primal Rend Ready → grants Primal Ruination Ready. |
| Primal Wrath | 36924 | ❌ | Gated on Wrathful (3901); swap never appears — Burgeoning Fury/Wrathful not modelled. |
| Primal Ruination | 36925 | ✅ | Consumes Primal Ruination Ready. |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Mastering the Beast | ✅ | Mythril Tempest AoE combo grants +20 Beast. |
| Enhanced Inner Release | ❌ | Burgeoning Fury (max 3)→Wrathful→Primal Wrath swap not modelled. |

## Role actions
Rampart, Low Blow, Provoke, Interject, Reprisal, Arm's Length, Shirk — all ➖.

## Not simulated
- **Primal Wrath chain (Lv96+).** Inner Release doesn't grant Burgeoning Fury (3833), so Wrathful (3901) never sets and the Inner Release→Primal Wrath (36924) swap never fires. Needs a conditional per-hit grant on Fell Cleave/Decimate while Inner Release is active — not expressible in the per-cast table.
- **Defiance (48) stance icon (91).** Not applied; no rotational impact.
- **Inner Strength (2663).** Inner Release's CC-immunity icon not applied.
