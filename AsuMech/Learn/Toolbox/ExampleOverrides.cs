using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;

namespace AsuMech.Learn.Toolbox;

// Pins a lesson's toolbox example on a scenario's *StateOverrides object by property name, in the
// same spelling StateSnapshot writes values: enums by name, numbers, compass names for Direction,
// "(x,z)" vectors and tuples, "[a,b]" lists of those, "[(role,value),…]" dictionaries and
// per-role settings. Unknown names and unparsable values are skipped, never thrown.
public static class ExampleOverrides
{
    // Returns the properties it set, for Lift.
    public static IReadOnlyList<string> Apply(object overrides, IReadOnlyDictionary<string, string> example)
    {
        var applied = new List<string>();
        foreach (var (name, text) in example)
        {
            var property = overrides.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property is null) continue;
            if (IsPerRole(property.PropertyType))
            {
                if (property.GetValue(overrides) is not { } setting || !ApplyPerRole(setting, text)) continue;
                applied.Add(property.Name);
                continue;
            }
            if (!property.CanWrite)
            {
                if (property.PropertyType.IsArray && property.GetValue(overrides) is Array slots && FillArray(slots, text)) applied.Add(property.Name);
                continue;
            }
            if (Parse(text, property.PropertyType) is not { } value) continue;
            property.SetValue(overrides, value);
            applied.Add(property.Name);
        }
        return applied;
    }

    public static void Lift(object overrides, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            var property = overrides.GetType().GetProperty(name);
            if (property is null) continue;
            if (IsPerRole(property.PropertyType)) (property.GetValue(overrides) as IPerRoleSetting)?.Clear();
            else if (!property.CanWrite) { if (property.GetValue(overrides) is Array slots) Array.Clear(slots); }
            else property.SetValue(overrides, null);
        }
    }

    public static object? Parse(string text, Type type)
    {
        text = text.Trim();
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target.IsEnum) return Enum.TryParse(target, text, true, out var e) ? e : null;
        if (target == typeof(float)) return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : null;
        if (target == typeof(double)) return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        if (target == typeof(int)) return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
        if (target == typeof(uint)) return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u) ? u : null;
        if (target == typeof(long)) return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : null;
        if (target == typeof(short)) return short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : null;
        if (target == typeof(byte)) return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : null;
        if (target == typeof(bool)) return text.Equals("true", StringComparison.OrdinalIgnoreCase) ? true : text.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null;
        if (target == typeof(string)) return text;
        if (target == typeof(Vector2)) return Vector(text) is { } v ? new Vector2(v.X, v.Z) : null;
        if (target == typeof(Vector3)) return Vector(text);
        if (target == typeof(Direction)) return ParseDirection(text);
        if (NamedValue(target, text) is { } named) return named;
        if (target.IsArray) return ParseArray(text, target.GetElementType()!);
        if (target.IsGenericType)
        {
            var def = target.GetGenericTypeDefinition();
            var args = target.GetGenericArguments();
            if (def == typeof(List<>) || def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>) || def == typeof(IEnumerable<>)
                || def == typeof(IList<>) || def == typeof(ICollection<>))
                return ParseList(text, args[0]);
            if (def == typeof(Dictionary<,>) || def == typeof(IReadOnlyDictionary<,>) || def == typeof(IDictionary<,>))
                return ParseDictionary(text, args[0], args[1]);
            if (target.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
                return ParseTuple(text, args);
        }
        return null;
    }

    // Enum-like records (OmegaAttack.Pile, NorthSouth.North) by the static member's name.
    public static object? NamedValue(Type type, string name)
        => NamedValues(type).FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    public static IEnumerable<KeyValuePair<string, object>> NamedValues(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string)) yield break;
        foreach (var f in type.GetFields(BindingFlags.Static | BindingFlags.Public).Where(f => f.FieldType == type))
            if (f.GetValue(null) is { } v) yield return new(f.Name, v);
        foreach (var p in type.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(p => p.PropertyType == type && p.GetIndexParameters().Length == 0))
            if (p.GetValue(null) is { } v) yield return new(p.Name, v);
    }

    // A compass name as Direction.Name() spells it, or degrees clockwise from north.
    private static Direction? ParseDirection(string text)
    {
        foreach (var d in Direction.All)
            if (d.Name().Equals(text, StringComparison.OrdinalIgnoreCase)) return d;
        var number = text.TrimEnd('°', ' ');
        return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees) ? new Direction(degrees * MathF.PI / 180f) : null;
    }

    private static IList? ParseList(string text, Type item)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item))!;
        foreach (var part in Items(text))
        {
            if (Parse(part, item) is not { } value) return null;
            list.Add(value);
        }
        return list;
    }

    private static Array? ParseArray(string text, Type item)
    {
        if (ParseList(text, item) is not { } list) return null;
        var array = Array.CreateInstance(item, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    private static IDictionary? ParseDictionary(string text, Type key, Type value)
    {
        var dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(key, value))!;
        foreach (var part in Items(text))
        {
            var pair = Items(part).ToList();
            if (pair.Count != 2 || Parse(pair[0], key) is not { } k || Parse(pair[1], value) is not { } v) return null;
            dict[k] = v;
        }
        return dict;
    }

    private static object? ParseTuple(string text, Type[] items)
    {
        var parts = Items(text).ToList();
        if (parts.Count != items.Length) return null;
        var values = new object[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            if (Parse(parts[i], items[i]) is not { } value) return null;
            values[i] = value;
        }
        return Activator.CreateInstance(typeof(ValueTuple).Assembly.GetType($"System.ValueTuple`{items.Length}")!.MakeGenericType(items), values);
    }

    // A fixed-size slot array (one roll per round): "[a,null,b]" fills the named slots in place.
    private static bool FillArray(Array slots, string text)
    {
        var item = slots.GetType().GetElementType()!;
        var parts = Items(text).ToList();
        if (parts.Count > slots.Length) return false;
        var any = false;
        for (var i = 0; i < parts.Count; i++)
        {
            if (parts[i] == "null") continue;
            if (Parse(parts[i], item) is not { } value) return false;
            slots.SetValue(value, i);
            any = true;
        }
        return any;
    }

    private static bool IsPerRole(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PerRoleSetting<>);

    // "[(MainTank,Cross),(OffTank,Square)]" seats the named roles; "[Cross,Square,…]" of eight
    // seats them in PartyRole order.
    private static bool ApplyPerRole(object setting, string text)
    {
        var item = setting.GetType().GetGenericArguments()[0];
        var seats = (Array)setting.GetType().GetProperty("Seats")!.GetValue(setting)!;
        var parts = Items(text).ToList();
        var any = false;
        for (var i = 0; i < parts.Count; i++)
        {
            var pair = Items(parts[i]).ToList();
            int seat;
            string valueText;
            if (pair.Count == 2 && Enum.TryParse<PartyRole>(pair[0], true, out var role)) { seat = (int)role; valueText = pair[1]; }
            else if (pair.Count == 1 && parts.Count == seats.Length) { seat = i; valueText = pair[0]; }
            else return false;
            if (valueText == "null") continue;
            if (Parse(valueText, item) is not { } value) return false;
            seats.SetValue(value, seat);
            any = true;
        }
        return any;
    }

    private static Vector3? Vector(string text)
    {
        if (text.Length < 5 || text[0] != '(' || text[^1] != ')') return null;
        var parts = text[1..^1].Split(',');
        return parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                                 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? new Vector3(x, 0f, z) : null;
    }

    // Top-level items of "[a,b]" or "(a,b)", nested brackets kept whole.
    public static IEnumerable<string> Items(string list)
    {
        list = list.Trim();
        var inner = list.Length >= 2 && (list[0] == '[' && list[^1] == ']' || list[0] == '(' && list[^1] == ')') ? list[1..^1] : list;
        var depth = 0;
        var start = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] is '(' or '[') depth++;
            else if (inner[i] is ')' or ']') depth--;
            else if (inner[i] == ',' && depth == 0) { yield return inner[start..i].Trim(); start = i + 1; }
        }
        if (inner.Trim().Length > 0) yield return inner[start..].Trim();
    }
}
