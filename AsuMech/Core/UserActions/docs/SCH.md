# Scholar (SCH)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Ruin / Broil (I–IV) | 16539… | ➖ | native upgrade chain |
| Bio / Bio II / Biolysis | 16540 | ➖ | target DoT |
| Physick | — | ➖ | |
| Summon Eos | 17215 | ➖ | pet not modelled |
| Resurrection | — | ➖ | |
| Whispering Dawn | — | ➖ | pet order |
| Adloquium | 185 | ➖ | Recitation consumer handled via clear list |
| Succor / Concitation | 37013 | ➖ | Recitation consumer handled |
| Ruin II | — | ➖ | |
| Fey Illumination | — | ➖ | pet order |
| Aetherflow | 166 | ✅ | +3 Aetherflow |
| Energy Drain | 167 | ⚠️ | Aetherflow spend handled; +10 Faerie gauge not modelled |
| Lustrate | 189 | ⚠️ | +10 Faerie gauge not modelled |
| Art of War / II | 16539/25866 | ➖ | AoE damage |
| Sacred Soil | 188 | ⚠️ | +10 Faerie gauge not modelled |
| Indomitability | 3583 | ⚠️ | +10 Faerie gauge not modelled |
| Deployment Tactics | 3585 | ➖ | |
| Emergency Tactics | 3586 / 37037 | ❌ | self-status 792 (15s) not granted |
| Dissipation | 3587 | ⚠️ | +3 Aetherflow handled; Dissipation self-status 791 (30s) missing |
| Excogitation | 7434 | ⚠️ | +10 Faerie gauge not modelled |
| Chain Stratagem | 7436 | ✅ | Impact Imminent (3882), gates Baneful Impaction |
| Aetherpact | 7437 | ➖ | Faerie/pet not modelled |
| Dissolve Union | — | ➖ | |
| Recitation | 16542 | ✅ | Recitation (1896), free-cast gate for Adlo/Conci/Indom/Excog |
| Fey Blessing | 16543 | ➖ | pet order |
| Summon Seraph | 16545 | ➖ | pet-state not modelled |
| Consolation | — | ➖ | Seraph order |
| Protraction | 25867 | ❌ | self/ally status 2710 (10s) not granted |
| Expedient | 25868 | ❌ | Expedience 2712 + Desperate Measures 2711 not granted |
| Baneful Impaction | 37012 | ✅ | consumes Impact Imminent |
| Seraphism | 37014 | ✅ | grants self-status 3884 + 4327, driving Adlo→Manifestation, Conci→Accession, Broil IV→Seraphic Halo swaps |
| Manifestation | 37015 | ✅ | Seraphism-transformed Adloquium; Recitation consumer handled |
| Accession | 37016 | ✅ | Seraphism-transformed Concitation; Recitation consumer handled |

## Traits
All potency or native button-swaps → ➖.

## Role actions
Repose, Esuna, Lucid Dreaming, Surecast, Rescue — ➖. Swiftcast ✅ (167 → next spell instant).

## Not simulated
- **Emergency Tactics (3586 / 37037)** — self-status 792 (15s) not granted.
- **Dissipation (3587)** — grants the +3 Aetherflow but not the Dissipation self-status 791 (30s).
- **Protraction (25867) / Expedient (25868)** — self-buff statuses (Protraction 2710; Expedience 2712 + Desperate Measures 2711) not granted.
- **Faerie Gauge unmodelled** — `ScholarGauge.FairyGauge` has no gauge/row; the +10 from Energy Drain/Lustrate/Sacred Soil/Indomitability/Excogitation and the −10/−50 spends (Aetherpact/Summon Seraph) are ignored.
