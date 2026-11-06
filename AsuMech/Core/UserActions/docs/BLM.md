# Black Mage (BLM)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Blizzard | 142 | ✅ | |
| Fire | 141 | ✅ | Astral Fire + 40% Firestarter + consumes an Umbral Heart |
| Transpose | 149 | ✅ | |
| Thunder | 144 | ✅ | |
| Blizzard II | 146 | ✅ | |
| Scathe | 156 | ➖ | |
| Fire II | 147 | ✅ | |
| Thunder II | 7447 | ✅ | |
| Manaward | 157 | ➖ | |
| Manafont | 158 | ✅ | AF III + 3 Umbral Hearts + Paradox marker + Thunderhead |
| Fire III | 152 | ✅ | consumes Firestarter |
| Blizzard III | 154 | ✅ | |
| Umbral Soul | 16506 | ✅ | Umbral Ice + 1 Umbral Heart (MP out of scope) |
| Freeze | 159 | ✅ | 3 Umbral Hearts |
| Thunder III | 153 | ✅ | |
| Aetherial Manipulation | 155 | ➖ | |
| Flare | 162 | ✅ | AF III + 3 Astral Soul + consumes Umbral Hearts |
| Ley Lines | 3573 | ✅ | |
| Blizzard IV | 3576 | ✅ | 3 Umbral Hearts |
| Fire IV | 3577 | ✅ | +1 Astral Soul |
| Between the Lines | 7419 | ➖ | |
| Thunder IV | 7420 | ✅ | |
| Triplecast | 7421 | ✅ | grants status 1211 (3 stacks); each of the next 3 spells is instant and decrements a stack |
| Foul | 7422 | ✅ | spends 1 Polyglot |
| Despair | 16505 | ✅ | AF III |
| Xenoglossy | 16507 | ✅ | spends 1 Polyglot |
| High Fire II | 25794 | ✅ | |
| High Blizzard II | 25795 | ✅ | |
| Amplifier | 25796 | ✅ | +1 Polyglot |
| Paradox | 25797 | ⚠️ | marker consume + native Fire/Blizzard→Paradox swap handled; the Astral-Fire Firestarter grant is not applied |
| High Thunder | 36986 | ✅ | |
| High Thunder II | 36987 | ✅ | |
| Retrace | 36988 | ➖ | |
| Flare Star | 36989 | ✅ | gated on full Astral Gauge (6 Astral Soul), spent on use |

## Traits
All BLM traits are potency bumps, native action upgrades/swaps, or gauge mechanics now driven by the state handler → ➖.

## Role actions
Addle, Sleep, Lucid Dreaming, Surecast — ➖. Swiftcast ✅ (167 → next spell instant).

## Not simulated
- **Paradox Firestarter** — Paradox's "Astral Fire Bonus: grants Firestarter" is not applied (only the marker consume + native button-swap are).
- **Polyglot passive regen** — modelled as an always-on +1/30s; real generation requires active Enochian under Astral Fire/Umbral Ice.
- **Elemental-gauge encoding unverified in-game** — Fire/Blizzard stance transitions from the opposite aspect, the Paradox arm condition, and whether the Umbral-Ice Enochian timer pauses are approximations pending in-game confirmation.
