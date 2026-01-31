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

    public bool ExisteNombreUsuario(string nombreUsuario)
    {
        return _context.Usuarios
            .Any(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());
    }

    public Usuario? RegistrarCliente(string nombreUsuario, string contrasena, string nombreCompleto, string? telefono, string? email)
    {
        // Validar que el nombre de usuario no exista
        if (ExisteNombreUsuario(nombreUsuario))
        {
            return null;
        }

        // Crear nuevo usuario cliente
        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Contrasena = PasswordHelper.HashPassword(contrasena),
            NombreCompleto = nombreCompleto,
            Telefono = telefono,
            Email = email, // Se guarda tal cual viene
            Rol = SD.RolCliente,
            Activo = true,
            FechaCreacion = DateTime.Now,
            TiendaId = null // Cliente no tiene tienda
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
            usuario.NombreCompleto = nombreCompleto;
        }

        usuario.Telefono = telefono;
        usuario.Email = email;
        if (fotoPerfilUrl != null)
            usuario.FotoPerfilUrl = fotoPerfilUrl;

        _context.SaveChanges();
        return true;
    }
}
