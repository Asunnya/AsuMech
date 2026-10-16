using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

public sealed class DsrP5WrathAi : IScenarioAi<DsrP5WrathState>
{
    public string Name => "NA PF";

    private const float W = DsrP5WrathScenario.Wrath;
    private static readonly float[] PlainWallBearings = [66f, 78f, 90f, 102f, 114f];
    private const float PlainWallRadius = 18.5f;
    private const float TetherWallRadius = 19f;
    private const float DefamationBearing = -60f;
    private const float DefamationRadius = 20f;
    private static readonly float[] MarkedProteanAngles = [90f, 135f, 225f];
    private static readonly float[] PlainProteanAngles = [0f, 45f, 270f, 315f];
    private const float ProteanRadius = 10f;
    private const float BeyondWarriorProteanRadius = 15f;
    private const float DivebombAngle = 180f;
    private const float WallRadius = 19f;
    private const float EruptionStep = 30f;
    private const float LiquidHeavenStep = 24f;
    private const float LiquidHeavenWallRadius = 18f;
    private const float LiquidHeavenMagewardRadius = 13f;
    private const float CentreHuddleRadius = 1.2f;
    private static readonly Vector2[] Huddle = [new(5f, -2.5f), new(5f, 0f), new(5f, 2.5f), new(6f, -1.25f), new(6f, 1.25f), new(4f, 0f)];
    private static readonly Vector2 LightningSpot = new(13.5f, 3.5f);
    private static readonly Vector2 AfterLightning = new(6f, 1.8f);

    private DsrP5WrathState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, float> proteanAngle = [];

