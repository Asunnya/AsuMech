using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using State = AsuMech.Scenarios.Ucob.P3Fellruin.UcobP3FellruinState;
using Scenario = AsuMech.Scenarios.Ucob.P3Fellruin.UcobP3FellruinScenario;

namespace AsuMech.Scenarios.Ucob.P3Fellruin;

public sealed class UcobP3FellruinAi(bool tuufless = false) : IScenarioAi<UcobP3FellruinState>, IUcobStrat
{
    public string Name => tuufless ? "Tuufless (Elemental)" : "Tanks take the tethers to their own neurolinks, party in the one opposite Bahamut";

    public UcobMarkers Markers => tuufless ? UcobMarkers.Elemental : UcobMarkers.Aether;

    private static readonly PartyRole[] Party = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private const float TankStepTowardBahamut = 4.5f;
    private const float PartyStepAwayFromBahamut = 1.5f;
    private const float RingAroundNael = 5f;
    private const float SpreadAroundNeurolink = 5f;
    private const float StackSpacing = 0.5f;
    private const float FlattenTankStep = 3f;
    private const float After = 0.05f;

    private const float TankStepTowardTheCentre = 2.5f;
    private const float TankStepAside = 2f;
    private const float SpreadFrontRow = 7.5f;
    private const float SpreadTankAside = 6.5f;
    private const float SpreadMiddleRowAside = 7.5f;
    private const float SpreadMiddleRowForward = 1f;
    private const float SpreadBackRowAside = 4.5f;
    private const float SpreadBackRow = 6f;
    private const float OffTankLeanTowardBahamut = 1.2f;
    private const float PartyLeanAwayFromBahamut = 1f;
    private const float MeteorShieldHealerTowardTheCentre = 8f;
    private const float MeteorMiddleRowAside = 8f;
    private const float MeteorMiddleRowTowardTheCentre = 3.5f;
    private const float MeteorBackRowAside = 7f;
    private const float MeteorBackRowOutward = 5.5f;

    private State state = null!;

