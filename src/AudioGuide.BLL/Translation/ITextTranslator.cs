namespace AudioGuide.BLL.Translation;

/// <summary>Dịch văn bản bằng một dịch vụ bên ngoài (hiện dùng Google Cloud Translation). Tách thành interface để dễ thay dịch vụ khác và dễ test.</summary>
public interface ITextTranslator
{
    /// <summary>False nếu chưa cấu hình API key → tính năng dịch tự động bị tắt.</summary>
    bool IsConfigured { get; }

    /// <summary>Dịch danh sách chuỗi từ <paramref name="sourceLanguage"/> sang <paramref name="targetLanguage"/> (mã ngôn ngữ của hệ thống: vi, en, zh, ja, ko...). Kết quả cùng thứ tự với đầu vào.</summary>
    Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default);
}
