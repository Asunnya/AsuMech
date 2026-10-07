using System;

namespace AnoMech.Core.EnemyActions;

public enum DamageType
{
    [Obsolete("A hit's size is its Severity: Damage(spec, Severity.Lethal).")]
    Lethal,
    [Obsolete("Declare the vuln on each type it applies to.")]
    Any,
    Magic,
    [Obsolete("A hit's size is its Severity: Damage(spec, Severity.TankBuster).")]
    TankBuster,
    Fire,
    Ice,
    Lightning,
    Earth,
    Wind,
    Black,
    White,
    Physical,
    Unique,
}
