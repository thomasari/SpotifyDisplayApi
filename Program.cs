using SpotifyAPI.Web;
using SpotifyDisplayApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ISpotifyService, SpotifyService>();

var app = builder.Build();

app.MapControllers();

app.Run();