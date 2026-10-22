using System.Text.Json;
using AudioGuide.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AudioGuide.DAL;

public static class DbSeeder
{
    private class SeedData
    {
        public List<SeedLanguage> Languages { get; set; } = new();
        public List<SeedPlace> Places { get; set; } = new();
    }
    private class SeedLanguage
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string SpeechLocale { get; set; } = "";
        public int SortOrder { get; set; }
    }
    private class SeedPlace
    {
        public string Slug { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; } = 100;
        public string Address { get; set; } = "";
        public Dictionary<string, SeedTranslation> Translations { get; set; } = new();
    }
    private class SeedTranslation
    {
        public string Name { get; set; } = "";
        public string NarrationText { get; set; } = "";
    }

    /// <summary>Tạo dữ liệu mẫu nếu bảng còn trống (idempotent).</summary>
    public static async Task SeedAsync(AppDbContext db, string adminUsername, string adminPasswordHash)
    {
        if (!await db.AdminUsers.AnyAsync())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = adminUsername,
                PasswordHash = adminPasswordHash,
                DisplayName = "Quản trị viên"
            });
        }

        if (!await db.Languages.AnyAsync() && !await db.Places.AnyAsync())
        {
            var data = LoadSeed();
            foreach (var l in data.Languages)
                db.Languages.Add(new Language { Code = l.Code, Name = l.Name, NativeName = l.NativeName, SpeechLocale = l.SpeechLocale, SortOrder = l.SortOrder });

            foreach (var p in data.Places)
            {
                db.Places.Add(new Place
                {
                    Slug = p.Slug,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    RadiusMeters = p.RadiusMeters,
                    Address = p.Address,
                    Translations = p.Translations.Select(kv => new PlaceTranslation
                    {
                        LanguageCode = kv.Key,
                        Name = kv.Value.Name,
                        NarrationText = kv.Value.NarrationText
                    }).ToList()
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static SeedData LoadSeed()
    {
        var asm = typeof(DbSeeder).Assembly;
        var resource = asm.GetManifestResourceNames().First(n => n.EndsWith("seed.json", StringComparison.OrdinalIgnoreCase));
        using var stream = asm.GetManifestResourceStream(resource)!;
        return JsonSerializer.Deserialize<SeedData>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
