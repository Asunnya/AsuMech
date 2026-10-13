using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Core.SimObjects;

// Placement.Position is scenario-local (offset from SimWorld.ScenarioOrigin), same
// coordinate space as the rest of the SimXxx API: +X = east, +Z = south.
// Placement.Rotation is absolute radians: 0 = south, π/2 = east, π = north, -π/2 = west.
// ModelCharaId (non-zero) overrides the BNpcBase visual, e.g. a no-shield variant.

// How the engine shows the actor on spawn; the value is the SpawnNpcPacket DisplayFlags every
// captured server spawn of that kind carries.
// Visible          — drawn at spawn.
// HiddenUntilShown — bit 0x20000; hidden until a warp_end/show timeline reveals it (bosses,
//                    clones, arm units).
// HiddenUntilPopIn — bit 0x20; hidden until ActorControl.PopIn (0x24) ~0.8s
//                    later (summoned adds, pets). UNVERIFIED on our actors.
// InvisibleHelper  — the meshless helper that casts a fight's AoEs (ModelChara 480); never
//                    drawn, and spawned with no MP.
public enum SpawnVisibility : uint
{
    Visible = 0x4000B,
    HiddenUntilShown = 0x6000B,
    HiddenUntilPopIn = 0x4002B,
    InvisibleHelper = 0x40008,
}

// Whether a SimEnemy shows in the _EnemyList HUD (sent in the Hater packet by EnemyList).
// Always          — listed while alive.
// OnlyWhenVisible — follows the engine's DrawObject.IsVisible; for adds that warp
//                   in/out. Transforming bosses use Always.
// Never           — never listed (AOE-source dummies, tether endpoints).
// Manual          — scenario drives it via SetInEnemyList(bool); default false.
public enum EnemyListMode
{
    Always,
    OnlyWhenVisible,
    Never,
    Manual,
}

// How a VFX-only timeline is pinned in place after its action fired (see
// SimEnemy.HoldTimelineBase): None also carries the id being released.
public enum TimelineHoldKind
{
    None,
    Base,
}

public record struct EnemySpawnConfig(
    uint BNpcBaseId,
    uint NameId = 0,
    byte Level = 0,
    bool Targetable = false,
    EnemyListMode EnemyList = EnemyListMode.Always,
    SpawnVisibility Visibility = SpawnVisibility.Visible,
    Placement Placement = default,
    uint ModelCharaId = 0,
    byte? InitialModeAttributeFlags = null, // null = engine default; set when the idle sub-mesh variant differs (Omega-M = 0x10)
    // A captured real NpcSpawn packet body (see UmadRealPackets) used instead of the one built
    // from this config; of the fields above only NameId, Targetable, EnemyList and Placement
    // still apply.
    byte[]? NpcSpawnTemplate = null,
    // Request the draw object ourselves. The engine builds none for a meshless actor, and a
    // caster without a draw object has its action timeline cleared within frames.
    bool PacketSpawnEnableDraw = false);

public sealed partial class SimEnemy : SimNpc
{
    // The cast packets and the animation lock live in SimCast; EnemyActionHandler decides when
    // they go out and what the action does.
    private readonly SimCast cast;
    private readonly EnemyActionHandler actions;
    private readonly EventScheduler events;

    // The server's own ceiling: bosses turn at most ~145° per ~0.3s movement tick.
    private const float BossTurnSpeed = MathF.PI * 8f / 3f;

    protected override float? TurnSpeed => BossTurnSpeed;

    // Peer-only smoothing for ApplyNetworkPosition, same model as SimNetworkPuppet: the
    // catch-up speed is a floor once the real snapshot interval is known, anything beyond
    // NetworkSnapThreshold (a scripted teleport, a lag spike) snaps, extrapolation only feeds
    // the visual glide, and rotation turns at BossTurnSpeed like the host's.
    private const float NetworkCatchUpSpeed = 20f;
    private const float NetworkSnapThreshold = 15f;

