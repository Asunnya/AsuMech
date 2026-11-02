using System;
using System.Numerics;
using AnoMech.Core.Native.Implementations;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Asu;

// The single-window layout: launcher, fight and scenario tabs, options, and a compact strip while
// a run plays. Selection lives in the shared ScenarioPicker, so either layout can take over.
public sealed partial class AsuMainWindow : Window, IDisposable, IPanelHost
{
    private const float MinWidthPx = 380f;
    private const float DefaultWidthPx = 600f;

    private readonly Plugin plugin;
    private readonly MultiplayerUi multiplayerUi;
    private readonly SpeedControl speedControl = new();
    private IDisposable? theme;

    private bool showMultiplayerSetup;
    private bool wasInSession;
    // Last frame's content height: the window is as tall as what it shows, width stays the player's.
    private float contentHeight;

    public AsuMainWindow(Plugin plugin) : base("AsuMech###AsuMainWindow")
    {
        this.plugin = plugin;
        multiplayerUi = new MultiplayerUi(plugin);
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        Size = new Vector2(DefaultWidthPx, 0f);
        SizeCondition = ImGuiCond.FirstUseEver;
        IsOpen = false;
        Plugin.Game.RunStarted += OnRunStarted;
        Plugin.Game.RunFinished += OnRunFinished;
        Plugin.Drill.TargetReached += OnDrillReached;
    }

    private ScenarioPicker Picker => plugin.Picker;
    private IScenario? Selected => Picker.Scenario;
    private static ScenarioCatalog Catalog => Plugin.Game.Catalog;

    public bool IsActuallyCollapsed => false;
    public bool ShowingMultiplayerSetup => showMultiplayerSetup && !RunState.InSession;
    public Vector2 RightPanelAnchor { get; private set; }
    public float ScenarioPanelHeight { get; private set; }

    public void Dispose()
    {
        Plugin.Game.RunStarted -= OnRunStarted;
        Plugin.Game.RunFinished -= OnRunFinished;
        Plugin.Drill.TargetReached -= OnDrillReached;
    }

    internal void OpenMultiplayer()
    {
        IsOpen = true;
        if (!RunState.InSession) showMultiplayerSetup = true;
    }

    public override void PreOpenCheck()
    {
        var inSession = RunState.InSession;
        if (inSession) showMultiplayerSetup = false;
        else if (wasInSession && Plugin.Multiplayer.SessionEndReason != null) showMultiplayerSetup = true;
        wasInSession = inSession;

        if (RunState.IsGuest && Plugin.Multiplayer.TryResolveScenario() is { } hostScenario)
            FollowScenario(hostScenario, persist: false);

        // Inside an instance the window is the only way out, so it can't be closed there.
        var inInstance = Plugin.Game.World.Map.IsInInstance;
        if (inInstance) IsOpen = true;
        ShowCloseButton = !inInstance;
        RespectCloseHotkey = !inInstance;
    }

    public override void PreDraw()
    {
        theme = AsuTheme.Push();
        var scale = ImGuiHelpers.GlobalScale;
        var height = contentHeight > 0f ? contentHeight : 0f;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(MinWidthPx, height / scale),
            MaximumSize = new Vector2(float.MaxValue, contentHeight > 0f ? height / scale : float.MaxValue),
        };
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
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        RightPanelAnchor = new Vector2(pos.X + size.X, pos.Y);
        ScenarioPanelHeight = size.Y;

        if (ShowingMultiplayerSetup)
        {
            multiplayerUi.DrawSetupScreen(() => showMultiplayerSetup = false);
            return;
        }

        var game = Plugin.Game;
        var inSession = RunState.InSession;
        Picker.Reconcile(inSession, RunState.IsGuest);
        if (inSession) game.EventTimeScale = 1f;
        if (!RunState.Active) expandedDuringRun = false;

        DrawToast();
        if (ShowRunStrip)
        {
            DrawRunStrip(game, inSession);
            return;
        }

        DrawTitleRow(game);
        if (inSession)
        {
            multiplayerUi.DrawSessionStrip(plugin.PartyPanel);
            multiplayerUi.DrawSessionMessages(Picker.Selection);
        }
        DrawLauncher(game, inSession);
        DrawBrowser(inSession);
        DrawOptions(game, inSession);
        if (!ZoneControl.IsInInn())
            Muted("Scenarios only run in an inn.");
    }

    private void FollowScenario(IScenario scenario, bool persist)
    {
        if (Selected == scenario) return;
        if (persist) Picker.Select(scenario);
        else Picker.FollowHost(scenario);
        RevealInScope(scenario);
    }

    private static void Muted(string text)
    {
        using (AsuTheme.PushTextColor(AsuTheme.Muted))
            ImGui.TextUnformatted(text);
    }

    private static void Gap() => ImGui.Dummy(new Vector2(0f, AsuTheme.RowGap * 0.25f));
}
