using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using AsuMech.Core.Map;
using AsuMech.Core;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using SessionStatus = AsuMech.Core.Game.SessionStatus;
using PracticeLoop = AsuMech.Core.Game.PracticeLoop;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios;
using static AsuMech.Core.Game.Game;
using AsuMech.Core.Native.Implementations;

namespace AsuMech.Windows;

public unsafe class MainWindow : Window, IDisposable, IPanelHost
{
    private const float ScenarioButtonExtraPadding = 6f;
    private const float SetupDropdownWidth = 180f;

    internal static readonly Vector4 StartColor = new(0.18f, 0.40f, 0.24f, 0.92f);
    internal static readonly Vector4 StopColor = new(0.45f, 0.14f, 0.16f, 0.92f);
    private static readonly Vector4 StopRunColor = new(0.30f, 0.30f, 0.33f, 0.92f);
    private static readonly Vector4 RunningColor = new(0.35f, 0.85f, 0.45f, 1f);
    private static readonly Vector4 MistakeColor = new(1f, 0.65f, 0.25f, 1f);
    private static readonly Vector4 FailedColor = new(1f, 0.38f, 0.35f, 1f);
    // Fixed rather than theme-derived: Dalamud's default ButtonActive is red, which reads as
    // "stop"/error next to the semantic Start/Stop buttons.
    private static readonly Vector4 SelectionColor = new(0.30f, 0.55f, 0.85f, 1f);
    private static readonly Vector4 SelectionHoveredColor = new(0.28f, 0.50f, 0.78f, 0.92f);
    private static readonly Vector4 SelectionActiveColor = new(0.22f, 0.42f, 0.68f, 1f);
    private static readonly Vector4 HoverColor = new(0.24f, 0.39f, 0.60f, 0.88f);

    private readonly Plugin plugin;
    private readonly TitleBarButton autoCollapseButton;
    private IZone? _openZone;
    private string? _pendingCategoryTab;
    internal ScenarioPanelWindow ScenarioPanel { get; }
    internal PartyPanelWindow PartyPanel { get; }
    private readonly MultiplayerUi multiplayerUi;
    internal Vector2 ScenarioPanelAnchor { get; private set; }
    internal Vector2 RightPanelAnchor { get; private set; }
    internal float ScenarioPanelHeight { get; private set; }
    internal bool IsActuallyCollapsed { get; private set; }
    bool IPanelHost.IsActuallyCollapsed => IsActuallyCollapsed;
    bool IPanelHost.ShowingMultiplayerSetup => ShowingMultiplayerSetup;
    Vector2 IPanelHost.RightPanelAnchor => RightPanelAnchor;
    float IPanelHost.ScenarioPanelHeight => ScenarioPanelHeight;
    private float _windowChromeHeight;
    private ScenarioPicker Picker => plugin.Picker;
    private IScenario? _selectedScenario => Picker.Scenario;
    internal IScenario? SelectedScenario => Picker.Scenario;
    internal ScenarioSelection? Selection => Picker.Selection;


#if DEBUG
    private readonly DebugMenu debugMenu;
    internal DebugMenu DebugMenu => debugMenu;
#endif

    // <Version> from AnoMech.csproj flows into the assembly version; surface it in the
    // title bar. Use a ### id so the window identity stays "MainWindow" across versions.
    private static readonly string TitleText = TitleWithVersion();

    private static string TitleWithVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        var version = v is null ? "" : $" v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        return $"AsuMech{version}###MainWindow";
    }

    // The title bar still shows while the window is collapsed for the run.
    private static string Title(bool failed) => $"{TitleText}{(failed ? " — Failed" : "")}###MainWindow";

    public MainWindow(Plugin plugin)
        : base(Title(failed: false))
    {
        var uiScale = ImGuiHelpers.GlobalScale;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(220, 80) * uiScale,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        this.plugin = plugin;
        ScenarioPanel = new ScenarioPanelWindow(this);
        PartyPanel = new PartyPanelWindow(this, Plugin.Multiplayer);
        multiplayerUi = new MultiplayerUi(plugin);
        IsOpen = false;
        RestoreSelectedScenario();

        autoCollapseButton = new TitleBarButton
        {
            Icon = FontAwesomeIcon.CompressAlt,
            IconOffset = new Vector2(1f, 1f),
            Priority = 2,
            Click = _ =>
            {
                Plugin.Config.AutoCollapseWhileRunning = !Plugin.Config.AutoCollapseWhileRunning;
                Plugin.Config.Save();
            },
            ShowTooltip = () => ImGui.SetTooltip(
                $"Auto-collapse while running: {(Plugin.Config.AutoCollapseWhileRunning ? "On" : "Off")}"),
        };
        TitleBarButtons.Add(autoCollapseButton);

        // Small gear opens the settings window (same toggle as /asumech config).
        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2f, 1f) * uiScale,
            Priority = 0,
            Click = _ => plugin.ToggleConfigUi(),
            ShowTooltip = () => ImGui.SetTooltip("Settings"),
        });
