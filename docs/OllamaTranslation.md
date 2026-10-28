# Dịch tự động bằng Ollama

## Chạy trên máy của bạn với Docker Desktop

Từ thư mục gốc dự án, mở PowerShell thứ nhất:

```powershell
.\scripts\start-ollama.ps1 -DockerAccess
```

Nếu báo server cũ chỉ nghe ở localhost, chờ các tác vụ dịch cũ xong rồi chạy:

```powershell
.\scripts\start-ollama.ps1 -DockerAccess -Restart
```

Giữ terminal này mở. Khi có dòng `Listening`, Ollama đã sẵn sàng và đang chờ yêu cầu, không phải đang bị treo.
Script dùng bản Ollama tại `D:\Tools\OllamaTrial\runtime\ollama.exe` và lưu mô hình tại `D:\Tools\OllamaTrial\models`.
Nếu cài Ollama theo cách thông thường, script tìm lệnh `ollama` trong PATH; cũng có thể truyền `-OllamaExe` và `-ModelsPath`.
Chế độ `-DockerAccess` nghe ở mọi giao diện để Docker truy cập được. Không chuyển tiếp cổng 11434 ra Internet.

Mở PowerShell thứ hai, kiểm tra mô hình:

```powershell
Invoke-RestMethod http://localhost:11434/api/tags
```

Nếu chưa có `translategemma:4b`, tải một lần (mô hình đã tải trước đó không cần tải lại):

```powershell
& 'D:\Tools\OllamaTrial\runtime\ollama.exe' pull translategemma:4b
```

Bật Docker Desktop và chạy ứng dụng:

```powershell
docker compose up --build -d
```

Mở **http://localhost:8080/admin/** và nhấn **Ctrl+F5**. Tài khoản mẫu là `admin` / `Admin@123` nếu chưa đổi.

Compose đã có cấu hình mặc định. Nếu dùng `.env` riêng, giữ các thiết lập database/JWT và bổ sung:

```dotenv
OLLAMA_BASE_URL=http://host.docker.internal:11434
OLLAMA_MODEL=translategemma:4b
OLLAMA_TIMEOUT_SECONDS=600
```

## Dùng trong trang quản trị

1. Trong **Ngôn ngữ**, có tiếng Việt `vi` và kích hoạt các ngôn ngữ muốn dịch. Nhập đúng mã, tên ngôn ngữ và locale giọng đọc.
2. Vào **Địa điểm → Thêm địa điểm**, nhập thông tin chung, tên và thuyết minh ở khung tiếng Việt.
3. Nhấn **Dịch tất cả ngôn ngữ còn thiếu**. Không cần lưu địa điểm trước. Nếu có 10 ngôn ngữ gồm tiếng Việt, nút này dịch sang 9 ngôn ngữ còn lại.
4. Giữ trang mở và theo dõi tiến độ. Có thể nhấn **Dừng dịch**; những kết quả hoàn thành vẫn giữ trên form.
5. Kiểm tra, sửa bản dịch, nhất là tên riêng và thông tin lịch sử, rồi nhấn **Lưu địa điểm và tất cả bản dịch**.

Khung đã có tên, thuyết minh hoặc URL âm thanh sẽ được bỏ qua để giữ nội dung bạn đã sửa. Khi một ngôn ngữ lỗi,
các ngôn ngữ khác vẫn tiếp tục. Nhấn lại để thử các khung còn trống. Thêm ngôn ngữ mới rồi mở lại địa điểm để dịch bổ sung.
Kết quả chưa lưu database cho đến khi nhấn lưu; đóng/tải lại trang trước khi lưu sẽ mất bản nháp.
Địa chỉ, tọa độ, ảnh dùng chung, không được dịch. Âm thanh bản dịch để trống để ứng dụng dùng giọng đọc trình duyệt nếu thiết bị có hỗ trợ.

## Chạy trực tiếp bằng .NET

Chạy `.\scripts\start-ollama.ps1` ở terminal thứ nhất. Terminal thứ hai cần .NET 10 và MySQL đang chạy:

```powershell
$env:Ollama__BaseUrl = 'http://127.0.0.1:11434'
$env:Ollama__Model = 'translategemma:4b'
# Sửa cho đúng tài khoản/cổng MySQL của bạn; ví dụ dưới dùng MySQL Docker của dự án.
$env:ConnectionStrings__Default = 'Server=localhost;Port=3307;Database=hcmc_audioguide;User=root;Password=root;CharSet=utf8mb4;'
dotnet run --project src/AudioGuide.API --urls http://localhost:5080
```

Mở **http://localhost:5080/admin/**. `.env` chỉ được Compose đọc, không tự nạp bởi `dotnet run`.

## Lỗi và tốc độ

- Máy ít RAM chạy bằng CPU có thể mất vài phút mỗi ngôn ngữ. Tên và thuyết minh được dịch riêng; văn bản dài được chia thành các đoạn nhỏ để tránh vượt giới hạn mô hình.
- Không kết nối: kiểm tra Ollama đang chạy, firewall và `Ollama:BaseUrl`. Container phải dùng `host.docker.internal`, không dùng `localhost` để trỏ tới Windows.
- Chưa có mô hình: kiểm tra `/api/tags`, tải mô hình đúng tên đã cấu hình.
- Quá thời gian hoặc thiếu RAM: đóng bớt ứng dụng, rút ngắn nguồn; có thể tăng `OLLAMA_TIMEOUT_SECONDS` (30–3600 giây). Khởi động lại API sau khi thay cấu hình.
- Bản dịch bị ngắt, rỗng hoặc quá giới hạn lưu sẽ báo lỗi để bạn xử lý, không tự lưu nội dung thiếu.
- Backend chỉ dịch một ngôn ngữ tại một thời điểm để tránh nhiều yêu cầu tranh RAM.
- Chất lượng và số ngôn ngữ phụ thuộc mô hình. Thêm ngôn ngữ vào database không làm mô hình tự hỗ trợ ngôn ngữ đó.
- Không cần Google API key; các lệnh trên chạy mô hình local và tắt cloud.

## Dừng và chạy lại

Nhấn **Ctrl+C** ở terminal Ollama; chạy `docker compose stop` ở terminal khác để dừng website mà giữ dữ liệu.
Lần sau chạy lại script Ollama và `docker compose up -d`. Nếu sửa code, dùng `docker compose up --build -d`.

## API và kiểm thử

`POST /api/admin/places/auto-translate` yêu cầu JWT admin. Nhận `{ "name": "Tên Việt", "narrationText": "Thuyết minh", "targetLanguage": "en" }`, trả `TranslationDto` để xem trước, không ghi database.
Giao diện gọi tuần tự các ngôn ngữ đang kích hoạt. Yêu cầu bị hủy khi nhấn Dừng dịch.

```powershell
dotnet test HcmcAudioGuide.slnx
node --test tests/admin-translation.test.cjs
```

Tài liệu: [Ollama API](https://docs.ollama.com/api/chat), [TranslateGemma](https://ollama.com/library/translategemma:4b).
