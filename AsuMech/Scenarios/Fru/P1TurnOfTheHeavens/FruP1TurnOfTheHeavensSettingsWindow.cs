using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Fru.P1UtopianSky;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;

public sealed class FruP1TurnOfTheHeavensSettingsWindow
{
    public FruP1TurnOfTheHeavensStateOverrides Overrides { get; } = new();

    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        var solo = !PerRole.SeatsActive;
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup1heavens"))
        {
            if (solo) DrawDuty();
            FruP1UtopianSkySettingsWindow.ElementRow("Cyclonic Break:", "##thcyclonic", Overrides.CyclonicFire, v => Overrides.CyclonicFire = v);
            FruP1UtopianSkySettingsWindow.ElementRow("Turn of the Heavens:", "##thheavens", Overrides.HeavensFire, v => Overrides.HeavensFire = v);
            DrawFlameSide();
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.BoundOfFaith.Clear();
        if (SettingsGrid.Begin("##frup1heavensplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##thseat", editingSeat);
            DrawDuty();
            SettingsGrid.ForcedRecapRow("Tethers set:", Overrides.BoundOfFaith);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void ResetAll()
    {
        Overrides.CyclonicFire = null;
        Overrides.HeavensFire = null;
        Overrides.FlameWest = null;
        Overrides.BoundOfFaith.Mine = null;
    }

    private void DrawDuty()
    {
        var v = Overrides.BoundOfFaith.Effective(editingSeat);
        SettingsGrid.PlayerRow("Bound of Faith:");
        if (ImGui.RadioButton("Auto##thduty", v == null)) Overrides.BoundOfFaith.Set(editingSeat, null);
        ImGui.SameLine();
        if (ImGui.RadioButton("Tethered##thduty", v == BoundOfFaithDuty.Tethered)) Overrides.BoundOfFaith.Set(editingSeat, BoundOfFaithDuty.Tethered);
        ImGui.SameLine();
        if (ImGui.RadioButton("Free##thduty", v == BoundOfFaithDuty.Untethered)) Overrides.BoundOfFaith.Set(editingSeat, BoundOfFaithDuty.Untethered);
    }

    private void DrawFlameSide()
    {
        var v = Overrides.FlameWest;
        SettingsGrid.Row("Halos of flame:");
        if (ImGui.RadioButton("Auto##thflame", v == null)) Overrides.FlameWest = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("West##thflame", v == true)) Overrides.FlameWest = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("East##thflame", v == false)) Overrides.FlameWest = false;
    }
}
