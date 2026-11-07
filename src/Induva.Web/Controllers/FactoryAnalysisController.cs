using Induva.Application.AI.Features;
using Induva.Domain.Constants;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
public class FactoryAnalysisController(IFactoryAnalysisService service) : Controller
{
    [HttpPost("factories/{factoryId:int}/analysis")]
    public async Task<IActionResult> Run(int factoryId, CancellationToken ct)
    {
        var r = await service.AnalyzeAsync(factoryId, ct);
        return r.Succeeded ? PartialView("_FactoryAnalysisCard", r.Value) : r.ToJsonError();
    }
}

