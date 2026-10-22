# Saigon Audio Guide — Hệ thống thuyết minh tự động đa ngôn ngữ (TP.HCM)

Ứng dụng thuyết minh du lịch theo GPS: du khách mở trang web, cho phép định vị, rồi đi tới các địa điểm nổi tiếng ở TP.HCM —
khi bước vào phạm vi một địa điểm, ứng dụng **tự động đọc và hiển thị thuyết minh** bằng ngôn ngữ của thiết bị (hoặc ngôn ngữ họ chọn).
Trang **Admin** quản lý địa điểm, nội dung đa ngôn ngữ, thống kê lượt nghe và **xem ai đang truy cập**.
Dữ liệu mẫu: **6 địa điểm × 5 ngôn ngữ** (vi, en, zh, ja, ko).

## Tính năng chính
- **Tự thuyết minh theo GPS**: cấp quyền định vị một lần; đi vào bán kính địa điểm → hiện tên + văn bản (tô sáng từng câu) và phát giọng đọc/mp3, không cần bấm thêm. Chỉ kích hoạt khi *đi vào* vùng, có cooldown 10 phút, hàng đợi khi đi qua nhiều địa điểm liền nhau.
- **Ngôn ngữ mặc định = ngôn ngữ thiết bị** (vi-VN → vi, zh-TW → zh…; chưa hỗ trợ → en) và có ô chọn ngôn ngữ / "Tự động".
- **Theo dõi truy cập** (admin → tab *Truy cập*): đang online, thiết bị/trình duyệt/HĐH, IP, ngôn ngữ, vị trí gần đúng (làm tròn ~110 m, chỉ khi bật GPS thật), số lượt nghe, khách mới/quay lại, biểu đồ theo ngày.
- Admin: CRUD địa điểm & bản dịch, **tải lên mp3/ảnh**, quản lý ngôn ngữ, thống kê, **đổi mật khẩu**.
- Bảo mật cơ bản: JWT, PBKDF2, giới hạn tần suất (login/ping/play-log), nhận IP thật khi chạy sau proxy.

## Kiến trúc 3 lớp (.NET 10 + MySQL/MariaDB)
```
Presentation  AudioGuide.API   ASP.NET Core Web API + giao diện web
                               ├─ /        → ứng dụng du khách (Leaflet + Geolocation + Speech Synthesis)
                               └─ /admin/  → trang quản trị (JWT)
Business      AudioGuide.BLL   Service, DTO, validation, Haversine, băm mật khẩu, nhận diện ngôn ngữ/thiết bị
Data access   AudioGuide.DAL   EF Core (Pomelo MySQL), Repository, seed dữ liệu, nâng cấp schema
Tests         AudioGuide.Tests xUnit (EF Core InMemory)
```

