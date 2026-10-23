using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P2LightRampant;

public sealed class FruP2LightRampantSettingsWindow
{
    public FruP2LightRampantStateOverrides Overrides { get; } = new();

    // Which seat the per-player rows are showing. UI state only; never broadcast.
    private PartyRole editingSeat = PartyRole.MainTank;

    // Solo, the player's own pick sits in this panel; a host assigns seats from the Multiplayer
    // window.
    public void Draw()
    {
        var solo = !PerRole.SeatsActive;
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup2lightrampant"))
        {
            if (solo) DrawDuty();
            DrawOrbs();
            DrawBanish();
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.Duty.Clear();
        if (SettingsGrid.Begin("##frup2lightrampantplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##lrseat", editingSeat);
            DrawDuty();
            SettingsGrid.ForcedRecapRow("Duties set:", Overrides.Duty);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    // Solo's own pick only: a host's seat assignments are kept apart.
    private void ResetAll()
    {
        Overrides.NorthOrbsFirst = null;
        Overrides.Pairs = null;
        Overrides.Duty.Mine = null;
    }

    private void DrawDuty()
    {
        var v = Overrides.Duty.Effective(editingSeat);
        SettingsGrid.PlayerRow("duty:");
        if (ImGui.RadioButton("Auto##lrduty", v == null)) Overrides.Duty.Set(editingSeat, null);
        ImGui.SameLine();
        if (ImGui.RadioButton("Tether##lrduty", v == LightRampantDuty.Tether)) Overrides.Duty.Set(editingSeat, LightRampantDuty.Tether);
        ImGui.SameLine();
        if (ImGui.RadioButton("Bait##lrduty", v == LightRampantDuty.Bait)) Overrides.Duty.Set(editingSeat, LightRampantDuty.Bait);
    }

    private void DrawOrbs()
    {
        var v = Overrides.NorthOrbsFirst;
        SettingsGrid.Row("First orbs:");
        if (ImGui.RadioButton("Auto##lrorbs", v == null)) Overrides.NorthOrbsFirst = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("North##lrorbs", v == true)) Overrides.NorthOrbsFirst = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("South##lrorbs", v == false)) Overrides.NorthOrbsFirst = false;
    }

    private void DrawBanish()
    {
        var v = Overrides.Pairs;
        SettingsGrid.Row("Banish III:");
        if (ImGui.RadioButton("Auto##lrbanish", v == null)) Overrides.Pairs = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Pairs##lrbanish", v == true)) Overrides.Pairs = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Spread##lrbanish", v == false)) Overrides.Pairs = false;
    }
}