    private const float NetworkIntervalSmoothingFactor = 0.3f;
    private const float MinNetworkPacingWindowSeconds = 0.05f;
    private float timeSinceLastNetworkUpdate;
    private float estimatedNetworkUpdateInterval = 0.05f;

    private const float MaxNetworkExtrapolationSeconds = 1f;
    private Vector3 networkVelocity;

    private Vector3? networkTargetPosition;
    private float networkTargetRotation;

    public void ApplyNetworkPosition(Vector3 position, float rotation)
    {
        if (networkTargetPosition is { } previous)
        {
            estimatedNetworkUpdateInterval += (timeSinceLastNetworkUpdate - estimatedNetworkUpdateInterval) * NetworkIntervalSmoothingFactor;
            networkVelocity = timeSinceLastNetworkUpdate > MinNetworkPacingWindowSeconds
                ? (position - previous) / timeSinceLastNetworkUpdate
                : Vector3.Zero;
        }
        networkTargetPosition = position;
        networkTargetRotation = rotation;
        timeSinceLastNetworkUpdate = 0f;
    }

    private void TickNetworkPosition(float deltaSeconds)
    {
        if (networkTargetPosition is not { } rawTarget) return;
        timeSinceLastNetworkUpdate += deltaSeconds;

        var target = rawTarget + networkVelocity * MathF.Min(timeSinceLastNetworkUpdate, MaxNetworkExtrapolationSeconds);
        var basePos = Position;
        var delta = target - basePos;
        var dist = delta.Length();
        var remainingWindow = MathF.Max(estimatedNetworkUpdateInterval - timeSinceLastNetworkUpdate, MinNetworkPacingWindowSeconds);
        var step = MathF.Max(dist / remainingWindow, NetworkCatchUpSpeed) * deltaSeconds;
        if (dist > NetworkSnapThreshold)
        {
            // Logged: position otherwise rides silently in every snapshot.
            DiagnosticLog.Info($"[SimEnemy.TickNetworkPosition] {DisplayName} (BNpcBase {BNpcBaseId}) snapped {dist:F1}y (> {NetworkSnapThreshold}y threshold): {basePos} -> {target}.");
            SetPosition(new Placement(target, networkTargetRotation));
        }
        else
        {
            // The model is aimed where the host's enemy will be a glide ahead (see
            // SimCharacter.ModelGlideSeconds).
            var glide = target + networkVelocity * ModelGlideSeconds;
            StepTo(dist <= step ? target : basePos + delta / dist * step, glide, ActorMoveFlags.None,
                deltaSeconds > 0f ? MathF.Min(dist, step) / deltaSeconds : 0f);
            TurnTo(networkTargetRotation);
        }
    }

    // SpawnConfig.Targetable is only the spawn-time default.
    private bool desiredTargetable;
    public bool Targetable => desiredTargetable;

    // Diagnostic: EnableDraw/IsVisible don't say whether each equipment/body model slot
    // finished streaming; logged shortly after spawn and again a few seconds later.
    private int slotCheckFrames;
    private bool slotCheckDone;

    public uint BNpcBaseId { get; }

    // Lets a peer reconstruct the same doppel via world.SpawnEnemy.
    public EnemySpawnConfig SpawnConfig { get; internal set; }


    // Live via GameObject::GetName() so engine-driven renames propagate (the Name[] buffer is
    // never refreshed for doppels). Falls back to the spawn-time name mid-despawn.
    public string DisplayName
    {
        get
        {
            if (Proxy is not { Exists: true } chara) return field;
            var name = chara.Name;
            return string.IsNullOrEmpty(name) ? field : name;
        }
    }

    public EnemyListMode EnemyListMode { get; }
    private bool manualInEnemyList;

