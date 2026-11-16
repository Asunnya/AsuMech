using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Act1;

// Pins the random rolls; null keeps the roll.
public sealed class M12sAct1StateOverrides
{
    public Act1Debuff? PlayerDebuff { get; set; }
    public GrotesquerieArrow? PlayerArrow { get; set; }
    public bool? EastSafe { get; set; }
    public int? VesselLayout { get; set; }
    public PartyRole? StackHealer { get; set; }
}
