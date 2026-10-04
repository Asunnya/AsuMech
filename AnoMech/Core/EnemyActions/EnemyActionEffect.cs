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
    public static IEnemyActionEffect Damage(DamageSpec spec) => new DamageEffect(spec);

    // Too few players in the stack kills all of them, unless a tank is allowed to take it on
    // `understackedTankMitigation`.
    public static IEnemyActionEffect StackDamage(DamageSpec spec, int min, float? understackedTankMitigation = null)
        => new StackDamageEffect(spec, min, understackedTankMitigation);

    // Survivors only.
    public static IEnemyActionEffect ApplyStatus(ushort statusId, float duration) => new ApplyStatusEffect(statusId, duration);

    // Survivors only.
    public static IEnemyActionEffect RemoveStatus(ushort statusId) => new RemoveStatusEffect(statusId);

    // One more stack of the family's `times` status, or the death it completes. Survivors only.
    public static IEnemyActionEffect ApplyRuin(RuinSpec ruin, int times, float duration)
        => new ApplyRuinEffect(ruin, ruin.StatusId(times), times, duration);

    // Away from the caster, by a Knockback sheet row. Survivors only.
    public static IEnemyActionEffect Knockback(uint knockbackId) => new KnockbackEffect(knockbackId);

    // `effect` applied to the cast's target alone, when the area caught it.
    public static IEnemyActionEffect OnTarget(IEnemyActionEffect effect) => new FilteredEffect(effect, castTarget: true);

    // `effect` applied to everyone hit but the cast's target.
    public static IEnemyActionEffect OnOthers(IEnemyActionEffect effect) => new FilteredEffect(effect, castTarget: false);
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

internal sealed class DamageEffect(DamageSpec spec) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        foreach (var target in ctx.Hits)
            Hit(ctx, spec, target, spec.RequiredMitigation);
    }

    internal static void Hit(EnemyActionContext ctx, DamageSpec spec, SimCharacter target, float requiredMitigation)
    {
        var distance = DistanceXZ(target, ctx.Origin);
        var killCause = distance < spec.LethalWithin
            ? $"{distance:F0}y from it, lethal inside {spec.LethalWithin:F0}y"
            : DamageCheck.LethalCause(target, spec, ctx.Party, requiredMitigation);
        Land(ctx, spec, target, killCause);
    }

    // A hit on someone an earlier hit already kills still shows its own number. `killCause` "" kills
    // with no explanation.
    internal static void Land(EnemyActionContext ctx, DamageSpec spec, SimCharacter target, string? killCause)
    {
        ctx.ShowDamage(target, spec.FlyTextAmount(kills: killCause != null), spec.Icon);
        if (killCause != null) ctx.Kill(target, killCause.Length == 0 ? null : killCause);
    }

    private static float DistanceXZ(SimCharacter target, Game.Placement origin)
    {
        var dx = target.Position.X - origin.Position.X;
        var dz = target.Position.Z - origin.Position.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}

internal sealed class StackDamageEffect(DamageSpec spec, int min, float? understackedTankMitigation) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        var understacked = ctx.Hits.Count < min;
        foreach (var target in ctx.Hits)
        {
            var tankSoaks = understacked && understackedTankMitigation != null && DamageCheck.IsTank(target);
            if (understacked && !tankSoaks)
            {
                DamageEffect.Land(ctx, spec, target, $"{ctx.Hits.Count}/{min} players in stack");
                continue;
            }
            var required = tankSoaks ? MathF.Max(spec.RequiredMitigation, understackedTankMitigation!.Value) : spec.RequiredMitigation;
            DamageEffect.Hit(ctx, spec, target, required);
        }
    }
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

internal sealed class KnockbackEffect(uint knockbackId) : IEnemyActionEffect
{
    public void Apply(EnemyActionContext ctx)
    {
        if (!KnockbackLookup.TryGet(knockbackId, out var distance, out var speed)) return;
        foreach (var target in ctx.Hits)
        {
            if (ctx.IsKilled(target) || !target.IsAlive() || target is not ISimPartyMember member) continue;
            member.Knockback(ctx.Caster.Position, distance, speed);
            ctx.ShowDamage(target, 0, FlyTextIcon.Unique);
        }
    }
}
