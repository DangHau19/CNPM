using AudioGuide.BLL;
using AudioGuide.BLL.Geo;
using AudioGuide.BLL.Security;
using AudioGuide.BLL.Services;
using AudioGuide.DAL;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AudioGuide.Tests;

public class GeoHelperTests
{
    [Fact]
    public void SamePoint_IsZero() =>
        Assert.Equal(0, GeoHelper.DistanceMeters(10.777, 106.695, 10.777, 106.695), 3);

    [Fact]
    public void NotreDameToPostOffice_IsAbout100Meters()
    {
        var d = GeoHelper.DistanceMeters(10.7798, 106.6990, 10.7799, 106.7000);
        Assert.InRange(d, 90, 130);
    }

    [Fact]
    public void BenThanhToIndependencePalace_IsAbout600Meters()
    {
        var d = GeoHelper.DistanceMeters(10.7725, 106.6980, 10.7770, 106.6954);
        Assert.InRange(d, 400, 800);
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_Works()
    {
        var hash = PasswordHasher.Hash("Admin@123");
        Assert.True(PasswordHasher.Verify("Admin@123", hash));
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_GarbageHash_ReturnsFalse() => Assert.False(PasswordHasher.Verify("x", "not-a-hash"));

    [Fact]
    public void SamePassword_ProducesDifferentHashes() =>
        Assert.NotEqual(PasswordHasher.Hash("abc"), PasswordHasher.Hash("abc"));
}

public class PlaceServiceTests
{
    private static async Task<(AppDbContext db, PlaceService service)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Languages.AddRange(
            new Language { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", SpeechLocale = "vi-VN", SortOrder = 1 },
            new Language { Code = "en", Name = "English", NativeName = "English", SpeechLocale = "en-US", SortOrder = 2 },
            new Language { Code = "ja", Name = "Japanese", NativeName = "日本語", SpeechLocale = "ja-JP", SortOrder = 3 });

        db.Places.Add(new Place
        {
            Slug = "a", Latitude = 10.7770, Longitude = 106.6954, RadiusMeters = 100, Address = "A",
            Translations =
            {
                new PlaceTranslation { LanguageCode = "vi", Name = "Địa điểm A", NarrationText = "Xin chào" },
                new PlaceTranslation { LanguageCode = "en", Name = "Place A", NarrationText = "Hello" }
            }
        });
        db.Places.Add(new Place
        {
            Slug = "hidden", Latitude = 10.78, Longitude = 106.70, RadiusMeters = 100, Address = "H", IsActive = false,
            Translations = { new PlaceTranslation { LanguageCode = "vi", Name = "Ẩn", NarrationText = "Ẩn" } }
        });
        await db.SaveChangesAsync();

        return (db, new PlaceService(new PlaceRepository(db), new LanguageRepository(db)));
    }

    [Fact]
    public async Task ReturnsRequestedLanguage_AndHidesInactivePlaces()
    {
        var (_, service) = await CreateAsync();
        var places = await service.GetPlacesAsync("en", null, null);
        var p = Assert.Single(places);
        Assert.Equal("Place A", p.Name);
        Assert.False(p.FellBack);
    }

    [Fact]
    public async Task MissingTranslation_FallsBackToEnglish()
    {
        var (_, service) = await CreateAsync();
        var p = Assert.Single(await service.GetPlacesAsync("ja", null, null));
        Assert.Equal("en", p.LanguageCode);
        Assert.True(p.FellBack);
        Assert.Equal("en-US", p.SpeechLocale);
    }

    [Fact]
    public async Task Nearest_ReturnsPlaceInsideRadius_OtherwiseNull()
    {
        var (_, service) = await CreateAsync();
        Assert.NotNull(await service.GetNearestInRangeAsync("vi", 10.7771, 106.6955));
        Assert.Null(await service.GetNearestInRangeAsync("vi", 10.80, 106.72));
    }

    [Fact]
    public async Task Places_AreSortedByDistance_WhenLocationGiven()
    {
        var (db, service) = await CreateAsync();
        db.Places.Add(new Place
        {
            Slug = "b", Latitude = 10.7725, Longitude = 106.6980, RadiusMeters = 100, Address = "B",
            Translations = { new PlaceTranslation { LanguageCode = "vi", Name = "B", NarrationText = "b" } }
        });
        await db.SaveChangesAsync();

        var list = await service.GetPlacesAsync("vi", 10.7726, 106.6981);
        Assert.Equal("b", list[0].Slug);
    }
}

public class PlaceAdminServiceTests
{
    private static PlaceAdminService Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        db.Languages.Add(new Language { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", SpeechLocale = "vi-VN" });
        db.SaveChanges();
        return new PlaceAdminService(new PlaceRepository(db), new LanguageRepository(db));
    }

    private static PlaceSaveRequest Valid(string slug = "dinh-doc-lap") =>
        new(slug, 10.777, 106.695, 120, null, "135 Nam Kỳ Khởi Nghĩa", true);

    [Fact]
    public async Task Create_RejectsDuplicateSlug()
    {
        var svc = Create();
        await svc.CreateAsync(Valid());
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(Valid()));
    }

    [Theory]
    [InlineData("Bad Slug", 10, 106, 100)]
    [InlineData("ok-slug", 95, 106, 100)]
    [InlineData("ok-slug", 10, 200, 100)]
    [InlineData("ok-slug", 10, 106, 5)]
    public async Task Create_RejectsInvalidInput(string slug, double lat, double lng, int radius)
    {
        var svc = Create();
        await Assert.ThrowsAsync<BusinessException>(() =>
            svc.CreateAsync(new PlaceSaveRequest(slug, lat, lng, radius, null, "addr", true)));
    }

    [Fact]
    public async Task UpsertTranslation_InsertsThenUpdates()
    {
        var svc = Create();
        var place = await svc.CreateAsync(Valid());

        var first = await svc.UpsertTranslationAsync(place.Id, "vi", new TranslationSaveRequest("Dinh", "Nội dung 1", null));
        Assert.Single(first.Translations);

        var second = await svc.UpsertTranslationAsync(place.Id, "vi", new TranslationSaveRequest("Dinh", "Nội dung 2", null));
        Assert.Equal("Nội dung 2", Assert.Single(second.Translations).NarrationText);
    }

    [Fact]
    public async Task UpsertTranslation_RejectsUnknownLanguage()
    {
        var svc = Create();
        var place = await svc.CreateAsync(Valid());
        await Assert.ThrowsAsync<BusinessException>(() =>
            svc.UpsertTranslationAsync(place.Id, "xx", new TranslationSaveRequest("n", "t", null)));
    }

    [Fact]
    public async Task Get_UnknownPlace_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Create().GetAsync(999));
}
