namespace Induva.Infrastructure.Email;

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "alilolicars@gmail.com";
    public string Password { get; set; } = "sweo udkc dhzf dgbq";
    public string SenderEmail { get; set; } = "alilolicars@gmail.com";
    public string SenderName { get; set; } = "INDUVA Platform";
}

