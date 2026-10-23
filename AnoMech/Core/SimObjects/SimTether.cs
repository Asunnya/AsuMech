using System;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Core.SimObjects;

// Two-ended tether. Each side writes the channeling-sheet id at slot 0 of its
// VfxContainer.Tethers, pointing at the other side's GameObjectId. Optionally
// asks each character to host a matching debuff for the tether's duration —
// SimCharacter.AddStatus(id, duration) owns the countdown and auto-removes on expiry.
//
// Each end is an ITetherEnd (see TetherEnd.cs) that resolves to a live character
// every tick: a fixed anchor, the farthest player, or a passable beam-pass. The
// `from` end hosts the VFX (drawn from `from` toward `to`). SimTether owns the
// per-tick state (currentSource/currentTarget) and re-resolves both ends each
// frame, swapping VFX + debuff whenever an end migrates to a new character.
//
// SimTether ticks its own elapsed counter so it can clear the tether VFX when
// duration is reached. Both auto-expire and Despawn (called via SimWorld.Reset
// or directly) use a sentinel check: only clear the slot when our TetherId is
// still occupying it. This handles chained tethers that overwrite the same
// slot in the same frame (e.g. P5 Delta prep → real at t=29s) without racing
// our own ClearTether against the new SetTether.
//
// It ends on its own when it expires, when an end dies, or when its BreaksWhen condition holds,
// and raises TetherEnded for each; a scenario's own Despawn raises nothing.
public sealed class SimTether : ISimObject
{
    private const byte Slot = 0;

    private readonly ITetherEnd from;
    private readonly ITetherEnd to;
    private readonly TetherContext ctx;
    private SimStatus? statusA;
    private SimStatus? statusB;
    private readonly float duration;
    private readonly ushort debuffStatusId;
    private float elapsed;
    private float age;
    private bool active;
    private SimCharacter? currentSource;
    private SimCharacter? currentTarget;
    private ConditionalStatus? conditionalStatus;
    private bool autoFaceTarget;
    private Predicate<SimTether>? breaksWhen;
    private readonly SimEvents? events;

    public ushort TetherId { get; }

    public SimCharacter? A => currentSource;
    public SimCharacter? B => currentTarget;
    public bool IsActive => active;

    public bool StretchGt(float distance) =>
        A is { } a && B is { } b && Vector3.DistanceSquared(a.Position, b.Position) > distance * distance;
    public bool StretchLt(float distance) =>
        A is { } a && B is { } b && Vector3.DistanceSquared(a.Position, b.Position) < distance * distance;

    internal SimTether(ITetherEnd from, ITetherEnd to, TetherContext ctx, ushort debuffStatusId, float duration, SimEvents? events)
    {
        this.events = events;
        this.from = from;
        this.to = to;
        this.ctx = ctx;
        TetherId = ctx.TetherId;
        this.debuffStatusId = debuffStatusId;
        this.duration = duration;

        // Seed both ends. At least one end is Fixed (enforced by the SimWorld.Tether
        // overload set); seeding it first hands the dynamic end a stable reference.
        if (from is FixedEnd)
        {
            currentSource = from.Resolve(null, null, ctx);
            currentTarget = to.Resolve(null, currentSource, ctx);
        }
        else
        {
            currentTarget = to.Resolve(null, null, ctx);
            currentSource = from.Resolve(null, currentTarget, ctx);
        }

        CreateVfx();
        if (debuffStatusId != 0 && duration > 0f)
        {
            statusA = currentSource?.AddStatus(debuffStatusId, duration);
            statusB = currentTarget?.AddStatus(debuffStatusId, duration);
        }
        active = true;
    }

