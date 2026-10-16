using System;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Game.Motion;

// Keeps a model on a transform the sim owns, through the packets the server would send: ActorSetPos
// for a teleport, ActorMove for a walk, a slide or a turn, the NPC knockback packet for an NPC's
// knockback.
internal sealed class ModelSync(Func<IBattleCharaProxy?> proxy, Coordinates coordinates, float? turnSpeed)
{
    // A character walking on the ground takes ActorMove through the client's ground controller,
    // which walks at the packet's speed byte and expects a packet every ~0.3s aimed at where the
    // server has the character now, as retail relays another player's. Per-frame packets aimed
    // ahead made the model rush. UNVERIFIED in game: both values come from the client's code.
    public const float LeadSeconds = 0f;
    public const float SendInterval = 0.2f;

    // The facing last sent to the model; trails the sim's under a turn speed.
    private float modelRotation;
    private float? turnTarget;

    private bool movePending;
    private ActorMoveFlags moveFlags;
    private float moveSpeed;

    // Where the model is being walked to, LeadSeconds ahead of the sim on its path.
    private Vector3 glideTarget;

    // Skips the SendInterval wait: the step that stops a walk must reach the model at once.
    private bool sendNow;
    private float sinceSend = SendInterval;

    // A repeat restarts the client's glide from the model's spot and slows it, so it's not resent.
    private Vector3 lastSentTarget;
    private float lastSentRotation;

    // Teleport: an ActorSetPos. A null rotation keeps the model's facing and any turn in progress.
    public void SetPos(Vector3 position, float? rotation = null)
    {
        if (rotation is { } r)
        {
            turnTarget = null;
            modelRotation = r;
        }
        movePending = false;
        sendNow = false;
        glideTarget = position;
        lastSentTarget = position;
        lastSentRotation = modelRotation;
        proxy()?.SetPos(coordinates.ToGlobal(position), modelRotation);
    }

    // For an actor whose spawn packet already carries the transform, or a model that got there on
    // its own (a server carry).
    public void Assume(Vector3 position, float? rotation = null)
    {
        if (rotation is { } r && !movePending)
        {
            turnTarget = null;
            modelRotation = r;
        }
        glideTarget = position;
        lastSentTarget = position;
        lastSentRotation = modelRotation;
    }

    // One step of a walk (or a slide, with ActorMoveFlags.Knockback): Flush aims the model at
    // `target` (see LeadSeconds). `now` for the step that ends the walk.
    public void Glide(Vector3 target, ActorMoveFlags flags, float speed, bool now = false)
    {
        glideTarget = target;
        moveFlags = flags;
        moveSpeed = speed;
        movePending = true;
        sendNow |= now;
    }

    // One packet; the client slides the model there itself, keeping its facing.
    public void NpcKnockback(Vector3 position, float durationSeconds)
    {
        movePending = false;
        sendNow = false;
        glideTarget = position;
        lastSentTarget = position;
        lastSentRotation = modelRotation;
        proxy()?.NpcKnockback(coordinates.ToGlobal(position), modelRotation, durationSeconds);
    }

    // Turns at the turn speed; at once without one.
    public void Turn(float rotation)
    {
        var target = MathUtil.NormalizeRotation(rotation);
        if (turnSpeed is null)
        {
            turnTarget = null;
            modelRotation = target;
            movePending = true;
            return;
        }
        turnTarget = target;
    }

    // For a glide that already smooths its own rotation.
    public void Face(float rotation)
    {
        turnTarget = null;
        modelRotation = rotation;
        movePending = true;
    }

    // Once per tick, after the sim moved.
    public void Flush(float deltaSeconds)
    {
        sinceSend += deltaSeconds;
        if (turnTarget is { } target && turnSpeed is { } speed)
        {
            modelRotation = MathUtil.StepRotation(modelRotation, target, speed * deltaSeconds);
            movePending = true;
            if (modelRotation == target) turnTarget = null;
        }

        if (!movePending || (!sendNow && sinceSend < SendInterval)) return;
        movePending = false;
        sendNow = false;
        sinceSend = 0f;
        if (glideTarget != lastSentTarget || modelRotation != lastSentRotation)
            proxy()?.Move(coordinates.ToGlobal(glideTarget), modelRotation, moveFlags, moveSpeed);
        lastSentTarget = glideTarget;
        lastSentRotation = modelRotation;
        moveFlags = ActorMoveFlags.None;
        moveSpeed = 0f;
    }
}
