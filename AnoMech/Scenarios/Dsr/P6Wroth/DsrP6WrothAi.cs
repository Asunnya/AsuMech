using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dsr.P6Wroth;

public sealed class DsrP6WrothAi : IScenarioAi<DsrP6WrothState>
{
    public string Name => "NA PF (Light vs Dark)";

    private static readonly PartyRole[] GroupOne = [MainTank, RegenHealer, MeleeDpsA, PhysRangedDps];
    private static readonly PartyRole[] GroupTwo = [OffTank, ShieldHealer, MeleeDpsB, CasterDps];
    private static readonly PartyRole[] LineupPriority = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer, ShieldHealer, MainTank, OffTank];
    private static readonly Vector2 FrontPairSpot = new(0f, 7.7f);
    private static readonly Vector2 WestPairSpot = new(-5.5f, 18.3f);
    private static readonly Vector2 EastPairSpot = new(5f, 18.3f);
    private const float PairOffset = 0.4f;
    private static readonly Vector2 MainTankCornerSpot = new(-12.5f, -14.5f);
    private static readonly Vector2 OffTankCornerSpot = new(12.5f, -14.5f);
    private static readonly Vector2 SharedBusterSpot = new(0f, -9f);
    private const float VowSpreadOffset = 7f;
    private static readonly float[] SpreadLineX = [-20f, -13.5f, -7f, -0.5f];
    private const float NearStackX = 7.5f;
    private const float WallStackX = 17f;
    private const float HotTailLineZ = 13f;

    private DsrP6WrothState state = null!;
    private SimWorld world = null!;

    public void Run(DsrP6WrothState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(PrePositionForTethers()).NaturalOrder());
        ai.Move(DsrP6WrothScenario.TethersOut + 0.4f, () => AiMove.Create(PairUpWithOppositeTethers()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP6WrothScenario.BreathsLand + 0.5f, () => AiMove.Create(DpsSpreadForMortalVow()).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.VowApplied + 0.4f, () => AiMove.Create(LightPartiesNorthAndSouth()).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.AkhAfahLands + 0.6f, () => AiMove.Create(Cluster(Vector2.Zero, Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.HallowedWingsCast + 0.4f, () => AiMove.Create(Cluster(new Vector2(0f, 6f * SafeWingSide), Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.NidhoggCauterizeCast + 0.3f, () => AiMove.Create(HallowedWingsSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP6WrothScenario.HallowedWingsLands + 0.8f, () => AiMove.Create(VowHolderMeetsTank(state.VowTarget, MainTank, new Vector2(0f, 0f), new Vector2(0f, 9f))).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.VowPasses[0] + 0.4f, () => AiMove.Create(Cluster(Vector2.Zero, Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP6WrothScenario.HraesvelgrCauterizeCast + 0.25f, () => AiMove.Create(Cluster(AkhMornPath()[0], Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f, sprint: true);
        for (var i = 0; i < DsrP6WrothScenario.AkhMornHits.Length; i++)
        {
            var next = i + 1;
            ai.Move(DsrP6WrothScenario.AkhMornHits[i] + 0.05f, () => AiMove.Create(Cluster(AkhMornPath()[next], Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(DsrP6WrothScenario.FlameBlastCasts[2] + DsrP6WrothScenario.FlameBlastDelay + 0.07f, () => AiMove.Create(LineUpForWrothFlames()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP6WrothScenario.FlamesResolve + 0.15f, () => AiMove.Create(VowHolderMeetsTank(MainTank, OffTank, new Vector2(2f, -6f), new Vector2(-2f, 6f))).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP6WrothScenario.VowPasses[1] + 0.4f, () => AiMove.Create(Cluster(Vector2.Zero, Enum.GetValues<PartyRole>())).NaturalOrder(), jitter: 0f);
    }

    private static Vector2?[] PrePositionForTethers()
    {
        var spots = new Vector2?[8];
        spots[(int)MainTank] = new Vector2(-2f, -3f);
        spots[(int)OffTank] = new Vector2(2f, -3f);
        Place(spots, new Vector2(0f, 5f), MeleeDpsA, MeleeDpsB);
        Place(spots, new Vector2(-5f, 14f), RegenHealer, PhysRangedDps);
        Place(spots, new Vector2(5f, 14f), ShieldHealer, CasterDps);
        return spots;
    }

    private Vector2?[] PairUpWithOppositeTethers()
    {
        var anchors = new[] { MeleeDpsA, RegenHealer, ShieldHealer };
        var flexes = new[] { MeleeDpsB, PhysRangedDps, CasterDps };
        var pairSpots = new[] { FrontPairSpot, WestPairSpot, EastPairSpot };
        SwapFlexesSoEachPairHoldsBothElements(anchors, flexes);
        var spots = new Vector2?[8];
        for (var i = 0; i < anchors.Length; i++)
        {
            spots[(int)anchors[i]] = pairSpots[i] + new Vector2(StandAwayFromYourDragon(anchors[i]), 0f);
            spots[(int)flexes[i]] = pairSpots[i] + new Vector2(StandAwayFromYourDragon(flexes[i]), 0f);
        }
        var sharedBuster = state.Glow == BreathGlow.Both;
        spots[(int)MainTank] = sharedBuster ? SharedBusterSpot + new Vector2(-0.3f, 0f) : MainTankCornerSpot;
        spots[(int)OffTank] = sharedBuster ? SharedBusterSpot + new Vector2(0.3f, 0f) : OffTankCornerSpot;
        return spots;
    }

    private void SwapFlexesSoEachPairHoldsBothElements(PartyRole[] anchors, PartyRole[] flexes)
    {
        bool Fire(PartyRole role) => state.NidhoggTethers.Contains(role);
        for (var i = 0; i < anchors.Length; i++)
        {
            if (Fire(anchors[i]) != Fire(flexes[i])) continue;
            for (var j = i + 1; j < anchors.Length; j++)
            {
                if (Fire(anchors[j]) != Fire(flexes[j]) || Fire(flexes[j]) == Fire(flexes[i])) continue;
                (flexes[i], flexes[j]) = (flexes[j], flexes[i]);
                break;
            }
        }
    }

    private float StandAwayFromYourDragon(PartyRole role) => state.NidhoggTethers.Contains(role) ? PairOffset : -PairOffset;

    private static Vector2?[] DpsSpreadForMortalVow()
    {
        var spots = Cluster(Vector2.Zero, [MainTank, OffTank, RegenHealer, ShieldHealer]);
        spots[(int)MeleeDpsA] = new Vector2(-VowSpreadOffset, -VowSpreadOffset);
        spots[(int)MeleeDpsB] = new Vector2(VowSpreadOffset, -VowSpreadOffset);
        spots[(int)PhysRangedDps] = new Vector2(-VowSpreadOffset, VowSpreadOffset);
        spots[(int)CasterDps] = new Vector2(VowSpreadOffset, VowSpreadOffset);
        return spots;
    }

    private static Vector2?[] LightPartiesNorthAndSouth()
    {
        var spots = Cluster(new Vector2(0f, -5.5f), GroupOne);
        var south = Cluster(new Vector2(0f, 6f), GroupTwo);
        foreach (var role in GroupTwo) spots[(int)role] = south[(int)role];
        return spots;
    }

    private float SafeWingSide => state.WingsNorth ? 1f : -1f;

    private float SafeDiveSide => state.NidhoggDivesEast ? -1f : 1f;

    private Vector2?[] HallowedWingsSpots()
    {
        var sz = SafeWingSide;
        var east = SafeDiveSide > 0f;
        var (mainTank, offTank, party) = (state.PlumesNear, east) switch
        {
            (true, false) => (new Vector2(-2.5f, 2.5f), new Vector2(-2.5f, 19.5f), new Vector2(-17f, 11f)),
            (true, true) => (new Vector2(19.5f, 2.5f), new Vector2(19.5f, 15f), new Vector2(3f, 4.5f)),
            (false, false) => (new Vector2(-19.5f, 2.5f), new Vector2(-19.5f, 19f), new Vector2(-2.5f, 7f)),
            _ => (new Vector2(2.5f, 2.5f), new Vector2(2.5f, 19f), new Vector2(19f, 7f)),
        };
        var spots = Cluster(party with { Y = party.Y * sz }, [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps]);
        spots[(int)MainTank] = mainTank with { Y = mainTank.Y * sz };
        spots[(int)OffTank] = offTank with { Y = offTank.Y * sz };
        return spots;
    }

    private static Vector2?[] VowHolderMeetsTank(PartyRole holder, PartyRole taker, Vector2 passSpot, Vector2 restSpot)
    {
        var rest = Enum.GetValues<PartyRole>().Where(r => r != holder && r != taker).ToArray();
        var spots = Cluster(restSpot, rest);
        spots[(int)holder] = passSpot + new Vector2(-0.3f, 0f);
        spots[(int)taker] = passSpot + new Vector2(0.3f, 0f);
        return spots;
    }

    private (int X, int Z) StartCorner()
    {
        var second = state.SecondSetCorner;
        var westFree = second.X < 0 ? (second.X, -second.Z) : (-second.X, second.Z);
        var eastFree = (-westFree.Item1, -westFree.Item2);
        return state.HraesvelgrLane < 0 ? eastFree : westFree;
    }

    private List<Vector2> AkhMornPath()
    {
        var (cx, cz) = StartCorner();
        var alongNorthOrSouthWall = cx == state.SecondSetCorner.X;
        Vector2 At(float acrossWall, float alongWall) =>
            alongNorthOrSouthWall ? new Vector2(cx * acrossWall, cz * alongWall) : new Vector2(cx * alongWall, cz * acrossWall);
        return [At(19.5f, 14f), At(17f, 14f), At(11.5f, 14f), At(3f, 12.5f), Vector2.Zero];
    }

    private Vector2?[] LineUpForWrothFlames()
    {
        var z = state.HotWing ? 0f : HotTailLineZ;
        var spots = new Vector2?[8];
        var spreads = LineupPriority.Where(state.Spreads.Contains).ToList();
        for (var i = 0; i < spreads.Count; i++) spots[(int)spreads[i]] = new Vector2(SpreadLineX[i], z);
        var stacks = LineupPriority.Where(state.Stacks.Contains).ToList();
        var partners = LineupPriority.Where(r => !state.Spreads.Contains(r) && !state.Stacks.Contains(r)).ToList();
        var stackX = new[] { NearStackX, WallStackX };
        for (var i = 0; i < stacks.Count; i++) spots[(int)stacks[i]] = new Vector2(stackX[i], z);
        for (var i = 0; i < partners.Count && i < stackX.Length; i++) spots[(int)partners[i]] = new Vector2(stackX[i] + 0.5f, z);
        return spots;
    }

    private static void Place(Vector2?[] spots, Vector2 at, params PartyRole[] roles)
    {
        foreach (var role in roles) spots[(int)role] = at;
    }

    private static Vector2?[] Cluster(Vector2 centre, IEnumerable<PartyRole> roles)
    {
        var spots = new Vector2?[8];
        var i = 0;
        foreach (var role in roles)
        {
            spots[(int)role] = centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1));
            i++;
        }
        return spots;
    }
}
