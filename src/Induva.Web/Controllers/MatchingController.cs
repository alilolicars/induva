using Induva.Application.AI.Features;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Enums;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[Authorize]
public class MatchingController(
    IMatchingService matching,
    IFactoryService factories,
    IStartupService startups) : Controller
{
    [HttpGet("matching")]
    public async Task<IActionResult> Index([FromQuery] MatchEntityType? source, [FromQuery] int? sourceId, [FromQuery] MatchEntityType? target, CancellationToken ct)
    {
        // Load user's owned factories and startups for source dropdown
        var myFactories = await factories.GetMyFactoriesAsync(ct);
        var myStartups = await startups.GetMyStartupsAsync(ct);

        ViewBag.MyFactories = myFactories;
        ViewBag.MyStartups = myStartups;
        ViewBag.InitialSource = source;
        ViewBag.InitialSourceId = sourceId;
        ViewBag.InitialTarget = target;

        if (source.HasValue && sourceId.HasValue && target.HasValue)
        {
            var r = await matching.FindMatchesAsync(source.Value, sourceId.Value, target.Value, ct);
            if (r.Succeeded)
            {
                ViewBag.InitialMatches = r.Value;
            }
        }

        return View();
    }

    [HttpPost("matching/find")]
    public async Task<IActionResult> Find([FromBody] MatchingQueryDto req, CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new { error = "Invalid query." });

        var r = await matching.FindMatchesAsync(req.SourceType, req.SourceId, req.TargetType, ct);
        return r.Succeeded ? PartialView("_MatchResultsCard", r.Value) : r.ToJsonError();
    }
}

