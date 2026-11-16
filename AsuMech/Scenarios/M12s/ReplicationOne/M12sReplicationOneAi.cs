using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Scenario = AsuMech.Scenarios.M12s.ReplicationOne.M12sReplicationOneScenario;

namespace AsuMech.Scenarios.M12s.ReplicationOne;

public sealed class M12sReplicationOneAi : IScenarioAi<M12sReplicationOneState>
{
    public string Name => "DN Replication";

    private const float PairRing = 11f;
    private const float NearTheAxis = 40f;
    private const float AwayFromTheAxis = 50f;
    private static readonly Vector2 BehindTheBoss = new(0f, 7f);
    private static readonly Vector2 InFrontOfTheBoss = new(0f, -4f);
    private const float HuddleRadius = 1.5f;
    private const float PartyAwayFromTheFollowUp = 10f;
    private const float TanksBehindTheFollowUp = 2f;
    private const float TanksApart = 9.5f;

    private static readonly IReadOnlyDictionary<int, Vector2> LetterSpotsWhenOuterFireSouthEastAndCleavesVertical = new Dictionary<int, Vector2>
    {
        [0] = new(0.7f, -13.6f),
        [1] = new(13.7f, 0f),
        [2] = new(-0.7f, 13.55f),
        [3] = new(-13.6f, 0f),
    };

    private static readonly IReadOnlyDictionary<int, Vector2> NumberSpotsWhenInnerDarkSouthEastAndCleavesVertical = new Dictionary<int, Vector2>
    {
        [1] = new(6.7f, -1.4f),
        [2] = new(-1.4f, 6.7f),
    };

    private M12sReplicationOneState state = null!;
    private SimWorld world = null!;

    public void Run(M12sReplicationOneState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(PairsOnTheIntercardinals));
        ai.Move(Scenario.FirstDarkAt + 0.05f, () => ByRole(role => Huddle(BehindTheBoss, role)), sprint: true);
        ai.Move(Scenario.ElementClonesDashAt + 1f, () => ByRole(SecondSetSpot), jitter: 0f);
        ai.Move(Scenario.SecondDarkAt + 0.05f, () => ByRole(MainTankInFrontEveryoneElseBehind), sprint: true);
        ai.Move(Scenario.SobatKickAt + 0.05f, () => ByRole(AwayFromTheFollowUpKick), sprint: true);
    }

    private static Vector2 PairsOnTheIntercardinals(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA => PairSpot(0, NearTheAxis),
        PartyRole.MainTank => PairSpot(0, AwayFromTheAxis),
        PartyRole.ShieldHealer => PairSpot(1, NearTheAxis),
        PartyRole.CasterDps => PairSpot(1, AwayFromTheAxis),
        PartyRole.MeleeDpsB => PairSpot(2, NearTheAxis),
        PartyRole.OffTank => PairSpot(2, AwayFromTheAxis),
        PartyRole.RegenHealer => PairSpot(3, NearTheAxis),
        _ => PairSpot(3, AwayFromTheAxis),
    };

    private static Vector2 PairSpot(int intercard, float degreesFromNorthSouth)
    {
        var east = intercard is 0 or 1 ? 1f : -1f;
        var south = intercard is 1 or 2 ? 1f : -1f;
        var angle = degreesFromNorthSouth * MathF.PI / 180f;
        return new Vector2(east * PairRing * MathF.Sin(angle), south * PairRing * MathF.Cos(angle));
    }

    private static Vector2 Huddle(Vector2 around, PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return around + HuddleRadius * new Vector2(MathF.Sin(angle), MathF.Cos(angle));
    }

    private Vector2 SecondSetSpot(PartyRole role)
    {
        var hasDark = world.Party.Get(role)?.HasStatus(StatusId.DarkResistanceDownII) == true;
        var outerFire = state.OuterFireIntercard;
        var outerDark = state.OuterDarkIntercard;
        return (IsMelee(role), hasDark) switch
        {
            (true, true) => Vector2.Zero,
            (false, true) => LetterSpot(FirstClockwiseFromNorth(outerFire)),
            (false, false) => LetterSpot(IsHealer(role) ? FirstClockwiseFromNorth(outerDark) : FirstCounterclockwiseFromNorthwest(outerDark)),
            (true, false) => NumberSpot(IsTank(role) ? FirstClockwiseFromNorth(outerFire) : FirstCounterclockwiseFromNorthwest(outerFire)),
        };
    }

    private static int FirstClockwiseFromNorth(int intercard) => intercard == 3 ? 0 : intercard;

    private static int FirstCounterclockwiseFromNorthwest(int intercard) => intercard == 3 ? 3 : intercard + 1;

    private Vector2 LetterSpot(int cardinal) => FromTheReferenceLayout(LetterSpotsWhenOuterFireSouthEastAndCleavesVertical[IntoTheReferenceLayout(cardinal)]);

    private Vector2 NumberSpot(int cardinal) => FromTheReferenceLayout(NumberSpotsWhenInnerDarkSouthEastAndCleavesVertical[IntoTheReferenceLayout(cardinal)]);

    private int QuarterTurnsFromTheReferenceLayout => (state.OuterFireIntercard + 3) % 4;

    private bool MirroredFromTheReferenceLayout => state.VerticalCleaves != (QuarterTurnsFromTheReferenceLayout % 2 == 0);

    private int IntoTheReferenceLayout(int cardinal)
    {
        var turned = (cardinal - QuarterTurnsFromTheReferenceLayout + 4) % 4;
        return MirroredFromTheReferenceLayout ? 3 - turned : turned;
    }

    private Vector2 FromTheReferenceLayout(Vector2 spot)
    {
        var p = MirroredFromTheReferenceLayout ? new Vector2(spot.Y, spot.X) : spot;
        for (var i = 0; i < QuarterTurnsFromTheReferenceLayout; i++)
            p = new Vector2(-p.Y, p.X);
        return p;
    }

    private static Vector2 MainTankInFrontEveryoneElseBehind(PartyRole role)
        => role == PartyRole.MainTank ? InFrontOfTheBoss : Huddle(BehindTheBoss, role);

    private Vector2 AwayFromTheFollowUpKick(PartyRole role)
    {
        var tank = world.Party.Get(PartyRole.MainTank);
        var towardTheTank = tank is null ? MathF.PI : MathF.Atan2(tank.Position.X, tank.Position.Z);
        var heading = towardTheTank + state.SobatTurns * MathF.PI / 2f;
        var kick = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        var side = new Vector2(-kick.Y, kick.X);
        return role switch
        {
            PartyRole.MainTank => -TanksBehindTheFollowUp * kick + TanksApart * side,
            PartyRole.OffTank => -TanksBehindTheFollowUp * kick - TanksApart * side,
            _ => Huddle(-PartyAwayFromTheFollowUp * kick, role),
        };
    }

    private static bool IsMelee(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;

    private static bool IsTank(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank;

    private static bool IsHealer(PartyRole role) => role is PartyRole.RegenHealer or PartyRole.ShieldHealer;

    private static IAiMove ByRole(Func<PartyRole, Vector2> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => (Vector2?)at(role)).ToArray()).NaturalOrder();
}
