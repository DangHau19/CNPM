using AudioGuide.BLL;
using AudioGuide.BLL.Security;
using AudioGuide.BLL.Services;
using AudioGuide.BLL.Visitors;
using AudioGuide.DAL;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AudioGuide.Tests;

public class UserAgentParserTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1", "mobile", "Safari", "iOS")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0", "desktop", "Edge", "Windows")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", "mobile", "Chrome", "Android")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-X700) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", "tablet", "Chrome", "Android")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0", "desktop", "Firefox", "macOS")]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)", "bot", "Other", "Other")]
    public void Parse_DetectsDeviceBrowserAndOs(string ua, string device, string browser, string os)
    {
        var info = UserAgentParser.Parse(ua);
        Assert.Equal(device, info.DeviceType);
        Assert.Equal(browser, info.Browser);
        Assert.Equal(os, info.Os);
    }

    [Fact]
    public void Parse_Empty_IsUnknownDesktop() => Assert.Equal("desktop", UserAgentParser.Parse(null).DeviceType);
}

public class LanguageNegotiatorTests
{
    private static readonly string[] Active = { "vi", "en", "zh", "ja", "ko" };

    [Theory]
    [InlineData("vi-VN,vi;q=0.9,en-US;q=0.8", "vi")]
    [InlineData("en-GB,en;q=0.9", "en")]
    [InlineData("zh-TW,zh;q=0.9", "zh")]
    [InlineData("fr-FR,fr;q=0.9,ja;q=0.8", "ja")]     // fr chưa hỗ trợ → chọn ngôn ngữ kế tiếp được hỗ trợ
    [InlineData("fr-FR,de;q=0.9", "en")]              // không có gì khớp → fallback
    [InlineData("", "en")]
    [InlineData(null, "en")]
    [InlineData("ko;q=0.5,ja;q=0.9", "ja")]           // q cao hơn được ưu tiên
    public void Pick_ChoosesBestSupportedLanguage(string? header, string expected) =>
        Assert.Equal(expected, LanguageNegotiator.Pick(header, Active, "en"));

    [Fact]
    public void Pick_FallbackMissing_UsesFirstActive() =>
        Assert.Equal("vi", LanguageNegotiator.Pick("fr", new[] { "vi", "ja" }, "en"));
}

