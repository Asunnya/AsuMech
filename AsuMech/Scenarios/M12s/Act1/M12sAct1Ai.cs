using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Act1.M12sAct1Scenario;

namespace AsuMech.Scenarios.M12s.Act1;

public sealed class M12sAct1Ai : IScenarioAi<M12sAct1State>
{
    public string Name => "Toxic Friends";

    private static readonly Vector2[] SpotlightBaits = [new(-13.5f, -12.4f), new(-7f, -12.4f), new(-0.5f, -12.4f), new(0f, -5.9f)];
    private const float LeaveTheLastBaitAt = 19f;
    private const float AimTheArrowsAt = 23.2f;
    private const float RunFromTheVesselsAt = 26.3f;
    private const float RegroupForTheFixerAt = 34f;

    private static readonly Vector2[] SpreadSpotsEastSafe = [new(4.5f, -14f), new(8f, -6.4f), new(15.8f, 0f), new(8.9f, 8.8f)];
    private static readonly Vector2[] StackSpotsEastSafe = [new(14.2f, -13.6f), new(16f, -13.6f), new(17.3f, -12.4f), new(17.3f, -10.8f)];

    private const float FaceWest = -MathF.PI / 2f;
    private const float FaceEast = MathF.PI / 2f;
    private const float FaceNorth = MathF.PI;

    private static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    private static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private M12sAct1State state = null!;

    public void Run(M12sAct1State stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => SpotlightBaits[0] + StackOffset(role)));
        for (var w = 1; w < SpotlightBaits.Length; w++)
        {
            var bait = w;
            ai.Move(SpotlightAt[bait - 1] + 0.06f, () => ByRole(role => SpotlightBaits[bait] + StackOffset(role)));
        }
        ai.Move(LeaveTheLastBaitAt, () => ByRole(StaticGrotesquerieSpot), jitter: 0f);
        world.Events.Add(AimTheArrowsAt, () => AimEveryArrow(world));
        ai.Move(RunFromTheVesselsAt, () => ByRole(PartyStaysTanksCross), jitter: 0f, sprint: true);
        ai.Move(RegroupForTheFixerAt, () => ByRole(role => new Vector2(0f, -2f) + StackOffset(role)));
    }

    private float Mirror => state.EastSafe ? 1f : -1f;

    private int PlaceInGroup(PartyRole role) => Array.IndexOf(role.IsDps() ? Dps : Supports, role);

    private Vector2? StaticGrotesquerieSpot(PartyRole role)
    {
        var spot = (state.Spreads(role) ? SpreadSpotsEastSafe : StackSpotsEastSafe)[PlaceInGroup(role)];
        return new Vector2(spot.X * Mirror, spot.Y);
    }

    private float CleaveHeading(PartyRole role)
    {
        var place = PlaceInGroup(role);
        if (state.Spreads(role)) return (place < 2 ? FaceWest : FaceEast) * Mirror;
        return place < 2 ? FaceNorth : FaceEast * Mirror;
    }

    private void AimEveryArrow(SimWorld world)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            member.TurnTo(CleaveHeading(role) - M12sAct1State.ArrowTurn(state.Arrows[role]));
        }
    }

    private Vector2? PartyStaysTanksCross(PartyRole role)
    {
        var spot = M12sAct1State.VesselSafeSpot(state.VesselLayout, role.IsTank() != state.EastSafe);
        return new Vector2(spot.X, spot.Z) + StackOffset(role);
    }

    public static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
