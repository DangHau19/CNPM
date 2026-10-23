using System.Net.Http.Json;
using System.Text.Json;
using AudioGuide.BLL;
using AudioGuide.BLL.Translation;

namespace AudioGuide.API.Translation;

/// <summary>
/// Dịch bằng Google Cloud Translation (Basic, v2) qua REST + API key.
/// Cấu hình: biến môi trường Translation__GoogleApiKey (hoặc Translation:GoogleApiKey trong appsettings).
/// </summary>
public class GoogleTextTranslator : ITextTranslator
{
    private const string Endpoint = "https://translation.googleapis.com/language/translate/v2";

    private readonly HttpClient _http;
    private readonly string? _apiKey;

    public GoogleTextTranslator(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Translation:GoogleApiKey"]?.Trim();
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default)
    {
        if (!IsConfigured) throw new BusinessException("Chưa cấu hình Translation__GoogleApiKey.");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                q = texts,
                source = Map(sourceLanguage),
                target = Map(targetLanguage),
                format = "text"
            })
        };
        request.Headers.Add("X-goog-api-key", _apiKey);   // gửi key qua header để không lộ trong URL/log

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new BusinessException($"Dịch vụ dịch trả lỗi {(int)response.StatusCode}. Hãy kiểm tra API key, đã bật Cloud Translation API và đã gắn tài khoản thanh toán.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var items = doc.RootElement.GetProperty("data").GetProperty("translations");
        return items.EnumerateArray().Select(x => x.GetProperty("translatedText").GetString() ?? "").ToList();
    }

    /// <summary>Mã ngôn ngữ của hệ thống → mã của Google (chỉ "zh" cần đổi thành Hán giản thể).</summary>
    private static string Map(string code) => code.ToLowerInvariant() switch
    {
        "zh" => "zh-CN",
        var c => c
    };
}
