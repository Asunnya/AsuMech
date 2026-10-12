using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.SimObjects;

// Common base for anything in the simulated world that has a BattleChara behind it
public abstract class SimCharacter(Coordinates coordinates) : ISimObject, IPositioned
{
    private readonly List<SimVfx> vfx = [];
    private readonly List<SimStatus> statusList = [];

    // Null once the character has no native object to stand for (despawned).
    internal abstract IBattleCharaProxy? Proxy { get; }

    private protected abstract Movement Movement { get; }

    protected readonly Coordinates Coordinates = coordinates;

    // Obstacles this character's Movement steers around. Defaults to the shared
    // empty field (no avoidance — straight lines); PartyCreator points party
    // doppels and the player at world.Obstacles so only bots avoid geometry.
    internal ObstacleField Obstacles { get; set; } = ObstacleField.Empty;

    public virtual bool IsActive => Proxy is { Exists: true };

    // True while the character is rooted by an in-progress action (cast bar up or
    // release animation still playing). The Movement subsystem reads this to hold
    // an active follow in place until the animation finishes. Default false; types
    // that simulate casts (SimEnemy) override it.
    public virtual bool AnimationLock => false;

    public GameObjectId GameObjectId => Proxy?.GameObjectId ?? default;
    public float HitboxRadius => Proxy?.HitboxRadius ?? 0f;
    public uint EntityId => Proxy?.EntityId ?? 0u;

