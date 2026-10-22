using AudioGuide.BLL.Security;
using AudioGuide.BLL.Services;
using AudioGuide.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AudioGuide.BLL;

public static class DependencyInjection
{
    /// <summary>Composition root cho BLL + DAL: lớp API chỉ cần tham chiếu BLL.</summary>
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccess(connectionString);
        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<IPlaceAdminService, PlaceAdminService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStatsService, StatsService>();
        return services;
    }

    /// <summary>Tạo database + dữ liệu mẫu. Thử lại nhiều lần vì container MySQL có thể khởi động chậm.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, string adminUsername, string adminPassword, int maxAttempts = 15)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
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
