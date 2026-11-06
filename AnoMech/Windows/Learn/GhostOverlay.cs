using System;
using System.Numerics;
using System.Threading.Tasks;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Learn.Ghost;
using AnoMech.Learn.Mitigation;
using AnoMech.Scenarios;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// A real pull's party walking beside the sim: one ring per player at the place the recording or
// the ACT log had them at this moment of the fight, labelled by seat. A reference, not a key: a real
// clear has its own mistakes and its own rolls.
internal sealed class GhostOverlay : Window
{
    private const float RingYalms = 0.5f;
    private const float RingThicknessPx = 2.5f;

    public static string Status { get; private set; } = "No pull loaded.";

    private (string Path, int Pull, uint Territory)? loadedFor;
    private Task<GhostTrack>? loading;
    private GhostTrack? track;
    private string? alignedFor;

    public GhostOverlay() : base("##AsuMechGhost")
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings
                | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        DisableWindowSounds = true;
    }

    public override void PreOpenCheck()
    {
        var config = Plugin.Config;
        if (!config.ShowGhost || config.GhostSource.Trim().Length == 0 || Plugin.Game.ActiveScenario is not { } scenario)
        {
            IsOpen = false;
            return;
        }
        Load(config.GhostSource.Trim(), Math.Max(1, config.GhostPull), scenario);
        IsOpen = track is { Aligned: true };
    }

    private void Load(string path, int pull, IScenario scenario)
    {
        var zone = scenario.Phase.Zone;
        var key = (path, pull, zone.TerritoryId);
        if (loadedFor != key)
        {
            loadedFor = key;
            track = null;
            alignedFor = null;
            Status = "Loading…";
            var origin = zone.Origin;
            loading = Task.Run(() => GhostTrack.Load(path, zone.TerritoryId, pull, origin));
        }
        if (loading is { IsCompleted: true } done)
        {
            loading = null;
            if (done.IsCompletedSuccessfully) track = done.Result;
            else Status = $"Couldn't load: {done.Exception?.GetBaseException().Message}";
        }
        var name = scenario.GetType().Name;
        if (track is null || alignedFor == name) return;
        alignedFor = name;
        if (MitTimelines.Load(name) is { } timeline && track.Align(timeline))
            Status = $"{track.Lanes.Count} players, aligned on {track.AlignedOn} ({track.Offset:+0.0;-0.0}s into the pull).";
        else
            Status = $"{track.Lanes.Count} players, but none of {scenario.Name}'s casts are in this pull: nothing to line up on.";
    }

    public override void PreDraw()
    {
        Position = Vector2.Zero;
        Size = ImGui.GetIO().DisplaySize;
        PositionCondition = SizeCondition = ImGuiCond.Always;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw() => ImGui.PopStyleVar();

    public override void Draw()
    {
        if (track is not { Aligned: true } ghost) return;
        var world = Plugin.Game.World;
        var now = world.Events.Elapsed;
        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var mine = world.Party.Player is ISimPartyMember seat ? seat.Role : (PartyRole?)null;
        var height = world.Party.Player?.Position.Y ?? 0f;
        foreach (var lane in ghost.Lanes)
        {
            var own = lane.Role is { } r && r == mine;
            if (Plugin.Config.GhostOnlyMine && !own) continue;
            if (ghost.At(lane, now) is not { } at) continue;
            var center = world.Coordinates.ToGlobal(new Vector3(at.Position.X, height, at.Position.Y));
            if (!Plugin.GameGui.WorldToScreen(center, out var screen)) continue;
            var radius = Plugin.GameGui.WorldToScreen(center + new Vector3(RingYalms, 0f, 0f), out var edge) ? MathF.Max(6f * scale, Vector2.Distance(screen, edge)) : 8f * scale;
            var color = AsuTheme.U32((own ? AsuTheme.WaymarkBlue : AsuTheme.Muted) with { W = own ? 0.95f : 0.7f });
            draw.AddCircle(screen, radius, color, 24, RingThicknessPx * scale);
            var facing = center + new Vector3(MathF.Sin(at.Rotation), 0f, MathF.Cos(at.Rotation)) * (RingYalms * 1.8f);
            if (Plugin.GameGui.WorldToScreen(facing, out var tip)) draw.AddLine(screen, tip, color, RingThicknessPx * scale);
            draw.AddText(screen + new Vector2(radius + 2f * scale, -radius), color, lane.Role is { } role ? GhostRoles.Short(role) : lane.Label);
        }
    }
}
