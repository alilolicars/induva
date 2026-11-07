using System.Security.Claims;
using Induva.Application.Abstractions;

namespace Induva.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}

