using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/places")]
public class AdminPlacesController : ControllerBase
{
    private readonly IPlaceAdminService _service;
    public AdminPlacesController(IPlaceAdminService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<PlaceAdminDto>>> List() => await _service.ListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlaceAdminDto>> Get(int id) => await _service.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<PlaceAdminDto>> Create([FromBody] PlaceSaveRequest request) =>
        await _service.CreateAsync(request);

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlaceAdminDto>> Update(int id, [FromBody] PlaceSaveRequest request) =>
        await _service.UpdateAsync(id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id:int}/translations/{lang}")]
    public async Task<ActionResult<PlaceAdminDto>> UpsertTranslation(int id, string lang, [FromBody] TranslationSaveRequest request) =>
        await _service.UpsertTranslationAsync(id, lang, request);

    [HttpDelete("{id:int}/translations/{lang}")]
    public async Task<ActionResult<PlaceAdminDto>> DeleteTranslation(int id, string lang) =>
        await _service.DeleteTranslationAsync(id, lang);
}
