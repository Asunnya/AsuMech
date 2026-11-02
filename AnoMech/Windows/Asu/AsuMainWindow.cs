using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Asu;

public sealed class AsuMainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private IDisposable? theme;

    public AsuMainWindow(Plugin plugin) : base("AsuMech###AsuMainWindow")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 120) * ImGuiHelpers.GlobalScale,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void PreDraw() => theme = AsuTheme.Push();

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("New layout (work in progress).");
        using (AsuTheme.PushTextColor(AsuTheme.Muted))
            ImGui.TextUnformatted("Theme preview");
        bool clicked;
        using (AsuTheme.PushAccentButton())
            clicked = ImGui.Button("Back to the classic layout");
        if (clicked)
            plugin.SetNewLayout(false);
    }
}
