using System.ComponentModel.DataAnnotations;
using Induva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class FactoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Factory name is required."), StringLength(200)]
    [Display(Name = "Factory Name")]
    public string Name { get; set; } = "";

    [Required]
    public Sector Sector { get; set; }

    [Required, Display(Name = "Governorate")]
    public int GovernorateId { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(0, 100), Display(Name = "Current Utilization %")]
    public int UtilizationPercent { get; set; } = 70;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Display(Name = "Visible on public map & directory")]
    public bool IsPublic { get; set; } = true;

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];
}

public class MachineFormViewModel
{
    [Required, StringLength(150), Display(Name = "Machine Name")]
    public string Name { get; set; } = "";

    [StringLength(100), Display(Name = "Machine Type / Model")]
    public string? MachineType { get; set; }

    [Range(1, 10000), Display(Name = "Quantity")]
    public int Quantity { get; set; } = 1;

    [Range(0, 1000000), Display(Name = "Capacity Per Hour")]
    public decimal? CapacityPerHour { get; set; }

    [StringLength(50), Display(Name = "Capacity Unit (e.g. kg, pieces, liters)")]
    public string? CapacityUnit { get; set; }

    [StringLength(1000), Display(Name = "Notes / Specs")]
    public string? Description { get; set; }
}

public class IdleWindowFormViewModel
{
    [Range(1, 12), Display(Name = "Start Month")]
    public int StartMonth { get; set; } = 1;

    [Range(1, 12), Display(Name = "End Month")]
    public int EndMonth { get; set; } = 3;

    [Range(1, 100), Display(Name = "Available Capacity % During Window")]
    public int AvailableCapacityPercent { get; set; } = 50;

    [StringLength(500), Display(Name = "Seasonal Note (e.g. post-harvest idle period)")]
    public string? Note { get; set; }
}

public class ProductFormViewModel
{
    [Required, StringLength(150), Display(Name = "Product Name")]
    public string Name { get; set; } = "";

    [StringLength(100), Display(Name = "Category")]
    public string? Category { get; set; }
}

public class MaterialNeedFormViewModel
{
    [Required, Display(Name = "Raw Material")]
    public int RawMaterialId { get; set; }

    [Range(0, 1000000), Display(Name = "Estimated Monthly Consumption (Tons)")]
    public decimal? MonthlyTons { get; set; }

    public IEnumerable<SelectListItem> RawMaterials { get; set; } = [];
}

