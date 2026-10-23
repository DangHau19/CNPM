using System.Net;
using System.Text;
using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using AudioGuide.BLL.Translation;
using AudioGuide.DAL;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AudioGuide.Tests;

public class AutoTranslateServiceTests
{
    private class FakeTranslator : ITextTranslator
    {
        public bool IsConfigured { get; set; } = true;
        public List<string> FailFor { get; } = new();
        public List<(string Source, string Target)> Calls { get; } = new();

        public Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default)
        {
            Calls.Add((sourceLanguage, targetLanguage));
            if (FailFor.Contains(targetLanguage)) throw new BusinessException("lỗi " + targetLanguage);
            IReadOnlyList<string> result = texts.Select(t => $"[{targetLanguage}] {t}").ToList();
            return Task.FromResult(result);
        }
    }

    private static (AutoTranslateService svc, FakeTranslator fake) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        db.Languages.AddRange(
            new Language { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", SpeechLocale = "vi-VN", SortOrder = 1 },
            new Language { Code = "en", Name = "English", NativeName = "English", SpeechLocale = "en-US", SortOrder = 2 },
            new Language { Code = "zh", Name = "Chinese", NativeName = "中文", SpeechLocale = "zh-CN", SortOrder = 3 },
            new Language { Code = "ko", Name = "Korean", NativeName = "한국어", SpeechLocale = "ko-KR", SortOrder = 4, IsActive = false });
        db.SaveChanges();
        var fake = new FakeTranslator();
        return (new AutoTranslateService(fake, new LanguageRepository(db)), fake);
    }

    private static AutoTranslateRequest Req(string name = "Dinh Độc Lập", string text = "Nội dung.", List<string>? targets = null) =>
        new("vi", name, text, targets);

    [Fact]
    public async Task Translates_ToAllActiveLanguages_ExceptSourceAndInactive()
    {
        var (svc, fake) = Create();
        var r = await svc.TranslateAsync(Req());

        Assert.Equal(new[] { "en", "zh" }, r.Translations.Select(t => t.LanguageCode).OrderBy(c => c).ToArray());
        Assert.Empty(r.Failed);
        Assert.Null(r.Error);
        Assert.All(fake.Calls, c => Assert.Equal("vi", c.Source));
        var en = r.Translations.Single(t => t.LanguageCode == "en");
        Assert.Equal("[en] Dinh Độc Lập", en.Name);
        Assert.Equal("[en] Nội dung.", en.NarrationText);
        Assert.Null(en.AudioUrl);
    }

    [Fact]
    public async Task TargetLanguages_FiltersResult()
    {
        var (svc, _) = Create();
        var r = await svc.TranslateAsync(Req(targets: new List<string> { "ZH" }));
        Assert.Equal("zh", Assert.Single(r.Translations).LanguageCode);
    }

    [Fact]
    public async Task PartialFailure_IsReported_AndOthersStillReturned()
    {
        var (svc, fake) = Create();
        fake.FailFor.Add("zh");
        var r = await svc.TranslateAsync(Req());

        Assert.Equal("en", Assert.Single(r.Translations).LanguageCode);
        Assert.Equal("zh", Assert.Single(r.Failed));
        Assert.Equal("lỗi zh", r.Error);
    }

    [Fact]
    public async Task NotConfigured_ThrowsBusinessException()
    {
        var (svc, fake) = Create();
        fake.IsConfigured = false;
        Assert.False(svc.IsConfigured);
        await Assert.ThrowsAsync<BusinessException>(() => svc.TranslateAsync(Req()));
    }

    [Theory]
    [InlineData("", "text")]
    [InlineData("name", "  ")]
    public async Task EmptySource_IsRejected(string name, string text)
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<BusinessException>(() => svc.TranslateAsync(Req(name, text)));
    }

    [Fact]
    public async Task UnknownSourceLanguage_IsRejected()
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<BusinessException>(() => svc.TranslateAsync(new AutoTranslateRequest("xx", "a", "b")));
    }
}

public class TextChunkerTests
{
    [Fact]
    public void ShortText_IsSingleChunk() =>
        Assert.Equal(new[] { "Xin chào. Tạm biệt." }, TextChunker.Split("Xin chào. Tạm biệt.", 450));

