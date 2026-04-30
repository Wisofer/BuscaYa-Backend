namespace BuscaYa.Services.IServices;

public interface IBrevoEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlContent, string? textContent = null, CancellationToken cancellationToken = default);
}