    public void Run(DsrP5WrathState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        proteanAngle.Clear();
        AssignProteanAngles();
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(MainTankSouthPartyNorth()).NaturalOrder());
        ai.Move(W + 0.3f, () => AiMove.Create(Stacked(new Vector2(0f, -2f))).NaturalOrder());
        ai.Move(W + 10.0f, () => AiMove.Create(SpreadForTethersDefamationAndTwisters()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(W + 16.05f, () => AiMove.Create(ProteanSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(W + 22.3f, () => AiMove.Create(UnbaitedToCentre()).NaturalOrder(), jitter: 0f);
        ai.Move(DsrP5WrathScenario.CauterizeCast + 0.15f, () => AiMove.Create(DivebombToCentre()).NaturalOrder(), jitter: 0f);
        var eruption = EruptionDrops();
        for (var i = 0; i < DsrP5WrathScenario.EruptionCasts.Length; i++)
        {
            var next = i + 1 < eruption.Count ? eruption[i + 1] : FinalSpot(state.Eruptions);
            ai.Move(DsrP5WrathScenario.EruptionCasts[i] + 0.05f, () => AiMove.Create(Only(state.Eruptions, next)).NaturalOrder(), jitter: 0f, sprint: true);
        }
        var liquid = LiquidHeavenDrops();
        for (var i = 0; i < DsrP5WrathScenario.LiquidHeavenDrops.Length; i++)
        {
            var next = i + 1 < liquid.Count ? liquid[i + 1] : FinalSpot(state.LiquidHeaven);
            ai.Move(DsrP5WrathScenario.LiquidHeavenDrops[i] + 0.05f, () => AiMove.Create(Only(state.LiquidHeaven, next)).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(W + 27.0f, () => AiMove.Create(UnbaitedUnderWarrior()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP5WrathScenario.ThunderstruckEnds + 0.3f, () => AiMove.Create(LightningBackUnderWarrior()).NaturalOrder(), jitter: 0f);
        ai.Move(W + 37.0f, () => AiMove.Create(MainTankAcrossFromParty()).NaturalOrder(), jitter: 0f);
    }

    private void AssignProteanAngles()
    {
        foreach (var (role, i) in state.Marked.Select((r, i) => (r, i)))
            proteanAngle[role] = MarkedProteanAngles[i];
        foreach (var (role, i) in state.Plain.OrderBy(r => r).Select((r, i) => (r, i)))
            proteanAngle[role] = PlainProteanAngles[i];
        proteanAngle[state.Divebomb] = DivebombAngle;
    }

    private static Vector2?[] MainTankSouthPartyNorth()
    {
        var spots = Stacked(new Vector2(0f, -4f));
        spots[(int)PartyRole.MainTank] = new Vector2(0f, 4f);
        return spots;
    }

    private Vector2?[] SpreadForTethersDefamationAndTwisters()
    {
        var spots = new Vector2?[8];
        spots[(int)state.Tethered[0]] = DragonFrame(DsrP5WrathState.DragoonOffset + 180f, TetherWallRadius);
        spots[(int)state.Tethered[1]] = DragonFrame(180f - DsrP5WrathState.DragoonOffset, TetherWallRadius);
        spots[(int)state.Defamation] = DragonFrame(DefamationBearing, DefamationRadius);
        var plain = state.Plain.Append(state.Divebomb).OrderBy(r => r).ToList();
        for (var i = 0; i < plain.Count; i++)
            spots[(int)plain[i]] = DragonFrame(PlainWallBearings[i], PlainWallRadius);
        return spots;
    }

    private Vector2?[] ProteanSpots()
    {
        var spots = new Vector2?[8];
        foreach (var (role, angle) in proteanAngle)
            spots[(int)role] = ProteanSpot(role, angle);
        return spots;
    }

    private Vector2 ProteanSpot(PartyRole role, float angle)
    {
        if (role == state.Divebomb) return WarriorFrame(WallRadius, angle);
        return WarriorFrame(angle == 0f ? BeyondWarriorProteanRadius : ProteanRadius, angle);
    }

    private Vector2?[] UnbaitedToCentre()
    {
        var spots = new Vector2?[8];
        foreach (var (role, angle) in proteanAngle)
            if (!IsBaiter(role) && role != state.Divebomb)
                spots[(int)role] = WarriorFrame(CentreHuddleRadius, angle);
        return spots;
    }

    private Vector2?[] DivebombToCentre() => Only(state.Divebomb, WarriorFrame(CentreHuddleRadius, DivebombAngle));

    private Vector2?[] UnbaitedUnderWarrior()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => !IsBaiter(r)))
            spots[(int)role] = FinalSpot(role);
        return spots;
    }

    private Vector2?[] LightningBackUnderWarrior()
    {
        var spots = new Vector2?[8];
        foreach (var role in state.Lightning)
            spots[(int)role] = Local(new Vector2(AfterLightning.X, LightningSide(role) * AfterLightning.Y));
        return spots;
    }

    private Vector2?[] MainTankAcrossFromParty() => Only(PartyRole.MainTank, WarriorFrame(4f, 180f));

    private bool IsBaiter(PartyRole role) => role == state.Eruptions || role == state.LiquidHeaven;

    private List<Vector2> EruptionDrops()
    {
        var start = proteanAngle[state.Eruptions];
        var towardWarrior = start < 180f ? -1f : 1f;
        return [ProteanSpot(state.Eruptions, start), WarriorFrame(WallRadius, start),
            WarriorFrame(WallRadius, start + towardWarrior * EruptionStep), WarriorFrame(WallRadius, start + towardWarrior * 2f * EruptionStep)];
    }

    private List<Vector2> LiquidHeavenDrops()
    {
        var start = proteanAngle[state.LiquidHeaven];
        var eruptionFlank = MathF.Sin(proteanAngle[state.Eruptions] * MathF.PI / 180f) > 0f ? 1f : -1f;
        var sweep = -eruptionFlank;
        if (state.Lightning.Contains(state.LiquidHeaven) && start != 0f) sweep = Signed(start) > 0f ? -1f : 1f;
        var mageward = MathF.Abs(Signed(start + sweep * LiquidHeavenStep * 4f)) > MathF.Abs(Signed(start));
        var radius = mageward ? LiquidHeavenMagewardRadius : LiquidHeavenWallRadius;
        var drops = new List<Vector2> { ProteanSpot(state.LiquidHeaven, start) };
        for (var k = 1; k <= 4; k++)
            drops.Add(WarriorFrame(radius, start + sweep * LiquidHeavenStep * k));
        return drops;
    }

    private Vector2 FinalSpot(PartyRole role)
    {
        if (state.Lightning.Contains(role))
            return Local(new Vector2(LightningSpot.X, LightningSide(role) * LightningSpot.Y));
        if (IsBaiter(role))
        {
            var last = WarriorLocal(role == state.Eruptions ? EruptionDrops()[^1] : LiquidHeavenDrops()[^1]);
            return Local(new Vector2(Math.Clamp(last.X, 4.5f, 7.5f), Math.Clamp(last.Y, -3f, 3f)));
        }
        var huddled = Enum.GetValues<PartyRole>().Where(r => !IsBaiter(r) && !state.Lightning.Contains(r)).ToList();
        return Local(Huddle[huddled.IndexOf(role)]);
    }

    private float LightningSide(PartyRole role)
    {
        var first = state.Lightning.OrderBy(r => IsBaiter(r) ? 0 : 1).ThenBy(r => r).First();
        var firstSide = IsBaiter(first) ? BaitSide(first) : 1f;
        return role == first ? firstSide : -firstSide;
    }

    private float BaitSide(PartyRole role)
    {
        var last = WarriorLocal(role == state.Eruptions ? EruptionDrops()[^1] : LiquidHeavenDrops()[^1]);
        return last.Y < 0f ? -1f : 1f;
    }

    private Vector2 DragonFrame(float relativeBearing, float radius) => Flat(AtBearing(state.DragonBearing + relativeBearing, radius));

    private Vector2 WarriorFrame(float radius, float angle) => Flat(AtBearing(state.WarriorBearing + angle, radius));

    private Vector2 Local(Vector2 frame)
    {
        var u = Flat(AtBearing(state.WarriorBearing, 1f));
        var v = Flat(AtBearing(state.WarriorBearing + 90f, 1f));
        return u * frame.X + v * frame.Y;
    }

    private Vector2 WarriorLocal(Vector2 world2)
    {
        var u = Flat(AtBearing(state.WarriorBearing, 1f));
        var v = Flat(AtBearing(state.WarriorBearing + 90f, 1f));
        return new Vector2(Vector2.Dot(world2, u), Vector2.Dot(world2, v));
    }

    private static float Signed(float angle) => ((angle + 180f) % 360f + 360f) % 360f - 180f;

    private static Vector2?[] Only(PartyRole role, Vector2 spot)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = spot;
        return spots;
    }

    private static Vector2?[] Stacked(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1)))).ToArray();

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
