# Bard (BRD)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Heavy Shot | 97 | ➖ | Upgraded to Burst Shot. |
| Straight Shot | 98 | ➖ | Upgraded to Refulgent Arrow. |
| Raging Strikes | 101 | ✅ | |
| Venomous Bite | 100 | ➖ | Upgraded to Caustic Bite. |
| Bloodletter | 110 | ➖ | |
| Repelling Shot | 112 | ➖ | |
| Quick Nock | 106 | ➖ | Upgraded to Ladonsbite. |
| Wide Volley | 36974 | ✅ | Consumes Hawk's Eye/Barrage. |
| Windbite | 113 | ➖ | Upgraded to Stormbite. |
| Mage's Ballad | 114 | ⚠️ | Song buff icon only; no Song Gauge / Repertoire / Soul Voice / coda. |
| The Warden's Paean | 3561 | ➖ | |
| Barrage | 107 | ✅ | Barrage + Resonant Arrow Ready. |
| Army's Paeon | 116 | ⚠️ | Song buff icon only; gauge economy unmodelled. |
| Rain of Death | 117 | ➖ | |
| Battle Voice | 118 | ✅ | |
| The Wanderer's Minuet | 3559 | ⚠️ | Song buff icon only; Repertoire gauge (gates Pitch Perfect) unmodelled. |
| Pitch Perfect | 7404 | ⚠️ | Clears Repertoire status (3137); gauge spend unmodelled — moot, Repertoire never generated. |
| Empyreal Arrow | 3558 | ⚠️ | Faked as flat Status(3137); no Repertoire/Soul Voice generation, no song-active gate. |
| Iron Jaws | 3560 | ✅ | 35% Hawk's Eye; DoT refresh out of scope. |
| Sidewinder | 3562 | ➖ | |
| Troubadour | 7405 | ➖ | |
| Caustic Bite | 7406 | ✅ | 35% Hawk's Eye; DoT out of scope. |
| Stormbite | 7407 | ✅ | 35% Hawk's Eye; DoT out of scope. |
| Nature's Minne | 7408 | ➖ | |
| Refulgent Arrow | 7409 | ✅ | Consumes Hawk's Eye/Barrage. |
| Shadowbite | 16494 | ✅ | Consumes Hawk's Eye/Barrage. |
| Burst Shot | 16495 | ✅ | 35% Hawk's Eye. |
| Apex Arrow | 16496 | ⚠️ | Grants Blast Arrow Ready unconditionally; SV≥80 gate + 20 Soul Voice spend inert (Soul Voice never fills). |
| Ladonsbite | 25783 | ✅ | 35% Hawk's Eye. |
| Blast Arrow | 25784 | ✅ | Consumes Blast Arrow Ready. |
| Radiant Finale | 25785 | ✅ | Radiant Finale + Radiant Encore Ready; coda gate out of scope. |
| Heartbreak Shot | 36975 | ➖ | |
| Resonant Arrow | 36976 | ✅ | Consumes Resonant Arrow Ready. |
| Radiant Encore | 36977 | ✅ | Consumes Radiant Encore Ready. |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Enhanced Empyreal Arrow | ❌ | Guarantees song Repertoire on Empyreal Arrow — Repertoire system unmodelled. |
| Bite Mastery II | ✅ | Adds 35% Hawk's Eye to Caustic/Stormbite/Iron Jaws. |
| Soul Voice | ❌ | +5 Soul Voice per Repertoire — Repertoire never fires, so Soul Voice never fills. |
| Minstrel's Coda | ❌ | Grants the three Codas on singing — coda flags unmodelled (procs still fire; potency/display only). |

## Role actions
Leg Graze, Second Wind, Foot Graze, Peloton, Head Graze, Arm's Length — all ➖.

## Not simulated
- **Song-tick Repertoire.** No timed/DoT-tick handler generates Repertoire (no BRD entry in `TimedGauges`), so the whole Repertoire → Soul Voice → Pitch Perfect economy is absent.
- **Soul Voice is never generated.** Only a spender (`CostGauges[(Brd,59)]`); stays 0, so Apex Arrow's 20-cost spend and its SV≥80 Blast gate are permanently inert. Blast Arrow works only because Apex Arrow grants Blast Arrow Ready unconditionally.
- **Codas / Song Gauge widget not driven.** Songs grant only the buff-bar status; they never set `BardGauge.SongFlags`/`SongTimer`/coda bits, so the Song Gauge UI stays blank and Radiant Finale's coda gate is unrepresented (procs still fire).
- **Empyreal Arrow (3558) approximated.** Grants a flat `Status(3137)` instead of +1 Repertoire (→ +5 Soul Voice via the Soul Voice trait), with no gauge change and no song-active check.
