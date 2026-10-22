using Microsoft.EntityFrameworkCore;

namespace AudioGuide.DAL;

/// <summary>
/// Nâng cấp schema cho database ĐÃ TỒN TẠI (tạo từ phiên bản cũ) mà không làm mất dữ liệu.
/// Database mới hoàn toàn được EF tạo bằng EnsureCreated, các lệnh bên dưới khi đó không làm gì.
/// Mọi lệnh đều idempotent (chạy lại nhiều lần vẫn an toàn) và chạy được trên MySQL 8 lẫn MariaDB của XAMPP.
/// </summary>
public static class SchemaUpgrader
{
    private static readonly string[] CreateTables =
    {
        @"CREATE TABLE IF NOT EXISTS `Visitors` (
            `Id` int NOT NULL AUTO_INCREMENT,
            `ClientId` varchar(64) NOT NULL,
            `DeviceLanguage` varchar(35) NOT NULL,
            `FirstSeenAt` datetime(6) NOT NULL,
            `LastSeenAt` datetime(6) NOT NULL,
            `VisitCount` int NOT NULL,
            CONSTRAINT `PK_Visitors` PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_Visitors_ClientId` (`ClientId`),
            KEY `IX_Visitors_LastSeenAt` (`LastSeenAt`)
        ) CHARACTER SET=utf8mb4",

        @"CREATE TABLE IF NOT EXISTS `VisitSessions` (
            `Id` bigint NOT NULL AUTO_INCREMENT,
            `VisitorId` int NOT NULL,
            `SessionKey` varchar(64) NOT NULL,
            `IpAddress` varchar(45) NULL,
            `UserAgent` varchar(400) NULL,
            `DeviceType` varchar(20) NOT NULL,
            `Browser` varchar(40) NOT NULL,
            `Os` varchar(40) NOT NULL,
            `LanguageCode` varchar(10) NOT NULL,
            `GpsEnabled` tinyint(1) NOT NULL,
            `Latitude` double NULL,
            `Longitude` double NULL,
            `StartedAt` datetime(6) NOT NULL,
            `LastSeenAt` datetime(6) NOT NULL,
            `PingCount` int NOT NULL,
            CONSTRAINT `PK_VisitSessions` PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_VisitSessions_SessionKey` (`SessionKey`),
            KEY `IX_VisitSessions_LastSeenAt` (`LastSeenAt`),
            KEY `IX_VisitSessions_VisitorId` (`VisitorId`),
            CONSTRAINT `FK_VisitSessions_Visitors_VisitorId` FOREIGN KEY (`VisitorId`) REFERENCES `Visitors` (`Id`) ON DELETE CASCADE
        ) CHARACTER SET=utf8mb4"
    };

    private static readonly string[] AddPlayLogVisitor =
    {
        "ALTER TABLE `PlayLogs` ADD COLUMN `VisitorId` int NULL",
        "ALTER TABLE `PlayLogs` ADD INDEX `IX_PlayLogs_VisitorId` (`VisitorId`)",
        "ALTER TABLE `PlayLogs` ADD CONSTRAINT `FK_PlayLogs_Visitors_VisitorId` FOREIGN KEY (`VisitorId`) REFERENCES `Visitors` (`Id`) ON DELETE SET NULL"
    };

    public static async Task ApplyAsync(AppDbContext db)
    {
        foreach (var sql in CreateTables)
            await db.Database.ExecuteSqlRawAsync(sql);

        if (!await ColumnExistsAsync(db, "PlayLogs", "VisitorId"))
        {
            foreach (var sql in AddPlayLogVisitor)
                await db.Database.ExecuteSqlRawAsync(sql);
        }
    }

    private static async Task<bool> ColumnExistsAsync(AppDbContext db, string table, string column)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t AND COLUMN_NAME = @c";
            var pt = cmd.CreateParameter(); pt.ParameterName = "@t"; pt.Value = table; cmd.Parameters.Add(pt);
            var pc = cmd.CreateParameter(); pc.ParameterName = "@c"; pc.Value = column; cmd.Parameters.Add(pc);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }
}
