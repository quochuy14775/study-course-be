using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using StudyCourseAPI.Configurations;

namespace StudyCourseAPI.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var secureOption = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}
