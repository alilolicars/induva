using System.Diagnostics;
using Induva.Application.AI;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Domain.Enums;
using Induva.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController(
    IAiClient aiClient,
    IAdminDashboardService adminDashboard,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("Admin")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var identityUsers = userManager.Users.ToList();
        var userDtos = new List<AdminUserDto>();

        foreach (var u in identityUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            var isLockedOut = await userManager.IsLockedOutAsync(u);
            userDtos.Add(new AdminUserDto(
                u.Id,
                u.Email ?? "",
                u.DisplayName,
                roles.ToList(),
                isLockedOut,
                DateTime.UtcNow));
        }

        var dashboard = await adminDashboard.GetDashboardStatsAsync(userDtos, ct);
        return View(dashboard);
    }

    [HttpPost("Admin/ToggleLockout")]
    public async Task<IActionResult> ToggleLockout(string userId)
    {
        var targetUser = await userManager.FindByIdAsync(userId);
        if (targetUser is null) return NotFound();

        var isLockedOut = await userManager.IsLockedOutAsync(targetUser);
        if (isLockedOut)
        {
            await userManager.SetLockoutEndDateAsync(targetUser, null);
            TempData["Success"] = $"Account for {targetUser.Email} unlocked.";
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.UtcNow.AddYears(10));
            TempData["Success"] = $"Account for {targetUser.Email} locked out.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Admin/AiHealth")]
    public async Task<IActionResult> AiHealth(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var req = new AiRequest(AiFeature.FactoryAnalysis, "You are a health check monitor.", "Reply with the single word OK.");
            var res = await aiClient.ChatAsync(req, ct);
            sw.Stop();

            return Json(new
            {
                status = "Healthy",
                latencyMs = sw.ElapsedMilliseconds,
                reply = res.Text.Trim(),
                timestampUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return StatusCode(503, new
            {
                status = "Unhealthy",
                latencyMs = sw.ElapsedMilliseconds,
                error = ex.Message,
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}
