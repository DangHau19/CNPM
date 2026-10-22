using System.Text.RegularExpressions;
using AudioGuide.BLL.Visitors;
using AudioGuide.DAL.Entities;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IVisitorService
{
    /// <summary>Ghi nhận/cập nhật một phiên truy cập (idempotent: gọi lặp lại chỉ làm mới LastSeenAt).</summary>
    Task PingAsync(PingRequest request, ClientInfo client);
    /// <param name="tzOffsetMinutes">Chênh lệch múi giờ của người xem so với UTC (VN = 420) để gom theo ngày đúng giờ địa phương.</param>
    Task<VisitOverviewDto> GetOverviewAsync(int days, int tzOffsetMinutes = 0);
    Task<List<VisitDto>> GetOnlineAsync();
    Task<PagedResult<VisitDto>> GetVisitsAsync(int days, int page, int pageSize);
    /// <summary>Xóa các phiên truy cập không còn hoạt động quá <paramref name="retentionDays"/> ngày. Trả về số dòng đã xóa.</summary>
    Task<int> PurgeOldSessionsAsync(int retentionDays);
}

public class VisitorService : IVisitorService
{
    /// <summary>Trình duyệt ping mỗi 30 giây; quá 90 giây không ping = đã rời đi.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(90);

    private static readonly Regex IdRegex = new("^[A-Za-z0-9-]{8,64}$", RegexOptions.Compiled);
    private static readonly Regex LocaleRegex = new("^[A-Za-z0-9_-]{1,35}$", RegexOptions.Compiled);

    private readonly IVisitorRepository _visitors;
    public VisitorService(IVisitorRepository visitors) => _visitors = visitors;

    public async Task PingAsync(PingRequest r, ClientInfo client)
    {
        var clientId = (r.ClientId ?? "").Trim();
        var sessionKey = (r.SessionKey ?? "").Trim();
        if (!IdRegex.IsMatch(clientId) || !IdRegex.IsMatch(sessionKey))
            throw new BusinessException("ClientId/SessionKey không hợp lệ.");

        var now = DateTime.UtcNow;
        var deviceLang = CleanLocale(r.DeviceLanguage, 35);
        var lang = CleanLocale(r.LanguageCode, 10).ToLowerInvariant();
        if (lang.Length == 0) lang = "vi";

        var visitor = await _visitors.FindByClientIdAsync(clientId);
        if (visitor == null)
        {
            visitor = new Visitor { ClientId = clientId, FirstSeenAt = now };
            await _visitors.AddVisitorAsync(visitor);
        }
        visitor.LastSeenAt = now;
        if (deviceLang.Length > 0) visitor.DeviceLanguage = deviceLang;

        var session = await _visitors.FindSessionAsync(sessionKey);
        if (session == null)
        {
            var ua = UserAgentParser.Parse(client.UserAgent);
            session = new VisitSession
            {
                SessionKey = sessionKey,
                Visitor = visitor,
                IpAddress = Truncate(client.IpAddress, 45),
                UserAgent = Truncate(client.UserAgent, 400),
                DeviceType = ua.DeviceType,
                Browser = ua.Browser,
                Os = ua.Os,
                StartedAt = now
            };
            visitor.VisitCount++;
            await _visitors.AddSessionAsync(session);
        }
        else if (session.VisitorId != visitor.Id)
        {
            throw new BusinessException("Phiên không thuộc thiết bị này.");
        }

        session.LastSeenAt = now;
        session.PingCount++;
        session.LanguageCode = lang;
        session.GpsEnabled = r.GpsEnabled;
        if (r.GpsEnabled && r.Latitude is >= -90 and <= 90 && r.Longitude is >= -180 and <= 180)
        {
            // Làm tròn 3 chữ số thập phân (~110 m) để không lưu vị trí chính xác của người dùng.
            session.Latitude = Math.Round(r.Latitude.Value, 3);
            session.Longitude = Math.Round(r.Longitude.Value, 3);
        }
        else
        {
            session.Latitude = null;
            session.Longitude = null;
        }

        await _visitors.SaveAsync();
    }

