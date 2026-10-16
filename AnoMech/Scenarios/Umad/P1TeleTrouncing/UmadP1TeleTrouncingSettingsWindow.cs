using System;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P1TeleTrouncing;

public sealed class UmadP1TeleTrouncingSettingsWindow
{
    public UmadP1TeleTrouncingStateOverrides Overrides { get; } = new();

    // fireAppear/fireWindUp: fire the statue beats right now (null while no run is active), so
    // the props can be exercised without waiting out the timeline each time.
    public void Draw(Action? fireAppear = null, Action? fireWindUp = null)
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##p1teletrouncing"))
        {
            SettingsGrid.FightOnlyNote();
            DrawDifferentArrows();
            TriStateRow("Confused:", "confused", "DPS", "Support", Overrides.DpsGetsConfused, v => Overrides.DpsGetsConfused = v);
            TriStateRow("Gaze:", "gaze", "Inverted", "Normal", Overrides.GazeInverted, v => Overrides.GazeInverted = v);
            TriStateRow("Fire:", "fire", "Stack", "Spread", Overrides.FireIsStack, v => Overrides.FireIsStack = v);
            TriStateRow("Fire orb:", "firelie", "Lie", "Truth", Overrides.FireIsLie, v => Overrides.FireIsLie = v);
            TriStateRow("Thunder orb:", "thunlie", "Lie", "Truth", Overrides.ThunderIsLie, v => Overrides.ThunderIsLie = v);
            TriStateRow("Thunder flip:", "thunflip", "Flipped", "Normal", Overrides.ThunderOrientationFlipped, v => Overrides.ThunderOrientationFlipped = v);
            DrawThunderOffset();
#if DEBUG
            DrawPropKnobs();
            DrawPropBeatRow(fireAppear, fireWindUp);
#endif
            SettingsGrid.End();
        }
    }

#if DEBUG
    private void DrawPropBeatRow(Action? fireAppear, Action? fireWindUp)
    {
        SettingsGrid.Row("Statue beats (debug):");
        ImGui.BeginDisabled(fireAppear == null);
        if (ImGui.Button("Appear now##propappear")) fireAppear?.Invoke();
        ImGui.SameLine();
        if (ImGui.Button("Wind-up now##propwindup")) fireWindUp?.Invoke();
        ImGui.EndDisabled();
    }
#endif

    private void ResetAll()
    {
        Overrides.DpsGetsDifferent = null;
        Overrides.DpsGetsConfused = null;
        Overrides.GazeInverted = null;
        Overrides.FireIsStack = null;
        Overrides.FireIsLie = null;
        Overrides.ThunderIsLie = null;
        Overrides.ThunderOrientationFlipped = null;
        Overrides.ThunderRealOffset = null;
        Overrides.PropsBindDirector = true;
    }

#if DEBUG
    private void DrawPropKnobs()
    {
        SettingsGrid.Row("Statue props (debug):");
        var bind = Overrides.PropsBindDirector;
        if (ImGui.Checkbox("Bind to director##propbind", ref bind)) Overrides.PropsBindDirector = bind;
    }
#endif

    private void DrawDifferentArrows()
    {
        var d = Overrides.DpsGetsDifferent;
        SettingsGrid.Row("Different arrows:");
        if (ImGui.RadioButton("Auto##diffarrows", d == null)) Overrides.DpsGetsDifferent = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("DPS##diffarrows", d == true)) Overrides.DpsGetsDifferent = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Support##diffarrows", d == false)) Overrides.DpsGetsDifferent = false;
    }

    // Auto / <trueLabel> / <falseLabel> tri-state row over a nullable-bool override
    // (null = Auto/random). Mirrors UmadP4KefkaSaysSettingsWindow.RealFakeRow.
    private static void TriStateRow(string label, string id, string trueLabel, string falseLabel, bool? value, Action<bool?> set)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Auto##{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{trueLabel}##{id}", value == true)) set(true);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{falseLabel}##{id}", value == false)) set(false);
    }

    private void DrawThunderOffset()
    {
        var v = Overrides.ThunderRealOffset;
        SettingsGrid.Row("Thunder reals:");
        if (ImGui.RadioButton("Auto##thunoff", v == null)) Overrides.ThunderRealOffset = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Slots 0,2##thunoff", v == 0)) Overrides.ThunderRealOffset = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton("Slots 1,3##thunoff", v == 1)) Overrides.ThunderRealOffset = 1;
    }
}