    // OnlyWhenVisible reads the live DrawObject.IsVisible flag, so any draw-lifecycle
    // toggle is reflected without extra plumbing; Manual lets the scenario drive it.
    public bool InEnemyList => !Dead && EnemyListMode switch
    {
        EnemyListMode.Always          => true,
        EnemyListMode.Never           => false,
        EnemyListMode.Manual          => manualInEnemyList,
        EnemyListMode.OnlyWhenVisible => IsEngineVisible(),
        _ => false,
    };

    public bool IsCasting => cast.IsCasting;
    public uint CastActionId => cast.ActionId;
    public float CastProgress => cast.Progress;
    public Vector3? CastTargetLocation => cast.TargetLocation;
    public float CastTotalSeconds => cast.Total;

    // The cast packets as sent, for multiplayer to sample on the host and replay on a peer.
    internal SimCast Casting => cast;

    // The engine's own NpcSpawn handler creates the actor and enables its draw, as for a real
    // server spawn.
    private SimEnemy(IBattleCharaProxy proxy, uint bNpcBaseId, string displayName, EnemyListMode enemyListMode, SimWorld world) : base(proxy, world.Coordinates, pendingDraw: false)
    {
        BNpcBaseId = bNpcBaseId;
        DisplayName = displayName;
        EnemyListMode = enemyListMode;
        cast = new SimCast(this, world.Coordinates);
        actions = new EnemyActionHandler(this, cast, world);
        events = world.Events;
    }

    // Spawns the BattleChara through the engine's own NpcSpawn handler (see
    // IBattleCharas.SpawnBattleNpcFromPacket) and wraps it; the actor arrives a few frames later.
    // Caller is responsible for registering the result in the world's children list (so
    // reset/teardown covers it). Null on missing LocalPlayer, BNpcBase miss, or no free slot.
    internal static SimEnemy? Spawn(EnemySpawnConfig config, SimWorld world)
    {
        if (!Natives.BattleCharas.LocalPlayer.Exists) return null;
        if (config.NpcSpawnTemplate is null && config.Visibility == SpawnVisibility.InvisibleHelper) config = config with { PacketSpawnEnableDraw = true };
        return SpawnFromPacket(config, world);
    }

    // Null when the handler refused the packet.
    private static SimEnemy? SpawnFromPacket(EnemySpawnConfig config, SimWorld world)
    {
        if (Natives.BattleCharas.SpawnBattleNpcFromPacket(config, world.Coordinates.ToGlobal(config.Placement), out var entityId) is not { } chara)
            return null;
        var displayName = Natives.Data.BNpcName(config.NameId) ?? $"BNpc {config.BNpcBaseId:X}";
        var enemy = new SimEnemy(chara, config.BNpcBaseId, displayName, config.EnemyList, world)
        {
            SpawnConfig = config,
        };
        enemy.BeginPacketSpawn(entityId, config.Placement);
        return enemy;
    }

    protected override void OnPacketSpawned(IBattleCharaProxy chara)
    {
        var targetableBefore = chara.TargetableStatus;
        SetTargetable(SpawnConfig.Targetable);
        // Retail engages every enemy, helpers included, ~0.8s into the pull and adds on arrival;
        // the sim has no pre-pull, so every enemy is engaged from its spawn.
        ActorControl.SetWeaponDrawn(true);
        ActorControl.SetInCombat(true);
        if (SpawnConfig.PacketSpawnEnableDraw) RequestDraw();
        DiagnosticLog.Info($"[SimEnemy.SpawnFromPacket] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) created by the engine after {PacketSpawnFrames} frames: {DescribeDrawState()} "
            + $"Targetable=0x{targetableBefore:X}->0x{chara.TargetableStatus:X} name=\"{chara.Name}\" pos {chara.Position}.");
    }

    public override void Despawn()
    {
        Movement.Follow(null);
        cast.Despawn();
        base.Despawn();
    }

