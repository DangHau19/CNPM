-- Câu lệnh mẫu cho database hcmc_audioguide (MySQL 8). Chạy trong phpMyAdmin → chọn database → tab SQL.
-- Lưu ý: mọi mốc thời gian trong database lưu theo UTC. Giờ Việt Nam = UTC + 7 giờ.

-- Các bảng chính
--   Visitors          mỗi thiết bị/trình duyệt (ClientId ẩn danh, ngôn ngữ thiết bị, số lần quay lại)
--   VisitSessions     mỗi lượt truy cập (IP, thiết bị, trình duyệt, HĐH, ngôn ngữ, vị trí gần đúng, StartedAt, LastSeenAt)
--   PlayLogs          mỗi lượt nghe thuyết minh (địa điểm, ngôn ngữ, gps/manual, ai nghe)
--   Places / PlaceTranslations / Languages   địa điểm, nội dung đa ngôn ngữ, danh sách ngôn ngữ
--   AdminUsers        tài khoản quản trị

-- 1) AI ĐANG TRUY CẬP (có tín hiệu trong 90 giây gần nhất; trình duyệt gửi tín hiệu mỗi 30 giây)
SELECT v.ClientId, s.IpAddress, s.DeviceType, s.Browser, s.Os, s.LanguageCode,
       s.Latitude, s.Longitude,
       DATE_ADD(s.StartedAt, INTERVAL 7 HOUR)  AS BatDauGioVN,
       DATE_ADD(s.LastSeenAt, INTERVAL 7 HOUR) AS ThayLanCuoiGioVN
FROM VisitSessions s
JOIN Visitors v ON v.Id = s.VisitorId
WHERE s.LastSeenAt >= UTC_TIMESTAMP() - INTERVAL 90 SECOND
ORDER BY s.LastSeenAt DESC;

-- 2) Số người đang online
SELECT COUNT(*) AS DangOnline
FROM VisitSessions
WHERE LastSeenAt >= UTC_TIMESTAMP() - INTERVAL 90 SECOND;

-- 3) Tổng quan: số thiết bị, số lượt truy cập, số lượt nghe
SELECT (SELECT COUNT(*) FROM Visitors)      AS TongThietBi,
       (SELECT COUNT(*) FROM VisitSessions) AS TongLuotTruyCap,
       (SELECT COUNT(*) FROM PlayLogs)      AS TongLuotNghe;

-- 4) Lượt truy cập theo ngày (giờ Việt Nam), 14 ngày gần nhất
SELECT DATE(DATE_ADD(StartedAt, INTERVAL 7 HOUR)) AS Ngay,
       COUNT(*)                  AS LuotTruyCap,
       COUNT(DISTINCT VisitorId) AS SoThietBi
FROM VisitSessions
WHERE StartedAt >= UTC_TIMESTAMP() - INTERVAL 14 DAY
GROUP BY Ngay
ORDER BY Ngay DESC;

-- 5) Thống kê theo loại thiết bị
SELECT DeviceType, COUNT(*) AS SoLuot FROM VisitSessions GROUP BY DeviceType ORDER BY SoLuot DESC;

-- 6) Thống kê theo trình duyệt và hệ điều hành
SELECT Browser, Os, COUNT(*) AS SoLuot FROM VisitSessions GROUP BY Browser, Os ORDER BY SoLuot DESC;

-- 7) Ngôn ngữ người dùng đang dùng trong ứng dụng
SELECT LanguageCode, COUNT(*) AS SoLuot FROM VisitSessions GROUP BY LanguageCode ORDER BY SoLuot DESC;

-- 8) Khách quay lại (đã vào từ 2 lần trở lên)
SELECT ClientId, DeviceLanguage, VisitCount,
       DATE_ADD(FirstSeenAt, INTERVAL 7 HOUR) AS LanDauGioVN,
       DATE_ADD(LastSeenAt, INTERVAL 7 HOUR)  AS GanNhatGioVN
FROM Visitors
WHERE VisitCount >= 2
ORDER BY VisitCount DESC;

-- 9) Địa điểm được nghe nhiều nhất
SELECT p.Id, COALESCE(t.Name, p.Slug) AS DiaDiem, COUNT(*) AS LuotNghe
FROM PlayLogs l
JOIN Places p ON p.Id = l.PlaceId
LEFT JOIN PlaceTranslations t ON t.PlaceId = p.Id AND t.LanguageCode = 'vi'
GROUP BY p.Id, DiaDiem
ORDER BY LuotNghe DESC;

-- 10) Các lượt nghe gần đây: ai nghe, ở đâu, bằng ngôn ngữ nào, tự động (gps) hay bấm tay (manual)
SELECT DATE_ADD(l.PlayedAt, INTERVAL 7 HOUR) AS ThoiGianGioVN,
       COALESCE(t.Name, p.Slug) AS DiaDiem, l.LanguageCode, l.TriggerType,
       LEFT(v.ClientId, 8) AS ThietBi
FROM PlayLogs l
JOIN Places p ON p.Id = l.PlaceId
LEFT JOIN PlaceTranslations t ON t.PlaceId = p.Id AND t.LanguageCode = 'vi'
LEFT JOIN Visitors v ON v.Id = l.VisitorId
ORDER BY l.PlayedAt DESC
LIMIT 50;

-- 11) Tỷ lệ nghe tự động (gps) so với bấm tay (manual)
SELECT TriggerType, COUNT(*) AS SoLuot FROM PlayLogs GROUP BY TriggerType;

-- 12) Danh sách địa điểm kèm ngôn ngữ đã có bản dịch
SELECT p.Id, p.Slug, p.Latitude, p.Longitude, p.RadiusMeters, p.IsActive,
       GROUP_CONCAT(t.LanguageCode ORDER BY t.LanguageCode) AS CacNgonNgu
FROM Places p
LEFT JOIN PlaceTranslations t ON t.PlaceId = p.Id
GROUP BY p.Id
ORDER BY p.Id;

-- 13) THÊM địa điểm bằng SQL (ví dụ). Sau đó thêm bản dịch bằng câu lệnh 14.
INSERT INTO Places (Slug, Latitude, Longitude, RadiusMeters, Address, ImageUrl, IsActive, CreatedAt, UpdatedAt)
VALUES ('thu-nghiem', 10.77690, 106.70090, 100, 'Địa điểm thử nghiệm, Quận 1', NULL, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

-- 14) THÊM bản dịch tiếng Việt cho địa điểm vừa thêm (LAST_INSERT_ID() chỉ đúng nếu chạy ngay sau câu 13)
INSERT INTO PlaceTranslations (PlaceId, LanguageCode, Name, NarrationText, AudioUrl)
VALUES (LAST_INSERT_ID(), 'vi', 'Địa điểm thử nghiệm', 'Đây là nội dung thuyết minh thử nghiệm.', NULL);

-- 15) SỬA bán kính kích hoạt của một địa điểm
UPDATE Places SET RadiusMeters = 150, UpdatedAt = UTC_TIMESTAMP(6) WHERE Slug = 'thu-nghiem';

-- 16) XÓA địa điểm thử (bản dịch và lượt nghe liên quan bị xóa theo)
DELETE FROM Places WHERE Slug = 'thu-nghiem';
