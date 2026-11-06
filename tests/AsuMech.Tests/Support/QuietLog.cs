using System.Collections.Concurrent;
using AsuMech.Relay;

namespace AsuMech.Tests;

internal sealed class QuietLog : IRelayLog
{
    public readonly ConcurrentQueue<string> Lines = new();
    public void Info(string message) => Lines.Enqueue(message);
    public void Warn(string message) => Lines.Enqueue(message);
    public void Detail(string message) => Lines.Enqueue(message);
}
