using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SpotifyDisplayApi.Services;
using Image = SixLabors.ImageSharp.Image;

[ApiController]
[Route("")]
public class CoverController : ControllerBase
{
    private readonly ISpotifyService _spotify;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http;

    public CoverController(
        ISpotifyService spotify,
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory)
    {
        _spotify = spotify;
        _env = env;
        _http = httpFactory.CreateClient();
    }

    [HttpGet("cover.jpg")]
    public async Task<IActionResult> Get([FromQuery] int width = 240, [FromQuery] int height = 240)
    {
        var imageUrl = await _spotify.GetCurrentImageUrl();

        if (imageUrl is null)
        {
            var path = Path.Combine(_env.ContentRootPath, "spotti.png");
            return File(path, "image/png");
        }

        var imgBytes = await _http.GetByteArrayAsync(imageUrl);

        using var image = Image.Load(imgBytes);

        image.Mutate(x => x.Resize(width, height));

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 90 });

        return File(ms.ToArray(), "image/jpeg");
    }
    
    [HttpGet("cover-stream")]
    public async Task Stream([FromQuery] int width = 240, [FromQuery] int height = 240)
    {
        var imageUrl = await _spotify.GetCurrentImageUrl();

        Response.ContentType = "image/jpeg";
        Response.Headers.CacheControl = "no-store";

        if (imageUrl is null)
        {
            var path = Path.Combine(_env.ContentRootPath, "spotti.png");
            await using var fs = System.IO.File.OpenRead(path);
            await fs.CopyToAsync(Response.Body);
            return;
        }

        await using var spotifyStream = await _http.GetStreamAsync(imageUrl);

        using var image = await Image.LoadAsync(spotifyStream);

        image.Mutate(x => x.Resize(width, height));

        await image.SaveAsync(Response.Body, new JpegEncoder { Quality = 70 });
    }
}