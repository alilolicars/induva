namespace Induva.Application.Abstractions;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