    public async Task<List<VisitDto>> GetOnlineAsync()
    {
        var sessions = await _visitors.GetOnlineAsync(DateTime.UtcNow - OnlineWindow);
        return await ToDtosAsync(sessions);
    }

    public async Task<PagedResult<VisitDto>> GetVisitsAsync(int days, int page, int pageSize)
    {
        days = Math.Clamp(days, 1, 365);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var from = DateTime.UtcNow.AddDays(-days);

        var total = await _visitors.CountSessionsAsync(from);
        var items = await _visitors.GetSessionsAsync(from, (page - 1) * pageSize, pageSize);
        return new PagedResult<VisitDto>(await ToDtosAsync(items), total, page, pageSize);
    }

    public async Task<VisitOverviewDto> GetOverviewAsync(int days, int tzOffsetMinutes = 0)
    {
        days = Math.Clamp(days, 1, 365);
        tzOffsetMinutes = Math.Clamp(tzOffsetMinutes, -840, 840);
        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);

        var online = await _visitors.CountOnlineAsync(now - OnlineWindow);
        var sessions = await _visitors.CountSessionsAsync(from);
        var unique = await _visitors.CountActiveVisitorsAsync(from);
        var fresh = await _visitors.CountNewVisitorsAsync(from);
        var total = await _visitors.CountVisitorsAsync();

        var points = await _visitors.GetTimePointsAsync(from);
        var daily = points
            .GroupBy(p => p.StartedAt.AddMinutes(tzOffsetMinutes).Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyVisitDto(g.Key.ToString("yyyy-MM-dd"), g.Count(), g.Select(x => x.VisitorId).Distinct().Count()))
            .ToList();

        var byDevice = (await _visitors.CountSessionsByDeviceAsync(from))
            .OrderByDescending(x => x.Count).Select(x => new StatItem(x.Key, x.Key, x.Count)).ToList();
        var byLang = (await _visitors.CountSessionsByLanguageAsync(from))
            .OrderByDescending(x => x.Count).Select(x => new StatItem(x.Key, x.Key, x.Count)).ToList();

        return new VisitOverviewDto(online, sessions, unique, fresh, Math.Max(0, unique - fresh), total, daily, byDevice, byLang);
    }

    public Task<int> PurgeOldSessionsAsync(int retentionDays)
    {
        if (retentionDays < 1) throw new BusinessException("Số ngày lưu trữ phải từ 1 trở lên.");
        return _visitors.DeleteSessionsBeforeAsync(DateTime.UtcNow.AddDays(-retentionDays));
    }

    private async Task<List<VisitDto>> ToDtosAsync(List<VisitSession> sessions)
    {
        var plays = (await _visitors.CountPlaysAsync(sessions.Select(s => s.VisitorId).Distinct().ToList()))
            .ToDictionary(x => x.VisitorId, x => x.Count);
        var onlineSince = DateTime.UtcNow - OnlineWindow;

        return sessions.Select(s => new VisitDto(
            s.Id, s.VisitorId,
            (s.Visitor?.ClientId ?? "").Length >= 8 ? s.Visitor!.ClientId[..8] : (s.Visitor?.ClientId ?? ""),
            s.IpAddress, s.DeviceType, s.Browser, s.Os,
            s.Visitor?.DeviceLanguage ?? "", s.LanguageCode,
            s.GpsEnabled, s.Latitude, s.Longitude,
            Utc(s.StartedAt), Utc(s.LastSeenAt),
            (int)Math.Max(0, (s.LastSeenAt - s.StartedAt).TotalSeconds),
            s.LastSeenAt >= onlineSince,
            s.Visitor?.VisitCount ?? 0,
            plays.TryGetValue(s.VisitorId, out var c) ? c : 0)).ToList();
    }

    /// <summary>MySQL trả DateTime không có "Kind"; ép về UTC để JSON có hậu tố Z và trình duyệt đổi đúng múi giờ.</summary>
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static string CleanLocale(string? value, int max)
    {
        var v = (value ?? "").Trim();
        if (v.Length > max) v = v[..max];
        return LocaleRegex.IsMatch(v) ? v : "";
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : (value.Length <= max ? value : value[..max]);
}
