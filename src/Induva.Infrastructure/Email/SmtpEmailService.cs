using System.Net;
using System.Net.Mail;
using Induva.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Induva.Infrastructure.Email;

public class SmtpEmailService(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var cleanedPassword = (_options.Password ?? string.Empty).Replace(" ", "");
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.Username, cleanedPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP ({Host}:{Port})", toEmail, _options.Host, _options.Port);
            throw;
        }
    }
}

