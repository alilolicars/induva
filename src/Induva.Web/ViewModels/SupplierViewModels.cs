using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class SupplierFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200), Display(Name = "Company Name")]
    public string CompanyName { get; set; } = "";

    [Required, Display(Name = "Governorate")]
    public int GovernorateId { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Display(Name = "Publicly listed")]
    public bool IsPublic { get; set; } = true;

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];
}

public class SupplierMaterialFormViewModel
{
    [Required, Display(Name = "Raw Material")]
    public int RawMaterialId { get; set; }

    [Range(0, 1000000), Display(Name = "Minimum Order Quantity (Tons)")]
    public decimal? MinOrderTons { get; set; }

    [StringLength(250), Display(Name = "Pricing / Supply Note")]
    public string? PriceNote { get; set; }

    public IEnumerable<SelectListItem> RawMaterials { get; set; } = [];
}

