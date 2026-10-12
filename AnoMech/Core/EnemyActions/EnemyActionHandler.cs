using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.EnemyActions;

// Runs EnemyActions for one SimEnemy: the visuals go out as its SimCast's packets, everything
// mechanical goes on the scenario's EventScheduler.
internal sealed class EnemyActionHandler(SimEnemy caster, SimCast cast, SimWorld world)
{
    private EnemyActionCast? casting;

    public EnemyActionCast Start(EnemyAction action, CastTarget at, byte animationVariation)
    {
        var handle = new EnemyActionCast();
        if (caster.Dead)
        {
            handle.Cancelled();
            return handle;
        }
        var id = action.ActionId;
        var target = at.Character;
        var location = at.Location;
        var sheetCastTime = action.Cast.CastSecondsOverride ?? Natives.Data.Action(id)?.CastSeconds ?? 0f;
        var castTime = MathF.Max(0f, sheetCastTime - CastSpec.ActionEffectOffset);
        GameObjectId? castTarget = location is null ? (target ?? caster).GameObjectId : null;
        DiagnosticLog.Info(
            $"[EnemyAction] Cast: {ActionLookup.Name(id)} ({id}) by {caster.DisplayName} from ({caster.Position.X:F1},{caster.Position.Z:F1}) castSeconds={castTime:F2}.");

        if (castTime > 0f)
        {
            casting = handle;
            cast.NativeCast(id, ActionType.Action, action.Cast.OmenDelay, castTime, interruptible: false,
                rotation: caster.Rotation + action.Cast.Rotation, position: location, targetId: castTarget);
        }

        // Resolve goes first: an instant action runs both inline, and the ActionEffect carries the hits.
        var delivery = new Delivery();
        if (action.Effects.Count > 0)
            Schedule(castTime + action.Timing.ResolveSnapshotOffset, () =>
            {
                if (handle.IsCancelled) return;
                if (casting == handle) casting = null;
                Resolve(action, target, location, handle, delivery);
            });
        Schedule(sheetCastTime, () =>
        {
            if (handle.IsCancelled) return;
            if (casting == handle) casting = null;
            Release(action, target, location, castTarget, animationVariation, delivery);
        });
        return handle;
    }

    public void CancelCast(CastCancelReason reason)
    {
        if (casting is not { } handle) return;
        casting = null;
        handle.Cancelled();
        cast.Cancel(reason);
    }

    // Faces a target first: the packet carries the caster's rotation. A ground location doesn't turn
    // the caster, since the game aims some lines from behind it. Some actions animate only when
    // delivered to a target, but one outside CharacterManager null-derefs ApplyAll.
    private void Release(EnemyAction action, SimCharacter? target, Vector3? location, GameObjectId? castTarget, byte animationVariation, Delivery delivery)
    {
        var id = action.ActionId;
        if (target == caster) target = null;
        var aim = location ?? target?.Position;
        caster.Face(target?.Position);
        var listed = new List<SimCharacter>();
        if (target is not null) listed.Add(target);
        listed.AddRange(delivery.Damage.Select(d => d.Target));
        listed.AddRange(delivery.Pushes.Select(p => p.Target));
        var targets = new List<ActionEffectTarget>();
        var confirmations = new List<(SimCharacter Target, byte Index)>();
        foreach (var who in listed.Distinct())
        {
            if (!Natives.BattleCharas.IsInCharacterManager(who.GameObjectId.ObjectId)) continue;
            var effects = new List<ActionEffectEntry>();
            var damage = delivery.Damage.Where(d => d.Target == who).OrderByDescending(d => d.Amount).ToList();
            var push = delivery.Pushes.Where(p => p.Target == who).ToList();
            if (damage.Count > 0)
            {
                effects.Add(ActionEffectEntry.Damage(damage[0].Amount, damage[0].Kind.AttackType()));
                confirmations.Add((who, (byte)targets.Count));
            }
            // A push that deals nothing still gets a typeless zero hit, and no EffectResult.
            else if (push.Count > 0)
                effects.Add(ActionEffectEntry.Damage(0, 0));
            if (push.Count > 0 && push[0].KnockbackRow is { } row)
                effects.Add(ActionEffectEntry.Knockback(row));
            effects.Add(ActionEffectEntry.ActionMarker(id));
            targets.Add(new ActionEffectTarget(who.GameObjectId, effects));
        }
        delivery.Sequence = cast.NativeActionEffect(
            id, action.Cast.AnimationLock, (ushort)id, animationVariation, ActionType.Action, 0,
            rotation: caster.Rotation + action.Cast.Rotation, position: aim ?? caster.Position, animationTargetId: castTarget, targets: targets);
        delivery.Confirmations = confirmations;
        if (delivery.ImpactDue) ConfirmDamage(delivery);
    }

