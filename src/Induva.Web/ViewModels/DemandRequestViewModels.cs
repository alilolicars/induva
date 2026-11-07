using System.ComponentModel.DataAnnotations;
using Induva.Domain.Constants;
using Induva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class DemandRequestFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(250), Display(Name = "Request Title")]
    public string Title { get; set; } = "";

    [Required, StringLength(150), Display(Name = "Product Needed")]
    public string ProductName { get; set; } = "";

    [Required]
    public Sector Sector { get; set; }

    [Display(Name = "Required Capability Tags")]
    public List<string> SelectedTags { get; set; } = [];

    [Required, Range(0.01, 100000000), Display(Name = "Quantity Required")]
    public decimal Quantity { get; set; } = 1000;

    [Required, StringLength(50), Display(Name = "Unit of Measure (e.g. pieces, tons, meters)")]
    public string Unit { get; set; } = "pieces";

    [Range(0, 1000000000), Display(Name = "Target Unit Price (EGP, optional)")]
    public decimal? TargetPriceEgp { get; set; }

    [Display(Name = "Delivery Deadline (Optional)")]
    public DateTime? DeadlineUtc { get; set; }

    [Required, Display(Name = "Destination Governorate")]
    public int GovernorateId { get; set; }

    [Display(Name = "This is an export procurement order")]
    public bool IsExport { get; set; }

    [Required, Display(Name = "Status")]
    public string Status { get; set; } = "Open";

    [StringLength(4000), Display(Name = "Procurement Specifications / Quality Standards")]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];

    public static IEnumerable<SelectListItem> AvailableCapabilityTags =>
        CapabilityTags.All.Select(t => new SelectListItem(t, t));
}

