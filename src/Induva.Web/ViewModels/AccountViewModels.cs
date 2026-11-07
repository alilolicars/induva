using System.ComponentModel.DataAnnotations;
using Induva.Domain.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, Display(Name = "Display Name"), StringLength(100)]
    public string DisplayName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required, Display(Name = "Account Type (Role)")]
    public string Role { get; set; } = Roles.FactoryOwner;

    public static IEnumerable<SelectListItem> AvailableRoles =>
        Roles.SelfRegistrable.Select(r => new SelectListItem
        {
            Value = r,
            Text = r switch
            {
                Roles.FactoryOwner => "Factory Owner (Manage factories & machines)",
                Roles.Supplier => "Supplier (Provide raw materials)",
                Roles.Startup => "Industrial Startup (Looking for manufacturing or funding)",
                Roles.Investor => "Investor (Fund industrial opportunities)",
                Roles.Buyer => "Buyer (Post procurement & demand requests)",
                _ => r
            }
        });
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your registered email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter the 6-digit verification code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must consist of 6 numeric digits.")]
    [Display(Name = "Verification Code")]
    public string Otp { get; set; } = "";

    [Required(ErrorMessage = "Please enter a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}


