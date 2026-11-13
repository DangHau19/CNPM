# Phân công 10 ngày — điều chỉnh theo phụ thuộc code

Cập nhật ngày 10/11/2026. Thành viên: Quốc (nhóm trưởng), Doanh và Hậu. Bảng này thay lịch phân công cũ cho phần chưa thực hiện; giữ nguyên các file và commit đã bàn giao. Đây là kế hoạch, không phải xác nhận hoàn thành.

## Cách áp dụng từ tiến độ hiện tại

- Quốc đã bàn giao ngày 1 (solution/project) và ngày 2 (cấu hình, tài liệu). Các thay đổi đang có trên máy cần được người dùng kiểm tra và tự commit/push khi phù hợp.
- Doanh đã bàn giao project DAL và entity ngày 1. Hậu đã bàn giao HTML/CSS ngày 1 và `API/Properties/launchSettings.json`; giữ nguyên các phần này. Không xóa hoặc đưa lại code để tạo commit mới.
- Chưa thấy phần ngày 2 của Doanh/Hậu trong phiên bản repo đã kiểm tra. Hoàn thành các đầu việc nền tảng ở hai dòng ngày 2 bên dưới trước các việc phụ thuộc ở ngày 3. Nếu làm sau ngày dự kiến, ghi ngày thực tế vào nhật ký; không đổi ngày commit.
- Mỗi người dự kiến 1 commit công việc/ngày, 10 commit/người; commit chuẩn bị repo tính riêng. Nếu lịch thực tế lệch, ưu tiên đúng phụ thuộc và ghi nhận trung thực, không tạo commit rỗng để bù số lượng.
- Chỉ cập nhật file trên máy trong lần điều chỉnh này. Người dùng tự quyết định commit và push; không sửa các commit đã có.

**Quy ước:** `API/` = `src/AudioGuide.API/`; `BLL/` = `src/AudioGuide.BLL/`; `DAL/` = `src/AudioGuide.DAL/`; `TEST/` = `tests/AudioGuide.Tests/`. Các đường dẫn khác tính từ gốc repository.

**Cách dùng cột commit:** đây là tiêu đề để nhập vào `git commit -m "tieu de"` sau khi đã stage đúng file. `feat` dùng khi thêm chức năng/code, `chore` cho cấu hình/hạ tầng, `test` cho kiểm thử, `docs` cho tài liệu và bằng chứng kiểm tra, `ci` cho workflow. Chỉ dùng tiêu đề phù hợp với thay đổi thực tế. Nếu sửa lỗi cụ thể, đổi thành `fix: ...` và nêu đúng lỗi; không đặt `test` khi chưa thêm/sửa kiểm thử. Những ngày đã có commit giữ nguyên mã và tiêu đề cũ, không tạo thêm commit theo gợi ý để thay thế.

## Quốc — nền tảng dùng chung, bảo mật và tích hợp

