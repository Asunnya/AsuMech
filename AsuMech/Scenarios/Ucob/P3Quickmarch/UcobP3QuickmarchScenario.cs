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
using State = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchState;

namespace AsuMech.Scenarios.Ucob.P3Quickmarch;

// Bahamut Prime's Quickmarch Trio, from the trio cast to Flare Breath. The main tank holds Bahamut.
public sealed class UcobP3QuickmarchScenario : IScenario
{
    public string Name => "Quickmarch Trio";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3QuickmarchAi()];

    private readonly UcobP3QuickmarchSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float TrioCast = 3.41f;
    private const float AddsAppear = 3.46f;
    public const float BahamutUntargetable = 9.42f;
    private const float LeaveAnimations = 9.47f;
    private const float DragonsTakePlaces = 10.2f;
    public const float DragonsArrive = 10.58f;
    public const float DivesCast = 11.74f;
    public const float DivesResolve = 15.44f;
    public const float TwistersDrop = 16.44f;
    private const float AddsLeave = 17.9f;
    public const float StackMarkers = 18.69f;
    public const float PuddlesCast = 18.78f;
    public const float PuddlesResolve = 21.48f;
    private const float BahamutReturns = 19.64f;
    public const float Spreads = 19.76f;
    private const float BahamutArrives = 19.9f;
    private const float BahamutTargetable = 20.96f;
    public const float EarthShakerMarkers = 21.01f;
    public const float Tethers = 22.03f;
    public const float MegaflareStack = 23.81f;
    public const float EarthShakers = 26.13f;
    public const float TempestWing = 28.13f;
    public static readonly float[] TempestWingBlasts = [29.29f, 29.42f];
    public const float FlareBreath = 32.23f;
    public const float End = 35f;

    public const float DiveDistance = 22f;
    public const float DiveSpacing = 10f;
    public const float TwisterArms = 0.9f;
    public const float TwisterFades = 5.56f;
    private const float TwisterGone = 6.62f;
    public const float TwisterTriggerRadius = 1.25f;

    private static readonly Vector3 NaelBesideBahamut = new(-4.72f, 0f, 4.82f);
    private static readonly Vector3 TwintaniaBesideBahamut = new(5.83f, 0f, 5.71f);

    private const float MegaflareStackRadius = 5f;

    private SimWorld world = null!;
    private State state = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly List<Twister> twisters = [];

    private sealed class Twister(Vector2 at, SimEventObject? eobj, float armsAt, float fadesAt)
    {
        public Vector2 At { get; } = at;
        public SimEventObject? EObj { get; } = eobj;
        public float ArmsAt { get; } = armsAt;
        public float FadesAt { get; } = fadesAt;
        public bool Spent { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new State(world.Rng, settingsWindow.Overrides);
        twisters.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnDragons);
        world.Events.Add(0.5f, TankBahamut);

        world.Events.Add(TrioCast, () => bahamut?.Cast(ActionId.QuickmarchTrio, animationLock: 1.1f));
        world.Events.Add(AddsAppear, AddsAppearBesideBahamut);
        world.Events.Add(BahamutUntargetable, () =>
        {
            bahamut?.SetTargetable(false);
            bahamut?.KeepFacing(null);
        });
        world.Events.Add(LeaveAnimations, PlayLeaveAnimations);
        world.Events.Add(DragonsTakePlaces, PlaceDivers);
        world.Events.Add(DragonsArrive, PlayArriveAnimations);
        world.Events.Add(DivesCast, () =>
        {
            bahamut?.Cast(Actions.MegaflareDive);
            nael?.Cast(Actions.LunarDive);
            twintania?.Cast(Actions.TwistingDive);
        });
        world.Events.Add(TwistersDrop, DropTwisters);
        world.Events.Add(AddsLeave, () =>
        {
            nael?.FadeOut();
            twintania?.FadeOut();
        });
        world.Events.Add(StackMarkers, () => Mark(state.StackTargets, LockonId.MegaflareStack));
        world.Events.Add(PuddlesCast, DropMegaflarePuddles);
        world.Events.Add(BahamutReturns, () => bahamut?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(Spreads, MegaflareSpreads);
        world.Events.Add(BahamutArrives, () => bahamut?.PlayActionTimeline(TimelineId.DragonArrives));
        world.Events.Add(BahamutTargetable, () =>
        {
            bahamut?.SetTargetable(true);
            TankBahamut();
        });
        world.Events.Add(EarthShakerMarkers, () => Mark(state.EarthShakerTargets, LockonId.EarthShaker));
        world.Events.Add(Tethers, TetherTanks);
        world.Events.Add(MegaflareStack, StackMegaflare);
        world.Events.Add(EarthShakers, CastEarthShakers);
        world.Events.Add(TempestWing, () =>
        {
            foreach (var tank in Alive(State.TetherTargets)) bahamut?.CastFromHelper(Actions.TempestWing, tank);
        });
        for (var i = 0; i < State.TetherTargets.Length; i++)
        {
            var role = State.TetherTargets[i];
            world.Events.Add(TempestWingBlasts[i], () =>
            {
                if (world.Party.Get(role) is { } tank && tank.IsAlive())
                    bahamut?.CastFromHelper(Actions.TempestWingBlast, tank, at: new Placement(tank.Position, 0f));
            });
        }
        world.Events.Add(FlareBreath, () =>
        {
            if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive()) bahamut?.Cast(Actions.FlareBreath, tank);
        });
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => TriggerTwisters(elapsed);

    public void OnActionResolved(ActionResolved resolved)
    {
        if (resolved.Action != Actions.MegaflareStackOfThree) return;
        foreach (var marked in Alive(state.StackTargets).Where(m => !resolved.Hits.Contains(m)))
            marked.Die(ActionId.MegaflareStack, "missed the Megaflare stack (the marked players stack together)");
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
        bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands);
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

    private void PlaceDivers()
    {
        var rotation = -state.DiveFrom.RadiansFromNorth;
        bahamut?.SetPosition(new Placement(DiveStart(0f), rotation));
        nael?.SetPosition(new Placement(DiveStart(DiveSpacing), rotation));
        twintania?.SetPosition(new Placement(DiveStart(-DiveSpacing), rotation));
    }

    // `lateral` is clockwise of the dive's edge, as seen from the arena centre.
    private Vector3 DiveStart(float lateral)
    {
        var theta = state.DiveFrom.RadiansFromNorth;
        var edge = new Vector3(MathF.Sin(theta), 0f, -MathF.Cos(theta));
        var clockwise = new Vector3(MathF.Cos(theta), 0f, MathF.Sin(theta));
        return edge * DiveDistance + clockwise * lateral;
    }

    private void PlayArriveAnimations()
    {
        bahamut?.PlayActionTimeline(TimelineId.DragonArrives);
        nael?.PlayActionTimeline(TimelineId.DragonArrives);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaArrives);
    }

    private void DropTwisters()
    {
        var now = world.Events.Elapsed;
        foreach (var member in Alive(state.TwisterTargets))
        {
            var at = Flat(member.Position);
            var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.Twister,
                Placement = new Placement(member.Position, 0f),
                TimelineState = 1,
            });
            var twister = new Twister(at, eobj, now + TwisterArms, now + TwisterFades);
            twisters.Add(twister);
            world.Events.Add(TwisterFades, () => eobj?.FadeOut());
            world.Events.Add(TwisterGone, () =>
            {
                eobj?.Despawn();
                twisters.Remove(twister);
            });
        }
    }

    private void TriggerTwisters(float now)
    {
        foreach (var twister in twisters.Where(t => !t.Spent && now >= t.ArmsAt && now < t.FadesAt).ToList())
        {
            if (!world.Party.ActiveMembers().Any(m => m.IsAlive() && Vector2.Distance(Flat(m.Position), twister.At) <= TwisterTriggerRadius)) continue;
            twister.Spent = true;
            twister.EObj?.Despawn();
            var at = new Vector3(twister.At.X, 0f, twister.At.Y);
            twintania?.CastFromHelper(Actions.TwisterBlast, at, at: new Placement(at, 0f));
        }
    }

    private void Mark(IEnumerable<PartyRole> roles, uint lockonId)
    {
        foreach (var member in Alive(roles)) member.AttachLockonVfx(lockonId);
    }

    private void DropMegaflarePuddles()
    {
        foreach (var member in Alive(state.PuddleTargets))
        {
            var at = member.Position;
            bahamut?.CastFromHelper(Actions.MegaflarePuddle, at, at: new Placement(at, 0f));
        }
    }

    private void MegaflareSpreads()
    {
        foreach (var member in Alive(state.SpreadTargets))
            bahamut?.CastFromHelper(Actions.MegaflareSpread, member, at: new Placement(member.Position, 0f));
    }

    private void TetherTanks()
    {
        foreach (var tank in Alive(State.TetherTargets))
            world.Tether(bahamut, tank, TetherId.TempestWing, TempestWing - Tethers);
    }

    // On whichever marked player has the most of the others with them.
    private void StackMegaflare()
    {
        var marked = Alive(state.StackTargets).ToList();
        var center = marked.OrderByDescending(m => marked.Count(o => Vector3.Distance(o.Position, m.Position) <= MegaflareStackRadius)).FirstOrDefault();
        if (center == null) return;
        bahamut?.CastFromHelper(Actions.MegaflareStackOfThree, center, at: new Placement(center.Position, 0f));
    }

    private void CastEarthShakers()
    {
        if (bahamut is not { } boss) return;
        boss.Cast(ActionId.EarthShaker, animationLock: 1.1f);
        foreach (var member in Alive(state.EarthShakerTargets))
            boss.CastFromHelper(Actions.EarthShakerCone, member, at: boss.Placement().Face(member.Position));
    }

    private void DespawnAll()
    {
        foreach (var twister in twisters) twister.EObj?.Despawn();
        twisters.Clear();
        foreach (var enemy in new[] { bahamut, nael, twintania }) enemy?.Despawn();
    }

    private IEnumerable<SimCharacter> Alive(IEnumerable<PartyRole> roles)
    {
        foreach (var role in roles)
            if (world.Party.Get(role) is { } member && member.IsAlive())
                yield return member;
    }

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