    [Fact]
    public void LongText_IsSplitAtSentences_WithinByteLimit()
    {
        var sentence = "Đây là một câu thuyết minh khá dài về Dinh Độc Lập ở Sài Gòn.";
        var text = string.Join(" ", Enumerable.Repeat(sentence, 30));
        var chunks = TextChunker.Split(text, 450);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(Encoding.UTF8.GetByteCount(c) <= 450));
        Assert.Equal(text, string.Join(" ", chunks));   // không mất chữ nào
    }

    [Fact]
    public void SingleHugeSentence_IsStillSplit()
    {
        var text = string.Join(" ", Enumerable.Repeat("từ", 400)) + ".";   // một câu rất dài, không có dấu chấm giữa chừng
        var chunks = TextChunker.Split(text, 200);
        Assert.All(chunks, c => Assert.True(Encoding.UTF8.GetByteCount(c) <= 200));
        Assert.Equal(text, string.Join(" ", chunks));
    }

    [Fact]
    public void Empty_GivesNoChunks() => Assert.Empty(TextChunker.Split("   ", 450));
}

public class MyMemoryTranslatorTests
{
    private class StubHandler : HttpMessageHandler
    {
        public List<string> Urls { get; } = new();
        public Func<string> Body { get; set; } = () => "{\"responseData\":{\"translatedText\":\"Hello\"},\"responseStatus\":200}";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body(), Encoding.UTF8, "application/json")
            });
        }
    }

    private static MyMemoryTranslator Create(StubHandler handler, string? email = null) =>
        new(new HttpClient(handler), new MyMemoryOptions(email));

    [Fact]
    public async Task Translate_BuildsRequest_MapsChineseAndSendsEmail()
    {
        var handler = new StubHandler();
        var result = await Create(handler, "me@example.com").TranslateAsync(new[] { "Xin chào" }, "vi", "zh");

        Assert.Equal("Hello", Assert.Single(result));
        var url = Uri.UnescapeDataString(Assert.Single(handler.Urls));
        Assert.Contains("langpair=vi|zh-CN", url);
        Assert.Contains("de=me@example.com", url);
        Assert.Contains("q=Xin chào", url);
    }

    [Fact]
    public async Task Translate_WithoutEmail_OmitsEmailParameter()
    {
        var handler = new StubHandler();
        await Create(handler).TranslateAsync(new[] { "Xin chào" }, "vi", "en");
        Assert.DoesNotContain("&de=", Assert.Single(handler.Urls));
    }

    [Fact]
    public async Task Translate_DecodesHtmlEntities()
    {
        var handler = new StubHandler { Body = () => "{\"responseData\":{\"translatedText\":\"Independence Palace &#39;s gate\"},\"responseStatus\":200}" };
        var result = await Create(handler).TranslateAsync(new[] { "x" }, "vi", "en");
        Assert.Equal("Independence Palace 's gate", Assert.Single(result));
    }

    [Fact]
    public async Task Translate_QuotaWarning_ThrowsBusinessException()
    {
        var handler = new StubHandler
        {
            Body = () => "{\"responseData\":{\"translatedText\":\"MYMEMORY WARNING: YOU USED ALL AVAILABLE FREE TRANSLATIONS FOR TODAY.\"},\"responseStatus\":429,\"quotaFinished\":true}"
        };
        await Assert.ThrowsAsync<BusinessException>(() => Create(handler).TranslateAsync(new[] { "x" }, "vi", "en"));
    }

    [Fact]
    public async Task Translate_LongText_SendsSeveralRequests_AndJoinsResults()
    {
        var handler = new StubHandler();
        var sentence = "Đây là một câu thuyết minh khá dài về Dinh Độc Lập ở Sài Gòn.";
        var text = string.Join(" ", Enumerable.Repeat(sentence, 30));
        var result = await Create(handler).TranslateAsync(new[] { text }, "vi", "en");

        Assert.True(handler.Urls.Count > 1);
        Assert.Equal(string.Join(" ", Enumerable.Repeat("Hello", handler.Urls.Count)), Assert.Single(result));
    }
}
