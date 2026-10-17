using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P2DiamondDust.FruP2DiamondDustState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P2DiamondDust;

public sealed class FruP2DiamondDustScenario : IScenario
{
    public string Name => "Diamond Dust";
    public IPhase Phase => FruZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP2DiamondDustAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP2DiamondDustSettingsWindow settingsWindow = new();

    // UNVERIFIED: no pull has anyone in a live puddle. Retail players are still within 6y of one
    // up to 1.23s after it shows, unhurt, so it arms no sooner than that.
    private const float PuddleArmsAfterShowing = 1.5f;
    private const float PuddleLastsAfterShowing = 14.1f;

    private SimWorld world = null!;
    private FruP2DiamondDustState state = null!;
    private SimEnemy? usurper, mirror;
    private readonly SimEnemy?[] icicleHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] needleHelpers = new SimEnemy?[12];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] reflectionHelpers = new SimEnemy?[2];
    private readonly List<Vector3> stars = [];
    private readonly FruHolyPuddles puddles = new();

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP2DiamondDustState(world.Rng, settingsWindow.Overrides);
        stars.Clear();
        puddles.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP2DiamondDustState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0.6f, () => usurper?.SetModelState(4));
        world.Events.Add(0.68f, () => DashStart(usurper, UsurperOpeningSpot));
        world.Events.Add(1.35f, () => DashArrive(usurper, UsurperOpeningSpot));
        world.Events.Add(1.67f, () => usurper?.TurnTo(MathF.PI));
        world.Events.Add(2f, () => usurper?.Cast(ActionId.MirrorImage, animationLock: 2.1f));
        world.Events.Add(7.05f, () => world.Map.AddEffect(MapEffect.DiamondDustStart, MapEffect.DiamondDust));
        world.Events.Add(7.1f, () => usurper?.VoiceLine(VoiceLineId.DiamondDust));
        world.Events.Add(7.1f, () => usurper?.Cast(Actions.DiamondDust));
        world.Events.Add(8.33f, ShowMirror);
        world.Events.Add(11.15f, () => world.Map.AddEffect(MapEffect.DiamondDustEnd, MapEffect.DiamondDust));
        world.Events.Add(14.65f, () => DropIcicles(0, 0));
        world.Events.Add(14.78f, () => mirror?.TurnTo(MathF.PI));
        world.Events.Add(14.95f, () => mirror?.VoiceLine(state.Axe ? VoiceLineId.AxeKick : VoiceLineId.ScytheKick));
        world.Events.Add(14.95f, () => mirror?.Cast(state.Axe ? Actions.AxeKick : Actions.ScytheKick));
        world.Events.Add(15.13f, () => usurper?.SetTargetable(false));
        world.Events.Add(15.13f, MarkFrigidStones);
        world.Events.Add(15.22f, () => usurper?.PlayActionTimeline(TimelineId.UsurperHide));
        world.Events.Add(15.93f, () => mirror?.SetModeAttributeFlags(0xF9));
        world.Events.Add(18.7f, () => DropIcicles(1, 2));
        world.Events.Add(21.7f, HouseOfLight);
        world.Events.Add(22.33f, () => mirror?.SetModeAttributeFlags(0xFC));
        world.Events.Add(22.7f, () => DropIcicles(2, 6));
        world.Events.Add(23.3f, FrigidStones);
        world.Events.Add(24.48f, () => DashStart(mirror, state.MirrorPosition));
        world.Events.Add(25.15f, () => DashArrive(mirror, state.MirrorPosition));
        world.Events.Add(25.33f, () => usurper?.SetPosition(new Placement(Vector3.Zero, 0f)));
        world.Events.Add(25.33f, FrigidNeedles);
        world.Events.Add(25.46f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(25.6f, () => mirror?.Face(Vector3.Zero));
        world.Events.Add(25.8f, () => mirror?.Cast(ActionId.SinboundHolyCast, animationLock: 3.1f));
        world.Events.Add(27.6f, () => usurper?.Cast(Actions.HeavenlyStrike));
        world.Events.Add(30.65f, () => usurper?.PlayActionTimeline(TimelineId.UsurperHide));
        for (var wave = 0; wave < HolyWaves; wave++)
        {
            var w = wave;
            var showsAt = HolyAt(w) + PuddleShowsAfterHoly;
            world.Events.Add(HolyAt(w) - FruHolyPuddles.SnapshotBeforeHit, SnapshotHolyTargets);
            world.Events.Add(HolyAt(w), () => SinboundHoly(w));
            world.Events.Add(showsAt, () => puddles.Show(world, w));
            world.Events.Add(PuddleFadesAt(w), () => puddles.Fade(w));
            world.Events.Add(showsAt + PuddleLastsAfterShowing, () => puddles.Remove(w));
        }
        world.Events.Add(32.75f, () => usurper?.SetPosition(new Placement(state.GazePosition, 0f).Face(Vector3.Zero)));
        world.Events.Add(32.87f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(34.9f, TurnUsurperToLightAndMirrorToIce);
        world.Events.Add(39.93f, PlaceArmorHelpers);
        world.Events.Add(40.05f, ShiningAndFrostArmor);
        world.Events.Add(IceStart, () => mirror?.SetModeAttributeFlags(0xF0));
        world.Events.Add(IceStart, FreezeTheFloor);
        world.Events.Add(42.9f, () => mirror?.SetModeAttributeFlags(0xF3));
        world.Events.Add(43f, () => usurper?.PlayActionTimeline(TimelineId.UsurperHide));
        world.Events.Add(43f, () => mirror?.VoiceLine(state.Stillness ? VoiceLineId.TwinStillness : VoiceLineId.TwinSilence));
        world.Events.Add(43f, () => mirror?.Cast(state.Stillness ? Actions.TwinStillnessFirst : Actions.TwinSilenceFirst));
        world.Events.Add(47.28f, () => mirror?.SetModelState(5));
        world.Events.Add(48.57f, () => mirror?.SetModeAttributeFlags(0xF0));
        world.Events.Add(48.65f, () => mirror?.Cast(state.Stillness ? Actions.TwinStillnessSecond : Actions.TwinSilenceSecond));
        world.Events.Add(49.54f, () => mirror?.SetModelState(0));
        world.Events.Add(51.62f, () => mirror?.SetVisibleInEnemyList(false));
        world.Events.Add(51.74f, () => mirror?.PlayActionTimeline(TimelineId.MirrorHide));
        world.Events.Add(52.17f, () => world.Map.AddEffect(MapEffect.ThawFloor, MapEffect.P2Platform));
        world.Events.Add(52.17f, () => usurper?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(52.17f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(52.17f, () => usurper?.SetTargetable(true));
    }

    private void SpawnActors()
    {
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP2,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Targetable: true,
            Placement: new Placement(Vector3.Zero, 0f),
            InitialModeAttributeFlags: 0xF0,
            LayoutId: LayoutId.UsurperP2));
        mirror = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.FrozenMirror,
            NameId: BNpcNameId.FrozenMirror,
            Level: Level,
            EnemyList: EnemyListMode.Manual,
            Visibility: SpawnVisibility.HiddenUntilShown,
            Placement: new Placement(Vector3.Zero, 0f),
            InitialModeAttributeFlags: 0xFC,
            LayoutId: LayoutId.FrozenMirror));
        for (var i = 0; i < icicleHelpers.Length; i++) icicleHelpers[i] = SpawnHelper(BNpcNameId.Usurper, new Placement(Vector3.Zero, 0f));
        for (var i = 0; i < needleHelpers.Length; i++) needleHelpers[i] = SpawnHelper(BNpcNameId.Usurper, new Placement(Vector3.Zero, 0f));
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper(BNpcNameId.Usurper, new Placement(Vector3.Zero, 0f));
        var reflectionSpots = state.Icicles(0).ToList();
        for (var i = 0; i < reflectionHelpers.Length; i++)
            reflectionHelpers[i] = SpawnHelper(BNpcNameId.FrozenMirror, new Placement(reflectionSpots[i], 0f).Face(Vector3.Zero));
    }

    private SimEnemy? SpawnHelper(uint nameId, Placement placement) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: placement));

    private void ShowMirror()
    {
        mirror?.PlayActionTimeline(TimelineId.MirrorShow);
        mirror?.SetVisibleInEnemyList(true);
    }

    private void MarkFrigidStones()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (state.Marked(role) && world.Party.Get(role) is { } member && member.IsAlive())
                member.AttachLockonVfx(LockonId.FrigidStone);
    }

    private void DropIcicles(int wave, int firstHelper)
    {
        var helper = firstHelper;
        foreach (var position in state.Icicles(wave))
        {
            icicleHelpers[helper]?.SetPosition(new Placement(position, 0f).Face(Vector3.Zero));
            icicleHelpers[helper]?.Cast(Actions.IcicleImpact, position);
            helper++;
        }
    }

    // On the four nearest the Usurper's spot at the center.
    private void HouseOfLight()
    {
        var baits = world.Party.ActiveMembers().OrderBy(m => m.Position.LengthSquared()).Take(4).ToList();
        for (var i = 0; i < baits.Count; i++)
        {
            spreadHelpers[i]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(baits[i].Position));
            spreadHelpers[i]?.Cast(Actions.HouseOfLightDiamondDust);
        }
    }

    private void FrigidStones()
    {
        var i = 0;
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (!state.Marked(role) || world.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            stars.Add(target.Position);
            spreadHelpers[i]?.SetPosition(new Placement(target.Position, 0f));
            spreadHelpers[i]?.Cast(Actions.FrigidStone, target.Position);
            i++;
        }
    }

    private static readonly Vector3 UsurperOpeningSpot = new(0, 0, 5);

    // Retail's move to a spot: the actor turns to it and plays the unnamed action aimed there, the
    // server puts it on the spot ~0.67s later still facing the way it went, and it turns after.
    private static void DashStart(SimEnemy? who, Vector3 destination)
    {
        if (who is null) return;
        who.TurnTo(who.Placement().Face(destination).Rotation);
        who.Cast(ActionId.Unnamed, destination, animationLock: 1.1f);
    }

    private static void DashArrive(SimEnemy? who, Vector3 destination)
        => who?.SetPosition(new Placement(destination, who.Rotation));

    // Each stone leaves a star: a circle and an eight-way cross, the cross's diagonals cast by a
    // helper turned 45°.
    private void FrigidNeedles()
    {
        var helper = 0;
        foreach (var star in stars)
        {
            foreach (var (action, rotation) in new[] { (Actions.FrigidNeedleCross, -MathF.PI / 4f), (Actions.FrigidNeedleCross, 0f), (Actions.FrigidNeedleCircle, 0f) })
            {
                needleHelpers[helper]?.SetPosition(new Placement(star, rotation));
                needleHelpers[helper]?.Cast(action);
                helper++;
            }
        }
    }

    private static readonly PartyRole[] Healers = [PartyRole.RegenHealer, PartyRole.ShieldHealer];

    private void SnapshotHolyTargets()
    {
        foreach (var role in Healers)
            if (world.Party.Get(role) is { } healer && healer.IsAlive()) puddles.Snapshot(healer);
    }

    private void SinboundHoly(int wave)
    {
        for (var i = 0; i < Healers.Length; i++)
        {
            if (world.Party.Get(Healers[i]) is not { } healer || !healer.IsAlive()) continue;
            reflectionHelpers[i]?.Cast(Actions.SinboundHoly, healer);
            puddles.Drop(wave, healer, HolyAt(wave) + PuddleShowsAfterHoly + PuddleArmsAfterShowing);
        }
    }

    // The Usurper turns to light at the gaze spot, the Mirror to ice for the Twin cleaves.
    private void TurnUsurperToLightAndMirrorToIce()
    {
        mirror?.ActorControl.Transform(MirrorIceBNpcState);
        mirror?.AddStatusParam(TransfigurationStatus, MirrorIceTransformation);
        if (usurper is { } boss) world.Map.DirectorUpdate(0x80000027U, 0x4U, 0x2U, UsurperBattleTalk, boss.EntityId);
        usurper?.SetModeAttributeFlags(0xFC);
        usurper?.ActorControl.Transform(UsurperLightBNpcState);
        usurper?.AddStatusParam(TransfigurationStatus, UsurperLightTransformation);
    }

    private void PlaceArmorHelpers()
    {
        spreadHelpers[0]?.SetPosition(new Placement(state.GazePosition, 0f).Face(Vector3.Zero));
        reflectionHelpers[0]?.SetPosition(new Placement(state.MirrorPosition, 0f).Face(Vector3.Zero));
    }

    private void ShiningAndFrostArmor()
    {
        spreadHelpers[0]?.Cast(Actions.ShiningArmor);
        reflectionHelpers[0]?.Cast(ActionId.FrostArmor, animationLock: 2.1f);
    }

    // The player slides on their own, by Thin Ice's param; the strat slides the bots.
    private void FreezeTheFloor()
    {
        world.Map.AddEffect(MapEffect.FreezeFloor, MapEffect.P2Platform);
        foreach (var member in world.Party.ActiveMembers())
            member.AddStatusParam(StatusId.ThinIce, ThinIceSlideParam, IceEnd - IceStart);
    }

    public void Tick(float delta, float elapsed)
    {
        puddles.KillWhoeverStandsIn(world.Party, elapsed, ActionId.SinboundHoly, "stepped in a Sinbound Holy puddle");
    }
}
