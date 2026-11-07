using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using State = AsuMech.Scenarios.Ucob.P2Nael.UcobP2NaelState;
using Scenario = AsuMech.Scenarios.Ucob.P2Nael.UcobP2NaelScenario;

namespace AsuMech.Scenarios.Ucob.P2Nael;

public sealed class UcobP2NaelAi : IScenarioAi<UcobP2NaelState>
{
    public string Name => "Stack north of Nael, rotate clockwise, fire in-out-in-in";

    private static readonly Dictionary<PartyRole, float> SpreadDegrees = new()
    {
        [MainTank] = 0f, [OffTank] = 180f, [RegenHealer] = 135f, [ShieldHealer] = 225f,
        [MeleeDpsA] = 90f, [MeleeDpsB] = 270f, [PhysRangedDps] = 45f, [CasterDps] = 315f,
    };
    private static readonly Vector2 PreHeavensfallStack = new(0f, 4.5f);
    private const float SpreadRadius = 12f;
    private const float DiveRadius = 9f;
    private const float StackRadius = 4.8f;
    private static readonly float[] DanceDegrees = [0f, 90f, -75f, 45f, -40f, 135f, -120f];
    private const float CloseEarRadius = 5.2f;
    private const float FarEarRadius = 11f;
    private static readonly float[] FarEarDegrees = [-35f, 35f];
    private const float FireOutRadius = 11f;
    private const float ChariotRadius = 11f;
    private const float ClockRadius = 5f;
    private const float RavenDynamoDistance = 2f;
    private const float RavenChariotDistance = 11.5f;
    private const float PuddleClearance = 6f;
    private const float DodgeTrigger = 5.2f;
    private const float EarClearance = 6.5f;
    private const float DoomWalk = 4f;
    private const float CleanseSearchRadius = 3f;
    private const float WalkRecheck = 0.4f;
    private const float StackLead = 2.6f;
    private const float After = 0.05f;

    private State state = null!;
    private SimWorld world = null!;
    private Vector2 nael = new(0f, 9f);
    private int danceStep;
    private float danceRadius = StackRadius;
    private Vector2? ravenSpot;
    private readonly Dictionary<PartyRole, (Vector2 Spot, float From, float Until)> dodges = [];
    private readonly List<(PartyRole Role, Vector2 Circle, float From, float Until)> doomWalks = [];

    public void Run(UcobP2NaelState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        nael = new Vector2(0f, 9f);
        ResetDance();
        ravenSpot = null;
        dodges.Clear();
        doomWalks.Clear();
        fixedEars.Clear();
        livePuddles.Clear();
        var ai = new AiManager(world);

        var times = new SortedSet<float> { 0.3f, Scenario.Heavensfall + 0.2f, Scenario.ThermionicBurstCasts[0] + Scenario.ThermionicBurstCastSeconds + After };
        ai.Move(Scenario.DalamudDive + After, () => { nael = Flat(world.Party.Get(MainTank)?.Position) ?? nael; return Formation(); }, jitter: 0f, sprint: true);
        times.Add(Scenario.DalamudDive + 0.15f);

        for (var i = 0; i < Scenario.ThunderMarks.Length; i++)
        {
            times.Add(ThunderEarsFrom(i));
            times.Add(ThunderSplash(i) + After);
        }
        foreach (var fire in new[] { 0, 2, 3 })
        {
            times.Add(Fireball(fire) - StackLead);
            times.Add(Fireball(fire) + After);
        }
        times.Add(Scenario.FireTethers[State.FireTakenAlone] + After);
        times.Add(Fireball(State.FireTakenAlone) + After);
        times.Add(FireballAloneAgainFrom);
        for (var q = 0; q < Scenario.Quotes.Length; q++)
        {
            times.Add(Scenario.Quotes[q] + 0.2f);
            foreach (var at in Scenario.QuoteMoves[q])
            {
                times.Add(at - StackLead);
                times.Add(at + After);
            }
        }
        times.Add(DanceStarts[1]);
        times.Add(DanceStarts[2]);
        ai.Move(Scenario.QuoteMoves[2][0] + After, () =>
        {
            var home = nael;
            nael = Flat(world.Party.Get(state.RavenDiveTarget)?.Position) ?? nael;
            ravenSpot = nael + Vector2.Normalize(home - nael + new Vector2(0.001f, 0f)) * (state.Quotes[2].Second == NaelMove.LunarDynamo ? RavenDynamoDistance : RavenChariotDistance);
            return Formation();
        }, jitter: 0f, sprint: true);
        ai.Move(DanceStarts[2], () => { ravenSpot = null; ResetDance(); return Formation(); }, jitter: 0f, sprint: true);
        ai.Move(DanceStarts[1], () => { ResetDance(); return Formation(); }, jitter: 0f, sprint: true);
        ai.Move(StackRegroupsAfterFirstDance, () => { ResetDance(); return Formation(); }, jitter: 0f, sprint: true);

        for (var set = 0; set < Scenario.WingsCasts.Length; set++)
        {
            for (var i = 0; i < Scenario.WingsCasts[set].Length; i++)
            {
                var (s, n) = (set, i);
                var cast = Scenario.WingsCasts[set][i];
                ai.Move(cast + 0.08f, () => DodgeWings(s, n, cast), jitter: 0f, sprint: true);
                times.Add(cast + Scenario.WingsCastSeconds + After);
                var go = CleanseWalkFrom(cast);
                for (var t = go; t <= go + DoomWalk + 0.01f; t += WalkRecheck) times.Add(t);
            }
        }

        foreach (var at in times)
            ai.Move(at, () => AiMove.Create(Spots(world.Events.Elapsed)).NaturalOrder(), jitter: 0f, sprint: true);

        if (state.Push == NaelPush.NoPush) BaitTheDivebombs(ai);
    }