| Ngày | Phạm vi file/công việc | Điều kiện trước khi thực hiện và kết quả cần kiểm tra | Tiêu đề commit gợi ý / commit đã có |
|---|---|---|---|
| 1 — giữ nguyên | `HcmcAudioGuide.slnx`, `.gitignore`, `API/AudioGuide.API.csproj`, `BLL/AudioGuide.BLL.csproj`, `TEST/AudioGuide.Tests.csproj` | Đã bàn giao. Kiểm tra solution và tham chiếu; nhật ký bổ sung ghi đúng thời điểm kiểm tra. | Đã có `316ffa4`: `Rà soát cấu trúc solution, tham chiếu project và danh sách file bỏ qua` — giữ nguyên |
| 2 — giữ nguyên | `API/appsettings.json`, `.env.example`, `docs/CauHinhMoiTruong.md`, `docs/nhat-ky/quoc-10-ngay.md` | Đã có commit bàn giao. Rà soát cấu hình, hướng dẫn và nhật ký; chưa xác nhận chạy API. | Đã có `f47fb90`: `chore: hoan thien cau hinh ngay 2 va bo sung nhat ky ngay 1` — giữ nguyên |
| 3 | `BLL/Dtos.cs`, `BLL/Exceptions.cs`, `BLL/Security/PasswordHasher.cs`, `BLL/Visitors/UserAgentParser.cs`, `BLL/Visitors/LanguageNegotiator.cs` | Bàn giao kiểu dữ liệu và helper đầy đủ trước các service. DTO do Quốc phụ trách chính từ lịch mới; không còn để đến ngày 5 của Doanh. | `feat: them DTO exception va cac helper nghiep vu` |
| 4 | `BLL/Services/AuthService.cs`, `API/Security/JwtTokenService.cs`, `API/Middleware/GlobalExceptionHandler.cs` | Sau DAL ngày 2 và helper ngày 3; kiểm tra chữ ký phương thức, tên DTO, issuer/audience và lỗi nghiệp vụ. Chưa đưa controller/Program lên. | `feat: them xac thuc JWT va xu ly loi API` |
| 5 | `BLL/Services/VisitorService.cs`, `API/Storage/VisitCleanupService.cs`, `BLL/DependencyInjection.cs` | Sau toàn bộ DAL ngày 3 và các service của Doanh ngày 4/Hậu ngày 3. Đưa VisitorService trước cleanup và đăng ký BLL; lúc này mọi kiểu trong AddBusinessLogic phải tồn tại. | `feat: them theo doi truy cap don lich su va dang ky BLL` |
| 6 | `API/Controllers/AdminAuthController.cs`, `API/Controllers/AdminVisitorsController.cs`, `API/Program.cs` | Sau upload/controller Doanh ngày 5 và dịch Ollama Hậu ngày 4. Tích hợp Program nguyên vẹn khi mọi kiểu nó tham chiếu đã có; restore/build solution. Sau đó Hậu bổ sung PublicController và chạy test nghiệp vụ. | `feat: tich hop khoi dong API va controller xac thuc truy cap` |
| 7 | `TEST/VisitorTests.cs`; sửa lỗi thực tế ở phần Quốc phụ trách | Sau API ngày 6; chạy kiểm thử nhận diện thiết bị, ngôn ngữ, ping, phiên truy cập và phân trang. Ghi rõ kiểm tra tự động và kiểm tra cần DB thật. | `test: them kiem thu thiet bi ngon ngu va phien truy cap` |
| 8 | `Dockerfile`, `docker-compose.yml`, `.dockerignore` | Sau khi build .NET và kiểm thử ngày 6–7 đạt. Kiểm tra build image, khởi động API/MySQL, cổng và volume; chỉ ghi đạt khi đã chạy được. | `chore: them Docker Compose cho API va MySQL` |
| 9 | `.github/workflows/ci.yml`; kiểm tra tích hợp và sửa lỗi thực tế | Sau Docker và đủ bộ test .NET/JS. Chạy tương đương workflow trên máy. Sau khi người dùng push/PR vào main mới có thể xác nhận kết quả GitHub Actions. | `ci: them workflow build test va Docker image` |
| 10 | `README.md`, tài liệu phân công và `docs/nhat-ky/quoc-10-ngay.md` | Tổng hợp cách chạy, trạng thái CI thực tế, kết quả nghiệm thu và lỗi còn tồn tại. Kiểm tra đủ phạm vi bàn giao, không đổi tác giả hoặc lịch sử commit. | `docs: hoan thien huong dan chay va bao cao nghiem thu` |

## Doanh — dữ liệu, quản trị và thống kê

