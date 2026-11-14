using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

public sealed class UcobP3HeavensfallTuuflessAi : UcobP3HeavensfallAi, IUcobStrat
{
    public override string Name => "Tuufless (Elemental)";
    public UcobMarkers Markers => UcobMarkers.Elemental;

    private static readonly PartyRole[] ClockwiseFromNaelIncludingIt = [OffTank, ShieldHealer, MeleeDpsB, CasterDps];
    private static readonly PartyRole[] CounterClockwiseFromNaelExcludingIt = [MainTank, RegenHealer, MeleeDpsA, PhysRangedDps];

    protected override PartyRole[] ClockwiseTowersFromNael => ClockwiseFromNaelIncludingIt;
    protected override PartyRole[] CounterClockwiseTowersFromNael => CounterClockwiseFromNaelExcludingIt;

    protected override float DiveSpreadBearing(PartyRole role) => role switch
    {
        MainTank => state.NaelBearing - 10f,
        OffTank => state.NaelBearing + 10f,
        RegenHealer => MiddleBossBearing - 80f,
        MeleeDpsA => MiddleBossBearing - 100f,
        ShieldHealer => MiddleBossBearing + 80f,
        MeleeDpsB => MiddleBossBearing + 100f,
        PhysRangedDps => state.NaelBearing + 190f,
        _ => state.NaelBearing + 170f,
    };
}
