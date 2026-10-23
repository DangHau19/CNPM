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
