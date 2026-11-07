using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

public class InvestorsController(IInvestorService investors) : Controller
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] InvestorFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        return View(await investors.SearchAsync(filter, ct));
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await investors.GetAsync(id, ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Value);
    }

    [HttpGet, Authorize(Roles = $"{Roles.Investor},{Roles.Admin}")]
    public async Task<IActionResult> Manage(CancellationToken ct)
    {
        var r = await investors.GetCurrentAsync(ct);
        if (r.Succeeded)
        {
            var p = r.Value!;
            var vm = new InvestorFormViewModel
            {
                Id = p.Id,
                DisplayName = p.DisplayName,
                MinTicketEgp = p.MinTicketEgp,
                MaxTicketEgp = p.MaxTicketEgp,
                Bio = p.Bio,
                SelectedSectors = p.Sectors
                    .Select(s => Enum.TryParse<Induva.Domain.Enums.Sector>(s, out var sec) ? sec : (Induva.Domain.Enums.Sector?)null)
                    .Where(s => s.HasValue).Select(s => s!.Value).ToList(),
                SelectedStages = p.PreferredStages
                    .Select(s => Enum.TryParse<Induva.Domain.Enums.StartupStage>(s, out var stg) ? stg : (Induva.Domain.Enums.StartupStage?)null)
                    .Where(s => s.HasValue).Select(s => s!.Value).ToList()
            };
            return View("Form", vm);
        }

        return View("Form", new InvestorFormViewModel());
    }

    [HttpPost, Authorize(Roles = $"{Roles.Investor},{Roles.Admin}")]
    public async Task<IActionResult> Manage(InvestorFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var r = await investors.SaveAsync(new SaveInvestorProfileDto(
            vm.DisplayName, vm.SelectedSectors, vm.MinTicketEgp, vm.MaxTicketEgp, vm.SelectedStages, vm.Bio), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to save profile.");
            return View("Form", vm);
        }

        TempData["Success"] = "Investor profile updated successfully.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Investor},{Roles.Admin}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await investors.DeleteAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.Forbidden ? Forbid() : NotFound();

        TempData["Success"] = "Investor profile removed.";
        return RedirectToAction(nameof(Index));
    }
}

