using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.AlleyOopTwo;

// Pins the random rolls; null keeps the roll.
public sealed class M10sAlleyOopTwoStateOverrides
{
    public bool? DoubleDip { get; set; }
    public PartyRole? PyrotationTarget { get; set; }
}
