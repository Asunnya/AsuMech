using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AsuMech.Tests;

// The plugin assembly the tests reference, read as IL, for rules about which code may touch the
// packet firewall. It is the configuration the tests were built in (Debug by default), so a
// #if DEBUG branch is the one analysed.
internal sealed class PluginIL
{
    private static readonly Lazy<PluginIL> Loaded = new(() => new PluginIL(typeof(Plugin).Assembly.Location));

    public static PluginIL Build => Loaded.Value;

    public ModuleDefinition Module { get; }
    public IReadOnlyList<MethodDefinition> Methods { get; }

    private PluginIL(string path)
    {
        Module = ModuleDefinition.ReadModule(path, new ReaderParameters { ReadSymbols = false, InMemory = true });
        Methods = Module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).ToList();
    }

    // "Namespace.Type::Member", generic arity dropped.
    public static string Name(MethodReference m) => $"{TypeName(m.DeclaringType)}::{m.Name}";

    public static string Name(FieldReference f) => $"{TypeName(f.DeclaringType)}::{f.Name}";

    public static string TypeName(TypeReference t)
    {
        var name = t.GetElementType().FullName;
        var tick = name.IndexOf('`');
        return tick < 0 ? name : name[..tick];
    }

    // The user-written method a lambda, local function or state machine belongs to.
    public static string Owner(MethodDefinition m)
    {
        var type = m.DeclaringType;
        var method = GeneratedFrom(m.Name);
        while (type.IsNested && IsCompilerGenerated(type))
        {
            method ??= GeneratedFrom(type.Name);
            type = type.DeclaringType;
        }
        return $"{TypeName(type)}::{method ?? m.Name}";
    }

    private static string? GeneratedFrom(string name)
    {
        var match = Regex.Match(name, "^<([^>]+)>");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool IsCompilerGenerated(TypeDefinition t)
        => t.Name.StartsWith('<') || t.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    public MethodDefinition Method(string type, string name, int? parameters = null)
    {
        var matches = Module.GetTypes().Where(t => TypeName(t) == type).SelectMany(t => t.Methods)
            .Where(m => m.Name == name && (parameters == null || m.Parameters.Count == parameters)).ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException($"Expected one method {type}::{name} in the plugin, found {matches.Count}; if it was renamed, update the rule that names it.");
        return matches[0];
    }

    public IEnumerable<(MethodDefinition Method, Instruction Instruction, MethodReference Callee)> Calls()
    {
        foreach (var m in Methods)
        foreach (var i in m.Body.Instructions)
            if (i.Operand is MethodReference callee && i.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj or Code.Ldftn or Code.Ldvirtftn)
                yield return (m, i, callee);
    }

    public IEnumerable<(MethodDefinition Method, Instruction Instruction, FieldReference Field)> FieldAccesses()
    {
        foreach (var m in Methods)
        foreach (var i in m.Body.Instructions)
            if (i.Operand is FieldReference f)
                yield return (m, i, f);
    }

    // Owners of every call to a member whose "Type::Member" matches the regex.
    public IReadOnlyList<string> CallersOf(string callee)
        => Calls().Where(c => Regex.IsMatch(Name(c.Callee), callee)).Select(c => Owner(c.Method)).Distinct().OrderBy(x => x).ToList();

    public static bool Calls(Instruction i, string name) => i.Operand is MethodReference m && Name(m) == name;
}
