using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AsuMech.Tests;

// Instruction-level control flow of one method, edges into exception handlers included, for
// proving every path to a call passes a check first. Calls named in noReturn end their path.
internal sealed class ControlFlow
{
    private readonly MethodDefinition method;
    private readonly Dictionary<Instruction, List<Instruction>> successors = new();

    public ControlFlow(MethodDefinition method, params string[] noReturn)
    {
        this.method = method;
        foreach (var i in method.Body.Instructions)
        {
            var next = new List<Instruction>();
            var endsPath = i.Operand is MethodReference callee && noReturn.Contains(PluginIL.Name(callee));
            if (!endsPath)
            {
                switch (i.OpCode.FlowControl)
                {
                    case FlowControl.Branch:
                        next.Add((Instruction)i.Operand);
                        break;
                    case FlowControl.Cond_Branch:
                        if (i.Operand is Instruction[] targets) next.AddRange(targets);
                        else next.Add((Instruction)i.Operand);
                        if (i.Next != null) next.Add(i.Next);
                        break;
                    case FlowControl.Return:
                    case FlowControl.Throw:
                        break;
                    default:
                        if (i.OpCode.Code is Code.Endfinally or Code.Endfilter) break;
                        if (i.Next != null) next.Add(i.Next);
                        break;
                }
            }
            foreach (var h in method.Body.ExceptionHandlers)
                if (Inside(i, h.TryStart, h.TryEnd))
                {
                    next.Add(h.HandlerStart);
                    if (h.FilterStart != null) next.Add(h.FilterStart);
                }
            successors[i] = next;
        }
    }

    private static bool Inside(Instruction i, Instruction start, Instruction? end)
        => i.Offset >= start.Offset && (end == null || i.Offset < end.Offset);

    public HashSet<Instruction> Reachable() => FeasiblyReachableAvoiding([]);

    // Everything a path from the entry can reach without taking any of `avoided`. A branch on a
    // constant the path itself put there (Debug builds park `a && b` in a local: ldc.i4.0, stloc,
    // ldloc, brfalse) only goes the way that constant sends it.
    public HashSet<Instruction> FeasiblyReachableAvoiding(ICollection<(Instruction From, Instruction To)> avoided)
    {
        var reached = new HashSet<Instruction>();
        var seen = new HashSet<(Instruction, int?, int?, int?)>();
        var work = new Stack<(Instruction At, int? Top, int? Local, int? LocalValue)>();
        work.Push((method.Body.Instructions[0], null, null, null));
        while (work.Count > 0)
        {
            var state = work.Pop();
            if (!seen.Add(state)) continue;
            var (i, top, local, localValue) = state;
            reached.Add(i);
            int? nextTop = null;
            var nextLocal = local;
            var nextLocalValue = localValue;
            if (Constant(i) is { } c) nextTop = c;
            else if (i.OpCode.Code is Code.Nop or Code.Br or Code.Br_S or Code.Dup) nextTop = top;
            else if (LocalIndex(i) is { } index && IsStore(i))
                (nextLocal, nextLocalValue) = top is { } value ? (index, value) : index == local ? (null, null) : (local, localValue);
            else if (LocalIndex(i) is { } loaded && loaded == local) nextTop = localValue;
            var conditional = i.OpCode.Code is Code.Brtrue or Code.Brtrue_S or Code.Brfalse or Code.Brfalse_S;
            foreach (var n in successors[i])
            {
                if (avoided.Contains((i, n))) continue;
                if (conditional && top is { } condition && (Instruction)i.Operand != i.Next)
                {
                    var jumps = i.OpCode.Code is Code.Brtrue or Code.Brtrue_S ? condition != 0 : condition == 0;
                    if (jumps != (n == (Instruction)i.Operand)) continue;
                }
                work.Push((n, nextTop, nextLocal, nextLocalValue));
            }
        }
        return reached;
    }

    // The branch deciding on a call's result: the edge taken when it returned true / non-null, and
    // the one taken on false / null. Null when the code does not branch on it in a shape this reads.
    public static (Instruction Branch, Instruction WhenTruthy, Instruction WhenFalsy)? OutcomeOf(Instruction call)
        => OutcomeOf(call, out var joined) is { } o && joined.Count == 0 ? o : null;

