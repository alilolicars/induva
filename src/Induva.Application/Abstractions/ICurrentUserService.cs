namespace Induva.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

