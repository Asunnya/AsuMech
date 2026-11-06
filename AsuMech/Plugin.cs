using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsuMech.Core;
using AsuMech.Core.Game;
using AsuMech.Core.Native.Implementations;
using AsuMech.Core.Native.Implementations.Interop;
using AsuMech.Core.Native.Implementations.Pointers;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Multiplayer;
using AsuMech.Scenarios;
using AsuMech.Windows;
using AsuMech.Windows.Asu;
using Dalamud.Game.Command;
using Dalamud.Game.DutyState;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace AsuMech;

public sealed class Plugin : IAsyncDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IToastGui ToastGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName = "/anomech"; // rename:keep
    private const string CommandAlias = "/ano";
    private const string AsuMechCommand = "/asumech";
    private const string AsuMechAlias = "/asu";
    private bool commandsRegistered;

    internal static Configuration Config { get; private set; } = null!;
    internal static Game Game { get; private set; } = null!;
    internal static PracticeLoop Practice { get; private set; } = null!;
    internal static DrillTracker Drill { get; private set; } = null!;
    internal static Learn.MasteryStore Mastery { get; private set; } = null!;
    internal static Learn.StepStatsStore StepStats { get; private set; } = null!;
    internal static Learn.RollHistory Rolls { get; private set; } = null!;
    internal static MultiplayerManager Multiplayer { get; private set; } = null!;
    internal static PullRecorder Recorder { get; private set; } = null!;
    internal RunController Runs { get; private set; } = null!;
    internal ScenarioPicker Picker { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("AnoMech"); // rename:keep

    // Plugin-lifetime singletons, static like the PluginServices above so sim code reaches them
    // without a Plugin reference.
    internal static LogManager LogManager { get; private set; } = null!;
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    internal PartyPanelWindow PartyPanel => MainWindow.PartyPanel;
#if DEBUG
    internal DebugMenu DebugMenu => MainWindow.DebugMenu;
#endif
    private AsuMainWindow AsuMainWindow { get; init; }
    private Window MainUi => Config.UseNewLayout ? AsuMainWindow : MainWindow;
    private FailureBanner? FailureBanner { get; init; }
    private Windows.Learn.GuidanceOverlay GuidanceOverlay { get; init; }
    private Windows.Learn.HazardOverlay HazardOverlay { get; init; }
    private Windows.Learn.ToolboxViewer ToolboxViewer { get; init; }
    private Windows.Learn.MitOverlay MitOverlay { get; init; }
    private Windows.Learn.TimelinePanel TimelinePanel { get; init; }
    private Windows.Learn.BotInspector BotInspector { get; init; }
    internal Windows.Learn.RollSelectorWindow RollSelector { get; init; } = null!;
    private Windows.Learn.GhostOverlay GhostOverlay { get; init; }
    private Windows.Learn.MitTraining MitTraining { get; init; }
    internal Windows.Learn.MitReportWindow MitReport { get; init; }
    internal Windows.Learn.LessonReportWindow LessonReport { get; init; }
    internal Windows.Learn.LessonAuthoringWindow LessonAuthoring { get; init; }
    internal Windows.Learn.StepDrillRunner StepDrill { get; init; }
#if DEBUG
    private DamageDebugWindow DamageDebugWindow { get; init; }
#endif

    private OpcodeUpdater? opcodeUpdater;

    public Plugin()
    {
        // First, so every subsequent construction step's own logging is captured from the start.
        DiagnosticLog.Initialize();
        try
        {
            var imported = LegacyConfig.Import(PluginInterface.ConfigFile, PluginInterface.ConfigDirectory);
            Config = imported ?? PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            if (imported != null) Config.Save();
            if (Config.MigrateLayout()) Config.Save();

            LogManager = new LogManager();
            if (Config.EnableEventLogging) LogManager.Open();

            GameNatives.Install();
            opcodeUpdater = new OpcodeUpdater();
            Game = new Game();
            Drill = new DrillTracker(Config.BestStreaks, Config.Save);
            Mastery = new Learn.MasteryStore(Config.Mastery, Config.Save);
            StepStats = new Learn.StepStatsStore(Config.StepStats, Config.Save);
            Rolls = new Learn.RollHistory(Config.RollHistory, Config.Save);
            // The rolls are read at the end, where the run's State still holds them.
            Game.RunFinished += clean =>
            {
                if (Game.LastRun is not { } finished || Game.Mode != SessionMode.Local) return;
                var strat = finished.SelectedAi is { } i && i >= 0 && i < finished.Scenario.AiStrats.Count ? finished.Scenario.AiStrats[i] : null;
                try { Rolls.Record(Learn.RollHistory.Key(finished.Scenario, strat), Learn.RollHistory.Pinnable(finished.Scenario), clean); }
                catch (Exception e) { DiagnosticLog.Warn($"[RollHistory] {e.Message}"); }
            };
            // Before PracticeLoop subscribes: a reached target must turn Auto-restart off before it restarts.
            IScenario? drillScenario = null;
            Game.RunStarted += (p, _) => drillScenario = p.Scenario;
            Game.RunFinished += clean =>
            {
                if (drillScenario is { } s && Drill.RecordFinish(s.FullName(), clean, Practice.Streak, Practice.Counting))
                    Practice.AutoRestart = false;
            };
            Practice = new PracticeLoop(Game);
            Multiplayer = new MultiplayerManager();
            Runs = new RunController(Game, Multiplayer);
            if (Config.EnableUserActions) Natives.UserActions.Enable();
            Picker = new ScenarioPicker(scenario =>
            {
                Config.LastSelectedScenario = scenario.FullName();
                Config.RecentScenarios = ScenarioSearch.PushRecent(Config.RecentScenarios, scenario.FullName());
                Config.Save();
            });
            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            AsuMainWindow = new AsuMainWindow(this);
            if (Config.UseNewLayout) PartyPanel.Host = AsuMainWindow;

            windowSystem.AddWindow(ConfigWindow);
            windowSystem.AddWindow(MainWindow);
            windowSystem.AddWindow(AsuMainWindow);
            windowSystem.AddWindow(MainWindow.ScenarioPanel);
            windowSystem.AddWindow(MainWindow.PartyPanel);
            FailureBanner = new FailureBanner();
            windowSystem.AddWindow(FailureBanner);
            // Before the guidance overlay, so the step's spot draws over the hazards.
            HazardOverlay = new Windows.Learn.HazardOverlay();
            windowSystem.AddWindow(HazardOverlay);
            GuidanceOverlay = new Windows.Learn.GuidanceOverlay();
            windowSystem.AddWindow(GuidanceOverlay);
            ToolboxViewer = new Windows.Learn.ToolboxViewer();
            windowSystem.AddWindow(ToolboxViewer);
            MitTraining = new Windows.Learn.MitTraining(Game);
            Windows.Learn.MitTraining.Install(MitTraining);
            MitOverlay = new Windows.Learn.MitOverlay();
            windowSystem.AddWindow(MitOverlay);
            TimelinePanel = new Windows.Learn.TimelinePanel();
            windowSystem.AddWindow(TimelinePanel);
            BotInspector = new Windows.Learn.BotInspector();
            windowSystem.AddWindow(BotInspector);
            RollSelector = new Windows.Learn.RollSelectorWindow(this);
            windowSystem.AddWindow(RollSelector);
            GhostOverlay = new Windows.Learn.GhostOverlay();
            windowSystem.AddWindow(GhostOverlay);
            MitReport = new Windows.Learn.MitReportWindow();
            windowSystem.AddWindow(MitReport);
            LessonReport = new Windows.Learn.LessonReportWindow();
            windowSystem.AddWindow(LessonReport);
            LessonAuthoring = new Windows.Learn.LessonAuthoringWindow(Game);
            windowSystem.AddWindow(LessonAuthoring);
            Learn.LessonLoader.DevDirectory = Config.LessonDevDir;
            StepDrill = new Windows.Learn.StepDrillRunner(this, Game);
#if DEBUG
            DamageDebugWindow = new DamageDebugWindow(this);
            windowSystem.AddWindow(DamageDebugWindow);
#endif

            if (Config.OpenSimMenuOnInn && ZoneControl.IsInInn())
                MainUi.IsOpen = true;

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Old name of /asumech"
            });
            CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
            {
                HelpMessage = "Old name of /asu"
            });
            CommandManager.AddHandler(AsuMechCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open AsuMech. Subcommands: config, mp, start, reset, leave, rec, rec stop, rolls"
            });
            CommandManager.AddHandler(AsuMechAlias, new CommandInfo(OnCommand)
            {
                HelpMessage = "Alias for /asumech"
            });
            commandsRegistered = true;

            PluginInterface.UiBuilder.Draw += windowSystem.Draw;
            Framework.Update += OnFrameworkUpdate;

            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
            ClientState.TerritoryChanged += OnTerritoryChanged;
            ClientState.Logout += OnLogout;
            DutyState.DutyStarted += OnDutyStarted;
            DutyState.DutyWiped += OnDutyWiped;
            DutyState.DutyCompleted += OnDutyCompleted;

            // Initialize Pointers
            EventObjectManagerPointers.Initialize();
            EventObjectPointers.Initialize();
            GameMainPointers.Initialize();
            ModelContainerPointers.Initialize();
            PacketDispatcherPointers.Initialize();
            ResolvedStringPointers.Initialize();
            // After the pointers: it hooks the handlers they resolve.
            Recorder = new PullRecorder();

            Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
            // The diagnostic log's own header is written before any hook exists, so it alone
            // doesn't prove a load succeeded.
            DiagnosticLog.Info("[Plugin] constructed OK -- all hooks and pointers initialized.");
        }
        catch (Exception)
        {
            // Dalamud never disposes an instance whose constructor threw; the log writer's open
            // handle would then block every later load from rotating the active log.
            Log.Warning("[Plugin] Load failed during construction -- tearing down partial state.");
            try
            {
                Dispose();
            }
            catch (Exception teardown)
            {
                Log.Warning($"[Plugin] Partial teardown after failed load threw: {teardown.Message}");
            }
            throw;
        }
    }

    public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Dalamud keeps this plugin's services and hooks until this completes, so an unload during a
    // stay leaves it as the Leave button does and frames tick on until the guarded lift is done.
    public async ValueTask DisposeAsync()
    {
        var since = Stopwatch.GetTimestamp();
        try
        {
            while (!await Framework.RunOnFrameworkThread(() => DisposeOnceOutOfStay(since)))
                await Framework.DelayTicks(1);
        }
        catch (OperationCanceledException)
        {
            // The game began closing mid-wait.
            await Framework.RunOnFrameworkThread(Dispose);
        }
    }

    // Leaving and the check share one callback, so no tick can start another stay in between.
    // The lift dies by itself well inside the timeout when it can't verify the inn.
    private bool DisposeOnceOutOfStay(long since)
    {
        if (ZoneControl.Current is { } zone && !Framework.IsFrameworkUnloading
            && Stopwatch.GetElapsedTime(since) < UnloadLiftTimeout)
        {
            if (zone.IsActive)
            {
                Log.Information("[Plugin] Unloaded during a stay -- leaving it before teardown.");
                Game?.LeaveNow();
                zone.Revert();
            }
            if (zone.GuardArmed) return false;
        }
        Dispose();
        return true;
    }

    private static readonly TimeSpan UnloadLiftTimeout = TimeSpan.FromSeconds(10);

    // Null-tolerant throughout: the constructor's failure path calls this on a half-built
    // instance, where anything past the throwing step was never assigned.
    private void Dispose()
    {
        try
        {
            DisposeSubsystems();
        }
        finally
        {
            // Dalamud disposes this plugin's hooks once Dispose returns or throws, so a run must be
            // stopped or lifted by then even when a step above threw. A no-op after a clean Game.Dispose.
            ZoneControl.Current?.Dispose();
        }

        // Last, so it captures every other subsystem's teardown logging before the DLL unloads.
        DiagnosticLog.Shutdown();
    }

    private void DisposeSubsystems()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        ClientState.Logout -= OnLogout;
        DutyState.DutyStarted -= OnDutyStarted;
        DutyState.DutyWiped -= OnDutyWiped;
        DutyState.DutyCompleted -= OnDutyCompleted;

        windowSystem.RemoveAllWindows();

        TimelineDebug.Shutdown();
        Recorder?.Dispose();
        Multiplayer?.Dispose();
        Game?.Dispose();
        Natives.Dispose();
        opcodeUpdater?.Dispose();
        LogManager?.Dispose();
        ConfigWindow?.Dispose();
        MainWindow?.Dispose();
        AsuMainWindow?.Dispose();
        LessonReport?.Dispose();
        StepDrill?.Dispose();
        Windows.Asu.AsuFonts.Dispose();
        FailureBanner?.Dispose();
        Windows.Learn.LearnSession.Current?.Dispose();
        MitTraining?.Dispose();
