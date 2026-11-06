using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AsuMech.Learn;
using AsuMech.Multiplayer;
using AsuMech.Network;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace AsuMech.Windows.Learn;

// Thumbs up/down on a lesson step's text or toolbox picture, sent to the study server. A down
// vote asks for an optional one-line reason first. The player's own last vote is kept in the
// config so the buttons show it.
internal static class LessonVoteButtons
{
    private static readonly ConcurrentQueue<(string Key, int Vote, LessonFeedbackSender.Result Result)> finished = new();
    private static readonly Dictionary<string, string> status = [];
    private static readonly HashSet<string> sending = [];
    private static string comment = "";

    public static string LessonId(Lesson lesson) => $"{lesson.Scenario}|{lesson.Strat}";

    public static void Draw(Lesson lesson, string step, string target)
    {
        Drain();
        var lessonId = LessonId(lesson);
        var key = $"{target}|{lessonId}|{step}";
        var votes = Plugin.Config.LessonVotes;
        var mine = votes.GetValueOrDefault(key);
        var busy = sending.Contains(key);

        ImGui.BeginDisabled(busy);
        using (AsuTheme.PushTextColor(mine > 0 ? AsuTheme.Ok : AsuTheme.Muted))
            if (ImGuiComponents.IconButton($"up##{key}", FontAwesomeIcon.ThumbsUp)) Send(lessonId, step, target, key, 1, "");
        Tooltip(key, target == LessonFeedback.Text ? "This text is clear" : "This picture is right");
        ImGui.SameLine(0f, 2f);
        using (AsuTheme.PushTextColor(mine < 0 ? AsuTheme.Bad : AsuTheme.Muted))
            if (ImGuiComponents.IconButton($"down##{key}", FontAwesomeIcon.ThumbsDown)) { comment = ""; ImGui.OpenPopup($"why##{key}"); }
        Tooltip(key, target == LessonFeedback.Text ? "This text is wrong or unclear" : "This picture is wrong");
        ImGui.EndDisabled();

        ImGui.SetNextWindowSize(new Vector2(280f * AsuTheme.Scale, 0f));
        if (!ImGui.BeginPopup($"why##{key}")) return;
        ImGui.TextWrapped("What's wrong? (optional, no names please)");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextMultiline($"##why{key}", ref comment, LessonFeedback.MaxCommentChars, new Vector2(-1f, ImGui.GetTextLineHeight() * 3f));
        bool send;
        using (AsuTheme.PushAccentButton())
            send = ImGui.Button("Send");
        if (send)
        {
            Send(lessonId, step, target, key, -1, comment.Trim().ReplaceLineEndings(" "));
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private static void Tooltip(string key, string what)
    {
        if (!ImGui.IsItemHovered()) return;
        ImGui.SetTooltip(status.TryGetValue(key, out var s) ? $"{what}\n{s}" : what);
    }

    private static void Send(string lessonId, string step, string target, string key, int vote, string why)
    {
        var config = Plugin.Config;
        var (url, token) = config.RecordingsDestination();
        var feedback = new LessonFeedback(lessonId, Clip(step), target, vote, Clip(why, LessonFeedback.MaxCommentChars), PluginBuildInfo.Version);
        var secret = RelayWire.RelayCredential(config.EnsurePeerSecret(), url);
        sending.Add(key);
        status[key] = "sending…";
        Task.Run(async () => finished.Enqueue((key, vote, await LessonFeedbackSender.SendAsync(feedback, url, token, secret))));
    }

    private static void Drain()
    {
        while (finished.TryDequeue(out var done))
        {
            sending.Remove(done.Key);
            status[done.Key] = done.Result.Message;
            if (!done.Result.Ok) continue;
            Plugin.Config.LessonVotes[done.Key] = done.Vote;
            Plugin.Config.Save();
        }
    }

    private static string Clip(string s, int max = LessonFeedback.MaxIdChars) => s.Length <= max ? s : s[..max];
}
