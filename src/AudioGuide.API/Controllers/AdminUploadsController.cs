using AudioGuide.API.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

/// <summary>Admin tải file âm thanh (.mp3...) và ảnh địa điểm lên máy chủ.</summary>
[ApiController]
[Authorize]
[Route("api/admin/uploads")]
public class AdminUploadsController : ControllerBase
{
    private static readonly string[] AudioExt = { ".mp3", ".m4a", ".wav", ".ogg" };
    private static readonly string[] ImageExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxAudioBytes = 15 * 1024 * 1024;
    private const long MaxImageBytes = 4 * 1024 * 1024;

    private readonly UploadOptions _options;
    public AdminUploadsController(UploadOptions options) => _options = options;

    [HttpPost("audio")]
    [RequestSizeLimit(MaxAudioBytes + 1024 * 1024)]
    public Task<IActionResult> UploadAudio(IFormFile? file) => SaveAsync(file, "audio", AudioExt, MaxAudioBytes);

    [HttpPost("image")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public Task<IActionResult> UploadImage(IFormFile? file) => SaveAsync(file, "images", ImageExt, MaxImageBytes);

    private async Task<IActionResult> SaveAsync(IFormFile? file, string folder, string[] allowedExt, long maxBytes)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Chưa chọn file." });
        if (file.Length > maxBytes) return BadRequest(new { message = $"File quá lớn (tối đa {maxBytes / 1024 / 1024} MB)." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExt.Contains(ext)) return BadRequest(new { message = "Định dạng không được hỗ trợ: " + string.Join(", ", allowedExt) });

        var dir = Path.Combine(_options.RootPath, folder);
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;   // không dùng tên gốc → không lo path traversal/trùng tên
        await using (var stream = System.IO.File.Create(Path.Combine(dir, name)))
        {
            await file.CopyToAsync(stream);
        }
        return Ok(new { url = $"/uploads/{folder}/{name}" });
    }
}
