using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.EnemyActions;

// The DamageSolver death rules, with vulnerabilities read from the DamageSpec instead of a
// per-scenario registry.
internal static class DamageCheck
{
    // Null when the hit is survivable, otherwise why it kills ("" when it simply does).
    public static string? LethalCause(SimCharacter target, DamageSpec spec, Severity severity, SimParty party)
    {
        if (spec.Protections.Any(id => target.FindStatus(id) != null)) return null;
        if (severity.Kind == SeverityKind.Lethal) return MissingProtection(spec) ?? "";
        var vulnMitigation = VulnRequiredMitigation(target, spec);
        if (vulnMitigation >= 1f) return "had vuln up debuff";
        if (severity.Kind == SeverityKind.TankBuster && !IsTank(target)) return "tank buster";
        if (Survives(target, party, MathF.Max(severity.MinMitigation, vulnMitigation ?? 0f), spec.Kind)) return null;
        return vulnMitigation != null ? "not enough mitigation for a hit with vuln up" : "not enough mitigation";
    }

    private static string? MissingProtection(DamageSpec spec)
        => spec.Protections.Count > 0 ? $"no {StatusLookup.Name(spec.Protections[0])}" : null;

    public static bool IsTank(SimCharacter target) => target is ISimPartyMember { Role: PartyRole.OffTank or PartyRole.MainTank };

    private static float? VulnRequiredMitigation(SimCharacter target, DamageSpec spec)
    {
        foreach (var vuln in spec.Vulnerabilities)
        {
            if (target.FindStatus(vuln.StatusId) is not { } status || status.Stacks < vuln.MinStacks) continue;
            DiagnosticLog.Info($"[EnemyAction] {(target as ISimPartyMember)?.Role} is hit carrying vuln up {vuln.StatusId}: needs {vuln.RequiredMitigation:P0} mitigation.");
            return vuln.RequiredMitigation;
        }
        return null;
    }

    // Only a human's own statuses are ever checked: a bot always passes. Spends the target's shields.
    private static bool Survives(SimCharacter target, SimParty party, float requiredMitigation, DamageKind kind)
    {
        if (requiredMitigation <= 0f || !ChecksMitigation(target, party)) return true;
        var effective = Mitigation.Effective(target.ActiveStatusSnapshot.Select(s => s.StatusId), kind);
        Mitigation.SpendShields(target);
        var survives = effective >= requiredMitigation - 0.0005f;
        DiagnosticLog.Info($"[EnemyAction] Mitigation check: {(target as ISimPartyMember)?.Role} has {effective:P1} {kind}, needs {requiredMitigation:P1} -- {(survives ? "survives" : "dies")}.");
        return survives;
    }

    private static bool ChecksMitigation(SimCharacter target, SimParty party)
        => Plugin.Config.EnableTankMitigation && Natives.UserActions.Enabled
           && target is ISimPartyMember && !party.IsBotDriven(target);
}
