using Induva.Application.Abstractions;
using Induva.Infrastructure.Identity;
using Induva.Web.Controllers;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class AccountPasswordResetTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;
    private readonly AccountController _controller;

    public AccountPasswordResetTests()
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(userStore, null, null, null, null, null, null, null, null);
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(_userManager, contextAccessor, claimsFactory, null, null, null, null);
        _emailService = Substitute.For<IEmailService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<AccountController>>();

        _controller = new AccountController(_userManager, _signInManager, _emailService, _cache, _logger);

        // Setup mock HttpContext and TempData
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        _controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
    }

    [Fact]
    public void ForgotPassword_GET_returns_view_with_empty_model()
    {
        var result = _controller.ForgotPassword();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ForgotPasswordViewModel>(viewResult.Model);
        Assert.Empty(model.Email);
    }

    [Fact]
    public async Task ForgotPassword_POST_with_non_existing_user_returns_view_with_error_and_does_not_send_email()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult<ApplicationUser?>(null));

        var vm = new ForgotPasswordViewModel { Email = "notfound@nowhere.com" };
        var result = await _controller.ForgotPassword(vm);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("No account found with this email address")));

        // Email must NOT be sent
        await _emailService.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ForgotPassword_POST_with_existing_user_generates_otp_and_redirects_to_reset_password()
    {
        var email = "owner@factory.com";
        var user = new ApplicationUser { Email = email, UserName = email, DisplayName = "Factory Owner" };
        _userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.GeneratePasswordResetTokenAsync(user).Returns(Task.FromResult("identity-token-xyz"));

        var vm = new ForgotPasswordViewModel { Email = email };
        var result = await _controller.ForgotPassword(vm);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ResetPassword), redirectResult.ActionName);
        Assert.Equal(email, redirectResult.RouteValues?["email"]);

        // Verify OTP is cached
        var cacheKey = $"PasswordResetOtp:{email.ToUpperInvariant()}";
        Assert.True(_cache.TryGetValue<PasswordResetOtpEntry>(cacheKey, out var entry));
        Assert.NotNull(entry);
        Assert.Equal(6, entry.Otp.Length);
        Assert.Matches(@"^\d{6}$", entry.Otp);

        // Verify email was sent with OTP
        await _emailService.Received(1).SendEmailAsync(
            email,
            Arg.Is<string>(s => s.Contains("Your INDUVA Password Reset Code")),
            Arg.Is<string>(b => b.Contains(entry.Otp)));
    }

    [Fact]
    public void ResetPassword_GET_without_email_redirects_to_forgot_password()
    {
        var result = _controller.ResetPassword(email: null);
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ForgotPassword), redirectResult.ActionName);
    }

    [Fact]
    public void ResetPassword_GET_with_email_returns_view()
    {
        var result = _controller.ResetPassword(email: "owner@factory.com");
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ResetPasswordViewModel>(viewResult.Model);
        Assert.Equal("owner@factory.com", model.Email);
    }

    [Fact]
    public async Task ResetPassword_POST_with_valid_otp_resets_password_and_clears_cache()
    {
        var email = "owner@factory.com";
        var user = new ApplicationUser { Email = email, UserName = email };
        _userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.ResetPasswordAsync(user, "valid-token", "NewStrongPass123!")
            .Returns(Task.FromResult(IdentityResult.Success));

        var cacheKey = $"PasswordResetOtp:{email.ToUpperInvariant()}";
        _cache.Set(cacheKey, new PasswordResetOtpEntry
        {
            Email = email,
            Otp = "654321",
            IdentityToken = "valid-token"
        });

        var vm = new ResetPasswordViewModel
        {
            Email = email,
            Otp = "654321",
            Password = "NewStrongPass123!",
            ConfirmPassword = "NewStrongPass123!"
        };

        var result = await _controller.ResetPassword(vm);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ResetPasswordConfirmation), redirectResult.ActionName);

        // OTP must be removed from cache after successful reset
        Assert.False(_cache.TryGetValue(cacheKey, out _));
    }

    [Fact]
    public async Task ResetPassword_POST_with_invalid_otp_fails_and_increments_attempts()
    {
        var email = "owner@factory.com";
        var user = new ApplicationUser { Email = email, UserName = email };
        _userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(user));

        var cacheKey = $"PasswordResetOtp:{email.ToUpperInvariant()}";
        _cache.Set(cacheKey, new PasswordResetOtpEntry
        {
            Email = email,
            Otp = "123456",
            IdentityToken = "valid-token",
            FailedAttempts = 0
        });

        var vm = new ResetPasswordViewModel
        {
            Email = email,
            Otp = "999999", // Wrong OTP
            Password = "NewStrongPass123!",
            ConfirmPassword = "NewStrongPass123!"
        };

        var result = await _controller.ResetPassword(vm);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("Invalid verification code")));

        // Verify attempts incremented
        Assert.True(_cache.TryGetValue<PasswordResetOtpEntry>(cacheKey, out var entry));
        Assert.Equal(1, entry!.FailedAttempts);
    }

    [Fact]
    public async Task ResetPassword_POST_with_expired_otp_returns_error()
    {
        var email = "owner@factory.com";
        var user = new ApplicationUser { Email = email, UserName = email };
        _userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(user));

        // Nothing in cache (simulating expired)
        var vm = new ResetPasswordViewModel
        {
            Email = email,
            Otp = "123456",
            Password = "NewStrongPass123!",
            ConfirmPassword = "NewStrongPass123!"
        };

        var result = await _controller.ResetPassword(vm);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("verification code has expired")));
    }
}
