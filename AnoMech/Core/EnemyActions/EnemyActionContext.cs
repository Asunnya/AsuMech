using System.Collections.Generic;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// One resolve of one cast, shared by its effects in list order. What it decides for each target
// becomes that target's entries in the ActionEffect.
public sealed class EnemyActionContext
{
    // Never removed from, so it enumerates in insertion order.
    private readonly Dictionary<SimCharacter, TargetOutcome> outcomes = [];
    private readonly List<System.Action> afterResolve = [];
    private readonly List<SimStatus> hiddenStatuses = [];

    internal EnemyActionContext(EnemyAction action, SimCharacter caster, SimCharacter? target, Placement origin, SimParty party)
    {
        Action = action;
        Caster = caster;
        Target = target;
        Origin = origin;
        Party = party;
    }

    public EnemyAction Action { get; }
    // An enemy, or a party member using an action through SimParty.Act.
    public SimCharacter Caster { get; }
    public SimCharacter? Target { get; }
    public Placement Origin { get; }
    public SimParty Party { get; }
    // Everyone the area caught, nearest to Origin first.
    public IReadOnlyList<SimCharacter> Hits { get; internal set; } = [];

    // The NPCs the area caught, the caster aside; effects reach them only through OnNpcs.
    public IReadOnlyList<SimEnemy> NpcHits { get; internal set; } = [];

    // Decided now; the character dies to this action after TimingSpec.DamageDelay. The first kill wins.
    public void Kill(SimCharacter who, string? explanation = null)
    {
        var outcome = Outcome(who);
        if (outcome.Killed) return;
        outcome.Killed = true;
        outcome.Explanation = explanation;
    }

    public bool IsKilled(SimCharacter who) => outcomes.TryGetValue(who, out var outcome) && outcome.Killed;

    // The largest hit is the one shown.
    public void ShowDamage(SimCharacter who, uint amount, DamageKind kind)
    {
        var outcome = Outcome(who);
        if (outcome.Damage is not { } shown || amount > shown.Amount) outcome.Damage = (amount, kind);
    }

    // Decided now; moves the character after `delay` (null = TimingSpec.DamageDelay), unless it died
    // meanwhile. knockbackRow goes into the ActionEffect; null for a push with no Knockback sheet
    // row. The first push wins.
    public void Knockback(SimCharacter who, float distance, float speed, float? delay = null, uint? knockbackRow = null)
    {
        var awayFromTarget = knockbackRow is { } row && Natives.Data.Knockback(row) is { AwayFromTarget: true } && Target is not null;
        Outcome(who).Push ??= new Push(awayFromTarget ? Target!.Position : Caster.Position, distance, speed, delay, knockbackRow);
    }

    // Shown as a dodge, for one who escaped the action, e.g. by facing a gaze correctly.
    public void Dodge(SimCharacter who) => Outcome(who).Dodged = true;

    // Applied now, so effects after this one see it, and shown to the client, from the caster, with
    // the ActionEffect. delayToEffectResult: shown only with the EffectResult at TimingSpec.DamageDelay,
    // its full duration running from then. entryParam goes in the ActionEffect's gain entry.
    public void AddStatus(SimCharacter who, ushort statusId, float duration = 0f, int stacks = 1, bool delayToEffectResult = false,
        byte entryParam = 0)
    {
        var delay = delayToEffectResult && duration > 0f ? Action.Timing.DamageDelay : 0f;
        if (who.AddStatus(statusId, duration + delay, stacks, broadcastAddEvent: false, hidden: delayToEffectResult) is not { Slot: >= 0 } status) return;
        status.ShownAsFrom(Caster);
        Outcome(who).StatusGains.Add((statusId, entryParam));
        if (status.Hidden) hiddenStatuses.Add(status);
    }

    // Runs once every effect has applied and the deaths due now have been dealt, so a follow-up
    // cast can't claim this action's kills.
    public void AfterResolve(System.Action run) => afterResolve.Add(run);

    internal IReadOnlyCollection<TargetOutcome> Outcomes => outcomes.Values;
    internal IReadOnlyList<System.Action> AfterResolveActions => afterResolve;
    internal IReadOnlyList<SimStatus> HiddenStatuses => hiddenStatuses;

    private TargetOutcome Outcome(SimCharacter who)
    {
        if (outcomes.TryGetValue(who, out var outcome)) return outcome;
        outcome = new TargetOutcome(who);
        outcomes.Add(who, outcome);
        return outcome;
    }

    internal sealed class TargetOutcome(SimCharacter target)
    {
        public SimCharacter Target { get; } = target;
        public (uint Amount, DamageKind Kind)? Damage { get; set; }
        public bool Killed { get; set; }
        public bool Dodged { get; set; }
        public string? Explanation { get; set; }
        public Push? Push { get; set; }
        public List<(ushort StatusId, byte EntryParam)> StatusGains { get; } = [];
    }

    internal sealed record Push(System.Numerics.Vector3 From, float Distance, float Speed, float? Delay, uint? KnockbackRow);
}