    private const float BaitRadius = 21f;
    private const float StagingRadius = 14f;
    private const float StepOffThePuddle = 7f;
    private const float LongestDashToTheBaitSpot = 10f;
    private const float FirstStagingSwing = 25f;
    private const float CentreClusterRadius = 1.5f;
    private const float RegroupBeforeTheQuote = 1.5f;
    private const float WaitingShare = 0.6f;
    private const float SpreadOutRadius = 7f;
    private const float TankDiveRadius = 12f;
    private const float PathClearance = 1.5f;
    private const float HalfSlot = 22.5f;
    private const float MeetTheLastBaitRadius = 6f;
    private static readonly float[] SingleDragonOffsets = [35f, -35f, 25f, -25f, 50f, -50f, 15f, -15f, 60f, -60f, 70f, -70f, 80f, -80f];
    private static readonly float[] StagingSwings = [30f, -30f, 40f, -40f, 20f, -20f, 50f, -50f, 60f, -60f];

    private readonly Vector2[] baitSpots = new Vector2[State.Divebombs];
    private readonly Vector2[] stagingSpots = new Vector2[State.Divebombs];
    private readonly Vector2[] sideSteps = new Vector2[State.Divebombs];

    private void BaitTheDivebombs(AiManager ai)
    {
        PlanTheBaitRoutes();
        var everyone = Enum.GetValues<PartyRole>();
        ai.Move(Scenario.DivebombQuoteAt, () => AiMove.Create(Cluster(Vector2.Zero, everyone)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombMarkers[0] + After, () => Send(Bait(0), stagingSpots[0]), jitter: 0f, sprint: true);
        ai.Move(Scenario.Hypernovas[0] + After, () => Send(Bait(0), baitSpots[0]), jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombMarkers[1] + After, () => Send(Bait(1), stagingSpots[1] * WaitingShare), jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombLocks[0] + After, () => Send(Bait(0), Vector2.Zero), jitter: 0f, sprint: true);
        ai.Move(Scenario.Hypernovas[1] + After, () => Send(Bait(1), stagingSpots[1]), jitter: 0f, sprint: true);
        ai.Move(Scenario.Hypernovas[2] + After, () => Send(Bait(1), sideSteps[1]), jitter: 0f, sprint: true);
        ai.Move(Scenario.Hypernovas[3] + After, () => Send(Bait(1), baitSpots[1]), jitter: 0f, sprint: true);
        ai.Move(Scenario.Hypernovas[3] + After, () => Send(Bait(2), baitSpots[2]), jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombLocks[1] + After, () => Send(Bait(1), Vector2.Zero), jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombLocks[1] + RegroupBeforeTheQuote, OutForTheFirstQuoteMove, jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombLocks[1] + Scenario.CauterizeCastSeconds + After, InForTheSecondQuoteMove, jitter: 0f, sprint: true);
        ai.Move(Scenario.DivebombQuoteMoves[1] + After, () => AiMove.Create(Cluster(Vector2.Zero, everyone)).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private PartyRole Bait(int dive) => state.DivebombTargets[dive];

    private static ISwapStep Send(PartyRole role, Vector2 spot)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = spot;
        return AiMove.Create(spots).NaturalOrder();
    }

    private void PlanTheBaitRoutes()
    {
        var dives = state.DiveDragons;
        baitSpots[0] = Compass(MidwayClockwise(dives[0][0], dives[0][1])) * BaitRadius;
        stagingSpots[0] = Compass(CompassDegrees(baitSpots[0]) + FirstStagingSwing) * StagingRadius;
        sideSteps[0] = baitSpots[0];
        var betweenTheLastDragons = Compass(MidwayClockwise(dives[2][0], dives[2][1])) * BaitRadius;
        var pastTheLastDragon = Compass(dives[2][1] * 45f + HalfSlot) * BaitRadius;
        var best = SecondBaitRoute(betweenTheLastDragons);
        if (best.Score < PathClearance && SecondBaitRoute(pastTheLastDragon) is var past && past.Score > best.Score) best = past;
        (baitSpots[1], stagingSpots[1], sideSteps[1], baitSpots[2]) = (best.Second.Bait, best.Second.Staging, best.Second.SideStep, best.ThirdBait);
    }

    private (float Score, BaitRoute Second, Vector2 ThirdBait) SecondBaitRoute(Vector2 thirdBait)
    {
        var dives = state.DiveDragons;
        var firstLines = LinesOf(dives[0], baitSpots[0]);
        var best = (Score: float.MinValue, Second: default(BaitRoute), ThirdBait: thirdBait);
        foreach (var offset in SingleDragonOffsets)
        {
            var secondBait = Compass(dives[1][0] * 45f + offset) * BaitRadius;
            var second = BestRoute(secondBait, firstLines, [stagingSpots[0], baitSpots[0]]);
            var secondLines = LinesOf(dives[1], secondBait);
            var hard = MathF.Min(OutsideLines(firstLines, secondBait, secondBait), OutsideLines(secondLines, Vector2.Zero, Vector2.Zero) - CentreClusterRadius);
            var score = hard < PathClearance ? hard - 100f : MathF.Min(second.Clearance, MathF.Min(
                OutsideLines(secondLines, Vector2.Zero, thirdBait),
                AwayFromPuddles([stagingSpots[0], baitSpots[0], second.Staging, second.SideStep], Vector2.Zero, thirdBait)));
            if (score > best.Score) best = (score, second, thirdBait);
            if (score >= PathClearance) break;
        }
        return best;
    }

    private readonly record struct BaitRoute(Vector2 Bait, Vector2 Staging, Vector2 SideStep, float Clearance);

    private static BaitRoute BestRoute(Vector2 bait, IReadOnlyList<(Vector2 From, Vector2 Aim)> earlierLines, IReadOnlyList<Vector2> earlierPuddles)
    {
        var best = new BaitRoute(bait, bait, bait, float.MinValue);
        foreach (var swing in StagingSwings)
        {
            var staging = Compass(CompassDegrees(bait) + swing) * StagingRadius;
            for (var degrees = 0f; degrees < 360f; degrees += 30f)
            {
                var sideStep = staging + Compass(degrees) * StepOffThePuddle;
                var withStaging = earlierPuddles.Append(staging).ToList();
                var clearance = new[]
                {
                    OutsideLines(earlierLines, sideStep, bait),
                    LongestDashToTheBaitSpot - Vector2.Distance(sideStep, bait),
                    BaitRadius + 0.5f - sideStep.Length(),
                    sideStep.Length() - (SpreadOutRadius + State.HypernovaPuddleRadius + 1f),
                    AwayFromPuddles(earlierPuddles, Vector2.Zero, staging),
                    AwayFromPuddles(earlierPuddles, staging, sideStep),
                    AwayFromPuddles(withStaging, sideStep, bait),
                    AwayFromPuddles(withStaging.Append(sideStep).ToList(), bait, Vector2.Zero),
                }.Min();
                if (clearance > best.Clearance) best = new BaitRoute(bait, staging, sideStep, clearance);
            }
        }
        return best;
    }

    private static List<(Vector2 From, Vector2 Aim)> LinesOf(IEnumerable<int> slots, Vector2 aim)
        => slots.Select(slot => (State.DragonPosition(slot), aim)).ToList();

    private static float OutsideLines(IEnumerable<(Vector2 From, Vector2 Aim)> lines, Vector2 start, Vector2 end)
        => !lines.Any() ? float.MaxValue : lines.Min(line => Walk(start, end).Min(at => OutsideLine(line.From, line.Aim, at)));

    private static float OutsideLine(Vector2 from, Vector2 aim, Vector2 at)
    {
        var along = Vector2.Normalize(aim - from);
        var offset = at - from;
        var ahead = Vector2.Dot(offset, along);
        var aside = MathF.Abs(along.X * offset.Y - along.Y * offset.X);
        return MathF.Max(aside - State.CauterizeHalfWidth, MathF.Max(-ahead, ahead - State.CauterizeLength));
    }

    private static float AwayFromPuddles(IReadOnlyList<Vector2> puddles, Vector2 start, Vector2 end)
        => puddles.Count == 0 ? float.MaxValue : puddles.Min(p => Walk(start, end).Min(at => Vector2.Distance(at, p))) - State.HypernovaPuddleRadius;

    private static IEnumerable<Vector2> Walk(Vector2 start, Vector2 end)
    {
        var steps = Math.Max(1, (int)(Vector2.Distance(start, end) / 0.5f));
        for (var i = 0; i <= steps; i++) yield return Vector2.Lerp(start, end, i / (float)steps);
    }

    private ISwapStep OutForTheFirstQuoteMove()
    {
        var (spread, tank) = SpreadAndTankDiveBearings();
        var spots = Cluster(Vector2.Zero, Enum.GetValues<PartyRole>().Where(r => r != MainTank));
        spots[(int)MainTank] = Compass(tank) * TankDiveRadius;
        if (state.DivebombQuote.First == NaelMove.MeteorStream)
            for (var i = 0; i < state.DivebombSpreads.Count; i++)
                spots[(int)state.DivebombSpreads[i]] = Compass(spread + 90f * i) * SpreadOutRadius;
        spots[(int)Bait(2)] = null;
        return AiMove.Create(spots).NaturalOrder();
    }

    private ISwapStep InForTheSecondQuoteMove()
    {
        var stackOnTheLastBait = state.DivebombQuote.Second == NaelMove.ThermionicBeam && state.DivebombStackTarget == Bait(2);
        var meet = stackOnTheLastBait ? Vector2.Normalize(baitSpots[2]) * MeetTheLastBaitRadius : Vector2.Zero;
        var spots = Cluster(meet, Enum.GetValues<PartyRole>());
        if (state.DivebombQuote.Second == NaelMove.DalamudDive) spots[(int)MainTank] = null;
        return AiMove.Create(spots).NaturalOrder();
    }

    private (float Spread, float Tank) SpreadAndTankDiveBearings()
    {
        var puddles = new[] { stagingSpots[0], baitSpots[0], stagingSpots[1], sideSteps[1] };
        var secondLines = LinesOf(state.DiveDragons[1], baitSpots[1]);
        var best = (Score: float.MinValue, Spread: 0f, Tank: 0f);
        foreach (var spread in new[] { 0f, 45f, 22.5f, 67.5f })
            for (var k = 0; k < 4; k++)
            {
                var tank = spread + 45f + 90f * k;
                var tankSpot = Compass(tank) * TankDiveRadius;
                var spreads = Enumerable.Range(0, 4).Select(i => Compass(spread + 90f * i) * SpreadOutRadius).ToList();
                var score = new[]
                {
                    AwayFromPuddles(puddles, Vector2.Zero, tankSpot),
                    OutsideLines(secondLines, tankSpot, tankSpot),
                    Walk(baitSpots[2], Vector2.Zero).Min(at => Vector2.Distance(at, tankSpot)) - State.DalamudDiveRadius,
                    spreads.Min(at => MathF.Min(AwayFromPuddles(puddles, Vector2.Zero, at), OutsideLines(secondLines, at, at))),
                }.Min();
                if (score > best.Score) best = (score, spread, tank);
            }
        return (best.Spread, best.Tank);
    }

    private static float MidwayClockwise(int firstSlot, int secondSlot) => (firstSlot + secondSlot) * 45f / 2f;

    private static Vector2 Compass(float degrees)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(r), -MathF.Cos(r));
    }

