using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game.Motion;

// A real remote player's seat: their own client moves them, and the poses they report are the
// position. Orders are dropped; forced moves are queued for the owner to apply to their character,
// and this copy shows them the way retail relays another player: an ActorMove aimed where they are
// now, walked at their own pace.
internal sealed class RemoteMotion(Func<IBattleCharaProxy?> proxy, Coordinates coordinates) : CharacterMotion
{
    // Anything beyond SnapThreshold (spawn, lag spike, teleport) snaps instead of walking, and
    // extrapolation only feeds the walk.
    private const float SnapThreshold = 15f;
    private const float AngularCatchUpSpeed = MathF.PI * 20f;
    private const float MaxPoseExtrapolationSeconds = 1f;
    // Faster than any run: a forced move or a teleport, which carrying on would overshoot.
    private const float FastestRun = 10f;
    // An ActorMove's speed byte holds tenths of a y/s, up to this.
    private const float MaxWalkSpeed = 25.5f;

    private readonly ModelSync model = new(proxy, coordinates, turnSpeed: null);

    // Where the client has this copy, if its ground controller walks as ModelSync says: to the last
    // ActorMove's spot at that packet's speed.
    private Vector3 modelPosition;
    private Vector3 walkTarget;
    private float walkSpeed;
    // What this tick's ActorMove carries, if ModelSync sends one.
    private Vector3 aim;
    private float aimSpeed;
    private float rotation;
    private bool placed;

    private Vector3? targetPosition;
    private float targetRotation;
    private Vector3 poseVelocity;
    private float timeSinceLastPose;

    private readonly List<ForcedMove> outbox = [];

    // Edge-triggered on the (target, speed) pair: Umad P1's confused chase re-issues Follow every
    // tick with the same target.
    private (SimCharacter? Target, float Speed) lastFollow;

    public override Vector3 Position => targetPosition ?? modelPosition;
    public override float Rotation => rotation;
    public override bool IsMoving => false;

    // As the owner's client reported it.
    public Vector3 PoseVelocity => poseVelocity;

    // The model walks toward the pose in Tick. Only a new spot or velocity starts the
    // extrapolation over: a repeat, or a turn on its own, says nothing new about where they are.
    public void ApplyPose(Vector3 position, float poseRotation, Vector3 velocity)
    {
        targetRotation = poseRotation;
        if (position == targetPosition && velocity == poseVelocity) return;
        targetPosition = position;
        poseVelocity = velocity;
        timeSinceLastPose = 0f;
    }

    // In the order the mechanic made them.
    public IReadOnlyList<ForcedMove> DrainForced()
    {
        if (outbox.Count == 0) return [];
        var moves = outbox.ToArray();
        outbox.Clear();
        return moves;
    }

    public override void SetPosition(Vector3 position)
    {
        Place(position);
        model.SetPos(position);
    }

    public override void SetPlacement(Placement placement)
    {
        Place(placement.Position);
        rotation = placement.Rotation;
        model.SetPos(placement.Position, placement.Rotation);
    }

    public override void Seed(Placement placement)
    {
        Place(placement.Position);
        rotation = placement.Rotation;
        model.Assume(placement.Position, placement.Rotation);
    }

    public override void ResendTransform()
    {
        Place(modelPosition);
        model.SetPos(modelPosition);
    }

    // The model stands there, with no walk under way.
    private void Place(Vector3 position)
    {
        placed = true;
        modelPosition = walkTarget = aim = position;
        walkSpeed = aimSpeed = 0f;
    }

    public override void TurnTo(float newRotation) { }
    public override void Face(Vector3 target) { }
    public override void MoveTo(Vector3 target, float speed, float? finalRotation) { }
    public override void Intercept(SimTether? tether, float margin) { }
    public override void Stop() { }

    // A strat walking its bots: the person in this seat is playing, not being driven.
    public override void Follow(SimCharacter? target, float speed) { }

