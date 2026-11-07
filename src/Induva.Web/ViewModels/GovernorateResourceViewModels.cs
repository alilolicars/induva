using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Induva.Web.ViewModels;

public class GovernorateResourceFormViewModel
{
    public int? Id { get; set; }

    [Required, Display(Name = "Governorate")]
    public int GovernorateId { get; set; }

    [Required, Display(Name = "Raw Material")]
    public int RawMaterialId { get; set; }

    [Range(0, 1000000000), Display(Name = "Estimated Annual Reserve/Yield (Tons)")]
    public decimal? EstimatedAnnualTons { get; set; }

    [StringLength(1000), Display(Name = "Indicative Notes / Extraction Areas")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];
    public IEnumerable<SelectListItem> RawMaterials { get; set; } = [];
}

