# Samurai (SAM)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Hakaze | 7477 | ✅ | Pressed as Gyofu; Kenki on 36963. |
| Jinpu | 7478 | ✅ | Combo: +5 Kenki + Fugetsu. |
| Third Eye | 7498 | ➖ | Kenki-on-hit is reactive, not simulable; upgrades to Tengentsu. |
| Enpi | 7486 | ✅ | +10 Kenki; consumes Enhanced Enpi. |
| Shifu | 7479 | ✅ | Combo: +5 Kenki + Fuka. |
| Fuga | 7483 | ✅ | Pressed as Fuko; Kenki on 25780. |
| Gekko | 7481 | ✅ | Combo: +10 Kenki + Getsu. |
| Iaijutsu | 7867 | ✅ | Native swap by Sen count (Sen now written). |
| Higanbana | 7489 | ✅ | Clears Sen; +1 Meditation. |
| Tenka Goken | 7488 | ✅ | Tsubame Ready + Sen clear; +1 Meditation. |
| Midare Setsugekka | 7487 | ✅ | Tsubame Ready + Sen clear; +1 Meditation. |
| Tendo Goken | 36965 | ✅ | Tsubame Ready + Tendo consume + Sen clear; +1 Meditation. |
| Tendo Setsugekka | 36966 | ✅ | Tsubame Ready + Tendo consume + Sen clear; +1 Meditation. |
| Mangetsu | 7484 | ✅ | Combo: +10 Kenki + Fugetsu + Getsu. |
| Kasha | 7482 | ✅ | Combo: +10 Kenki + Ka. |
| Oka | 7485 | ✅ | Combo: +10 Kenki + Fuka + Ka. |
| Yukikaze | 7480 | ✅ | Combo: +15 Kenki + Setsu. |
| Meikyo Shisui | 7499 | ✅ | Meikyo (3 stk) + Tendo; combo-unlock caveat below. |
| Hissatsu: Shinten | 7490 | ✅ | −25 Kenki (native cost). |
| Hissatsu: Gyoten | 7492 | ✅ | −10 Kenki. |
| Hissatsu: Yaten | 7493 | ✅ | −10 Kenki + Enhanced Enpi. |
| Meditate | 7497 | ❌ | Meditation stacks + gradual Kenki both unmodelled. |
| Hissatsu: Kyuten | 7491 | ✅ | −25 Kenki. |
| Hagakure | 7495 | ⚠️ | Clears Sen; +10-Kenki-per-Sen conversion not added. |
| Ikishoten | 16482 | ✅ | +50 Kenki + Ogi Namikiri Ready + Zanshin Ready. |
| Hissatsu: Guren | 7496 | ✅ | −25 Kenki. |
| Hissatsu: Senei | 16481 | ✅ | −25 Kenki. |
| Tsubame-gaeshi | 16483 | ✅ | Consumes Tsubame Ready; repeat resolved via Kaeshi gauge. |
| Kaeshi: Goken | 16485 | ➖ | Damage follow-up; Kaeshi-gauge-driven. |
| Kaeshi: Setsugekka | 16486 | ➖ | Damage follow-up; Kaeshi-gauge-driven. |
| Tendo Kaeshi Goken | 36967 | ➖ | Damage follow-up; Kaeshi-gauge-driven. |
| Tendo Kaeshi Setsugekka | 36968 | ➖ | Damage follow-up; Kaeshi-gauge-driven. |
| Shoha | 16487 | ✅ | Spends 3 Meditation (CostGauges); gate fills from Iaijutsu/Ogi. |
| Tengentsu | 36962 | ➖ | Mitigation/regen; Kenki-on-hit reactive. |
| Fuko | 25780 | ✅ | +10 Kenki. |
| Ogi Namikiri | 25781 | ✅ | Ready-consume + Kaeshi: Namikiri arm; +1 Meditation. |
| Kaeshi: Namikiri | 25782 | ➖ | Damage follow-up; Kaeshi-gauge-driven. |
| Gyofu | 36963 | ✅ | +5 Kenki (Hakaze upgrade). |
| Zanshin | 36964 | ✅ | −50 Kenki; consumes Zanshin Ready. |

## Traits
All potency or native button-swaps → ➖.

## Role actions
Second Wind, Leg Sweep, Bloodbath, Feint, Arm's Length, True North — all ➖.

## Not simulated
- **Meditate (7497)** — its channeled Meditation-stack build and gradual Kenki gain aren't ticked. (Every Iaijutsu + Ogi Namikiri now grant their Meditation stack, so Shoha's 3-stack gate fills.)
- **Hagakure Sen→Kenki** — clears Sen but doesn't add the +10 Kenki per Sen converted.
- **Meikyo Shisui** — the combo model reads live combo state (`PlayerCombo`) and ignores the Meikyo status, so a Meikyo'd weaponskill used out of combo grants no Kenki/Fugetsu/Fuka/Sen. Its stack consumer (`StatusClearedOnAction[1233]=AnyWeaponskill`) also over-broadly decrements on Enpi/Iaijutsu/Ogi, which don't consume Meikyo in-game.
- **Sen-count / Kaeshi swap unconfirmed** — the Sen bitfield and Kaeshi gauge are written directly, but the in-game `GetAdjustedActionId` swap read (Iaijutsu→Higanbana/Tenka/Midare by Sen count; Tsubame-gaeshi→Kaeshi mirror) is not yet confirmed in-game.
