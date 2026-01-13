using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;

namespace BuscaYa.Services.IServices;

public interface IBusquedaService
{
    BuscarResponse BuscarProductos(BuscarRequest request);
    List<string> Sugerencias(string termino, int limite = 10);
}
