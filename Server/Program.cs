using Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameHubData>(new GameHubData());
builder.Services.AddHostedService<IPBroadcastService>();

var app = builder.Build();

app.MapHub<GameHub>("/game");

app.Run();