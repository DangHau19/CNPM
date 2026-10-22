using System.Text.RegularExpressions;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface ILanguageService
{
    Task<List<LanguageDto>> GetActiveAsync();
    Task<List<LanguageDto>> GetAllAsync();
    Task<LanguageDto> CreateAsync(LanguageSaveRequest request);
    Task<LanguageDto> UpdateAsync(string code, LanguageSaveRequest request);
}

public class LanguageService : ILanguageService
{
    private static readonly Regex CodeRegex = new("^[a-z]{2,3}(-[A-Za-z]{2,4})?$", RegexOptions.Compiled);
    private readonly ILanguageRepository _repo;
    public LanguageService(ILanguageRepository repo) => _repo = repo;

    public async Task<List<LanguageDto>> GetActiveAsync() => (await _repo.GetAllAsync(true)).Select(ToDto).ToList();
    public async Task<List<LanguageDto>> GetAllAsync() => (await _repo.GetAllAsync(false)).Select(ToDto).ToList();

    public async Task<LanguageDto> CreateAsync(LanguageSaveRequest r)
    {
        Validate(r);
        var code = r.Code.Trim().ToLowerInvariant();
        if (await _repo.FindAsync(code) != null) throw new BusinessException("Mã ngôn ngữ đã tồn tại.");
        var lang = new Language();
        Apply(lang, r, code);
        await _repo.AddAsync(lang);
        return ToDto(lang);
    }

    public async Task<LanguageDto> UpdateAsync(string code, LanguageSaveRequest r)
    {
        Validate(r);
        var lang = await _repo.FindAsync(code.Trim().ToLowerInvariant())
                   ?? throw new NotFoundException("Không tìm thấy ngôn ngữ.");
        Apply(lang, r, lang.Code);   // không đổi mã
        await _repo.SaveAsync();
        return ToDto(lang);
    }

    private static void Validate(LanguageSaveRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Code) || !CodeRegex.IsMatch(r.Code.Trim().ToLowerInvariant()))
            throw new BusinessException("Mã ngôn ngữ không hợp lệ (vd: vi, en, fr).");
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.NativeName))
            throw new BusinessException("Tên ngôn ngữ bắt buộc.");
        if (string.IsNullOrWhiteSpace(r.SpeechLocale))
            throw new BusinessException("SpeechLocale bắt buộc (vd: fr-FR).");
    }

    private static void Apply(Language l, LanguageSaveRequest r, string code)
    {
        l.Code = code;
        l.Name = r.Name.Trim();
        l.NativeName = r.NativeName.Trim();
        l.SpeechLocale = r.SpeechLocale.Trim();
        l.IsActive = r.IsActive;
        l.SortOrder = r.SortOrder;
    }

    private static LanguageDto ToDto(Language l) => new(l.Code, l.Name, l.NativeName, l.SpeechLocale, l.IsActive, l.SortOrder);
}