    private static float CompassDegrees(Vector2 at) => MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI;

    private static readonly float[] DanceStarts = [Scenario.QuoteMoves[0][1] + After, Scenario.QuoteMoves[1][1] + After, Fireball(3) + After];
    private const float FireballAloneAgainFrom = 89.0f;
    private const float StackRegroupsAfterFirstDance = 57.8f;

    private static float Fireball(int i) => Scenario.FireTethers[i] + Scenario.FireballAfterTether;
    private static float ThunderSplash(int i) => Scenario.ThunderMarks[i] + Scenario.ThunderstruckAfterMark + Scenario.ThunderstruckSeconds;
    private static float ThunderEarsFrom(int i) => i == 3 ? Fireball(3) + After : Scenario.ThunderMarks[i] + Scenario.ThunderstruckAfterMark + After;

    private static float CleanseWalkFrom(float cast)
    {
        var go = cast + Scenario.WingsCastSeconds + 0.08f;
        for (var i = 0; i < Scenario.ThunderMarks.Length; i++)
            if (ThunderSplash(i) is var splash && splash >= go - 1f && splash <= go + 1.2f) go = splash + 0.15f;
        return go;
    }

    private void ResetDance()
    {
        danceStep = 0;
        danceRadius = StackRadius;
    }

    private ISwapStep Formation() => AiMove.Create(Spots(world.Events.Elapsed)).NaturalOrder();

