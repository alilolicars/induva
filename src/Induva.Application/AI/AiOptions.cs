namespace Induva.Application.AI;

public class AiOptions
{
    public string Mode { get; set; } = "Live"; // Live | Demo
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1/";
    public string ModelId { get; set; } = "deepseek.v3.2";
    public string? ApiKey { get; set; }
    public string OutputLanguage { get; set; } = "en"; // en | ar
    public int CacheHours { get; set; } = 24;
    public int DailyLimitPerUser { get; set; } = 30;
    public int MaxConcurrentCalls { get; set; } = 2;
    public int RequestTimeoutSeconds { get; set; } = 120;
}

