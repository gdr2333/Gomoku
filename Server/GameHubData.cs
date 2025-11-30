using System.Collections.Concurrent;

namespace Server;

public class GameHubData
{
    public List<string> QueueingUsers { get; } = [];
    public ConcurrentDictionary<string, string> UserInGameGroup { get; } = [];
}
