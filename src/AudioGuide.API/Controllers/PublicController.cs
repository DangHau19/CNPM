using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using AudioGuide.BLL.Visitors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AudioGuide.API.Controllers;

/// <summary>API công khai cho ứng dụng du khách.</summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ILanguageService _languages;
    private readonly IPlaceService _places;
    private readonly IStatsService _stats;
    private readonly IVisitorService _visitors;

    public PublicController(ILanguageService languages, IPlaceService places, IStatsService stats, IVisitorService visitors)
    {
        _languages = languages;
        _places = places;
        _stats = stats;
        _visitors = visitors;
    }

    [HttpGet("languages")]
    public async Task<ActionResult<List<LanguageDto>>> Languages() => await _languages.GetActiveAsync();

    [HttpGet("places")]
    public async Task<ActionResult<List<PlaceDto>>> Places([FromQuery] string? lang, [FromQuery] double? lat, [FromQuery] double? lng) =>
        await _places.GetPlacesAsync(await ResolveLanguageAsync(lang), lat, lng);

    [HttpGet("places/{id:int}")]
    public async Task<ActionResult<PlaceDto>> Place(int id, [FromQuery] string? lang) =>
        await _places.GetPlaceAsync(id, await ResolveLanguageAsync(lang));

    /// <summary>Địa điểm gần nhất nằm trong bán kính kích hoạt (204 nếu không có).</summary>
    [HttpGet("places/nearby")]
    public async Task<ActionResult<PlaceDto>> Nearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] string? lang)
    {
        var place = await _places.GetNearestInRangeAsync(await ResolveLanguageAsync(lang), lat, lng);
        return place == null ? NoContent() : place;
    }

    [HttpPost("play-log")]
    [EnableRateLimiting("play")]
    public async Task<IActionResult> PlayLog([FromBody] PlayLogRequest request)
    {
        await _stats.LogPlayAsync(request);
        return NoContent();
    }

    /// <summary>Trình duyệt gọi khi mở trang và định kỳ ~30 giây để báo "tôi đang online".</summary>
    [HttpPost("visits/ping")]
    [EnableRateLimiting("ping")]
    public async Task<IActionResult> Ping([FromBody] PingRequest request)
    {
        var client = new ClientInfo(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        await _visitors.PingAsync(request, client);
        return NoContent();
    }

    /// <summary>Không truyền lang → chọn theo header Accept-Language của thiết bị (ngôn ngữ gốc của máy).</summary>
    private async Task<string> ResolveLanguageAsync(string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang)) return lang;
        var active = (await _languages.GetActiveAsync()).Select(l => l.Code).ToList();
        return LanguageNegotiator.Pick(Request.Headers.AcceptLanguage.ToString(), active, "en");
    }
}