    /// <summary>
    /// Sets the targetable status of this <see cref="SimEnemy"/>, which will reflect in their Nameplate and in the Enemy List (if visible there).
    /// </summary>
    /// <param name="targetable">
    /// If <see langword="true"/>, then the Nameplate will be visible, and able to target them using the Enemy List.
    /// If <see langword="false"/>, then the Nameplate will not be visible, and not able to target them using the Enemy List.
    /// </param>
    public override void SetTargetable(bool targetable)
    {
        desiredTargetable = targetable;
        base.SetTargetable(targetable);
    }

    /// <summary>
    /// Only executed when <see cref="EnemyListMode"/> is <see cref="EnemyListMode.Manual"/>
    /// </summary>
    /// <param name="inEnemyList">Will make the Enemy appear or not in the Enemy List (Enmity List)</param>
    public void SetVisibleInEnemyList(bool inEnemyList)
    {
        if (EnemyListMode != EnemyListMode.Manual)
        {
            Plugin.Log.Warning($"SetInEnemyList({inEnemyList}) ignored: SimEnemy {DisplayName} has mode {EnemyListMode}; declare EnemyListMode.Manual in EnemySpawnConfig to use explicit toggles.");
            return;
        }
        manualInEnemyList = inEnemyList;
    }

    /// <summary>
    /// Sets the target of this <see cref="SimEnemy"/>.
    /// </summary>
    /// <remarks>For now, this is purely visual and does not contain any logic relating to auto-attacks or similar.</remarks>
    /// <param name="target">The <see cref="SimCharacter.GameObjectId"/> will be retrieved and used as the TargetId. If <see langword="null"/>, then the target is cleared.</param>
    /// <param name="follow">If <paramref name="target"/> is valid, this will determine if the <see cref="SimEnemy"/> should now follow <paramref name="target"/> or not.</param>
    /// <param name="speed">If <paramref name="target"/> is valid and <paramref name="follow"/> is <see langword="true"/>, this will be the speed that the <see cref="SimEnemy"/> will follow the <paramref name="target"/></param>
    public void SetTarget(SimCharacter? target, bool follow = true, float speed = 6f)
    {
        if (target == null)
        {
            ActorControl.ClearTarget();
        }
        else
        {
            ActorControl.SetTarget(target.GameObjectId);

            if (follow)
            {
                Follow(target);
            }
        }
    }

    // The engine-level state below is driven by explicit scenario calls and sampled for peers.
    // Tracked here rather than read back from native, which the run animation and the cast
    // pipeline overwrite every frame; each is edge-triggered on its own seq.
    public (byte Mode, byte Param)? LastMode { get; private set; }
    public int ModeSeq { get; private set; }
    public TimelineHoldKind TimelineHoldState { get; private set; }
    public ushort TimelineHoldId { get; private set; }
    public int TimelineHoldSeq { get; private set; }
    public ushort DirectTimelineId { get; private set; }
    public int DirectTimelineSeq { get; private set; }
    public int ForceLoadTimelineSeq { get; private set; }

    // Sticky, so an ordinary enemy carries no engine block while a carrier that has been driven
    // keeps reporting: dropping the block after the last call would strand a peer on it.
    public bool HasEngineState { get; private set; }

    // AnimLock (8) is the mode the client holds an actor in while an action animation plays.
    public void SetMode(CharacterModes mode, byte param = 0)
    {
        LastMode = ((byte)mode, param);
        ModeSeq++;
        HasEngineState = true;
        ActorControl.SetMode(mode, param);
    }

    private void TickPacketSpawnCheckpoints()
    {
        if (PacketSpawnPending || PacketSpawnFailed) return;
        if (PacketSpawnFrames is 5 or 30 or 90 or 210)
        {
            var targetable = Proxy?.TargetableStatus ?? 0;
            DiagnosticLog.Info($"[SimEnemy.PacketSpawn] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) +{PacketSpawnFrames} frames: {DescribeDrawState()} Targetable=0x{targetable:X} -- {DescribeActionTimeline()}");
        }
    }

    private float timelineWatchRemaining;
    private int timelineWatchFrames;
    private string? timelineWatchLast;

