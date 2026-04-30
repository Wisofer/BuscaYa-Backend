using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BuscaYa.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IBrevoEmailService _brevoEmailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IBrevoEmailService brevoEmailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _brevoEmailService = brevoEmailService;
        _logger = logger;
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

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);

        if (usuario == null || !usuario.Activo)
            return; // Respuesta genérica para no revelar si existe la cuenta

        var rawToken = GenerateSecureToken();
        usuario.PasswordResetTokenHash = ComputeSha256(rawToken);
        usuario.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
        await _context.SaveChangesAsync(cancellationToken);

        var resetUrl = _configuration["PasswordRecovery:ResetPasswordUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(resetUrl))
            resetUrl = "https://buscaya.app/reset-password";

        var link = $"{resetUrl}?email={Uri.EscapeDataString(usuario.Email!)}&token={Uri.EscapeDataString(rawToken)}";
        var subject = "Recupera tu contraseña de BuscaYa";
        var html = $"""
            <!doctype html>
            <html lang="es">
            <body style="margin:0;padding:0;background:#f6f8ff;font-family:Arial,sans-serif;color:#1f2937;">
              <div style="max-width:620px;margin:28px auto;padding:0 12px;">
                <div style="background:#ffffff;border:1px solid #dbe4ff;border-radius:16px;overflow:hidden;">
                  <div style="background:#eef2ff;padding:22px 24px 14px;text-align:center;">
                    <h1 style="margin:0;font-size:24px;color:#1d4ed8;">Restablecer contraseña</h1>
                    <p style="margin:10px 0 0;font-size:14px;color:#334155;">
                      Recibimos una solicitud para cambiar tu contraseña en BuscaYa.
                    </p>
                  </div>
                  <div style="padding:24px;">
                    <p style="margin:0 0 14px;font-size:15px;line-height:1.55;">
                      Haz clic en el siguiente botón para crear una nueva contraseña:
                    </p>
                    <p style="margin:0 0 18px;text-align:center;">
                      <a href="{link}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;padding:13px 24px;border-radius:10px;font-weight:700;">
                        Restablecer contraseña
                      </a>
                    </p>
                    <p style="margin:0 0 8px;color:#475569;font-size:13px;">Este enlace expira en 30 minutos.</p>
                    <p style="margin:0;color:#64748b;font-size:13px;">Si no solicitaste este cambio, ignora este correo.</p>
                    <hr style="border:none;border-top:1px solid #e2e8f0;margin:18px 0;" />
                    <p style="margin:0;color:#64748b;font-size:12px;word-break:break-all;">
                      Si el botón no abre, copia y pega este enlace:<br />{link}
                    </p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
        var text = $"Restablece tu contraseña: {link} (expira en 30 minutos)";

        try
        {
            await _brevoEmailService.SendAsync(usuario.Email!, subject, html, text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo de recuperación para usuario {UsuarioId}", usuario.Id);
        }
    }

    public async Task<bool> ResetPasswordByTokenAsync(string email, string token, string nuevaContrasena, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var rawToken = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(nuevaContrasena))
            return false;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);

        if (usuario == null || !usuario.Activo)
            return false;
        if (string.IsNullOrWhiteSpace(usuario.PasswordResetTokenHash) || !usuario.PasswordResetTokenExpiresAt.HasValue)
            return false;
        if (usuario.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
            return false;

        var tokenHash = ComputeSha256(rawToken);
        if (!string.Equals(usuario.PasswordResetTokenHash, tokenHash, StringComparison.Ordinal))
            return false;

        usuario.Contrasena = PasswordHelper.HashPassword(nuevaContrasena);
        usuario.PasswordResetTokenHash = null;
        usuario.PasswordResetTokenExpiresAt = null;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateSecureToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string ComputeSha256(string value)
    {
        var data = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}
