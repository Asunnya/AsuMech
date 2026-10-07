using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// What a SimEnemy.Cast aims at: a character, a scenario-local ground location, or neither
// (default, or a null character), which centres the cast on the caster.
public readonly struct CastTarget
{
    public SimCharacter? Character { get; }
    public Vector3? Location { get; }

    private CastTarget(SimCharacter? character, Vector3? location)
    {
        Character = character;
        Location = location;
    }

    public static implicit operator CastTarget(SimCharacter? character) => new(character, null);
    public static implicit operator CastTarget(Vector3 location) => new(null, location);
}
