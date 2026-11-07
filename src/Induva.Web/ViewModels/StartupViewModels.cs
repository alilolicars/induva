using System.ComponentModel.DataAnnotations;
using Induva.Domain.Constants;
using Induva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class StartupFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200), Display(Name = "Startup / Project Name")]
    public string Name { get; set; } = "";

    [Required]
    public Sector Sector { get; set; }

    [Required, Display(Name = "Current Development Stage")]
    public StartupStage Stage { get; set; } = StartupStage.Prototype;

    [Required, StringLength(4000), Display(Name = "Product Idea & Innovation")]
    public string ProductIdea { get; set; } = "";

    [StringLength(4000), Display(Name = "Detailed Description")]
    public string? Description { get; set; }

    [Range(0, 1000000000), Display(Name = "Funding Needed (EGP)")]
    public decimal? FundingNeededEgp { get; set; }

    [Display(Name = "Required Manufacturing Capabilities (Select all that apply)")]
    public List<string> SelectedTags { get; set; } = [];

    [Display(Name = "Governorate (Optional)")]
    public int? GovernorateId { get; set; }

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];

    public static IEnumerable<SelectListItem> AvailableCapabilityTags =>
        CapabilityTags.All.Select(t => new SelectListItem(t, t));
}

