using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class UserDeletionService : IUserDeletionService
{
    private readonly ApplicationDbContext _context;

    public UserDeletionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DeleteUserAndAllDataAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!await _context.Usuarios.AnyAsync(u => u.Id == userId, cancellationToken))
            return false;

        var tienda = await _context.Tiendas.FirstOrDefaultAsync(t => t.UsuarioId == userId, cancellationToken);
        if (tienda != null)
        {
            _context.Tiendas.Remove(tienda);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _context.HistorialBusquedas.Where(h => h.UsuarioId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var stub = new Usuario { Id = userId };
        _context.Usuarios.Attach(stub);
        _context.Usuarios.Remove(stub);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
