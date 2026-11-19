using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Umad.P1Opening;

// Pins the random rolls; null keeps the roll. A forced role only binds within its own role group.
public sealed class UmadP1OpeningStateOverrides
{
    public bool? TetheredAreDps { get; set; }
    public bool? FireIsStack { get; set; }
    public bool? FireIsLie { get; set; }
    public PartyRole? FireStackOn { get; set; }
    public bool? FirstIceIsLie { get; set; }
    public bool? SecondIceIsLie { get; set; }
    public bool? ThunderIsLie { get; set; }
    public bool? ThirdIceIsLie { get; set; }
    public PartyRole? LaserOn { get; set; }
    public PartyRole? FirstConfettiOn { get; set; }
    public PartyRole? SecondConfettiOn { get; set; }
    public bool? FirstGravitasOnDps { get; set; }
    public bool? SecondGravitasOnDps { get; set; }
    public bool? FirstWillEast { get; set; }
    public bool? SecondWillEast { get; set; }
}
