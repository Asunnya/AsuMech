using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

// Fire left on the floor: an armed patch gives the burn status and kills after burnToDeath seconds
// spent in it; stepping out resets the clock.
public sealed class BurningGround(ushort burnStatus, float burnToDeath, string deathExplanation)
{
    private readonly List<Patch> patches = [];
    private readonly Dictionary<SimCharacter, float> exposure = [];

    private abstract record Patch(uint ActionId, float ArmedAt)
    {
        public abstract bool Contains(Vector2 p);
    }

    private sealed record Strip(uint ActionId, float ArmedAt, Vector2 Start, Vector2 Direction, float Length, float HalfWidth) : Patch(ActionId, ArmedAt)
    {
        public override bool Contains(Vector2 p)
        {
            var d = p - Start;
            var along = Vector2.Dot(d, Direction);
            var across = MathF.Abs(d.X * Direction.Y - d.Y * Direction.X);
            return along >= 0f && along <= Length && across <= HalfWidth;
        }
    }

    private sealed record Circle(uint ActionId, float ArmedAt, Vector2 Centre, float Radius) : Patch(ActionId, ArmedAt)
    {
        public override bool Contains(Vector2 p) => Vector2.Distance(p, Centre) <= Radius;
    }

    private sealed record Slice(uint ActionId, float ArmedAt, Vector2 Origin, Vector2 Direction, float HalfAngle) : Patch(ActionId, ArmedAt)
    {
        public override bool Contains(Vector2 p)
        {
            var d = p - Origin;
            if (d.LengthSquared() < 0.01f) return false;
            return Vector2.Dot(Vector2.Normalize(d), Direction) >= MathF.Cos(HalfAngle);
        }
    }

    public void AddStrip(uint actionId, float armedAt, Vector3 start, Vector2 direction, float length, float halfWidth)
        => patches.Add(new Strip(actionId, armedAt, Flat(start), direction, length, halfWidth));

    public void AddCircle(uint actionId, float armedAt, Vector3 centre, float radius)
        => patches.Add(new Circle(actionId, armedAt, Flat(centre), radius));

    public void AddSlice(uint actionId, float armedAt, Vector3 origin, Vector2 direction, float halfAngle)
        => patches.Add(new Slice(actionId, armedAt, Flat(origin), direction, halfAngle));

    public void Tick(SimParty party, float delta, float elapsed)
    {
        if (patches.Count == 0) return;
        foreach (var member in party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            var at = Flat(member.Position);
            var patch = patches.FirstOrDefault(p => elapsed >= p.ArmedAt && p.Contains(at));
            if (patch == null)
            {
                exposure.Remove(member);
                continue;
            }
            var burnt = exposure.GetValueOrDefault(member) + delta;
            exposure[member] = burnt;
            if (!member.HasStatus(burnStatus)) member.AddStatus(burnStatus);
            if (burnt >= burnToDeath) member.Die(patch.ActionId, deathExplanation);
        }
    }

    public void PutOut(SimParty party)
    {
        patches.Clear();
        exposure.Clear();
        foreach (var member in party.ActiveMembers()) member.RemoveStatus(burnStatus);
    }

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
