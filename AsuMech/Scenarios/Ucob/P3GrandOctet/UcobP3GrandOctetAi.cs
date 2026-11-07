using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Scenario = AsuMech.Scenarios.Ucob.P3GrandOctet.UcobP3GrandOctetScenario;
using State = AsuMech.Scenarios.Ucob.P3GrandOctet.UcobP3GrandOctetState;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

public sealed class UcobP3GrandOctetAi(bool tankLimitBreak) : IScenarioAi<UcobP3GrandOctetState>
{
    public string Name => tankLimitBreak
        ? "Run the rim from opposite Bahamut, tank LB3 for the stacks"
        : "Run the rim from opposite Bahamut, no LB3: stack clockwise of Twintania";

    private const float RimRadius = 19f;
    private const float RunStarts = 23.4f;
    private const float RunEnds = 35.9f;
    private const float SecondsPerSlot = 2.08f;
    private const float RunStep = 0.52f;
    private const float BaitOffset = -20f;
    private const float BaitRadius = 19f;
    private const float StackOffset = 25f;
    private const float StackRadius = 7f;
    private const float StackSpread = 3f;
    private const float SpreadRadius = 6f;
    private const float TwisterStep = 3.5f;
    private const float TwisterClearance = 2.6f;
    private const float MaxDodgeRadius = 20f;
    private const float BackToCentre = 36.35f;
    private const float LeaveCentreForPosts = 38.4f;
    private const float LimitBreakCheck = 42.0f;
    private const float BaitLeaves = 43.35f;
    private const float StackSplits = 46.45f;
    private const float TwisterTargetsStepOff = 47.72f;
    private const float TowerTwisterTargetsStepOff = 48.12f;

    private static readonly float[] SpreadBearings = [45f, 135f, 180f, 270f];
    private static readonly Vector2[] StackHuddle = [new(-0.3f, -0.3f), new(0.3f, -0.3f), new(-0.3f, 0.3f), new(0.3f, 0.3f)];
    private static readonly ushort[] LimitBreakStatuses = [StatusId.LastBastion, StatusId.LandWaker, StatusId.DarkForce, StatusId.GunmetalSoul];

    private State state = null!;
    private SimWorld world = null!;
    private int runFromSlot;
    private bool stacksIgnored;
    private List<Vector2> twisterSpots = [];

