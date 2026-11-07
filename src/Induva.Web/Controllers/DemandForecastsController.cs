using Induva.Application.AI.Features;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

[AllowAnonymous]
public class DemandForecastsController(IDemandForecastService forecasts, ILookupService lookups) : Controller
{
    [HttpGet("forecasts")]
    public async Task<IActionResult> Index([FromQuery] string? product, [FromQuery] string? category, [FromQuery] int? governorateId, CancellationToken ct)
    {
        ViewBag.Governorates = (await lookups.GetGovernoratesAsync(ct))
            .Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
        ViewBag.ProductName = product ?? "";
        ViewBag.Category = category ?? "";
        ViewBag.SelectedGovernorateId = governorateId;

        if (!string.IsNullOrWhiteSpace(product))
        {
            var r = await forecasts.GetLatestForecastAsync(product, governorateId, ct);
            if (r.Succeeded)
            {
                ViewBag.InitialForecast = r.Value;
            }
        }

        return View();
    }

    [HttpPost("forecasts/generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateDemandForecastDto req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.ProductName))
            return BadRequest(new { error = "Product name is required." });

        var r = await forecasts.ForecastAsync(req, ct);
        return r.Succeeded ? PartialView("_DemandForecastCard", r.Value) : r.ToJsonError();
    }
}

