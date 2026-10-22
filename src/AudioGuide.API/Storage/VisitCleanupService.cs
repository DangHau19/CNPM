using AudioGuide.BLL.Services;

namespace AudioGuide.API.Storage;

/// <summary>
/// Định kỳ xóa lịch sử truy cập cũ để bảng VisitSessions không phình mãi (mặc định giữ 90 ngày,
/// cấu hình bằng Retention:VisitDays; đặt 0 để tắt). Số liệu khách mới/quay lại (bảng Visitors) vẫn được giữ.
/// </summary>
public class VisitCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<VisitCleanupService> _logger;

    public VisitCleanupService(IServiceScopeFactory scopes, IConfiguration config, ILogger<VisitCleanupService> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var days = _config.GetValue("Retention:VisitDays", 90);
        if (days <= 0) return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var visitors = scope.ServiceProvider.GetRequiredService<IVisitorService>();
                var removed = await visitors.PurgeOldSessionsAsync(days);
                if (removed > 0) _logger.LogInformation("Đã dọn {Count} phiên truy cập cũ hơn {Days} ngày.", removed, days);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Không dọn được lịch sử truy cập cũ (sẽ thử lại sau).");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