| Ngày | Phạm vi file/công việc | Điều kiện trước khi thực hiện và kết quả cần kiểm tra | Tiêu đề commit gợi ý / commit đã có |
|---|---|---|---|
| 1 — giữ nguyên | `DAL/AudioGuide.DAL.csproj`, `DAL/Entities/Entities.cs` | Đã bàn giao. Giữ mô hình gốc và quan hệ entity. | Đã có `417ecc4`: `khóa và quan hệ dữ liệu` — giữ nguyên |
| 2 — ưu tiên nền tảng | `DAL/AppDbContext.cs`, `DAL/DbConstraints.cs`, `DAL/Repositories/Repositories.cs`, `DAL/DependencyInjection.cs` | Đưa DbContext/ràng buộc, toàn bộ interface và implementation repository trong cùng một commit; sau đó mới đăng ký DAL. Không đưa file DI tham chiếu repository chưa tồn tại. Build riêng project DAL nếu môi trường đủ SDK/package. | `feat: them DbContext rang buoc repository va dang ky DAL` |
| 3 | `DAL/DbSeeder.cs`, `DAL/Seed/seed.json`, `docs/queries.sql` | Sau DAL ngày 2. Kiểm tra seeder, quan hệ khóa ngoại và dữ liệu mẫu. BLL.InitializeDatabaseAsync chỉ được đưa lên sau bước này. | `feat: them seeder du lieu mau va truy van SQL` |
| 4 | `BLL/Services/PlaceAdminService.cs`, `BLL/Services/LanguageService.cs`, `BLL/Services/StatsService.cs` | Sau DTO/exception của Quốc ngày 3 và DAL ngày 2–3. Kiểm tra CRUD, validation, bản dịch và tổng hợp lượt nghe; hoàn thành trước OllamaService của Hậu cùng ngày. | `feat: them nghiep vu quan ly dia diem ngon ngu va thong ke` |
| 5 | `API/Controllers/AdminPlacesController.cs`, `API/Controllers/AdminLanguagesController.cs`, `API/Controllers/AdminStatsController.cs`, `API/Controllers/AdminUploadsController.cs`, `API/Storage/UploadOptions.cs` | Sau dịch Ollama Hậu ngày 4 vì AdminPlacesController có endpoint auto-translate. Đưa UploadOptions cùng controller upload; các kiểu phải có trước Program của Quốc ngày 6. | `feat: them API quan tri dia diem ngon ngu thong ke va upload` |
| 6 | `API/wwwroot/admin/index.html`, toàn bộ `API/wwwroot/admin/admin.js` | Sau API quản trị ngày 5. Đưa JS nguyên vẹn, gồm helper, auth, CRUD, dịch, upload, ngôn ngữ, thống kê và truy cập. Thử luồng trên trình duyệt sau khi Quốc hoàn tất Program. | `feat: them giao dien quan tri va ket noi API` |
| 7 | `TEST/DbConstraintsTests.cs`; sửa lỗi thực tế ở DAL/admin | Sau API khởi động ngày 6. Chạy test ràng buộc hiện có; kiểm tra thêm CHECK trên MySQL thật khi DB sẵn sàng. Không coi test InMemory là bằng chứng DB MySQL đã chặn dữ liệu sai. | `test: them kiem thu rang buoc du lieu` |
| 8 | `docs/FocusedUseCases.md`; đối chiếu SQL, ERD và hành vi CRUD thực tế | Phối hợp Quốc sau khi Docker/MySQL chạy được; kiểm tra thêm/sửa/xóa, seed và thống kê, cập nhật tài liệu theo kết quả thực tế. | `docs: doi chieu use case ERD va ket qua kiem tra CRUD` |
| 9 | Kiểm tra admin end-to-end; sửa lỗi thực tế và cập nhật `docs/nhat-ky/doanh-10-ngay.md` | Kiểm tra đăng nhập, upload, dịch/lưu bản dịch, ngôn ngữ, thống kê, truy cập; đối chiếu UI–API–DB. Commit phải có sửa đổi hữu ích hoặc bằng chứng kiểm tra có thể tái hiện. | `docs: ghi nhan ket qua kiem tra luong quan tri end to end` |
| 10 | Chốt tài liệu dữ liệu/use case và `docs/nhat-ky/doanh-10-ngay.md` | Bàn giao kết quả, lỗi còn tồn tại và link commit; hỗ trợ nghiệm thu chung. | `docs: chot tai lieu du lieu va ket qua nghiem thu admin` |

## Hậu — API công khai, dịch và giao diện du khách