    private void Resolve(EnemyAction action, SimCharacter? target, Vector3? location, EnemyActionCast handle, Delivery delivery)
    {
        var party = world.Party;
        var origin = target is not null && target != caster && IsDirectional(action)
            ? caster.Placement().Face(target.Position)
            : target?.Placement()
              ?? (location is { } at ? new Placement(at, caster.Rotation) : caster.Placement());
        var ctx = new EnemyActionContext(action, caster, target, origin, party);

        var hits = IsSingleTarget(action)
            ? party.ActiveMembers().Where(m => ReferenceEquals(m, target)).ToList()
            : AreaHits(action, origin, origin.Position == caster.Position ? caster.HitboxRadius : 0f, party);
        if (action.Area.ExcludeCaster
            && hits.MinBy(h => DistanceXZ(h.Position, caster.Position)) is { } baiter
            && DistanceXZ(baiter.Position, caster.Position) <= ExcludeCasterThreshold)
            hits = hits.Where(h => h != baiter).ToList();
        if (action.Area.AdjustTargets is { } adjust) hits = adjust(ctx, hits);
        ctx.Hits = hits;
        handle.Resolved(origin, hits.Select(h => (h, h.Position)).ToList());
        DiagnosticLog.Info(
            $"[EnemyAction] Resolve: {ActionLookup.Name(action.ActionId)} at ({origin.Position.X:F1},{origin.Position.Z:F1}) rot={origin.Rotation:F3} -- {hits.Count} target(s): "
            + string.Join(", ", hits.Select(t => (t as ISimPartyMember)?.Role.ToString() ?? "?")));

        foreach (var effect in action.Effects)
            effect.Apply(ctx);
        handle.Killed(ctx.Killed.Keys.ToList());

        delivery.Damage = ctx.DamageShown;
        delivery.Pushes = ctx.Knockbacks.Select(k => (k.Target, k.KnockbackRow)).ToList();
        // Ahead of the deaths it shares a time with, so the killing hit lands on a living target.
        if (ctx.DamageShown.Count > 0)
            Schedule(action.Timing.DamageDelay, () => ConfirmDamage(delivery));
        foreach (var (who, from, distance, speed, delay, row) in ctx.Knockbacks)
        {
            var byClient = who is SimPlayer && row is { } r && KnockbackLookup.ClientApplies(r);
            Schedule(delay ?? action.Timing.DamageDelay, () =>
            {
                if (!who.IsAlive()) return;
                if (byClient && who is SimPlayer player) player.HoldForClientKnockback(distance / speed);
                else if (who is ISimPartyMember member) member.Knockback(from, distance, speed);
            });
        }
        foreach (var (who, explanation) in ctx.Killed)
            Schedule(action.Timing.DamageDelay, () => who.Die(action.ActionId, Explain(action.DeathExplanation, explanation)));
        foreach (var run in ctx.AfterResolveActions)
            run();
    }

    private const float ExcludeCasterThreshold = 0.01f;

    private static IReadOnlyList<SimCharacter> AreaHits(EnemyAction action, Placement origin, float casterHitboxRadius, SimParty party)
    {
        var query = new AoeQuery(action.ActionId, origin, action.Cast.Rotation, action.Area.Size, action.Area.CastTypeOverride, casterHitboxRadius);
#if DEBUG
        AnoMech.Windows.DamageDebugWindow.Instance?.Record(query);
#endif
        return query.Run(party.Find);
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
        => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

    // No area, only the cast target is hit.
    private static bool IsSingleTarget(EnemyAction action)
        => (action.Area.CastTypeOverride ?? Natives.Data.Action(action.ActionId)?.CastType) is CastType.SingleTarget;

    // Cones and lines.
    private static bool IsDirectional(EnemyAction action)
        => (action.Area.CastTypeOverride ?? Natives.Data.Action(action.ActionId)?.CastType)
            is CastType.Cone2 or CastType.Cone or CastType.Rectangle2 or CastType.Rectangle or CastType.Charge;

    private static string? Explain(string? action, string? hit)
        => action is null ? hit : hit is null ? action : $"{action}; {hit}";

    // The EffectResult that makes the client apply a target's damage entry, flytext included. An
    // impact due before the ActionEffect went out waits for it.
    private static void ConfirmDamage(Delivery delivery)
    {
        delivery.ImpactDue = delivery.Sequence == 0;
        if (delivery.ImpactDue) return;
        foreach (var (who, index) in delivery.Confirmations)
            if (who.Proxy is { Exists: true } chara)
                chara.ReceiveEffectResult(delivery.Sequence, index, chara.Health);
    }

    // Resolve's damage, for the ActionEffect; the ActionEffect's ids, for the EffectResults.
    private sealed class Delivery
    {
        public IReadOnlyList<(SimCharacter Target, uint Amount, DamageKind Kind)> Damage = [];
        public IReadOnlyList<(SimCharacter Target, uint? KnockbackRow)> Pushes = [];
        public uint Sequence;
        public IReadOnlyList<(SimCharacter Target, byte Index)> Confirmations = [];
        public bool ImpactDue;
    }

    // Anything due now runs inline: an instant action resolves inside the Cast() call, so casts
    // issued in one timeline event see each other's statuses in call order.
    private void Schedule(float delay, Action run)
    {
        if (delay <= 0f) run();
        else world.Events.Add(delay, run);
    }
}
