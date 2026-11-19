using System;
using System.Reflection;

namespace AsuMech.Core.Native.Implementations;

// Dalamud moves ClientState.TerritoryType only on a server zone-in, which a client-side load never
// gets, so plugins that gate on it (Splatoon zone locks, script territories) keep reading the inn.
// A plugin's ClientState is a scoped proxy without a setter; the service it wraps has a private
// one, which also raises TerritoryChanged.
internal static class ClientTerritory
{
    // True while the sim moves the reading: Plugin.OnTerritoryChanged leaves the windows alone.
    public static bool Moving { get; private set; }

    public static bool TrySet(uint territory)
    {
        if (Plugin.ClientState.TerritoryType == territory) return true;
        try
        {
            if (FindSetter(Plugin.ClientState) is not var (target, setter))
            {
                DiagnosticLog.Warn($"[ClientTerritory] No TerritoryType setter behind {Plugin.ClientState.GetType().FullName}.");
                return false;
            }
            Moving = true;
            var type = setter.GetParameters()[0].ParameterType;
            try { setter.Invoke(target, [Convert.ChangeType(territory, type)]); }
            finally { Moving = false; }
            return Plugin.ClientState.TerritoryType == territory;
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[ClientTerritory] Could not set Dalamud's territory to {territory}: {e}");
            return false;
        }
    }

    private static (object Target, MethodInfo Setter)? FindSetter(object proxy)
    {
        if (SetterOf(proxy.GetType()) is { } own) return (proxy, own);
        foreach (var field in proxy.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetValue(proxy) is not { } inner || inner == proxy) continue;
            if (SetterOf(inner.GetType()) is { } setter) return (inner, setter);
        }
        return null;
    }

    private static MethodInfo? SetterOf(Type type)
        => type.GetProperty("TerritoryType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetSetMethod(nonPublic: true);
}
