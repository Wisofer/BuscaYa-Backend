using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IAuthService
{
    Usuario? ValidarUsuario(string nombreUsuario, string contrasena);
    Usuario? ObtenerUsuarioPorId(int id);
    bool EsAdministrador(Usuario usuario);
    bool EsUsuarioNormal(Usuario usuario);
    
    // Registro
    Usuario? RegistrarCliente(string nombreUsuario, string contrasena, string nombreCompleto, string? telefono, string? email);
    bool ExisteNombreUsuario(string nombreUsuario);
    
    // Convertir Cliente a TiendaOwner
    Usuario? ConvertirClienteATienda(int usuarioId, string nombreTienda, string? descripcionTienda, 
        string? telefonoTienda, string whatsAppTienda, string? emailTienda,
        string direccionTienda, decimal latitud, decimal longitud, string ciudad, string departamento,
        TimeSpan? horarioApertura, TimeSpan? horarioCierre, string? diasAtencion, string? logoTienda, string? fotoTienda);
    
    // Actualizar perfil de usuario
    bool ActualizarPerfil(int usuarioId, string nombreCompleto, string? telefono, string? email, string? fotoPerfilUrl = null);
}

