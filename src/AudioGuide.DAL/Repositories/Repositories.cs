using AudioGuide.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AudioGuide.DAL.Repositories;

public record PlayCount(string Key, int Count);

public interface ILanguageRepository
{
    Task<List<Language>> GetAllAsync(bool onlyActive);
    Task<Language?> FindAsync(string code);
    Task AddAsync(Language language);
    Task SaveAsync();
}

public interface IPlaceRepository
{
    Task<List<Place>> GetAllAsync(bool onlyActive);          // kèm Translations
    Task<Place?> FindAsync(int id);                          // kèm Translations, có tracking
    Task<bool> SlugExistsAsync(string slug, int? exceptId);
    Task AddAsync(Place place);
    Task SaveAsync();
    Task DeleteAsync(Place place);
    Task DeleteTranslationAsync(PlaceTranslation translation);
}

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username);
    Task<AdminUser?> FindAsync(int id);
    Task SaveAsync();
}

public interface IPlayLogRepository
{
    Task AddAsync(PlayLog log);
    Task<List<PlayCount>> CountByPlaceAsync(DateTime fromUtc);
    Task<List<PlayCount>> CountByLanguageAsync(DateTime fromUtc);
    Task<List<PlayCount>> CountByTriggerAsync(DateTime fromUtc);
}

public class LanguageRepository : ILanguageRepository
{
    private readonly AppDbContext _db;
    public LanguageRepository(AppDbContext db) => _db = db;

    public Task<List<Language>> GetAllAsync(bool onlyActive) =>
        _db.Languages.Where(l => !onlyActive || l.IsActive).OrderBy(l => l.SortOrder).ThenBy(l => l.Code).ToListAsync();

    public Task<Language?> FindAsync(string code) => _db.Languages.FirstOrDefaultAsync(l => l.Code == code);

