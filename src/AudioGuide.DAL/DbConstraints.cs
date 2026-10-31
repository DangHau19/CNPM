using Microsoft.EntityFrameworkCore;

namespace AudioGuide.DAL;

/// <summary>
/// Các ràng buộc CHECK ở tầng MySQL, khớp với các điều kiện kiểm tra trong lớp BLL/giao diện.
/// Nhờ vậy dù sửa trực tiếp bằng phpMyAdmin hay câu lệnh SQL, dữ liệu sai (bán kính âm, vĩ độ ngoài
/// -90..90, slug sai định dạng, ...) cũng bị MySQL từ chối.
/// Database mới: được tạo cùng bảng (xem AppDbContext). Database cũ: <see cref="EnsureAsync"/> tự bổ sung khi khởi động.
/// </summary>
public static class DbConstraints
{
    public record Check(string Table, string Name, string Sql);

    public static readonly IReadOnlyList<Check> All = new List<Check>
    {
        // ---- Places ----
        new("Places", "CK_Places_Latitude", "`Latitude` BETWEEN -90 AND 90"),
        new("Places", "CK_Places_Longitude", "`Longitude` BETWEEN -180 AND 180"),
        new("Places", "CK_Places_Radius", "`RadiusMeters` BETWEEN 20 AND 2000"),
        new("Places", "CK_Places_Slug", "REGEXP_LIKE(`Slug`, '^[a-z0-9]+(-[a-z0-9]+)*$', 'c')"),
        new("Places", "CK_Places_Address", "CHAR_LENGTH(TRIM(`Address`)) > 0"),

        // ---- PlaceTranslations ----
        new("PlaceTranslations", "CK_PlaceTranslations_Name", "CHAR_LENGTH(TRIM(`Name`)) > 0"),
        new("PlaceTranslations", "CK_PlaceTranslations_Text", "CHAR_LENGTH(TRIM(`NarrationText`)) BETWEEN 1 AND 4000"),
        new("PlaceTranslations", "CK_PlaceTranslations_Audio",
            "`AudioUrl` IS NULL OR `AudioUrl` LIKE 'http://%' OR `AudioUrl` LIKE 'https://%' OR `AudioUrl` LIKE '/%'"),

        // ---- Languages ----
        new("Languages", "CK_Languages_Code", "REGEXP_LIKE(`Code`, '^[a-z]{2,3}(-[A-Za-z]{2,4})?$', 'c')"),
        new("Languages", "CK_Languages_Name", "CHAR_LENGTH(TRIM(`Name`)) > 0 AND CHAR_LENGTH(TRIM(`NativeName`)) > 0"),
        new("Languages", "CK_Languages_Locale", "CHAR_LENGTH(TRIM(`SpeechLocale`)) > 0"),

        // ---- AdminUsers ----
        new("AdminUsers", "CK_AdminUsers_Username", "CHAR_LENGTH(TRIM(`Username`)) > 0"),
        new("AdminUsers", "CK_AdminUsers_Password", "CHAR_LENGTH(`PasswordHash`) > 0"),

        // ---- PlayLogs ----
        new("PlayLogs", "CK_PlayLogs_Trigger", "`TriggerType` IN ('gps', 'manual')"),

        // ---- Visitors / VisitSessions ----
        new("Visitors", "CK_Visitors_Count", "`VisitCount` >= 0"),
        new("Visitors", "CK_Visitors_ClientId", "REGEXP_LIKE(`ClientId`, '^[A-Za-z0-9-]{8,64}$', 'c')"),
        new("VisitSessions", "CK_VisitSessions_Ping", "`PingCount` >= 0"),
        new("VisitSessions", "CK_VisitSessions_Device", "`DeviceType` IN ('mobile', 'tablet', 'desktop', 'bot')"),
        new("VisitSessions", "CK_VisitSessions_Gps",
            "(`Latitude` IS NULL OR `Latitude` BETWEEN -90 AND 90) AND (`Longitude` IS NULL OR `Longitude` BETWEEN -180 AND 180)"),
    };

    /// <summary>Bổ sung các ràng buộc còn thiếu cho database đã tồn tại. Trả về danh sách ràng buộc không thêm được.</summary>
    public static async Task<List<string>> EnsureAsync(AppDbContext db)
    {
        var failed = new List<string>();
        if (!db.Database.IsRelational()) return failed;   // provider InMemory (kiểm thử) không có ràng buộc CHECK

        foreach (var c in All)
        {
            var exists = (await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS `Value` FROM information_schema.TABLE_CONSTRAINTS " +
                "WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = {0} AND CONSTRAINT_NAME = {1} AND CONSTRAINT_TYPE = 'CHECK'",
                c.Table, c.Name).ToListAsync()).FirstOrDefault();
            if (exists > 0) continue;
            try
            {
                // Tên bảng/ràng buộc/biểu thức đều là hằng số trong mã nguồn, không lấy từ người dùng.
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE `{c.Table}` ADD CONSTRAINT `{c.Name}` CHECK ({c.Sql})");
            }
            catch (Exception ex)
            {
                // Thường do dữ liệu cũ đã vi phạm (vd: có địa điểm bán kính âm). Sửa dữ liệu rồi khởi động lại để thêm.
                failed.Add(c.Name);
                Console.Error.WriteLine($"[DbConstraints] Không thêm được {c.Name}: {ex.Message}");
            }
        }
        return failed;
    }
}
