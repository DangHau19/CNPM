using AudioGuide.BLL.Geo;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IPlaceService
{
    Task<List<PlaceDto>> GetPlacesAsync(string? lang, double? lat, double? lng);
    Task<PlaceDto> GetPlaceAsync(int id, string? lang);
    Task<PlaceDto?> GetNearestInRangeAsync(string? lang, double lat, double lng);
}

/// <summary>Nghiệp vụ phía người dùng: chọn bản dịch theo ngôn ngữ (fallback en → vi), tính khoảng cách GPS.</summary>
public class PlaceService : IPlaceService
{
    public const string DefaultLanguage = "vi";
    private readonly IPlaceRepository _places;
    private readonly ILanguageRepository _languages;

    public PlaceService(IPlaceRepository places, ILanguageRepository languages)
    {
        _places = places;
        _languages = languages;
    }

    public async Task<List<PlaceDto>> GetPlacesAsync(string? lang, double? lat, double? lng)
    {
        var code = Normalize(lang);
        var languages = await LoadLanguagesAsync();
        var result = new List<PlaceDto>();
        foreach (var p in await _places.GetAllAsync(onlyActive: true))
        {
            var dto = ToDto(p, code, languages, lat, lng);
            if (dto != null) result.Add(dto);
        }
        return lat.HasValue && lng.HasValue
            ? result.OrderBy(x => x.DistanceMeters).ToList()
            : result.OrderBy(x => x.Id).ToList();
    }

    public async Task<PlaceDto> GetPlaceAsync(int id, string? lang)
    {
        var p = await _places.FindAsync(id);
        if (p == null || !p.IsActive) throw new NotFoundException("Không tìm thấy địa điểm.");
        var dto = ToDto(p, Normalize(lang), await LoadLanguagesAsync(), null, null);
        return dto ?? throw new NotFoundException("Địa điểm chưa có nội dung thuyết minh.");
    }

    public async Task<PlaceDto?> GetNearestInRangeAsync(string? lang, double lat, double lng)
    {
        var all = await GetPlacesAsync(lang, lat, lng);
        return all.Where(p => p.DistanceMeters <= p.RadiusMeters).OrderBy(p => p.DistanceMeters).FirstOrDefault();
    }

    private async Task<Dictionary<string, Language>> LoadLanguagesAsync() =>
        (await _languages.GetAllAsync(onlyActive: true)).ToDictionary(l => l.Code, StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string? lang) =>
        string.IsNullOrWhiteSpace(lang) ? DefaultLanguage : lang.Trim().ToLowerInvariant();

    private static PlaceDto? ToDto(Place p, string lang, Dictionary<string, Language> languages, double? lat, double? lng)
    {
        PlaceTranslation? t = null;
        foreach (var code in new[] { lang, "en", DefaultLanguage })
        {
            if (!languages.ContainsKey(code)) continue;
            t = p.Translations.FirstOrDefault(x => string.Equals(x.LanguageCode, code, StringComparison.OrdinalIgnoreCase));
            if (t != null) break;
        }
        t ??= p.Translations.FirstOrDefault(x => languages.ContainsKey(x.LanguageCode));
        if (t == null) return null;

        double? distance = null;
        if (lat.HasValue && lng.HasValue)
            distance = Math.Round(GeoHelper.DistanceMeters(lat.Value, lng.Value, p.Latitude, p.Longitude));

        return new PlaceDto(
            p.Id, p.Slug, p.Latitude, p.Longitude, p.RadiusMeters, p.ImageUrl, p.Address,
            t.Name, t.NarrationText, t.AudioUrl,
            t.LanguageCode, languages[t.LanguageCode].SpeechLocale,
            !string.Equals(t.LanguageCode, lang, StringComparison.OrdinalIgnoreCase),
            distance);
    }
}