public class VisitorServiceTests
{
    private const string Client1 = "aaaaaaaa-1111-4222-8333-444444444444";
    private const string Client2 = "bbbbbbbb-1111-4222-8333-444444444444";
    private const string Session1 = "s1s1s1s1-1111-4222-8333-444444444444";
    private const string Session2 = "s2s2s2s2-1111-4222-8333-444444444444";
    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static (AppDbContext db, VisitorService service) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        return (db, new VisitorService(new VisitorRepository(db)));
    }

    private static PingRequest Ping(string client, string session, string lang = "vi", bool gps = false, double? lat = null, double? lng = null) =>
        new(client, session, "vi-VN", lang, gps, lat, lng);

    private static readonly ClientInfo Info = new("203.0.113.7", Chrome);

    [Fact]
    public async Task FirstPing_CreatesVisitorAndSession()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);

        var v = Assert.Single(db.Visitors);
        Assert.Equal(1, v.VisitCount);
        Assert.Equal("vi-VN", v.DeviceLanguage);
        var s = Assert.Single(db.VisitSessions);
        Assert.Equal("Chrome", s.Browser);
        Assert.Equal("Windows", s.Os);
        Assert.Equal("203.0.113.7", s.IpAddress);
    }

    [Fact]
    public async Task RepeatedPing_SameSession_DoesNotAddVisit()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);
        await svc.PingAsync(Ping(Client1, Session1, "en"), Info);

        Assert.Equal(1, Assert.Single(db.Visitors).VisitCount);
        var s = Assert.Single(db.VisitSessions);
        Assert.Equal(2, s.PingCount);
        Assert.Equal("en", s.LanguageCode);
    }

    [Fact]
    public async Task NewSession_SameDevice_CountsAsReturningVisit()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);
        await svc.PingAsync(Ping(Client1, Session2), Info);

        Assert.Equal(2, Assert.Single(db.Visitors).VisitCount);
        Assert.Equal(2, db.VisitSessions.Count());
    }

    [Fact]
    public async Task Gps_PositionIsRounded_AndClearedWhenDisabled()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1, gps: true, lat: 10.777123, lng: 106.695987), Info);
        var s = Assert.Single(db.VisitSessions);
        Assert.Equal(10.777, s.Latitude);
        Assert.Equal(106.696, s.Longitude);

        await svc.PingAsync(Ping(Client1, Session1, gps: false, lat: 10.777123, lng: 106.695987), Info);
        s = Assert.Single(db.VisitSessions);
        Assert.Null(s.Latitude);
        Assert.False(s.GpsEnabled);
    }

    [Theory]
    [InlineData("short", Session1)]
    [InlineData(Client1, "bad key!")]
    [InlineData("", Session1)]
    public async Task InvalidIds_AreRejected(string client, string session)
    {
        var (_, svc) = Create();
        await Assert.ThrowsAsync<BusinessException>(() => svc.PingAsync(Ping(client, session), Info));
    }

    [Fact]
    public async Task Session_CannotBeHijackedByAnotherDevice()
    {
        var (_, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);
        await Assert.ThrowsAsync<BusinessException>(() => svc.PingAsync(Ping(Client2, Session1), Info));
    }

    [Fact]
    public async Task Online_OnlyIncludesRecentlySeenSessions()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);
        await svc.PingAsync(Ping(Client2, Session2), Info);

        var old = db.VisitSessions.First(s => s.SessionKey == Session2);
        old.LastSeenAt = DateTime.UtcNow.AddMinutes(-10);
        await db.SaveChangesAsync();

        var online = await svc.GetOnlineAsync();
        var one = Assert.Single(online);
        Assert.True(one.IsOnline);
        Assert.Equal(Client1[..8], one.ClientShort);
        Assert.Equal(DateTimeKind.Utc, one.LastSeenAt.Kind);
    }

    [Fact]
    public async Task Overview_CountsVisitorsSessionsAndReturning()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);
        await svc.PingAsync(Ping(Client2, Session2, "en"), new ClientInfo("198.51.100.2", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) Mobile/15E148 Safari/604.1"));

        // Client2 đã tồn tại từ 30 ngày trước → trong kỳ 7 ngày là "khách quay lại".
        db.Visitors.First(v => v.ClientId == Client2).FirstSeenAt = DateTime.UtcNow.AddDays(-30);
        await db.SaveChangesAsync();

        var o = await svc.GetOverviewAsync(7);
        Assert.Equal(2, o.OnlineNow);
        Assert.Equal(2, o.SessionsInRange);
        Assert.Equal(2, o.UniqueVisitorsInRange);
        Assert.Equal(1, o.NewVisitorsInRange);
        Assert.Equal(1, o.ReturningVisitorsInRange);
        Assert.Equal(2, o.TotalVisitors);
        Assert.Equal(2, Assert.Single(o.Daily).Sessions);
        Assert.Contains(o.ByDevice, x => x.Key == "mobile" && x.Count == 1);
        Assert.Contains(o.ByLanguage, x => x.Key == "en" && x.Count == 1);
    }

    [Fact]
    public async Task Overview_GroupsDaysByViewerTimeZone()
    {
        var (db, svc) = Create();
        await svc.PingAsync(Ping(Client1, Session1), Info);

        // 20:00 UTC hôm qua = 03:00 hôm nay theo giờ Việt Nam (UTC+7).
        var utcYesterday = DateTime.UtcNow.Date.AddDays(-1);
        db.VisitSessions.Single().StartedAt = utcYesterday.AddHours(20);
        await db.SaveChangesAsync();

        Assert.Equal(utcYesterday.ToString("yyyy-MM-dd"), Assert.Single((await svc.GetOverviewAsync(7)).Daily).Date);
        Assert.Equal(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), Assert.Single((await svc.GetOverviewAsync(7, 420)).Daily).Date);
    }

    [Fact]
    public async Task Purge_RejectsInvalidRetention()
    {
        var (_, svc) = Create();
        await Assert.ThrowsAsync<BusinessException>(() => svc.PurgeOldSessionsAsync(0));
    }

    [Fact]
    public async Task Visits_ArePaged_NewestFirst()
    {
        var (_, svc) = Create();
        for (var i = 0; i < 7; i++)
            await svc.PingAsync(Ping(Client1, $"sess{i:D4}-1111-4222-8333-444444444444"), Info);

        var page = await svc.GetVisitsAsync(7, 1, 5);
        Assert.Equal(7, page.Total);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(2, (await svc.GetVisitsAsync(7, 2, 5)).Items.Count);
    }
}

