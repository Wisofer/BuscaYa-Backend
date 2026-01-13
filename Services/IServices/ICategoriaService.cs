using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface ICategoriaService
{
    List<Categoria> ObtenerTodas();
    List<Categoria> ObtenerActivas();
    Categoria? ObtenerPorId(int id);
    Categoria Crear(string nombre, string? icono = null);
    bool Actualizar(int id, string nombre, string? icono = null);
    bool Activar(int id);
    bool Desactivar(int id);
}
