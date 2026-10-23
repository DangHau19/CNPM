using AudioGuide.BLL.Translation;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IAutoTranslateService
{
    bool IsConfigured { get; }

    /// <summary>
    /// Dịch tên + nội dung thuyết minh từ ngôn ngữ nguồn (mặc định tiếng Việt) sang các ngôn ngữ đang kích hoạt còn lại.
    /// KHÔNG lưu vào database: chỉ trả bản dịch để admin xem lại, chỉnh sửa rồi mới lưu.
    /// </summary>
    Task<AutoTranslateResult> TranslateAsync(AutoTranslateRequest request, CancellationToken ct = default);
}

public class AutoTranslateService : IAutoTranslateService
{
    private const int MaxNameLength = 200;
    private const int MaxTextLength = 4000;

    private readonly ITextTranslator _translator;
    private readonly ILanguageRepository _languages;

    public AutoTranslateService(ITextTranslator translator, ILanguageRepository languages)
    {
        _translator = translator;
        _languages = languages;
    }

    public bool IsConfigured => _translator.IsConfigured;

    private record Outcome(string Code, TranslationDto? Dto, string? Error);

    public async Task<AutoTranslateResult> TranslateAsync(AutoTranslateRequest r, CancellationToken ct = default)
    {
        if (!_translator.IsConfigured)
            throw new BusinessException("Dịch tự động đang tắt (chưa cấu hình dịch vụ dịch).");

        var source = (r.SourceLanguage ?? "vi").Trim().ToLowerInvariant();
        var name = (r.Name ?? "").Trim();
        var text = (r.NarrationText ?? "").Trim();
        if (name.Length == 0 || name.Length > MaxNameLength)
            throw new BusinessException($"Hãy nhập tên địa điểm tiếng Việt trước (tối đa {MaxNameLength} ký tự).");
        if (text.Length == 0 || text.Length > MaxTextLength)
            throw new BusinessException($"Hãy nhập nội dung thuyết minh tiếng Việt trước (tối đa {MaxTextLength} ký tự).");

        var active = await _languages.GetAllAsync(onlyActive: true);
        if (!active.Any(l => l.Code == source))
            throw new BusinessException("Ngôn ngữ nguồn không tồn tại hoặc đang tắt.");

        var wanted = (r.TargetLanguages ?? new List<string>())
            .Select(c => (c ?? "").Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .ToHashSet();
        var targets = active.Where(l => l.Code != source && (wanted.Count == 0 || wanted.Contains(l.Code))).ToList();
        if (targets.Count == 0)
            throw new BusinessException("Không có ngôn ngữ đích nào để dịch (hãy bật thêm ngôn ngữ ở tab Ngôn ngữ).");

        // Các ngôn ngữ dịch song song cho nhanh; mỗi ngôn ngữ lỗi riêng không làm hỏng các ngôn ngữ khác.
        var outcomes = await Task.WhenAll(targets.Select(lang => TranslateOneAsync(lang.Code, source, name, text, ct)));

        var translations = outcomes.Where(o => o.Dto != null).Select(o => o.Dto!).ToList();
        var failed = outcomes.Where(o => o.Dto == null).Select(o => o.Code).ToList();
        var error = outcomes.FirstOrDefault(o => o.Error != null)?.Error;
        return new AutoTranslateResult(translations, failed, error);
    }

    private async Task<Outcome> TranslateOneAsync(string code, string source, string name, string text, CancellationToken ct)
    {
        try
        {
            var result = await _translator.TranslateAsync(new[] { name, text }, source, code, ct);
            if (result.Count != 2) throw new BusinessException("Dịch vụ dịch trả kết quả không đúng định dạng.");
            return new Outcome(code, new TranslationDto(code, result[0].Trim(), result[1].Trim(), null), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Outcome(code, null, ex is BusinessException ? ex.Message : "Không gọi được dịch vụ dịch.");
        }
    }
}
