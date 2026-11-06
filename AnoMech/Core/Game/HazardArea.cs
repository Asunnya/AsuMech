using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

// One enemy action in flight whose area will be checked at ResolveAt: the same origin, shape and
// query the handler resolves with, exposed read-only so an overlay can draw what is about to hit
// and a report can say why it did. Origin is read live, as the resolve will read it, so a caster
// turning during its bar turns the hazard with it.
public sealed class HazardArea
{
    private readonly SimCharacter? target;
    private readonly Vector3? location;

    internal HazardArea(EnemyAction action, SimCharacter caster, SimCharacter? target, Vector3? location, float resolveAt)
    {
        Action = action;
        Caster = caster;
        this.target = target;
        this.location = location;
        ResolveAt = resolveAt;
    }

    public EnemyAction Action { get; }
    public SimCharacter Caster { get; }
    // Scenario clock.
    public float ResolveAt { get; }

    public string Name => Natives.Data.ActionName(Action.ActionId);

    public Placement Origin => OriginOf(Action, Caster, target, location);

    public AoeQuery Query()
    {
        var origin = Origin;
        return new AoeQuery(Action.ActionId, origin, Action.Cast.Rotation + Action.Area.Rotation, Action.Area.Size, Action.Area.CastTypeOverride,
            origin.Position == Caster.Position ? Caster.HitboxRadius : 0f);
    }

    public CastType? CastType => Action.Area.CastTypeOverride ?? Natives.Data.Action(Action.ActionId)?.CastType;

    public string ShapeWord => ShapeWordOf(CastType);

    // Negative inside, in yalms; null when the shape is unknown.
    public float? SignedDistance(Vector3 localPosition) => Query().SignedDistance(localPosition);

    // The area's edges as closed loops in scenario coordinates, for drawing; empty for a shape
    // the hit test doesn't know either.
    public IReadOnlyList<IReadOnlyList<Vector3>> Outline(int segments = 48) => OutlineOf(Query(), segments);

    // Where the resolve measures from: a cone or line aimed at someone turns the caster to them, a
    // targeted action starts at the target, a ground action at its point, anything else at the caster.
    internal static Placement OriginOf(EnemyAction action, SimCharacter caster, SimCharacter? target, Vector3? location)
        => target is not null && target != caster && IsDirectional(action)
            ? caster.Placement().Face(target.Position)
            : target?.Placement() ?? (location is { } at ? new Placement(at, caster.Rotation) : caster.Placement());

    internal static bool IsSingleTarget(EnemyAction action)
        => (action.Area.CastTypeOverride ?? Natives.Data.Action(action.ActionId)?.CastType) is Native.Interfaces.CastType.SingleTarget;

    internal static bool IsDirectional(EnemyAction action)
        => (action.Area.CastTypeOverride ?? Natives.Data.Action(action.ActionId)?.CastType)
            is Native.Interfaces.CastType.Cone2 or Native.Interfaces.CastType.Cone or Native.Interfaces.CastType.Rectangle2
            or Native.Interfaces.CastType.Rectangle or Native.Interfaces.CastType.Charge;

    public static string ShapeWordOf(CastType? type) => type switch
    {
        Native.Interfaces.CastType.Circle or Native.Interfaces.CastType.Circle2 or Native.Interfaces.CastType.GroundCircle or Native.Interfaces.CastType.Custom => "circle",
        Native.Interfaces.CastType.Cone or Native.Interfaces.CastType.Cone2 => "cone",
        Native.Interfaces.CastType.Rectangle or Native.Interfaces.CastType.Rectangle2 or Native.Interfaces.CastType.Charge => "line",
        Native.Interfaces.CastType.Donut => "donut",
        Native.Interfaces.CastType.Cross => "cross",
        Native.Interfaces.CastType.SingleTarget => "target",
        _ => "area",
    };

