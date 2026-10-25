# Saigon Audio Guide — Hệ thống thuyết minh tự động đa ngôn ngữ (TP.HCM)

Ứng dụng thuyết minh du lịch theo GPS: du khách mở trang web, cho phép định vị, rồi đi tới các địa điểm nổi tiếng ở TP.HCM —
khi bước vào phạm vi một địa điểm, ứng dụng **tự động đọc và hiển thị thuyết minh** bằng ngôn ngữ của thiết bị (hoặc ngôn ngữ họ chọn).
Trang **Admin** quản lý địa điểm, nội dung đa ngôn ngữ, thống kê lượt nghe và **xem ai đang truy cập**.
Dữ liệu mẫu: **6 địa điểm × 5 ngôn ngữ** (vi, en, zh, ja, ko).

## Tính năng chính
- **Tự thuyết minh theo GPS**: cấp quyền định vị một lần; đi vào bán kính địa điểm → hiện tên + văn bản (tô sáng từng câu) và phát giọng đọc/mp3, không cần bấm thêm. Chỉ kích hoạt khi *đi vào* vùng; sau khi đọc xong phải chờ 45 giây mới tự phát lại cùng địa điểm; hàng đợi khi đi qua nhiều địa điểm liền nhau.
- **Giao diện bản đồ lớn**: bản đồ chiếm toàn màn hình; nội dung thuyết minh và danh sách địa điểm nằm trong bảng bên trái, có thể **thu nhỏ / mở rộng** để không che bản đồ.
- **Ngôn ngữ mặc định = ngôn ngữ thiết bị** (vi-VN → vi, zh-TW → zh…; chưa hỗ trợ → en) và có ô chọn ngôn ngữ / "Tự động".
- **Theo dõi truy cập** (admin → tab *Truy cập*): đang online, thiết bị/trình duyệt/HĐH, IP, ngôn ngữ, vị trí gần đúng (làm tròn ~110 m, chỉ khi bật GPS thật), số lượt nghe, khách mới/quay lại, biểu đồ theo ngày.
- Admin: CRUD địa điểm & bản dịch (nhập tay từng ngôn ngữ, có nút **Lưu tất cả bản dịch**), **tải lên mp3/ảnh**, quản lý ngôn ngữ, thống kê, **đổi mật khẩu**.
- Bảo mật cơ bản: JWT, PBKDF2, giới hạn tần suất (login/ping/play-log).

## Kiến trúc 3 lớp (.NET 10 + MySQL 8)
```
Presentation  AudioGuide.API   ASP.NET Core Web API + giao diện web
                               ├─ /        → ứng dụng du khách (Leaflet + Geolocation + Speech Synthesis)
                               └─ /admin/  → trang quản trị (JWT)
Business      AudioGuide.BLL   Service, DTO, validation, Haversine, băm mật khẩu, nhận diện ngôn ngữ/thiết bị
Data access   AudioGuide.DAL   EF Core (Pomelo MySQL), Repository, seed dữ liệu
Tests         AudioGuide.Tests xUnit (EF Core InMemory)
```

## Chạy bằng Docker (cách chạy chính thức của dự án)
Cần cài **Docker Desktop** (Windows dùng WSL2) và bật nó lên. Không cần cài .NET hay MySQL.

```bash
git clone https://github.com/DangHau19/CNPM.git
cd CNPM
docker compose up --build
```
Lần đầu build mất vài phút. Khi thấy dòng báo ứng dụng đang lắng nghe, mở:

| Dịch vụ | Địa chỉ | Ghi chú |
|---|---|---|
| Du khách | http://localhost:8080/ | |
| Admin | http://localhost:8080/admin/ | `admin` / `Admin@123` (đổi bằng nút **Đổi mật khẩu**) |
| phpMyAdmin | http://localhost:8081/ | đăng nhập `root` / `root` → database `hcmc_audioguide` → bảng `VisitSessions`, `Visitors`, `PlayLogs`… |
| MySQL (công cụ ngoài) | `localhost:3307` | user `root`, mật khẩu `root` |

Ứng dụng tự tạo database, các bảng và dữ liệu mẫu ở lần chạy đầu.

