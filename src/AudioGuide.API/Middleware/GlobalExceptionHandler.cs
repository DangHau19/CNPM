using AudioGuide.BLL;
using Microsoft.AspNetCore.Diagnostics;

namespace AudioGuide.API.Middleware;

/// <summary>Chuyển exception của BLL thành HTTP status: NotFound→404, Business→400, còn lại→500.</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == 500) _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new { message = status == 500 ? "Lỗi hệ thống, vui lòng thử lại sau." : exception.Message }, ct);
        return true;
    }
}
