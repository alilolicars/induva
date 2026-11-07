using Microsoft.AspNetCore.Identity;

namespace Induva.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}

