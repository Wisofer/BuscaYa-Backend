namespace BuscaYa.Models.Entities;

public class ProductoImagen
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }

    public virtual Producto Producto { get; set; } = null!;
}
