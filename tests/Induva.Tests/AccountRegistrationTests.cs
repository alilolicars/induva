using Induva.Domain.Constants;
using Induva.Infrastructure.Identity;
using Induva.Web.Controllers;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class AccountRegistrationTests
{
    [Fact]
    public void SelfRegistrable_roles_never_contain_Admin()
    {
        Assert.DoesNotContain(Roles.Admin, Roles.SelfRegistrable);
        Assert.Contains(Roles.FactoryOwner, Roles.SelfRegistrable);
        Assert.Contains(Roles.Supplier, Roles.SelfRegistrable);
        Assert.Contains(Roles.Startup, Roles.SelfRegistrable);
        Assert.Contains(Roles.Investor, Roles.SelfRegistrable);
        Assert.Contains(Roles.Buyer, Roles.SelfRegistrable);
    }

    [Fact]
    public async Task Registration_with_Admin_role_is_rejected()
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(userStore, null, null, null, null, null, null, null, null);
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInManager = Substitute.For<SignInManager<ApplicationUser>>(userManager, contextAccessor, claimsFactory, null, null, null, null);

        var controller = new AccountController(userManager, signInManager);

        var model = new RegisterViewModel
        {
            DisplayName = "Test Attacker",
            Email = "attacker@evil.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Role = Roles.Admin // Attempt privilege escalation!
        };

        var result = await controller.Register(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(RegisterViewModel.Role)));

        // Verify user was NOT created
        await userManager.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
    }
}