#if DEBUG
        DamageDebugWindow?.Dispose();
#endif

        if (commandsRegistered)
        {
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(CommandAlias);
            CommandManager.RemoveHandler(AsuMechCommand);
            CommandManager.RemoveHandler(AsuMechAlias);
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        // FrameDeltaTime, not framework.UpdateDelta: UpdateDelta is wall-clock
        // truncated to whole ms, so summing it drifts. FrameDeltaTime is the
        // full-precision delta the game ticks its own animations with.
        var fw = CSFramework.Instance();
        if (fw == null) return;
        // First: the zone guard must run when Game.Tick is paused or throwing.
        Natives.Tick(fw->FrameDeltaTime);
        // Both ticks reach code driven by whatever a relay sent; neither may take the frame
        // pump down.
        try { Recorder.Tick(); }
        catch (Exception e) { DiagnosticLog.Warn($"[Plugin] Recorder.Tick threw: {e}"); }
        try { Runs.Tick(); }
        catch (Exception e) { DiagnosticLog.Warn($"[Plugin] Runs.Tick threw: {e}"); }
        try { Game.Tick(fw->FrameDeltaTime); }
        catch (Exception e) { DiagnosticLog.Warn($"[Plugin] Game.Tick threw: {e}"); }
#if DEBUG
        try { DamageDebugWindow.Instance?.Tick(fw->FrameDeltaTime); }
        catch (Exception e) { DiagnosticLog.Warn($"[Plugin] DamageDebugWindow.Tick threw: {e}"); }
#endif
        try
        {
            // Every frame, not just while the settings are drawn: peers read what the host has
            // selected whether or not its window is open.
            if (Multiplayer.SessionCode != null)
            {
                Multiplayer.PublishSelectedScenario(Picker.Scenario);
                Multiplayer.PublishScenarioSettings(Picker.Scenario);
            }
            Multiplayer.Tick(fw->FrameDeltaTime);
        }
        catch (Exception e) { DiagnosticLog.Warn($"[Plugin] Multiplayer.Tick threw: {e}"); }
    }

    private void OnTerritoryChanged(uint territory)
    {
        ZoneControl.NoteTerritoryChanged(territory);
        var row = DataManager.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territory);
        var isInn = row?.TerritoryIntendedUse.RowId == 2; // TerritoryIntendedUse.Inn
        if (!isInn)
        {
            var name = row?.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;
            LogManager.LogEnterInstance(territory, name);
        }

        if (!isInn)
        {
            MainUi.IsOpen = false;
            return;
        }
        if (Config.OpenSimMenuOnInn)
            MainUi.IsOpen = true;
    }

    private void OnLogout(int type, int code) => ZoneControl.NoteLogout(type, code);

    private void OnDutyStarted(IDutyStateEventArgs args)
        => LogManager.LogCombatStart(args.TerritoryType.RowId);

    private void OnDutyWiped(IDutyStateEventArgs args)
        => LogManager.LogCombatEnd(args.TerritoryType.RowId, wipe: true);

    private void OnDutyCompleted(IDutyStateEventArgs args)
        => LogManager.LogCombatEnd(args.TerritoryType.RowId, wipe: false);

    private void OnCommand(string command, string args)
    {
        switch (args.Trim())
        {
            case "config":
                ConfigWindow.Toggle();
                break;
            case "mp":
            case "multiplayer":
                if (Config.UseNewLayout) AsuMainWindow.OpenMultiplayer();
                else MainWindow.OpenMultiplayer();
                break;
            case "start":
                Runs.Start(Picker.Selection, solo: false);
                break;
            case "start solo":
                Runs.Start(Picker.Selection, solo: true);
                break;
            case "reset":
                Runs.Reset();
                break;
            case "leave":
                Runs.Leave();
                break;
            case "rec":
                Recorder.Arm();
                break;
            case "rec stop":
                Recorder.Disarm();
                break;
            case "rolls":
                RollSelector.Toggle();
                break;
            default:
                MainUi.Toggle();
                break;
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainUi.Toggle();

    // Swaps the open main window in place, so the switch needs no reload.
    public void SetNewLayout(bool on)
    {
        if (Config.UseNewLayout == on) return;
        var wasOpen = MainUi.IsOpen;
        MainUi.IsOpen = false;
        Config.UseNewLayout = on;
        Config.Save();
        PartyPanel.Host = Config.UseNewLayout ? AsuMainWindow : MainWindow;
        MainUi.IsOpen = wasOpen;
    }

    public void OpenSettings()
    {
        ConfigWindow.IsOpen = true;
        ConfigWindow.BringToFront();
    }
}
