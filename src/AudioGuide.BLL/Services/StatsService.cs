using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IStatsService
{
    Task LogPlayAsync(PlayLogRequest request);
    Task<StatsDto> GetStatsAsync(int days);
}

public class StatsService : IStatsService
{
    private static readonly string[] Triggers = { "gps", "manual" };
    private readonly IPlayLogRepository _logs;
    private readonly IPlaceRepository _places;
    private readonly ILanguageRepository _languages;

    public StatsService(IPlayLogRepository logs, IPlaceRepository places, ILanguageRepository languages)
    {
        _logs = logs;
        _places = places;
        _languages = languages;
    }

    public async Task LogPlayAsync(PlayLogRequest r)
    {
        var trigger = (r.TriggerType ?? "").Trim().ToLowerInvariant();
        if (!Triggers.Contains(trigger)) throw new BusinessException("TriggerType phải là gps hoặc manual.");
        var lang = (r.LanguageCode ?? "").Trim().ToLowerInvariant();
        if (await _languages.FindAsync(lang) == null) throw new BusinessException("Ngôn ngữ không tồn tại.");
        var place = await _places.FindAsync(r.PlaceId);
        if (place == null) throw new NotFoundException("Không tìm thấy địa điểm.");

        await _logs.AddAsync(new PlayLog { PlaceId = place.Id, LanguageCode = lang, TriggerType = trigger });
    }

    public async Task<StatsDto> GetStatsAsync(int days)
    {
        days = Math.Clamp(days, 1, 365);
        var from = DateTime.UtcNow.AddDays(-days);

        var byTrigger = await _logs.CountByTriggerAsync(from);
        var byPlace = await _logs.CountByPlaceAsync(from);
        var byLang = await _logs.CountByLanguageAsync(from);
        var places = (await _places.GetAllAsync(onlyActive: false)).ToDictionary(p => p.Id.ToString());
        var languages = (await _languages.GetAllAsync(onlyActive: false)).ToDictionary(l => l.Code);

        int Count(string key) => byTrigger.FirstOrDefault(x => x.Key == key)?.Count ?? 0;

        var top = byPlace.OrderByDescending(x => x.Count).Take(10)
            .Select(x => new StatItem(x.Key, PlaceLabel(places, x.Key), x.Count)).ToList();
        var langs = byLang.OrderByDescending(x => x.Count)
            .Select(x => new StatItem(x.Key, languages.TryGetValue(x.Key, out var l) ? l.NativeName : x.Key, x.Count)).ToList();

        return new StatsDto(byTrigger.Sum(x => x.Count), Count("gps"), Count("manual"), top, langs);
    }

    private static string PlaceLabel(Dictionary<string, Place> places, string id)
    {
        if (!places.TryGetValue(id, out var p)) return id;
        var t = p.Translations.FirstOrDefault(x => x.LanguageCode == "vi") ?? p.Translations.FirstOrDefault();
        return t?.Name ?? p.Slug;
    }
}
