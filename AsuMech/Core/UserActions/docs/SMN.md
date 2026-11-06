# Summoner (SMN)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Ruin / Ruin II / Ruin III | 163/172/3579 | ➖ | |
| Physick | 16230 | ➖ | |
| Summon Carbuncle | 25798 | ➖ | server-side pet |
| Radiant Aegis | 25799 | ➖ | self shield |
| Aethercharge | 25800 | ❌ | grants Ruby/Topaz/Emerald Arcanum (summon-select gauge bits) — unmodelled; native-upgrades to Summon Bahamut at cap |
| Gemshine | 25883 | ✅ | spends 1 Attunement; native swap to aspected Rite keys on the gauge we write |
| Precious Brilliance | 25884 | ✅ | AoE attunement spender |
| Outburst | 16511 | ➖ | AoE nuke |
| Ruin IV | 7426 | ✅ | consumes Further Ruin (2701) |
| Energy Drain | 16508 | ✅ | +2 Aetherflow + Further Ruin (2701, 60s) |
| Energy Siphon | 16510 | ✅ | +2 Aetherflow + Further Ruin (2701, 60s) |
| Fester | 181 | ✅ | spends 1 Aetherflow |
| Painflare | 3578 | ✅ | spends 1 Aetherflow |
| Necrotize | 36990 | ✅ | spends 1 Aetherflow |
| Summon Ifrit / Ifrit II | 25805/25838 | ✅ | Fire Attunement (2) + Ifrit's Favor (2724); favor granted on the II id |
| Summon Titan / Titan II | 25806/25839 | ✅ | Earth Attunement (4) |
| Summon Garuda / Garuda II | 25807/25840 | ✅ | Wind Attunement (4) + Garuda's Favor (2725); favor granted on the II id |
| Ruby/Topaz/Emerald Ruin (I–III) | 25808–25813, 25817–25819 | ✅ | sub-72 aspected Gemshine forms; spends 1 Attunement |
| Ruby/Topaz/Emerald Outburst | 25814/25815/25816 | ✅ | sub-74 aspected AoE forms; spends 1 Attunement |
| Ruby/Topaz/Emerald Rite | 25823/25824/25825 | ✅ | spends 1 Attunement; Topaz Rite → Titan's Favor (2853) |
| Ruby/Topaz/Emerald Disaster | 25827/25828/25829 | ✅ | spends 1 Attunement |
| Ruby/Topaz/Emerald Catastrophe | 25832/25833/25834 | ✅ | spends 1 Attunement; Topaz Catastrophe → Titan's Favor (2853) |
| Dreadwyrm Trance | 3581 | ✅ | Dreadwyrm Trance (3228, 15s) + summon-timer gauge |
| Astral Impulse / Astral Flare | 25820/25821 | ➖ | in-trance nuke; gate we set |
| Deathflare | 3582 | ➖ | in-trance; gate we set |
| Astral Flow | 25822 | ✅ | native swap per trance/favor; all gates set |
| Searing Light | 25801 | ✅ | Searing Light (2703, 20s) + Ruby's Glimmer (3873, 30s) |
| Summon Bahamut | 7427 | ✅ | Dreadwyrm Trance (3228) + summon-timer gauge |
| Enkindle Bahamut | 7429 | ➖ | pet damage |
| Summon Phoenix | 25831 | ✅ | Firebird Trance (3229, 15s) + summon-timer gauge |
| Fountain of Fire / Brand of Purgatory | 16514/16515 | ➖ | in-trance nuke; gate we set |
| Rekindle | 25830 | ➖ | heal (Firebird Astral Flow) |
| Enkindle Phoenix | 16516 | ➖ | pet damage |
| Crimson Cyclone | 25835 | ✅ | consumes Ifrit's Favor; grants Crimson Strike Ready (4403) |
| Crimson Strike | 25885 | ✅ | consumes Crimson Strike Ready |
| Mountain Buster | 25836 | ✅ | consumes Titan's Favor |
| Slipstream | 25837 | ✅ | consumes Garuda's Favor |
| Searing Flash | 36991 | ✅ | consumes Ruby's Glimmer |
| Lux Solaris | 36997 | ✅ | consumes Refulgent Lux; heal |
| Summon Solar Bahamut | 36992 | ✅ | Refulgent Lux (3874, 30s) + summon-timer gauge (solar primed bits) |
| Umbral Impulse / Umbral Flare / Sunflare | 36994/36995/36996 | ➖ | in-trance nuke; solar gauge we set |
| Luxwave | 36993 | ➖ | pet-window nuke |
| Enkindle Solar Bahamut | 36998 | ➖ | pet damage |

## Traits
All potency or native button-swaps → ➖.

## Role actions
Addle, Sleep, Lucid Dreaming, Surecast — ➖. Swiftcast ✅ (167 → next spell instant).

## Not simulated
- **Arcanum (Ruby/Topaz/Emerald) unmodelled.** Aethercharge and the four demi-summons (Summon Bahamut / Phoenix / Solar Bahamut / Dreadwyrm Trance) grant the three Arcanum — the `AetherFlags` Ifrit/Titan/Garuda-Ready bits that gate the elemental summon buttons and the gauge's summon-select indicator. Nothing sets them, so summon availability/order isn't reproduced. (Those actions' trance status + timer, their primary effect, do work.)
- **Attunement type codes assumed, not confirmed in-game.** The handler packs Fire/Earth/Wind = 1/2/3 into the Attunement byte; count/spend and the aetherflow-stack + demi-summon primed bits are layout-confirmed against FFXIVClientStructs, but which type value maps to which element (hence which aspect Gemshine/Precious Brilliance display after each elemental summon) is unverified.
