using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

public class SuppliersController(ISupplierService suppliers, ILookupService lookups) : Controller
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] SupplierFilter filter, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        ViewBag.RawMaterials = (await lookups.GetRawMaterialsAsync(ct))
            .Select(m => new SelectListItem(m.NameEn, m.Id.ToString()));
        ViewBag.Filter = filter;
        return View(await suppliers.SearchAsync(filter, ct));
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await suppliers.GetAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.NotFound ? NotFound() : Forbid();

        ViewBag.RawMaterials = (await lookups.GetRawMaterialsAsync(ct))
            .Select(m => new SelectListItem($"{m.NameEn} ({m.Category})", m.Id.ToString()));

        return View(r.Value);
    }

    [HttpGet, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        return View("Form", new SupplierFormViewModel { Governorates = await GovernorateSelectAsync(ct) });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> Create(SupplierFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await suppliers.CreateAsync(new SaveSupplierDto(
            vm.CompanyName, vm.GovernorateId, vm.Description, vm.Latitude, vm.Longitude, vm.IsPublic), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to create supplier profile.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Supplier profile created. You can now add supplied raw materials.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    [HttpGet, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var r = await suppliers.GetForEditAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.NotFound ? NotFound() : Forbid();

        var s = r.Value!;
        return View("Form", new SupplierFormViewModel
        {
            Id = id,
            CompanyName = s.CompanyName,
            GovernorateId = s.GovernorateId,
            Description = s.Description,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            IsPublic = s.IsPublic,
            Governorates = await GovernorateSelectAsync(ct)
        });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await suppliers.UpdateAsync(id, new SaveSupplierDto(
            vm.CompanyName, vm.GovernorateId, vm.Description, vm.Latitude, vm.Longitude, vm.IsPublic), ct);

        if (!r.Succeeded)
        {
            if (r.Kind == ErrorKind.Forbidden) return Forbid();
            if (r.Kind == ErrorKind.NotFound) return NotFound();

            ModelState.AddModelError("", r.Error ?? "Failed to update profile.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Supplier profile updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await suppliers.DeleteAsync(id, ct);
        if (!r.Succeeded) return r.Kind == ErrorKind.Forbidden ? Forbid() : NotFound();

        TempData["Success"] = "Supplier profile deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> AddMaterial(int id, SupplierMaterialFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please select a material.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await suppliers.AddMaterialAsync(id, new SaveSupplierMaterialDto(
            vm.RawMaterialId, vm.MinOrderTons, vm.PriceNote), ct);

        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Supplied material added.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
    public async Task<IActionResult> RemoveMaterial(int id, int materialId, CancellationToken ct)
    {
        var r = await suppliers.RemoveMaterialAsync(id, materialId, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Supplied material removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct) =>
        (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
}

