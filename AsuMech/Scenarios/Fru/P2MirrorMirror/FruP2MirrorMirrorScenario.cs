using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P2MirrorMirror;

// t=0 is Diamond Dust's end, as the Usurper reappears targetable in the centre.
public sealed class FruP2MirrorMirrorScenario : IScenario
{
    public string Name => "Mirror, Mirror";
    public IPhase Phase => FruZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP2MirrorMirrorAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP2MirrorMirrorSettingsWindow settingsWindow = new();

    private const int ProteanBaits = 4;
    private const float MusicAtStart = 76.2f;
    // Hallowed Ray follows Diamond Dust's end straight on; carrying on, this much of the lead-in is skipped.
    private const float HandoffLag = 1.5f;

    private SimWorld world = null!;
    private FruP2MirrorMirrorState state = null!;
    private SimEnemy? usurper;
    private float skipped;

    // A skipped lead-in skips into the track too.
    public float BgmSecondsAtStart => MusicAtStart + skipped;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP2MirrorMirrorState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP2MirrorMirrorState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        if (!world.Continued)
        {
            world.Events.Add(0.6f, () => usurper?.SetModelState(4));
            world.Events.Add(0.6f, () => usurper?.AddStatusParam(TransfigurationStatus, UsurperLightTransformation));
        }
        world.Events.Add(2.23f, MarkRayTarget);
        world.Events.Add(2.23f, () => usurper?.Cast(ActionId.HallowedRayCast, animationLock: 1.1f));
        world.Events.Add(7.75f, HallowedRay);
        world.Events.Add(13.85f, () => usurper?.Cast(ActionId.MirrorMirror, animationLock: 2.1f));
        world.Events.Add(18.83f, ShowMirrors);
        world.Events.Add(19.3f, () => usurper?.MoveTo(state.BossSpot, finalRotation: new Placement(state.BossSpot, 0f).Face(Vector3.Zero).Rotation));
        world.Events.Add(25.02f, () => usurper?.VoiceLine(VoiceLineId.ScytheKick));
        world.Events.Add(25.02f, () => usurper?.Cast(Actions.ScytheKick));
        world.Events.Add(31f, RedMirrorsKick);
        world.Events.Add(31.05f, () => usurper?.CastFromHelper(Actions.ReflectedScytheKick, at: new Placement(state.Blue, 0f)));
        world.Events.Add(31.37f, () => HouseOfLight(state.Blue));
        world.Events.Add(31.45f, () => HouseOfLight(state.BossSpot));
        world.Events.Add(33.07f, () => world.Map.AddEffect(MapEffect.MirrorHide, MirrorSlot(state.BlueOctant)));
        world.Events.Add(41.42f, () => { foreach (var red in state.RedOctants) HouseOfLight(Mirror(red)); });
        world.Events.Add(42.05f, () => usurper?.Cast(state.Pairs ? ActionId.BanishPairsCast : ActionId.BanishSpreadCast, animationLock: 3.1f));
        world.Events.Add(42.05f, () => usurper?.VoiceLine(VoiceLineId.Banish));
        world.Events.Add(43.1f, () => { foreach (var red in state.RedOctants) world.Map.AddEffect(MapEffect.MirrorHide, MirrorSlot(red)); });
        world.Events.Add(46.84f, Banish);
        skipped = world.Continued ? HandoffLag : 0f;
        world.Events.Advance(skipped);
    }

    private void SpawnActors()
    {
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP2,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Targetable: true,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            ModelCharaId: ModelCharaId.UsurperLight,
            InitialModeAttributeFlags: 0xFC,
            LayoutId: LayoutId.UsurperP2,
            // Carrying on, she is already in the light form Diamond Dust left her in.
            Statuses: world.Continued ? [new SpawnStatus(TransfigurationStatus, UsurperLightTransformation)] : null,
            ModelState: world.Continued ? (byte)4 : (byte)0));
    }

    private static Vector3 Mirror(int octant) => FruP2MirrorMirrorState.Point(octant, FruP2MirrorMirrorState.MirrorRadius);

    private static byte MirrorSlot(int octant) => (byte)(MapEffect.FirstMirror + octant);

    private void MarkRayTarget()
    {
        if (world.Party.Get(state.RayTarget) is { } target && target.IsAlive())
            target.AttachLockonVfx(LockonId.HallowedRay);
    }

    private void HallowedRay()
    {
        if (usurper is null || world.Party.Get(state.RayTarget) is not { } target || !target.IsAlive()) return;
        usurper.CastFromHelper(Actions.HallowedRay, at: new Placement(usurper.Position, 0f).Face(target.Position));
    }

    private void ShowMirrors()
    {
        world.Map.AddEffect(MapEffect.MirrorBlue, MirrorSlot(state.BlueOctant));
        foreach (var red in state.RedOctants) world.Map.AddEffect(MapEffect.MirrorRed, MirrorSlot(red));
    }

    private void RedMirrorsKick()
    {
        foreach (var red in state.RedOctants)
        {
            world.Map.AddEffect(MapEffect.MirrorRedKicking, MirrorSlot(red));
            usurper?.CastFromHelper(Actions.ReflectedScytheKickCast, at: new Placement(Mirror(red), 0f).Face(Vector3.Zero));
        }
    }

    // On the four nearest the source.
    private void HouseOfLight(Vector3 source)
    {
        var baits = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, source)).Take(ProteanBaits).ToList();
        foreach (var bait in baits)
            usurper?.CastFromHelper(Actions.HouseOfLightDiamondDust, at: new Placement(source, 0f).Face(bait.Position));
    }

    // Pairs go on all supports or all DPS; spreads on everyone.
    private void Banish()
    {
        if (usurper is null) return;
        var targets = (state.Pairs
            ? world.Party.ActiveMembers().Where(m => m is ISimPartyMember p && p.Role.IsDps() != state.SupportPairs)
            : world.Party.ActiveMembers()).ToList();
        foreach (var target in targets)
            usurper.CastFromHelper(state.Pairs ? Actions.BanishPairs : Actions.BanishSpread, target, at: new Placement(usurper.Position, 0f));
    }
}