    // Mirrors AoeQuery.SignedDistance's parameters shape for shape.
    public static IReadOnlyList<IReadOnlyList<Vector3>> OutlineOf(AoeQuery query, int segments = 48)
    {
        if (Natives.Data.Action(query.ActionId) is not { } action) return [];
        var castType = query.CastTypeOverride ?? action.CastType;
        var halfWidth = action.XAxisModifier > 0 ? action.XAxisModifier * 0.5f : action.EffectRange;
        var range = action.EffectRange + (castType.AddsCasterHitbox() ? query.CasterHitboxRadius : 0f);
        var rotation = query.Source.Rotation + query.OmenRotate;
        var origin = query.Source.Position;
        var fwd = new Vector3(MathF.Sin(rotation), 0f, MathF.Cos(rotation));
        var side = new Vector3(MathF.Cos(rotation), 0f, -MathF.Sin(rotation));
        switch (castType)
        {
            case Native.Interfaces.CastType.Circle or Native.Interfaces.CastType.Circle2 or Native.Interfaces.CastType.Custom or Native.Interfaces.CastType.GroundCircle:
                return [Circle(origin, range, segments)];
            case Native.Interfaces.CastType.Cone or Native.Interfaces.CastType.Cone2:
            {
                var half = query.Size ?? MathF.PI / 6f;
                var points = new List<Vector3> { origin };
                var arc = Math.Max(2, (int)(segments * half / MathF.PI));
                for (var i = 0; i <= arc; i++)
                {
                    var a = rotation - half + 2f * half * i / arc;
                    points.Add(origin + new Vector3(MathF.Sin(a), 0f, MathF.Cos(a)) * range);
                }
                return [points];
            }
            case Native.Interfaces.CastType.Charge:
            {
                var length = query.Size ?? 100f;
                return [Box(origin, fwd, side, 0f, length, halfWidth)];
            }
            case Native.Interfaces.CastType.Rectangle or Native.Interfaces.CastType.Rectangle2:
                return [Box(origin, fwd, side, 0f, range, halfWidth)];
            case Native.Interfaces.CastType.Donut:
                return [Circle(origin, range, segments), Circle(origin, query.Size ?? 0f, segments)];
            case Native.Interfaces.CastType.Cross:
                return [Box(origin, fwd, side, -range, range, halfWidth), Box(origin, side, fwd, -range, range, halfWidth)];
            default:
                return [];
        }
    }

    private static List<Vector3> Circle(Vector3 center, float radius, int segments)
    {
        var points = new List<Vector3>(segments);
        for (var i = 0; i < segments; i++)
        {
            var a = MathF.Tau * i / segments;
            points.Add(center + new Vector3(MathF.Sin(a), 0f, MathF.Cos(a)) * radius);
        }
        return points;
    }

    private static List<Vector3> Box(Vector3 origin, Vector3 along, Vector3 across, float from, float to, float halfWidth)
        =>
        [
            origin + along * from - across * halfWidth,
            origin + along * to - across * halfWidth,
            origin + along * to + across * halfWidth,
            origin + along * from + across * halfWidth,
        ];
}

// Why a hit landed, in the words the banner and the report show.
public static class HitExplanation
{
    // "Sweeping Victory: you were 2.0 yalms inside the cone" from the resolve's own origin and
    // the player's position at the snapshot.
    public static string Phrase(ActionResolved resolved, SimCharacter who)
    {
        var query = new AoeQuery(resolved.Action.ActionId, resolved.Origin, resolved.Action.Cast.Rotation + resolved.Action.Area.Rotation,
            resolved.Action.Area.Size, resolved.Action.Area.CastTypeOverride, resolved.Origin.Position == resolved.Caster.Position ? resolved.Caster.HitboxRadius : 0f);
        return Phrase(Natives.Data.ActionName(resolved.Action.ActionId), query, who.Position);
    }

    public static string Phrase(string actionName, AoeQuery query, Vector3 position)
    {
        var shape = HazardArea.ShapeWordOf(query.CastTypeOverride ?? Natives.Data.Action(query.ActionId)?.CastType);
        var inside = query.SignedDistance(position);
        if (shape == "target") return $"{actionName}: it was aimed at you";
        if (inside is not { } d) return $"{actionName}: you were in its area";
        var depth = MathF.Max(0f, -d).ToString("0.0", CultureInfo.InvariantCulture);
        return d <= 0f ? $"{actionName}: you were {depth} yalms inside the {shape}" : $"{actionName}: you were caught at the {shape}'s edge";
    }
}