    public async Task AddAsync(Language language)
    {
        _db.Languages.Add(language);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class PlaceRepository : IPlaceRepository
{
    private readonly AppDbContext _db;
    public PlaceRepository(AppDbContext db) => _db = db;

    public Task<List<Place>> GetAllAsync(bool onlyActive) =>
        _db.Places.Include(p => p.Translations)
            .Where(p => !onlyActive || p.IsActive)
            .OrderBy(p => p.Id).ToListAsync();

    public Task<Place?> FindAsync(int id) =>
        _db.Places.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> SlugExistsAsync(string slug, int? exceptId) =>
        _db.Places.AnyAsync(p => p.Slug == slug && (exceptId == null || p.Id != exceptId));

    public async Task AddAsync(Place place)
    {
        _db.Places.Add(place);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();

    public async Task DeleteAsync(Place place)
    {
        _db.Places.Remove(place);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTranslationAsync(PlaceTranslation translation)
    {
        _db.PlaceTranslations.Remove(translation);
        await _db.SaveChangesAsync();
    }
}

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _db;
    public AdminUserRepository(AppDbContext db) => _db = db;

    public Task<AdminUser?> GetByUsernameAsync(string username) =>
        _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username);

    public Task<AdminUser?> FindAsync(int id) => _db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id);

    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class PlayLogRepository : IPlayLogRepository
{
    private readonly AppDbContext _db;
    public PlayLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PlayLog log)
    {
        _db.PlayLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public Task<List<PlayCount>> CountByPlaceAsync(DateTime fromUtc) =>
        _db.PlayLogs.Where(x => x.PlayedAt >= fromUtc)
            .GroupBy(x => x.PlaceId)
            .Select(g => new PlayCount(g.Key.ToString(), g.Count()))
            .ToListAsync();

    public Task<List<PlayCount>> CountByLanguageAsync(DateTime fromUtc) =>
        _db.PlayLogs.Where(x => x.PlayedAt >= fromUtc)
            .GroupBy(x => x.LanguageCode)
            .Select(g => new PlayCount(g.Key, g.Count()))
            .ToListAsync();

    public Task<List<PlayCount>> CountByTriggerAsync(DateTime fromUtc) =>
        _db.PlayLogs.Where(x => x.PlayedAt >= fromUtc)
            .GroupBy(x => x.TriggerType)
            .Select(g => new PlayCount(g.Key, g.Count()))
            .ToListAsync();
}

// ---------------------------------------------------------------------------------------------
// Theo dõi truy cập
// ---------------------------------------------------------------------------------------------

public record VisitorPlayCount(int VisitorId, int Count);
public record VisitTimePoint(DateTime StartedAt, int VisitorId);

public interface IVisitorRepository
{
    Task<Visitor?> FindByClientIdAsync(string clientId);                    // có tracking
    Task<VisitSession?> FindSessionAsync(string sessionKey);                // có tracking
    Task AddVisitorAsync(Visitor visitor);
    Task AddSessionAsync(VisitSession session);
    Task SaveAsync();

    Task<List<VisitSession>> GetOnlineAsync(DateTime sinceUtc);             // kèm Visitor, mới nhất trước
    Task<List<VisitSession>> GetSessionsAsync(DateTime fromUtc, int skip, int take);
    Task<int> CountSessionsAsync(DateTime fromUtc);
    Task<int> CountOnlineAsync(DateTime sinceUtc);
    Task<int> CountActiveVisitorsAsync(DateTime fromUtc);                   // số thiết bị khác nhau có phiên trong kỳ
    Task<int> CountNewVisitorsAsync(DateTime fromUtc);
    Task<int> CountVisitorsAsync();
    Task<List<VisitTimePoint>> GetTimePointsAsync(DateTime fromUtc);
    Task<List<PlayCount>> CountSessionsByDeviceAsync(DateTime fromUtc);
    Task<List<PlayCount>> CountSessionsByLanguageAsync(DateTime fromUtc);
    Task<List<VisitorPlayCount>> CountPlaysAsync(IReadOnlyCollection<int> visitorIds);
    Task<int> DeleteSessionsBeforeAsync(DateTime cutoffUtc);                 // dọn lịch sử truy cập cũ
}

public class VisitorRepository : IVisitorRepository
{
    private readonly AppDbContext _db;
    public VisitorRepository(AppDbContext db) => _db = db;

    public Task<Visitor?> FindByClientIdAsync(string clientId) =>
        _db.Visitors.FirstOrDefaultAsync(v => v.ClientId == clientId);

    public Task<VisitSession?> FindSessionAsync(string sessionKey) =>
        _db.VisitSessions.FirstOrDefaultAsync(s => s.SessionKey == sessionKey);

    public Task AddVisitorAsync(Visitor visitor)
    {
        _db.Visitors.Add(visitor);
        return Task.CompletedTask;
    }

    public Task AddSessionAsync(VisitSession session)
    {
        _db.VisitSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();

    public Task<List<VisitSession>> GetOnlineAsync(DateTime sinceUtc) =>
        _db.VisitSessions.Include(s => s.Visitor)
            .Where(s => s.LastSeenAt >= sinceUtc)
            .OrderByDescending(s => s.LastSeenAt).ToListAsync();

    public Task<List<VisitSession>> GetSessionsAsync(DateTime fromUtc, int skip, int take) =>
        _db.VisitSessions.Include(s => s.Visitor)
            .Where(s => s.StartedAt >= fromUtc)
            .OrderByDescending(s => s.StartedAt)
            .Skip(skip).Take(take).ToListAsync();

    public Task<int> CountSessionsAsync(DateTime fromUtc) =>
        _db.VisitSessions.CountAsync(s => s.StartedAt >= fromUtc);

    public Task<int> CountOnlineAsync(DateTime sinceUtc) =>
        _db.VisitSessions.CountAsync(s => s.LastSeenAt >= sinceUtc);

    public Task<int> CountActiveVisitorsAsync(DateTime fromUtc) =>
        _db.VisitSessions.Where(s => s.StartedAt >= fromUtc).Select(s => s.VisitorId).Distinct().CountAsync();

    public Task<int> CountNewVisitorsAsync(DateTime fromUtc) =>
        _db.Visitors.CountAsync(v => v.FirstSeenAt >= fromUtc);

    public Task<int> CountVisitorsAsync() => _db.Visitors.CountAsync();

    public Task<List<VisitTimePoint>> GetTimePointsAsync(DateTime fromUtc) =>
        _db.VisitSessions.Where(s => s.StartedAt >= fromUtc)
            .Select(s => new VisitTimePoint(s.StartedAt, s.VisitorId)).ToListAsync();

    public Task<List<PlayCount>> CountSessionsByDeviceAsync(DateTime fromUtc) =>
        _db.VisitSessions.Where(s => s.StartedAt >= fromUtc)
            .GroupBy(s => s.DeviceType)
            .Select(g => new PlayCount(g.Key, g.Count())).ToListAsync();

    public Task<List<PlayCount>> CountSessionsByLanguageAsync(DateTime fromUtc) =>
        _db.VisitSessions.Where(s => s.StartedAt >= fromUtc)
            .GroupBy(s => s.LanguageCode)
            .Select(g => new PlayCount(g.Key, g.Count())).ToListAsync();

    public async Task<List<VisitorPlayCount>> CountPlaysAsync(IReadOnlyCollection<int> visitorIds)
    {
        if (visitorIds.Count == 0) return new List<VisitorPlayCount>();
        var ids = visitorIds.ToList();
        var rows = await _db.PlayLogs
            .Where(x => x.VisitorId != null && ids.Contains(x.VisitorId.Value))
            .GroupBy(x => x.VisitorId!.Value)
            .Select(g => new VisitorPlayCount(g.Key, g.Count()))
            .ToListAsync();
        return rows;
    }

    public Task<int> DeleteSessionsBeforeAsync(DateTime cutoffUtc) =>
        _db.VisitSessions.Where(s => s.LastSeenAt < cutoffUtc).ExecuteDeleteAsync();
}
