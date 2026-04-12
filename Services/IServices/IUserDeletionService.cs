namespace BuscaYa.Services.IServices;

/// <summary>Elimina un usuario y todos sus datos asociados (tienda, productos, favoritos, etc.).</summary>
public interface IUserDeletionService
{
    /// <returns>False si el usuario no existía.</returns>
    Task<bool> DeleteUserAndAllDataAsync(int userId, CancellationToken cancellationToken = default);
}