    // Per-frame trace of the action-timeline state for `seconds`, logged on change plus a
    // heartbeat. The first sample is taken synchronously.
    public void StartTimelineWatch(float seconds)
    {
        timelineWatchRemaining = seconds;
        timelineWatchFrames = 0;
        timelineWatchLast = null;
        TickTimelineWatch(0f);
    }

    private void TickTimelineWatch(float deltaSeconds)
    {
        if (timelineWatchRemaining <= 0f) return;
        timelineWatchRemaining -= deltaSeconds;
        // Slot ids decide "changed"; playback positions advance every frame.
        var ids = Proxy?.DescribeTimelineSlotIds() ?? "-";
        var changed = ids != timelineWatchLast;
        if (changed || timelineWatchFrames < 15 || timelineWatchFrames % 15 == 0)
        {
            var state = DescribeActionTimeline();
            DiagnosticLog.Info($"[SimEnemy.TimelineWatch] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) +{timelineWatchFrames}f: {state}{(changed ? "" : " (slots unchanged)")}");
        }
        timelineWatchLast = ids;
        timelineWatchFrames++;
        if (timelineWatchRemaining <= 0f)
            DiagnosticLog.Info($"[SimEnemy.TimelineWatch] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) watch ended after {timelineWatchFrames} frames.");
    }

    internal string DescribeActionTimeline() => Proxy?.DescribeActionTimeline() ?? "no BattleChara";

    // Debug: the engine's own resource loader for the base slot's scheduler timeline.
    public ulong ForceLoadBaseTimeline()
    {
        ForceLoadTimelineSeq++;
        HasEngineState = true;
        if (Proxy is not { Exists: true } chara) return 0;
        var result = chara.LoadBaseTimelineResources();
        DiagnosticLog.Info($"[SimEnemy] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) LoadTimelineResources on slot 0 -> {result}: {DescribeActionTimeline()}");
        return result;
    }

    // Debug: the sequencer's own entry point, with no action effect around it.
    public void PlayTimelineDirect(ushort timelineId)
    {
        DirectTimelineId = timelineId;
        DirectTimelineSeq++;
        HasEngineState = true;
        Proxy?.PlayTimelineDirect(timelineId);
    }

    // Debug hold for a timeline whose VFX dies the moment its slot clears: sets
    // TimelineContainer.BaseOverride. Release clears it.
    public void HoldTimelineBase(ushort timelineId)
    {
        NoteTimelineHold(TimelineHoldKind.Base, timelineId);
        Proxy?.SetBaseOverride(timelineId);
    }

    private void NoteTimelineHold(TimelineHoldKind kind, ushort timelineId)
    {
        TimelineHoldState = kind;
        TimelineHoldId = timelineId;
        TimelineHoldSeq++;
        HasEngineState = true;
    }

    public void ReleaseTimelineHold(ushort timelineId)
    {
        NoteTimelineHold(TimelineHoldKind.None, timelineId);
        if (Proxy is not { Exists: true } chara) return;
        chara.SetBaseOverride(0);
        if (chara.GetSlotTimeline(0) == timelineId)
            chara.SetSlotTimeline(0, 0);
        DiagnosticLog.Info($"[SimEnemy] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) timeline hold released: {DescribeActionTimeline()}");
    }

    internal string DescribeDrawState() => Proxy?.DescribeDrawState() ?? "no BattleChara";

    // Edge-tracked like AnimationTimelineId, and sampled for peers.
    public (byte Slot, byte Value)? AnimationState { get; private set; }
    public int AnimationStateSeq { get; private set; }

    public void SetAnimationState(byte slot, byte value)
    {
        AnimationState = (slot, value);
        AnimationStateSeq++;
        ActorControl.SetAnimationState(slot, value);
    }

    // Authoritative draw state (DrawObject.Flags bits 0 and 3, set by Enable/DisableDraw).
    // False during the async model-load window where DrawObject is still null.
    private bool IsEngineVisible() => Proxy?.IsDrawObjectVisible ?? false;

