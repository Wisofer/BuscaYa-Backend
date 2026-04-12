using BuscaYa.Data;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class AccountDeletionService : IAccountDeletionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserDeletionService _userDeletion;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(
        ApplicationDbContext context,
        IUserDeletionService userDeletion,
        ILogger<AccountDeletionService> logger)
    {
        _context = context;
        _userDeletion = userDeletion;
        _logger = logger;
    }

    public AccountDeletionStatusDto GetStatus(int userId)
    {
        var user = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return new AccountDeletionStatusDto { pending = false };

        var scheduled = user.AccountDeletionScheduledAt;
        if (scheduled == null)
            return new AccountDeletionStatusDto { pending = false };

        var now = DateTime.UtcNow;
        int? daysRemaining = null;
        if (scheduled > now)
            daysRemaining = (int)Math.Ceiling((scheduled.Value - now).TotalDays);

        return new AccountDeletionStatusDto
        {
            pending = true,
            requested_at = user.AccountDeletionRequestedAt,
            scheduled_deletion_at = scheduled,
            days_remaining = daysRemaining
        };
    }

    public async Task<AccountDeletionRequestResult> RequestDeletionAsync(
        int userId,
        string? password,
        bool confirmWithoutPassword)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new AccountDeletionRequestResult { ok = false, error = "Usuario no encontrado." };

        if (user.AccountDeletionScheduledAt != null)
            return new AccountDeletionRequestResult
            {
                ok = true,
                scheduled_deletion_at = user.AccountDeletionScheduledAt,
                error = null
            };

        var hasPassword = !string.IsNullOrEmpty(user.Contrasena);
        if (hasPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || !PasswordHelper.VerifyPassword(password, user.Contrasena))
                return new AccountDeletionRequestResult { ok = false, error = "Contraseña incorrecta." };
        }
        else
        {
            if (!confirmWithoutPassword)
                return new AccountDeletionRequestResult
                {
                    ok = false,
                    error = "Confirma que deseas eliminar la cuenta (confirm_without_password: true)."
                };
        }

        var scheduled = DateTime.UtcNow.Add(AccountDeletionPolicy.GracePeriod);
        user.AccountDeletionRequestedAt = DateTime.UtcNow;
        user.AccountDeletionScheduledAt = scheduled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuario {UserId} solicitud de eliminación de cuenta; fecha efectiva {Utc}", userId, scheduled);

        return new AccountDeletionRequestResult { ok = true, scheduled_deletion_at = scheduled };
    }

    public async Task<bool> CancelDeletionAsync(int userId)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return false;

        if (user.AccountDeletionScheduledAt == null)
            return true;

        user.AccountDeletionRequestedAt = null;
        user.AccountDeletionScheduledAt = null;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Usuario {UserId} canceló la eliminación programada de cuenta.", userId);
        return true;
    }

    public async Task<AccountDeletionProcessResult> ProcessDueDeletionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueIds = await _context.Usuarios.AsNoTracking()
            .Where(u => u.AccountDeletionScheduledAt != null && u.AccountDeletionScheduledAt <= now)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var id in dueIds)
        {
            try
            {
                var ok = await _userDeletion.DeleteUserAndAllDataAsync(id, cancellationToken);
                if (ok)
                {
                    deleted++;
                    _logger.LogInformation("Cuenta eliminada tras periodo de gracia: usuario {UserId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cuenta programada para usuario {UserId}", id);
            }
        }

        return new AccountDeletionProcessResult { users_deleted = deleted };
    }
}
