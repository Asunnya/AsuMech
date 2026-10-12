using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// One resolve of one cast, shared by its effects in list order.
public sealed class EnemyActionContext
{
    private readonly Dictionary<SimCharacter, string?> killed = [];
    private readonly List<(SimCharacter Target, uint Amount, DamageKind Kind)> damageShown = [];
    private readonly List<(SimCharacter Target, Vector3 From, float Distance, float Speed, float? Delay, uint? KnockbackRow)> knockbacks = [];
    private readonly List<System.Action> afterResolve = [];

    internal EnemyActionContext(EnemyAction action, SimEnemy caster, SimCharacter? target, Placement origin, SimParty party)
    {
        Action = action;
        Caster = caster;
        Target = target;
        Origin = origin;
        Party = party;
    }

    public EnemyAction Action { get; }
    public SimEnemy Caster { get; }
    public SimCharacter? Target { get; }
    public Placement Origin { get; }
    public SimParty Party { get; }
    // Nearest to Origin first.
    public IReadOnlyList<SimCharacter> Hits { get; internal set; } = [];

    // Decided now; the character dies to this action after TimingSpec.DamageDelay. The first kill wins.
    public void Kill(SimCharacter who, string? explanation = null) => killed.TryAdd(who, explanation);
    public bool IsKilled(SimCharacter who) => killed.ContainsKey(who);
    public void ShowDamage(SimCharacter who, uint amount, DamageKind kind) => damageShown.Add((who, amount, kind));
    // Decided now; moves the character after `delay` (null = TimingSpec.DamageDelay), unless it died meanwhile.
    // knockbackRow goes into the ActionEffect; null for a push with no Knockback sheet row.
    public void Knockback(SimCharacter who, Vector3 from, float distance, float speed, float? delay = null, uint? knockbackRow = null)
        => knockbacks.Add((who, from, distance, speed, delay, knockbackRow));

    // Runs once every effect has applied and the deaths due now have been dealt, so a follow-up
    // cast can't claim this action's kills.
    public void AfterResolve(System.Action run) => afterResolve.Add(run);

    internal IReadOnlyDictionary<SimCharacter, string?> Killed => killed;
    internal IReadOnlyList<(SimCharacter Target, uint Amount, DamageKind Kind)> DamageShown => damageShown;    internal IReadOnlyList<(SimCharacter Target, Vector3 From, float Distance, float Speed, float? Delay, uint? KnockbackRow)> Knockbacks => knockbacks;
    internal IReadOnlyList<System.Action> AfterResolveActions => afterResolve;
}