    public void Run(UcobP3GrandOctetState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        runFromSlot = OppositeBahamutSkippingNael();
        stacksIgnored = false;
        twisterSpots = [];

        ai.Move(0.5f, () => AiMove.Create(Everyone(Vector2.Zero)).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.NaelMarker + 0.06f, () => AiMove.Create(Everyone(RimSpot(0f))).NaturalOrder(), jitter: 0f);
        for (var t = RunStarts; t < RunEnds; t += RunStep)
        {
            var progress = (t + RunStep - RunStarts) / SecondsPerSlot;
            ai.Move(t, () => AiMove.Create(Everyone(RimSpot(progress))).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(BackToCentre, () => AiMove.Create(Everyone(Vector2.Zero)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(LeaveCentreForPosts, () => AiMove.Create(BaitToTheLeftOfTwintania()).NaturalOrder(), jitter: 0f, sprint: true);
        if (tankLimitBreak) world.Events.Add(Scenario.StackMarkers + 0.03f, MainTankLimitBreak);
        ai.Move(Scenario.StackMarkers + 0.03f, () => AiMove.Create(TowerSoakersOnTheirTowers()).NaturalOrder(), jitter: 0f);
        ai.Move(tankLimitBreak ? LimitBreakCheck : Scenario.StackMarkers + 0.03f, () => AiMove.Create(StackersToTheirSpots()).NaturalOrder(), jitter: 0f);
        ai.Move(BaitLeaves, () => AiMove.Create(BaitToItsPost()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(StackSplits, () => AiMove.Create(StackSplitsForTheTwisters()).NaturalOrder(), jitter: 0f);
        ai.Move(TwisterTargetsStepOff, () => AiMove.Create(StepOffTheTwisters(towerSoakers: false)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(TowerTwisterTargetsStepOff, () => AiMove.Create(StepOffTheTwisters(towerSoakers: true)).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private int OppositeBahamutSkippingNael()
    {
        var opposite = Wrap(state.BahamutSlot + 4 * state.Step);
        return opposite == state.NaelSlot ? Wrap(opposite + state.Step) : opposite;
    }

    private Vector2 RimSpot(float slotsRun) => State.AtBearing((runFromSlot + state.Step * Math.Clamp(slotsRun, 0f, (RunEnds - RunStarts) / SecondsPerSlot)) * 45f, RimRadius);

    private float TwintaniaBearing => state.TwintaniaSlot * 45f;

    private Vector2 BaitSpot => State.AtBearing(TwintaniaBearing + BaitOffset, BaitRadius);

    private Vector2 StackSpot => State.AtBearing(TwintaniaBearing + StackOffset, StackRadius);

    private void MainTankLimitBreak() => world.Party.UseLimitBreak(PartyRole.MainTank);

    private Vector2?[] BaitToTheLeftOfTwintania()
    {
        var spots = new Vector2?[8];
        spots[(int)state.TwintaniaTarget] = BaitSpot;
        return spots;
    }

    private IReadOnlyList<PartyRole> TowerSoakers => Enum.GetValues<PartyRole>().Except(state.StackTargets).ToList();

    private Dictionary<PartyRole, Vector2> TowerPosts()
    {
        var towersFarthestFromTwintaniaFirst = State.TowerSpots.OrderByDescending(t => Vector2.Distance(t, State.SlotPosition(state.TwintaniaSlot))).ToList();
        var soakers = TowerSoakers.Where(r => r != state.TwintaniaTarget).ToList();
        if (TowerSoakers.Contains(state.TwintaniaTarget)) soakers.Add(state.TwintaniaTarget);
        return soakers.Select((role, i) => (role, towersFarthestFromTwintaniaFirst[i])).ToDictionary(p => p.role, p => p.Item2);
    }

    private Dictionary<PartyRole, Vector2> StackerPosts()
    {
        var stackers = state.StackTargets;
        if (stacksIgnored)
            return stackers.Select((role, i) => (role, State.AtBearing(SpreadBearings[i], SpreadRadius))).ToDictionary(p => p.role, p => p.Item2);
        return stackers.Select((role, i) => (role, StackSpot + StackHuddle[i])).ToDictionary(p => p.role, p => p.Item2);
    }

    private Vector2?[] TowerSoakersOnTheirTowers()
    {
        var spots = new Vector2?[8];
        foreach (var (role, post) in TowerPosts())
            if (role != state.TwintaniaTarget) spots[(int)role] = post;
        return spots;
    }

    private Vector2?[] StackersToTheirSpots()
    {
        stacksIgnored = tankLimitBreak && world.Party.ActiveMembers().Any(m => m.IsAlive() && LimitBreakStatuses.Any(m.HasStatus));
        var spots = new Vector2?[8];
        foreach (var (role, post) in StackerPosts())
            if (role != state.TwintaniaTarget) spots[(int)role] = post;
        return spots;
    }

    private Vector2?[] BaitToItsPost()
    {
        var spots = new Vector2?[8];
        var posts = TowerPosts().Concat(StackerPosts()).ToDictionary(p => p.Key, p => p.Value);
        spots[(int)state.TwintaniaTarget] = posts[state.TwintaniaTarget];
        return spots;
    }

    private Vector2?[] StackSplitsForTheTwisters()
    {
        var spots = new Vector2?[8];
        if (stacksIgnored) return spots;
        var stackers = state.StackTargets;
        var split = SplitFarthestFromTheTowers();
        for (var i = 0; i < stackers.Count; i++) spots[(int)stackers[i]] = split[i];
        return spots;
    }

    private IReadOnlyList<Vector2> SplitFarthestFromTheTowers()
        => new[] { 0f, MathF.PI / 4f }
            .Select(turn => Enumerable.Range(0, 4).Select(i => StackSpot + StackSpread * new Vector2(MathF.Cos(turn + i * MathF.PI / 2f), MathF.Sin(turn + i * MathF.PI / 2f))).ToList())
            .OrderByDescending(split => split.Min(spot => State.TowerSpots.Min(tower => Vector2.Distance(spot, tower))))
            .First();

    private Vector2?[] StepOffTheTwisters(bool towerSoakers)
    {
        var spots = new Vector2?[8];
        if (twisterSpots.Count == 0)
            twisterSpots = state.TwisterTargets
                .Select(r => world.Party.Get(r))
                .OfType<SimCharacter>()
                .Where(m => m.IsAlive())
                .Select(m => new Vector2(m.Position.X, m.Position.Z))
                .ToList();
        var soakers = TowerPosts().Keys.ToHashSet();
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (soakers.Contains(role) != towerSoakers || world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            var at = new Vector2(member.Position.X, member.Position.Z);
            if (twisterSpots.All(t => Vector2.Distance(t, at) > TwisterClearance)) continue;
            spots[(int)role] = FarthestFromTwisters(at, twisterSpots);
        }
        return spots;
    }

    private static Vector2 FarthestFromTwisters(Vector2 from, IReadOnlyList<Vector2> twisterSpots)
        => Enumerable.Range(0, 16)
            .Select(i => from + TwisterStep * new Vector2(MathF.Cos(i * MathF.PI / 8f), MathF.Sin(i * MathF.PI / 8f)))
            .Where(spot => spot.Length() <= MaxDodgeRadius)
            .OrderByDescending(spot => twisterSpots.Min(t => Vector2.Distance(t, spot)))
            .First();

    private static Vector2?[] Everyone(Vector2 centre)
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < spots.Length; i++) spots[i] = centre + new Vector2(0.6f * (i % 3 - 1), 0.6f * (i / 3 - 1));
        return spots;
    }

    private static int Wrap(int slot) => (slot % State.SlotCount + State.SlotCount) % State.SlotCount;
}
