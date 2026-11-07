using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;
using State = AsuMech.Scenarios.Ucob.P3Heavensfall.UcobP3HeavensfallState;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

// Bahamut Prime's Heavensfall Trio, through the Gigaflare that ends it. The bosses' HP is not
// simulated; Bahamut Prime and Twintania stay where they dove from.
public sealed class UcobP3HeavensfallScenario : IScenario
{
    public string Name => "Heavensfall Trio";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3HeavensfallAi()];

    private readonly UcobP3HeavensfallSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float HeavensfallTrio = 4.00f;
    private const float BahamutUntargetable = 10.01f;
    private const float TrioLeave = 10.06f;
    private const float TrioMove = 11.10f;
    private const float TrioLand = 11.17f;
    public const float DiveMarkers = 12.24f;
    public const float Dives = 12.38f;
    public const float DivesResolve = 16.08f;
    public const float Twisters = 17.37f;
    public const float TowersCast = 18.35f;
    private const float NaelHeavensfall = 18.84f;
    public const float MegaflarePuddles = 19.33f;
    public const float MegaflarePuddlesResolve = 22.33f;
    public const float Heavensfall = 23.93f;
    public const float TowersResolve = 26.35f;
    public static readonly float[] BurstCasts = [27.94f, 29.95f, 30.48f, 31.01f, 31.55f, 32.09f, 32.62f, 33.16f];
    public const float BurstCastSeconds = 3f;
    public static readonly float[] Hypernovas = [29.50f, 31.06f, 32.62f];
    private const float HypernovaPuddleAppears = 0.9f;
    private const float HypernovaPuddleLasts = 15.2f;
    private const float TwintaniaLands = 32.53f;
    public const float FireballMarker = 33.65f;
    private const float NaelLeaves = 34.23f;
    private const float BahamutReturns = 36.54f;
    public const float Fireball = 38.77f;
    public const float Gigaflare = 37.70f;
    private const float TwintaniaLeaves = 40.87f;
    public const float End = 45f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private State state = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly UcobTwisters twisters = new();
    private readonly List<SimEventObject> puddles = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new State(world.Rng, settingsWindow.Overrides);
        twisters.Clear();
        puddles.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnBosses);

        world.Events.Add(HeavensfallTrio, () => bahamut?.Cast(ActionId.HeavensfallTrio));
        world.Events.Add(BahamutUntargetable, () =>
        {
            bahamut?.SetTargetable(false);
            world.Map.DirectorUpdate(MusicDirector, HeavensfallTrioMusic);
        });
        world.Events.Add(TrioLeave, () => bahamut?.PlayActionTimeline(TimelineId.TrioLeave));
        world.Events.Add(TrioMove, MoveTrioToTheEdge);
        world.Events.Add(TrioLand, PlayTrioLand);
        world.Events.Add(DiveMarkers, MarkDiveTargets);
        world.Events.Add(Dives, () =>
        {
            Dive(bahamut, state.MegaflareDiveTarget, Actions.MegaflareDive);
            Dive(twintania, state.TwistingDiveTarget, Actions.TwistingDive);
        });
        world.Events.Add(Twisters, DropTwisters);
        world.Events.Add(TowersCast, CastTowers);
        world.Events.Add(NaelHeavensfall, () => nael?.Cast(ActionId.HeavensfallCast));
        world.Events.Add(MegaflarePuddles, CastMegaflarePuddles);
        world.Events.Add(Heavensfall, () => nael?.CastFromHelper(Actions.Heavensfall, at: new Placement(Vector3.Zero, 0f)));

        for (var k = 0; k < BurstCasts.Length; k++)
        {
            var step = k;
            world.Events.Add(BurstCasts[k], () => CastBursts(step));
        }
        foreach (var at in Hypernovas) world.Events.Add(at, DropHypernova);
        world.Events.Add(TwintaniaLands, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLand));
        world.Events.Add(FireballMarker, () => party.Get(state.FireballTarget)?.AttachLockonVfx(LockonId.Fireball));
        world.Events.Add(NaelLeaves, () => nael?.PlayActionTimeline(TimelineId.TrioLeave));
        world.Events.Add(BahamutReturns, ReturnBahamutToTheCentre);
        world.Events.Add(Gigaflare, () =>
        {
            bahamut?.SetTargetable(true);
            bahamut?.Cast(Actions.Gigaflare);
        });
        world.Events.Add(Fireball, () =>
        {
            if (party.Get(state.FireballTarget) is { } target && target.IsAlive()) twintania?.Cast(Actions.Fireball, target);
        });
        world.Events.Add(TwintaniaLeaves, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLeave));
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
        => twisters.Tick(world, (at, _) => twintania?.CastFromHelper(Actions.TwisterBlast, at: new Placement(ToWorld(at), 0f)));

    public void OnActionResolved(ActionResolved resolved)
        => Towers.ResolveTower(world, resolved, ActionId.MegaflareTower, Actions.MegaflareStrike, minSoakers: 1);

    private void SpawnBosses()
    {
        bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, Vector2.Zero, MathF.PI, true, EnemyListMode.Always, SpawnVisibility.Visible);
        nael = Spawn(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, Vector2.Zero, 0f, false, EnemyListMode.Never, SpawnVisibility.HiddenUntilShown);
        twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, Vector2.Zero, 0f, false, EnemyListMode.Never, SpawnVisibility.HiddenUntilShown);
    }

    private SimEnemy? Spawn(uint baseId, uint nameId, Vector2 at, float rotation, bool targetable, EnemyListMode list, SpawnVisibility visibility)
        => world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: targetable,
            EnemyList: list,
            Visibility: visibility,
            Placement: new Placement(ToWorld(at), rotation)));

    private void MoveTrioToTheEdge()
    {
        PlaceFacingTheCentre(bahamut, State.OnBearing(state.BahamutBearing, State.DiverDistance));
        PlaceFacingTheCentre(nael, State.OnBearing(state.NaelBearing, State.NaelDistance));
        PlaceFacingTheCentre(twintania, State.OnBearing(state.TwintaniaBearing, State.DiverDistance));
        nael?.SetVisible(true);
        twintania?.SetVisible(true);
    }

    private static void PlaceFacingTheCentre(SimEnemy? boss, Vector2 at)
        => boss?.SetPosition(new Placement(ToWorld(at), State.FacingOf(at, Vector2.Zero)));

    private void PlayTrioLand()
    {
        bahamut?.PlayActionTimeline(TimelineId.TrioLand);
        nael?.PlayActionTimeline(TimelineId.TrioLand);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLand);
    }

    private void MarkDiveTargets()
    {
        party.Get(state.MegaflareDiveTarget)?.AttachLockonVfx(LockonId.MegaflareDive);
        party.Get(state.TwistingDiveTarget)?.AttachLockonVfx(LockonId.TwistingDive);
    }

    // The line is aimed where the marked player stands as the cast starts.
    private void Dive(SimEnemy? diver, PartyRole role, EnemyAction dive)
    {
        if (diver == null) return;
        var aim = party.Get(role) is { } target ? Flat(target.Position) : Vector2.Zero;
        diver.SetRotation(State.FacingOf(Flat(diver.Position), aim));
        diver.Cast(dive);
    }

    private void DropTwisters()
    {
        foreach (var role in state.TwisterTargets)
            if (party.Get(role) is { } member && member.IsAlive())
                twisters.Drop(world, Flat(member.Position));
    }

    private void CastTowers()
    {
        foreach (var slot in state.TowerSlotsTaken)
        {
            var at = ToWorld(State.OnBearing(state.TowerBearing(slot), State.TowerDistance));
            bahamut?.CastFromHelper(Actions.MegaflareTower, at, new Placement(at, 0f));
        }
    }

    private void CastMegaflarePuddles()
    {
        foreach (var role in state.MegaflarePuddleTargets)
            if (party.Get(role) is { } member && member.IsAlive())
                bahamut?.CastFromHelper(Actions.MegaflarePuddle, member.Position, new Placement(member.Position, 0f));
    }

    private void CastBursts(int step)
    {
        var bearing = state.FirstBurstBearing + (state.BurstsClockwise ? 1f : -1f) * State.BurstStepDegrees * step;
        foreach (var aim in new[] { bearing, bearing + 180f })
            nael?.CastFromHelper(Actions.ThermionicBurst, at: new Placement(Vector3.Zero, State.RotationOfBearing(aim)));
    }

    // UNVERIFIED: the lingering puddles are only shown; standing in one does nothing here.
    private void DropHypernova()
    {
        if (party.Get(state.HypernovaTarget) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        nael?.CastFromHelper(Actions.Hypernova, at, new Placement(at, 0f));
        world.Events.Add(HypernovaPuddleAppears, () =>
        {
            var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.HypernovaPuddle,
                Placement = new Placement(at, 0f),
                TimelineState = 1,
            });
            if (eobj == null) return;
            puddles.Add(eobj);
            world.Events.Add(HypernovaPuddleLasts, () =>
            {
                eobj.Despawn();
                puddles.Remove(eobj);
            });
        });
    }

    private void ReturnBahamutToTheCentre()
    {
        bahamut?.SetPosition(new Placement(Vector3.Zero, MathF.PI));
        bahamut?.PlayActionTimeline(TimelineId.TrioLand);
    }

    private void DespawnAll()
    {
        twisters.Clear();
        foreach (var puddle in puddles) puddle.Despawn();
        puddles.Clear();
        bahamut?.Despawn();
        nael?.Despawn();
        twintania?.Despawn();
    }

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
