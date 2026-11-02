using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Asu;

public sealed class AsuMainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

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

    public override void Draw()
    {
        ImGui.TextUnformatted("New layout (work in progress).");
        if (ImGui.Button("Back to the classic layout"))
            plugin.SetNewLayout(false);
    }
}
