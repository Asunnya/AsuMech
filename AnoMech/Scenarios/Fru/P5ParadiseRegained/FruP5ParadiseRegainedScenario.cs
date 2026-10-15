using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P5ParadiseRegained.FruP5ParadiseRegainedState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P5ParadiseRegained;

public sealed class FruP5ParadiseRegainedScenario : IScenario
{
    public string Name => "Paradise Regained";
    public IPhase Phase => FruZone.P5;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5ParadiseRegainedAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP5ParadiseRegainedSettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private FruP5ParadiseRegainedState state = null!;
    private SimEnemy? pandora;
    private SimEnemy? busterHelper;
    private readonly SimEnemy?[] towerHelpers = new SimEnemy?[3];
    // Per round: the light line's helper, then the dark one's.
    private readonly SimEnemy?[,] lineHelpers = new SimEnemy?[4, 2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP5ParadiseRegainedState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP5ParadiseRegainedState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => pandora?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(1.3f, () => pandora?.Cast(Actions.ParadiseRegained));
        world.Events.Add(TowerShowAt(0), () => ShowTower(0));
        world.Events.Add(8.3f, () => pandora?.Cast(state.DarkFirst ? Actions.WingsDarkThenLight : Actions.WingsLightThenDark));
        world.Events.Add(TowerShowAt(1), () => ShowTower(1));
        world.Events.Add(TowerShowAt(2), () => ShowTower(2));
        world.Events.Add(15f, () => FaceTank(PartyRole.MainTank));
        world.Events.Add(TowerHitAt(0), () => { ResolveTower(0); Wing(0); });
        world.Events.Add(18.5f, () => FaceTank(PartyRole.OffTank));
        world.Events.Add(TowerHitAt(1), () => { ResolveTower(1); Wing(1); });
        world.Events.Add(TowerHitAt(2), () => ResolveTower(2));

        world.Events.Add(PolarizingStart, () => pandora?.SetRotation(MathF.PI));
        world.Events.Add(PolarizingStart + 3.5f, () => pandora?.Cast(Actions.PolarizingStrikes));
        for (var round = 0; round < 4; round++)
        {
            var r = round;
            // The lines' 0.5s cast snapshots 0.2s in.
            world.Events.Add(StrikeAt(r) - 0.2f, () => Strike(r));
            if (r < 3) world.Events.Add(PathsCastAt(r), () => pandora?.Cast(Actions.PolarizingPaths));
            world.Events.Add(EchoAt(r), () => Echo(r));
        }
    }

    private void SpawnActors()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: BNpcNameId.Pandora,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        busterHelper = SpawnHelper(Vector3.Zero);
        for (var tower = 0; tower < 3; tower++) towerHelpers[tower] = SpawnHelper(state.TowerPosition(tower));
        for (var round = 0; round < 4; round++)
            for (var side = 0; side < 2; side++)
                lineHelpers[round, side] = SpawnHelper(Vector3.Zero);
    }

    private SimEnemy? SpawnHelper(Vector3 position) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Pandora,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(position, 0f)));

    private void ShowTower(int tower) => world.Map.AddEffect(MapEffect.ParadiseTowerShow, state.TowerSlot(tower));

    private void FaceTank(PartyRole role)
    {
        if (world.Party.Get(role) is not { } tank || !tank.IsAlive()) return;
        pandora?.SetTarget(tank, follow: false);
        pandora?.Face(tank);
    }

    private void ResolveTower(int tower)
    {
        world.Map.AddEffect(MapEffect.Hide, state.TowerSlot(tower));
        var soakers = world.Party.Find.InsideCircle(state.TowerPosition(tower), TowerRadius).Count;
        towerHelpers[tower]?.Cast(soakers == 2 ? Actions.ParadiseTowerExplosion : Actions.ParadiseTowerFailure);
    }

    // The cleave goes out from Pandora's facing; the buster on whoever stands nearest (dark) or
    // farthest (light) from her.
    private void Wing(int wing)
    {
        if (pandora is not { } boss) return;
        var dark = state.IsDark(wing);
        boss.Cast(dark ? Actions.WingsCleaveDark : Actions.WingsCleaveLight);
        var byDistance = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).ToList();
        if (byDistance.Count == 0) return;
        var bait = dark ? byDistance[0] : byDistance[^1];
        busterHelper?.SetPosition(bait.Position);
        busterHelper?.Cast(dark ? Actions.WingsBusterDark : Actions.WingsBusterLight, bait);
    }

    private void Strike(int round)
    {
        if (pandora is not { } boss) return;
        var light = NearestOnSide(boss, west: true);
        var dark = NearestOnSide(boss, west: false);
        if (light is null || dark is null)
        {
            world.Party.WipeAllPlayers(ActionId.PolarizingStrikes, "nobody on one side of Pandora");
            return;
        }
        CastLine(lineHelpers[round, 0], boss.Position, Actions.CruelPathOfLight, light);
        CastLine(lineHelpers[round, 1], boss.Position, Actions.CruelPathOfDarkness, dark);
    }

    private SimCharacter? NearestOnSide(SimEnemy boss, bool west)
    {
        var right = new Vector3(MathF.Cos(boss.Rotation), 0f, -MathF.Sin(boss.Rotation));
        return world.Party.ActiveMembers()
            .Where(m => Vector3.Dot(m.Position - boss.Position, right) > 0f == west)
            .MinBy(m => Vector3.DistanceSquared(m.Position, boss.Position));
    }

    private static void CastLine(SimEnemy? helper, Vector3 from, Core.EnemyActions.EnemyAction action, SimCharacter bait)
    {
        helper?.SetPosition(from);
        helper?.Cast(action, bait);
    }

    // The echo repeats each line where it went, from the same helper, still turned to its bait.
    private void Echo(int round)
    {
        lineHelpers[round, 0]?.Cast(Actions.CruelPathOfLightEcho);
        lineHelpers[round, 1]?.Cast(Actions.CruelPathOfDarknessEcho);
    }
}
