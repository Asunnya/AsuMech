# Pictomancer (PCT)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Fire in Red | 34650 | ✅ | |
| Aero in Green | 34651 | ✅ | |
| Tempera Coat | 34685 | ✅ | Grants self-status 3686, driving the Tempera Grassa swap; barrier out of scope |
| Water in Blue | 34652 | ✅ | +25 Palette, +1 Paint; consumes Aetherhues II |
| Smudge | 34684 | ➖ | Movement-speed utility buff; gates nothing |
| Fire II in Red | 34656 | ✅ | |
| Creature Motif | 34689 | ✅ | Resolves to the painted creature variant (canvas) |
| Living Muse | 35347 | ✅ | Resolves to Pom/Winged/Clawed/Fanged Muse |
| Mog of the Ages | 34676 | ✅ | Consumes Moogle Portrait |
| Pom Motif | 34664 | ✅ | Paints Pom canvas |
| Wing Motif | 34665 | ✅ | Paints Wing canvas |
| Pom Muse | 34670 | ✅ | Spends Pom canvas; advances creature cycle |
| Winged Muse | 34671 | ✅ | Grants Moogle Portrait; advances cycle |
| Aero II in Green | 34657 | ✅ | |
| Water II in Blue | 34658 | ✅ | +25 Palette, +1 Paint; consumes Aetherhues II |
| Weapon Motif | 34690 | ✅ | Resolves to Hammer Motif (Weapon canvas) |
| Steel Muse | 35348 | ✅ | Resolves to Striking Muse |
| Hammer Stamp | 34678 | ✅ | Consumes Hammer Time; combo native |
| Hammer Motif | 34668 | ✅ | Paints Weapon canvas |
| Striking Muse | 34674 | ✅ | Grants Hammer Time ×3; spends Weapon canvas |
| Blizzard in Cyan | 34653 | ✅ | Consumes 1 Subtractive Palette |
| Blizzard II in Cyan | 34659 | ✅ | Consumes 1 Subtractive Palette |
| Subtractive Palette | 34683 | ✅ | Spends 50 Palette; grants Subtractive ×3 + Monochrome Tones |
| Stone in Yellow | 34654 | ✅ | Consumes Aetherhues + Subtractive |
| Thunder in Magenta | 34655 | ✅ | +1 Paint; consumes Aetherhues II + Subtractive |
| Stone II in Yellow | 34660 | ✅ | Consumes Aetherhues + Subtractive |
| Thunder II in Magenta | 34661 | ✅ | +1 Paint; consumes Aetherhues II + Subtractive |
| Landscape Motif | 34691 | ✅ | Resolves to Starry Sky Motif (Landscape canvas) |
| Scenic Muse | 35349 | ✅ | Resolves to Starry Muse |
| Starry Sky Motif | 34669 | ✅ | Paints Landscape canvas |
| Starry Muse | 34675 | ✅ | Grants Subtractive Spectrum + Inspiration + Hyperphantasia ×5 + Starstruck |
| Holy in White | 34662 | ✅ | −1 Paint; Monochrome swap ↔ Comet |
| Hammer Brush | 34679 | ✅ | Combo step; consumes Hammer Time |
| Polishing Hammer | 34680 | ✅ | Combo step; consumes Hammer Time |
| Tempera Grassa | 34686 | ➖ | AoE barrier, out of scope |
| Comet in Black | 34663 | ✅ | −1 Paint; consumes Monochrome Tones |
| Rainbow Drip | 34688 | ✅ | +1 Paint; consumes Rainbow Bright when present (see Not simulated) |
| Claw Motif | 34666 | ✅ | Paints Claw canvas |
| Maw Motif | 34667 | ✅ | Paints Maw canvas |
| Clawed Muse | 34672 | ✅ | Spends Claw canvas; advances cycle |
| Fanged Muse | 34673 | ✅ | Grants Madeen Portrait |
| Retribution of the Madeen | 34677 | ✅ | Consumes Madeen Portrait |
| Star Prism | 34681 | ✅ | Consumes Starstruck; heal out of scope |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Enhanced Tempera | ✅ | Tempera Coat grants status 3686, driving the Grassa swap |
| Enhanced Pictomancy III | ❌ | Grants Rainbow Bright (3679) after all 5 Hyperphantasia consumed — never granted |

## Role actions
Addle, Sleep, Lucid Dreaming, Surecast — ➖. Swiftcast ✅ (167 → next spell instant).

## Not simulated
- **Rainbow Bright (3679) never granted** — it's a conditional grant when the 5th Hyperphantasia stack is consumed (Enhanced Pictomancy III), which the flat table can't express; Rainbow Drip's consume is wired but its instant-cast state is unreachable.
- **CreatureFlags creature-cycle encoding unconfirmed** — the `PictomancerStateHandler` writes the Pom→Wings→Claw→Maw progression + portrait-ready bits, but the packing is inferred from action descriptions, not confirmed in-game.
- **Hyperphantasia decrements on any GCD** — `StatusClearedOnAction[3688] = AnyGcd()`; strictly only aetherhue spells + Holy/Comet/Star Prism consume it, so motif GCDs also decrement it (minor).