    private ISwapStep DodgeWings(int set, int index, float cast)
    {
        var now = world.Events.Elapsed;
        var spots = Spots(now);
        if (world.Party.Get(state.WingsTargets[set][index]) is { } target && target.IsAlive())
        {
            var puddle = Flat(target.Position)!.Value;
            var until = cast + Scenario.WingsCastSeconds + After;
            livePuddles.Add((puddle, until));
            if (Vector2.Distance(puddle, Cluster()) <= PuddleClearance) StepTheStackAwayFrom(puddle, now);
            var later = Spots(until - 0.3f);
            foreach (var role in Enum.GetValues<PartyRole>())
            {
                if (later[(int)role] is not { } walking) continue;
                var spot = Spot(role, until - 0.3f, afterWalk: true);
                if (IsInCluster(spot)) continue;
                var at = Flat(world.Party.Get(role)?.Position) ?? spot;
                if (new[] { walking, spot, at }.All(p => Vector2.Distance(p, puddle) > DodgeTrigger)) continue;
                var from = now;
                while (from < until && IsInCluster(Spot(role, from)))
                    from += 0.1f;
                dodges[role] = (StepClockwise(spot, puddle), from, until);
            }
            var circle = Array.IndexOf(Scenario.WingsCasts[set], cast);
            if (circle < state.DoomTargets[set].Count)
            {
                var go = CleanseWalkFrom(cast);
                doomWalks.Add((state.DoomTargets[set][circle], puddle, go, go + DoomWalk));
            }
        }
        return AiMove.Create(Spots(now)).NaturalOrder();
    }

