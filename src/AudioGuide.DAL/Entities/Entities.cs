namespace AudioGuide.DAL.Entities;

public class Language
{
    public string Code { get; set; } = "";          // vi, en, zh, ja, ko ...
    public string Name { get; set; } = "";          // Vietnamese
    public string NativeName { get; set; } = "";    // Tiếng Việt
    public string SpeechLocale { get; set; } = "";  // vi-VN (dùng cho text-to-speech)
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class Place
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 100;    // bán kính kích hoạt thuyết minh
    public string Address { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PlaceTranslation> Translations { get; set; } = new();
}

public class PlaceTranslation
{
    public int Id { get; set; }
    public int PlaceId { get; set; }
    public string LanguageCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string NarrationText { get; set; } = "";
    public string? AudioUrl { get; set; }           // file mp3 (tùy chọn). Trống => dùng text-to-speech
    public Place? Place { get; set; }
    public Language? Language { get; set; }
}

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PlayLog
{
    public long Id { get; set; }
    public int PlaceId { get; set; }
    public string LanguageCode { get; set; } = "";
    public string TriggerType { get; set; } = "manual";   // gps | manual
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public Place? Place { get; set; }
}
