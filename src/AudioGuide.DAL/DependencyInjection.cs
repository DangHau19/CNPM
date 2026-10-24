using AudioGuide.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AudioGuide.DAL;

public static class DependencyInjection
{
    private static ServerVersion? _detectedVersion;
    private static readonly object VersionLock = new();

    /// <param name="serverVersion">
    /// Tùy chọn. Bỏ trống = tự dò theo máy chủ MySQL đang kết nối.
    /// Ví dụ chỉ định tay: "8.4.0".
    /// </param>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString, string? serverVersion = null)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseMySql(connectionString, ResolveServerVersion(connectionString, serverVersion)));

        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IPlayLogRepository, PlayLogRepository>();
        services.AddScoped<IVisitorRepository, VisitorRepository>();
        return services;
    }

    /// <summary>Dò phiên bản máy chủ một lần rồi nhớ lại. Nếu MySQL chưa sẵn sàng thì ném lỗi để bên gọi thử lại.</summary>
    private static ServerVersion ResolveServerVersion(string connectionString, string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return ServerVersion.Parse(configured.Trim());
        lock (VersionLock)
        {
            return _detectedVersion ??= ServerVersion.AutoDetect(connectionString);
        }
    }
}
