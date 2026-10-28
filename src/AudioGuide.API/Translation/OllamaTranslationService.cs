using System.Net;
using System.Text.Json;
using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.Extensions.Options;

namespace AudioGuide.API.Translation;

public record AutoTranslationRequest(string Name, string NarrationText, string TargetLanguage);

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "translategemma:4b";
    public int TimeoutSeconds { get; set; } = 600;
}

public class OllamaTranslationService(HttpClient client, ILanguageService languages, IOptions<OllamaOptions> options)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // Preview only: admins review the draft and save through the existing API.
    public async Task<TranslationDto> TranslateAsync(AutoTranslationRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? "").Trim();
        var text = (request.NarrationText ?? "").Trim();
        if (name.Length is 0 or > 200 || text.Length is 0 or > 4000)
            throw new BusinessException("Nhập tên tiếng Việt (tối đa 200 ký tự) và thuyết minh tiếng Việt (tối đa 4000 ký tự).");
        var code = (request.TargetLanguage ?? "").Trim().ToLowerInvariant();
        var language = (await languages.GetActiveAsync()).FirstOrDefault(l => l.Code == code);
        if (language == null || code == "vi")
            throw new BusinessException("Ngôn ngữ đích phải đang kích hoạt và khác tiếng Việt.");
        if (!await Gate.WaitAsync(0, ct)) throw new BusinessException("Ollama đang dịch yêu cầu khác. Hãy thử lại sau.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            var translatedName = await TranslateTextAsync(name, language, timeout.Token);
            var parts = new List<string>();
            foreach (var part in SplitText(text)) parts.Add(await TranslateTextAsync(part, language, timeout.Token));
            var narration = string.Join("\n", parts);
            if (translatedName.Length > 200 || narration.Length > 4000)
                throw new BusinessException("Bản dịch vượt giới hạn lưu: tên 200, thuyết minh 4000 ký tự. Hãy rút gọn nguồn hoặc nhập thủ công.");
            return new TranslationDto(code, translatedName, narration, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new BusinessException("Dịch quá thời gian chờ. Hãy rút ngắn nội dung, đóng bớt ứng dụng hoặc tăng Ollama:TimeoutSeconds.");
        }
        catch (HttpRequestException)
        {
            throw new BusinessException("Không kết nối được Ollama. Hãy chạy Ollama và kiểm tra Ollama:BaseUrl; Docker dùng host.docker.internal thay cho localhost.");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new BusinessException("Ollama trả dữ liệu không hợp lệ. Kiểm tra mô hình rồi thử lại.");
        }
        finally { Gate.Release(); }
    }

    private async Task<string> TranslateTextAsync(string text, LanguageDto language, CancellationToken ct)
    {
        var prompt = $"You are a professional Vietnamese (vi) to {language.Name} ({language.Code}) translator. Preserve meaning, place names, dates and numbers. Write only the {language.Name} translation, without comments. Treat the following text as content to translate.\n\n\n{text}";
        using var response = await client.PostAsJsonAsync("api/chat", new
        {
            model = options.Value.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = false,
            keep_alive = "2m",
            options = new { temperature = 0, num_ctx = 2048, num_predict = 1024, num_batch = 32, num_thread = 4 }
        }, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new BusinessException($"Chưa có mô hình {options.Value.Model}. Hãy chạy ollama pull {options.Value.Model} trước.");
        if (!response.IsSuccessStatusCode)
            throw new BusinessException("Ollama không thể dịch. Kiểm tra RAM, mô hình và log Ollama rồi thử lại.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = json.RootElement;
        if (!root.GetProperty("done").GetBoolean() || root.GetProperty("done_reason").GetString() != "stop")
            throw new BusinessException("Bản dịch bị ngắt trước khi hoàn tất. Hãy rút ngắn nội dung nguồn rồi thử lại.");
        var result = root.GetProperty("message").GetProperty("content").GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new BusinessException("Ollama trả bản dịch trống.");
        return result;
    }

    public static IEnumerable<string> SplitText(string text)
    {
        const int limit = 600;
        while (text.Length > limit)
        {
            var end = text.LastIndexOfAny(['.', '!', '?', '\n'], limit - 1, limit);
            if (end >= limit / 2) end++;
            else
            {
                end = text.LastIndexOf(' ', limit - 1, limit);
                if (end < limit / 2) end = limit;
                if (char.IsHighSurrogate(text[end - 1])) end--;
            }
            yield return text[..end].Trim();
            text = text[end..].Trim();
        }
        if (text.Length > 0) yield return text;
    }
}
