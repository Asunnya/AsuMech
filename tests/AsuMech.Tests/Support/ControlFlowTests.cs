using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AsuMech.Tests;

// The IL rules credit a check with a path only when the path took the check's wanted branch.
// These pin that reading on hand-built IL, since which shapes the compiler emits is not up to the
// tests.
public class ControlFlowTests
{
    // c ? Check() : true, with the call falling into the join: the other arm takes a constant
    // true into the same branch, so that branch says nothing about the check.
    [Test]
    public void ABranchAnotherPathAlsoReaches_IsNoProofOfTheCheck()
    {
        var (m, check, write) = Build((il, checkMethod, skip) =>
        {
            var call = il.Create(OpCodes.Call, checkMethod);
            var join = il.Create(OpCodes.Brfalse_S, skip);
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Brtrue_S, call));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Br_S, join));
            il.Append(call);
            il.Append(join);
            return call;
        });
        Assert.That(IsReachableWithoutTheCheck(m, check, write), Is.True, "With c false the write runs without asking.");
    }

    [Test]
    public void ACheckInItsOwnStatement_GuardsTheWrite()
    {
        var (m, check, write) = Build((il, checkMethod, skip) =>
        {
            var call = il.Create(OpCodes.Call, checkMethod);
            il.Append(call);
            il.Append(il.Create(OpCodes.Brfalse_S, skip));
            return call;
        });
        Assert.That(IsReachableWithoutTheCheck(m, check, write), Is.False);
    }

    // if (!Check()) return; as Debug builds emit it.
    [Test]
    public void ACheckThroughADebugTemporary_GuardsTheWrite()
    {
        var (m, check, write) = Build((il, checkMethod, skip) =>
        {
            AddLocal(il);
            var call = il.Create(OpCodes.Call, checkMethod);
            il.Append(call);
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Ceq));
            il.Append(il.Create(OpCodes.Stloc_0));
            il.Append(il.Create(OpCodes.Ldloc_0));
            il.Append(il.Create(OpCodes.Brtrue_S, skip));
            return call;
        });
        Assert.That(IsReachableWithoutTheCheck(m, check, write), Is.False);
    }

    [Test]
    public void ACheckWhoseResultTheBranchDoesNotRead_GuardsNothing()
    {
        var (m, check, write) = Build((il, checkMethod, skip) =>
        {
            AddLocal(il);
            AddLocal(il);
            var call = il.Create(OpCodes.Call, checkMethod);
            il.Append(call);
            il.Append(il.Create(OpCodes.Stloc_0));
            il.Append(il.Create(OpCodes.Ldloc_1));
            il.Append(il.Create(OpCodes.Brfalse_S, skip));
            return call;
        });
        Assert.That(IsReachableWithoutTheCheck(m, check, write), Is.True);
    }

    // if (c || !Check()) return; as Debug builds emit it: c's arm brings a constant true into the
    // stloc the check's negation also lands in, and that true leads to the return.
    [Test]
    public void AnOrJoinWhoseOtherArmReturns_GuardsTheWrite()
    {
        var (m, check, write) = OrJoin(otherArm: OpCodes.Ldc_I4_1);
        Assert.That(IsReachableWithoutTheCheck(m, check, write), Is.False);
    }

    // The same join with the other arm bringing false: that arm runs the write without the check,
    // so the branch proves nothing and the rule must refuse to read it.
    [Test]
    public void AnOrJoinWhoseOtherArmFallsThrough_IsNoProofOfTheCheck()
    {
        var (_, check, _) = OrJoin(otherArm: OpCodes.Ldc_I4_0);
        var outcome = ControlFlow.OutcomeOf(check, out var joined);
        Assert.That(outcome, Is.Not.Null);
        Assert.That(joined.Any(c => ControlFlow.Taken(outcome!.Value.Branch, c) == outcome.Value.WhenTruthy), Is.True);
    }

    private static (MethodDefinition, Instruction, Instruction) OrJoin(OpCode otherArm) => Build((il, checkMethod, skip) =>
    {
        AddLocal(il);
        var call = il.Create(OpCodes.Call, checkMethod);
        var constant = il.Create(otherArm);
        var store = il.Create(OpCodes.Stloc_0);
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Brtrue_S, constant));
        il.Append(call);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ceq));
        il.Append(il.Create(OpCodes.Br_S, store));
        il.Append(constant);
        il.Append(store);
        il.Append(il.Create(OpCodes.Ldloc_0));
        il.Append(il.Create(OpCodes.Brtrue_S, skip));
        return call;
    });

    private static bool IsReachableWithoutTheCheck(MethodDefinition m, Instruction check, Instruction write)
    {
        var outcome = ControlFlow.OutcomeOf(check, out var joined);
        if (outcome is not { } o || joined.Any(c => ControlFlow.Taken(o.Branch, c) == o.WhenTruthy)) return true;
        return new ControlFlow(m).FeasiblyReachableAvoiding([(o.Branch, o.WhenTruthy)]).Contains(write);
    }

    private static void AddLocal(ILProcessor il) => il.Body.Variables.Add(new VariableDefinition(il.Body.Method.Module.TypeSystem.Boolean));

    // static void M(bool c): the shape `emit` appends, then a call to Write, then ret. `emit` gets
    // the Check method and the ret to branch to when the check says no.
    private static (MethodDefinition Method, Instruction Check, Instruction Write) Build(Func<ILProcessor, MethodReference, Instruction, Instruction> emit)
    {
        var module = ModuleDefinition.CreateModule("Probe", ModuleKind.Dll);
        var type = new TypeDefinition("Probe", "Shapes", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(type);
        var checkMethod = Stub(type, "Check", module.TypeSystem.Boolean);
        var writeMethod = Stub(type, "Write", module.TypeSystem.Void);
        var m = new MethodDefinition("M", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        m.Parameters.Add(new ParameterDefinition("c", ParameterAttributes.None, module.TypeSystem.Boolean));
        type.Methods.Add(m);
        var il = m.Body.GetILProcessor();
        var write = il.Create(OpCodes.Call, writeMethod);
        var ret = il.Create(OpCodes.Ret);
        var check = emit(il, checkMethod, ret);
        il.Append(write);
        il.Append(ret);
        return (m, check, write);
    }

    private static MethodDefinition Stub(TypeDefinition type, string name, TypeReference returns)
    {
        var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, returns);
        var il = method.Body.GetILProcessor();
        if (returns.MetadataType == MetadataType.Boolean) il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Ret));
        type.Methods.Add(method);
        return method;
    }
}
