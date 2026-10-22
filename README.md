# Saigon Audio Guide — Hệ thống thuyết minh tự động đa ngôn ngữ (TP.HCM)

Ứng dụng thuyết minh du lịch theo GPS: du khách chọn ngôn ngữ, đi tới các địa điểm nổi tiếng ở TP.HCM và nghe thuyết minh tự động.
Có trang **Admin** riêng để quản lý địa điểm, nội dung đa ngôn ngữ và xem thống kê. Dữ liệu mẫu: **6 địa điểm × 5 ngôn ngữ** (vi, en, zh, ja, ko).

## Kiến trúc 3 lớp (.NET 10 + MySQL 8)
```
Presentation  AudioGuide.API   ASP.NET Core Web API + giao diện web
                               ├─ /        → ứng dụng du khách (Leaflet + Geolocation + Speech Synthesis)
                               └─ /admin/  → trang quản trị (JWT)
Business      AudioGuide.BLL   Service, DTO, validation, Haversine, băm mật khẩu
Data access   AudioGuide.DAL   EF Core (Pomelo MySQL), Repository, seed dữ liệu
Tests         AudioGuide.Tests xUnit (EF Core InMemory)
```
API chỉ tham chiếu BLL; BLL tham chiếu DAL (`AddBusinessLogic` đăng ký cả DAL).

## Chạy nhanh bằng Docker
```bash
docker compose up --build
```
- Du khách: http://localhost:8080/
- Admin: http://localhost:8080/admin/ — tài khoản mặc định `admin` / `Admin@123` (**đổi trước khi triển khai thật**)

## Chạy bằng .NET SDK 10
1. Chạy MySQL 8 và sửa `ConnectionStrings:Default` trong `src/AudioGuide.API/appsettings.json`.
2. `dotnet run --project src/AudioGuide.API` — tự tạo database/bảng và nạp dữ liệu mẫu ở lần chạy đầu.
3. `dotnet test` để chạy unit test.

> Lưu ý gói: Pomelo 9.0.0 chỉ tương thích EF Core 9.0.x (chưa có bản cho EF Core 10), nhưng vẫn chạy trên runtime .NET 10. Không thêm package `Microsoft.EntityFrameworkCore.*` bản 10.

## Demo GPS
- Trình duyệt chỉ cấp GPS trên **HTTPS** hoặc `localhost`. Test trên điện thoại: dùng tunnel HTTPS (ngrok/Cloudflare Tunnel) hoặc triển khai sau reverse proxy HTTPS.
- Không cần đi thật: tích **"Chế độ demo"**, bấm "Bật thuyết minh tự động" rồi nhấp lên bản đồ vào gần một địa điểm.
- Giọng đọc dùng Web Speech API của trình duyệt; máy cần có giọng cho ngôn ngữ tương ứng (Chrome/Edge trên Windows, Android và iOS thường đủ). Có thể gắn file mp3 cho từng bản dịch trong trang Admin.

## API chính
| Phương thức | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/public/languages` | Ngôn ngữ đang kích hoạt |
| GET | `/api/public/places?lang=&lat=&lng=` | Danh sách địa điểm theo ngôn ngữ (có fallback), sắp xếp theo khoảng cách |
| GET | `/api/public/places/nearby?lat=&lng=&lang=` | Địa điểm gần nhất trong bán kính (204 nếu không có) |
| POST | `/api/public/play-log` | Ghi lượt nghe |
| POST | `/api/admin/auth/login` | Đăng nhập, trả JWT |
| GET/POST/PUT/DELETE | `/api/admin/places[/{id}]` | CRUD địa điểm 🔒 |
| PUT/DELETE | `/api/admin/places/{id}/translations/{lang}` | Thêm/sửa/xóa bản dịch 🔒 |
| GET/POST/PUT | `/api/admin/languages[/{code}]` | Quản lý ngôn ngữ 🔒 |
| GET | `/api/admin/stats?days=30` | Thống kê lượt nghe 🔒 |

## CI/CD (GitHub Actions — `.github/workflows/ci-cd.yml`)
1. **build-test**: restore → build → test (mọi push/PR).
2. **docker**: build image và đẩy lên GHCR (push vào `main`).
3. **deploy**: SSH vào server, `docker compose pull && up -d`. Cần secrets: `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, `JWT_KEY`, `MYSQL_ROOT_PASSWORD`, `ADMIN_PASSWORD`. Trên server cần cài Docker và đăng nhập GHCR nếu image ở chế độ private.

## Tài liệu
`docs/FocusedUseCases.md` — use case theo mẫu của giảng viên, use case diagram, activity diagram (UC3), ERD (Mermaid).
