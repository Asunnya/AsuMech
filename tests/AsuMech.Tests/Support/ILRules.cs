using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AsuMech.Tests;

// Shared checks for the IL rules: each returns what broke, empty when the rule holds.
internal static class ILRules
{
    public static void AssertNone(IEnumerable<string> failures)
    {
        var list = failures.ToList();
        Assert.That(list, Is.Empty, string.Join("\n", list));
    }

    // `required`: a rule over a member nothing calls any more has gone stale.
    public static IEnumerable<string> CallersMustBeIn(string callee, bool required, params string[] allowed)
    {
        var callers = PluginIL.Build.CallersOf($"^{Regex.Escape(callee)}$");
        if (required && callers.Count == 0) yield return $"nothing calls {callee} any more; if it was renamed, update this rule.";
        foreach (var c in callers.Where(c => !allowed.Contains(c)))
            yield return $"{c} calls {callee}; only {string.Join(", ", allowed)} may.";
    }

    // Every path from the method's entry to each `target` call takes the wanted branch of a
    // `guard` (a call or field load whose bool or null-ness is branched on). Paths into a
    // `noReturn` call end there.
    public static IEnumerable<string> EveryPathPasses(MethodDefinition method, Func<Instruction, bool> guard, string guardName, bool mustBeTruthy,
        Func<Instruction, bool> target, string targetName, params string[] noReturn)
    {
        var where = PluginIL.Owner(method);
        var guards = method.Body.Instructions.Where(guard).ToList();
        var targets = method.Body.Instructions.Where(target).ToList();
        if (guards.Count == 0)
        {
            yield return $"{where}: no {guardName} found; update this rule if the check moved.";
            yield break;
        }
        if (targets.Count == 0)
        {
            yield return $"{where}: no {targetName} found; update this rule if it moved.";
            yield break;
        }
        var wanted = new List<(Instruction, Instruction)>();
        foreach (var g in guards)
        {
            var outcome = ControlFlow.OutcomeOf(g, out var joined);
            var edge = outcome is { } o ? (o.Branch, mustBeTruthy ? o.WhenTruthy : o.WhenFalsy) : default;
            // An arm that brings a constant through the wanted edge would pass without the check.
            if (outcome == null || joined.Any(c => ControlFlow.Taken(edge.Branch, c) == edge.Item2))
            {
                yield return $"{where}: {guardName} (IL_{g.Offset:X4}) is not branched on in a shape this rule reads; keep it a plain if-check.";
                yield break;
            }
            wanted.Add(edge);
        }
        var reached = new ControlFlow(method, noReturn).FeasiblyReachableAvoiding(wanted);
        foreach (var t in targets.Where(reached.Contains))
            yield return $"{where}: {targetName} (IL_{t.Offset:X4}) is reachable without {guardName} having come out {(mustBeTruthy ? "true / non-null" : "false / null")}.";
    }

    public static Func<Instruction, bool> CallTo(string name) => i => PluginIL.Calls(i, name);

    public static Func<Instruction, bool> LoadOf(string field) => i => i.OpCode.Code is Code.Ldfld or Code.Ldsfld && i.Operand is FieldReference f && PluginIL.Name(f) == field;
}
