using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using AnoMech.Multiplayer;
using AnoMech.Network;

namespace AnoMech;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    // LegacyConfig.Import: the plugin whose config this one started from, empty for a fresh one.
    public string ImportedFrom { get; set; } = "";

    
    public bool OpenSimMenuOnInn { get; set; } = true;
    public bool OpenSimMenuOnSupportedInstanceSolo { get; set; } = false;
    public bool EnableEventLogging { get; set; } = false;
    // PullRecorder: a real savage, ultimate or extreme pull, written as a SimTimeline CSV. Zoning
    // into one asks in chat unless the territory is in RecordAlwaysTerritories. The folder is the
    // repo's datalogs/replays when set; empty means the plugin config folder's replays/. A
    // finished recording is also sent to the active relay's /recordings when UploadRecordings is on.
    public bool AskToRecordPulls { get; set; } = true;
    public List<uint> RecordAlwaysTerritories { get; set; } = [];
    public string PullRecordingsDir { get; set; } = "";
    public bool UploadRecordings { get; set; } = true;
    // Override of DefaultRecordingsRelay; both empty means no upload destination.
    public string RecordingsRelayUrl { get; set; } = "";
    public string RecordingsRelayToken { get; set; } = "";

    public (string Url, string? Token) RecordingsDestination()
        => RecordingsRelayUrl.Trim() is { Length: > 0 } url
            ? (url, RecordingsRelayToken.Trim() is { Length: > 0 } token ? token : null)
            : (DefaultRecordingsRelay.Url, DefaultRecordingsRelay.Token.Length > 0 ? DefaultRecordingsRelay.Token : null);
    public bool SuppressBgm { get; set; } = true;
    public bool EnableMechanicResultMarks { get; set; } = false;
    public bool AutoCollapseWhileRunning { get; set; } = false;
    public bool ShowSpeedControl { get; set; } = false;
    public bool UseNewLayout { get; set; } = true;
    public uint AccentColor { get; set; } = DefaultAccentColor;
    public const uint DefaultAccentColor = 0xe8835f;
    public int WindowScalePercent { get; set; } = 100;

    // Configs saved before the new layout became the default stored it as off.
    public int LayoutVersion { get; set; }
    private const int CurrentLayoutVersion = 1;

    public bool MigrateLayout()
    {
        if (LayoutVersion >= CurrentLayoutVersion) return false;
        UseNewLayout = true;
        LayoutVersion = CurrentLayoutVersion;
        return true;
    }
    // New layout scope; empty means all.
    public string SelectedExpansion { get; set; } = "";
    public string SelectedType { get; set; } = "";
    public List<string> RecentScenarios { get; set; } = [];
    public Dictionary<string, int> BestStreaks { get; set; } = [];
    public Dictionary<string, int> Mastery { get; set; } = [];
    // StepStatsStore: every judged try per lesson step and role (result, reaction, distance, cause).
    public Dictionary<string, List<AnoMech.Learn.StepAttempt>> StepStats { get; set; } = [];
    // RollHistory: each finished run's pinnable rolls and whether it was clean, per scenario and strat.
    public Dictionary<string, List<AnoMech.Learn.RollRun>> RollHistory { get; set; } = [];

    // Mitigation training: checks the plan's action for your column against what you pressed.
    public bool EnableMitigationTraining { get; set; } = false;
    // GhostOverlay: a real pull's party (a PullRecorder CSV or an ACT log, one of its pulls) drawn
    // beside the sim's, lined up on the scenario's first cast the pull also has.
    public bool ShowGhost { get; set; } = false;
    public string GhostSource { get; set; } = "";
    public int GhostPull { get; set; } = 1;
    public bool GhostOnlyMine { get; set; } = false;
    // BotInspector: targeting a bot during a run shows the strat call that sent it where it is.
    public bool ShowBotInspector { get; set; } = true;
    // TimelinePanel: the next casts of the embedded timeline, top right, during a lesson.
    public bool ShowTimelinePanel { get; set; } = true;
    // HazardOverlay: the areas about to resolve, drawn on the ground, per lesson level.
    public bool ShowHazardsAssisted { get; set; } = true;
    public bool ShowHazardsGuided { get; set; } = false;
    public bool ShowHazardsObserve { get; set; } = false;
    public bool ToolboxShowWaymarks { get; set; } = true;
    public float ToolboxOpacity { get; set; } = 1f;
    public string SelectedMitPlan { get; set; } = "";
    // Windows.Learn.MitAssist: 0 none, 1 reminder, 2 strict (a miss fails the run).
    public int MitPracticeAssist { get; set; } = 1;
    // With the Reminder assist: a missed mitigation still lets the run go on, but ends the streak.
    public bool MitFailureEndsStreak { get; set; } = false;
    // Bots press the plan's mitigation for their columns, so the damage checks see the party mitigated.
    public bool BotsFollowMitPlan { get; set; } = true;
    // A folder of lesson JSON files that shadow the embedded ones and reload on change (authoring).
    public string LessonDevDir { get; set; } = "";
    public string LastSelectedScenario { get; set; } = "";

    // The player's own multiplayer server (see AnoMech.Relay.Host/README.md), used instead of
    // DefaultRelay while UseCustomRelay is on. Kept while the default is picked, so switching
    // back doesn't mean typing it again.
    public string RelayServerUrl { get; set; } = "";
    public bool UseCustomRelay { get; set; }

    public string ActiveRelayUrl() => UseCustomRelay ? RelayServerUrl.Trim() : DefaultRelay.Url;

    // The servers the active one is grouped with (RelayGroups), as its /info last listed them, so
    // the others stay reachable while it's down. RelayGroupOrigin: the server that listed them.
    public string RelayGroupJson { get; set; } = "";
    public string RelayGroupOrigin { get; set; } = "";

    // Where to host when the active server is in a group, by address; empty for the closest.
    public string HostRelayUrl { get; set; } = "";

    // Optional shared secret some relays require to connect. Bound to the relay it was entered
    // for (RelayTokenOrigin) and only ever sent there, so pointing the plugin at another relay
    // can't hand it over; never logged.
    public string RelayAccessToken { get; set; } = "";
    public string RelayTokenOrigin { get; set; } = "";

    public int EmbeddedRelayPort { get; set; } = 7890;

    public string TokenForRelay(string url)
    {
        // A password saved before it was tied to a relay belongs to the one saved alongside it.
        if (RelayTokenOrigin.Length == 0 && RelayAccessToken.Length != 0) RelayTokenOrigin = OriginOf(RelayServerUrl);
        var origin = OriginOf(url);
        if (origin.Length != 0 && origin == RelayTokenOrigin) return RelayAccessToken;
        return origin.Length != 0 && origin == OriginOf(DefaultRelay.Url) ? DefaultRelay.Token : "";
    }

    public static string OriginOf(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        try { return RelayWire.Origin(url); }
        catch (Exception) { return ""; }
    }

    // Stable per-install multiplayer identity. Each relay gets a credential derived from it
    // (RelayWire.RelayCredential); the relay turns that into the public id it stamps on
    // everything we send, so nobody else can act as us. Reused across Host/Join so a peer who
    // drops and rejoins the same session is the same player and keeps their role.
    public string PeerSecret { get; set; } = "";

    public string EnsurePeerSecret()
    {
        if (RelayWire.IsValidSecret(PeerSecret)) return PeerSecret;
        PeerSecret = RelayWire.NewSecret();
        Save();
        return PeerSecret;
    }
    // Resolve the player's own actions client-side, since the sim firewall blocks
    // the server responses that normally grant them. Sprint is always resolved;
    // this gates everything else (shared actions + per-job kits).
    public bool EnableUserActions { get; set; } = true;

    // Seconds before a player cast finishes during which it can no longer be
    // interrupted by move/jump/cancel — the slidecast window the server's
    // ActionEffect ack opens (measured ~0.5s from replay data). Gated by
    // EnableUserActions.
    public float CastInterruptThreshold { get; set; } = 0.5f;

    // Survivable hits kill a human player whose mitigation is below the hit's threshold; off, they always survive. Gated by EnableUserActions.
    public bool EnableTankMitigation { get; set; } = true;

    // Firewall opcode config — updated automatically by OpcodeUpdater on game version change.
    public uint[] ZoneDownOpcodes { get; set; } = [];
    public string ZoneFirewallGameVersion { get; set; } = "";

    // Safe mode (incoming packet firewall):
    //   true  — only ZoneDownOpcodes pass; cuts you off from server traffic
    //           (no party join/leave updates, no ready checks, no duty pops).
    //   false — all incoming packets pass to the engine. You'll see popups
    //           and party updates, but it's easier to break the sim zone.
    // The send-side firewall stays on either way: nothing the client does in
    // the sim zone leaks back to the server.
    public bool SafeMode { get; set; } = true;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
