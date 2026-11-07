using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class GovernorateResourcesController(IGovernorateResourceService resources, ILookupService lookups) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int? governorateId, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        ViewBag.SelectedGovernorateId = governorateId;
        return View(await resources.ListAsync(governorateId, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new GovernorateResourceFormViewModel
        {
            Governorates = await GovernorateSelectAsync(ct),
            RawMaterials = await RawMaterialSelectAsync(ct)
        };
        return View("Form", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(GovernorateResourceFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            vm.RawMaterials = await RawMaterialSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await resources.CreateAsync(new SaveGovernorateResourceDto(
            vm.GovernorateId, vm.RawMaterialId, vm.EstimatedAnnualTons, vm.Notes), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to save resource.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            vm.RawMaterials = await RawMaterialSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Governorate raw material resource added.";
        return RedirectToAction(nameof(Index), new { governorateId = vm.GovernorateId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var r = await resources.GetByIdAsync(id, ct);
        if (!r.Succeeded) return NotFound();

        var res = r.Value!;
        var vm = new GovernorateResourceFormViewModel
        {
            Id = id,
            GovernorateId = res.GovernorateId,
            RawMaterialId = res.RawMaterialId,
            EstimatedAnnualTons = res.EstimatedAnnualTons,
            Notes = res.Notes,
            Governorates = await GovernorateSelectAsync(ct),
            RawMaterials = await RawMaterialSelectAsync(ct)
        };
        return View("Form", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, GovernorateResourceFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            vm.RawMaterials = await RawMaterialSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await resources.UpdateAsync(id, new SaveGovernorateResourceDto(
            vm.GovernorateId, vm.RawMaterialId, vm.EstimatedAnnualTons, vm.Notes), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to update resource.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            vm.RawMaterials = await RawMaterialSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Governorate raw material resource updated.";
        return RedirectToAction(nameof(Index), new { governorateId = vm.GovernorateId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await resources.DeleteAsync(id, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Resource entry deleted.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct) =>
        (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));

    private async Task<IEnumerable<SelectListItem>> RawMaterialSelectAsync(CancellationToken ct) =>
        (await lookups.GetRawMaterialsAsync(ct)).Select(m => new SelectListItem($"{m.NameEn} ({m.Category})", m.Id.ToString()));
}

