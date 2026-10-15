using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P3UltimateRelativity.FruP3UltimateRelativityState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P3UltimateRelativity;

public sealed class FruP3UltimateRelativityScenario : IScenario
{
    public string Name => "Ultimate Relativity";
    public IPhase Phase => FruZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP3UltimateRelativityAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP3UltimateRelativitySettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private FruP3UltimateRelativityState state = null!;
    private SimEnemy? oracle;
    private readonly SimEnemy?[] hourglasses = new SimEnemy?[8];
    private readonly SimEnemy?[] lasers = new SimEnemy?[8];
    private readonly Vector3[] laserDirections = new Vector3[8];
    private readonly SimEnemy?[] debuffHelpers = new SimEnemy?[8];
    private readonly List<SimTether> speedTethers = [];
    private readonly Dictionary<PartyRole, Vector3> returns = new();

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP3UltimateRelativityState(world.Rng, settingsWindow.Overrides);
        speedTethers.Clear();
        returns.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP3UltimateRelativityState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnOracleAndHelpers);
        world.Events.Add(1f, () => oracle?.Cast(Actions.UltimateRelativity));
        world.Events.Add(AssignAt, AssignDebuffs);
        world.Events.Add(AssignAt, SpawnHourglasses);
        world.Events.Add(15f, TetherHourglasses);
        world.Events.Add(20.3f, SpeedUpAndSlowDownHourglasses);
        for (var wave = 0; wave < 3; wave++)
        {
            var w = wave;
            world.Events.Add(FireAt(w), () => ResolveFire(w));
            world.Events.Add(LaserStartAt(w), () => StartLasers(w));
            world.Events.Add(LaserHitAt(w, 0), () => FireFirstLasers(w));
            if (w < 2) world.Events.Add(LaserHitAt(w, 0), () => SaveReturns(earlyReturn: w == 0));
            for (var shot = 1; shot < LaserShots; shot++)
            {
                var s = shot;
                world.Events.Add(LaserHitAt(w, s), () => SweepLasers(w, s));
            }
        }
        world.Events.Add(StunAt, () => world.Party.ActiveMembers().ToList().ForEach(m => m.AddStatus(StatusId.Stun, FinalAt - StunAt)));
        world.Events.Add(RewindAt, Rewind);
        world.Events.Add(FinalAt, ResolveReturnDebuffs);
        world.Events.Add(55.8f, () => oracle?.Cast(Actions.ShellCrusherCast, world.Party.Get(state.ShellCrusherTarget)));
        world.Events.Add(58.5f, () => CastOn(debuffHelpers[0], Actions.ShellCrusher, state.ShellCrusherTarget));
    }

    private void SpawnOracleAndHelpers()
    {
        oracle = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.OracleP3,
            NameId: BNpcNameId.Oracle,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < debuffHelpers.Length; i++) debuffHelpers[i] = SpawnHelper(Vector3.Zero);
    }

    private SimEnemy? SpawnHelper(Vector3 position) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Oracle,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(position, 0f)));

    // TODO: retail also shows an actor-bound clock (VFX 1001, d1049_stlp_b0k1) over each player
    // until their Return is saved; there's no way to attach a VFX sheet row to an actor.
    private void AssignDebuffs()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            if (role == state.IceHolder) member.AddStatus(StatusId.DarkBlizzardWaiting, FireAt(1) - AssignAt);
            else member.AddStatus(StatusId.DarkFireWaiting, FireAt(FireWave(state.Debuff(role))) - AssignAt);
            var finalDebuff = state.Shadoweye(role) ? StatusId.ShadoweyeWaiting
                : state.DarkWater(role) ? StatusId.DarkWaterWaiting : StatusId.DarkEruptionWaiting;
            member.AddStatus(finalDebuff, FinalAt - AssignAt);
            member.AddStatus(StatusId.ReturnWaiting, LaserHitAt(state.EarlyReturn(role) ? 0 : 1, 0) - AssignAt);
        }
        for (var wave = 0; wave < 3; wave++)
            world.Party.Get(state.Darkness(wave))?.AddStatus(StatusId.UnholyDarknessWaiting, FireAt(wave) - AssignAt);
    }

    private void SpawnHourglasses()
    {
        for (var clock = 0; clock < 8; clock++)
        {
            hourglasses[clock] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.SorrowsHourglassP3,
                NameId: BNpcNameId.SorrowsHourglass,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(state.Hourglass(clock), MathF.PI)));
            lasers[clock] = SpawnHelper(state.Hourglass(clock));
            world.Map.AddEffect(MapEffect.Show, state.HourglassSlot(clock));
        }
    }

    private static bool Quickened(int clock) => Wave(clock) == 0;
    private static bool Slowed(int clock) => Wave(clock) == 2;

    private void TetherHourglasses()
    {
        oracle?.Cast(Actions.Speed);
        for (var clock = 0; clock < 8; clock++)
            if (Quickened(clock) || Slowed(clock))
                speedTethers.Add(world.Tether(hourglasses[clock], oracle, Quickened(clock) ? TetherId.Quicken : TetherId.Slow));
    }

    private void SpeedUpAndSlowDownHourglasses()
    {
        speedTethers.ForEach(tether => tether.Despawn());
        speedTethers.Clear();
        for (var clock = 0; clock < 8; clock++)
            if (Quickened(clock) || Slowed(clock))
                lasers[clock]?.Cast(Quickened(clock) ? Actions.Quicken : Actions.Slow, hourglasses[clock]);
    }

    private void ResolveFire(int wave)
    {
        var helper = 0;
        foreach (var role in Enum.GetValues<PartyRole>().Where(role => state.HasFire(role, wave)))
            CastOn(debuffHelpers[helper++], Actions.DarkFire, role);
        if (wave == 1) CastOn(debuffHelpers[helper++], Actions.DarkBlizzard, state.IceHolder);
        CastOn(debuffHelpers[helper], Actions.UnholyDarkness, state.Darkness(wave));
    }

    private void CastOn(SimEnemy? helper, EnemyAction action, PartyRole role)
    {
        if (world.Party.Get(role) is not { } target || !target.IsAlive()) return;
        helper?.SetPosition(target.Position);
        helper?.Cast(action, target);
    }

    private void StartLasers(int wave)
    {
        for (var clock = 0; clock < 8; clock++)
        {
            if (Wave(clock) != wave) continue;
            var param = state.Clockwise(clock) ? HourglassClockwiseParam : HourglassCounterclockwiseParam;
            hourglasses[clock]?.AddStatusParam(StatusId.HourglassRotation, param, LaserHitAt(wave, 0) - LaserStartAt(wave));
            hourglasses[clock]?.Cast(Actions.SinboundMeltdownCast);
        }
    }

    // The first shot goes at whoever stands nearest the hourglass; the rest sweep on from there.
    private void FireFirstLasers(int wave)
    {
        for (var clock = 0; clock < 8; clock++)
        {
            if (Wave(clock) != wave) continue;
            var origin = state.Hourglass(clock);
            var bait = world.Party.ActiveMembers().MinBy(m => Vector3.DistanceSquared(m.Position, origin));
            var offset = bait is null ? Vector3.Zero : bait.Position - origin;
            laserDirections[clock] = offset.LengthSquared() > 0.0001f ? Vector3.Normalize(offset) : -state.Direction(clock);
            lasers[clock]?.SetPosition(new Placement(origin, MathF.Atan2(laserDirections[clock].X, laserDirections[clock].Z)));
            lasers[clock]?.Cast(Actions.SinboundMeltdownFirst, bait);
        }
    }

    private void SweepLasers(int wave, int shot)
    {
        for (var clock = 0; clock < 8; clock++)
        {
            if (Wave(clock) != wave) continue;
            var angle = (state.Clockwise(clock) ? 1 : -1) * shot * MathF.PI / 12f;
            var d = laserDirections[clock];
            var direction = new Vector3(d.X * MathF.Cos(angle) - d.Z * MathF.Sin(angle), 0f, d.X * MathF.Sin(angle) + d.Z * MathF.Cos(angle));
            lasers[clock]?.SetRotation(MathF.Atan2(direction.X, direction.Z));
            lasers[clock]?.Cast(Actions.SinboundMeltdownRest);
        }
    }

    private void SaveReturns(bool earlyReturn)
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(role => state.EarlyReturn(role) == earlyReturn))
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            returns[role] = member.Position;
            member.AddStatus(StatusId.Return, StunAt - LaserHitAt(earlyReturn ? 0 : 1, 0));
            member.PlayAction(ActionId.Return);
            world.PlaceGroundVfx(VfxId.ReturnMarker, member.Position);
        }
    }

    private void Rewind()
    {
        foreach (var (role, position) in returns)
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            member.RemoveStatus(StatusId.Return);
            member.PlayAction(ActionId.ReturnIV);
            var offset = position - member.Position;
            if (offset.LengthSquared() < 0.0001f) continue;
            member.Push(member.Position - offset, offset.Length(), 0.4f);
        }
    }

    private void ResolveReturnDebuffs()
    {
        var helper = 0;
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var action = state.Shadoweye(role) ? Actions.Shadoweye : state.DarkWater(role) ? Actions.DarkWater : Actions.DarkEruption;
            CastOn(debuffHelpers[helper++], action, role);
        }
    }
}
