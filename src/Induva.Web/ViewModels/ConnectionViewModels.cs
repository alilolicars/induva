using System.ComponentModel.DataAnnotations;
using Induva.Domain.Enums;

namespace Induva.Web.ViewModels;

public class SendConnectionModalViewModel
{
    [Required]
    public string ToUserId { get; set; } = "";

    [Required]
    public MatchEntityType RelatedType { get; set; }

    [Required]
    public int RelatedId { get; set; }

    public string TargetTitle { get; set; } = "";

    [Required, StringLength(1000)]
    public string Message { get; set; } = "I would like to explore mutual cooperation based on platform capabilities.";
}

