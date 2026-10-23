using System.Text;
using System.Threading.RateLimiting;
using AudioGuide.API.Middleware;
using AudioGuide.API.Security;
using AudioGuide.API.Storage;
using AudioGuide.API.Translation;
using AudioGuide.BLL;
using AudioGuide.BLL.Translation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Render/Heroku... cấp cổng qua biến PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default");

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key phải dài tối thiểu 32 ký tự.");

// ---- Business + Data layers (MySQL 8 hoặc MariaDB của XAMPP) ----
builder.Services.AddBusinessLogic(connectionString, builder.Configuration["Database:ServerVersion"]);

// ---- Dịch tự động (Google Cloud Translation). Chưa có API key thì nút "Dịch tự động" ở admin báo chưa cấu hình ----
builder.Services.AddHttpClient<ITextTranslator, GoogleTextTranslator>(c => c.Timeout = TimeSpan.FromSeconds(30));

// ---- Thư mục upload ----
var configuredUploadPath = builder.Configuration["Storage:UploadPath"];
var uploadRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredUploadPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "data", "uploads")
    : configuredUploadPath);
Directory.CreateDirectory(uploadRoot);
builder.Services.AddSingleton(new UploadOptions(uploadRoot));

// ---- Auth (JWT cho Admin) ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---- Giới hạn tần suất (chống dò mật khẩu admin và spam API công khai) ----
static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
static RateLimitPartition<string> Window(HttpContext ctx, int permits, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permits,
        Window = window,
        QueueLimit = 0
    });

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => Window(ctx, 10, TimeSpan.FromMinutes(1)));
    o.AddPolicy("ping", ctx => Window(ctx, 240, TimeSpan.FromMinutes(1)));
    o.AddPolicy("play", ctx => Window(ctx, 120, TimeSpan.FromMinutes(1)));
});

// Phía sau reverse proxy (Render, Nginx...): lấy IP thật của khách từ X-Forwarded-For.
// Tự bật trên Render (Render luôn đặt biến RENDER=true); nơi khác chỉ bật khi chắc chắn có proxy tin cậy (Hosting__TrustProxy=true).
var trustProxy = builder.Configuration.GetValue<bool>("Hosting:TrustProxy")
    || string.Equals(Environment.GetEnvironmentVariable("RENDER"), "true", StringComparison.OrdinalIgnoreCase);
if (trustProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

builder.Services.AddHostedService<VisitCleanupService>();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (trustProxy) app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseDefaultFiles();   // / → index.html (du khách), /admin/ → admin/index.html
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Cảnh báo nếu còn dùng khóa/mật khẩu mặc định khi chạy Production.
if (app.Environment.IsProduction())
{
    if (jwt.Key.Contains("change-me", StringComparison.OrdinalIgnoreCase))
        app.Logger.LogWarning("Jwt:Key đang là giá trị mặc định. Hãy đặt biến môi trường Jwt__Key khi triển khai thật.");
    if ((app.Configuration["Seed:AdminPassword"] ?? "Admin@123") == "Admin@123")
        app.Logger.LogWarning("Mật khẩu admin đang là mặc định (Admin@123). Hãy đổi trong trang Admin hoặc đặt Seed__AdminPassword.");
}

await app.Services.InitializeDatabaseAsync(
    app.Configuration["Seed:AdminUsername"] ?? "admin",
    app.Configuration["Seed:AdminPassword"] ?? "Admin@123");

app.Run();

public partial class Program { }   // phục vụ integration test sau này
