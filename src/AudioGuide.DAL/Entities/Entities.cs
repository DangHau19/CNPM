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
    public int? VisitorId { get; set; }                   // ai đã nghe (null nếu không xác định)
    public Place? Place { get; set; }
    public Visitor? Visitor { get; set; }
}

/// <summary>Một thiết bị/trình duyệt truy cập (định danh ẩn danh do trình duyệt tự sinh, lưu ở localStorage).</summary>
public class Visitor
{
    public int Id { get; set; }
    public string ClientId { get; set; } = "";
    public string DeviceLanguage { get; set; } = "";     // ngôn ngữ gốc của thiết bị (navigator.language)
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public int VisitCount { get; set; }
    public List<VisitSession> Sessions { get; set; } = new();
}

/// <summary>Một lượt truy cập (mỗi tab/phiên trình duyệt). Còn "online" nếu LastSeenAt rất gần hiện tại.</summary>
public class VisitSession
{
    public long Id { get; set; }
    public int VisitorId { get; set; }
    public string SessionKey { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string DeviceType { get; set; } = "desktop";  // mobile | tablet | desktop | bot
    public string Browser { get; set; } = "";
    public string Os { get; set; } = "";
    public string LanguageCode { get; set; } = "";        // ngôn ngữ đang dùng trong ứng dụng
    public bool GpsEnabled { get; set; }
    public double? Latitude { get; set; }                  // đã làm tròn ~100 m, chỉ có khi người dùng bật GPS
    public double? Longitude { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public int PingCount { get; set; }
    public Visitor? Visitor { get; set; }
}