**Lệnh thường dùng**
```bash
docker compose up -d --build   # chạy nền (sau khi sửa code thì thêm --build)
docker compose logs -f api     # xem log của ứng dụng
docker compose down            # dừng (giữ dữ liệu)
docker compose down -v         # dừng và XÓA toàn bộ dữ liệu (database + file tải lên)
```
Muốn đổi mật khẩu/khóa: sao chép `.env.example` thành `.env` rồi sửa (xem file đó). Dữ liệu nằm trong volume Docker `mysql_data`, file admin tải lên nằm trong volume `uploads`.

## Cho người khác truy cập qua Internet (Cloudflare Tunnel)
Ứng dụng vẫn chạy trên máy bạn; Cloudflare tạo một link **https** công khai trỏ vào nó (miễn phí, không cần tài khoản). Link https cũng giúp **GPS hoạt động trên điện thoại**.
1. Sao chép `.env.example` thành `.env` và **đổi** `MYSQL_ROOT_PASSWORD`, `ADMIN_PASSWORD`, `JWT_KEY` (không dùng mật khẩu mặc định khi công khai).
2. Chạy: `docker compose --profile tunnel up --build -d`
3. Lấy link: `docker compose logs tunnel` rồi tìm dòng có `https://....trycloudflare.com` (PowerShell: `docker compose logs tunnel | Select-String trycloudflare`). Gửi link đó cho mọi người.
4. Yêu cầu: máy bạn phải **bật, có mạng, Docker Desktop đang chạy** (tắt chế độ Sleep). Link **đổi mỗi lần chạy lại** tunnel. Dừng: `docker compose --profile tunnel down`.

**Trang Admin chỉ vào được từ máy bạn** (`http://localhost:8080/admin/`): qua link công khai, `/admin` và `/api/admin` trả 404 (bật mặc định, tắt bằng `ADMIN_LOCAL_ONLY=false` trong `.env`). Admin và khách dùng chung một database nên thay đổi ở admin hiện ngay cho khách.

phpMyAdmin (cổng 8081) và MySQL (cổng 3307) chỉ mở trên máy bạn (`127.0.0.1`), **không** đi qua link công khai — đừng đưa chúng ra Internet.

## Xem và truy vấn database
Mở http://localhost:8081/ (`root` + mật khẩu trong `.env`, mặc định `root`) → database `hcmc_audioguide`. Tab **SQL** để chạy câu lệnh; các câu mẫu (đang online, lượt truy cập theo ngày, thiết bị, ngôn ngữ, địa điểm được nghe nhiều, thêm địa điểm bằng SQL…) nằm trong [`docs/queries.sql`](docs/queries.sql). Dữ liệu người dùng vào bằng link công khai hiện ngay trong các bảng này.

### Demo không cần đi thật
Tích **"Chế độ demo"** rồi nhấp lên bản đồ vào bên trong vòng tròn của một địa điểm: ứng dụng tự phát thuyết minh như khi đi thật (vị trí demo **không** được lưu vào thống kê truy cập). Ở chế độ demo không có thời gian chờ: nhấp ra ngoài vòng tròn rồi nhấp lại vào trong là nghe lại ngay.

### Lưu ý về âm thanh tự động
Trình duyệt chỉ cho phát tiếng sau khi người dùng chạm vào trang ít nhất một lần (nút **Bắt đầu** ở màn hình chào đã làm việc này). Nếu địa điểm kích hoạt trước lần chạm đầu tiên, chữ vẫn hiện ngay và có nút "Chạm để nghe". Trang cần được mở (màn hình sáng) khi di chuyển — ứng dụng tự xin giữ màn hình sáng nếu thiết bị hỗ trợ. Giọng đọc dùng Web Speech API; máy cần có giọng cho ngôn ngữ tương ứng (hoặc gắn file mp3 trong Admin).

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
Ứng dụng ghi nhận **ẩn danh**: mã thiết bị ngẫu nhiên (UUID lưu trên trình duyệt), ngôn ngữ, loại thiết bị/trình duyệt, IP, và — chỉ khi người dùng bật GPS thật — vị trí làm tròn ~110 m. Có thông báo hiển thị cho du khách. Lịch sử truy cập cũ hơn 90 ngày tự động bị xóa (`Retention__VisitDays`).

## CI (GitHub Actions — `.github/workflows/ci.yml`)
Mỗi lần push/PR: restore → build → test, rồi kiểm tra Docker image build được. (Chưa có bước deploy; sẽ bổ sung khi cần.)

## Tài liệu
`docs/FocusedUseCases.md` — use case UC1–UC12 theo mẫu của giảng viên, business rules BR1–BR13, use case diagram, activity diagram (UC3), ERD (Mermaid).
