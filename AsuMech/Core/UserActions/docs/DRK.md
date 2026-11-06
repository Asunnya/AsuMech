# Dark Knight (DRK)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Hard Slash | 3617 | ➖ | Linear combo state handled generically. |
| Syphon Strike | 3623 | ➖ | MP restore not a job gauge. |
| Unleash | 3621 | ➖ | AoE combo starter. |
| Grit | 3629 | ➖ | Tank stance. |
| Release Grit | 32068 | ➖ | |
| Unmend | 3624 | ➖ | |
| Souleater | 3632 | ✅ | +20 Blood (combo). |
| Flood of Darkness | 16466 | ✅ | Darkside (751). |
| Blood Weapon | 3625 | ⚠️ | Sub-Lv68, natively upgraded to Delirium; Blood Weapon status blood-gen not modelled. |
| Shadow Wall | 3636 | ➖ | Mitigation. |
| Stalwart Soul | 16468 | ✅ | +20 Blood (combo). |
| Edge of Darkness | 16467 | ✅ | Darkside (751); consumes Dark Arts. |
| Dark Mind | 3634 | ➖ | Mitigation. |
| Living Dead | 3638 | ➖ | |
| Salted Earth | 3639 | ➖ | |
| Shadowstride | 36926 | ➖ | |
| Abyssal Drain | 3641 | ➖ | |
| Carve and Spit | 3643 | ➖ | |
| Bloodspiller | 7392 | ✅ | Spends 50 Blood (CostGauges). |
| Quietus | 7391 | ✅ | Spends 50 Blood (CostGauges). |
| Dark Missionary | 16471 | ➖ | Party mitigation. |
| Delirium | 7390 | ⚠️ | Delirium (3836) + Blood Weapon (742) set; Blood Weapon blood-gen missing. |
| The Blackest Night | 7393 | ⚠️ | Dark Arts (752) granted on cast (sheet: on shield break); DarkArtsState gauge flag not set. |
| Flood of Shadow | 16469 | ✅ | Darkside (751); consumes Dark Arts. |
| Edge of Shadow | 16470 | ✅ | Darkside (751); consumes Dark Arts. |
| Living Shadow | 16472 | ✅ | Scorn (3837); ShadowTimer HUD not set. |
| Oblation | 25754 | ➖ | Mitigation. |
| Salt and Darkness | 25755 | ➖ | |
| Shadowbringer | 25757 | ➖ | Under-Darkside gate rides Darkside status (set). |
| Shadowed Vigil | 36927 | ➖ | Mitigation. |
| Scarlet Delirium | 36928 | ✅ | Consumes Delirium stack; route step +1 (DrkDeliriumStep). |
| Comeuppance | 36929 | ✅ | Route step 1→2. |
| Torcleaver | 36930 | ✅ | Ends route (step −2). |
| Impalement | 36931 | ✅ | Consumes Delirium stack (AoE swap). |
| Disesteem | 36932 | ✅ | Consumes Scorn (3837). |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Enhanced Blackblood | ❌ | +10 Blood per weaponskill/spell while Blood Weapon (742) is up — never generated in-sim. |

## Role actions
Rampart, Low Blow, Provoke, Interject, Reprisal, Arm's Length, Shirk — all ➖.

## Not simulated
- **Blood Weapon blood generation.** While Blood Weapon (742) is up, each weaponskill/spell should add +10 Blood (Enhanced Blackblood); only the stack is decremented, so ~30 Blood per Delirium window is never generated.
- **The Blackest Night Dark Arts.** Granted on cast rather than on full shield absorption, and only the status is set — the `DarkArtsState` gauge flag (HUD glow) stays dark; the status icon does show.
- **Delirium route encoding.** `DrkDeliriumStep` is written (36928/36929 +1, 36930 −2) but the exact step values the game's action-swap reads for the Comeuppance/Torcleaver advance aren't confirmed in-game.
- **Gauge HUD timers.** Darkside bar and Living Shadow countdown aren't written; the gates that matter (Shadowbringer, Disesteem) ride the statuses, which are set.
