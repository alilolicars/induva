using Induva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[AllowAnonymous]
public class MapController(IMapService map, ILookupService lookups) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int? governorateId, CancellationToken ct)
    {
        ViewBag.Governorates = await lookups.GetGovernoratesAsync(ct);
        ViewBag.SelectedGovernorateId = governorateId;
        return View();
    }

    [HttpGet("api/map")]
    public async Task<IActionResult> Data([FromQuery] string[]? layers, [FromQuery] int? governorateId, CancellationToken ct)
    {
        var activeLayers = layers is { Length: > 0 }
            ? layers
            : ["factories", "suppliers", "resources", "demand"];

        var markers = await map.GetMarkersAsync(activeLayers, governorateId, ct);
        return Json(markers);
    }
}

