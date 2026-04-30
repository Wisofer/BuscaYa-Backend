using System.Net;
using System.Net.Mail;
using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

public class BrevoEmailService : IBrevoEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        IConfiguration configuration,
        ILogger<BrevoEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlContent, string? textContent = null, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"];
        var fromName = _configuration["Smtp:FromName"] ?? "BuscaYa";
        var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl;
        var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("SMTP no está configurado. Se omite envío a {ToEmail}", toEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = string.IsNullOrWhiteSpace(htmlContent) ? (textContent ?? string.Empty) : htmlContent,
            IsBodyHtml = !string.IsNullOrWhiteSpace(htmlContent)
        };
        message.To.Add(new MailAddress(toEmail));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password)
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo por SMTP a {ToEmail}", toEmail);
        }
    }
}
