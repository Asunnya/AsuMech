using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M9s.VampStomp;

// User-controlled overrides for M9sVampStompState. Spin pins all three bat rings to one direction
// (null keeps each ring's spin random); ring start angles are always randomized.
public sealed class M9sVampStompStateOverrides
{
    public BatSpin? Spin { get; set; }
    public PartyRole? BrutalRainTarget { get; set; }
}
