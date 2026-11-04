using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace AnoMech.Learn.Toolbox;

// Pins a lesson's toolbox example on a scenario's *StateOverrides object by property name, in the
// same spelling StateSnapshot writes values: enums by name, numbers, "(x,z)" vectors and "[a,b]"
// lists of those. Unknown names and unparsable values are skipped, never thrown.
public static class ExampleOverrides
{
    // Returns the properties it set, for Lift.
    public static IReadOnlyList<string> Apply(object overrides, IReadOnlyDictionary<string, string> example)
    {
        var applied = new List<string>();
        foreach (var (name, text) in example)
        {
            var property = overrides.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property is null || !property.CanWrite) continue;
            if (Parse(text, property.PropertyType) is not { } value) continue;
            property.SetValue(overrides, value);
            applied.Add(property.Name);
        }
        return applied;
    }

    public static void Lift(object overrides, IReadOnlyList<string> names)
    {
        foreach (var name in names)
            overrides.GetType().GetProperty(name)?.SetValue(overrides, null);
    }

    public static object? Parse(string text, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target.IsEnum) return Enum.TryParse(target, text, true, out var e) ? e : null;
        if (target == typeof(float)) return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : null;
        if (target == typeof(int)) return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
        if (target == typeof(bool)) return text.Equals("true", StringComparison.OrdinalIgnoreCase) ? true : text.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null;
        if (target == typeof(string)) return text;
        if (target == typeof(Vector2)) return Vector(text) is { } v ? new Vector2(v.X, v.Z) : null;
        if (target == typeof(Vector3)) return Vector(text);
        if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(List<>))
        {
            var item = target.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(target)!;
            foreach (var part in Items(text))
            {
                if (Parse(part, item) is not { } value) return null;
                list.Add(value);
            }
            return list;
        }
        return null;
    }

    private static Vector3? Vector(string text)
    {
        if (text.Length < 5 || text[0] != '(' || text[^1] != ')') return null;
        var parts = text[1..^1].Split(',');
        return parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                                 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? new Vector3(x, 0f, z) : null;
    }

    private static IEnumerable<string> Items(string list)
    {
        var inner = list.Length >= 2 && list[0] == '[' && list[^1] == ']' ? list[1..^1] : list;
        var depth = 0;
        var start = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '(') depth++;
            else if (inner[i] == ')') depth--;
            else if (inner[i] == ',' && depth == 0) { yield return inner[start..i].Trim(); start = i + 1; }
        }
        if (inner.Length > 0) yield return inner[start..].Trim();
    }
}
