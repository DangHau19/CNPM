using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

/// <summary>Admin xem ai đang truy cập và lịch sử truy cập.</summary>
[ApiController]
[Authorize]
[Route("api/admin/visitors")]
public class AdminVisitorsController : ControllerBase
{
    private readonly IVisitorService _service;
    public AdminVisitorsController(IVisitorService service) => _service = service;

    [HttpGet("overview")]
    public async Task<ActionResult<VisitOverviewDto>> Overview([FromQuery] int days = 7, [FromQuery] int tz = 0) =>
        await _service.GetOverviewAsync(days, tz);

    [HttpGet("online")]
    public async Task<ActionResult<List<VisitDto>>> Online() => await _service.GetOnlineAsync();

    [HttpGet]
    public async Task<ActionResult<PagedResult<VisitDto>>> List([FromQuery] int days = 7, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        await _service.GetVisitsAsync(days, page, pageSize);
}
