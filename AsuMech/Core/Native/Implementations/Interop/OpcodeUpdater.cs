using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ThreadingTask = System.Threading.Tasks.Task;

namespace AsuMech.Core.Native.Implementations.Interop;

// Downloads the per-version firewall opcodes from Hyperborea's GitHub so the packet firewall
// stays current after game patches. Runs asynchronously on plugin load when the stored list is
// not this binary's; until it lands, StartBlockedReason refuses a stay. A patch Hyperborea has no
// file for yet takes the newest published one, and is fetched again on each load until its own lands.
internal sealed unsafe class OpcodeUpdater : IDisposable
{
    internal static string CurrentVersion =>
        $"{Framework.Instance()->GameVersionString}_{typeof(OpcodeUpdater).Assembly.GetName().Version}";

    internal static bool Downloading { get; private set; }

    private volatile bool disposed;

    internal OpcodeUpdater()
    {
        if (CurrentVersion == Plugin.Config.ZoneFirewallGameVersion && Plugin.Config.ZoneUpOpcode != 0 && Plugin.Config.ZoneFirewallOpcodesFrom.Length == 0)
        {
            Plugin.Log.Information("[OpcodeUpdater] Opcodes are current.");
            return;
        }
        Plugin.Log.Information("[OpcodeUpdater] Game version changed — fetching new opcodes.");
        var version = new string(Framework.Instance()->GameVersionString);
        var current = CurrentVersion;
        Downloading = true;
        ThreadingTask.Run(() => DownloadOpcodes(version, current));
    }

    private void DownloadOpcodes(string gameVersion, string current)
    {
        var handedOff = false;
        try
        {
            if (disposed) return;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AsuMech");
            var from = gameVersion;
            var text = FetchFile(client, gameVersion);
            if (text == null)
            {
                if (disposed) return;
                from = LatestPublishedFor(client, gameVersion);
                if (from == null)
                {
                    Plugin.Log.Warning($"[OpcodeUpdater] Hyperborea has no opcode file for {gameVersion} nor any earlier one.");
                    handedOff = KeepStoredOpcodes(current);
                    return;
                }
                Plugin.Log.Warning($"[OpcodeUpdater] Hyperborea has no opcode file for {gameVersion} yet; using the newest published one, {from}.");
                text = FetchFile(client, from);
                if (text == null)
                {
                    handedOff = KeepStoredOpcodes(current);
                    return;
                }
            }
            if (disposed) return;
            if (FirewallOpcodes.Parse(text, out var problem) is not { } opcodes)
            {
                Plugin.Log.Warning($"[OpcodeUpdater] Refused the opcode file for {from}: {problem}.");
                handedOff = KeepStoredOpcodes(current);
                return;
            }
            var fallback = from == gameVersion ? "" : from;
            Plugin.Framework.Run(() =>
            {
                Plugin.Config.ZoneUpOpcode = opcodes.ZoneUp;
                Plugin.Config.ZoneDownOpcodes = opcodes.ZoneDown;
                Plugin.Config.ZoneFirewallGameVersion = current;
                Plugin.Config.ZoneFirewallOpcodesFrom = fallback;
                Plugin.Config.Save();
                Downloading = false;
                Plugin.Log.Information($"[OpcodeUpdater] Opcodes updated: ZoneUp {opcodes.ZoneUp}, ZoneDown {string.Join(", ", opcodes.ZoneDown)}");
            });
            handedOff = true;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[OpcodeUpdater] Failed to fetch opcodes: {e.Message}");
            if (!disposed && !handedOff) handedOff = KeepStoredOpcodes(current);
        }
        finally
        {
            // A handed-off update clears it on the framework thread, once the list is stored.
            if (!handedOff) Downloading = false;
        }
    }

    // Nothing could be fetched: the last stored list stands in, still checked against the heartbeat read
    // off the binary, and the next load tries again.
    private bool KeepStoredOpcodes(string current)
    {
        Plugin.Framework.Run(() =>
        {
            if (Plugin.Config.ZoneUpOpcode != 0 && Plugin.Config.ZoneDownOpcodes.Length > 0 && Plugin.Config.ZoneFirewallGameVersion != current)
            {
                Plugin.Log.Warning($"[OpcodeUpdater] Keeping the stored opcodes from {Plugin.Config.ZoneFirewallGameVersion}.");
                Plugin.Config.ZoneFirewallOpcodesFrom = Plugin.Config.ZoneFirewallGameVersion is { Length: > 0 } v ? v : "stored";
                Plugin.Config.ZoneFirewallGameVersion = current;
                Plugin.Config.Save();
            }
            Downloading = false;
        });
        return true;
    }

    // Null when the file is not published.
    private static string? FetchFile(HttpClient client, string version)
    {
        using var response = client.GetAsync($"https://github.com/kawaii/Hyperborea/raw/main/opcodes/{version}.txt").Result;
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    private static string? LatestPublishedFor(HttpClient client, string gameVersion)
    {
        var listing = client.GetStringAsync("https://api.github.com/repos/kawaii/Hyperborea/contents/opcodes").Result;
        using var json = JsonDocument.Parse(listing);
        var names = json.RootElement.EnumerateArray().Select(f => f.TryGetProperty("name", out var n) ? n.GetString() : null).OfType<string>().ToList();
        return FirewallOpcodes.LatestPublishedFor(names, gameVersion);
    }

    public void Dispose() => disposed = true;
}
