using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.DeepAerial;

// Deep Aerial: whoever soaks the tower is locked in the Watery Grave, then six rounds of one fire
// and one water tether. Each wave runs from its brother to the tethered player, kills if the
// tether is short, and the brother lands where it hit. Fire waves must cross the prison to break
// it; a water wave through it shields it for a while. The prisoners drown if it holds.
public sealed class M10sDeepAerialScenario : IScenario
{
    public string Name => "Deep Aerial";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sDeepAerialAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sDeepAerialSettingsWindow settingsWindow = new();

    public const float DeepAerialAt = 4.05f;
    public const float TowerHitAt = 10.05f;
    public const float BrothersTakePlaceAt = 16f;
    public static readonly float[] TetherAt = [16.13f, 24.5f, 33.1f, 41.65f, 50.29f, 58.89f];
    public const float TetherToWave = 5.3f;
    public const float WaveToSteam = 0.8f;
    public const float PrisonBreaksAt = 66.82f;
    public const float GraveDrownsAt = 70.2f;
    public const float DiversDareAt = 69.8f;

    public static readonly Vector3[] GraveSpots = [new(-2f, 0f, 0f), new(2f, 0f, 0f)];
    public const float TowerRadius = 6f;
    private const float GraveRadius = 1f;
    private const float WaveHalfWidth = 4f;
    private const float WaveLength = 60f;
    private const float ShieldSeconds = 10f;
    private const float GraveSeconds = 59f;
    private const byte SurfingModelState = 0x2F;

