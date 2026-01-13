using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;

namespace BuscaYa.Services.IServices;

public interface IProductoService
{
    List<Producto> ObtenerPorTienda(int tiendaId);
    Producto? ObtenerPorId(int id);
    Producto Crear(int tiendaId, CrearProductoRequest request);
    bool Actualizar(int id, ActualizarProductoRequest request);
    bool Eliminar(int id);
    bool VerificarPerteneceATienda(int productoId, int tiendaId);
}
