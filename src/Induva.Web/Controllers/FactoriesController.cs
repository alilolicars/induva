using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.Controllers;

public class FactoriesController(IFactoryService factories, ILookupService lookups) : Controller
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] FactoryFilter filter, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        ViewBag.Filter = filter;
        var paged = await factories.SearchAsync(filter, ct);
        return View(paged);
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await factories.GetAsync(id, ct);
        if (!r.Succeeded)
        {
            return r.Kind switch
            {
                ErrorKind.NotFound => NotFound(),
                ErrorKind.Forbidden => Forbid(),
                _ => BadRequest(r.Error)
            };
        }

        ViewBag.RawMaterials = (await lookups.GetRawMaterialsAsync(ct))
            .Select(m => new SelectListItem($"{m.NameEn} ({m.Category})", m.Id.ToString()));

        return View(r.Value);
    }

    [HttpGet, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new FactoryFormViewModel
        {
            Governorates = await GovernorateSelectAsync(ct)
        };
        return View("Form", vm);
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Create(FactoryFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await factories.CreateAsync(
            new SaveFactoryDto(
                vm.Name,
                vm.Sector,
                vm.GovernorateId,
                vm.Description,
                vm.UtilizationPercent,
                vm.Latitude,
                vm.Longitude,
                vm.IsPublic),
            ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Failed to create factory.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Factory created successfully! You can now add machines, idle periods, and products.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    [HttpGet, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var r = await factories.GetForEditAsync(id, ct);
        if (!r.Succeeded)
        {
            return r.Kind switch
            {
                ErrorKind.NotFound => NotFound(),
                ErrorKind.Forbidden => Forbid(),
                _ => BadRequest(r.Error)
            };
        }

        var f = r.Value!;
        var vm = new FactoryFormViewModel
        {
            Id = id,
            Name = f.Name,
            Sector = f.Sector,
            GovernorateId = f.GovernorateId,
            Description = f.Description,
            UtilizationPercent = f.UtilizationPercent,
            Latitude = f.Latitude,
            Longitude = f.Longitude,
            IsPublic = f.IsPublic,
            Governorates = await GovernorateSelectAsync(ct)
        };

        return View("Form", vm);
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Edit(int id, FactoryFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        var r = await factories.UpdateAsync(
            id,
            new SaveFactoryDto(
                vm.Name,
                vm.Sector,
                vm.GovernorateId,
                vm.Description,
                vm.UtilizationPercent,
                vm.Latitude,
                vm.Longitude,
                vm.IsPublic),
            ct);

        if (!r.Succeeded)
        {
            if (r.Kind == ErrorKind.Forbidden) return Forbid();
            if (r.Kind == ErrorKind.NotFound) return NotFound();

            ModelState.AddModelError("", r.Error ?? "Failed to update factory.");
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }

        TempData["Success"] = "Factory updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var r = await factories.DeleteAsync(id, ct);
        if (!r.Succeeded)
        {
            if (r.Kind == ErrorKind.Forbidden) return Forbid();
            if (r.Kind == ErrorKind.NotFound) return NotFound();
            TempData["Error"] = r.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Factory deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // --- Inline Child Actions ---

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> AddMachine(int id, MachineFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in all required machine fields.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await factories.AddMachineAsync(id, new SaveMachineDto(
            vm.Name, vm.MachineType, vm.Quantity, vm.CapacityPerHour, vm.CapacityUnit, vm.Description), ct);

        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Machine added.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> RemoveMachine(int id, int machineId, CancellationToken ct)
    {
        var r = await factories.RemoveMachineAsync(id, machineId, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Machine removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> AddIdleWindow(int id, IdleWindowFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid idle window values.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await factories.AddIdleWindowAsync(id, new SaveIdleWindowDto(
            vm.StartMonth, vm.EndMonth, vm.AvailableCapacityPercent, vm.Note), ct);

        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Idle capacity period added.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> RemoveIdleWindow(int id, int windowId, CancellationToken ct)
    {
        var r = await factories.RemoveIdleWindowAsync(id, windowId, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Idle period removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> AddProduct(int id, ProductFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Product name is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await factories.AddProductAsync(id, new SaveFactoryProductDto(vm.Name, vm.Category), ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Product added.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> RemoveProduct(int id, int productId, CancellationToken ct)
    {
        var r = await factories.RemoveProductAsync(id, productId, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Product removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> AddMaterialNeed(int id, MaterialNeedFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Select a raw material.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await factories.AddMaterialNeedAsync(id, new SaveFactoryMaterialNeedDto(vm.RawMaterialId, vm.MonthlyTons), ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Material need added.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> RemoveMaterialNeed(int id, int needId, CancellationToken ct)
    {
        var r = await factories.RemoveMaterialNeedAsync(id, needId, ct);
        if (!r.Succeeded) TempData["Error"] = r.Error;
        else TempData["Success"] = "Material need removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct) =>
        (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
}

