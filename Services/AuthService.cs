using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Usuario? ValidarUsuario(string nombreUsuario, string contrasena)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            return null;
        }

        // Buscar usuario en la base de datos
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower() && u.Activo);

        if (usuario == null)
        {
            return null;
        }

        // Verificar contraseña
        if (!PasswordHelper.VerifyPassword(contrasena, usuario.Contrasena))
        {
            return null;
        }

        return usuario;
    }

    public bool EsAdministrador(Usuario usuario)
    {
        return usuario.Rol == SD.RolAdministrador;
    }

    public bool EsUsuarioNormal(Usuario usuario)
    {
        return usuario.Rol == SD.RolTiendaOwner || usuario.Rol == SD.RolCliente;
    }

    public Usuario? ObtenerUsuarioPorId(int id)
    {
        return _context.Usuarios.Find(id);
    }
}
