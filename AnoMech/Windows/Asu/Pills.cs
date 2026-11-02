using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

internal static class Pills
{
    // A rounded on/off button, accent when on. Returns true when clicked.
    public static bool Toggle(string label, bool on, bool disabled = false)
    {
        bool clicked;
        ImGui.BeginDisabled(disabled);
        using (AsuTheme.PushPill())
        using (on ? AsuTheme.PushAccentButton() : null)
            clicked = ImGui.Button($"{(on ? "● " : "○ ")}{label}");
        ImGui.EndDisabled();
        return clicked;
    }
}