    // Also reads `a || b` as Debug builds emit it: the check's value and a constant from the other
    // operand's arm meet at one stloc before the branch. `joined` holds those constants, which the
    // branch then takes regardless of the check.
    public static (Instruction Branch, Instruction WhenTruthy, Instruction WhenFalsy)? OutcomeOf(Instruction call, out List<int> joined)
    {
        joined = new List<int>();
        var inverted = false;
        int? stored = null;
        Instruction? join = null;
        var i = call.Next;
        for (var steps = 0; i != null && steps < 8; steps++, i = i.Next)
        {
            // Another path landing here would take its own value into the branch.
            if (i != join && IsJumpTarget(i)) return null;
            switch (i.OpCode.Code)
            {
                case Code.Br or Code.Br_S when join == null && stored == null:
                    join = (Instruction)i.Operand;
                    if (ConstantsInto(join, i) is not { } constants) return null;
                    joined.AddRange(constants);
                    i = join.Previous;
                    continue;
                case Code.Dup:
                case Code.Nop:
                    continue;
                case Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3 or Code.Stloc_S or Code.Stloc:
                    stored = LocalIndex(i);
                    continue;
                case Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc_S or Code.Ldloc:
                    if (stored != LocalIndex(i)) return null;
                    continue;
                case Code.Ldnull when i.Next?.OpCode.Code is Code.Cgt_Un or Code.Ceq:
                case Code.Ldc_I4_0 when i.Next?.OpCode.Code == Code.Ceq:
                    continue;
                case Code.Cgt_Un when i.Previous.OpCode.Code == Code.Ldnull:
                    continue;
                case Code.Ceq when i.Previous.OpCode.Code is Code.Ldnull or Code.Ldc_I4_0:
                    inverted = !inverted;
                    continue;
                case Code.Brtrue or Code.Brtrue_S:
                    return inverted ? (i, i.Next, (Instruction)i.Operand) : (i, (Instruction)i.Operand, i.Next);
                case Code.Brfalse or Code.Brfalse_S:
                    return inverted ? (i, (Instruction)i.Operand, i.Next) : (i, i.Next, (Instruction)i.Operand);
                default:
                    return null;
            }
        }
        return null;
    }

    // The constants every other way into `join` brings; null when one brings something else.
    private static List<int>? ConstantsInto(Instruction join, Instruction from)
    {
        var first = join;
        while (first.Previous != null) first = first.Previous;
        var constants = new List<int>();
        for (var i = first; i != null; i = i.Next)
        {
            if (i == from) continue;
            var entersByJump = i.Operand == join || i.Operand is Instruction[] targets && targets.Contains(join);
            var entersByFallingThrough = i.Next == join && i.OpCode.FlowControl is FlowControl.Next;
            if (!entersByJump && !entersByFallingThrough) continue;
            var pushed = entersByJump && i.OpCode.FlowControl == FlowControl.Branch ? i.Previous : i;
            if (entersByJump && i.OpCode.FlowControl != FlowControl.Branch || pushed is null || Constant(pushed) is not { } c) return null;
            constants.Add(c);
        }
        return constants;
    }

    // The successor a branch on a known constant takes.
    public static Instruction Taken(Instruction branch, int constant)
        => (branch.OpCode.Code is Code.Brtrue or Code.Brtrue_S ? constant != 0 : constant == 0) ? (Instruction)branch.Operand : branch.Next;

    private static bool IsJumpTarget(Instruction target)
    {
        var first = target;
        while (first.Previous != null) first = first.Previous;
        for (var i = first; i != null; i = i.Next)
            if (i.Operand == target || i.Operand is Instruction[] targets && targets.Contains(target))
                return true;
        return false;
    }

    private static int? Constant(Instruction i) => i.OpCode.Code switch
    {
        Code.Ldc_I4_M1 => -1,
        Code.Ldc_I4_0 => 0,
        Code.Ldc_I4_1 => 1,
        Code.Ldc_I4_2 => 2,
        Code.Ldc_I4_3 => 3,
        Code.Ldc_I4_4 => 4,
        Code.Ldc_I4_5 => 5,
        Code.Ldc_I4_6 => 6,
        Code.Ldc_I4_7 => 7,
        Code.Ldc_I4_8 => 8,
        Code.Ldc_I4_S => (sbyte)i.Operand,
        Code.Ldc_I4 => (int)i.Operand,
        _ => null,
    };

    private static bool IsStore(Instruction i) => i.OpCode.Code is Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3 or Code.Stloc_S or Code.Stloc;

    private static int? LocalIndex(Instruction i) => i.OpCode.Code switch
    {
        Code.Stloc_0 or Code.Ldloc_0 => 0,
        Code.Stloc_1 or Code.Ldloc_1 => 1,
        Code.Stloc_2 or Code.Ldloc_2 => 2,
        Code.Stloc_3 or Code.Ldloc_3 => 3,
        Code.Stloc_S or Code.Stloc or Code.Ldloc_S or Code.Ldloc => (i.Operand as VariableDefinition)?.Index,
        _ => null,
    };
}
