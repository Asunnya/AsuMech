namespace AnoMech.Core.Native.Interfaces;

// Direction: 0 away from the source, 1 the DirectionArg angle, 2 random, 3/4/5 the source's
// forward/right/left, 6 away from the action's target.
public sealed record KnockbackRow(uint Id, float Distance, float Speed, byte Direction);
