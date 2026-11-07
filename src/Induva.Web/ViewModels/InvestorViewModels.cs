using System.ComponentModel.DataAnnotations;
using Induva.Domain.Enums;

namespace Induva.Web.ViewModels;

public class InvestorFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200), Display(Name = "Display Name or Fund Title")]
    public string DisplayName { get; set; } = "";

    [Display(Name = "Target Industrial Sectors")]
    public List<Sector> SelectedSectors { get; set; } = [];

    [Display(Name = "Preferred Startup Stages")]
    public List<StartupStage> SelectedStages { get; set; } = [];

    [Range(0, 1000000000), Display(Name = "Minimum Ticket Size (EGP)")]
    public decimal? MinTicketEgp { get; set; }

    [Range(0, 1000000000), Display(Name = "Maximum Ticket Size (EGP)")]
    public decimal? MaxTicketEgp { get; set; }

    [StringLength(4000), Display(Name = "Investment Mandate & Background")]
    public string? Bio { get; set; }
}

