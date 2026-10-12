namespace AnoMech.Core.EnemyActions;

// Unique also covers darkness and typeless hits: the game cuts them only by mitigation that reduces
// all damage, and shows them with the same flytext icon.
public enum DamageKind { Physical, Magic, Unique }

public static class DamageKinds
{
    // From the Action sheet's AttackType: 1-4 slashing/piercing/blunt/shot, 5 magic, 6 darkness.
    public static DamageKind FromAttackType(sbyte attackType) => attackType switch
    {
        >= 1 and <= 4 => DamageKind.Physical,
        5 => DamageKind.Magic,
        _ => DamageKind.Unique,
    };

    // The flytext icon follows it. UNVERIFIED that darkness shows the unique icon; retail also
    // sends typeless hits as 0.
    public static byte AttackType(this DamageKind kind) => kind switch
    {
        DamageKind.Physical => 1,
        DamageKind.Magic => 5,
        _ => 6,
    };
}
