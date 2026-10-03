using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

public sealed class DsrP2ThordanAi : IScenarioAi<DsrP2ThordanState>
{
    public string Name => "NAUR (PF)";

    private const float MercyDodgeDegrees = 60f;
    private const float WardMercyDodgeDegrees = 55f;
    private const float OuterRingDodgeDegrees = 32f;
    private const float HeavyImpactRingWidth = 6f;
    private const float MinSpreadDistanceFromGuerrique = 6.5f;
    private const float TankSpreadRadius = 11.8f;
    private const float SpreadSpacing = 5.1f;
    private const float DpsSpreadRadius = 19.5f;
    private const float DpsSpreadDegrees = 16f;
    private const float WallRadius = 20f;
    private const float StackRadius = 19.5f;
    private const float TankTetherStartRadius = 11.5f;
    private const float TankBesideThordanDegrees = 17f;
    private const float AfterLightningStorm = 45.60f;
    private static readonly float[] HeavyImpactWaves = [45.36f, 47.28f, 49.14f, 51.02f, 52.89f];
    private static readonly float[] StackerOffsetDegrees = [-3f, 0f, 3f];
    private const float PlungeGroupRadius = 20f;
    private const float PlungeGroupOffsetDegrees = 12f;
    private const float BrightsphereDodgeDegrees = 32f;
    private const float MeteorPairOffset = 0.7f;
    private const float MeteorSweepRadius = 19f;
    private const float MeteorSweepDegrees = 130f;
    private const float KnockbackRideRadius = 2.3f;
    private static readonly float[] HolyCometDrops = [136.80f, 138.23f, 139.66f, 141.08f, 142.50f, 143.93f, 145.36f];
    private static readonly Vector2 BroadSwingThordan = new(0f, -9f);
    private const float BroadSwingDodgeDistance = 4f;
    private const float BehindThordanDistance = 2.5f;
    private const float TetherGrabDistance = 1.5f;

    private DsrP2ThordanState state = null!;
    private SimWorld world = null!;

    public void Run(DsrP2ThordanState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(MainTankNorthPartyStacksSouth(0f)).NaturalOrder());
        ai.Move(14.0f, () => AiMove.Create(MainTankNorthPartyStacksSouth(MercyDodgeDegrees)).NaturalOrder(), jitter: 0.05f);
        ai.Move(16.2f, () => AiMove.Create(MainTankNorthPartyStacksSouth(0f)).NaturalOrder());

