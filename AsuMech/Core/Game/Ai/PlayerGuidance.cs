using System;
using System.Numerics;
using System.Reflection;
using AsuMech.Core.Game.Party;

namespace AsuMech.Core.Game.Ai;

// Which strat call issued an order: the strat class and the time literal passed to AiManager.Move,
// with the source line as a tiebreaker and the position method's name when the call used a method
// group. Lambdas get no method name; the compiler's name says nothing.
public readonly record struct GuidanceKey(string StratClass, float Time, string Source, string? Method)
{
    public static GuidanceKey Of(Delegate positions, float time, string source)
    {
        var method = positions.Method;
        var type = method.DeclaringType;
        while (type is { IsNested: true } nested && nested.Name.StartsWith('<')) type = nested.DeclaringType;
        var name = method.Name.StartsWith('<') ? null : method.Name;
        return new GuidanceKey(type?.Name ?? "?", time, source, name);
    }
}

// A move the strat asked of the player's slot: where, from when, and by when (absolute scenario
// seconds; null when the strat set no deadline). Target is the exact spot, without bot jitter.
public sealed record GuidanceOrder(GuidanceKey Key, PartyRole Role, Vector3 Target, float IssuedAt, float? ArrivalTime, bool Sprint);

public interface IPlayerGuidanceSink
{
    void OnMove(GuidanceOrder order);
    // A mechanic is about to move the player's character (knockback, pull, carry).
    void OnForcedMove() { }
}
