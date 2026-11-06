using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Learn;
using AnoMech.Learn.Toolbox;
using AnoMech.Scenarios;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// The selected scenario's rolls and how often the player fails on each value, from the runs they
// finished; "Practise the worst" pins that value for the next runs and leaves every other roll random.
internal sealed class RollSelectorWindow : Window
{
    private object? pinnedOn;
    private IReadOnlyList<string> pinnedNames = [];
    private string? pinnedLabel;

    private readonly Plugin plugin;

    public RollSelectorWindow(Plugin plugin) : base("Rolls###AsuMechRolls")
    {
        this.plugin = plugin;
        Size = new Vector2(520f, 360f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        using var theme = AsuTheme.Push();
        var picker = plugin.Picker;
        if (picker.Scenario is not { } scenario)
        {
            ImGui.TextUnformatted("Pick a scenario first.");
            return;
        }
        var strat = picker.Strat >= 0 && picker.Strat < scenario.AiStrats.Count ? scenario.AiStrats[picker.Strat] : null;
        var key = RollHistory.Key(scenario, strat);
        ImGui.TextUnformatted($"{scenario.FullName()}{(strat is null ? "" : $" · {strat.Name}")}");
        var runs = Plugin.Rolls.Runs(key);
        using (AsuTheme.PushTextColor(AsuTheme.Muted))
            ImGui.TextUnformatted($"{runs.Count} finished run{(runs.Count == 1 ? "" : "s")}, {runs.Count(r => !r.Clean)} failed");
        if (scenario.SettingsOverrides is not { } overrides)
        {
            ImGui.TextWrapped("This scenario has no rolls to pin.");
            return;
        }
        if (pinnedLabel is { } label && ReferenceEquals(pinnedOn, overrides))
        {
            using (AsuTheme.PushTextColor(AsuTheme.Accent)) ImGui.TextUnformatted($"Pinned: {label}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Unpin##rolls")) Unpin();
        }
        var worst = Plugin.Rolls.Worst(key);
        if (worst is { } w)
        {
            using (AsuTheme.PushAccentButton())
                if (ImGui.Button($"Practise the worst: {w.Field} = {w.Value} ({w.FailRate:P0} of {w.Runs})##rolls-worst")) Pin(overrides, w);
        }
        else
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextWrapped($"Nothing to single out yet: a value needs {RollHistory.MinRunsForWorst} runs and a failure.");
        var variants = Plugin.Rolls.Variants(key);
        if (variants.Count == 0 || !ImGui.BeginTable("roll-variants", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp)) return;
        ImGui.TableSetupColumn("Roll");
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupColumn("Failed");
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();
        var row = 0;
        foreach (var v in variants)
        {
            row++;
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(v.Field);
            ImGui.TableNextColumn(); ImGui.TextWrapped(v.Value);
            ImGui.TableNextColumn();
            using (AsuTheme.PushTextColor(v.FailRate >= 0.5f ? AsuTheme.Bad : v.Fails > 0 ? AsuTheme.Accent : AsuTheme.Ok))
                ImGui.TextUnformatted($"{v.Fails}/{v.Runs}");
            ImGui.TableNextColumn();
            if (ImGui.SmallButton($"Pin##roll-{row}")) Pin(overrides, v);
        }
        ImGui.EndTable();
    }

    private void Pin(object overrides, RollVariant variant)
    {
        Unpin();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string> { [variant.Field] = variant.Value });
        if (applied.Count == 0) return;
        pinnedOn = overrides;
        pinnedNames = applied;
        pinnedLabel = $"{variant.Field} = {variant.Value}";
    }

    private void Unpin()
    {
        if (pinnedOn is { } overrides) ExampleOverrides.Lift(overrides, pinnedNames);
        pinnedOn = null;
        pinnedNames = [];
        pinnedLabel = null;
    }
}
