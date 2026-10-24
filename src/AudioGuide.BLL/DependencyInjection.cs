using AudioGuide.BLL.Security;
using AudioGuide.BLL.Services;
using AudioGuide.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AudioGuide.BLL;

public static class DependencyInjection
{
    /// <summary>Composition root cho BLL + DAL: lớp API chỉ cần tham chiếu BLL.</summary>
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services, string connectionString, string? serverVersion = null)
    {
        services.AddDataAccess(connectionString, serverVersion);
        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<IPlaceAdminService, PlaceAdminService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IVisitorService, VisitorService>();
        return services;
    }

    /// <summary>
    /// Tạo database + bảng nếu chưa có, rồi nạp dữ liệu mẫu.
    /// Thử lại nhiều lần vì MySQL có thể khởi động chậm hơn ứng dụng.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, string adminUsername, string adminPassword, int maxAttempts = 15)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();   // database trống: tạo toàn bộ bảng
                await DbSeeder.SeedAsync(db, adminUsername, PasswordHasher.Hash(adminPassword));
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
