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
