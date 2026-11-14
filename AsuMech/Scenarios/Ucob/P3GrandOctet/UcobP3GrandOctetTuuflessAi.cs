using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

public sealed class UcobP3GrandOctetTuuflessAi(bool tankLimitBreak) : UcobP3GrandOctetAi(tankLimitBreak), IUcobStrat
{
    public override string Name => UsesTankLimitBreak
        ? "Tuufless (Elemental): tank LB3"
        : "Tuufless (Elemental): no LB3, Megaflares stack on Twintania's right";

    public UcobMarkers Markers => UcobMarkers.Elemental;

    protected override PartyRole LimitBreakTank => state.TwintaniaTarget == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
}
