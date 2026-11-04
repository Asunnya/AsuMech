using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

// Row placement that wraps to the next line instead of drawing over the last item.
internal static class Flow
{
    // SameLine only when an item this wide still fits after the last one.
    public static void SameLineIfFits(float width)
    {
        var lineRight = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        if (ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + width <= lineRight) ImGui.SameLine();
    }

    // Flush right on this line, or flush right on the next one when it would cover the last item.
    public static void SameLineRight(float width)
    {
        if (width <= 0f)
        {
            ImGui.SameLine();
            return;
        }
        var x = ImGui.GetWindowContentRegionMax().X - width;
        var lastEnd = ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().ItemSpacing.X;
        if (x >= lastEnd) ImGui.SameLine(x);
        else if (x > ImGui.GetCursorPosX()) ImGui.SetCursorPosX(x);
    }

    public static float ButtonWidth(string label)
        => ImGui.CalcTextSize(label.Split("##")[0]).X + ImGui.GetStyle().FramePadding.X * 2f;

    public static float TextWidth(string text) => ImGui.CalcTextSize(text).X;
}