    private SimWorld world = null!;
    private M10sDeepAerialState state = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? towerHelper;
    private readonly SimEnemy?[] graves = new SimEnemy?[2];
    private readonly List<SimCharacter> prisoners = [];
    private readonly SimTether?[] waveTethers = new SimTether?[2];
    private readonly SimCharacter?[] waveTargets = new SimCharacter?[2];
    private bool fireCrossedThePrison;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sDeepAerialState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        prisoners.Clear();
        Array.Clear(waveTethers);
        Array.Clear(waveTargets);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sDeepAerialState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3.96f, () => world.Map.AddEffect(MapEffect.TowerShows, MapEffect.TowerSlot));
        world.Events.Add(DeepAerialAt, () =>
        {
            deepBlue?.Cast(ActionId.DeepAerial, animationLock: 0.2f);
            towerHelper?.Cast(Actions.DeepAerialTower);
        });
        world.Events.Add(9.04f, () => world.Map.AddEffect(MapEffect.TowerResolves, MapEffect.TowerSlot));
        world.Events.Add(TowerHitAt + 0.05f, Imprison);
        world.Events.Add(13.28f, () =>
        {
            redHot?.SetTargetable(false);
            deepBlue?.SetTargetable(false);
            foreach (var grave in graves) grave?.SetTargetable(true);
        });
        world.Events.Add(BrothersTakePlaceAt, () =>
        {
            redHot?.SetPosition(new Placement(new Vector3(14f, 0f, 14f * state.Side), 0f).Face(Vector3.Zero));
            deepBlue?.SetPosition(new Placement(new Vector3(-14f, 0f, 14f * state.Side), 0f).Face(Vector3.Zero));
        });
        for (var r = 0; r < M10sDeepAerialState.Rounds; r++)
        {
            var round = r;
            world.Events.Add(TetherAt[round], () => TetherTheWaves(round));
            world.Events.Add(TetherAt[round] + 0.09f, () =>
            {
                redHot?.SetModelState(SurfingModelState);
                deepBlue?.SetModelState(SurfingModelState);
                redHot?.Cast(round == 0 ? ActionId.XtremeWaveRedFirst : ActionId.XtremeWaveRed, animationLock: 0.2f);
                deepBlue?.Cast(round == 0 ? ActionId.XtremeWaveBlueFirst : ActionId.XtremeWaveBlue, animationLock: 0.2f);
            });
            world.Events.Add(TetherAt[round] + TetherToWave, Waves);
            world.Events.Add(TetherAt[round] + TetherToWave + WaveToSteam, ScathingSteam);
        }
        world.Events.Add(66.5f, () =>
        {
            redHot?.SetTargetable(true);
            deepBlue?.SetTargetable(true);
        });
        world.Events.Add(PrisonBreaksAt, BreakThePrison);
        world.Events.Add(67.08f, () =>
        {
            redHot?.SetModelState(0);
            deepBlue?.SetModelState(0);
        });
        world.Events.Add(DiversDareAt, () =>
        {
            redHot?.Cast(Actions.DiversDare);
            deepBlue?.Cast(Actions.DiversDareBlue);
        });
        world.Events.Add(GraveDrownsAt, Drown);
        world.Events.Add(76f, () => { });
    }

    public void Tick(float delta, float elapsed)
    {
        HoldThePrisoners();
        UpdateWaveTethers();
    }

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Vector3(3f, 0f, -3f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Vector3(-3f, 0f, -3f));
        towerHelper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Helper,
            NameId: BNpcNameId.DeepBlue,
            Level: 1,
            EnemyList: EnemyListMode.Never,
            Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(Vector3.Zero, 0f)));
        for (var i = 0; i < graves.Length; i++)
        {
            graves[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.WateryGrave,
                NameId: BNpcNameId.WateryGrave,
                Level: Level,
                EnemyList: EnemyListMode.Always,
                Placement: new Placement(GraveSpots[i], 0f)));
            graves[i]?.SetVisible(false);
        }
    }

    private SimEnemy? SpawnBrother(uint baseId, uint nameId, Vector3 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: baseId,
        NameId: nameId,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.Always,
        Placement: new Placement(at, 0f).Face(Vector3.Zero)));

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

    private void Imprison()
    {
        var inside = Alive().Where(m => Flat(m.Position).Length() <= TowerRadius)
            .OrderBy(m => Flat(m.Position).Length()).Take(GraveSpots.Length).ToList();
        if (inside.Count == 0)
        {
            towerHelper?.Cast(Actions.UnmitigatedExplosion);
            return;
        }
        prisoners.AddRange(inside.OrderBy(m => m.Position.X));
        world.Map.AddEffect(MapEffect.GraveRises, MapEffect.GraveSlot);
        for (var i = 0; i < prisoners.Count; i++)
        {
            graves[i]?.SetVisible(true);
            prisoners[i].SetPosition(new Placement(GraveSpots[i], 0f));
            prisoners[i].AddStatus(StatusId.WateryGrave, GraveSeconds);
        }
    }

    private void HoldThePrisoners()
    {
        for (var i = 0; i < prisoners.Count; i++)
        {
            var prisoner = prisoners[i];
            if (prisoner.IsAlive() && prisoner.HasStatus(StatusId.WateryGrave) && Vector3.Distance(prisoner.Position, GraveSpots[i]) > 0.5f)
                prisoner.SetPosition(new Placement(GraveSpots[i], prisoner.Rotation));
        }
    }

    private SimCharacter? Tethered(PartyRole role, IReadOnlyCollection<SimCharacter?> taken)
    {
        if (world.Party.Get(role) is { } member && member.IsAlive() && !prisoners.Contains(member) && !taken.Contains(member)) return member;
        var free = Alive().Where(m => !prisoners.Contains(m) && !taken.Contains(m)).ToList();
        return free.Count == 0 ? null : free[world.Rng.Next(free.Count)];
    }

    private void TetherTheWaves(int round)
    {
        var (fire, water) = state.Tethers[round];
        waveTargets[0] = Tethered(fire, []);
        waveTargets[1] = Tethered(water, [waveTargets[0]]);
        SimEnemy?[] sources = [redHot, deepBlue];
        uint[] lockons = [LockonId.FireWave, LockonId.WaterWave];
        for (var i = 0; i < 2; i++)
        {
            if (waveTargets[i] is not { } target) continue;
            waveTethers[i] = world.Tether(sources[i], target, TetherId.WaveSlack);
            target.AttachLockonVfx(lockons[i]);
        }
    }

    private void UpdateWaveTethers()
    {
        SimEnemy?[] sources = [redHot, deepBlue];
        for (var i = 0; i < 2; i++)
        {
            if (waveTethers[i] is not { IsActive: true } tether || sources[i] is not { } source || waveTargets[i] is not { } target) continue;
            var wanted = Vector3.Distance(source.Position, target.Position) >= Actions.XtremeWaveSafeFrom ? TetherId.WaveTaut : TetherId.WaveSlack;
            if (tether.TetherId == wanted) continue;
            tether.Despawn();
            waveTethers[i] = world.Tether(source, target, wanted);
        }
    }

    private void Waves()
    {
        fireCrossedThePrison = false;
        SimEnemy?[] sources = [redHot, deepBlue];
        for (var i = 0; i < 2; i++)
        {
            waveTethers[i]?.Despawn();
            waveTethers[i] = null;
            if (sources[i] is not { } boss || waveTargets[i] is not { } target || !target.IsAlive()) continue;
            var stretched = Vector3.Distance(boss.Position, target.Position) >= Actions.XtremeWaveSafeFrom;
            var fire = i == 0;
            var crosses = CrossesThePrison(boss.Position, target.Position);
            boss.Cast(fire
                ? stretched ? Actions.XtremeWaveFire : Actions.XtremeWaveFireShort
                : stretched ? Actions.XtremeWaveWater : Actions.XtremeWaveWaterShort, target);
            var landing = target.Position;
            world.Events.Add(0.3f, () => boss.SetPosition(new Placement(landing, 0f).Face(Vector3.Zero)));
            if (!crosses) continue;
            if (fire)
            {
                fireCrossedThePrison = true;
                if (world.Events.Elapsed >= state.PrisonShieldedUntil) state.FireWavesThroughThePrison++;
            }
            else state.PrisonShieldedUntil = world.Events.Elapsed + ShieldSeconds;
        }
    }

    private bool CrossesThePrison(Vector3 from, Vector3 toward)
    {
        if (prisoners.Count == 0) return false;
        var dir = Flat(toward) - Flat(from);
        if (dir.LengthSquared() < 0.01f) return false;
        dir = Vector2.Normalize(dir);
        return GraveSpots.Take(prisoners.Count).Any(g =>
        {
            var d = Flat(g) - Flat(from);
            var along = Vector2.Dot(d, dir);
            var across = MathF.Abs(d.X * dir.Y - d.Y * dir.X);
            return along >= -GraveRadius && along <= WaveLength + GraveRadius && across <= WaveHalfWidth + GraveRadius;
        });
    }

    private void ScathingSteam()
    {
        if (!fireCrossedThePrison || graves[0] is not { Visible: true } grave) return;
        world.Map.AddEffect(MapEffect.GraveSteams, MapEffect.GraveSlot);
        grave.Cast(Actions.ScathingSteam);
    }

    // UNVERIFIED: the prison breaks only if all six fire waves crossed it unshielded.
    private void BreakThePrison()
    {
        if (prisoners.Count == 0 || state.FireWavesThroughThePrison < M10sDeepAerialState.Rounds) return;
        world.Map.AddEffect(MapEffect.GraveBreaks, MapEffect.GraveSlot);
        foreach (var grave in graves) grave?.SetVisible(false);
        foreach (var prisoner in prisoners) prisoner.RemoveStatus(StatusId.WateryGrave);
        prisoners.Clear();
    }

    private void Drown()
    {
        foreach (var prisoner in prisoners.Where(p => p.IsAlive()))
            prisoner.Die(ActionId.ScathingSteam, "the Watery Grave held: every fire wave must cross it, no water wave");
        prisoners.Clear();
    }

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