    // Sent on a change only; ForceFollow is re-issued every tick.
    private void ForceFollow(SimCharacter? target, float speed)
    {
        var liveTarget = target.IsAlive() ? target : null;
        if (ReferenceEquals(liveTarget, lastFollow.Target) && (liveTarget == null || speed == lastFollow.Speed)) return;
        lastFollow = (liveTarget, speed);
        outbox.Add(new ForcedMove.Follow(liveTarget, speed));
    }

    public override void Force(ForcedMove move)
    {
        switch (move)
        {
            case ForcedMove.Follow follow:
                ForceFollow(follow.Target, follow.Speed);
                return;
            // The owner is sent a heading, taken from where this copy has them.
            case ForcedMove.PushAway away:
                var position = Position;
                var dx = position.X - away.From.X;
                var dz = position.Z - away.From.Z;
                if (dx * dx + dz * dz < 1e-6f) return;
                outbox.Add(new ForcedMove.Push(MathF.Atan2(dx, dz), away.Distance, away.DurationSeconds));
                return;
            case ForcedMove.IceSlide slide:
                var offset = slide.To - Position;
                if (offset.LengthSquared() < 1e-6f) return;
                outbox.Add(new ForcedMove.Push(MathF.Atan2(offset.X, offset.Z), offset.Length(), slide.DurationSeconds));
                return;
            // This copy snaps too, so mechanics read the new spot until the owner's next pose.
            case ForcedMove.Teleport teleport:
                SetPlacement(teleport.To);
                if (targetPosition != null) targetPosition = teleport.To.Position;
                poseVelocity = Vector3.Zero;
                break;
        }
        outbox.Add(move);
    }

    public override void Clear()
    {
        outbox.Clear();
        lastFollow = default;
    }

    public override void Tick(float deltaSeconds, MotionGates gates)
    {
        if (!placed && proxy() is { Exists: true } native)
        {
            Place(coordinates.ToLocal(native.Position));
            rotation = native.Rotation;
            model.Assume(modelPosition, rotation);
        }

        if (gates.Alive && targetPosition is { } pose) FollowPose(deltaSeconds, pose);
        if (model.Flush(deltaSeconds)) (walkTarget, walkSpeed) = (aim, aimSpeed);
        Walk(deltaSeconds);
    }

    // The client's own walk, which runs after the packets of its frame.
    private void Walk(float deltaSeconds)
    {
        var away = walkTarget - modelPosition;
        var distance = away.Length();
        var step = walkSpeed * deltaSeconds;
        modelPosition = distance <= step ? walkTarget : modelPosition + away / distance * step;
    }

    private void FollowPose(float deltaSeconds, Vector3 pose)
    {
        timeSinceLastPose += deltaSeconds;
        var velocity = poseVelocity.Length() <= FastestRun ? poseVelocity : Vector3.Zero;
        var target = pose + velocity * MathF.Min(timeSinceLastPose, MaxPoseExtrapolationSeconds);
        rotation = MathUtil.StepRotation(rotation, targetRotation, AngularCatchUpSpeed * deltaSeconds);
        if (Vector3.Distance(target, modelPosition) > SnapThreshold)
        {
            SetPlacement(new Placement(target, rotation));
            return;
        }
        // The player's own pace while the copy keeps up with them, slow enough that it's still
        // walking when the next packet comes: the client walks this frame's time on top of the
        // interval. In the byte's tenths, rounded down for the same reason.
        aim = target + velocity * ModelSync.LeadSeconds;
        var walkSeconds = ModelSync.SendInterval + deltaSeconds;
        aimSpeed = MathF.Floor(MathF.Min(Vector3.Distance(aim, modelPosition) / walkSeconds, MaxWalkSpeed) * 10f) / 10f;
        model.Face(rotation);
        model.Glide(aim, ActorMoveFlags.None, aimSpeed);
    }
}