## Chạy local với XAMPP + phpMyAdmin
Yêu cầu: [.NET SDK 10](https://dotnet.microsoft.com/download) và XAMPP.

1. Mở **XAMPP Control Panel** → bấm **Start** ở dòng **MySQL** (Apache chỉ cần nếu muốn dùng phpMyAdmin).
2. Chạy ứng dụng:
   ```bash
   dotnet run --project src/AudioGuide.API --urls http://localhost:5000
   ```
   - **Chưa có database**: ứng dụng tự tạo database `hcmc_audioguide`, toàn bộ bảng và nạp dữ liệu mẫu.
   - **Đã có database** (từ phiên bản cũ): ứng dụng tự **bổ sung** bảng `Visitors`, `VisitSessions` và cột `PlayLogs.VisitorId`, **giữ nguyên dữ liệu cũ**. Chạy lại nhiều lần vẫn an toàn.
3. Mở **http://localhost/phpmyadmin** → chọn database `hcmc_audioguide` để xem dữ liệu (bảng `Visitors`, `VisitSessions`, `PlayLogs`…).
4. Du khách: http://localhost:5000/ · Admin: http://localhost:5000/admin/ — tài khoản `admin` / `Admin@123` (**hãy đổi ngay bằng nút "Đổi mật khẩu"**).

Chuỗi kết nối mặc định (`appsettings.json`) dùng tài khoản XAMPP mặc định `root` không mật khẩu, cổng 3306:
`Server=localhost;Port=3306;Database=hcmc_audioguide;User=root;Password=;CharSet=utf8mb4;`
Nếu root của bạn có mật khẩu hoặc MySQL chạy cổng khác (vd 3307), ghi đè bằng biến môi trường, không cần sửa file:
```bash
# Windows PowerShell
$env:ConnectionStrings__Default = "Server=localhost;Port=3307;Database=hcmc_audioguide;User=root;Password=matkhau;CharSet=utf8mb4;"
```
Ứng dụng tự dò phiên bản máy chủ (MariaDB của XAMPP hoặc MySQL 8). Có thể chỉ định tay bằng `Database__ServerVersion` (vd `mariadb-10.4.32` hoặc `8.0.36`).

> Lưu ý gói: Pomelo 9.0.0 chỉ tương thích EF Core 9.0.x (chưa có bản cho EF Core 10), nhưng vẫn chạy trên runtime .NET 10. Không thêm package `Microsoft.EntityFrameworkCore.*` bản 10.

### Thử GPS thật trên điện thoại
Trình duyệt chỉ cấp GPS trên **HTTPS** hoặc `localhost`. Chạy `dotnet run ... --urls http://0.0.0.0:5000` rồi mở tunnel HTTPS (ngrok / Cloudflare Tunnel) tới cổng 5000 và mở link đó trên điện thoại.

### Demo không cần đi thật
Tích **"Chế độ demo"** rồi nhấp lên bản đồ vào gần một địa điểm: ứng dụng tự phát thuyết minh như khi đi thật (vị trí demo **không** được lưu vào thống kê truy cập).

### Lưu ý về âm thanh tự động
Trình duyệt chỉ cho phát tiếng sau khi người dùng chạm vào trang ít nhất một lần (nút **Bắt đầu** ở màn hình chào đã làm việc này). Nếu địa điểm kích hoạt trước lần chạm đầu tiên, chữ vẫn hiện ngay và có nút "Chạm để nghe". Trang cần được mở (màn hình sáng) khi di chuyển — ứng dụng tự xin giữ màn hình sáng nếu thiết bị hỗ trợ. Giọng đọc dùng Web Speech API; máy cần có giọng cho ngôn ngữ tương ứng (hoặc gắn file mp3 trong Admin).

## Chạy bằng Docker
```bash
docker compose up --build
```
Du khách: http://localhost:8080/ · Admin: http://localhost:8080/admin/ (file tải lên lưu trong volume `uploads`).

## Triển khai (ví dụ Render)
Render chạy được `Dockerfile` của dự án (tự cấp biến `PORT`, ứng dụng đã đọc biến này). Cần:
- **Database MySQL/MariaDB bên ngoài** (Render không có MySQL; dùng dịch vụ MySQL cloud như Aiven, TiDB Cloud, Railway…) → đặt `ConnectionStrings__Default`.
- Biến môi trường: `Jwt__Key` (≥ 32 ký tự ngẫu nhiên), `Seed__AdminPassword`, `Hosting__TrustProxy=true` (để lấy IP thật của khách qua proxy của Render).
- Ổ đĩa của gói miễn phí là tạm thời: file tải lên sẽ mất khi redeploy. Dùng URL âm thanh/ảnh bên ngoài, hoặc gắn Persistent Disk và đặt `Storage__UploadPath` trỏ vào đó.
- Render cấp HTTPS sẵn nên GPS hoạt động trên điện thoại.

## API chính
| Phương thức | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/public/languages` | Ngôn ngữ đang kích hoạt |
| GET | `/api/public/places?lang=&lat=&lng=` | Địa điểm theo ngôn ngữ (có fallback). Bỏ `lang` → theo `Accept-Language` |
| GET | `/api/public/places/nearby?lat=&lng=&lang=` | Địa điểm gần nhất trong bán kính (204 nếu không có) |
| POST | `/api/public/play-log` | Ghi lượt nghe (kèm `clientId` để biết ai nghe) |
| POST | `/api/public/visits/ping` | Báo "đang online" (mỗi 30 giây) |
| POST | `/api/admin/auth/login` | Đăng nhập, trả JWT (giới hạn 10 lần/phút/IP) |
| PUT | `/api/admin/auth/password` | Đổi mật khẩu 🔒 |
| GET/POST/PUT/DELETE | `/api/admin/places[/{id}]` | CRUD địa điểm 🔒 |
| PUT/DELETE | `/api/admin/places/{id}/translations/{lang}` | Thêm/sửa/xóa bản dịch 🔒 |
| GET/POST/PUT | `/api/admin/languages[/{code}]` | Quản lý ngôn ngữ 🔒 |
| GET | `/api/admin/stats?days=30` | Thống kê lượt nghe 🔒 |
| GET | `/api/admin/visitors/overview?days=` · `/online` · `?days=&page=` | Người truy cập 🔒 |
| POST | `/api/admin/uploads/audio` · `/image` | Tải file lên 🔒 |

## Quyền riêng tư
Ứng dụng ghi nhận **ẩn danh**: mã thiết bị ngẫu nhiên (UUID lưu trên trình duyệt), ngôn ngữ, loại thiết bị/trình duyệt, IP, và — chỉ khi người dùng bật GPS thật — vị trí làm tròn ~110 m. Có thông báo hiển thị cho du khách. Nếu triển khai công khai, nên bổ sung chính sách quyền riêng tư và có kế hoạch xóa dữ liệu cũ.

## CI/CD (GitHub Actions — `.github/workflows/ci-cd.yml`)
1. **build-test**: restore → build → test (mọi push/PR).
2. **docker**: build image và đẩy lên GHCR (push vào `main`).
3. **deploy**: SSH vào server, `docker compose pull && up -d`. Cần secrets: `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, `JWT_KEY`, `MYSQL_ROOT_PASSWORD`, `ADMIN_PASSWORD`.

## Tài liệu
`docs/FocusedUseCases.md` — use case UC1–UC12 theo mẫu của giảng viên, business rules BR1–BR13, use case diagram, activity diagram (UC3), ERD (Mermaid).
