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

    private const float PuddleRadius = 6f;
    // Time to step off a Sinbound Holy before its puddle hurts.
    private const float PuddleGrace = 1.25f;

    private SimWorld world = null!;
    private FruP2DiamondDustState state = null!;
    private SimEnemy? usurper, mirror;
    private readonly SimEnemy?[] icicleHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] needleHelpers = new SimEnemy?[12];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[4];
    private readonly List<Vector3> stars = [];
    private readonly List<(Vector3 Position, float ArmedAt, SimEventObject? Visual)> puddles = [];
    private readonly Dictionary<SimCharacter, Vector3> previousPositions = new();

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP2DiamondDustState(world.Rng, settingsWindow.Overrides);
        stars.Clear();
        puddles.Clear();
        previousPositions.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP2DiamondDustState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnUsurperAndHelpers);
        world.Events.Add(2f, () => usurper?.Cast(Actions.MirrorImage));
        world.Events.Add(5f, () => mirror = SpawnMirror(Vector3.Zero, 0f, 0));
        world.Events.Add(7.1f, () => usurper?.Cast(Actions.DiamondDust));
        world.Events.Add(14.9f, () => mirror?.Cast(state.Axe ? Actions.AxeKick : Actions.ScytheKick));
        world.Events.Add(15f, () => DropIcicles(0, 0));
        world.Events.Add(15.2f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(15.2f, () => usurper?.SetTargetable(false));
        world.Events.Add(15.2f, MarkFrigidStones);
        world.Events.Add(18.9f, () => DropIcicles(1, 2));
        world.Events.Add(21.7f, HouseOfLight);
        world.Events.Add(23f, () => DropIcicles(2, 6));
        world.Events.Add(23.3f, FrigidStones);
        world.Events.Add(24.2f, MoveMirrorOut);
        world.Events.Add(24.2f, () => usurper?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(24.2f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(25.4f, FrigidNeedles);
        world.Events.Add(25.8f, () => mirror?.Cast(Actions.SinboundHolyCast));
        world.Events.Add(27.6f, () => usurper?.Cast(Actions.HeavenlyStrike));
        world.Events.Add(28.4f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpStart));
        for (var wave = 0; wave < HolyWaves; wave++)
        {
            var w = wave;
            world.Events.Add(HolyAt(w), () => SinboundHoly(w));
        }
        world.Events.Add(32.9f, () => usurper?.SetPosition(new Placement(state.GazePosition, 0f).Face(Vector3.Zero)));
        world.Events.Add(32.9f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(IceStart, ShiningAndFrostArmor);
        world.Events.Add(IceStart, FreezeTheFloor);
        world.Events.Add(43.1f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(43.1f, () => mirror?.VoiceLine(state.Stillness ? VoiceLineId.TwinStillness : VoiceLineId.TwinSilence));
        world.Events.Add(43.1f, () => mirror?.Cast(state.Stillness ? Actions.TwinStillnessFirst : Actions.TwinSilenceFirst));
        world.Events.Add(48.7f, () => mirror?.Cast(state.Stillness ? Actions.TwinStillnessSecond : Actions.TwinSilenceSecond));
        world.Events.Add(IceEnd, ThawTheFloor);
        world.Events.Add(52.3f, () => mirror?.FadeOut());
        world.Events.Add(52.3f, () => usurper?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(52.3f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(52.3f, () => usurper?.SetTargetable(true));
    }

    private void SpawnUsurperAndHelpers()
    {
        usurper = SpawnUsurper(new Vector3(0, 0, 5), MathF.PI, 0);
        for (var i = 0; i < icicleHelpers.Length; i++) icicleHelpers[i] = SpawnHelper();
        for (var i = 0; i < needleHelpers.Length; i++) needleHelpers[i] = SpawnHelper();
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnUsurper(Vector3 position, float rotation, uint modelCharaId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.UsurperP2,
        NameId: BNpcNameId.Usurper,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.OnlyWhenVisible,
        Placement: new Placement(position, rotation),
        ModelCharaId: modelCharaId));

    private SimEnemy? SpawnMirror(Vector3 position, float rotation, uint modelCharaId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.FrozenMirror,
        NameId: BNpcNameId.FrozenMirror,
        Level: Level,
        EnemyList: EnemyListMode.OnlyWhenVisible,
        Placement: new Placement(position, rotation),
        ModelCharaId: modelCharaId));

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Usurper,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

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
            icicleHelpers[helper]?.SetPosition(position);
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
            spreadHelpers[i]?.SetPosition(Vector3.Zero);
            spreadHelpers[i]?.Cast(Actions.HouseOfLightDiamondDust, baits[i]);
        }
    }

    private void FrigidStones()
    {
        var i = 0;
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (!state.Marked(role) || world.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            stars.Add(target.Position);
            spreadHelpers[i]?.SetPosition(target.Position);
            spreadHelpers[i]?.Cast(Actions.FrigidStone, target);
            i++;
        }
    }

    private void MoveMirrorOut()
        => mirror?.SetPosition(new Placement(state.MirrorPosition, 0f).Face(Vector3.Zero));

    // Each stone leaves a star: a circle and an eight-way cross.
    private void FrigidNeedles()
    {
        var helper = 0;
        foreach (var star in stars)
        {
            foreach (var action in new[] { Actions.FrigidNeedleCircle, Actions.FrigidNeedleCross, Actions.FrigidNeedleCrossDiagonal })
            {
                needleHelpers[helper]?.SetPosition(new Placement(star, 0f));
                needleHelpers[helper]?.Cast(action);
                helper++;
            }
        }
    }

    private void SinboundHoly(int wave)
    {
        var at = HolyAt(wave);
        var i = 0;
        foreach (var role in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
        {
            if (world.Party.Get(role) is not { } healer || !healer.IsAlive()) continue;
            spreadHelpers[i]?.SetPosition(healer.Position);
            spreadHelpers[i]?.Cast(Actions.SinboundHoly, healer);
            var visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.HolyPuddle,
                Placement = new Placement(healer.Position, 0f),
                TimelineState = 1,
            });
            puddles.Add((healer.Position, at + PuddleGrace, visual));
            i++;
        }
    }

    // The Usurper and the Mirror trade places and looks: she turns to light at the gaze spot, the
    // Mirror to ice for the Twin cleaves.
    private void ShiningAndFrostArmor()
    {
        usurper?.Cast(Actions.ShiningArmor);
        mirror?.Cast(Actions.FrostArmor);
        usurper?.FadeOut();
        mirror?.FadeOut();
        usurper = SpawnUsurper(state.GazePosition, new Placement(state.GazePosition, 0f).Face(Vector3.Zero).Rotation, ModelCharaId.UsurperLight);
        mirror = SpawnMirror(state.MirrorPosition, new Placement(state.MirrorPosition, 0f).Face(Vector3.Zero).Rotation, ModelCharaId.UsurperIce);
    }

    // The player slides on their own, by Thin Ice's param; the strat pushes the bots.
    // TODO: a bot's push goes out as a knockback slide; retail shows the icefloor slide
    // (ActorControl 0xDF), which isn't one of the forced moves a bot can be given.
    private void FreezeTheFloor()
    {
        world.Map.AddEffect(MapEffect.FreezeFloor, MapEffect.P2Platform);
        foreach (var member in world.Party.ActiveMembers())
            member.AddStatusParam(StatusId.ThinIce, ThinIceSlideParam, IceEnd - IceStart);
    }

    private void ThawTheFloor()
    {
        world.Map.AddEffect(MapEffect.ThawFloor, MapEffect.P2Platform);
        foreach (var puddle in puddles) puddle.Visual?.Despawn();
        puddles.Clear();
    }

    // A puddle kills whoever crosses it, including on the ice between two frames.
    public void Tick(float delta, float elapsed)
    {
        foreach (var member in world.Party.ActiveMembers().ToList())
        {
            var previous = previousPositions.GetValueOrDefault(member, member.Position);
            previousPositions[member] = member.Position;
            if (puddles.Any(p => elapsed >= p.ArmedAt && DistanceToSegment(p.Position, previous, member.Position) <= PuddleRadius))
                member.Die(ActionId.SinboundHoly, "stepped in a Sinbound Holy puddle");
        }
    }

    private static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        var line = to - from;
        var t = line.LengthSquared() > 0 ? Math.Clamp(Vector3.Dot(point - from, line) / line.LengthSquared(), 0, 1) : 0;
        return Vector3.Distance(point, from + t * line);
    }
}