| Ngày | Phạm vi file/công việc | Điều kiện trước khi thực hiện và kết quả cần kiểm tra | Tiêu đề commit gợi ý / commit đã có |
|---|---|---|---|
| 1 — giữ nguyên | `API/wwwroot/index.html`, `API/wwwroot/css/site.css`; giữ `API/Properties/launchSettings.json` đã có | Đã bàn giao. Giao diện chưa có JS/API đầy đủ thì ghi rõ trạng thái; không ghi đã hoạt động end-to-end. | Đã có `56e07fa`: `giải quyết vấn đề bố cục trang web và các phát hiện về khả năng thích ứng thiết bị` — giữ nguyên |
| 2 — chuẩn bị độc lập | `BLL/Geo/GeoHelper.cs`, `scripts/start-ollama.ps1`, phần hướng dẫn chuẩn bị trong `docs/OllamaTranslation.md` | Helper địa lý và script độc lập với service. Kiểm tra khoảng cách, đọc script và ghi điều kiện Ollama; không đưa PlaceService hoặc PublicController khi thiếu repository/DTO. | `feat: them helper khoang cach va script chuan bi Ollama` |
| 3 | `BLL/Services/PlaceService.cs` | Sau DAL/repository Doanh ngày 2 và DTO Quốc ngày 3; Quốc bàn giao trước Hậu trong ngày này. Kiểm tra chọn bản dịch, fallback, ẩn địa điểm và khoảng cách. | `feat: them nghiep vu dia diem cong khai va fallback ngon ngu` |
| 4 | `API/Translation/OllamaTranslationService.cs` | Sau LanguageService của Doanh cùng ngày và DTO/exception Quốc ngày 3. Đưa đủ AutoTranslationRequest/OllamaOptions trong file; hoàn thành trước AdminPlacesController ngày 5. | `feat: them dich vu dich tu dong bang Ollama` |
| 5 | Toàn bộ `API/wwwroot/js/app.js` | Sau PlaceService, giữ đúng hợp đồng API/DTO. Đưa JS nguyên vẹn gồm state/helper, ngôn ngữ, bản đồ, GPS, âm thanh, log/ping và init; kiểm tra cú pháp. Luồng API chỉ kiểm tra được sau ngày 6. | `feat: them ban do GPS va phat thuyet minh cho du khach` |
| 6 | `API/Controllers/PublicController.cs`, `TEST/BllTests.cs` | Sau VisitorService Quốc ngày 5, Program Quốc cùng ngày và PlaceService ngày 3. Bổ sung API công khai, build lại toàn solution và chạy test nghiệp vụ; thử bản đồ/GPS demo nếu API và DB sẵn sàng. | `feat: them API cong khai va kiem thu nghiep vu` |
| 7 | `TEST/OllamaTranslationTests.cs`, `tests/admin-translation.test.cjs` | Sau Ollama ngày 4, admin.js Doanh ngày 6 và Program ngày 6. Chạy kiểm thử dịch, hủy, lỗi và giữ bản nháp; phân biệt mock test với gọi Ollama thật. | `test: them kiem thu dich huy yeu cau va ban nhap admin` |
| 8 | Hoàn thiện `docs/OllamaTranslation.md`; thử luồng du khách và dịch thật | Sau Docker của Quốc khi dịch vụ sẵn sàng. Kiểm tra GPS demo, âm thanh, fallback giọng đọc, dịch từ admin; ghi rõ phần chưa thử trên thiết bị thật. | `docs: hoan thien huong dan Ollama va ket qua kiem tra tich hop` |
| 9 | Kiểm tra mobile, GPS thật/quyền vị trí, autoplay và sai số; sửa lỗi thực tế | Ghi thiết bị/trình duyệt, bước tái hiện và kết quả trong `docs/nhat-ky/hau-10-ngay.md`; phối hợp sửa các lỗi tích hợp trước nghiệm thu. | `docs: ghi nhan kiem tra mobile GPS va am thanh` |
| 10 | Chốt hướng dẫn và `docs/nhat-ky/hau-10-ngay.md` | Demo luồng du khách và dịch, bàn giao bằng chứng test và giới hạn còn lại. | `docs: chot huong dan va ket qua nghiem thu du khach` |

## Ví dụ nhập nội dung commit

Ví dụ Quốc ngày 3, sau khi đưa đúng các file trong bảng vào repo và kiểm tra nội dung:

```powershell
git add src/AudioGuide.BLL/Dtos.cs src/AudioGuide.BLL/Exceptions.cs src/AudioGuide.BLL/Security/PasswordHasher.cs src/AudioGuide.BLL/Visitors/UserAgentParser.cs src/AudioGuide.BLL/Visitors/LanguageNegotiator.cs
# Nếu có cập nhật nhật ký thực tế trong ngày thì thêm cùng commit.
git add docs/nhat-ky/quoc-10-ngay.md
git diff --cached
git commit -m "feat: them DTO exception va cac helper nghiep vu"
```

Các thành viên thay danh sách file và nội dung trong dấu ngoặc kép bằng dòng được giao của mình. Cột commit là tiêu đề commit; nếu cần mô tả thêm có thể dùng `git commit -m "Tieu de" -m "Mo ta thay doi va ket qua kiem tra thuc te"`. Không chạy lại commit của những ngày đã hoàn thành. Các lệnh trên chỉ là hướng dẫn để thành viên tự thực hiện, không tự push.

