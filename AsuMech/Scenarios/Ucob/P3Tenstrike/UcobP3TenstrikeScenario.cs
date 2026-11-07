using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using State = AsuMech.Scenarios.Ucob.P3Tenstrike.UcobP3TenstrikeState;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

// Tenstrike Trio to its Gigaflare: two Generates on the same three, Meteor Streams, two Earth Shaker sets.
public sealed class UcobP3TenstrikeScenario : IScenario
{
    public string Name => "Tenstrike Trio";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3TenstrikeAi()];

    private readonly UcobP3TenstrikeSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public static readonly Vector2 NaelSpot = new(-4.97f, 4.57f);
    public static readonly Vector2 TwintaniaSpot = new(5.52f, 6.0f);

    public const float TenstrikeCast = 5.00f;
    public const float TrioUntargetable = 11.02f;
    public const float TrioLeaves = 11.06f;
    private const float FadesAfterLeaving = 0.55f;
    public const float TwintaniaReturns = 12.13f;
    public static readonly float[] GenerateCasts = [13.34f, 17.48f];
    private const float HatchMarkerLead = 0.09f;
    private const float GenerateCastSeconds = 2.7f;
    public const float OviformChaseAfterCast = 1.25f;
    public static readonly float[] MeteorStreams = [17.26f, 18.28f, 19.26f, 20.28f, 21.26f, 22.29f, 23.26f, 24.29f];
    public const float TwintaniaLeaves = 24.64f;
    public static readonly float[] EarthShakerMarkers = [25.18f, 30.34f];
    public const float BahamutReturns = 29.10f;
    private const float ArrivalAfterSpawn = 0.17f;
    public static readonly float[] EarthShakers = [30.43f, 35.47f];
    public const float PuddleAppears = 1.09f;
    public const float PuddleFades = 8.68f;
    private const float PuddleGone = 9.0f;
    public const float PuddleRadius = 4f;
    public const float GigaflareCast = 37.56f;
    public const float End = 46f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private State state = null!;
    private UcobHatches hatches = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly List<SimEnemy> spawned = [];
    private readonly List<SimEventObject> eobjs = [];
    private readonly List<(Vector2 At, float Until)> puddles = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new State(world.Rng, settingsWindow.Overrides);
        hatches = new UcobHatches(world);
        spawned.Clear();
        eobjs.Clear();
        puddles.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnTrio);
        world.Events.Add(0f, () => { foreach (var at in Geometry.Neurolinks) hatches.AddNeurolink(at); });

        world.Events.Add(TenstrikeCast, () => bahamut?.Cast(ActionId.TenstrikeTrio, animationLock: 3.1f));
        world.Events.Add(TrioUntargetable, () => bahamut?.SetTargetable(false));
        world.Events.Add(TrioLeaves, () =>
        {
            bahamut?.PlayActionTimeline(TimelineId.BahamutLeaves);
            nael?.PlayActionTimeline(TimelineId.NaelLeaves);
            twintania?.PlayActionTimeline(TimelineId.TwintaniaLeaves);
        });
        world.Events.Add(TrioLeaves + FadesAfterLeaving, () =>
        {
            bahamut?.FadeOut();
            nael?.FadeOut();
            twintania?.FadeOut();
        });

        world.Events.Add(TwintaniaReturns, () => twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, Vector2.Zero, MathF.PI, targetable: false));
        world.Events.Add(TwintaniaReturns + ArrivalAfterSpawn, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaArrives));
        foreach (var at in GenerateCasts)
        {
            world.Events.Add(at - HatchMarkerLead, Generate);
            world.Events.Add(at, () => twintania?.Cast(ActionId.Generate, animationLock: 1.1f));
            world.Events.Add(at + GenerateCastSeconds + OviformChaseAfterCast, hatches.Chase);
        }
        for (var i = 0; i < MeteorStreams.Length; i++)
        {
            var target = state.MeteorOrder[i];
            world.Events.Add(MeteorStreams[i], () => MeteorStream(target));
        }
        world.Events.Add(TwintaniaLeaves, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaLeaves));
        world.Events.Add(TwintaniaLeaves + FadesAfterLeaving, () => twintania?.FadeOut());

        IReadOnlyList<PartyRole>[] shakerSets = [state.FirstEarthShakers, state.SecondEarthShakers];
        for (var set = 0; set < shakerSets.Length; set++)
        {
            var targets = shakerSets[set];
            world.Events.Add(EarthShakerMarkers[set], () => Mark(targets, LockonId.EarthShaker));
            world.Events.Add(EarthShakers[set], () => EarthShaker(targets));
        }
        world.Events.Add(BahamutReturns, () =>
        {
            bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, Vector2.Zero, MathF.PI, targetable: false);
            bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands);
        });
        world.Events.Add(BahamutReturns + ArrivalAfterSpawn, () => bahamut?.PlayActionTimeline(TimelineId.NaelOrBahamutArrives));
        world.Events.Add(EarthShakerMarkers[1], () => bahamut?.SetTargetable(true));

        world.Events.Add(GigaflareCast, () => bahamut?.Cast(UcobActions.Gigaflare));
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        hatches.Tick();
        KillInPuddles(world.Events.Elapsed);
    }

    private void SpawnTrio()
    {
        bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, Vector2.Zero, MathF.PI, targetable: true);
        bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands);
        nael = Spawn(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, NaelSpot, MathF.PI, targetable: false);
        twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, TwintaniaSpot, MathF.PI, targetable: false);
    }

    private SimEnemy? Spawn(uint baseId, uint nameId, Vector2 at, float rotation, bool targetable)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: targetable,
            EnemyList: targetable ? EnemyListMode.Always : EnemyListMode.Never,
            Placement: new Placement(ToWorld(at), rotation)));
        if (enemy != null) spawned.Add(enemy);
        return enemy;
    }

    private void Generate()
    {
        if (twintania is { } boss) hatches.Generate(boss, Members(state.HatchTargets));
    }

    private void MeteorStream(PartyRole role)
    {
        if (party.Get(role) is { } target && target.IsAlive())
            nael?.CastFromHelper(UcobActions.MeteorStreamSpread, target, at: target.Placement());
    }

    private void Mark(IEnumerable<PartyRole> roles, uint lockonId)
    {
        foreach (var member in Members(roles)) member.AttachLockonVfx(lockonId);
    }

    private void EarthShaker(IReadOnlyList<PartyRole> roles)
    {
        bahamut?.Cast(ActionId.EarthShaker, animationLock: 1.1f);
        var spots = new List<Vector2>();
        foreach (var member in Members(roles))
        {
            spots.Add(Flat(member.Position));
            bahamut?.CastFromHelper(UcobActions.EarthShakerCone, member, at: new Placement(Vector3.Zero, 0f));
        }
        world.Events.Add(PuddleAppears, () => DropPuddles(spots));
    }

    private void DropPuddles(IEnumerable<Vector2> spots)
    {
        foreach (var at in spots)
        {
            var puddle = (at, world.Events.Elapsed + PuddleFades);
            puddles.Add(puddle);
            var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.EarthShakerPuddle,
                Placement = new Placement(ToWorld(at), 0f),
                TimelineState = 1,
            });
            if (eobj != null) eobjs.Add(eobj);
            world.Events.Add(PuddleFades, () =>
            {
                puddles.Remove(puddle);
                eobj?.FadeOut();
            });
            world.Events.Add(PuddleGone, () => eobj?.SetState(0));
        }
    }

    private void KillInPuddles(float now)
    {
        foreach (var member in party.ActiveMembers().Where(m => m.IsAlive()).ToList())
            if (puddles.Any(p => now < p.Until && Vector2.Distance(Flat(member.Position), p.At) <= PuddleRadius))
                member.Die(ActionId.EarthShaker, "stood in an Earth Shaker puddle (leave your spot as the cone goes off)");
    }

    private IEnumerable<SimCharacter> Members(IEnumerable<PartyRole> roles)
        => roles.Select(r => party.Get(r)).OfType<SimCharacter>().Where(m => m.IsAlive());

    private void DespawnAll()
    {
        hatches.Clear();
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        puddles.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
    }

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
