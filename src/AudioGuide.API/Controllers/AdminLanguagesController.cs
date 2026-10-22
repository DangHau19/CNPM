using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/languages")]
public class AdminLanguagesController : ControllerBase
{
    private readonly ILanguageService _service;
    public AdminLanguagesController(ILanguageService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<LanguageDto>>> List() => await _service.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<LanguageDto>> Create([FromBody] LanguageSaveRequest request) =>
        await _service.CreateAsync(request);

    [HttpPut("{code}")]
    public async Task<ActionResult<LanguageDto>> Update(string code, [FromBody] LanguageSaveRequest request) =>
        await _service.UpdateAsync(code, request);
}
