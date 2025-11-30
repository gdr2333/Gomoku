using Microsoft.AspNetCore.SignalR;

namespace Server;

public class GameHub(GameHubData data) : Hub
{
    public async Task EnterQueue()
    {
        if (data.UserInGameGroup.ContainsKey(Context.ConnectionId))
            return;
        lock (data.QueueingUsers)
            data.QueueingUsers.Add(Context.ConnectionId);
        if (data.QueueingUsers.Count >= 2)
            await StartGameAsync();
    }

    private async Task StartGameAsync()
    {
        string user1, user2;
        lock (data.QueueingUsers)
        {
            user1 = data.QueueingUsers[0];
            user2 = data.QueueingUsers[1];
            data.QueueingUsers.RemoveAt(0);
            data.QueueingUsers.RemoveAt(0);
        }
        var gameGuid = Guid.NewGuid().ToString();
        data.UserInGameGroup.TryAdd(user1, gameGuid);
        data.UserInGameGroup.TryAdd(user2, gameGuid);
        await Groups.AddToGroupAsync(user1, gameGuid);
        await Groups.AddToGroupAsync(user2, gameGuid);
        bool isUser1First = Convert.ToBoolean(Random.Shared.Next(0, 1));
        await Clients.Client(user1).SendAsync("GameStart", isUser1First);
        await Clients.Client(user2).SendAsync("GameStart", !isUser1First);
    }

    public async Task DownGamePiece(int row, int col)
    {
        await Clients.Group(data.UserInGameGroup[Context.ConnectionId]).SendAsync("DownGamePiece", row, col);
    }

    public async Task GameEnd()
    {
        if (data.UserInGameGroup.TryRemove(Context.ConnectionId, out var gid))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (data.QueueingUsers)
            if (data.QueueingUsers.Contains(Context.ConnectionId))
            {
                data.QueueingUsers.Remove(Context.ConnectionId);
            }
        if (data.UserInGameGroup.TryGetValue(Context.ConnectionId, out var gid))
        {
            await GameEnd();
            await Clients.Group(gid).SendAsync("GameEndForce");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
