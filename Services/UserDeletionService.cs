using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class UserDeletionService : IUserDeletionService
{
    private readonly ApplicationDbContext _context;
    private readonly IS3BucketService _s3Service;

    public UserDeletionService(ApplicationDbContext context, IS3BucketService s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    public async Task<bool> DeleteUserAndAllDataAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Usuarios
            .Include(u => u.Tienda)
            .ThenInclude(t => t!.Productos)
            .ThenInclude(p => p.Imagenes)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return false;

        var filesToDelete = new List<string>();

        // Recolectar foto de perfil
        if (!string.IsNullOrEmpty(user.FotoPerfilUrl))
        {
            filesToDelete.Add(user.FotoPerfilUrl);
        }

        // Recolectar logos, portadas, productos y galerías
        if (user.Tienda != null)
        {
            if (!string.IsNullOrEmpty(user.Tienda.LogoUrl))
            {
                filesToDelete.Add(user.Tienda.LogoUrl);
            }
            if (!string.IsNullOrEmpty(user.Tienda.FotoUrl))
            {
                filesToDelete.Add(user.Tienda.FotoUrl);
            }

            foreach (var product in user.Tienda.Productos)
            {
                if (!string.IsNullOrEmpty(product.FotoUrl))
                {
                    filesToDelete.Add(product.FotoUrl);
                }
                foreach (var img in product.Imagenes)
                {
                    if (!string.IsNullOrEmpty(img.Url))
                    {
                        filesToDelete.Add(img.Url);
                    }
                }
            }
        }

        // Borrar archivos físicamente de S3
        foreach (var fileUrl in filesToDelete)
        {
            try
            {
                await _s3Service.DeleteFileIfExistsAsync(fileUrl);
            }
            catch
            {
                // Silenciamos excepciones para evitar bloquear el proceso de eliminación principal en BD
            }
        }

        // Remover tienda en cascada (si existe)
        if (user.Tienda != null)
        {
            _context.Tiendas.Remove(user.Tienda);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Limpiar historial de búsquedas
        await _context.HistorialBusquedas.Where(h => h.UsuarioId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // Remover usuario de la base de datos
        _context.Usuarios.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
