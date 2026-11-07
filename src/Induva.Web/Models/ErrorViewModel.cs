namespace Induva.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public int? StatusCode { get; set; }
    public string Title { get; set; } = "An unexpected error occurred";
    public string Message { get; set; } = "We encountered an issue processing your request. Please try again or return to the home page.";
}
