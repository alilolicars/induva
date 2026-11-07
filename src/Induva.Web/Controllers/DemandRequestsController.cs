using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

public class DemandRequestsController(IDemandRequestService demands, ILookupService lookups) : Controller
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] DemandRequestFilter filter, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        ViewBag.Filter = filter;
        return View(await demands.SearchAsync(filter, ct));
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await demands.GetAsync(id, ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Value);
    }

    [HttpGet, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        return View("Form", new DemandRequestFormViewModel { Governorates = await GovernorateSelectAsync(ct) });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Create(DemandRequestFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await demands.CreateAsync(new SaveDemandRequestDto(
            vm.Title, vm.ProductName, vm.Sector, vm.SelectedTags, vm.Quantity, vm.Unit,
            vm.TargetPriceEgp, vm.DeadlineUtc, vm.GovernorateId, vm.IsExport, vm.Status, vm.Description), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to create demand request.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Demand request published.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    [HttpGet, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var r = await demands.GetForEditAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.NotFound ? NotFound() : Forbid();

        var d = r.Value!;
        return View("Form", new DemandRequestFormViewModel
        {
            Id = id,
            Title = d.Title,
            ProductName = d.ProductName,
            Sector = d.Sector,
            SelectedTags = d.RequiredTags.ToList(),
            Quantity = d.Quantity,
            Unit = d.Unit,
            TargetPriceEgp = d.TargetPriceEgp,
            DeadlineUtc = d.DeadlineUtc,
            GovernorateId = d.GovernorateId,
            IsExport = d.IsExport,
            Status = d.Status,
            Description = d.Description,
            Governorates = await GovernorateSelectAsync(ct)
        });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Edit(int id, DemandRequestFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await demands.UpdateAsync(id, new SaveDemandRequestDto(
            vm.Title, vm.ProductName, vm.Sector, vm.SelectedTags, vm.Quantity, vm.Unit,
            vm.TargetPriceEgp, vm.DeadlineUtc, vm.GovernorateId, vm.IsExport, vm.Status, vm.Description), ct);

        if (!r.Succeeded)
        {
            if (r.Kind == ErrorKind.Forbidden) return Forbid();
            if (r.Kind == ErrorKind.NotFound) return NotFound();

            ModelState.AddModelError("", r.Error ?? "Failed to update demand request.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Demand request updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await demands.DeleteAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.Forbidden ? Forbid() : NotFound();

        TempData["Success"] = "Demand request deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = $"{Roles.Buyer},{Roles.Admin},{Roles.FactoryOwner}")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var r = await demands.CloseAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.Forbidden ? Forbid() : NotFound();

        TempData["Success"] = "Demand request closed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct) =>
        (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
}

