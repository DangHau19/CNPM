namespace AudioGuide.BLL;

// ---------- Public (người dùng) ----------
public record LanguageDto(string Code, string Name, string NativeName, string SpeechLocale, bool IsActive, int SortOrder);

/// <summary>Địa điểm đã được chọn bản dịch phù hợp. LanguageCode là ngôn ngữ THỰC TẾ của nội dung (có thể khác ngôn ngữ yêu cầu nếu fallback).</summary>
public record PlaceDto(
    int Id, string Slug, double Latitude, double Longitude, int RadiusMeters,
    string? ImageUrl, string Address,
    string Name, string NarrationText, string? AudioUrl,
    string LanguageCode, string SpeechLocale, bool FellBack, double? DistanceMeters);

public record PlayLogRequest(int PlaceId, string LanguageCode, string TriggerType, string? ClientId = null);

// ---------- Admin ----------
public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminProfile(int Id, string Username, string DisplayName);

public record TranslationDto(string LanguageCode, string Name, string NarrationText, string? AudioUrl);
public record TranslationSaveRequest(string Name, string NarrationText, string? AudioUrl);

public record PlaceAdminDto(
    int Id, string Slug, double Latitude, double Longitude, int RadiusMeters,
    string? ImageUrl, string Address, bool IsActive, List<TranslationDto> Translations);

public record PlaceSaveRequest(
    string Slug, double Latitude, double Longitude, int RadiusMeters,
    string? ImageUrl, string Address, bool IsActive);

public record LanguageSaveRequest(string Code, string Name, string NativeName, string SpeechLocale, bool IsActive, int SortOrder);

// ---------- Dịch tự động ----------
/// <summary>Nội dung nguồn (mặc định tiếng Việt). TargetLanguages trống = dịch sang mọi ngôn ngữ đang kích hoạt.</summary>
public record AutoTranslateRequest(string? SourceLanguage, string Name, string NarrationText, List<string>? TargetLanguages = null);
/// <summary>Failed: mã các ngôn ngữ dịch lỗi; Error: lý do của lỗi đầu tiên (nếu có).</summary>
public record AutoTranslateResult(List<TranslationDto> Translations, List<string> Failed, string? Error);
public record AutoTranslateStatus(bool Configured);

public record StatItem(string Key, string Label, int Count);
public record StatsDto(int TotalPlays, int GpsPlays, int ManualPlays, List<StatItem> TopPlaces, List<StatItem> ByLanguage);

// ---------- Theo dõi truy cập ----------
/// <summary>Gói tin "tôi đang online" mà trình duyệt gửi định kỳ.</summary>
public record PingRequest(
    string ClientId, string SessionKey, string? DeviceLanguage, string? LanguageCode,
    bool GpsEnabled, double? Latitude, double? Longitude);

/// <summary>Thông tin lấy từ HTTP request (do lớp API điền).</summary>
public record ClientInfo(string? IpAddress, string? UserAgent);

public record VisitDto(
    long SessionId, int VisitorId, string ClientShort, string? IpAddress,
    string DeviceType, string Browser, string Os,
    string DeviceLanguage, string LanguageCode,
    bool GpsEnabled, double? Latitude, double? Longitude,
    DateTime StartedAt, DateTime LastSeenAt, int DurationSeconds, bool IsOnline,
    int VisitorVisitCount, int Plays);

public record DailyVisitDto(string Date, int Sessions, int Visitors);

public record VisitOverviewDto(
    int OnlineNow, int SessionsInRange, int UniqueVisitorsInRange, int NewVisitorsInRange,
    int ReturningVisitorsInRange, int TotalVisitors,
    List<DailyVisitDto> Daily, List<StatItem> ByDevice, List<StatItem> ByLanguage);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
