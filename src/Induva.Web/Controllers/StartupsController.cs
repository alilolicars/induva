using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

public class StartupsController(IStartupService startups, ILookupService lookups) : Controller
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] StartupFilter filter, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        ViewBag.Filter = filter;
        return View(await startups.SearchAsync(filter, ct));
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await startups.GetAsync(id, ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Value);
    }

    [HttpGet, Authorize(Roles = $"{Roles.Startup},{Roles.Admin}")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        return View("Form", new StartupFormViewModel { Governorates = await GovernorateSelectAsync(ct) });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Startup},{Roles.Admin}")]
    public async Task<IActionResult> Create(StartupFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await startups.CreateAsync(new SaveStartupDto(
            vm.Name, vm.Sector, vm.Stage, vm.Description, vm.ProductIdea, vm.FundingNeededEgp, vm.SelectedTags, vm.GovernorateId), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to create startup profile.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Startup profile registered.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    [HttpGet, Authorize(Roles = $"{Roles.Startup},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var r = await startups.GetForEditAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.NotFound ? NotFound() : Forbid();

        var s = r.Value!;
        return View("Form", new StartupFormViewModel
        {
            Id = id,
            Name = s.Name,
            Sector = s.Sector,
            Stage = s.Stage,
            Description = s.Description,
            ProductIdea = s.ProductIdea,
            FundingNeededEgp = s.FundingNeededEgp,
            SelectedTags = s.RequiredTags.ToList(),
            GovernorateId = s.GovernorateId,
            Governorates = await GovernorateSelectAsync(ct)
        });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Startup},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, StartupFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await startups.UpdateAsync(id, new SaveStartupDto(
            vm.Name, vm.Sector, vm.Stage, vm.Description, vm.ProductIdea, vm.FundingNeededEgp, vm.SelectedTags, vm.GovernorateId), ct);

        if (!r.Succeeded)
        {
            if (r.Kind == ErrorKind.Forbidden) return Forbid();
            if (r.Kind == ErrorKind.NotFound) return NotFound();

            ModelState.AddModelError("", r.Error ?? "Failed to update startup.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Startup profile updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Startup},{Roles.Admin}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await startups.DeleteAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.Forbidden ? Forbid() : NotFound();

        TempData["Success"] = "Startup profile deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct) =>
        (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
}