        ai.Move(37.4f, () => AiMove.Create(LightPartiesSpreadOnSafeLine()).NaturalOrder(), jitter: 0f);
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var wave = HeavyImpactWave(SpreadSpot(role));
            if (wave == HeavyImpactWaves.Length - 1) continue;
            var stepIn = MathF.Max(HeavyImpactWaves[wave - 1] + 0.05f, AfterLightningStorm);
            ai.Move(stepIn, () => AiMove.Create(Only(role, StepIntoFiredRing(role, wave))).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(50.18f, () => AiMove.Create(DodgeWardMercy()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(52.05f, () => AiMove.Create(LeaveOuterRing()).NaturalOrder(), jitter: 0f, sprint: true);

        ai.Move(53.0f, () => AiMove.Create(TanksUnderKnightsDefamationsOutStackUnderThordan()).NaturalOrder(), sprint: true);
        ai.Move(56.95f, () => AiMove.Create(TanksStepIntoTheirTethers()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(57.7f, () => AiMove.Create(TanksCrossTethersToThordan()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(62.0f, () => AiMove.Create(SoakTowers()).NaturalOrder(), jitter: 0.1f);
        ai.Move(67.3f, () => AiMove.Create(MainTankNorthPartyStacksSouth(0f)).NaturalOrder());
        ai.Move(68.5f, () => AiMove.Create(MainTankNorthPartyStacksSouth(0f)).NaturalOrder());

        ai.Move(106.3f, () => AiMove.Create(PlungeGroupsOnTheWall(PlungeGroupOffsetDegrees)).NaturalOrder(), jitter: 0f);
        world.Events.Add(113.9f, FaceAwayFromBothGazes);
        ai.Move(state.LateBrightsphereDodge ? 118.47f : 117.22f, () => AiMove.Create(PlungeGroupsOnTheWall(BrightsphereDodgeDegrees)).NaturalOrder(), jitter: 0f);

        ai.Move(120.6f, () => AiMove.Create(MeteorPairsOnCardinals()).NaturalOrder(), jitter: 0f);
        ai.Move(132.4f, () => AiMove.Create(SoakFirstSanctityTowers()).NaturalOrder(), jitter: 0f);
        ai.Move(136.5f, () => AiMove.Create(WaitForSecondSanctityTowers()).NaturalOrder(), jitter: 0f);
        for (var step = 1; step < HolyCometDrops.Length; step++)
        {
            var fraction = (float)step / (HolyCometDrops.Length - 1);
            ai.Move(HolyCometDrops[step - 1] + 0.05f, () => AiMove.Create(MeteorsSweepClockwise(fraction)).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(HolyCometDrops[^1] + 0.05f, () => AiMove.Create(MeteorsToSecondTowers()).NaturalOrder(), jitter: 0f);

        ai.Move(150.5f, () => AiMove.Create(StackSouthOfUltimateEndThordan()).NaturalOrder());
        ai.Move(177.25f, () => AiMove.Create(StackBehindThordan(0)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(180.95f, () => AiMove.Create(DodgeIntoFirstBroadSwing(0)).NaturalOrder(), jitter: 0f);
        ai.Move(185.65f, () => AiMove.Create(StackBehindThordan(1)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(189.35f, () => AiMove.Create(DodgeIntoFirstBroadSwing(1)).NaturalOrder(), jitter: 0f);
    }

    private static Vector2 Tangential(float bearing, float offset)
    {
        var right = Flat(AtBearing(bearing + 90f, 1f));
        return right * offset;
    }

    private Vector2?[] PlungeGroupsOnTheWall(float offsetDegrees)
    {
        var spots = new Vector2?[8];
        var sign = state.SanctitySign;
        PlaceGroup(spots, state.AwayGroup, state.DarkKnightBearing + 180f + sign * offsetDegrees);
        PlaceGroup(spots, state.BehindGroup, state.DarkKnightBearing + sign * offsetDegrees);
        return spots;
    }

    private static void PlaceGroup(Vector2?[] spots, System.Collections.Generic.IReadOnlyList<PartyRole> group, float bearing)
    {
        float[] offsets = [-1.2f, -0.4f, 0.4f, 1.2f];
        for (var i = 0; i < group.Count; i++)
            spots[(int)group[i]] = Flat(AtBearing(bearing, PlungeGroupRadius)) + Tangential(bearing, offsets[i % offsets.Length]);
    }

    private void FaceAwayFromBothGazes()
    {
        var sources = new[] { Flat(state.SanctityThordanSpot), Flat(state.EyeSpot) };
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var at = Flat(member.Position);
            var bestFacing = 0f;
            var bestMargin = -1f;
            for (var degrees = 0f; degrees < 360f; degrees += 5f)
            {
                var facing = degrees * MathF.PI / 180f;
                var forward = new Vector2(MathF.Sin(facing), MathF.Cos(facing));
                var margin = sources.Min(source => MathF.Acos(Math.Clamp(Vector2.Dot(forward, Vector2.Normalize(source - at)), -1f, 1f)));
                if (margin <= bestMargin) continue;
                bestMargin = margin;
                bestFacing = facing;
            }
            member.SetRotation(bestFacing);
        }
    }

    private Vector2?[] MeteorPairsOnCardinals()
    {
        var cardinals = state.MeteorCardinals();
        return Enum.GetValues<PartyRole>().Select(role =>
        {
            var bearing = cardinals[role];
            var side = DsrP2ThordanState.IsSupport(role) ? -MeteorPairOffset : MeteorPairOffset;
            return (Vector2?)(Flat(AtBearing(bearing, DsrP2ThordanState.MeteorPairRadius)) + Tangential(bearing, side));
        }).ToArray();
    }

    private Vector2?[] SoakFirstSanctityTowers() =>
        state.FirstTowerSoaks().OrderBy(kv => kv.Key).Select(kv => (Vector2?)Flat(AtBearing(kv.Value.Bearing, kv.Value.Radius))).ToArray();

    private Vector2?[] WaitForSecondSanctityTowers()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (state.MeteorTargets.Contains(role)) continue;
            var bearing = state.SecondTowerBearing(role);
            spots[(int)role] = state.IsMeteorRole(role)
                ? Flat(AtBearing(bearing, DsrP2ThordanState.OuterTowerRadius))
                : Flat(AtBearing(bearing, KnockbackRideRadius));
        }
        return spots;
    }

    private Vector2?[] MeteorsSweepClockwise(float fraction)
    {
        var spots = new Vector2?[8];
        var cardinals = state.MeteorCardinals();
        var towers = state.FirstTowerSoaks();
        foreach (var role in state.MeteorTargets)
        {
            var cardinal = cardinals[role];
            var start = cardinal + (DsrP2ThordanState.Normalize(towers[role].Bearing - cardinal + 180f) - 180f);
            var end = cardinal + MeteorSweepDegrees;
            spots[(int)role] = Flat(AtBearing(start + (end - start) * fraction, MeteorSweepRadius));
        }
        return spots;
    }

    private Vector2?[] MeteorsToSecondTowers()
    {
        var spots = new Vector2?[8];
        foreach (var role in state.MeteorTargets)
            spots[(int)role] = Flat(AtBearing(state.SecondTowerBearing(role), DsrP2ThordanState.OuterTowerRadius));
        return spots;
    }

    private static Vector2?[] StackSouthOfUltimateEndThordan()
    {
        var spots = Enumerable.Range(0, 8).Select(i => (Vector2?)new Vector2(-1.5f + 0.43f * i, -4f)).ToArray();
        spots[(int)PartyRole.MainTank] = new Vector2(0f, -13f);
        return spots;
    }

    private static Vector2 Forward(float rotation) => new(MathF.Sin(rotation), MathF.Cos(rotation));

    private static Vector2?[] Clustered(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * ((i % 3) - 1), 0.5f * ((i / 3) - 1)))).ToArray();

    private Vector2?[] StackBehindThordan(int swing) =>
        Clustered(BroadSwingThordan - Forward(state.BroadSwingFacing[swing]) * BehindThordanDistance);

    private Vector2?[] DodgeIntoFirstBroadSwing(int swing)
    {
        var rotation = state.BroadSwingFacing[swing] + DsrP2ThordanScenario.BroadSwingOffset(state.BroadSwingRightFirst[swing], 0);
        return Clustered(BroadSwingThordan + Forward(rotation) * BroadSwingDodgeDistance);
    }

    private Vector2?[] TanksStepIntoTheirTethers()
    {
        var spots = new Vector2?[8];
        (PartyRole Tank, float KnightBearing)[] tanks = [(PartyRole.MainTank, state.AdelphelBearing), (PartyRole.OffTank, state.JanlenouxBearing)];
        for (var i = 0; i < tanks.Length; i++)
        {
            var knight = Flat(AtBearing(tanks[i].KnightBearing, DsrP2ThordanState.GuideKnightRadius));
            if (world.Party.Get(state.ShieldBashTethers[i]) is not { } holder) continue;
            spots[(int)tanks[i].Tank] = knight + Vector2.Normalize(Flat(holder.Position) - knight) * TetherGrabDistance;
        }
        return spots;
    }

    private static readonly Vector2[] MainTankNorthPartySouth =
    [
        new(0f, -6f),
        new(1f, 4.5f),
        new(-1f, 4.5f),
        new(1f, 5.7f),
        new(-1f, 5.7f),
        new(0f, 5.1f),
        new(-1f, 6.9f),
        new(1f, 6.9f),
    ];

    private static Vector2?[] MainTankNorthPartyStacksSouth(float swingDegrees)
    {
        var swing = Matrix3x2.CreateRotation(swingDegrees * MathF.PI / 180f);
        return MainTankNorthPartySouth.Select(spot => (Vector2?)Vector2.Transform(spot, swing)).ToArray();
    }

    private static Vector2?[] Only(PartyRole role, Vector2 spot)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = spot;
        return spots;
    }

    private Vector2 Guerrique => Flat(state.GuerriqueSpot);

    private int HeavyImpactWave(Vector2 at) =>
        Math.Clamp((int)MathF.Ceiling(Vector2.Distance(at, Guerrique) / HeavyImpactRingWidth) - 1, 0, HeavyImpactWaves.Length - 1);

    private float SpreadDegreesFromLine(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => DpsSpreadDegrees,
        PartyRole.PhysRangedDps or PartyRole.CasterDps => -DpsSpreadDegrees,
        _ => 0f,
    };

    private Vector2 SpreadSpot(PartyRole role)
    {
        var side = state.SpreadSideBearing(role);
        var tankRadius = TankSpreadRadius;
        while (Vector2.Distance(Flat(AtBearing(side, tankRadius)), Guerrique) < MinSpreadDistanceFromGuerrique) tankRadius += 0.5f;
        var radius = role switch
        {
            PartyRole.MainTank or PartyRole.OffTank => tankRadius,
            PartyRole.RegenHealer or PartyRole.ShieldHealer => tankRadius + SpreadSpacing,
            _ => DpsSpreadRadius,
        };
        return Flat(AtBearing(side + SpreadDegreesFromLine(role), radius));
    }

    private Vector2?[] LightPartiesSpreadOnSafeLine() =>
        Enum.GetValues<PartyRole>().Select(role => (Vector2?)SpreadSpot(role)).ToArray();

    private Vector2 StepIntoFiredRing(PartyRole role, int wave)
    {
        var safeDistance = wave * HeavyImpactRingWidth - 0.5f;
        var bearing = state.SpreadSideBearing(role) + SpreadDegreesFromLine(role) / 2f;
        for (var radius = 10f; radius >= 2f; radius -= 0.5f)
        {
            var spot = Flat(AtBearing(bearing, radius));
            if (Vector2.Distance(spot, Guerrique) < safeDistance) return spot;
        }
        var spread = SpreadSpot(role);
        return Guerrique + Vector2.Normalize(spread - Guerrique) * (safeDistance - 0.5f);
    }

    private bool StillOutsideLastRing(PartyRole role) => HeavyImpactWave(SpreadSpot(role)) == HeavyImpactWaves.Length - 1;

    private Vector2?[] DodgeWardMercy()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member) continue;
            var at = Flat(member.Position);
            var radius = at.Length();
            var bearing = MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI;
            var slide = MathF.Sign(SpreadDegreesFromLine(role));
            if (StillOutsideLastRing(role) && slide != 0f)
            {
                spots[(int)role] = Flat(AtBearing(bearing + slide * OuterRingDodgeDegrees, radius));
                continue;
            }
            var safeDistance = 3f * HeavyImpactRingWidth - 0.5f;
            Vector2 dodge;
            var scale = 0.64f;
            do
            {
                dodge = Flat(AtBearing(bearing + WardMercyDodgeDegrees, MathF.Max(3f, radius * scale)));
                scale -= 0.08f;
            } while (Vector2.Distance(dodge, Guerrique) >= safeDistance && scale > 0.2f);
            spots[(int)role] = dodge;
        }
        return spots;
    }

    private Vector2?[] LeaveOuterRing()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (!StillOutsideLastRing(role) || world.Party.Get(role) is not { } member) continue;
            var at = Flat(member.Position);
            spots[(int)role] = Vector2.Normalize(at) * 14f;
        }
        return spots;
    }

    private Vector2?[] TanksUnderKnightsDefamationsOutStackUnderThordan()
    {
        var spots = new Vector2?[8];
        spots[(int)PartyRole.MainTank] = Flat(AtBearing(state.AdelphelBearing, TankTetherStartRadius));
        spots[(int)PartyRole.OffTank] = Flat(AtBearing(state.JanlenouxBearing, TankTetherStartRadius));
        for (var i = 0; i < state.Defamations.Count; i++)
            spots[(int)state.Defamations[i]] = Flat(state.RelativeToThordan(DsrP2ThordanState.DefamationSpotBearings[i], WallRadius));
        for (var i = 0; i < state.Stackers.Count; i++)
            spots[(int)state.Stackers[i]] = Flat(state.RelativeToThordan(StackerOffsetDegrees[i], StackRadius));
        return spots;
    }

    private Vector2?[] TanksCrossTethersToThordan()
    {
        var spots = new Vector2?[8];
        spots[(int)PartyRole.MainTank] = Flat(state.RelativeToThordan(-TankBesideThordanDegrees, WallRadius));
        spots[(int)PartyRole.OffTank] = Flat(state.RelativeToThordan(TankBesideThordanDegrees, WallRadius));
        return spots;
    }

    private Vector2?[] SoakTowers()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < state.Defamations.Count; i++)
            spots[(int)state.Defamations[i]] = Flat(state.RelativeToThordan(DsrP2ThordanState.DefamationTowerBearings[i], DsrP2ThordanState.TowerRadius));
        for (var i = 0; i < state.Stackers.Count; i++)
            spots[(int)state.Stackers[i]] = Flat(state.RelativeToThordan(DsrP2ThordanState.StackerTowerBearings[i], DsrP2ThordanState.TowerRadius));
        return spots;
    }

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