#if DEBUG
        debugMenu = new DebugMenu(plugin);
#endif
    }

    public void Dispose()
    {
#if DEBUG
        debugMenu.Dispose();
#endif
    }

    private bool _wasInInstance;
    private bool _wasScenarioActive;
    private SessionStatus _lastStatus;
    private bool _clearCollapsedRequest;

    // In a session the run counts from the start handshake, before Game has one.
    private bool RunActive => Plugin.Multiplayer.SessionCode != null ? Plugin.Multiplayer.IsRunning : Plugin.Game.Status >= SessionStatus.Running;

    private bool InSession => Plugin.Multiplayer.InSession;
    private bool IsGuest => Plugin.Multiplayer.SessionCode != null && !Plugin.Multiplayer.IsHost;

    // The whole window becomes the multiplayer setup screen until a session starts.
    private bool _showMultiplayerSetup;
    internal bool ShowingMultiplayerSetup => _showMultiplayerSetup && !InSession;
    private bool _wasInSession;

    internal void OpenMultiplayer()
    {
        IsOpen = true;
        if (!InSession) _showMultiplayerSetup = true;
    }

    // A session that ended on someone else's terms reopens the setup screen, where its reason
    // is shown; leaving on our own returns to the scenario view.
    private void TrackSessionEnd()
    {
        var inSession = InSession;
        if (inSession) _showMultiplayerSetup = false;
        else if (_wasInSession && Plugin.Multiplayer.SessionEndReason != null) _showMultiplayerSetup = true;
        _wasInSession = inSession;
    }

    // A guest runs whatever the host picked, so its window shows that scenario.
    private void FollowHostScenario()
    {
        if (!IsGuest || Plugin.Multiplayer.TryResolveScenario() is not { } hostScenario) return;
        Picker.FollowHost(hostScenario);
        _openZone = hostScenario.Phase.Zone;
    }

    // The window is collapsible while a scenario runs, and expands again when the run ends.
    // Outside a run, fake-zone sessions keep it expanded so the next action is visible.
    public override void PreOpenCheck()
    {
        TrackSessionEnd();
        FollowHostScenario();
        if (_clearCollapsedRequest)
        {
            Collapsed = null;
            CollapsedCondition = ImGuiCond.None;
            _clearCollapsedRequest = false;
        }

        var scenarioActive = RunActive;
        var status = Plugin.Game.Status;
        WindowName = Title(Plugin.Game.LastRun is { } lastRun && IsFailed(lastRun.Status));
        if (scenarioActive && !_wasScenarioActive)
        {
            if (Plugin.Config.AutoCollapseWhileRunning)
                RequestCollapsed(true);
        }
        // Not on a failure the run plays on from: expanding mid-fight gets in the way.
        if (status >= SessionStatus.Frozen && status != _lastStatus)
        {
            RequestCollapsed(false);
        }
        else if (!scenarioActive && _wasScenarioActive)
        {
            RequestCollapsed(false);
        }

        var inInstance = Plugin.Game.World.Map.IsInInstance;
        if (inInstance)
        {
            // Both main windows check every frame; only the active layout's may force itself open.
            if (!Plugin.Config.UseNewLayout) IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            if (scenarioActive)
                Flags &= ~ImGuiWindowFlags.NoCollapse;
            else
                Flags |= ImGuiWindowFlags.NoCollapse;
            if (!_wasInInstance)
            {
                ScenarioPanel.Close();
                if (!scenarioActive)
                    RequestCollapsed(false);
            }
        }
        else
        {
            ShowCloseButton = true;
            RespectCloseHotkey = true;
            Flags &= ~ImGuiWindowFlags.NoCollapse;
            if (_wasInInstance)
                CollapsedCondition = ImGuiCond.FirstUseEver;
        }

        _wasScenarioActive = scenarioActive;
        _lastStatus = status;
        _wasInInstance = inInstance;
    }

    private void RequestCollapsed(bool collapsed)
    {
        Collapsed = collapsed;
        CollapsedCondition = ImGuiCond.Always;
        _clearCollapsedRequest = true;
    }

    public override void PreDraw()
    {
        autoCollapseButton.IconColor = Plugin.Config.AutoCollapseWhileRunning
            ? StyleColor(ImGuiCol.Text)
            : StyleColor(ImGuiCol.TextDisabled);

        var uiScale = ImGuiHelpers.GlobalScale;
        var minimumHeight = 80f;
        if (ScenarioPanel.RequestedOpen && ScenarioPanel.NaturalHeight > 0f)
            minimumHeight = Math.Max(minimumHeight, (_windowChromeHeight + ScenarioPanel.NaturalHeight) / uiScale);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(220f, minimumHeight),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void PostDraw()
    {
        var window = ImGuiP.FindWindowByName(WindowName);
        IsActuallyCollapsed = !window.IsNull && window.Collapsed;
    }

    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        var contentTop = windowPos.Y + ImGui.GetFrameHeight();
        ScenarioPanelAnchor = new Vector2(windowPos.X, contentTop);
        _windowChromeHeight = contentTop - windowPos.Y;
        ScenarioPanelHeight = windowPos.Y + ImGui.GetWindowSize().Y - contentTop;
        RightPanelAnchor = new Vector2(windowPos.X + ImGui.GetWindowSize().X, contentTop);
        if (ShowingMultiplayerSetup)
            multiplayerUi.DrawSetupScreen(() => _showMultiplayerSetup = false);
        else
            DrawMainContent();
    }

    // Size the left panel to the widest scenario label so names never clip as scenarios are added.
    internal float ScenarioPanelWindowWidth()
    {
        var style = ImGui.GetStyle();
        var widest = 0f;
        foreach (var zone in Plugin.Game.Catalog.Zones)
        {
            widest = Math.Max(widest, ImGui.CalcTextSize(zone.Name).X + style.IndentSpacing);
            foreach (var phase in Plugin.Game.Catalog.PhasesOf(zone))
                foreach (var scenario in Plugin.Game.Catalog.ScenariosOf(phase))
                    widest = Math.Max(widest, ImGui.CalcTextSize(scenario.DisplayName()).X + style.IndentSpacing);
        }
        var measured = widest
            + style.FramePadding.X * 2
            + style.CellPadding.X * 2
            + style.ScrollbarSize
            + ScenarioButtonExtraPadding * 2 * ImGuiHelpers.GlobalScale;
        var contentWidth = Math.Max(180f * ImGuiHelpers.GlobalScale, measured);
        return contentWidth + style.WindowPadding.X * 2;
    }

    internal void DrawScenariosPanel()
    {
        // Switching scenarios inside a loaded zone skips the zone reload, so another zone's
        // scenario would run on the wrong territory.
        var lockedZone = Plugin.Game.World.Map.IsInInstance ? _selectedScenario?.Phase.Zone : null;
        ImGui.TextUnformatted(lockedZone?.Name ?? "Scenarios");
        ImGui.Separator();

        if (lockedZone != null)
        {
            var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
            DrawZoneScenarios(lockedZone, right);
            return;
        }
        if (!ImGui.BeginTabBar("##scenario-categories")) return;
        foreach (var category in Plugin.Game.Catalog.Zones.Select(z => z.Category).Distinct())
        {
            var flags = _pendingCategoryTab == category ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (!ImGui.BeginTabItem($"{category}###scenario-category-{category}", flags)) continue;
            if (_pendingCategoryTab == category) _pendingCategoryTab = null;
            string? lastExpansion = null;
            foreach (var zone in Plugin.Game.Catalog.Zones.Where(z => z.Category == category).OrderBy(ExpansionRank))
            {
                if (zone.Expansion is { } expansion && expansion != lastExpansion)
                {
                    ImGui.TextDisabled(expansion);
                    lastExpansion = expansion;
                }
                DrawZoneSection(zone);
            }
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }

    // Catalog order interleaves expansions; each heading must appear once.
    private static int ExpansionRank(IZone zone)
    {
        var rank = Asu.CatalogView.ExpansionOrder.ToList().IndexOf(zone.Expansion ?? "");
        return rank < 0 ? int.MaxValue : rank;
    }

    private void DrawZoneSection(IZone zone)
    {
        var shouldOpen = _openZone == zone;
        ImGui.SetNextItemOpen(shouldOpen, ImGuiCond.Always);
        var containsSelection = ZoneContains(zone, _selectedScenario);
        var headerColor = BlendColor(
            StyleColor(ImGuiCol.WindowBg),
            StyleColor(ImGuiCol.Header),
            0.72f);
        ImGui.PushStyleColor(ImGuiCol.Header,
            containsSelection ? BlendColor(headerColor, SelectionColor, 0.58f) : headerColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, containsSelection ? SelectionHoveredColor : HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SelectionActiveColor);
        var open = ImGui.CollapsingHeader($"{zone.Name}###scenario-zone-{zone.GetType().FullName}");
        ImGui.PopStyleColor(3);
        if (containsSelection) DrawSelectionAccent();
        var sectionRight = ImGui.GetItemRectMax().X;
        if (open != shouldOpen) _openZone = open ? zone : null;
        if (!open) return;
        ImGui.Indent();
        DrawZoneScenarios(zone, sectionRight);
        ImGui.Unindent();
    }

    private void DrawZoneScenarios(IZone zone, float sectionRight)
    {
        var inSession = InSession;
        var guest = IsGuest;
        var buttonPadding = ImGui.GetStyle().FramePadding;
        buttonPadding.X += ScenarioButtonExtraPadding * ImGuiHelpers.GlobalScale;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, buttonPadding);
        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));
        foreach (var phase in Plugin.Game.Catalog.PhasesOf(zone))
            foreach (var scenario in Plugin.Game.Catalog.ScenariosOf(phase))
            {
                var selected = _selectedScenario == scenario;
                var mpUnsupported = inSession && !scenario.SupportsMultiplayer;
                if (selected) PushSelectedScenarioStyle();
                else PushScenarioHoverStyle();
                // Zone-qualified: two zones can hold same-named scenarios (UMAD and UCOB
                // both have a P5 "Exaflares"), and a shared ImGui id makes the second
                // button unclickable.
                ImGui.PushID(scenario.FullName());
                var buttonWidth = sectionRight - ImGui.GetCursorScreenPos().X;
                ImGui.BeginDisabled(mpUnsupported || guest);
                var clicked = ImGui.Button(scenario.DisplayName(), new Vector2(buttonWidth, 0));
                ImGui.EndDisabled();
                if ((mpUnsupported || guest) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip(guest ? "The host picks the scenario." : "This scenario doesn't support multiplayer.");
                if (selected)
                {
                    DrawSelectionAccent();
                    ImGui.PopStyleColor(3);
                }
                else
                {
                    ImGui.PopStyleColor(2);
                }
                ImGui.PopID();
                if (clicked) SelectScenario(scenario);
            }
        ImGui.PopStyleVar(2);
    }

    private void RestoreSelectedScenario()
    {
        if (Picker.Restore(Plugin.Config.LastSelectedScenario, Plugin.Game.Catalog.Scenarios))
            _openZone = Picker.Scenario!.Phase.Zone;
    }

    private void SelectScenario(IScenario scenario)
    {
        Picker.Select(scenario);
        _openZone = scenario.Phase.Zone;
        _pendingCategoryTab = scenario.Phase.Zone.Category;
    }

    private const string MpDisabledReason = "Disabled in a multiplayer session.";


    private void DrawMainContent()
    {
        var inSession = InSession;
        if (_selectedScenario == null)
        {
            DrawScenarioPanelToggle();
            ImGui.SameLine();
            ImGui.TextDisabled("Select a scenario");
            if (inSession) DrawSession();
            else DrawMultiplayerButton();
            return;
        }

        var game = Plugin.Game;
        var mpGuest = IsGuest;
        Picker.Reconcile(inSession, mpGuest);
        if (inSession) game.EventTimeScale = 1f;

        DrawScenarioHeader(game);
        if (inSession) DrawSession();
        DrawPrimaryActions(game, inSession);
        if (!inSession) DrawSoloOption(game);
        DrawLocationHint();
        DrawRunOptions(game, Plugin.Practice, inSession);
        DrawSpeedOption(game, inSession);

        ImGui.Spacing();
        DrawSections(_selectedScenario, inSession, mpGuest);
    }

    // Every frame, Setup expanded or not: both Start buttons read these.
    // In a session the role comes from the Party panel seat, and only the host's region/strat
    // is broadcast and run; each is reset, not just disabled, so a stale pick can't apply.



    private void DrawSections(IScenario scenario, bool inSession, bool mpGuest)
    {
        // In a session the seat comes from the Party panel, so Setup can be left with nothing.
        var hasSetup = !inSession || ScenarioPicker.HasStratChoice(scenario) || ScenarioPicker.HasWaymarkChoice(scenario);
        if (hasSetup)
        {
            if (ImGui.TreeNodeEx("Setup###scenario-setup-v3",
                    ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.FramePadding))
            {
                ScenarioSetupUi.DrawSetupGrid(Picker, inSession, mpGuest);
                ImGui.TreePop();
            }
            ImGui.Spacing();
        }

        if (ImGui.TreeNodeEx("Scenario settings###scenario-config-v3",
                ImGuiTreeNodeFlags.FramePadding))
        {
            if (inSession)
                multiplayerUi.DrawScenarioSettings(scenario);
            else
            {
                scenario.DrawSettings();
                scenario.DrawMultiplayerSettings();
            }
            ImGui.TreePop();
        }

#if DEBUG
        ImGui.Spacing();
        if (ImGui.TreeNodeEx("Debug###debug-v3",
                ImGuiTreeNodeFlags.FramePadding))
        {
            DebugSection.Draw(Picker, debugMenu, inSession);
            ImGui.TreePop();
        }
#endif
    }

    private void DrawScenarioHeader(AsuMech.Core.Game.Game game)
    {
        var uiScale = ImGuiHelpers.GlobalScale;
        var contentStart = ImGui.GetCursorScreenPos();
        var scenario = _selectedScenario!;

        DrawScenarioPanelToggle();
        ImGui.SameLine();
        ImGui.TextDisabled($"{scenario.Phase.Zone.Name} —");
        ImGui.SameLine(0f, 4f * uiScale);
        ImGui.TextUnformatted(scenario.DisplayName());
        DrawRunStatus(game);

        var dividerColor = StyleColor(ImGuiCol.TextDisabled);
        dividerColor.W *= 0.35f;
        var dividerY = contentStart.Y + ImGui.GetFrameHeight();
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(ImGui.GetWindowPos().X, dividerY),
            new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowSize().X, dividerY),
            ImGui.GetColorU32(dividerColor),
            1f * uiScale);
        ImGui.SetCursorScreenPos(new Vector2(
            contentStart.X,
            dividerY + ImGui.GetStyle().ItemSpacing.Y));
    }

    // Left-aligned after the name: a right-aligned widget isn't always given the room to draw.
    private void DrawRunStatus(AsuMech.Core.Game.Game game)
    {
        var (label, color) = RunStatus(game);
        ImGui.SameLine(0f, 8f * ImGuiHelpers.GlobalScale);
        ImGui.TextColored(color, $"· {label}");
        if (game.LastRun is not { Issues.Count: > 0 } run || !ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        foreach (var issue in run.Issues)
            ImGui.TextUnformatted($"{issue.Time,6:F1}s  {issue.Description}");
        ImGui.EndTooltip();
    }

    // The live run's state, else how the selected scenario's last run ended.
    private (string Label, Vector4 Color) RunStatus(AsuMech.Core.Game.Game game)
    {
        var idle = ("Idle", StyleColor(ImGuiCol.TextDisabled));
        if (game.LastRun is not { } run) return RunActive ? ("Running", RunningColor) : idle;
        if (run.Ended && run.Scenario != _selectedScenario) return idle;
        var status = run.Status;
        if (IsFailed(status)) return ("Failed", FailedColor);
        return status switch
        {
            SessionStatus.Succeeded => ("Cleared", RunningColor),
            SessionStatus.CompletedWithMistake => ("Cleared with mistakes", MistakeColor),
            _ when run.Ended => ("Stopped", StyleColor(ImGuiCol.TextDisabled)),
            SessionStatus.RunningWithMistake => ("Running with mistakes", MistakeColor),
            _ => ("Running", RunningColor),
        };
    }

    private static bool IsFailed(SessionStatus status)
        => status is SessionStatus.Failed or SessionStatus.Frozen or SessionStatus.CompletedFailed;

    // A run still playing, as opposed to one frozen or finished and only left standing.
    private static bool RunLive(AsuMech.Core.Game.Game game)
        => game.Status is SessionStatus.Running or SessionStatus.RunningWithMistake or SessionStatus.Failed;

    private void DrawScenarioPanelToggle()
    {
        if (DrawQuietIconButton(
                "scenario-panel-toggle",
                FontAwesomeIcon.Columns,
                active: ScenarioPanel.RequestedOpen))
            ScenarioPanel.ToggleRequested();
    }

    private static bool DrawQuietIconButton(string id, FontAwesomeIcon icon, bool active = false)
    {
        var button = StyleColor(ImGuiCol.Button);
        if (active)
        {
            var activeColor = StyleColor(ImGuiCol.ButtonActive);
            button = BlendColor(button, activeColor, 0.72f);
            button.W = MathF.Max(button.W, activeColor.W * 0.9f);
        }
        else
        {
            button.W *= 0.42f;
        }
        var hovered = StyleColor(ImGuiCol.ButtonHovered);
        if (active)
            hovered = BlendColor(hovered, StyleColor(ImGuiCol.ButtonActive), 0.55f);
        else
            hovered.W *= 0.78f;
        ImGui.PushStyleColor(ImGuiCol.Button, button);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        if (active)
        {
            var border = AdjustColor(StyleColor(ImGuiCol.ButtonActive), 1.18f);
            border.W = 0.9f;
            ImGui.PushStyleColor(ImGuiCol.Border, border);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f * ImGuiHelpers.GlobalScale);
        }
        ImGui.PushStyleVar(
            ImGuiStyleVar.FramePadding,
            new Vector2(5f, 2f) * ImGuiHelpers.GlobalScale);
        var clicked = ImGuiComponents.IconButton(id, icon);
        ImGui.PopStyleVar(active ? 2 : 1);
        ImGui.PopStyleColor(active ? 3 : 2);
        return clicked;
    }

    private void DrawPrimaryActions(AsuMech.Core.Game.Game game, bool inSession)
    {
        var uiScale = ImGuiHelpers.GlobalScale;
        var actionSize = new Vector2(140f * uiScale, 32f * uiScale);
        if (!inSession)
            DrawStartButton(game, actionSize);
        else if (RunActive)
        {
            if (DrawSemanticButton("Stop", actionSize, StopColor))
                plugin.Runs.Reset();
        }
        else
            multiplayerUi.DrawStartButton(actionSize, Selection);

        if (!inSession && RunLive(game))
        {
            ImGui.SameLine();
            if (DrawSemanticButton("Stop###stop", actionSize, StopRunColor))
                plugin.Runs.Reset();
        }
        else if (game.World.Map.IsInInstance)
        {
            ImGui.SameLine();
            if (DrawSemanticButton(inSession ? "Return to inn###leave-instance" : "Leave###leave-instance", actionSize, StopColor))
                plugin.Runs.Leave();
            if (inSession && ImGui.IsItemHovered())
                ImGui.SetTooltip("Takes the whole party back to the inn. Everyone stays in the session.");
        }
        else if (!inSession && !RunActive)
        {
            DrawMultiplayerButton(actionSize);
        }
    }

    private void DrawSession()
    {
        multiplayerUi.DrawSessionStrip(PartyPanel);
        multiplayerUi.DrawSessionMessages(Selection);
    }

    // Sessions start from the inn, like any scenario.
    private void DrawMultiplayerButton(Vector2 size = default)
    {
#if DEBUG
        if (!ZoneControl.IsInInn() || Plugin.Game.World.Map.IsInInstance) return;
        ImGui.SameLine();
        if (ImGui.Button("Multiplayer", size)) _showMultiplayerSetup = true;
#endif
    }

    private bool SoloSelected => _selectedScenario is { SupportsSolo: true } && Picker.Solo;

    // A start replaces whatever run is standing, so it needs no Stop first.
    private void DrawStartButton(AsuMech.Core.Game.Game game, Vector2 size)
    {
        var solo = SoloSelected;
        var selection = Selection;
        var refusal = plugin.Runs.StartRefusal(selection, solo);
        var restart = game.ActiveScenario is { } active && active == _selectedScenario;
        var label = plugin.Runs.StartWaitingOn != null ? "Waiting to start..." : restart ? "Restart" : "Start";
        ImGui.BeginDisabled(refusal != null);
        if (DrawSemanticButton($"{label}###start", size, StartColor))
            plugin.Runs.Start(selection, solo);
        ImGui.EndDisabled();
        if (refusal != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(refusal);
    }

    private static bool DrawSemanticButton(string label, Vector2 size, Vector4 color)
    {
        PushSemanticColors(color);
        var clicked = ImGui.Button(label, size);
        PopSemanticColors();
        return clicked;
    }

    internal static void PushSemanticColors(Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AdjustColor(color, 1.16f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, AdjustColor(color, 0.84f));
    }

    internal static void PopSemanticColors() => ImGui.PopStyleColor(3);

    private static void PushSelectedScenarioStyle()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, BlendColor(StyleColor(ImGuiCol.Button), SelectionColor, 0.78f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SelectionHoveredColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SelectionActiveColor);
    }

    private static void PushScenarioHoverStyle()
    {
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, HoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SelectionActiveColor);
    }

    private static void DrawSelectionAccent()
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var accentWidth = 3f * ImGuiHelpers.GlobalScale;
        ImGui.GetWindowDrawList().AddRectFilled(
            min,
            new Vector2(min.X + accentWidth, max.Y),
            ImGui.GetColorU32(SelectionColor));
    }

    private bool ZoneContains(IZone zone, IScenario? scenario) =>
        scenario != null && Plugin.Game.Catalog.PhasesOf(zone).Any(p => Plugin.Game.Catalog.ScenariosOf(p).Contains(scenario));

    private static Vector4 StyleColor(ImGuiCol color) => *ImGui.GetStyleColorVec4(color);

    private static Vector4 BlendColor(Vector4 from, Vector4 to, float amount) =>
        Vector4.Lerp(from, to, amount);

    private static Vector4 AdjustColor(Vector4 color, float factor) => new(
        Math.Clamp(color.X * factor, 0f, 1f),
        Math.Clamp(color.Y * factor, 0f, 1f),
        Math.Clamp(color.Z * factor, 0f, 1f),
        color.W);

    private void DrawSoloOption(AsuMech.Core.Game.Game game)
    {
        if (!_selectedScenario!.SupportsSolo) return;
        var running = RunLive(game);
        ImGui.BeginDisabled(running);
        var solo = Picker.Solo;
        if (ImGui.Checkbox("Solo", ref solo)) Picker.Solo = solo;
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(running
                ? "Stop the current scenario before changing the run mode."
                : "Run without simulated party members or party AI.");
    }

    // God mode and auto-restart are disabled while a session is being set up or is live; forced
    // off, not just disabled, so a stale value can't apply.
    private readonly SpeedControl speedControl = new();

    private void DrawSpeedOption(AsuMech.Core.Game.Game game, bool mpActive)
    {
        if (!Plugin.Config.ShowSpeedControl) return;
        ImGui.BeginDisabled(mpActive);
        ImGui.BeginGroup();
        speedControl.Draw(game, "main-speed");
        ImGui.EndGroup();
        ImGui.EndDisabled();
        if (mpActive && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(MpDisabledReason);
    }

    private static void DrawRunOptions(AsuMech.Core.Game.Game game, PracticeLoop practice, bool mpActive)
    {
        ImGui.Spacing();
        if (mpActive) game.GodMode = false;
        ImGui.BeginDisabled(mpActive);
        var god = game.GodMode;
        if (ImGui.Checkbox("God mode", ref god)) game.GodMode = god;
        ImGui.EndDisabled();
        if (mpActive && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(MpDisabledReason);
        ImGui.SameLine();
        // A host rerunning on its own would desync the session, so this is solo-only.
        if (mpActive) practice.AutoRestart = false;
        ImGui.BeginDisabled(mpActive);
        var autoRestart = practice.AutoRestart;
        if (ImGui.Checkbox("Auto-restart", ref autoRestart)) practice.AutoRestart = autoRestart;
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(mpActive
                ? MpDisabledReason
                : "Restart the same scenario immediately after a successful run. A failed run turns this back off.");
        ImGui.SameLine();
        ImGui.TextDisabled($"Streak: {practice.Streak}");
    }


    private void DrawLocationHint()
    {
        if (ZoneControl.IsInInn()) return;
        ImGui.TextDisabled("Scenarios only run in an inn");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Scenarios can only be started from an inn — return to one to run a scenario.");
    }
}
