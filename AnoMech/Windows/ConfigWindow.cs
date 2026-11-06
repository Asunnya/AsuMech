using System;
using System.Linq;
using System.Numerics;
using AnoMech.Multiplayer;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Windows.Asu;

namespace AnoMech.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private string customRelayUrl;
    private string relayPassword;
    private bool askedForLocations;

    public ConfigWindow(Plugin plugin) : base("AsuMech Settings###AnoMechConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        this.plugin = plugin;
        configuration = Plugin.Config;
        ApplySizing();
        customRelayUrl = configuration.RelayServerUrl;
        relayPassword = configuration.TokenForRelay(configuration.ActiveRelayUrl());
    }

    public void Dispose() => SaveAccent();

    public override void OnClose() => SaveAccent();

    private IDisposable? theme;
    private const float ClassicWidthPx = 380f;
    private const float NewMinWidthPx = 380f;
    private const float NewDefaultWidthPx = 600f;
    private float contentHeight;

    // Classic: fixed width, auto height. New layout: the player sets the width, height follows the content.
    private void ApplySizing()
    {
        var scale = ImGuiHelpers.GlobalScale;
        if (!configuration.UseNewLayout)
        {
            Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
            var width = ClassicWidthPx * scale;
            SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(width, 0), MaximumSize = new Vector2(width, float.MaxValue) };
            return;
        }
        Flags &= ~(ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize);
        Size = new Vector2(NewDefaultWidthPx, 0f);
        SizeCondition = ImGuiCond.FirstUseEver;
        var height = contentHeight > 0f ? contentHeight / scale : 0f;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(NewMinWidthPx * AsuTheme.UserScale, height),
            MaximumSize = new Vector2(float.MaxValue, contentHeight > 0f ? height : float.MaxValue),
        };
    }

    public override void PreDraw()
    {
        ApplySizing();
        if (configuration.UseNewLayout) theme = AsuTheme.Push();
    }

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }

    public override void Draw()
    {
        DrawContent();
        contentHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y - ImGui.GetStyle().ItemSpacing.Y;
    }

    private void DrawContent()
    {
        var onInn = configuration.OpenSimMenuOnInn;
        if (Pills.Checkbox("Open Sim Menu when entering Inn", ref onInn))
        {
            configuration.OpenSimMenuOnInn = onInn;
            configuration.Save();
        }

        var newLayout = configuration.UseNewLayout;
        if (Pills.Checkbox("Use the new main window (preview)", ref newLayout))
            plugin.SetNewLayout(newLayout);

        if (configuration.UseNewLayout)
        {
            DrawAccentColor();
            DrawWindowScale();
        }

        var suppressBgm = configuration.SuppressBgm;
        if (Pills.Checkbox("Suppress scenario BGM", ref suppressBgm))
        {
            configuration.SuppressBgm = suppressBgm;
            configuration.Save();
        }

        var resultMarks = configuration.EnableMechanicResultMarks;
        if (Pills.Checkbox("Show mechanic success/failure marks", ref resultMarks))
        {
            configuration.EnableMechanicResultMarks = resultMarks;
            configuration.Save();
        }

        var showSpeed = configuration.ShowSpeedControl;
        if (Pills.Checkbox("Show speed control", ref showSpeed))
        {
            configuration.ShowSpeedControl = showSpeed;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Adds the scenario speed buttons and a typed value under God mode in the main window.\nWarning: bots can die at other speeds, since they still run at normal speed.");

        var userActions = configuration.EnableUserActions;
        if (Pills.Checkbox("Resolve your own actions", ref userActions))
        {
            configuration.EnableUserActions = userActions;
            configuration.Save();
            if (userActions) Natives.UserActions.Enable();
            else Natives.UserActions.Disable();
        }

        if (configuration.EnableUserActions)
        {
            var threshold = configuration.CastInterruptThreshold;
            ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputFloat("Slidecast window (s)", ref threshold, 0.05f, 0.1f, "%.2f"))
            {
                configuration.CastInterruptThreshold = Math.Clamp(threshold, 0f, 5f);
                configuration.Save();
            }

            var tankMitigation = configuration.EnableTankMitigation;
            if (Pills.Checkbox("Require tank mitigation", ref tankMitigation))
            {
                configuration.EnableTankMitigation = tankMitigation;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Some tankbusters (depending on scenario support) will check your mitigation.");
        }

        ImGui.Separator();

        var ghost = configuration.ShowGhost;
        if (Pills.Checkbox("Show a real pull's party beside the sim", ref ghost)) { configuration.ShowGhost = ghost; configuration.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Rings where a real group stood at each moment, from a recorded pull (.csv / .csv.gz) or an ACT log (.log). A reference, not the answer.");
        if (ghost)
        {
            var source = configuration.GhostSource;
            ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint("File##ghost-source", "recording .csv/.csv.gz or ACT .log", ref source, 400)) { configuration.GhostSource = source; configuration.Save(); }
            var pull = configuration.GhostPull;
            ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("Pull##ghost-pull", ref pull)) { configuration.GhostPull = Math.Max(1, pull); configuration.Save(); }
            ImGui.SameLine();
            var onlyMine = configuration.GhostOnlyMine;
            if (Pills.Checkbox("Only my seat##ghost-mine", ref onlyMine)) { configuration.GhostOnlyMine = onlyMine; configuration.Save(); }
            using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextWrapped(Learn.GhostOverlay.Status);
        }
        var botInspector = configuration.ShowBotInspector;
        if (Pills.Checkbox("Explain the targeted bot", ref botInspector)) { configuration.ShowBotInspector = botInspector; configuration.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Target a bot during a run to see which strat call sent it where, and the lesson's rule for that step.");
        var timelinePanel = configuration.ShowTimelinePanel;
        if (Pills.Checkbox("Show the next casts during a lesson", ref timelinePanel)) { configuration.ShowTimelinePanel = timelinePanel; configuration.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("The scenario's embedded cast timeline with a countdown, top right, and the lesson's steps marked among the casts. Never in Exam.");
        ImGui.TextUnformatted("Show the areas about to hit, during a lesson:");
        var hazardsAssisted = configuration.ShowHazardsAssisted;
        if (Pills.Checkbox("Assisted##hazards-assisted", ref hazardsAssisted)) { configuration.ShowHazardsAssisted = hazardsAssisted; configuration.Save(); }
        ImGui.SameLine();
        var hazardsGuided = configuration.ShowHazardsGuided;
        if (Pills.Checkbox("Guided##hazards-guided", ref hazardsGuided)) { configuration.ShowHazardsGuided = hazardsGuided; configuration.Save(); }
        ImGui.SameLine();
        var hazardsObserve = configuration.ShowHazardsObserve;
        if (Pills.Checkbox("Observe##hazards-observe", ref hazardsObserve)) { configuration.ShowHazardsObserve = hazardsObserve; configuration.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("The outline is the one the sim checks you against: red when you're inside it. Never in Exam.");

        ImGui.Separator();

        var logging = configuration.EnableEventLogging;
        if (Pills.Checkbox("Enable event logging", ref logging))
        {
            configuration.EnableEventLogging = logging;
            configuration.Save();
            if (logging) Plugin.LogManager.Open();
            else Plugin.LogManager.Close();
        }
        ImGui.SameLine();
        if (ImGui.Button("Open logs folder"))
            Plugin.LogManager.OpenLogsFolder();

        DrawPullRecorder();

        ImGui.Separator();
        DrawMultiplayerServer();

#if DEBUG
        ImGui.Separator();

        var safeMode = configuration.SafeMode;
        if (Pills.Checkbox("Safe mode (debug)", ref safeMode))
        {
            configuration.SafeMode = safeMode;
            configuration.Save();
        }
        if (safeMode)
            ImGui.TextWrapped(
                "Safe mode cuts you off from server traffic while in the sim zone. " +
                "You won't see players joining or leaving the party, ready checks, " +
                "or duty pops.");
        else
            ImGui.TextWrapped(
                "Safe mode off — server packets reach the engine, so you'll see " +
                "party updates, ready checks, and duty pops. It's easier to break " +
                "the sim zone this way. You still can't send anything to the server " +
                "while in the instance.");
#endif
    }

    private bool accentUnsaved;

    // Saved once the picker is let go, not on every drag frame.
    private void DrawAccentColor()
    {
        var color = AsuTheme.Rgb(configuration.AccentColor);
        var rgb = new Vector3(color.X, color.Y, color.Z);
        if (ImGui.ColorEdit3("Main color", ref rgb, ImGuiColorEditFlags.NoInputs))
        {
            configuration.AccentColor = AsuTheme.ToRgb(rgb);
            accentUnsaved = true;
        }
        if (configuration.AccentColor != Configuration.DefaultAccentColor)
        {
            ImGui.SameLine();
            if (ImGui.Button("Reset##accent"))
            {
                configuration.AccentColor = Configuration.DefaultAccentColor;
                accentUnsaved = true;
            }
        }
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)) SaveAccent();
    }

    private int? scaleDraft;

    // Applied on release: resizing mid-drag would move the slider out from under the mouse.
    private void DrawWindowScale()
    {
        var percent = scaleDraft ?? AsuTheme.ScalePercent;
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderInt("Window size", ref percent, AsuTheme.MinScalePercent, AsuTheme.MaxScalePercent, "%d%%"))
            scaleDraft = (int)MathF.Round(percent / (float)ScaleStepPercent) * ScaleStepPercent;
        if (ImGui.IsItemDeactivated() && scaleDraft is { } draft)
        {
            scaleDraft = null;
            configuration.WindowScalePercent = draft;
            configuration.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Scales the text, buttons and spacing of the main window and this one. Ctrl+click to type a value.");
        if (configuration.WindowScalePercent != 100 && scaleDraft == null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Reset##scale"))
            {
                configuration.WindowScalePercent = 100;
                configuration.Save();
            }
        }
    }

    private const int ScaleStepPercent = 5;

    // Also on close and unload: a pick still held in memory is lost when the plugin reloads.
    private void SaveAccent()
    {
        if (!accentUnsaved) return;
        configuration.Save();
        accentUnsaved = false;
    }

    // Zoning into a savage, ultimate or extreme asks in chat; the recorder itself never starts
    // while a scenario is loaded.
    private void DrawPullRecorder()
    {
        var ask = configuration.AskToRecordPulls;
        if (Pills.Checkbox("Ask to record real pulls (savage, ultimate, extreme)", ref ask))
        {
            configuration.AskToRecordPulls = ask;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("On zoning in, a chat line offers Yes / No / Always here. Every cast, ability, status, spawn and position goes to a CSV in the SimTimeline format, for replay-diff against the scenario. /asu rec starts one anywhere.");
        var recorder = Plugin.Recorder;
        var dir = configuration.PullRecordingsDir;
        ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("Folder##pull-recordings-dir", "plugin config folder/replays", ref dir, 260))
        {
            configuration.PullRecordingsDir = dir;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Point this at the repo's datalogs/replays so the recordings sit next to the ACT logs.");
        if (configuration.RecordAlwaysTerritories.Count > 0)
        {
            ImGui.TextUnformatted($"Always recording in {configuration.RecordAlwaysTerritories.Count} dut{(configuration.RecordAlwaysTerritories.Count == 1 ? "y" : "ies")}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Forget##record-always"))
            {
                configuration.RecordAlwaysTerritories.Clear();
                configuration.Save();
            }
        }
        var upload = configuration.UploadRecordings;
        if (Pills.Checkbox("Send finished recordings to the study server", ref upload))
        {
            configuration.UploadRecordings = upload;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Gzipped and sent to the recordings server built into this build, so whoever studies the fight gets your pulls without any setup. The file stays on your disk either way.");
        if (upload)
        {
            var (relay, _) = configuration.RecordingsDestination();
            var peerId = relay.Length > 0 ? AnoMech.Network.RelayWire.PeerId(AnoMech.Network.RelayWire.RelayCredential(configuration.EnsurePeerSecret(), relay)).ToString() : "";
            ImGui.TextUnformatted(relay.Length > 0 ? $"Server: {relay}" : "No recordings server in this build.");
            if (peerId.Length > 0)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Copy my id##peer-id")) ImGui.SetClipboardText(peerId);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Your id on that server, {peerId}: what its operator puts in --recordings-from to allow you.");
            }
            if (ImGui.TreeNodeEx("Another server##recordings-override", ImGuiTreeNodeFlags.None))
            {
                var url = configuration.RecordingsRelayUrl;
                ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputTextWithHint("Address##recordings-relay-url", "wss://host (empty = built-in)", ref url, 200))
                {
                    configuration.RecordingsRelayUrl = url;
                    configuration.Save();
                }
                var token = configuration.RecordingsRelayToken;
                ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputText("Password##recordings-relay-token", ref token, 200, ImGuiInputTextFlags.Password))
                {
                    configuration.RecordingsRelayToken = token;
                    configuration.Save();
                }
                ImGui.TreePop();
            }
        }
        if (recorder.Recording)
            ImGui.TextUnformatted($"Recording: {recorder.Rows} rows, {recorder.Elapsed:mm\\:ss} -- {recorder.Path}");
        else if (recorder.Armed)
            ImGui.TextUnformatted("Armed: starts as soon as you're in the duty.");
        else
            ImGui.TextUnformatted(recorder.Path is { } last ? $"Idle. Last recording: {last}" : "Idle.");
        if (recorder.MissingHooks.Count > 0)
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.3f, 1f), $"Not recorded this build: {string.Join(", ", recorder.MissingHooks)}");
    }

    private void DrawMultiplayerServer()
    {
        ImGui.TextUnformatted("Multiplayer server");
        if (Plugin.Multiplayer.InSession)
            ImGui.TextDisabled("Changes apply the next time you host or join.");

        var activeUrl = configuration.ActiveRelayUrl();
        var valid = MultiplayerUi.IsPlausibleRelayUrl(activeUrl);
        // Only a server the player typed in, or asked about, is contacted from here: opening Settings
        // mustn't contact the default server for someone who never uses multiplayer. The Multiplayer
        // screen asks.
        var ask = configuration.UseCustomRelay || (askedForLocations && !RelayGroups.Known);
        (RelayServerStatus.Reachability State, bool RequiresPassword)? status =
            !valid ? null : ask ? RelayServerStatus.Of(activeUrl) : RelayServerStatus.Known(activeUrl);
        var needsPassword = status?.RequiresPassword ?? false;

        if (ImGui.RadioButton("Default server", !configuration.UseCustomRelay)) UseCustomRelay(false);
        if (!configuration.UseCustomRelay)
        {
            if (status is { } defaultStatus)
            {
                ImGui.SameLine();
                MultiplayerUi.DrawServerStatus(defaultStatus.State);
            }
            ImGui.Indent();
            DrawLocations(DefaultRelay.Location);
            if (needsPassword) DrawPassword();
            DrawHostOn();
            ImGui.Unindent();
        }

        if (ImGui.RadioButton("Custom server", configuration.UseCustomRelay)) UseCustomRelay(true);
        if (!configuration.UseCustomRelay) return;
        if (status is { } customStatus)
        {
            ImGui.SameLine();
            MultiplayerUi.DrawServerStatus(customStatus.State);
        }
        ImGui.Indent();
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("##relayUrl", "relay.example.com:7890", ref customRelayUrl, 256))
        {
            configuration.RelayServerUrl = customRelayUrl;
            configuration.Save();
            // Shows the password saved for this server, if any. Hidden rather than cleared while
            // the address points elsewhere, so a typo doesn't lose it.
            relayPassword = configuration.TokenForRelay(customRelayUrl);
        }
        if (!valid && !string.IsNullOrWhiteSpace(customRelayUrl))
            ImGui.TextColored(MultiplayerUi.BadColor, "Doesn't look like a valid server address.");
        DrawLocations(null);
        if (needsPassword) DrawPassword();
        DrawHostOn();
        MultiplayerUi.DrawLink("How to run your own server", MultiplayerUi.RelayReadmeUrl);
        ImGui.Unindent();
    }

    // A default server may run in several locations, which only its list says: until that's
    // fetched, naming the one built in would read as the only one. Pings come from the
    // Multiplayer screen; Settings never measures them.
    private void DrawLocations(string? builtIn)
    {
        if (!RelayGroups.Known)
        {
            if (builtIn == null) return;
            if (ImGui.Button("Connect to see available locations")) askedForLocations = true;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Asks the default server which locations it has.");
            return;
        }
        if (RelayGroups.Current is not { Members.Length: > 1 } group)
        {
            if ((RelayGroups.Current?.Members[0].Location ?? builtIn) is { } location) ImGui.TextDisabled($"Location: {location}");
            return;
        }
        ImGui.TextDisabled("Locations:");
        foreach (var member in group.Members) ImGui.BulletText($"{member.Location}{MultiplayerUi.PingSuffix(member.Url)}");
    }

    private void DrawHostOn()
    {
        if (RelayGroups.Current is not { Members.Length: > 1 } group) return;
        const string closest = "Closest (automatic)";
        var chosen = group.Members.FirstOrDefault(m => RelayGroups.SameServer(m.Url, configuration.HostRelayUrl));
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Host on");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("##hostOn", chosen?.Location ?? closest))
        {
            if (ImGui.Selectable(closest, chosen is null)) SetHostOn("");
            foreach (var member in group.Members)
                if (ImGui.Selectable($"{member.Location}{MultiplayerUi.PingSuffix(member.Url)}###hostOn{member.Number}", member == chosen))
                    SetHostOn(member.Url);
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Where sessions you host run. Players who join are connected there automatically.");
    }

    private void SetHostOn(string url)
    {
        configuration.HostRelayUrl = url;
        configuration.Save();
    }

    private void UseCustomRelay(bool custom)
    {
        configuration.UseCustomRelay = custom;
        configuration.Save();
        relayPassword = configuration.TokenForRelay(configuration.ActiveRelayUrl());
    }

    private void DrawPassword()
    {
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (!ImGui.InputTextWithHint("##relayToken", "Server password", ref relayPassword, 128, ImGuiInputTextFlags.Password)) return;
        configuration.RelayAccessToken = relayPassword;
        configuration.RelayTokenOrigin = Configuration.OriginOf(configuration.ActiveRelayUrl());
        configuration.Save();
    }
}
