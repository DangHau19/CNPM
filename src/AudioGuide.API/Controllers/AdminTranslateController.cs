using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

/// <summary>Dịch tự động nội dung thuyết minh từ tiếng Việt sang các ngôn ngữ khác (chỉ trả kết quả, không lưu).</summary>
[ApiController]
[Authorize]
[Route("api/admin/translate")]
public class AdminTranslateController : ControllerBase
{
    private readonly IAutoTranslateService _service;
    public AdminTranslateController(IAutoTranslateService service) => _service = service;

    [HttpGet("status")]
    public ActionResult<AutoTranslateStatus> Status() => new AutoTranslateStatus(_service.IsConfigured);

    [HttpPost]
    public async Task<ActionResult<AutoTranslateResult>> Translate([FromBody] AutoTranslateRequest request, CancellationToken ct) =>
        await _service.TranslateAsync(request, ct);
}
