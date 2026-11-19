using System;
using System.Net.Http;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ThreadingTask = System.Threading.Tasks.Task;

namespace AsuMech.Core.Native.Implementations.Interop;

// Downloads the per-version firewall opcodes from Hyperborea's GitHub so the packet firewall
// stays current after game patches. Runs asynchronously on plugin load when the stored list is
// not this binary's; until it lands, StartBlockedReason refuses a stay.
internal sealed unsafe class OpcodeUpdater : IDisposable
{
    internal static string CurrentVersion =>
        $"{Framework.Instance()->GameVersionString}_{typeof(OpcodeUpdater).Assembly.GetName().Version}";

    internal static bool Downloading { get; private set; }

    private volatile bool disposed;

    internal OpcodeUpdater()
    {
        if (CurrentVersion == Plugin.Config.ZoneFirewallGameVersion && Plugin.Config.ZoneUpOpcode != 0)
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
            var url = $"https://github.com/kawaii/Hyperborea/raw/main/opcodes/{gameVersion}.txt";
            var text = client.GetStringAsync(url).Result;
            if (disposed) return;
            if (FirewallOpcodes.Parse(text, out var problem) is not { } opcodes)
            {
                Plugin.Log.Warning($"[OpcodeUpdater] Refused the opcode file for {gameVersion}: {problem}.");
                return;
            }
            Plugin.Framework.Run(() =>
            {
                Plugin.Config.ZoneUpOpcode = opcodes.ZoneUp;
                Plugin.Config.ZoneDownOpcodes = opcodes.ZoneDown;
                Plugin.Config.ZoneFirewallGameVersion = current;
                Plugin.Config.Save();
                Downloading = false;
                Plugin.Log.Information($"[OpcodeUpdater] Opcodes updated: ZoneUp {opcodes.ZoneUp}, ZoneDown {string.Join(", ", opcodes.ZoneDown)}");
            });
            handedOff = true;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[OpcodeUpdater] Failed to fetch opcodes: {e.Message}");
        }
        finally
        {
            // A handed-off update clears it on the framework thread, once the list is stored.
            if (!handedOff) Downloading = false;
        }
    }

    public void Dispose() => disposed = true;
}
