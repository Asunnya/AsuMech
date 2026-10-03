using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.EnemyActions;

// RequiredMitigation 1 = lethal short of an invuln.
public readonly record struct Vulnerability(ushort StatusId, float RequiredMitigation = 1f, int MinStacks = 1);

public sealed record DamageSpec(params DamageType[] Types)
{
    public IReadOnlyList<Vulnerability> Vulnerabilities { get; init; } = [];
    public float RequiredMitigation { get; init; }
    public float LethalWithin { get; init; }
    // Flytext only, never compared to HP. Null shows the default for the hit's type; a hit that
    // kills always shows 120,000.
    public uint? FlyText { get; init; }

    public uint FlyTextAmount(bool kills)
        => kills ? 120_000u : FlyText ?? (Has(DamageType.TankBuster) ? 80_000u : 40_000u);

    public bool Has(DamageType type) => Types.Contains(type);

    public DamageKind Kind => Has(DamageType.Magic) ? DamageKind.Magic : DamageKind.Physical;

    public FlyTextIcon Icon => Kind == DamageKind.Magic ? FlyTextIcon.Magic : FlyTextIcon.Physical;

    public DamageSpec VulnerableTo(ushort statusId, float requiredMitigation = 1f, int minStacks = 1)
        => this with { Vulnerabilities = [.. Vulnerabilities, new Vulnerability(statusId, requiredMitigation, minStacks)] };

    public static implicit operator DamageSpec(DamageType type) => new(type);
}

public static class DamageTypeExtensions
{
    public static DamageSpec VulnerableTo(this DamageType type, ushort statusId, float requiredMitigation = 1f, int minStacks = 1)
        => new DamageSpec(type).VulnerableTo(statusId, requiredMitigation, minStacks);
}
