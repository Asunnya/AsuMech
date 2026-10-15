namespace AnoMech.Core.EnemyActions;

// Unique also covers darkness and typeless hits: the game cuts them only by mitigation that reduces
// all damage, and shows them with the same flytext icon. Typeless is the same for mitigation, but
// goes out with attack type 0, as retail sends a hit with no type at all (White Hole).
public enum DamageKind { Physical, Magic, Unique, Typeless }

public static class DamageKinds
{
    public static DamageKind FromAttackType(sbyte attackType) => attackType switch
    {
        >= 1 and <= 4 => DamageKind.Physical,
        5 => DamageKind.Magic,
        _ => DamageKind.Unique,
    };

    public static byte AttackType(this DamageKind kind) => kind switch
    {
        DamageKind.Physical => 1,
        DamageKind.Magic => 5,
        DamageKind.Typeless => 0,
        _ => 6,
    };
}
