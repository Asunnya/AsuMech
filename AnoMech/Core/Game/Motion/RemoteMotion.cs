using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game.Motion;

// A real remote player's seat: their own client moves them, and the poses they report are the
// position. Orders are dropped; forced moves are queued for the owner to apply to their character,
// and this copy shows them the way a replay shows another player's, by following the poses.
internal sealed class RemoteMotion(Func<IBattleCharaProxy?> proxy, Coordinates coordinates) : CharacterMotion
{
    // The catch-up speed is a floor once the real pose interval is known, anything beyond
    // SnapThreshold (spawn, lag spike, teleport) snaps instead of gliding, and extrapolation only
    // feeds the visual glide.
    private const float CatchUpSpeed = 20f;
    private const float SnapThreshold = 15f;
    private const float AngularCatchUpSpeed = MathF.PI * 20f;
    private const float IntervalSmoothingFactor = 0.3f;
    private const float MinPacingWindowSeconds = 0.05f;
    private const float MaxPoseExtrapolationSeconds = 1f;

    private readonly ModelSync model = new(proxy, coordinates, turnSpeed: null);

    // Where this copy's model is being walked, trailing the reported pose.
    private Vector3 modelPosition;
    private float rotation;
    private bool placed;

    private Vector3? targetPosition;
    private float targetRotation;
    private float timeSinceLastPose;
    private float estimatedPoseInterval = 0.05f;
    private Vector3 poseVelocity;

    private readonly List<ForcedMove> outbox = [];

    // Edge-triggered on the (target, speed) pair: Umad P1's confused chase re-issues Follow every
    // tick with the same target.
    private (SimCharacter? Target, float Speed) lastFollow;

    public override Vector3 Position => targetPosition ?? modelPosition;
    public override float Rotation => rotation;
    public override bool IsMoving => false;

    // The position moves in Tick, which steps toward the pose.
    public void ApplyPose(Vector3 position, float poseRotation)
    {
        if (targetPosition is { } previous)
        {
            estimatedPoseInterval += (timeSinceLastPose - estimatedPoseInterval) * IntervalSmoothingFactor;
            poseVelocity = timeSinceLastPose > MinPacingWindowSeconds ? (position - previous) / timeSinceLastPose : Vector3.Zero;
        }
        targetPosition = position;
        targetRotation = poseRotation;
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
        placed = true;
        modelPosition = position;
        model.SetPos(position);
    }

    public override void SetPlacement(Placement placement)
    {
        placed = true;
        modelPosition = placement.Position;
        rotation = placement.Rotation;
        model.SetPos(placement.Position, placement.Rotation);
    }

    public override void Seed(Placement placement)
    {
        placed = true;
        modelPosition = placement.Position;
        rotation = placement.Rotation;
        model.Assume(placement.Position, placement.Rotation);
    }

    public override void ResendTransform() => model.SetPos(modelPosition);

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
            modelPosition = coordinates.ToLocal(native.Position);
            rotation = native.Rotation;
            model.Assume(modelPosition, rotation);
            placed = true;
        }

        if (gates.Alive && targetPosition is { } rawTarget) FollowPose(deltaSeconds, rawTarget);
        model.Flush(deltaSeconds);
    }

    private void FollowPose(float deltaSeconds, Vector3 rawTarget)
    {
        timeSinceLastPose += deltaSeconds;
        var target = rawTarget + poseVelocity * MathF.Min(timeSinceLastPose, MaxPoseExtrapolationSeconds);
        var delta = target - modelPosition;
        var dist = delta.Length();
        var remainingWindow = MathF.Max(estimatedPoseInterval - timeSinceLastPose, MinPacingWindowSeconds);
        var step = MathF.Max(dist / remainingWindow, CatchUpSpeed) * deltaSeconds;
        var nextRotation = MathUtil.StepRotation(rotation, targetRotation, AngularCatchUpSpeed * deltaSeconds);
        if (dist > SnapThreshold)
        {
            SetPlacement(new Placement(target, nextRotation));
            return;
        }
        modelPosition = dist <= step ? target : modelPosition + delta / dist * step;
        rotation = nextRotation;
        model.Face(nextRotation);
        // The model is aimed where the peer will be ModelSync.LeadSeconds ahead.
        var glideSpeed = deltaSeconds > 0f ? MathF.Min(dist, step) / deltaSeconds : 0f;
        model.Glide(target + poseVelocity * ModelSync.LeadSeconds, ActorMoveFlags.None, glideSpeed);
    }
}
