using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;
using State = AsuMech.Scenarios.Ucob.P3Blackfire.UcobP3BlackfireState;

namespace AsuMech.Scenarios.Ucob.P3Blackfire;

// Bahamut Prime's Blackfire Trio, from the trio cast to Gigaflare. The main tank holds Bahamut.
public sealed class UcobP3BlackfireScenario : IScenario
{
    public string Name => "Blackfire Trio";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3BlackfireAi()];

    private readonly UcobP3BlackfireSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float TrioCast = 3.47f;
    private const float AddsAppear = 3.52f;
    private const float BahamutUntargetable = 9.49f;
    private const float LeaveAnimations = 9.53f;
    private const float DragonsTakePlaces = 10.18f;
    public const float DragonsArrive = 10.65f;
    public const float LiquidHellMarker = 11.72f;
    public static readonly float[] LiquidHells = [11.81f, 12.97f, 14.12f, 15.28f, 16.45f];
    public const float MegaflareDiveCast = 11.85f;
    public const float MegaflareDiveResolves = 15.55f;
    public const float ThermionicBeam = 14.84f;
    private const float TwintaniaLeaves = 17.65f;
    private const float TwintaniaGone = 18.15f;
    public const float TowersCast = 17.83f;
    public const float TowersResolve = 25.53f;
    public const float StackMarkers = 18.76f;
    public static readonly float[] Hypernovas = [21.97f, 23.53f, 25.09f, 26.69f];
    public const float MegaflareStack = 23.88f;
    private const float BahamutReturns = 24.48f;
    private const float BahamutArrives = 25f;
    private const float BahamutTargetable = 26.07f;
    public const float GigaflareCast = 26.16f;
    private const float NaelLeaves = 28.29f;
    private const float NaelGone = 29f;
    public const float End = 34f;

    public const float DragonDistance = 23.57f;
    public const float BahamutDistance = 24f;
    public const float TwintaniaBearing = 90f;
    public const float BahamutBearing = 225f;
    public const float TowerDistance = 14f;
    public static readonly float[] TowerBearings = [45f, 135f, -135f, -45f];

    public const float PuddleAppears = 0.9f;
    public const float PuddleArms = 2.5f; // UNVERIFIED
    public const float PuddleFades = 12.6f;
    private const float PuddleGone = 13.6f;
    public const float PuddleRadius = 4f; // UNVERIFIED
    private const float BurnToDeath = 1f; // UNVERIFIED

    private static readonly Vector3 NaelBesideBahamut = new(-4.13f, 0f, 4.67f);
    private static readonly Vector3 TwintaniaBesideBahamut = new(6.44f, 0f, 4.07f);

    private const float MegaflareStackRadius = 5f;

    private SimWorld world = null!;
    private State state = null!;
    private BurningGround ground = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly List<SimEventObject> puddles = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new State(world.Rng, settingsWindow.Overrides);
        ground = new BurningGround(StatusId.Burns, BurnToDeath, "stood in a Liquid Hell puddle (lead them away from the party)");
        puddles.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnDragons);
        world.Events.Add(0.5f, TankBahamut);

        world.Events.Add(TrioCast, () => bahamut?.Cast(ActionId.BlackfireTrio, animationLock: 1.1f));
        world.Events.Add(AddsAppear, AddsAppearBesideBahamut);
        world.Events.Add(BahamutUntargetable, () =>
        {
            bahamut?.SetTargetable(false);
            bahamut?.KeepFacing(null);
        });
        world.Events.Add(LeaveAnimations, PlayLeaveAnimations);
        world.Events.Add(DragonsTakePlaces, PlaceDragons);
        world.Events.Add(DragonsArrive, PlayArriveAnimations);
        world.Events.Add(LiquidHellMarker, () => world.Party.Get(state.LiquidHellTarget)?.AttachLockonVfx(LockonId.LiquidHell));
        foreach (var at in LiquidHells) world.Events.Add(at, DropLiquidHell);
        world.Events.Add(MegaflareDiveCast, () => bahamut?.Cast(Actions.MegaflareDive));
        world.Events.Add(ThermionicBeam, () =>
        {
            if (world.Party.Get(state.BeamTarget) is { } target && target.IsAlive()) nael?.Cast(Actions.ThermionicBeam, target);
        });
        world.Events.Add(TwintaniaLeaves, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaLeaves));
        world.Events.Add(TwintaniaGone, () => twintania?.FadeOut());
        world.Events.Add(TowersCast, CastTowers);
        world.Events.Add(StackMarkers, () =>
        {
            foreach (var member in Alive(state.StackTargets)) member.AttachLockonVfx(LockonId.MegaflareStack);
        });
        for (var i = 0; i < Hypernovas.Length; i++)
        {
            var role = state.HypernovaTargets[i];
            world.Events.Add(Hypernovas[i], () => Hypernova(role));
        }
        world.Events.Add(MegaflareStack, StackMegaflare);
        world.Events.Add(BahamutReturns, () => bahamut?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(BahamutArrives, () => bahamut?.PlayActionTimeline(TimelineId.DragonArrives));
        world.Events.Add(BahamutTargetable, () =>
        {
            bahamut?.SetTargetable(true);
            TankBahamut();
        });
        world.Events.Add(GigaflareCast, () => bahamut?.Cast(Actions.Gigaflare));
        world.Events.Add(NaelLeaves, () => nael?.PlayActionTimeline(TimelineId.NaelLeaves));
        world.Events.Add(NaelGone, () => nael?.FadeOut());
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => ground.Tick(world.Party, delta, elapsed);

    public void OnActionResolved(ActionResolved resolved)
    {
        Towers.ResolveTower(world, resolved, ActionId.MegaflareTower, Actions.MegaflareStrike, minSoakers: 1);
        if (resolved.Action != Actions.MegaflareStackOfFour) return;
        foreach (var marked in Alive(state.StackTargets).Where(m => !resolved.Hits.Contains(m)))
            marked.Die(ActionId.MegaflareStack, "missed the Megaflare stack (the marked players stack together)");
    }

    // `bearing` is clockwise of Nael, in degrees.
    public static Vector3 AroundNael(Direction naelFrom, float bearing, float distance)
    {
        var radians = naelFrom.RadiansFromNorth + bearing * MathF.PI / 180f;
        return new Vector3(MathF.Sin(radians), 0f, -MathF.Cos(radians)) * distance;
    }

    private void SpawnDragons()
    {
        bahamut = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.BahamutPrime,
            NameId: BNpcNameId.BahamutPrime,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        nael = SpawnAdd(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, NaelBesideBahamut);
        twintania = SpawnAdd(BNpcBaseId.Twintania, BNpcNameId.Twintania, TwintaniaBesideBahamut);
    }

    private SimEnemy? SpawnAdd(uint baseId, uint nameId, Vector3 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: baseId,
        NameId: nameId,
        Level: Level,
        Targetable: false,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.HiddenUntilShown,
        Placement: new Placement(at, MathF.PI)));

    private void TankBahamut()
    {
        var tank = world.Party.Get(PartyRole.MainTank);
        bahamut?.SetTarget(tank, follow: false);
        bahamut?.KeepFacing(tank);
    }

    private void AddsAppearBesideBahamut()
    {
        nael?.SetVisible(true);
        twintania?.SetVisible(true);
        nael?.PlayActionTimeline(TimelineId.DragonArrives);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaArrives);
    }

    private void PlayLeaveAnimations()
    {
        bahamut?.PlayActionTimeline(TimelineId.BahamutLeaves);
        nael?.PlayActionTimeline(TimelineId.NaelLeaves);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaLeaves);
    }

    private void PlaceDragons()
    {
        nael?.SetPosition(FacingTheCentre(AroundNael(state.NaelFrom, 0f, DragonDistance)));
        twintania?.SetPosition(FacingTheCentre(AroundNael(state.NaelFrom, TwintaniaBearing, DragonDistance)));
        bahamut?.SetPosition(FacingTheCentre(AroundNael(state.NaelFrom, BahamutBearing, BahamutDistance)));
    }

    private static Placement FacingTheCentre(Vector3 at) => new Placement(at, 0f).Face(Vector3.Zero);

    private void PlayArriveAnimations()
    {
        bahamut?.PlayActionTimeline(TimelineId.DragonArrives);
        nael?.PlayActionTimeline(TimelineId.DragonArrives);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaArrives);
    }

    private void DropLiquidHell()
    {
        if (world.Party.Get(state.LiquidHellTarget) is not { } target || !target.IsAlive() || twintania is not { } twin) return;
        var at = target.Position;
        var now = world.Events.Elapsed;
        twin.CastFromHelper(Actions.LiquidHell, at, at: twin.Placement());
        ground.AddCircle(ActionId.LiquidHell, now + PuddleArms, at, PuddleRadius, now + PuddleFades);
        world.Events.Add(PuddleAppears, () =>
        {
            var puddle = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.LiquidHell,
                Placement = new Placement(at, 0f),
                TimelineState = 1,
            });
            if (puddle == null) return;
            puddles.Add(puddle);
            world.Events.Add(PuddleFades - PuddleAppears, puddle.FadeOut);
            world.Events.Add(PuddleGone - PuddleAppears, puddle.Despawn);
        });
    }

    private void CastTowers()
    {
        foreach (var bearing in TowerBearings)
        {
            var at = AroundNael(state.NaelFrom, bearing, TowerDistance);
            bahamut?.CastFromHelper(Actions.MegaflareTower, at, at: new Placement(at, 0f));
        }
    }

    private void Hypernova(PartyRole role)
    {
        if (world.Party.Get(role) is not { } target || !target.IsAlive() || nael is not { } boss) return;
        boss.CastFromHelper(Actions.Hypernova, target.Position, at: boss.Placement());
    }

    // On whichever marked player has the most of the others with them.
    private void StackMegaflare()
    {
        var marked = Alive(state.StackTargets).ToList();
        var center = marked.OrderByDescending(m => marked.Count(o => Vector3.Distance(o.Position, m.Position) <= MegaflareStackRadius)).FirstOrDefault();
        if (center == null) return;
        bahamut?.CastFromHelper(Actions.MegaflareStackOfFour, center, at: new Placement(center.Position, 0f));
    }

    private void DespawnAll()
    {
        ground.PutOut(world.Party);
        foreach (var puddle in puddles) puddle.Despawn();
        puddles.Clear();
        foreach (var enemy in new[] { bahamut, nael, twintania }) enemy?.Despawn();
    }

    private IEnumerable<SimCharacter> Alive(IEnumerable<PartyRole> roles)
    {
        foreach (var role in roles)
            if (world.Party.Get(role) is { } member && member.IsAlive())
                yield return member;
    }
}
