using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

/// <summary>API công khai cho ứng dụng du khách.</summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ILanguageService _languages;
    private readonly IPlaceService _places;
    private readonly IStatsService _stats;

    public PublicController(ILanguageService languages, IPlaceService places, IStatsService stats)
    {
        _languages = languages;
        _places = places;
        _stats = stats;
    }

    [HttpGet("languages")]
    public async Task<ActionResult<List<LanguageDto>>> Languages() => await _languages.GetActiveAsync();

    [HttpGet("places")]
    public async Task<ActionResult<List<PlaceDto>>> Places([FromQuery] string? lang, [FromQuery] double? lat, [FromQuery] double? lng) =>
        await _places.GetPlacesAsync(lang, lat, lng);

    [HttpGet("places/{id:int}")]
    public async Task<ActionResult<PlaceDto>> Place(int id, [FromQuery] string? lang) =>
        await _places.GetPlaceAsync(id, lang);

    /// <summary>Địa điểm gần nhất nằm trong bán kính kích hoạt (204 nếu không có).</summary>
    [HttpGet("places/nearby")]
    public async Task<ActionResult<PlaceDto>> Nearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] string? lang)
    {
        var place = await _places.GetNearestInRangeAsync(lang, lat, lng);
        return place == null ? NoContent() : place;
    }

    [HttpPost("play-log")]
    public async Task<IActionResult> PlayLog([FromBody] PlayLogRequest request)
    {
        await _stats.LogPlayAsync(request);
        return NoContent();
    }
}
