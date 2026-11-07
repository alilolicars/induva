using Induva.Application.Abstractions;
using Induva.Domain.Constants;
using Induva.Infrastructure.Identity;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Induva.Web.Controllers;

public class PasswordResetOtpEntry
{
    public string Email { get; set; } = "";
    public string Otp { get; set; } = "";
    public string IdentityToken { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int FailedAttempts { get; set; } = 0;
}

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailService? emailService = null,
    IMemoryCache? cache = null,
    ILogger<AccountController>? logger = null) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Account is locked out. Please wait a few minutes before trying again.");
            return View(vm);
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(vm);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await userManager.FindByEmailAsync(vm.Email.Trim());
        if (user == null)
        {
            // Explicit error when user is not registered in the database!
            ModelState.AddModelError(nameof(vm.Email), "No account found with this email address. Please check your spelling or register.");
            return View(vm);
        }

        // Generate 6-digit numeric OTP & ASP.NET Core Identity Reset Token
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        if (cache != null)
        {
            var cacheKey = $"PasswordResetOtp:{user.Email!.ToUpperInvariant()}";
            var entry = new PasswordResetOtpEntry
            {
                Email = user.Email,
                Otp = otp,
                IdentityToken = token,
                CreatedAt = DateTime.UtcNow
            };
            cache.Set(cacheKey, entry, TimeSpan.FromMinutes(15));
        }

        if (emailService != null)
        {
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'IBM Plex Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F6F4EF; color: #0E1B24; margin: 0; padding: 24px; }}
    .card {{ max-width: 540px; margin: 0 auto; background: #FFFFFF; border: 1px solid #E3DFD6; border-radius: 12px; padding: 36px 32px; box-shadow: 0 2px 8px rgba(14,27,36,0.04); }}
    .brand {{ font-size: 20px; font-weight: 700; color: #0B5563; margin-bottom: 24px; text-decoration: none; display: inline-block; letter-spacing: 0.04em; }}
    h2 {{ font-size: 22px; color: #0E1B24; margin-top: 0; margin-bottom: 12px; }}
    p {{ font-size: 14px; line-height: 1.6; color: #5B6670; margin-bottom: 16px; }}
    .otp-box {{ background-color: #F6F4EF; border: 2px dashed #0B5563; border-radius: 10px; padding: 18px 24px; text-align: center; margin: 24px 0; }}
    .otp-code {{ font-family: 'Courier New', Courier, monospace; font-size: 34px; font-weight: 700; letter-spacing: 0.35em; color: #0B5563; display: inline-block; }}
    .footnote {{ font-size: 12px; color: #8A949C; border-top: 1px solid #E3DFD6; padding-top: 16px; margin-top: 24px; line-height: 1.5; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='brand'>INDUVA &middot; Industrial Platform</div>
    <h2>Your Password Reset Verification Code</h2>
    <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(user.DisplayName ?? user.Email)}</strong>,</p>
    <p>We received a request to reset the password for your INDUVA account. Use the 6-digit verification code below:</p>
    <div class='otp-box'>
      <div class='otp-code'>{otp}</div>
    </div>
    <p>Enter this code on the password reset page to verify your identity and set a new password. This code is valid for <strong>15 minutes</strong>.</p>
    <p class='footnote'>If you did not request this verification code, please ignore this email. Your password will remain unchanged.</p>
  </div>
</body>
</html>";

            try
            {
                await emailService.SendEmailAsync(user.Email!, "Your INDUVA Password Reset Code", emailBody);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email);
            }
        }

        TempData["Success"] = $"A 6-digit verification code has been sent to {user.Email}.";
        return RedirectToAction(nameof(ResetPassword), new { email = user.Email });
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await userManager.FindByEmailAsync(vm.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError(nameof(vm.Email), "No account found with this email address.");
            return View(vm);
        }

        var cacheKey = $"PasswordResetOtp:{user.Email!.ToUpperInvariant()}";
        if (cache == null || !cache.TryGetValue<PasswordResetOtpEntry>(cacheKey, out var entry) || entry == null)
        {
            ModelState.AddModelError(nameof(vm.Otp), "The verification code has expired. Please request a new one.");
            return View(vm);
        }

        if (entry.FailedAttempts >= 5)
        {
            cache.Remove(cacheKey);
            ModelState.AddModelError(nameof(vm.Otp), "Too many failed attempts. Please request a new verification code.");
            return View(vm);
        }

        var cleanEnteredOtp = (vm.Otp ?? "").Trim().Replace(" ", "");
        if (!string.Equals(entry.Otp, cleanEnteredOtp, StringComparison.Ordinal))
        {
            entry.FailedAttempts++;
            ModelState.AddModelError(nameof(vm.Otp), "Invalid verification code. Please check your email and try again.");
            return View(vm);
        }

        var result = await userManager.ResetPasswordAsync(user, entry.IdentityToken, vm.Password);
        if (result.Succeeded)
        {
            cache.Remove(cacheKey);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(vm);
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(ForgotPassword));

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            TempData["Error"] = "No account found with this email address.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        if (cache != null)
        {
            var cacheKey = $"PasswordResetOtp:{user.Email!.ToUpperInvariant()}";
            var entry = new PasswordResetOtpEntry
            {
                Email = user.Email,
                Otp = otp,
                IdentityToken = token,
                CreatedAt = DateTime.UtcNow
            };
            cache.Set(cacheKey, entry, TimeSpan.FromMinutes(15));
        }

        if (emailService != null)
        {
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'IBM Plex Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F6F4EF; color: #0E1B24; margin: 0; padding: 24px; }}
    .card {{ max-width: 540px; margin: 0 auto; background: #FFFFFF; border: 1px solid #E3DFD6; border-radius: 12px; padding: 36px 32px; box-shadow: 0 2px 8px rgba(14,27,36,0.04); }}
    .brand {{ font-size: 20px; font-weight: 700; color: #0B5563; margin-bottom: 24px; text-decoration: none; display: inline-block; letter-spacing: 0.04em; }}
    h2 {{ font-size: 22px; color: #0E1B24; margin-top: 0; margin-bottom: 12px; }}
    p {{ font-size: 14px; line-height: 1.6; color: #5B6670; margin-bottom: 16px; }}
    .otp-box {{ background-color: #F6F4EF; border: 2px dashed #0B5563; border-radius: 10px; padding: 18px 24px; text-align: center; margin: 24px 0; }}
    .otp-code {{ font-family: 'Courier New', Courier, monospace; font-size: 34px; font-weight: 700; letter-spacing: 0.35em; color: #0B5563; display: inline-block; }}
    .footnote {{ font-size: 12px; color: #8A949C; border-top: 1px solid #E3DFD6; padding-top: 16px; margin-top: 24px; line-height: 1.5; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='brand'>INDUVA &middot; Industrial Platform</div>
    <h2>Your New Password Reset Verification Code</h2>
    <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(user.DisplayName ?? user.Email)}</strong>,</p>
    <p>We received a request to resend your INDUVA verification code:</p>
    <div class='otp-box'>
      <div class='otp-code'>{otp}</div>
    </div>
    <p>This verification code is valid for <strong>15 minutes</strong> and can only be used once.</p>
    <p class='footnote'>If you did not request this verification code, please ignore this email.</p>
  </div>
</body>
</html>";

            try
            {
                await emailService.SendEmailAsync(user.Email!, "Your New INDUVA Verification Code", emailBody);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to resend verification code to {Email}", user.Email);
            }
        }

        TempData["Success"] = "A new verification code has been sent to your email.";
        return RedirectToAction(nameof(ResetPassword), new { email = user.Email });
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        // Security check: Role must be strictly in Roles.SelfRegistrable
        if (!Roles.SelfRegistrable.Contains(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), "The selected role is not available for public registration.");
        }

        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            DisplayName = vm.DisplayName.Trim()
        };

        var result = await userManager.CreateAsync(user, vm.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, vm.Role);
            await signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = $"Welcome to INDUVA, {user.DisplayName}!";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(vm);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

