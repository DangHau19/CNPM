using System.Net;
using System.Text.Json;

namespace AudioGuide.BLL.Translation;

/// <summary>Cấu hình MyMemory. Email (tùy chọn, email thật của bạn) giúp tăng hạn mức miễn phí mỗi ngày; không cần API key.</summary>
public record MyMemoryOptions(string? Email);

/// <summary>
/// Dịch bằng MyMemory (api.mymemory.translated.net) — miễn phí, không cần thẻ hay API key.
/// Giới hạn: mỗi yêu cầu tối đa ~500 byte (nên văn bản được cắt theo câu) và có hạn mức ký tự mỗi ngày
/// (khoảng 5.000 ký tự nếu ẩn danh, khoảng 50.000 nếu kèm email). Chất lượng thấp hơn Google/DeepL nên admin cần xem lại.
/// </summary>
public class MyMemoryTranslator : ITextTranslator
{
    private const string Endpoint = "https://api.mymemory.translated.net/get";
    private const int MaxChunkBytes = 450;
    private const string QuotaMessage = "Dịch vụ dịch miễn phí (MyMemory) đang từ chối vì hết hạn mức hoặc bị gọi quá nhiều. Hãy đợi vài phút hoặc thử lại vào ngày mai, hoặc đặt biến Translation__MyMemoryEmail (email thật của bạn) để tăng hạn mức.";

    private readonly HttpClient _http;
    private readonly string? _email;

    public MyMemoryTranslator(HttpClient http, MyMemoryOptions options)
    {
        _http = http;
        var email = options.Email?.Trim();
        _email = !string.IsNullOrEmpty(email) && email.Contains('@') ? email : null;
    }

    public bool IsConfigured => true;   // không cần API key

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default)
    {
        var results = new List<string>();
        foreach (var text in texts)
        {
            var parts = new List<string>();
            foreach (var chunk in TextChunker.Split(text, MaxChunkBytes))
                parts.Add(await TranslateChunkAsync(chunk, sourceLanguage, targetLanguage, ct));
            results.Add(string.Join(" ", parts));
        }
        return results;
    }

    private async Task<string> TranslateChunkAsync(string chunk, string source, string target, CancellationToken ct)
    {
        var url = $"{Endpoint}?q={Uri.EscapeDataString(chunk)}&langpair={Uri.EscapeDataString(Map(source) + "|" + Map(target))}";
        if (_email != null) url += "&de=" + Uri.EscapeDataString(_email);

        using var response = await _http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new BusinessException(QuotaMessage);
        if (!response.IsSuccessStatusCode)
            throw new BusinessException($"Dịch vụ dịch trả lỗi {(int)response.StatusCode}. Hãy thử lại sau.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var translated = root.TryGetProperty("responseData", out var data) && data.TryGetProperty("translatedText", out var tt)
            ? tt.GetString() ?? ""
            : "";
        var quotaFinished = root.TryGetProperty("quotaFinished", out var qf) && qf.ValueKind == JsonValueKind.True;

        if (quotaFinished || translated.StartsWith("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase) || ReadStatus(root) == 429)
            throw new BusinessException(QuotaMessage);
        if (ReadStatus(root) != 200 || translated.Length == 0)
            throw new BusinessException("Dịch vụ dịch không trả kết quả. Hãy thử lại sau.");

        return WebUtility.HtmlDecode(translated).Trim();   // MyMemory đôi khi trả ký tự dạng &#39; ...
    }

    /// <summary>responseStatus có thể là số hoặc chuỗi tùy trường hợp; không có thì coi là 200.</summary>
    private static int ReadStatus(JsonElement root)
    {
        if (!root.TryGetProperty("responseStatus", out var s)) return 200;
        if (s.ValueKind == JsonValueKind.Number && s.TryGetInt32(out var n)) return n;
        if (s.ValueKind == JsonValueKind.String && int.TryParse(s.GetString(), out var m)) return m;
        return 200;
    }

    /// <summary>Mã ngôn ngữ của hệ thống → mã MyMemory (chỉ "zh" cần đổi thành Hán giản thể).</summary>
    private static string Map(string code) => code.ToLowerInvariant() switch
    {
        "zh" => "zh-CN",
        var c => c
    };
}
