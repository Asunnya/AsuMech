using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P4KefkaSays;

// Exdeath cast 2's Real/Fake override. Unlike the other casts it can also mirror
// cast 1's resolution (OppositeTo1 => Wave2True = !Wave1True).
public enum ExdeathCast2Mode { Auto, Real, Fake, OppositeTo1 }

// User-controlled overrides for UmadP4KefkaSaysState's randomized fields. Bound by
// the scenario's settings UI; null/default values leave the field randomized at
// scenario start. The state ctor consumes this directly.
// See UmadP2ForsakenStateOverrides for the canonical shape.
public sealed class UmadP4KefkaSaysStateOverrides
{
    // Kefka's five Mystery casts, by cast order (only [0] has debug-only UI). null = random.
    public bool?[] BlizzardReal { get; } = new bool?[5];
    public bool?[] LightningReal { get; } = new bool?[5];
    public int?[] BlizzardOffset { get; } = new int?[5];       // 0 or 1
    public int?[] LightningOffset { get; } = new int?[5];      // 0 or 1
    public float?[] LightningOrientation { get; } = new float?[5]; // +1 or -1

    // Neo Exdeath's four Mystery casts (3x Grand Cross + Flood of Naught), by cast order.
    // null = random; true = Real (boss tells the truth), false = Fake (boss lies).
    public bool? ExdeathCast1Real { get; set; }
    public ExdeathCast2Mode ExdeathCast2 { get; set; }   // Auto/Real/Fake, or Opposite-to-1
    public bool? ExdeathCast3Real { get; set; }
    public bool? ExdeathCast4Real { get; set; }

    // Chaos's two casts, by cast order (the element type Inferno/Tsunami stays randomized).
    // null = random; true = Real, false = Fake.
    public bool? ChaosCast1Real { get; set; }
    public bool? ChaosCast2Real { get; set; }
    public bool? InfernoFirst { get; set; }

    // No UI: tests pin these so the AI's coordinates are fixed.
    public bool? Wave1First { get; set; }
    public PartyRole[]? Wave1 { get; set; }               // supports in [0..3], dps in [4..7]
    public bool?[] Wave2Swaps { get; } = new bool?[4];    // swap the pair Wave2[2i], Wave2[2i+1]
    public PartyRole[]? Wave3 { get; set; }               // supports in [0..3], dps in [4..7]
    public bool[]? Wounds { get; set; }                   // by Wave3 index; true = White
    public Direction? NeoExdeathDirection { get; set; }
    public bool? Antilight0White { get; set; }
}
