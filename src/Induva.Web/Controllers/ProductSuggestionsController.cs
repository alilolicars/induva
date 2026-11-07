using Induva.Application.AI.Features;
using Induva.Domain.Constants;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[Authorize]
public class ProductSuggestionsController(IProductSuggestionService suggestions) : Controller
{
    [HttpPost("factories/{factoryId:int}/suggestions")]
    [Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Generate(int factoryId, CancellationToken ct)
    {
        var r = await suggestions.SuggestAsync(factoryId, ct);
        return r.Succeeded ? PartialView("_ProductSuggestionsCard", r.Value) : r.ToJsonError();
    }

    [HttpPost("suggestions/{id:int}/save")]
    public async Task<IActionResult> Save(int id, CancellationToken ct)
    {
        var r = await suggestions.SaveSuggestionAsync(id, ct);
        if (!r.Succeeded) return r.ToJsonError();

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });

        TempData["Success"] = "Product suggestion saved.";
        return RedirectBack();
    }

    [HttpPost("suggestions/{id:int}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, CancellationToken ct)
    {
        var r = await suggestions.DismissSuggestionAsync(id, ct);
        if (!r.Succeeded) return r.ToJsonError();

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });

        TempData["Success"] = "Product suggestion dismissed.";
        return RedirectBack();
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Uri.IsWellFormedUriString(referer, UriKind.RelativeOrAbsolute))
            return Redirect(referer);

        return RedirectToAction("Index", "Factories");
    }
}