    public void Tick(float deltaSeconds)
    {
        if (!active) return;
        age += deltaSeconds;
        if (duration > 0f)
        {
            elapsed += deltaSeconds;
            if (elapsed >= duration)
            {
                if (conditionalStatus != null)
                {
                    currentSource?.RemoveStatus(conditionalStatus.StatusId);
                    currentTarget?.RemoveStatus(conditionalStatus.StatusId);
                }
                ClearTetherVfxIfOwned();
                active = false;
                events?.Raise(new TetherEnded(this, TetherEndReason.Expired));
                return;
            }
        }

        var nextTarget = to.Resolve(currentTarget, currentSource, ctx);
        if (nextTarget != currentTarget)
        {
            // The source keeps the slot, so the new tether just overwrites it, as the server does.
            if (nextTarget == null) ClearTetherVfxIfOwned();
            statusB?.Despawn();
            currentTarget = nextTarget;
            CreateVfx();
            TargetTetheredPlayer();
            if (debuffStatusId != 0 && duration > 0f)
            {
                statusB = currentTarget?.AddStatus(debuffStatusId, duration);
            }
        }

        var nextSource = from.Resolve(currentSource, currentTarget, ctx);
        if (nextSource != currentSource)
        {
            ClearTetherVfxIfOwned();
            statusA?.Despawn();
            currentSource = nextSource;
            CreateVfx();
            if (debuffStatusId != 0 && duration > 0f)
            {
                statusA = currentSource?.AddStatus(debuffStatusId, duration);
            }
        }

        if (currentSource is { } a && currentTarget is { } b && (!a.IsAlive() || !b.IsAlive()))
        {
            End(TetherEndReason.EndDied);
            return;
        }
        if (breaksWhen?.Invoke(this) == true)
        {
            End(TetherEndReason.Stretched);
            return;
        }

        if (conditionalStatus != null && age >= ConditionalStatusDelay)
        {
            if (conditionalStatus.Condition(this))
            {
                // Pinned at 1 stack (not the AddStatus default of "add one more") -- this runs
                // every tick the condition holds, so the additive default would pile on a new
                // stack per tick instead of just holding the debuff present.
                currentSource?.AddStatus(conditionalStatus.StatusId, stacks: 1, overrideStacks: true);
                currentTarget?.AddStatus(conditionalStatus.StatusId, stacks: 1, overrideStacks: true);
            }
            else
            {
                currentSource?.RemoveStatus(conditionalStatus.StatusId);
                currentTarget?.RemoveStatus(conditionalStatus.StatusId);
            }
        }

        if (autoFaceTarget && currentTarget != null)
        {
            currentSource?.Face(currentTarget);
        }
    }
    
    public void Despawn()
    {
        if (active)
        {
            ClearTetherVfxIfOwned();
            active = false;
        }
        // SimStatus.Despawn is idempotent — safe even if already auto-expired.
        statusA?.Despawn();
        statusB?.Despawn();
        if (conditionalStatus != null)
        {
            currentSource?.RemoveStatus(conditionalStatus.StatusId);
            currentTarget?.RemoveStatus(conditionalStatus.StatusId);
        }
    }

    private void End(TetherEndReason reason)
    {
        Despawn();
        events?.Raise(new TetherEnded(this, reason));
    }

    // Checked every tick, e.g. t => t.StretchGt(maxLength) for a tether that snaps when pulled too far.
    public SimTether BreaksWhen(Predicate<SimTether> condition)
    {
        breaksWhen = condition;
        return this;
    }

    private void CreateVfx()
    {
        if (currentSource != null && currentTarget != null)
            currentSource.ActorControl.SetTether(TetherId, currentTarget.GameObjectId);
    }

    // Sentinel-checked clear: only wipe a slot we still own. A chained tether
    // (new SetTether on the same slot via the new SimTether ctor) will have
    // overwritten Vfx.Tethers[slot].Id; we leave that alone.
    private void ClearTetherVfxIfOwned()
    {
        if (currentSource is { Proxy: { } source } owner && source.GetTetherId(Slot) == TetherId)
            owner.ActorControl.ClearTether();
    }

    public SimTether SetConditionalStatus(ushort statusId, Predicate<SimTether> predicate)
    {
        conditionalStatus = new ConditionalStatus(statusId, predicate);
        return this;
    }
    
    // The server first checks the condition about a second after the tether goes out.
    private const float ConditionalStatusDelay = 1f;

    private record ConditionalStatus(ushort StatusId, Predicate<SimTether> Condition);

    // A source enemy also targets whoever it is tethered to.
    public SimTether SetAutoFaceTarget(bool value)
    {
        autoFaceTarget = value;
        TargetTetheredPlayer();
        return this;
    }

    private void TargetTetheredPlayer()
    {
        if (autoFaceTarget && currentSource is SimEnemy enemy && currentTarget != null)
            enemy.SetTarget(currentTarget, follow: false);
    }
}
