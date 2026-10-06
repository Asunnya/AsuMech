using System;
using System.Linq;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// Applied to the characters an action hit (EnemyActionContext.Hits), after the area decided who
// they are. Effects run in list order: one listed after a Damage sees who that damage killed.
public interface IEnemyActionEffect
{
    void Apply(EnemyActionContext ctx);
}

// Authoring helpers: `using static AnoMech.Core.EnemyActions.EnemyActionEffects;`.
public static class EnemyActionEffects
{
    public static IEnemyActionEffect Damage(DamageSpec spec, Severity? severity = null, Distribution? split = null)
        => new DamageEffect(spec, severity ?? Severity.Normal, split ?? Distribution.Each);

    // Survivors only.
    public static IEnemyActionEffect ApplyStatus(ushort statusId, float duration) => new ApplyStatusEffect(statusId, duration);

    // Survivors only.
    public static IEnemyActionEffect RemoveStatus(ushort statusId) => new RemoveStatusEffect(statusId);

    // One more stack of the family's `times` status, or the death it completes. Survivors only.
    public static IEnemyActionEffect ApplyRuin(RuinSpec ruin, int times, float duration)
        => new ApplyRuinEffect(ruin, ruin.StatusId(times), times, duration);

    // ApplyStatus, except whoever already holds `maxStacks` dies instead. Survivors only.
    public static IEnemyActionEffect ApplyStatusOrOverload(ushort statusId, int maxStacks, float duration = 0f)
        => new ApplyStatusOrOverloadEffect(statusId, maxStacks, duration);

    // `followUp` cast by the same caster once this resolve's deaths are dealt, when `when` holds.
    public static IEnemyActionEffect FollowUp(EnemyAction followUp, Func<EnemyActionContext, bool> when)
        => new FollowUpEffect(followUp, when);

    // Away from the caster, by a Knockback sheet row. Survivors only. `knockbackDelay` is from
    // resolve; null = TimingSpec.DamageDelay.
    public static IEnemyActionEffect Knockback(uint knockbackId, float? knockbackDelay = null)
        => new KnockbackEffect((_, _) => KnockbackLookup.TryGet(knockbackId, out var distance, out var speed) ? (distance, speed) : null, knockbackDelay);

    // Away from the caster, for an action with no known Knockback row. Survivors only.
    public static IEnemyActionEffect Knockback(float distance, float speed, float? knockbackDelay = null)
        => new KnockbackEffect((_, _) => (distance, speed), knockbackDelay);

    // Away from the caster, as far as `distance` says for each target (read at resolve; null = not
    // pushed). Survivors only.
    public static IEnemyActionEffect Knockback(Func<EnemyActionContext, SimCharacter, float?> distance, float speed, float? knockbackDelay = null)
        => new KnockbackEffect((ctx, target) => distance(ctx, target) is { } d ? (d, speed) : null, knockbackDelay);

    // Kills by facing alone: with `lookAway`, whoever has the origin in their front 90° arc; without,
    // whoever has it in their back 90° arc.
    public static IEnemyActionEffect Gaze(bool lookAway) => new GazeEffect(lookAway);

    // `effect` applied to the cast's target alone, when the area caught it.
    public static IEnemyActionEffect OnTarget(IEnemyActionEffect effect) => new FilteredEffect(effect, castTarget: true);

    // `effect` applied to everyone hit but the cast's target.
    public static IEnemyActionEffect OnOthers(IEnemyActionEffect effect) => new FilteredEffect(effect, castTarget: false);

    // `effect` applied to the `count` hit nearest the origin (the front of a wild charge).
    public static IEnemyActionEffect OnFront(int count, IEnemyActionEffect effect) => new FrontEffect(effect, count);
}

internal sealed class FrontEffect(IEnemyActionEffect effect, int count) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        var hits = ctx.Hits;
        ctx.Hits = hits.Take(count).ToList();
        try { effect.Apply(ctx); }
        finally { ctx.Hits = hits; }
    }
}

internal sealed class FilteredEffect(IEnemyActionEffect effect, bool castTarget) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        var hits = ctx.Hits;
        ctx.Hits = hits.Where(t => ReferenceEquals(t, ctx.Target) == castTarget).ToList();
        try { effect.Apply(ctx); }
        finally { ctx.Hits = hits; }
    }
}

// A hit on someone an earlier hit already kills still shows its own number.
internal sealed class DamageEffect(DamageSpec spec, Severity severity, Distribution split) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        for (var i = 0; i < ctx.Hits.Count; i++)
        {
            var target = ctx.Hits[i];
            var (hit, reason) = split.Assign(ctx, i, target);
            var hitSpec = hit?.Spec ?? spec;
            var hitSeverity = hit?.Severity ?? severity;
            var cause = DamageCheck.LethalCause(target, hitSpec, hitSeverity, ctx.Party);
            ctx.ShowDamage(target, hitSeverity.FlyTextAmount(kills: cause != null), hitSpec.Icon);
            if (cause != null) ctx.Kill(target, Explain(reason, cause));
        }
    }

    // `cause` "" kills with no explanation of its own.
    private static string? Explain(string? reason, string cause)
        => cause.Length == 0 ? reason : reason is null ? cause : $"{reason}; {cause}";
}

internal sealed class ApplyStatusEffect(ushort statusId, float duration) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
            if (!ctx.IsKilled(target) && target.IsAlive())
                target.AddStatus(statusId, duration);
    }
}

internal sealed class RemoveStatusEffect(ushort statusId) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
            if (!ctx.IsKilled(target) && target.IsAlive())
                target.RemoveStatus(statusId);
    }
}

internal sealed class ApplyRuinEffect(RuinSpec ruin, ushort statusId, int times, float duration) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
        {
            if (ctx.IsKilled(target) || !target.IsAlive()) continue;
            if (ruin.Overloads(target, times))
                ctx.Kill(target, $"{StatusLookup.Name(statusId)} overload");
            else
                target.AddStatus(statusId, duration);
        }
    }
}

internal sealed class ApplyStatusOrOverloadEffect(ushort statusId, int maxStacks, float duration) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
        {
            if (ctx.IsKilled(target) || !target.IsAlive()) continue;
            if (target.FindStatus(statusId) is { } status && status.Stacks >= maxStacks)
                ctx.Kill(target, $"already at {maxStacks} {StatusLookup.Name(statusId)}");
            else
                target.AddStatus(statusId, duration);
        }
    }
}

internal sealed class GazeEffect(bool lookAway) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        var from = ctx.Origin.Position;
        foreach (var target in ctx.Hits)
        {
            var facing = target.Placement();
            if (lookAway ? facing.IsLookingAt(from) : facing.IsLookingAwayFrom(from))
                ctx.Kill(target, lookAway ? "looked at the gaze" : "faced away from the gaze");
        }
    }
}

internal sealed class FollowUpEffect(EnemyAction followUp, Func<EnemyActionContext, bool> when) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        if (!when(ctx)) return;
        var caster = ctx.Caster;
        ctx.AfterResolve(() => caster.Cast(followUp));
    }
}

internal sealed class KnockbackEffect(Func<EnemyActionContext, SimCharacter, (float Distance, float Speed)?> push, float? knockbackDelay) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
        {
            if (ctx.IsKilled(target) || !target.IsAlive() || target is not ISimPartyMember) continue;
            if (push(ctx, target) is { } p)
                ctx.Knockback(target, ctx.Caster.Position, p.Distance, p.Speed, knockbackDelay);
        }
    }
}
