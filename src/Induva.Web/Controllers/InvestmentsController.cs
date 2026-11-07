using Induva.Application.AI.Features;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

[Authorize]
public class InvestmentsController(
    IInvestmentService investments,
    IFactoryService factories,
    ILookupService lookups) : Controller
{
    [HttpGet("investments")]
    public async Task<IActionResult> Index([FromQuery] string? product, [FromQuery] string? category, [FromQuery] int? factoryId, [FromQuery] int? governorateId, CancellationToken ct)
    {
        ViewBag.Governorates = (await lookups.GetGovernoratesAsync(ct))
            .Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
        ViewBag.MyFactories = (await factories.GetMyFactoriesAsync(ct))
            .Select(f => new SelectListItem(f.Name, f.Id.ToString()));

        ViewBag.ProductName = product ?? "";
        ViewBag.Category = category ?? "";
        ViewBag.FactoryId = factoryId;
        ViewBag.GovernorateId = governorateId;

        return View();
    }

    [HttpPost("investments/assumptions")]
    public async Task<IActionResult> GetAssumptions([FromBody] GetInvestmentAssumptionsRequestDto req, CancellationToken ct)
    {
        var r = await investments.GetAssumptionsAsync(req, ct);
        return r.Succeeded ? PartialView("_InvestmentForm", r.Value) : r.ToJsonError();
    }

    [HttpPost("investments/calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateInvestmentRequestDto req, CancellationToken ct)
    {
        // Deterministic C# math - zero AI cost
        var r = await investments.CalculateAsync(req, ct);
        return r.Succeeded ? PartialView("_InvestmentResultsCard", r.Value) : r.ToJsonError();
    }

    [HttpGet("investments/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await investments.GetByIdAsync(id, ct);
        if (!r.Succeeded) return NotFound();
        return View("Results", r.Value);
    }
}