    private void StepTheStackAwayFrom(Vector2 puddle, float now)
    {
        var ears = fixedEars.Where(e => e.Value.Until > now).Select(e => e.Value.Spot).ToList();
        var best = (Score: float.MinValue, Step: 0, Radius: StackRadius);
        foreach (var radius in new[] { StackRadius, StackRadius + 2.5f })
            for (var step = 0; step < DanceDegrees.Length; step++)
            {
                var spot = nael + Rotate(Inward, DanceDegrees[step]) * radius;
                var score = MathF.Min(Vector2.Distance(spot, puddle) - (PuddleClearance + 0.7f),
                    ears.Select(e => Vector2.Distance(spot, e) - EarClearance).DefaultIfEmpty(10f).Min());
                if (radius > StackRadius) score -= 0.5f;
                if (score > best.Score) best = (score, step, radius);
            }
        (danceStep, danceRadius) = (best.Step, best.Radius);
    }

    private Vector2 CleanseLaidAt(Vector2 expected)
        => world.Children.OfType<SimEventObject>()
            .Where(e => e.IsActive && e.SpawnConfig.EObjId == UcobConstants.EObjId.WingsOfSalvationCleanse)
            .Select(e => new Vector2(e.Position.X, e.Position.Z))
            .Where(at => Vector2.Distance(at, expected) <= CleanseSearchRadius)
            .OrderBy(at => Vector2.Distance(at, expected))
            .Cast<Vector2?>()
            .FirstOrDefault() ?? expected;