    // A purely visual cast: just the action's bar and animation, hitting no one. For an action that
    // has effects or needs more configuration, define an EnemyAction and use the overload taking it.
    public EnemyActionCast Cast(uint actionId, CastTarget target = default, float animationLock = 0.6f, byte animationVariation = 0) =>
        actions.Start(new EnemyAction(actionId) { Cast = new() { AnimationLock = animationLock } }, target, animationVariation);

    // An action with effects (area, damage, statuses, ...) or configuration beyond the sheet's; a
    // purely visual one needs no EnemyAction, use Cast(actionId).
    public EnemyActionCast Cast(EnemyAction action, CastTarget target = default, byte animationVariation = 0) =>
        actions.Start(action, target, animationVariation);

    // Interrupts the cast still on its bar: neither its effect nor its mechanics go out.
    public void CancelCast(CastCancelReason reason) => actions.CancelCast(reason);

    public override bool AnimationLock => cast.IsBusy;

    public const float DefaultCorpseFadeDelay = 8f;
    private const float CorpseFadeDuration = 1.7f;

    // Killed by damage. corpseFadeDelay counts from the death animation; a boss leaving at a
    // phase transition fades sooner (FRU Fatebreaker: 3s).
    public void Defeat(float corpseFadeDelay = DefaultCorpseFadeDelay)
    {
        if (Dead) return;
        actions.CancelCast(CastCancelReason.Interrupted);
        GoDown(byEnvironment: false);
        ScheduleCorpse(0f, corpseFadeDelay);
    }

    // Only for DefeatCasterEffect. The enemy dies by its own action (a self-destruct): it goes
    // down as the action lands and plays its death animation once the action's lock ends.
    internal void DefeatByOwnAction(EnemyAction action)
    {
        if (Dead) return;
        actions.CancelCast(CastCancelReason.Cancelled);
        MarkDead();
        ActorControl.SetMode(CharacterModes.Dead, 2);
        var animationDelay = action.Cast.AnimationLock;
        events.Add(animationDelay, PlayDeath);
        ScheduleCorpse(animationDelay, DefaultCorpseFadeDelay);
    }

    private void ScheduleCorpse(float animationDelay, float corpseFadeDelay)
    {
        events.Add(animationDelay + corpseFadeDelay, ActorControl.FadeCorpse);
        events.Add(animationDelay + corpseFadeDelay + CorpseFadeDuration, Despawn);
    }

    private const float FadeOutDuration = 1.3f;

    // A spent add or a boss leaving at a phase transition: gone without dying.
    public void FadeOut()
    {
        ActorControl.FadeOut();
        events.Add(FadeOutDuration, Despawn);
    }

    public override void Tick(float deltaSeconds)
    {
        base.Tick(deltaSeconds);
        TickNetworkPosition(deltaSeconds);
        TickForkVisibility();
        cast.Tick(deltaSeconds);
        TickTimelineWatch(deltaSeconds);
        TickPacketSpawnCheckpoints();

        if (!slotCheckDone && SpawnConfig.Visibility == SpawnVisibility.Visible)
        {
            slotCheckFrames++;
            if (slotCheckFrames == 1) LogModelSlotState("+1 frame");
            else if (slotCheckFrames == 5) LogModelSlotState("+5 frames");
            else if (slotCheckFrames == 210)
            {
                LogModelSlotState("+210 frames (~3.5s)");
                slotCheckDone = true;
            }
        }
    }

    private void LogModelSlotState(string label)
    {
        if (Proxy is not { Exists: true } chara) return;
        DiagnosticLog.Info($"[SimEnemy.LogModelSlotState] {DisplayName} (goid 0x{GameObjectId.ObjectId:X}) {label}: {chara.DescribeModelSlots()}");
    }

    public CharacterFind<T> Find<T>(List<T> targets) where T : IPositioned
    {
        return new CharacterFind<T>(targets);
    }
}