    public void Run(UcobP3FellruinState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(0.3f, () => AiMove.Create(StackAt(Vector2.Zero)).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.Quote + 0.1f, () => AiMove.Create(FirstMoveSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.QuoteMoves[0] + After, () => AiMove.Create(SecondMoveSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.QuoteMoves[1] + After, () => AiMove.Create(NeurolinkSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.AethericProfusionResolves + After, () => AiMove.Create(MeteorSpreadSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.MeteorStreams + After, () => AiMove.Create(GigaflareAndFlattenSpots()).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private Vector2?[] FirstMoveSpots() => NaelMoveSpots(state.Quote.First);

    private Vector2?[] SecondMoveSpots() => NaelMoveSpots(state.Quote.Second);

    private Vector2?[] NaelMoveSpots(NaelMove move) => (move == NaelMove.LunarDynamo, tuufless) switch
    {
        (true, false) => StackUnderNael(),
        (false, false) => RingAroundNaelSpots(),
        (true, true) => StackOnNaelTanksTowardTheCentre(),
        (false, true) => SpreadAroundNaelTanksTowardTheCentre(),
    };

    private Vector2 NaelTowardTheCentre => -Vector2.Normalize(state.Nael);

    private Vector2 NaelLeftFacingTheCentre => LeftOf(NaelTowardTheCentre);

    private Vector2 NaelTowardBahamutSide
        => Vector2.Dot(state.Bahamut - state.Nael, NaelLeftFacingTheCentre) > 0f ? NaelLeftFacingTheCentre : -NaelLeftFacingTheCentre;

    private Vector2?[] StackOnNaelTanksTowardTheCentre()
    {
        var spots = StackAt(state.Nael);
        var tanks = state.Nael + NaelTowardTheCentre * TankStepTowardTheCentre;
        spots[(int)MainTank] = tanks + NaelTowardBahamutSide * TankStepAside;
        spots[(int)OffTank] = tanks - NaelTowardBahamutSide * TankStepAside;
        return spots;
    }

    private Vector2?[] SpreadAroundNaelTanksTowardTheCentre()
    {
        var forward = NaelTowardTheCentre;
        var left = NaelLeftFacingTheCentre;
        var spots = new Vector2?[8];
        spots[(int)MainTank] = state.Nael + forward * SpreadFrontRow + NaelTowardBahamutSide * SpreadTankAside;
        spots[(int)OffTank] = state.Nael + forward * SpreadFrontRow - NaelTowardBahamutSide * SpreadTankAside;
        spots[(int)ShieldHealer] = state.Nael + forward * SpreadFrontRow;
        spots[(int)MeleeDpsA] = state.Nael + forward * SpreadMiddleRowForward + left * SpreadMiddleRowAside;
        spots[(int)RegenHealer] = state.Nael;
        spots[(int)MeleeDpsB] = state.Nael + forward * SpreadMiddleRowForward - left * SpreadMiddleRowAside;
        spots[(int)PhysRangedDps] = state.Nael - forward * SpreadBackRow + left * SpreadBackRowAside;
        spots[(int)CasterDps] = state.Nael - forward * SpreadBackRow - left * SpreadBackRowAside;
        return spots;
    }

    private Vector2 TowardBahamut => Vector2.Normalize(state.Bahamut - state.Nael);

    private Vector2?[] StackUnderNael()
    {
        var spots = StackAt(state.Nael - TowardBahamut * PartyStepAwayFromBahamut);
        var side = new Vector2(-TowardBahamut.Y, TowardBahamut.X);
        spots[(int)MainTank] = state.Nael + TowardBahamut * TankStepTowardBahamut + side;
        spots[(int)OffTank] = state.Nael + TowardBahamut * TankStepTowardBahamut - side;
        return spots;
    }

    private Vector2?[] RingAroundNaelSpots()
    {
        PartyRole[] aroundFromBahamut = [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
        float[] degrees = [22.5f, -22.5f, 67.5f, -67.5f, 112.5f, -112.5f, 157.5f, -157.5f];
        var spots = new Vector2?[8];
        for (var i = 0; i < aroundFromBahamut.Length; i++)
            spots[(int)aroundFromBahamut[i]] = state.Nael + Rotate(TowardBahamut, degrees[i]) * RingAroundNael;
        return spots;
    }

    private Vector2 MainTankNeurolink => tuufless ? NeurolinkUnder(state.BahamutBearing) : State.NeurolinkNearest(state.BahamutBearing + 60f);
    private Vector2 OffTankNeurolink => tuufless ? NeurolinkUnder(state.TwintaniaBearing) : State.NeurolinkNearest(state.BahamutBearing - 60f);
    private Vector2 PartyNeurolink => tuufless ? NeurolinkUnder(state.NaelBearing) : State.NeurolinkNearest(state.BahamutBearing + 180f);

    private Vector2 NeurolinkUnder(float bossBearing) => State.NeurolinkNearest(bossBearing, Markers);

    private Vector2?[] NeurolinkSpots()
    {
        if (tuufless) return NeurolinkSpotsOffTankNearerBahamutThanTheParty();
        var spots = StackAt(PartyNeurolink);
        spots[(int)MainTank] = MainTankNeurolink;
        spots[(int)OffTank] = OffTankNeurolink;
        return spots;
    }

    private Vector2?[] NeurolinkSpotsOffTankNearerBahamutThanTheParty()
    {
        var spots = StackAt(PartyNeurolink + Vector2.Normalize(PartyNeurolink - state.Bahamut) * PartyLeanAwayFromBahamut);
        spots[(int)MainTank] = MainTankNeurolink;
        spots[(int)OffTank] = OffTankNeurolink + Vector2.Normalize(state.Bahamut - OffTankNeurolink) * OffTankLeanTowardBahamut;
        return spots;
    }

    private Vector2?[] MeteorSpreadSpots() => tuufless ? MeteorSpreadAroundNaelsNeurolink() : MeteorSpreadInARingAroundTheNeurolink();

    private Vector2?[] MeteorSpreadAroundNaelsNeurolink()
    {
        var link = PartyNeurolink;
        var outward = Vector2.Normalize(link);
        var left = LeftOf(-outward);
        var spots = new Vector2?[8];
        spots[(int)MainTank] = MainTankNeurolink;
        spots[(int)OffTank] = OffTankNeurolink;
        spots[(int)RegenHealer] = link;
        spots[(int)ShieldHealer] = link - outward * MeteorShieldHealerTowardTheCentre;
        spots[(int)MeleeDpsA] = link - outward * MeteorMiddleRowTowardTheCentre + left * MeteorMiddleRowAside;
        spots[(int)MeleeDpsB] = link - outward * MeteorMiddleRowTowardTheCentre - left * MeteorMiddleRowAside;
        spots[(int)PhysRangedDps] = link + outward * MeteorBackRowOutward + left * MeteorBackRowAside;
        spots[(int)CasterDps] = link + outward * MeteorBackRowOutward - left * MeteorBackRowAside;
        return spots;
    }

    private Vector2?[] MeteorSpreadInARingAroundTheNeurolink()
    {
        var spots = new Vector2?[8];
        var outward = Vector2.Normalize(PartyNeurolink);
        for (var i = 0; i < Party.Length; i++)
            spots[(int)Party[i]] = PartyNeurolink + Rotate(outward, 30f + 60f * i) * SpreadAroundNeurolink;
        spots[(int)MainTank] = MainTankNeurolink;
        spots[(int)OffTank] = OffTankNeurolink;
        return spots;
    }

    private Vector2?[] GigaflareAndFlattenSpots()
    {
        var spots = StackAt(Vector2.Zero);
        var tankSpot = state.Bahamut - Vector2.Normalize(state.Bahamut) * FlattenTankStep;
        spots[(int)MainTank] = tankSpot + new Vector2(StackSpacing, 0f);
        spots[(int)OffTank] = tankSpot - new Vector2(StackSpacing, 0f);
        return spots;
    }

    private static Vector2?[] StackAt(Vector2 centre)
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < 8; i++)
            spots[i] = centre + new Vector2(StackSpacing * (i % 3 - 1), StackSpacing * (i / 3 - 1));
        return spots;
    }

    private static Vector2 LeftOf(Vector2 facing) => new(facing.Y, -facing.X);

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(v.X * MathF.Cos(r) - v.Y * MathF.Sin(r), v.X * MathF.Sin(r) + v.Y * MathF.Cos(r));
    }
}
