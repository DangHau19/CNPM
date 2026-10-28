using System.Net;
using System.Text.Json;
using AudioGuide.API.Translation;
using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AudioGuide.Tests;

public class OllamaTranslationTests
{
    [Fact]
    public async Task UnsavedSource_IsTranslatedAsSeparateFields()
    {
        var calls = new List<string>();
        var service = Service(async request =>
        {
            Assert.Equal("http://localhost:11434/api/chat", request.RequestUri!.ToString());
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("translategemma:4b", json.RootElement.GetProperty("model").GetString());
            Assert.False(json.RootElement.GetProperty("stream").GetBoolean());
            calls.Add(json.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!);
            return Reply(calls.Count == 1 ? "Museum" : "Welcome to the museum.");
        });
        var result = await service.TranslateAsync(new(" Bảo tàng ", "Chào mừng đến bảo tàng.", " EN "), default);
        Assert.Equal("Museum", result.Name);
        Assert.Equal("Welcome to the museum.", result.NarrationText);
        Assert.Equal("en", result.LanguageCode);
        Assert.Null(result.AudioUrl);
        Assert.Contains("Bảo tàng", calls[0]);
        Assert.Equal(2, calls.Count);
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("fr")]
    [InlineData("unknown")]
    public async Task InvalidTarget_DoesNotCallOllama(string code)
    {
        var service = Service(_ => throw new Exception("Must not call"));
        await Assert.ThrowsAsync<BusinessException>(() => service.TranslateAsync(new("Name", "Text", code), default));
    }

    [Theory]
    [InlineData("", "Text")]
    [InlineData("Name", "")]
    public async Task MissingSource_IsRejected(string name, string text)
    {
        var service = Service(_ => throw new Exception("Must not call"));
        await Assert.ThrowsAsync<BusinessException>(() => service.TranslateAsync(new(name, text, "en"), default));
    }

    [Fact]
    public async Task LongNarration_UsesBoundedChunks()
    {
        var calls = 0;
        var service = Service(async request =>
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var prompt = json.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!;
            Assert.InRange(prompt.Split("\n\n\n")[1].Length, 1, 600);
            calls++;
            return Reply("Translation");
        });
        await service.TranslateAsync(new("Name", new string('a', 1300), "en"), default);
        Assert.Equal(4, calls);
        var text = string.Concat(Enumerable.Repeat("word ", 400));
        Assert.Equal(text.Trim(), string.Join(" ", OllamaTranslationService.SplitText(text)));
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ProviderErrors_DoNotExposeRawBody(int status)
    {
        var service = Service(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("private details") }));
        var error = await Assert.ThrowsAsync<BusinessException>(() => service.TranslateAsync(new("Name", "Text", "en"), default));
        Assert.DoesNotContain("private", error.Message);
    }

    [Fact]
    public async Task TruncatedTranslation_IsRejected()
    {
        var service = Service(_ => Task.FromResult(Reply("Partial", "length")));
        await Assert.ThrowsAsync<BusinessException>(() => service.TranslateAsync(new("Name", "Text", "en"), default));
    }

    [Fact]
    public async Task Cancellation_IsPropagated()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = Service(_ => throw new Exception("Must not call"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.TranslateAsync(new("Name", "Text", "en"), cts.Token));
    }

    private static OllamaTranslationService Service(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://localhost:11434/") }, new Languages(), Options.Create(new OllamaOptions()));
    private static HttpResponseMessage Reply(string text, string reason = "stop") => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { done = true, done_reason = reason, message = new { content = text } }))
    };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private sealed class Languages : ILanguageService
    {
        public Task<List<LanguageDto>> GetActiveAsync() => Task.FromResult(new List<LanguageDto>
        {
            new("vi", "Vietnamese", "Tiếng Việt", "vi-VN", true, 1),
            new("en", "English", "English", "en-US", true, 2)
        });
        public Task<List<LanguageDto>> GetAllAsync() => GetActiveAsync();
        public Task<LanguageDto> CreateAsync(LanguageSaveRequest request) => throw new NotSupportedException();
        public Task<LanguageDto> UpdateAsync(string code, LanguageSaveRequest request) => throw new NotSupportedException();
    }
}
