#if DEBUG
using System.Globalization;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using AnoMech.Windows.Asu;

namespace AnoMech.Windows;

// The Debug section, shared by both main windows so they drive the one DebugMenu and its hooks.
internal static class DebugSection
{
    private const string SessionDisabledReason = "Disabled in a multiplayer session.";

    public static void Draw(ScenarioPicker picker, DebugMenu menu, bool inSession)
    {
        DrawSeedControl(picker);
        DrawBotControlsPlayer(inSession);
        ImGui.BeginDisabled(inSession);
        ImGui.BeginGroup();
        menu.DrawSpeedControl();
        ImGui.EndGroup();
        ImGui.EndDisabled();
        if (inSession && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(SessionDisabledReason);
        menu.DrawDebugContent();
    }

    private static void DrawSeedControl(ScenarioPicker picker)
    {
        ImGui.SetNextItemWidth(120f * ImGuiHelpers.GlobalScale);
        var seedText = picker.SeedText;
        if (ImGui.InputTextWithHint("Seed##run-seed", "random", ref seedText, 16, ImGuiInputTextFlags.CharsDecimal))
            picker.SeedText = seedText;
        if (seedText.Length > 0)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Clear##run-seed")) picker.SeedText = "";
        }

        // The world keeps its last run's Rng until the next start reseeds it.
        var lastText = Plugin.Game.World.Rng.Seed.ToString(CultureInfo.InvariantCulture);
        ImGui.TextUnformatted($"Last seed: {lastText}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Reuse##last-seed")) picker.SeedText = lastText;
        ImGui.SameLine();
        if (ImGui.SmallButton("Copy##last-seed")) ImGui.SetClipboardText(lastText);
        ImGui.Separator();
    }

    // In a session MultiplayerManager owns DebugBotControl.Enabled per run, so route through its
    // sticky toggle (as the Party panel does); solo nothing else writes the flag.
    private static void DrawBotControlsPlayer(bool inSession)
    {
        var mp = Plugin.Multiplayer;
        if (inSession)
        {
            var botControlled = mp.DebugBotControlled;
            ImGui.BeginDisabled(mp.Session.Started);
            if (Pills.Checkbox("Bot controls player##bot-controls-player", ref botControlled))
                mp.SetDebugBotControlled(botControlled);
            ImGui.EndDisabled();
        }
        else
        {
            var enabled = DebugBotControl.Enabled;
            if (Pills.Checkbox("Bot controls player##bot-controls-player", ref enabled))
                DebugBotControl.Enabled = enabled;
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? "Your seat is driven by the strat's AI. Locked once the fight starts."
                : "Your character is driven by the strat's AI, like a bot in your role.");
        ImGui.Separator();
    }
}
#endif