## Thứ tự đồng bộ và mốc kiểm tra

Không cần mọi người luôn làm theo một thứ tự cố định. Chỉ các đầu việc phụ thuộc phải chờ phần nền tảng đã được người thực hiện commit/push và người tiếp theo pull về:

1. Doanh ngày 2 (DAL đầy đủ) trước các service ngày 3–4.
2. Quốc ngày 3 (DTO/helper) trước Hậu ngày 3 (PlaceService) và các service ngày 4.
3. Doanh ngày 4 (LanguageService) trước Hậu ngày 4 (Ollama), rồi Doanh ngày 5 (AdminPlacesController).
4. Doanh ngày 3 (seed), Hậu ngày 3 (PlaceService), Doanh ngày 4 (service) trước Quốc ngày 5 (VisitorService và đăng ký BLL).
5. Toàn bộ service, JWT/middleware, upload/cleanup và Ollama có trước Quốc ngày 6 (Program). Quốc hoàn tất trước Hậu ngày 6 (API công khai và kiểm thử); Doanh có thể chuẩn bị admin.js song song nhưng chỉ nghiệm thu sau khi API chạy.
6. Cuối ngày 6 build toàn solution; ngày 7 đủ bộ test hiện có; ngày 8 Docker/MySQL và kiểm tra tích hợp thật; ngày 9 CI và sửa lỗi; ngày 10 nghiệm thu.

**Giới hạn:** lịch này bảo đảm thứ tự các thành phần được đưa lên phù hợp với phụ thuộc đã đọc trong code gốc; không bảo đảm mọi commit từ ngày 1 đều build toàn solution. Trước khi có Program ở ngày 6, API thiếu entry point; có thể build riêng thư viện khi đủ dependency. Không tạo Program/service giả để làm xanh build tạm thời. Các kết quả build/test chỉ được xác nhận sau khi chạy thực tế.

## Quy tắc bàn giao và file dùng chung

- Dùng tài khoản và danh tính Git của người thực hiện. Không đổi ngày commit hay tác giả; nhật ký có thể bổ sung muộn nhưng phải ghi ngày bổ sung.
- Mỗi ngày commit phần thực tế và nhật ký cùng nhau khi phù hợp. Tiêu đề nêu đúng thay đổi; không tạo thêm commit chỉ để lấp số lượng.
- Người phụ trách sửa chính: Quốc — DTO, helper nhận diện, cấu hình, Program, BLL DI, README; Doanh — DAL/repository, CRUD admin và admin.js; Hậu — PlaceService, API công khai, Ollama, app.js, CSS và BllTests.
- Với file nguyên khối như app.js/admin.js, đưa đủ file ở ngày được chỉ định để tránh hàm/state bị thiếu. Các ngày tiếp theo kiểm tra và cải thiện thực tế, không sao chép lại file không đổi.
- Nếu service cần interface trong cùng file của bản gốc, bàn giao cả file; không tách interface rồi quên implementation. Nếu phát hiện phụ thuộc khác, điều chỉnh cùng nhóm trước khi bàn giao.
- Chỉ stage file của mình sau khi kiểm tra `git diff` và `git diff --cached`. Không gộp các thay đổi đang có của người khác vào commit của mình. Người dùng tự commit/push; tài liệu này không tự động thực hiện Git mutation.

## Lệnh kiểm tra khi đạt mốc tương ứng

Sau Doanh ngày 2, nếu đã cài .NET 10 và có kết nối restore package:

```powershell
dotnet build src/AudioGuide.DAL/AudioGuide.DAL.csproj -c Release
```

Cuối ngày 6 và sau khi đưa thêm test ngày 7:

```powershell
dotnet restore HcmcAudioGuide.slnx
dotnet build HcmcAudioGuide.slnx -c Release --no-restore
dotnet test HcmcAudioGuide.slnx -c Release --no-build
```

Sau khi đủ JS ngày 6 và test JS ngày 7:

```powershell
node --check src/AudioGuide.API/wwwroot/js/app.js
node --check src/AudioGuide.API/wwwroot/admin/admin.js
node --test tests/admin-translation.test.cjs
```

Ngày 8 trở đi, khi có Docker Desktop, compose và cấu hình môi trường:

```powershell
docker compose up -d --build
```

Đây là các lệnh dự kiến cho tiến độ tương lai, chưa phải kết quả kiểm tra của lần cập nhật lịch này.