public class StatsVisitorLinkTests
{
    [Fact]
    public async Task PlayLog_IsLinkedToVisitor_WhenClientIdKnown()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        db.Languages.Add(new Language { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", SpeechLocale = "vi-VN" });
        db.Places.Add(new Place { Slug = "a", Address = "A", Translations = { new PlaceTranslation { LanguageCode = "vi", Name = "A", NarrationText = "a" } } });
        db.Visitors.Add(new Visitor { ClientId = "aaaaaaaa-1111-4222-8333-444444444444", DeviceLanguage = "vi-VN", VisitCount = 1 });
        await db.SaveChangesAsync();

        var stats = new StatsService(new PlayLogRepository(db), new PlaceRepository(db), new LanguageRepository(db), new VisitorRepository(db));
        var placeId = db.Places.First().Id;
        await stats.LogPlayAsync(new PlayLogRequest(placeId, "vi", "gps", "aaaaaaaa-1111-4222-8333-444444444444"));
        await stats.LogPlayAsync(new PlayLogRequest(placeId, "vi", "manual"));   // không có ClientId

        var logs = db.PlayLogs.OrderBy(x => x.Id).ToList();
        Assert.Equal(db.Visitors.First().Id, logs[0].VisitorId);
        Assert.Null(logs[1].VisitorId);

        var s = await stats.GetStatsAsync(7);
        Assert.Equal(2, s.TotalPlays);
        Assert.Equal(1, s.GpsPlays);
    }
}

public class AuthServiceTests
{
    private static async Task<(AuthService svc, AppDbContext db, int id)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var user = new AdminUser { Username = "admin", PasswordHash = PasswordHasher.Hash("Admin@123"), DisplayName = "Admin" };
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();
        return (new AuthService(new AdminUserRepository(db)), db, user.Id);
    }

    [Fact]
    public async Task Login_Works_AndRejectsWrongPassword()
    {
        var (svc, _, _) = await CreateAsync();
        Assert.NotNull(await svc.LoginAsync("admin", "Admin@123"));
        Assert.Null(await svc.LoginAsync("admin", "nope"));
        Assert.Null(await svc.LoginAsync("", ""));
    }

    [Fact]
    public async Task ChangePassword_UpdatesHash()
    {
        var (svc, _, id) = await CreateAsync();
        await svc.ChangePasswordAsync(id, "Admin@123", "NewPassw0rd!");
        Assert.Null(await svc.LoginAsync("admin", "Admin@123"));
        Assert.NotNull(await svc.LoginAsync("admin", "NewPassw0rd!"));
    }

    [Theory]
    [InlineData("wrong", "NewPassw0rd!")]      // sai mật khẩu hiện tại
    [InlineData("Admin@123", "short")]         // quá ngắn
    [InlineData("Admin@123", "Admin@123")]     // trùng mật khẩu cũ
    public async Task ChangePassword_RejectsInvalid(string current, string next)
    {
        var (svc, _, id) = await CreateAsync();
        await Assert.ThrowsAsync<BusinessException>(() => svc.ChangePasswordAsync(id, current, next));
    }
}
