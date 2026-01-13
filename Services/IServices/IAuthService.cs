using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IAuthService
{
    Usuario? ValidarUsuario(string nombreUsuario, string contrasena);
    Usuario? ObtenerUsuarioPorId(int id);
    bool EsAdministrador(Usuario usuario);
    bool EsUsuarioNormal(Usuario usuario);
}

