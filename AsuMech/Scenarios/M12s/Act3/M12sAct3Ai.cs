using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Act3.M12sAct3Scenario;

namespace AsuMech.Scenarios.M12s.Act3;

public sealed class M12sAct3Ai : IScenarioAi<M12sAct3State>
{
    public string Name => "Toxic Friends";

    private const float LeanTowardThePlatformsAt = 7.2f;
    private const float TakeYourPlatformAt = 9.1f;
    private const float LeaveThePlatformAt = 20f;
    private const float LegPause = 0.3f;
    private const float SpreadAt = 29f;

    private M12sAct3State state = null!;

    public void Run(M12sAct3State stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => Everyone(_ => Vector2.Zero));
        ai.Move(LeanTowardThePlatformsAt, () => Everyone(role => Vector2.Lerp(PlatformSpot(role, cardinal: true), PlatformSpot(role, cardinal: false), 0.5f)), jitter: 0f);
        ai.Move(TakeYourPlatformAt, () => Everyone(role => PlatformSpot(role, state.CardinalPlatforms)), jitter: 0f);
        WalkToTheHeadBaits(ai);
        ai.Move(SpreadAt, () => Everyone(SpreadSpot), jitter: 0f);
    }

    private static readonly Dictionary<Growth, (Vector2 Support, Vector2 Dps)> CardinalSpots = new()
    {
        [Growth.East] = (new(-19f, 4f), new(-11f, -4f)),
        [Growth.West] = (new(11f, 4f), new(19f, -4f)),
        [Growth.South] = (new(-4f, -14f), new(4f, -6f)),
        [Growth.North] = (new(-4f, 6f), new(4f, 14f)),
    };

    private static readonly Dictionary<Growth, (Vector2 Support, Vector2 Dps)> IntercardinalSpots = new()
    {
        [Growth.East] = (new(-19f, -6f), new(-11f, -14f)),
        [Growth.South] = (new(11f, -6f), new(19f, -14f)),
        [Growth.West] = (new(11f, 14f), new(19f, 6f)),
        [Growth.North] = (new(-19f, 14f), new(-11f, 6f)),
    };

    private Vector2 PlatformSpot(PartyRole role, bool cardinal) => PlatformSpot(state.Growths[role], role.IsDps(), cardinal);

    public static Vector2 PlatformSpot(Growth growth, bool dps, bool cardinal)
    {
        var spots = (cardinal ? CardinalSpots : IntercardinalSpots)[growth];
        return dps ? spots.Dps : spots.Support;
    }

    private static readonly Dictionary<PartyRole, Vector2> CardinalBaits = new()
    {
        [PartyRole.MainTank] = new(-9.3f, -14.4f),
        [PartyRole.OffTank] = new(9.3f, -14.3f),
        [PartyRole.RegenHealer] = new(-4.3f, 0.1f),
        [PartyRole.ShieldHealer] = new(3.8f, 0.1f),
        [PartyRole.MeleeDpsA] = new(-1f, -9.5f),
        [PartyRole.MeleeDpsB] = new(0.7f, -9.5f),
        [PartyRole.PhysRangedDps] = new(-10.9f, -0.2f),
        [PartyRole.CasterDps] = new(9.1f, 0.1f),
    };

    private static readonly Dictionary<PartyRole, Vector2> CardinalSpreads = new()
    {
        [PartyRole.MainTank] = new(-0.4f, -6.6f),
        [PartyRole.OffTank] = new(0.6f, -6.4f),
        [PartyRole.RegenHealer] = new(-9.1f, -0.2f),
        [PartyRole.ShieldHealer] = new(9.6f, -0.2f),
        [PartyRole.MeleeDpsA] = new(-9.2f, -11.3f),
        [PartyRole.MeleeDpsB] = new(9.3f, -11f),
        [PartyRole.PhysRangedDps] = new(-18.9f, 0.2f),
        [PartyRole.CasterDps] = new(18.8f, -0.1f),
    };

    private static readonly Dictionary<PartyRole, Vector2> IntercardinalSupportBaits = new()
    {
        [PartyRole.MainTank] = new(-9f, -5f),
        [PartyRole.OffTank] = new(9f, -5f),
        [PartyRole.RegenHealer] = new(-1.9f, 1.8f),
        [PartyRole.ShieldHealer] = new(2f, 1.9f),
    };

    private static readonly Dictionary<PartyRole, Vector2> IntercardinalSupportSpreads = new()
    {
        [PartyRole.MainTank] = new(-0.4f, 0f),
        [PartyRole.OffTank] = new(0.4f, 0f),
        [PartyRole.RegenHealer] = new(-8.9f, 0f),
        [PartyRole.ShieldHealer] = new(8.6f, 0f),
    };

    private Vector2 OuterCornerOfYourPlatform(PartyRole role)
    {
        var spot = PlatformSpot(role, cardinal: false);
        return new Vector2(MathF.Sign(spot.X) * 14.2f, MathF.Sign(spot.Y) * 12.4f);
    }

    private Vector2 HeadBaitSpot(PartyRole role)
    {
        if (state.CardinalPlatforms) return CardinalBaits[role];
        return role.IsDps() ? OuterCornerOfYourPlatform(role) : IntercardinalSupportBaits[role];
    }

    private Vector2? SpreadSpot(PartyRole role)
    {
        if (state.CardinalPlatforms) return CardinalSpreads[role];
        return role.IsDps() ? OuterCornerOfYourPlatform(role) : IntercardinalSupportSpreads[role];
    }

    private static readonly Vector2[] Crossings =
    [
        new(-7.5f, -7.5f), new(7.5f, -7.5f), new(7.5f, 7.5f), new(-7.5f, 7.5f),
        new(-7.5f, 0f), new(7.5f, 0f), new(0f, 0f), new(0f, -7.5f), new(0f, 7.5f),
    ];

    private List<Vector2> RouteAroundTheHoles(Vector2 from, Vector2 to)
    {
        var nodes = new List<Vector2> { from };
        nodes.AddRange(Crossings);
        nodes.Add(to);
        var distance = nodes.Select(_ => float.MaxValue).ToArray();
        var previous = nodes.Select(_ => -1).ToArray();
        var done = new bool[nodes.Count];
        distance[0] = 0f;
        for (var round = 0; round < nodes.Count; round++)
        {
            var current = Enumerable.Range(0, nodes.Count).Where(i => !done[i]).OrderBy(i => distance[i]).First();
            if (distance[current] == float.MaxValue) break;
            done[current] = true;
            for (var next = 0; next < nodes.Count; next++)
            {
                if (done[next] || !StaysOnTheFloor(nodes[current], nodes[next])) continue;
                var through = distance[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (through >= distance[next]) continue;
                distance[next] = through;
                previous[next] = current;
            }
        }
        var path = new List<Vector2>();
        for (var at = nodes.Count - 1; at > 0 && previous[at] >= 0; at = previous[at]) path.Insert(0, nodes[at]);
        return path.Count > 0 ? path : [to];
    }

    private void WalkToTheHeadBaits(AiManager ai)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var from = PlatformSpot(role, state.CardinalPlatforms);
            var at = LeaveThePlatformAt;
            foreach (var stop in RouteAroundTheHoles(from, HeadBaitSpot(role)))
            {
                var leg = stop;
                var who = role;
                ai.Move(at, () => Only(who, leg), jitter: 0f);
                at += Vector2.Distance(from, stop) / AiManager.RunSpeed + LegPause;
                from = stop;
            }
        }
    }

    private const float FloorMargin = 0.5f;

    private bool StaysOnTheFloor(Vector2 from, Vector2 to)
    {
        var steps = (int)MathF.Ceiling(Vector2.Distance(from, to) / 0.25f) + 1;
        for (var i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(from, to, (float)i / steps);
            if (!FarFromTheHoles(p)) return false;
        }
        return true;
    }

    private bool FarFromTheHoles(Vector2 p)
        => state.Holes.All(c => MathF.Abs(p.X - c.X) > M12sAct3State.PlatformHalf + FloorMargin || MathF.Abs(p.Y - c.Y) > M12sAct3State.PlatformHalf + FloorMargin)
           && Act3Floor.IsFloor(state, Act3Floor.Stage.Whole, p);

    private static IAiMove Only(PartyRole who, Vector2 spot)
        => Everyone(role => role == who ? spot : null);

    private static IAiMove Everyone(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
