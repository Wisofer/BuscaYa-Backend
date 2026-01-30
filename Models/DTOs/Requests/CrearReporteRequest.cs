using System.ComponentModel.DataAnnotations;

namespace BuscaYa.Models.DTOs.Requests;

public class CrearReporteRequest
{
    [Required(ErrorMessage = "El tipo es requerido")]
    [RegularExpression("^(producto|tienda)$", ErrorMessage = "El tipo debe ser 'producto' o 'tienda'")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ID del recurso es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del recurso debe ser mayor a 0")]
    public int RecursoId { get; set; }

    [Required(ErrorMessage = "La razón es requerida")]
    public string Razon { get; set; } = string.Empty;

    public string? Detalle { get; set; }
}
