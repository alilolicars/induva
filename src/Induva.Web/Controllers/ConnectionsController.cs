using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[Authorize]
public class ConnectionsController(IConnectionService connections) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Received = await connections.GetReceivedAsync(ct);
        ViewBag.Sent = await connections.GetSentAsync(ct);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Send(SendConnectionModalViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Message is required to send connection request.";
            return RedirectBack();
        }

        var r = await connections.SendAsync(new SendConnectionRequestDto(
            vm.ToUserId, vm.RelatedType, vm.RelatedId, vm.Message), ct);

        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Connection request sent successfully.";

        return RedirectBack();
    }

    [HttpPost]
    public async Task<IActionResult> Respond(int id, bool accept, CancellationToken ct)
    {
        var r = await connections.RespondAsync(id, accept, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = accept ? "Connection request accepted." : "Connection request declined.";

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            return Redirect(referer);

        return RedirectToAction(nameof(Index));
    }
}

