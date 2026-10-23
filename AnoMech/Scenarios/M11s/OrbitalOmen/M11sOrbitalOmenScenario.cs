using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

// After One and Only: Great Wall of Fire's two line busters, each leaving a wall that explodes,
// then Orbital Omen's four pairs of portal lines with Fire and Fury's front and back cones.
public sealed class M11sOrbitalOmenScenario : IScenario
{
    public string Name => "Great Wall of Fire to Orbital Omen";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sOrbitalOmenAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sOrbitalOmenSettingsWindow settingsWindow = new();

    public const float WallCastAt = 6.61f;
    public static readonly float[] LineAt = [11.82f, 15.02f];
    public static readonly float[] WallExplodesCastAt = [12.62f, 15.82f];
    public const float WallExplosionResolves = 3f;
    public const float OrbitalCastAt = 20.14f;
    public static readonly float[] PairCastAt = [28.3f, 29.81f, 31.33f, 32.8f];
    public const float PairResolves = 6f;
    public const float FuryCastAt = 29.81f;
    public const float FuryResolves = 5f;
    private const float PortalShowsBefore = 2.08f;
    private const float PortalFiresBefore = 0.09f;
    private const float PortalOffAfter = 7.03f;
    private const float PortalDistance = 25f;
    private static readonly float[] AutoAttackAt = [1f, 4f, 22f, 25f];

    private SimWorld world = null!;
    private M11sOrbitalOmenState state = null!;
    private SimEnemy? tyrant;
    private SimEnemy? lineHelper;
    private readonly SimEnemy?[] wallHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] portalHelpers = new SimEnemy?[2 * M11sOrbitalOmenState.Pairs];
    private readonly SimEnemy?[] furyHelpers = new SimEnemy?[2];
    private readonly float[] lineBearing = new float[2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sOrbitalOmenState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sOrbitalOmenState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        foreach (var at in AutoAttackAt)
            world.Events.Add(at, AutoAttack);
        world.Events.Add(WallCastAt, HoldTheBoss);
        world.Events.Add(WallCastAt, () => tyrant?.Cast(ActionId.GreatWallOfFire, animationLock: 0.2f));
        PartyRole[] lineTargets = [PartyRole.MainTank, PartyRole.OffTank];
        for (var i = 0; i < LineAt.Length; i++)
        {
            var n = i;
            world.Events.Add(LineAt[n], () => FireLine(n, lineTargets[n]));
            world.Events.Add(WallExplodesCastAt[n], () => ExplodeWall(n));
        }
        world.Events.Add(OrbitalCastAt, () =>
        {
            tyrant?.Face(new Vector3(0f, 0f, -1f));
            tyrant?.Cast(ActionId.OrbitalOmen, animationLock: 0.2f);
        });
        for (var p = 0; p < M11sOrbitalOmenState.Pairs; p++)
        {
            var pair = p;
            world.Events.Add(PairCastAt[pair] - PortalShowsBefore, () => Portals(pair, MapEffect.PortalShows));
            world.Events.Add(PairCastAt[pair] - PortalFiresBefore, () => Portals(pair, MapEffect.PortalFires));
            world.Events.Add(PairCastAt[pair], () => FirePair(pair));
            world.Events.Add(PairCastAt[pair] + PortalOffAfter, () => Portals(pair, MapEffect.PortalOff));
        }
        world.Events.Add(FuryCastAt, FireAndFury);
    }

    public void Tick(float delta, float elapsed)
    {
    }

    private void SpawnActors()
    {
        tyrant = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Tyrant,
            NameId: BNpcNameId.Tyrant,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        lineHelper = SpawnHelper();
        for (var i = 0; i < wallHelpers.Length; i++) wallHelpers[i] = SpawnHelper();
        for (var i = 0; i < portalHelpers.Length; i++) portalHelpers[i] = SpawnHelper();
        for (var i = 0; i < furyHelpers.Length; i++) furyHelpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void TankTheBoss() => tyrant?.SetTarget(Alive(PartyRole.MainTank));

    private void HoldTheBoss() => tyrant?.Follow();

    private void AutoAttack()
    {
        if (Alive(PartyRole.MainTank) is { } tank)
            tyrant?.Cast(ActionId.Attack, tank, animationLock: 0.2f);
    }

    private void FireLine(int n, PartyRole target)
    {
        if (tyrant is not { } boss || (Alive(target) ?? Alive(PartyRole.MainTank)) is not { } tank) return;
        lineBearing[n] = M11sTrophyWeapons.Bearing(boss.Position, tank.Position);
        lineHelper?.SetPosition(new Placement(boss.Position, lineBearing[n]));
        lineHelper?.Cast(Actions.GreatWallOfFireLine, tank);
        boss.Face(tank);
    }

    private void ExplodeWall(int n)
    {
        if (tyrant is not { } boss) return;
        wallHelpers[n]?.SetPosition(new Placement(boss.Position, lineBearing[n]));
        wallHelpers[n]?.Cast(Actions.WallExplosion);
    }

    private void Portals(int pair, uint effect)
    {
        world.Map.AddEffect(effect, (byte)(MapEffect.NorthPortalSlot + M11sOrbitalOmenState.NorthSlotOffset(state.NorthLines[pair])));
        world.Map.AddEffect(effect, (byte)(MapEffect.EastPortalSlot + M11sOrbitalOmenState.NorthSlotOffset(state.EastLines[pair])));
    }

    private void FirePair(int pair)
    {
        var north = portalHelpers[2 * pair];
        var east = portalHelpers[2 * pair + 1];
        north?.SetPosition(new Placement(new Vector3(state.NorthLines[pair], 0f, -PortalDistance), 0f));
        north?.Cast(Actions.OrbitalOmenLine);
        east?.SetPosition(new Placement(new Vector3(PortalDistance, 0f, state.EastLines[pair]), -MathF.PI / 2f));
        east?.Cast(Actions.OrbitalOmenLine);
    }

    private void FireAndFury()
    {
        if (tyrant is not { } boss) return;
        boss.Cast(ActionId.FireAndFury, animationLock: 0.2f);
        furyHelpers[0]?.SetPosition(boss.Placement());
        furyHelpers[0]?.Cast(Actions.FireAndFuryFront);
        furyHelpers[1]?.SetPosition(new Placement(boss.Position, boss.Rotation + MathF.PI));
        furyHelpers[1]?.Cast(Actions.FireAndFuryBack);
    }
}
