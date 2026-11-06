using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsuMech.Network;

namespace AsuMech.Multiplayer;

// One thumbs up or down to the study relay's POST /feedback.
internal static class LessonFeedbackSender
{
    public sealed record Result(bool Ok, string Message);

    public static async Task<Result> SendAsync(LessonFeedback feedback, string relayUrl, string? accessToken, string credential, CancellationToken ct = default)
    {
        if (!feedback.IsValid) return new Result(false, "nothing valid to send");
        var sent = await StudyRelayPost.SendAsync("feedback", JsonSerializer.Serialize(feedback, LessonFeedback.Json), relayUrl, accessToken, credential, ct).ConfigureAwait(false);
        return new Result(sent.Ok, sent.Ok ? "thanks, sent" : sent.Message.Replace("doesn't take /feedback", "doesn't take feedback"));
    }
}