    private bool StillDoomed(PartyRole role, float at)
        => at > world.Events.Elapsed + 0.05f || world.Party.Get(role) is not { } member || member.HasStatus(UcobConstants.StatusId.Doom);

    private bool IsInCluster(Vector2 spot) => Vector2.Distance(spot, Cluster()) <= 1f;

    private Vector2 StepClockwise(Vector2 spot, Vector2 puddle)
    {
        var from = Vector2.Normalize(spot - nael + new Vector2(0.001f, 0f));
        for (var radius = MathF.Max(Vector2.Distance(spot, nael), StackRadius); radius < 20f; radius += 2.5f)
            for (var degrees = 20f; degrees <= 180f; degrees += 10f)
            {
                var candidate = nael + Rotate(from, degrees) * radius;
                if (Vector2.Distance(candidate, puddle) >= PuddleClearance) return candidate;
            }
        return spot;
    }

    private Vector2?[] Spots(float now)
    {
        if (now < Scenario.Heavensfall + 0.1f) return Cluster(PreHeavensfallStack, Enum.GetValues<PartyRole>());
        if (now < Scenario.DalamudDive)
        {
            var secondSet = now >= Scenario.ThermionicBurstCasts[0] + Scenario.ThermionicBurstCastSeconds;
            var spots = new Vector2?[8];
            foreach (var (role, degrees) in SpreadDegrees)
            {
                var radius = secondSet && role == MainTank ? DiveRadius : SpreadRadius;
                spots[(int)role] = Bearing(degrees + (secondSet ? 0f : 22.5f)) * radius;
            }
            return spots;
        }

        var result = new Vector2?[8];
        foreach (var group in Enum.GetValues<PartyRole>().GroupBy(role => OffOthersCircles(role, Spot(role, now), now)))
            Place(result, Cluster(group.Key, group));
        return result;
    }

    private Vector2 Spot(PartyRole role, float now, bool afterWalk = false)
    {
        foreach (var walk in doomWalks)
            if (!afterWalk && walk.Role == role && now >= walk.From && now < walk.Until && StillDoomed(role, now)) return CleanseLaidAt(walk.Circle);
        if (dodges.TryGetValue(role, out var dodge) && now >= dodge.From && now < dodge.Until) return dodge.Spot;
        var alone = state.FireTargets[State.FireTakenAlone];
        if (role == alone && ((now >= Scenario.FireTethers[State.FireTakenAlone] && now < Fireball(State.FireTakenAlone) + After)
                              || (now >= FireballAloneAgainFrom && now < Fireball(2) + After)))
        {
            foreach (var walk in doomWalks)
                if (walk.Role == role && walk.From >= FireballAloneAgainFrom - 3f && walk.From <= Fireball(2))
                    return nael + Vector2.Normalize(walk.Circle - nael + new Vector2(0.001f, 0f)) * FireOutRadius;
            return nael + Vector2.Normalize(nael - Cluster() + new Vector2(0.001f, 0f)) * FireOutRadius;
        }

        for (var i = 0; i < Scenario.ThunderMarks.Length; i++)
        {
            var holders = state.ThunderTargets[i];
            var index = holders.ToList().IndexOf(role);
            if (index < 0 || now < ThunderEarsFrom(i)) continue;
            if (now < ThunderSplash(i) + After) return Ear(i, index, now);
        }
        var q = Array.FindIndex(Scenario.QuoteMoves, moves => now >= moves[0] - 10f && now < moves[1] + After);
        if (q == 2)
        {
            if (now < Scenario.QuoteMoves[2][0] + After)
                return nael + Rotate(Inward, ClockDegrees(role)) * ClockRadius;
            if (ravenSpot is { } raven) return raven;
        }
        if (ravenSpot is { } fourthFire) return fourthFire;
        if (q >= 0)
        {
            var moves = Scenario.QuoteMoves[q];
            var next = now < moves[0] + After ? 0 : 1;
            var move = next == 0 ? state.Quotes[q].First : state.Quotes[q].Second;
            var at = moves[next];
            if (now >= at - StackLead)
            {
                if (move == NaelMove.IronChariot) return nael + Inward * ChariotRadius;
                if (move == NaelMove.ThermionicBeam) return Cluster();
            }
        }
        foreach (var fire in new[] { 0, 2, 3 })
            if (now >= Fireball(fire) - StackLead && now < Fireball(fire) + After) return Cluster();
        return Cluster();
    }