    public virtual void Tick(float deltaSeconds)
    {
        modelDrivenSeconds = MathF.Max(0f, modelDrivenSeconds - deltaSeconds);
        if (Proxy is { Exists: true } native)
        {
            var model = Coordinates.ToLocal(native.Position);
            if (ModelDriven || !placed)
            {
                position = model;
                glideTarget = model;
                // A turn queued since the last tick hasn't reached the model yet.
                if (!movePending)
                {
                    modelRotation = native.Rotation;
                    if (turnTarget is null) Rotation = native.Rotation;
                }
                placed = true;
            }
            ModelDivergence = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(model.X, model.Z));
            MaxModelDivergence = MathF.Max(MaxModelDivergence, ModelDivergence);
        }
        statusList.Update(deltaSeconds);
        vfx.Update(deltaSeconds);
        Movement.Tick(deltaSeconds);
        TickTurn(deltaSeconds);
        FlushMove();
    }


    public virtual void Despawn()
    {
        statusList.Despawn();
        vfx.Despawn();
    }

    // -------------------------
    // Location Subsystem
    // -------------------------

    // The sim's own position, in local coordinates: what mechanics and the AI read, like the
    // server's. The model follows it through SetPos/Move packets and trails by however long the
    // client takes to apply them. Where the game moves the character itself (ModelDriven) it's
    // copied from the model instead. Virtual so SimNetworkPuppet can report the peer's real
    // position while its glide catches up.
    private Vector3 position;
    public virtual Vector3 Position => position;
    public float Rotation { get; private set; }

    // The facing last sent to the model; trails Rotation while TurnTo turns it at TurnSpeed.
    private float modelRotation;

    // Yalms (XZ) between Position and the model at the start of the last tick.
    public float ModelDivergence { get; private set; }
    public float MaxModelDivergence { get; private set; }

    // The game, not the sim, moves the character: the local player, or a server-driven move
    // (a carry) in flight.
    protected virtual bool ModelDriven => modelDrivenSeconds > 0f;
    private float modelDrivenSeconds;

    protected void FollowModelFor(float seconds) => modelDrivenSeconds = MathF.Max(modelDrivenSeconds, seconds);

    // False until the sim gives the character a transform; the first tick with a model adopts
    // the model's instead, so a turn can't drag it to the origin.
    private bool placed;

    // Teleport: an ActorSetPos.
    public void SetPosition(Vector3 newPosition)
    {
        position = newPosition;
        SendSetPos();
    }

    public void SetRotation(float rotation)
    {
        turnTarget = null;
        Rotation = rotation;
        modelRotation = rotation;
        SendSetPos();
    }

    public void SetPosition(Placement placement)
    {
        turnTarget = null;
        position = placement.Position;
        Rotation = placement.Rotation;
        modelRotation = placement.Rotation;
        SendSetPos();
    }

    // Puts the model back on the sim's transform, e.g. once an actor that was moved while its
    // spawn was in flight exists.
    protected void ResendTransform() => SendSetPos();

    private void SendSetPos()
    {
        placed = true;
        movePending = false;
        glideTarget = position;
        lastSentTarget = position;
        lastSentRotation = modelRotation;
        Proxy?.SetPos(Coordinates.ToGlobal(position), modelRotation);
    }

    // The client walks the model from wherever it is to each ActorMove's position over exactly
    // this long, at a constant speed, whatever the packet's speed byte says. A move aimed at
    // where the sim will be this far ahead keeps the model level with it; one aimed at the sim's
    // current spot leaves it trailing, still walking for up to this long after the sim stopped.
    internal const float ModelGlideSeconds = 1f;

    private bool movePending;
    private ActorMoveFlags moveFlags;
    private float moveSpeed;

    // Where the model is being walked to, ModelGlideSeconds ahead of the sim on its path.
    private Vector3 glideTarget;

    // A repeat restarts the client's glide from the model's spot and slows it, so it's not resent.
    private Vector3 lastSentTarget;
    private float lastSentRotation;

    // One step of a walk (or a slide, with ActorMoveFlags.Knockback): the position moves now,
    // and the tick ends with an ActorMove aiming the model at `glideTarget` (see ModelGlideSeconds).
    internal void StepTo(Vector3 next, Vector3 glideTarget, ActorMoveFlags flags, float speed)
    {
        placed = true;
        position = next;
        this.glideTarget = glideTarget;
        moveFlags = flags;
        moveSpeed = speed;
        movePending = true;
    }

    // StepTo plus the facing at once, for a glide that already smooths its own rotation.
    internal void StepTo(Placement next, Vector3 glideTarget, ActorMoveFlags flags, float speed)
    {
        turnTarget = null;
        Rotation = next.Rotation;
        modelRotation = next.Rotation;
        StepTo(next.Position, glideTarget, flags, speed);
    }

    private void FlushMove()
    {
        if (!movePending) return;
        movePending = false;
        if (glideTarget != lastSentTarget || modelRotation != lastSentRotation)
            SendMove(Coordinates.ToGlobal(glideTarget), modelRotation, moveFlags, moveSpeed);
        lastSentTarget = glideTarget;
        lastSentRotation = modelRotation;
        moveFlags = ActorMoveFlags.None;
        moveSpeed = 0f;
    }

    // SimPlayer overrides it: the client drops an ActorMove for its own player.
    protected virtual void SendMove(Vector3 worldPosition, float rotation, ActorMoveFlags flags, float speed)
    {
        if (!ModelDriven) Proxy?.Move(worldPosition, rotation, flags, speed);
    }

    // Radians per second the model turns at under TurnTo; null turns it in one Move.
    protected virtual float? TurnSpeed => null;

    private float? turnTarget;

    // Rotation (what mechanics read) takes the new facing at once; only the model turns
    // gradually, so a scenario that faces and fires in the same tick still aims where it faced.
    public void TurnTo(float rotation)
    {
        Rotation = rotation;
        if (TurnSpeed is null)
        {
            turnTarget = null;
            modelRotation = MathUtil.NormalizeRotation(rotation);
            movePending = true;
            return;
        }
        turnTarget = MathUtil.NormalizeRotation(rotation);
    }

    private void TickTurn(float deltaSeconds)
    {
        if (turnTarget is not { } target || TurnSpeed is not { } speed) return;
        modelRotation = MathUtil.StepRotation(modelRotation, target, speed * deltaSeconds);
        movePending = true;
        if (modelRotation == target) turnTarget = null;
    }

    // Transform for an actor the engine hasn't created yet (a packet spawn in flight, see
    // SimEnemy.SpawnFromPacket): the spawn packet already carries it.
    protected void SeedTransform(Vector3 newPosition, float rotation)
    {
        placed = true;
        position = newPosition;
        glideTarget = newPosition;
        lastSentTarget = newPosition;
        Rotation = rotation;
        modelRotation = rotation;
    }

    public void Face(Vector3? target) => Movement.Face(target);
    public void Face(IPositioned? target) => Face(target?.Position);
    public void MoveTo(Vector3 target, float speed = 6f, float? finalRotation = null)
        => Movement.MoveTo(target, speed, finalRotation);
    public void MoveTo(Placement p) => MoveTo(p.Position);
    public void StopMoving() => Movement.Stop();

    public void Intercept(SimTether? tether, float margin = 3f) => Movement.Intercept(tether, margin);
    public bool IsIntercepting => Movement.IsIntercepting;
    public bool IsEasedMoving => Movement.IsEasedMoving;
    public Vector3? MoveDestination => Movement.Destination;

    // forced: the mechanic is taking control, not a strat positioning a bot (see
    // Movement.Follow). Virtual so SimNetworkPuppet can hand a forced follow to its owner.
    public virtual void Follow(SimCharacter? target = null, float speed = 6f, bool forced = false)
        => Movement.Follow(target, speed, forced);


    // -------------------------
    // VFX Subsystem
    // -------------------------

    // Self-attached actor VFX keyed by path.
    // persistent: true  → tracked by sim (might crash if we try to remove vfx after game already did that)
    // persistent: false → fire-and-forget (game is responsible for duration and cleaning of vfx)
    public void AddVfx(string path, float duration = 0f, bool persistent = true)
    {
        if (!Natives.Data.FileExists(path))
        {
            Plugin.Log.Warning($"VFX path not found '{path}'");
            return;
        }
        if (!IsActive) return;
        if (persistent && FindVfx(path) is {} existing)
        {
            existing.Refresh(duration);
            return;
        }
        var spawned = new SimVfx(this, path, duration);
        if (persistent && spawned.IsActive)
            vfx.Add(spawned);
        else if (!persistent)
        {
            if (pendingVfx.Count < AnoMech.Multiplayer.NetGuard.MaxVfxPerEntity) pendingVfx.Add((path, duration));
            else droppedPendingVfx++;
        }
    }

    // Every non-persistent AddVfx since the last drain, sampled for peers like the lockons.
    private readonly List<(string Path, float Duration)> pendingVfx = [];
    private int droppedPendingVfx;

    public IReadOnlyList<(string Path, float Duration)> DrainPendingVfx(out int dropped)
    {
        dropped = droppedPendingVfx;
        droppedPendingVfx = 0;
        if (pendingVfx.Count == 0) return [];
        var result = pendingVfx.ToArray();
        pendingVfx.Clear();
        return result;
    }

    // Every lockon attached since the last drain, so two in one tick (P4's Blizzard+Lightning
    // orbs) both replicate.
    private readonly List<uint> pendingLockonVfxIds = [];
    private int droppedPendingLockonVfxIds;

    public IReadOnlyList<uint> DrainPendingLockonVfxIds(out int dropped)
    {
        dropped = droppedPendingLockonVfxIds;
        droppedPendingLockonVfxIds = 0;
        if (pendingLockonVfxIds.Count == 0) return [];
        var result = pendingLockonVfxIds.ToArray();
        pendingLockonVfxIds.Clear();
        return result;
    }

    // The Lockon check keeps an id a peer sent from reaching the client's handler.
    public void AttachLockonVfx(uint lockonId)
    {
        if (!IsActive || Natives.Vfx.LockonIconName(lockonId) is null) return;
        ActorControl.HeadMarker(lockonId);
        if (pendingLockonVfxIds.Count < AnoMech.Multiplayer.NetGuard.MaxLockonVfxPerEntity) pendingLockonVfxIds.Add(lockonId);
        else droppedPendingLockonVfxIds++;
    }

    // Sampled for peers and reconciled there like the statuses: unlike the fire-and-forget ones
    // above, a persistent VFX ends by removal, which no one-shot event could carry.
    public IReadOnlyList<string> ActivePersistentVfxPaths
        => vfx.Count == 0 ? [] : vfx.Where(v => v.IsActive).Select(v => v.Path).Distinct().ToList();

    public SimVfx? FindVfx(string path)
    {
        return vfx.Find(v => v.IsActive && v.Path == path);
    }

    public void RemoveVfx(string path)
    {
        FindVfx(path)?.Despawn();
    }

    // FIXME: minor, keep track of tethers and slots attached to character
    public bool HasTetherInSlot0(ushort tetherId)
        => Proxy is { Exists: true } chara && chara.GetTetherId(0) == tetherId;

    // -------------------------
    // Status Subsystem
    // -------------------------

    // sourceObject distinguishes independent same-id instances (UMAD P1 Tele-portent applies
    // the same id twice with separate expiries).
    public SimStatus? AddStatus(ushort statusId, float duration = 0f, int stacks = 1, bool overrideStacks = false, GameObjectId sourceObject = default)
    {
        Core.DiagnosticLog.Info($"[SimCharacter] AddStatus: {DiagnosticName} gets status {statusId} (duration={duration:F1}, stacks={stacks}, overrideStacks={overrideStacks}, source=0x{sourceObject.ObjectId:X}).");
        if (FindStatus(statusId, sourceObject) is {} status)
        {
            // overrideStacks: stacks is the absolute target; otherwise it's a
            // relative delta (negative consumes stacks).
            int delta = overrideStacks ? stacks - status.Stacks : stacks;
            status.Reapply(duration, delta);
            if (status.Stacks == 0)
            {
                status.Despawn();   // last stack consumed → remove the status
                return null;
            }
            if (status.Slot >= 0) SendStatusList();
            return status;
        }

        // No existing status: a non-positive request has nothing to remove.
        if (stacks <= 0) return null;
        return SpawnStatus(statusId, duration, (ushort)stacks, sourceObject);
    }

    // For logging: the party role when available, else the type name.
    private string DiagnosticName => (this as ISimPartyMember)?.Role.ToString() ?? GetType().Name;

    public SimStatus AddStatusParam(ushort statusId, int param, float duration = 0f)
        => SpawnStatus(statusId, duration, (ushort)param, default);

    private SimStatus SpawnStatus(ushort statusId, float duration, ushort param, GameObjectId sourceObject)
    {
        var status = new SimStatus(this, statusId, duration, param, sourceObject, FreeStatusSlot(statusId));
        statusList.Add(status);
        if (status.Slot < 0) return status;
        SendStatusList();
        ActorControl.GainEffect(statusId, param);
        return status;
    }

    private const int StatusSlots = 60;

    // The lowest free slot, as the server fills them. Ids without a sheet row get none: the party
    // list and nameplates look the row up.
    private int FreeStatusSlot(ushort statusId)
    {
        if (SimOnlyStatus.Is(statusId) || Natives.Data.Status(statusId) is null) return -1;
        for (var slot = 0; slot < StatusSlots; slot++)
            if (!statusList.Any(s => s.IsActive && s.Slot == slot))
                return slot;
        return -1;
    }

    // Sent on every change rather than once per frame: a status removed and re-added in the same
    // frame keeps its slot, and the client only replays the gain effects when it sees the slot
    // empty in between.
    internal void SendStatusList()
    {
        if (Proxy is { } chara) SendStatusList(chara.Health, chara.Mana);
    }

    private void SendStatusList(uint health, uint mana)
    {
        if (Proxy is not { } chara) return;
        var slots = new StatusSlot[StatusSlots];
        foreach (var status in statusList)
            if (status.IsActive && status.Slot >= 0)
                slots[status.Slot] = new StatusSlot(status.StatusId, status.Stacks, status.RemainingTime, status.SourceObject.ObjectId);
        var shield = Mitigation.ShieldFraction(statusList.Where(s => s.IsActive).Select(s => s.StatusId));
        chara.ReceiveStatusList(slots, health, mana, (byte)Math.Clamp(shield * 100f, 0f, 100f));
    }

    public void RemoveStatus(ushort statusId)
    {
        RemoveStatus(statusId, default);
    }

    // Source-aware: an id the character holds twice (two appliers) needs the right one named.
    public void RemoveStatus(ushort statusId, GameObjectId sourceObject)
    {
        if (FindStatus(statusId, sourceObject) is not {} status) return;
        Core.DiagnosticLog.Info($"[SimCharacter] RemoveStatus: {DiagnosticName} loses status {statusId}.");
        status.Despawn();
    }

    public SimStatus? FindStatus(ushort statusId, GameObjectId sourceObject = default)
    {
        foreach (var status in statusList)
        {
            if (status.IsActive && status.StatusId == statusId && status.SourceObject == sourceObject)
                return status;
        }
        return null;
    }

    public bool HasStatus(ushort statusId) => FindStatus(statusId) != null;

    // Sampled for peers; AddStatus is otherwise entirely local.
    public IReadOnlyList<(ushort StatusId, ushort Stacks, float RemainingTime)> ActiveStatusSnapshot =>
        statusList.Where(s => s.IsActive).Select(s => (s.StatusId, s.Stacks, s.RemainingTime)).ToList();

    public IReadOnlyList<SimStatus> ActiveStatuses => statusList.Where(s => s.IsActive).ToList();

    // The client enforces the Status sheet's Lock* flags from the server's statuses; sim statuses
    // never reach that path, so the sheet is read here.
    public bool MovementLocked => AnyActiveStatusRow(row => row.LockMovement || row.LockControl);
    public bool ActionsLocked => AnyActiveStatusRow(row => row.LockActions || row.LockControl);

    private bool AnyActiveStatusRow(Func<StatusRow, bool> predicate)
    {
        foreach (var status in statusList)
        {
            if (status.IsActive && Natives.Data.Status(status.StatusId) is { } row && predicate(row))
                return true;
        }
        return false;
    }


    // -------------------------
    // Other Subsystem
    // -------------------------

    // Sampled by MultiplayerManager for scripted animation cues (a boss warp, a sleep pose).
    public ushort? AnimationTimelineId { get; private set; }

    // Same reasoning as SimCast.CastSeq: a repeat of the same id must read as a change.
    public int AnimationTimelineSeq { get; private set; }

    public void PlayActionTimeline(ushort timelineId)
    {
        AnimationTimelineId = timelineId;
        AnimationTimelineSeq++;
        ActorControl.PlayActionTimeline(timelineId);
    }

    public void VoiceLine(uint voiceLineId) => ActorControl.PlayVoiceLine(voiceLineId);

    public virtual void SetTargetable(bool targetable) => ActorControl.SetTargetable(targetable);

    // Retail's carry has settled ~0.9s after the AC; the engine animates it, so the sim follows
    // the model until then.
    private const float CarryModelSeconds = 1f;

    public virtual void CarryTo(Vector3 destination)
    {
        FollowModelFor(CarryModelSeconds);
        ActorControl.CarryTo(Coordinates.ToGlobal(destination), Rotation);
    }

    public bool Dead { get; private set; }

    // Retail's death, all on one tick: 0x50 first for a death no action dealt (wall, falling off).
    protected bool GoDown(bool byEnvironment)
    {
        if (!MarkDead()) return false;
        if (byEnvironment) ActorControl.WallDeath(Proxy?.MaxHealth ?? 0);
        PlayDeath();
        return true;
    }

    // Split from PlayDeath for a death the client animates later than the server records it.
    protected bool MarkDead()
    {
        if (Dead) return false;
        Dead = true;
        StopMoving();
        if (Proxy is { Exists: true } chara && chara.GetTetherId(0) != 0) ActorControl.ClearTether();
        return true;
    }

    protected void PlayDeath()
    {
        ActorControl.PlayDeathAnimation();
        ActorControl.SetMode(CharacterModes.Dead);
        statusList.Despawn();
        SendStatusList(0, 0);
    }

    protected void Revive()
    {
        if (!Dead) return;
        Dead = false;
        ActorControl.Revive();
        if (Proxy is { } chara) SendStatusList(chara.MaxHealth, chara.MaxMana);
    }

    public ActorControl ActorControl => field ??= new ActorControl(() => Proxy);
}
