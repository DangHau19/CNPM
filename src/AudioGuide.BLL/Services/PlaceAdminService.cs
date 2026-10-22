using System.Text.RegularExpressions;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IPlaceAdminService
{
    Task<List<PlaceAdminDto>> ListAsync();
    Task<PlaceAdminDto> GetAsync(int id);
    Task<PlaceAdminDto> CreateAsync(PlaceSaveRequest request);
    Task<PlaceAdminDto> UpdateAsync(int id, PlaceSaveRequest request);
    Task DeleteAsync(int id);
    Task<PlaceAdminDto> UpsertTranslationAsync(int placeId, string languageCode, TranslationSaveRequest request);
    Task<PlaceAdminDto> DeleteTranslationAsync(int placeId, string languageCode);
}

public class PlaceAdminService : IPlaceAdminService
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private readonly IPlaceRepository _places;
    private readonly ILanguageRepository _languages;

    public PlaceAdminService(IPlaceRepository places, ILanguageRepository languages)
    {
        _places = places;
        _languages = languages;
    }

    public async Task<List<PlaceAdminDto>> ListAsync() =>
        (await _places.GetAllAsync(onlyActive: false)).Select(ToDto).ToList();

    public async Task<PlaceAdminDto> GetAsync(int id) => ToDto(await RequirePlaceAsync(id));

    public async Task<PlaceAdminDto> CreateAsync(PlaceSaveRequest r)
    {
        Validate(r);
        if (await _places.SlugExistsAsync(r.Slug.Trim(), null)) throw new BusinessException("Slug đã tồn tại.");
        var place = new Place();
        Apply(place, r);
        await _places.AddAsync(place);
        return ToDto(place);
    }

    public async Task<PlaceAdminDto> UpdateAsync(int id, PlaceSaveRequest r)
    {
        Validate(r);
        var place = await RequirePlaceAsync(id);
        if (await _places.SlugExistsAsync(r.Slug.Trim(), id)) throw new BusinessException("Slug đã tồn tại.");
        Apply(place, r);
        place.UpdatedAt = DateTime.UtcNow;
        await _places.SaveAsync();
        return ToDto(place);
    }

    public async Task DeleteAsync(int id) => await _places.DeleteAsync(await RequirePlaceAsync(id));

    public async Task<PlaceAdminDto> UpsertTranslationAsync(int placeId, string languageCode, TranslationSaveRequest r)
    {
        var place = await RequirePlaceAsync(placeId);
        var code = (languageCode ?? "").Trim().ToLowerInvariant();
        if (await _languages.FindAsync(code) == null) throw new BusinessException("Ngôn ngữ không tồn tại.");

        var name = (r.Name ?? "").Trim();
        var text = (r.NarrationText ?? "").Trim();
        if (name.Length == 0 || name.Length > 200) throw new BusinessException("Tên địa điểm bắt buộc (tối đa 200 ký tự).");
        if (text.Length == 0 || text.Length > 4000) throw new BusinessException("Nội dung thuyết minh bắt buộc (tối đa 4000 ký tự).");
        var audio = string.IsNullOrWhiteSpace(r.AudioUrl) ? null : r.AudioUrl.Trim();
        if (audio != null && !(audio.StartsWith("http://") || audio.StartsWith("https://") || audio.StartsWith("/")))
            throw new BusinessException("AudioUrl phải bắt đầu bằng http://, https:// hoặc /.");

        var t = place.Translations.FirstOrDefault(x => x.LanguageCode == code);
        if (t == null)
        {
            t = new PlaceTranslation { LanguageCode = code };
            place.Translations.Add(t);
        }
        t.Name = name;
        t.NarrationText = text;
        t.AudioUrl = audio;
        place.UpdatedAt = DateTime.UtcNow;
        await _places.SaveAsync();
        return ToDto(place);
    }

    public async Task<PlaceAdminDto> DeleteTranslationAsync(int placeId, string languageCode)
    {
        var place = await RequirePlaceAsync(placeId);
        var t = place.Translations.FirstOrDefault(x => x.LanguageCode == (languageCode ?? "").Trim().ToLowerInvariant())
                ?? throw new NotFoundException("Không tìm thấy bản dịch.");
        await _places.DeleteTranslationAsync(t);
        place.Translations.Remove(t);
        return ToDto(place);
    }

    private async Task<Place> RequirePlaceAsync(int id) =>
        await _places.FindAsync(id) ?? throw new NotFoundException("Không tìm thấy địa điểm.");

    private static void Validate(PlaceSaveRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Slug) || !SlugRegex.IsMatch(r.Slug.Trim()) || r.Slug.Trim().Length > 100)
            throw new BusinessException("Slug chỉ gồm chữ thường, số và dấu gạch ngang (vd: dinh-doc-lap).");
        if (r.Latitude is < -90 or > 90) throw new BusinessException("Vĩ độ phải trong khoảng -90 đến 90.");
        if (r.Longitude is < -180 or > 180) throw new BusinessException("Kinh độ phải trong khoảng -180 đến 180.");
        if (r.RadiusMeters is < 20 or > 2000) throw new BusinessException("Bán kính kích hoạt phải từ 20 đến 2000 mét.");
        if (string.IsNullOrWhiteSpace(r.Address) || r.Address.Length > 300) throw new BusinessException("Địa chỉ bắt buộc (tối đa 300 ký tự).");
        if (!string.IsNullOrWhiteSpace(r.ImageUrl) && r.ImageUrl.Length > 500) throw new BusinessException("ImageUrl tối đa 500 ký tự.");
    }

    private static void Apply(Place p, PlaceSaveRequest r)
    {
        p.Slug = r.Slug.Trim();
        p.Latitude = r.Latitude;
        p.Longitude = r.Longitude;
        p.RadiusMeters = r.RadiusMeters;
        p.Address = r.Address.Trim();
        p.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
        p.IsActive = r.IsActive;
    }

    private static PlaceAdminDto ToDto(Place p) => new(
        p.Id, p.Slug, p.Latitude, p.Longitude, p.RadiusMeters, p.ImageUrl, p.Address, p.IsActive,
        p.Translations.OrderBy(t => t.LanguageCode)
            .Select(t => new TranslationDto(t.LanguageCode, t.Name, t.NarrationText, t.AudioUrl)).ToList());
}
