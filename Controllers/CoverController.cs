
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

    private static readonly object _lock = new();
    private static string? _cachedImageUrl;
    private static byte[]? _cachedJpeg;

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

        var etag = "\"" + imageUrl.GetHashCode().ToString("X") + "\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=0";
        
        var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();

        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch.Contains(etag))
        {
            return StatusCode(304);
        }

        byte[] jpeg;

        lock (_lock)
        {
            if (_cachedImageUrl == imageUrl && _cachedJpeg != null)
            {
                Response.Headers.ETag = etag;
                return File(_cachedJpeg, "image/jpeg");
            }
        }

        var imgBytes = await _http.GetByteArrayAsync(imageUrl);

        using var image = Image.Load(imgBytes);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Lanczos3
        }));

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 70 });

        jpeg = ms.ToArray();

        lock (_lock)
        {
            _cachedImageUrl = imageUrl;
            _cachedJpeg = jpeg;
        }

        Response.Headers.ETag = etag;

        return File(jpeg, "image/jpeg");
    }

    [HttpGet("cover-stream")]
    public async Task Stream([FromQuery] int width = 240, [FromQuery] int height = 240)
    {
        var imageUrl = await _spotify.GetCurrentImageUrl();

        Response.ContentType = "image/jpeg";

        if (imageUrl is null)
        {
            var path = Path.Combine(_env.ContentRootPath, "spotti.png");
            await using var fs = System.IO.File.OpenRead(path);
            await fs.CopyToAsync(Response.Body);
            return;
        }

        var etag = "\"" + imageUrl.GetHashCode().ToString("X") + "\"";
        
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=0";

        var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();

        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch.Contains(etag))
        {
            Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        Response.Headers.ETag = etag;

        byte[]? cached = null;

        lock (_lock)
        {
            if (_cachedImageUrl == imageUrl && _cachedJpeg != null)
            {
                cached = _cachedJpeg;
            }
        }

        if (cached != null)
        {
            Response.ContentLength = cached.Length;
            await Response.Body.WriteAsync(cached);
            return;
        }

        var imgBytes = await _http.GetByteArrayAsync(imageUrl);

        using var image = Image.Load(imgBytes);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Lanczos3
        }));

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 70 });

        var jpeg = ms.ToArray();

        lock (_lock)
        {
            _cachedImageUrl = imageUrl;
            _cachedJpeg = jpeg;
        }

        Response.ContentLength = jpeg.Length;
        await Response.Body.WriteAsync(jpeg);
    }
}