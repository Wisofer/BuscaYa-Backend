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

        // Buscar usuario (ToLower para que EF Core traduzca a SQL; OrdinalIgnoreCase no se traduce)
        var nombreNorm = nombreUsuario.Trim().ToLowerInvariant();
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.NombreUsuario.ToLower() == nombreNorm && u.Activo);

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

    public bool EsAdministrador(Usuario? usuario)
    {
        return usuario?.Rol == SD.RolAdministrador;
    }

    public bool EsUsuarioNormal(Usuario? usuario)
    {
        return usuario != null && (usuario.Rol == SD.RolTiendaOwner || usuario.Rol == SD.RolCliente);
    }

    public Usuario? ObtenerUsuarioPorId(int id)
    {
        return _context.Usuarios.Find(id);
    }

    public bool ExisteNombreUsuario(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario)) return false;
        var nombreNorm = nombreUsuario.Trim().ToLowerInvariant();
        return _context.Usuarios
            .Any(u => u.NombreUsuario.ToLower() == nombreNorm);
    }

    public Usuario? RegistrarCliente(string nombreUsuario, string contrasena, string nombreCompleto, string? telefono, string? email)
    {
        if (ExisteNombreUsuario(nombreUsuario))
            return null;

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario.Trim(),
            Contrasena = PasswordHelper.HashPassword(contrasena),
            NombreCompleto = (nombreCompleto ?? "").Trim(),
            Telefono = telefono?.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Rol = SD.RolCliente,
            Activo = true,
            FechaCreacion = DateTime.Now,
            TiendaId = null
        };

        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
        return usuario;
    }

    public Usuario? ObtenerUsuarioPorGoogleIdOEmail(string? googleId, string? email)
    {
        if (string.IsNullOrWhiteSpace(googleId) && string.IsNullOrWhiteSpace(email))
            return null;

        var query = _context.Usuarios.Where(u => u.Activo);

        if (!string.IsNullOrWhiteSpace(googleId))
        {
            var byGoogle = query.FirstOrDefault(u => u.GoogleId == googleId);
            if (byGoogle != null) return byGoogle;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailNorm = email.Trim().ToLowerInvariant();
            // ToLower() en columna para que EF Core traduzca a SQL (LOWER); no usar StringComparison
            return query.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == emailNorm);
        }

        return null;
    }

    public Usuario? RegistrarClienteConGoogle(string googleId, string nombreUsuario, string contrasena, string nombreCompleto, string? telefono, string? email, string? fotoPerfilUrl)
    {
        if (ExisteNombreUsuario(nombreUsuario))
            return null;

        var usuario = new Usuario
        {
            GoogleId = googleId,
            NombreUsuario = nombreUsuario.Trim(),
            Contrasena = PasswordHelper.HashPassword(contrasena),
            NombreCompleto = (nombreCompleto ?? "").Trim(),
            Telefono = telefono?.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            FotoPerfilUrl = fotoPerfilUrl,
            Rol = SD.RolCliente,
            Activo = true,
            FechaCreacion = DateTime.Now,
            TiendaId = null
        };

        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
        return usuario;
    }

    public Usuario? ObtenerUsuarioPorAppleIdOEmail(string? appleId, string? email)
    {
        if (string.IsNullOrWhiteSpace(appleId) && string.IsNullOrWhiteSpace(email))
            return null;

        var query = _context.Usuarios.Where(u => u.Activo);

        if (!string.IsNullOrWhiteSpace(appleId))
        {
            var byApple = query.FirstOrDefault(u => u.AppleId == appleId);
            if (byApple != null) return byApple;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailNorm = email.Trim().ToLowerInvariant();
            return query.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == emailNorm);
        }

        return null;
    }

    public Usuario? RegistrarClienteConApple(string appleId, string nombreUsuario, string contrasena, string nombreCompleto, string? telefono, string? email, string? fotoPerfilUrl)
    {
        if (ExisteNombreUsuario(nombreUsuario))
            return null;

        var usuario = new Usuario
        {
            AppleId = appleId,
            NombreUsuario = nombreUsuario.Trim(),
            Contrasena = PasswordHelper.HashPassword(contrasena),
            NombreCompleto = (nombreCompleto ?? "").Trim(),
            Telefono = telefono?.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            FotoPerfilUrl = fotoPerfilUrl,
            Rol = SD.RolCliente,
            Activo = true,
            FechaCreacion = DateTime.Now,
            TiendaId = null
        };

        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
        return usuario;
    }

    public Usuario? ConvertirClienteATienda(int usuarioId, string nombreTienda, string? descripcionTienda,
        string? telefonoTienda, string whatsAppTienda, string? emailTienda,
        string direccionTienda, decimal latitud, decimal longitud, string ciudad, string departamento,
        TimeSpan? horarioApertura, TimeSpan? horarioCierre, string? diasAtencion, string? logoTienda, string? fotoTienda)
    {
        // Buscar el usuario
        var usuario = _context.Usuarios.Find(usuarioId);
        if (usuario == null)
        {
            return null;
        }

        // Validar que sea Cliente (no puede convertir si ya es TiendaOwner)
        if (usuario.Rol != SD.RolCliente)
        {
            return null; // Ya tiene tienda o es admin
        }

        // Validar que no tenga tienda ya
        if (usuario.TiendaId.HasValue)
        {
            return null; // Ya tiene tienda asociada
        }

        // Crear la tienda asociada
        var tienda = new Tienda
        {
            Nombre = nombreTienda,
            Descripcion = descripcionTienda,
            Telefono = telefonoTienda,
            WhatsApp = whatsAppTienda,
            Email = emailTienda,
            Direccion = direccionTienda,
            Latitud = latitud,
            Longitud = longitud,
            Ciudad = ciudad,
            Departamento = departamento,
            HorarioApertura = horarioApertura,
            HorarioCierre = horarioCierre,
            DiasAtencion = diasAtencion,
            LogoUrl = logoTienda,
            FotoUrl = fotoTienda,
            Plan = SD.PlanFree, // Todo gratis al inicio
            Activo = true,
            UsuarioId = usuario.Id,
            FechaCreacion = DateTime.Now
        };

        _context.Tiendas.Add(tienda);
        _context.SaveChanges();

        // Actualizar el usuario: cambiar rol y vincular tienda
        usuario.Rol = SD.RolTiendaOwner;
        usuario.TiendaId = tienda.Id;
        _context.SaveChanges();

        return usuario;
    }

    public bool ActualizarPerfil(int usuarioId, string nombreCompleto, string? telefono, string? email, string? fotoPerfilUrl = null)
    {
        var usuario = _context.Usuarios.Find(usuarioId);
        if (usuario == null || !usuario.Activo)
        {
            return false;
        }

        // Actualizar campos
        if (!string.IsNullOrWhiteSpace(nombreCompleto))
        {
            usuario.NombreCompleto = nombreCompleto.Trim();
        }

        usuario.Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        usuario.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (fotoPerfilUrl != null)
            usuario.FotoPerfilUrl = fotoPerfilUrl;

        _context.SaveChanges();
        return true;
    }
}
