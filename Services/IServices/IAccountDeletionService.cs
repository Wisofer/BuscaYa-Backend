namespace BuscaYa.Services.IServices;

public static class AccountDeletionPolicy
{
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(10);
}

public class AccountDeletionStatusDto
{
    public bool pending { get; set; }
    public DateTime? requested_at { get; set; }
    public DateTime? scheduled_deletion_at { get; set; }
    public int? days_remaining { get; set; }
}

public class AccountDeletionRequestResult
{
    public bool ok { get; set; }
    public string? error { get; set; }
    public DateTime? scheduled_deletion_at { get; set; }
}

public interface IAccountDeletionService
{
    AccountDeletionStatusDto GetStatus(int userId);
    Task<AccountDeletionRequestResult> RequestDeletionAsync(int userId, string? password, bool confirmWithoutPassword);
    Task<bool> CancelDeletionAsync(int userId);
    Task<AccountDeletionProcessResult> ProcessDueDeletionsAsync(CancellationToken cancellationToken = default);
}

public class AccountDeletionProcessResult
{
    public int users_deleted { get; set; }
}
