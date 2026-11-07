using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using State = AsuMech.Scenarios.Ucob.P3GrandOctet.UcobP3GrandOctetState;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

// Grand Octet: the eight take the rim, each player gets one marker, and each one dives at its own.
public sealed class UcobP3GrandOctetScenario : IScenario
{
    public string Name => "Grand Octet";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3GrandOctetAi(tankLimitBreak: true), new UcobP3GrandOctetAi(tankLimitBreak: false)];

    private readonly UcobP3GrandOctetSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public static readonly Vector2 NaelSpot = new(-4.84f, 4.70f);
    public static readonly Vector2 TwintaniaSpot = new(5.68f, 5.86f);
    private static readonly uint[] DragonBaseIds = [BNpcBaseId.Firehorn, BNpcBaseId.Iceclaw, BNpcBaseId.Thunderwing, BNpcBaseId.TailOfDarkness, BNpcBaseId.FangOfLight];
    private static readonly uint[] DragonNameIds = [BNpcNameId.Firehorn, BNpcNameId.Iceclaw, BNpcNameId.Thunderwing, BNpcNameId.TailOfDarkness, BNpcNameId.FangOfLight];

    public const float GrandOctetCast = 5.00f;
    public const float NaelAndTwintaniaAppear = 5.04f;
    public const float DragonsAppear = 5.07f;
    public const float BahamutUntargetable = 11.01f;
    public const float TrioLeaves = 11.05f;
    private const float FadesAfterLeaving = 0.55f;
    public const float TrioOnTheRim = 12.12f;
    private const float ArrivalAfterSpawn = 0.17f;
    public static readonly float[] DragonMarkers = [17.24f, 19.25f, 21.25f, 23.25f, 25.26f];
    public const float NaelMarker = 17.24f;
    public const float LunarDiveCast = 17.38f;
    public const float NaelLeaves = 23.53f;
    public static readonly float[] CauterizeCasts = [24.46f, 26.51f, 28.56f, 30.61f, 32.62f];
    private const float DragonFadesAfterCast = 5.4f;
    public const float BahamutMarker = 34.27f;
    public const float MegaflareDiveCast = 34.62f;
    public const float TowersCast = 40.38f;
    public const float BahamutLeaves = 40.71f;
    public const float StackMarkers = 41.27f;
    public const float TwintaniaMarker = 43.27f;
    public const float TwistingDiveCast = 43.63f;
    public const float Stacks = 46.39f;
    public const float Twisters = 47.65f;
    public const float TwintaniaLeaves = 49.78f;
    public const float End = 55f;
    // Retail reaches here with the gauge full.
    private const float LimitBreakBars = 3f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private State state = null!;
    private UcobTwisters twisters = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly SimEnemy?[] dragons = new SimEnemy?[State.DragonCount];
    private readonly List<SimEnemy> spawned = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new State(world.Rng, settingsWindow.Overrides);
        twisters = new UcobTwisters(world);
        spawned.Clear();
        Array.Clear(dragons);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, () => party.LimitBreak.Set(LimitBreakBars));
        world.Events.Add(0f, () => bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, Vector2.Zero, MathF.PI, targetable: true));

        world.Events.Add(GrandOctetCast, () => bahamut?.Cast(ActionId.GrandOctet, animationLock: 3.1f));
        world.Events.Add(NaelAndTwintaniaAppear, () =>
        {
            nael = Spawn(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, NaelSpot, MathF.PI, targetable: false);
            twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, TwintaniaSpot, MathF.PI, targetable: false);
        });
        world.Events.Add(DragonsAppear, SpawnDragons);
        world.Events.Add(BahamutUntargetable, () => bahamut?.SetTargetable(false));
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
        world.Events.Add(TrioOnTheRim, SpawnTrioOnTheRim);
        world.Events.Add(TrioOnTheRim + ArrivalAfterSpawn, () =>
        {
            bahamut?.PlayActionTimeline(TimelineId.NaelOrBahamutArrives);
            nael?.PlayActionTimeline(TimelineId.NaelOrBahamutArrives);
            twintania?.PlayActionTimeline(TimelineId.TwintaniaArrives);
        });

        world.Events.Add(NaelMarker, () => Mark(state.NaelTarget, LockonId.LunarDive, nael));
        world.Events.Add(LunarDiveCast, () => nael?.Cast(UcobActions.LunarDive));
        world.Events.Add(NaelLeaves, () => nael?.PlayActionTimeline(TimelineId.NaelLeaves));
        world.Events.Add(NaelLeaves + FadesAfterLeaving, () => nael?.FadeOut());
        for (var k = 0; k < State.DragonCount; k++)
        {
            var target = state.DragonTargets[k];
            var dragon = state.DiveOrder[k];
            world.Events.Add(DragonMarkers[k], () => Mark(target, LockonId.DragonDive, null));
            world.Events.Add(CauterizeCasts[k], () => Cauterize(dragon, target));
            world.Events.Add(CauterizeCasts[k] + DragonFadesAfterCast, () => dragons[dragon]?.FadeOut());
        }

        world.Events.Add(BahamutMarker, () => Mark(state.BahamutTarget, LockonId.MegaflareDive, bahamut));
        world.Events.Add(MegaflareDiveCast, () => bahamut?.Cast(UcobActions.MegaflareDive));
        world.Events.Add(TowersCast, CastTowers);
        world.Events.Add(BahamutLeaves, () => bahamut?.PlayActionTimeline(TimelineId.BahamutLeaves));
        world.Events.Add(BahamutLeaves + FadesAfterLeaving, () => bahamut?.FadeOut());
        world.Events.Add(StackMarkers, () => { foreach (var role in state.StackTargets) Mark(role, LockonId.MegaflareStack, null); });
        world.Events.Add(TwintaniaMarker, () => Mark(state.TwintaniaTarget, LockonId.TwistingDive, twintania));
        world.Events.Add(TwistingDiveCast, () => twintania?.Cast(UcobActions.TwistingDive));
        world.Events.Add(Stacks, MegaflareStacks);
        world.Events.Add(Twisters, DropTwisters);
        world.Events.Add(TwintaniaLeaves, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaLeaves));
        world.Events.Add(TwintaniaLeaves + FadesAfterLeaving, () => twintania?.FadeOut());
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => twisters.Tick();

    public void OnActionResolved(ActionResolved resolved)
        => Towers.ResolveTower(world, resolved, ActionId.MegaflareTower, UcobActions.MegaflareStrike, minSoakers: 1);

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

    private SimEnemy? SpawnOnTheRim(uint baseId, uint nameId, int slot)
    {
        var at = State.SlotPosition(slot);
        return Spawn(baseId, nameId, at, FacingCentre(at), targetable: false);
    }

    private void SpawnDragons()
    {
        for (var d = 0; d < State.DragonCount; d++)
            dragons[d] = SpawnOnTheRim(DragonBaseIds[d], DragonNameIds[d], state.DragonSlots[d]);
    }

    private void SpawnTrioOnTheRim()
    {
        bahamut = SpawnOnTheRim(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, state.BahamutSlot);
        nael = SpawnOnTheRim(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, state.NaelSlot);
        twintania = SpawnOnTheRim(BNpcBaseId.Twintania, BNpcNameId.Twintania, state.TwintaniaSlot);
    }

    // A boss's dive is aimed the moment its marker shows; a dragon's when its marker goes.
    private void Mark(PartyRole role, uint lockonId, SimEnemy? aimer)
    {
        if (party.Get(role) is not { } member || !member.IsAlive()) return;
        member.AttachLockonVfx(lockonId);
        aimer?.SetRotation(aimer.Placement().Face(member.Position).Rotation);
    }

    private void Cauterize(int dragon, PartyRole role)
    {
        if (dragons[dragon] is not { } caster) return;
        if (party.Get(role) is { } member && member.IsAlive()) caster.SetRotation(caster.Placement().Face(member.Position).Rotation);
        caster.Cast(UcobActions.Cauterize[dragon]);
    }

    private void CastTowers()
    {
        foreach (var spot in State.TowerSpots)
        {
            var at = ToWorld(spot);
            bahamut?.CastFromHelper(UcobActions.MegaflareTower, at, new Placement(at, 0f));
        }
    }

    private void MegaflareStacks()
    {
        foreach (var role in state.StackTargets)
            if (party.Get(role) is { } member && member.IsAlive())
                bahamut?.CastFromHelper(UcobActions.MegaflareStackOfTwo, member, member.Placement());
    }

    private void DropTwisters()
    {
        if (twintania is not { } boss) return;
        var spots = state.TwisterTargets.Select(r => party.Get(r)).OfType<SimCharacter>().Where(m => m.IsAlive()).Select(m => Flat(m.Position)).ToList();
        twisters.Drop(boss, spots);
    }

    private void DespawnAll()
    {
        twisters.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
    }

    private static float FacingCentre(Vector2 at) => MathF.Atan2(-at.X, -at.Y);

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