    private Vector2 OffOthersCircles(PartyRole role, Vector2 spot, float now)
    {
        if (now > world.Events.Elapsed + 0.05f || world.Party.Get(role) is not { } member || !member.HasStatus(UcobConstants.StatusId.Doom)) return spot;
        var from = Flat(member.Position) ?? spot;
        foreach (var walk in doomWalks)
        {
            if (walk.Role == role || now < walk.From - 0.7f || now > walk.Until) continue;
            if (Vector2.Distance(spot, walk.Circle) <= State.CleanseRadius + 1f)
                return walk.Circle + Vector2.Normalize(spot - walk.Circle + new Vector2(0.001f, 0f)) * (State.CleanseRadius + 1.5f);
            if (DistanceToSegment(walk.Circle, from, spot) <= State.CleanseRadius + 1f)
                return walk.Circle + Vector2.Normalize(from - walk.Circle + new Vector2(0.001f, 0f)) * (State.CleanseRadius + 1.5f);
        }
        return spot;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = ab.LengthSquared() < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(point - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(point, a + ab * t);
    }

    private Vector2 Ear(int set, int holder, float now)
    {
        if (set == 0) return nael + Rotate(Inward, holder == 0 ? -90f : 90f) * CloseEarRadius;
        if (fixedEars.TryGetValue((set, holder), out var known)) return known.Spot;
        var away = Vector2.Normalize(nael - Cluster() + new Vector2(0.001f, 0f));
        var sign = FarEarDegrees[holder] < 0f ? -1f : 1f;
        var taken = fixedEars.Where(e => e.Key.Set == set).Select(e => e.Value.Spot).ToList();
        var avoid = new List<Vector2> { Cluster() };
        avoid.AddRange(livePuddles.Where(p => p.Until > now).Select(p => p.At));
        var ear = nael + Rotate(away, FarEarDegrees[holder]) * FarEarRadius;
        for (var degrees = MathF.Abs(FarEarDegrees[holder]); degrees <= 180f; degrees += 15f)
        {
            var candidate = nael + Rotate(away, sign * degrees) * FarEarRadius;
            if (avoid.All(a => Vector2.Distance(candidate, a) >= EarClearance) && taken.All(t => Vector2.Distance(candidate, t) >= 3f))
            {
                ear = candidate;
                break;
            }
        }
        fixedEars[(set, holder)] = (ear, ThunderSplash(set) + After);
        return ear;
    }

    private readonly Dictionary<(int Set, int Holder), (Vector2 Spot, float Until)> fixedEars = [];
    private readonly List<(Vector2 At, float Until)> livePuddles = [];

    private static float ClockDegrees(PartyRole role) => role switch
    {
        MainTank => 180f, OffTank => 0f, RegenHealer => 45f, ShieldHealer => 315f,
        MeleeDpsA => 90f, MeleeDpsB => 270f, PhysRangedDps => 135f, _ => 225f,
    };

    private Vector2 Cluster() => DanceSpot(danceStep);

    private Vector2 DanceSpot(int step) => nael + Rotate(Inward, DanceDegrees[step]) * danceRadius;

    private Vector2 Inward => nael.LengthSquared() < 0.01f ? new Vector2(0f, -1f) : Vector2.Normalize(-nael);

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(v.X * MathF.Cos(r) - v.Y * MathF.Sin(r), v.X * MathF.Sin(r) + v.Y * MathF.Cos(r));
    }

    private static Vector2 Bearing(float degrees)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(r), MathF.Cos(r));
    }

    private static Vector2? Flat(System.Numerics.Vector3? at) => at is { } p ? new Vector2(p.X, p.Z) : null;

    private static void Place(Vector2?[] into, Vector2?[] from)
    {
        for (var i = 0; i < into.Length; i++)
            if (from[i] is { } spot) into[i] = spot;
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
