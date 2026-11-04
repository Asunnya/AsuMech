using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace AnoMech.Learn.Tools;

// The scenario's rolled state as strings: every public member of each *State object the scenario
// holds, read by reflection so no scenario has to declare anything. Sim objects are skipped.
public static class StateSnapshot
{
    public static Dictionary<string, string> Of(object scenario)
    {
        var result = new Dictionary<string, string>();
        foreach (var field in scenario.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (!field.FieldType.Name.EndsWith("State", StringComparison.Ordinal)) continue;
            if (field.GetValue(scenario) is not { } state) continue;
            var type = state.GetType();
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0))
                Add(result, p.Name, Try(() => p.GetValue(state)));
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                Add(result, f.Name, Try(() => f.GetValue(state)));
        }
        return result;
    }

    private static object? Try(Func<object?> read)
    {
        try { return read(); }
        catch (TargetInvocationException) { return null; }
    }

    private static void Add(Dictionary<string, string> into, string name, object? value)
    {
        if (Describe(value) is { } text) into[name] = text;
    }

    // Null for values with nothing to compare across seeds (sim objects, delegates).
    public static string? Describe(object? value) => value switch
    {
        null => "null",
        string s => s,
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        float f => f.ToString("0.##", CultureInfo.InvariantCulture),
        double d => d.ToString("0.##", CultureInfo.InvariantCulture),
        int or long or short or byte or uint or ushort => Convert.ToString(value, CultureInfo.InvariantCulture),
        Vector3 v => $"({v.X:0.#},{v.Z:0.#})",
        Vector2 v => $"({v.X:0.#},{v.Y:0.#})",
        IEnumerable list => Join(list),
        _ when value.GetType().Namespace?.StartsWith("AnoMech.Core.SimObjects", StringComparison.Ordinal) == true => null,
        _ when value is Delegate => null,
        _ when value.GetType().IsValueType && !value.GetType().IsPrimitive => Tuple(value),
        _ => value.ToString(),
    };

    // A record struct (a lane, a pair) as "(a,b,c)" so list features can name one.
    private static string? Tuple(object value)
    {
        var parts = new List<string>();
        foreach (var p in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0))
        {
            if (Describe(Try(() => p.GetValue(value))) is not { } text) return null;
            parts.Add(text);
        }
        return parts.Count == 0 ? value.ToString() : "(" + string.Join(",", parts) + ")";
    }

    private static string? Join(IEnumerable list)
    {
        var parts = new List<string>();
        foreach (var item in list)
        {
            if (Describe(item) is not { } text) return null;
            parts.Add(text);
        }
        return "[" + string.Join(",", parts) + "]";
    }
}
