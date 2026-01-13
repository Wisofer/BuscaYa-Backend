namespace BuscaYa.Utils;

public static class SD
{
    // Roles de usuario
    public const string RolAdministrador = "Administrador";
    public const string RolTiendaOwner = "TiendaOwner";
    public const string RolCliente = "Cliente";

    // Planes de Tienda
    public const string PlanFree = "Free";
    public const string PlanPro = "Pro";

    // Tipos de Evento (Analytics)
    public const string EventoVistaTienda = "VistaTienda";
    public const string EventoClickWhatsApp = "ClickWhatsApp";
    public const string EventoClickLlamar = "ClickLlamar";
    public const string EventoClickDireccion = "ClickDireccion";
    public const string EventoBusqueda = "Busqueda";

    // Monedas
    public const string MonedaCordoba = "C$";
    public const string MonedaDolar = "$";

    // Límites
    public const int LimiteProductosPlanFree = 10;
    public const int TamanoPaginaDefault = 20;
    public const int TamanoPaginaMaximo = 50;
    public const double RadioBusquedaDefault = 5.0; // km
    public const double RadioBusquedaMaximo = 50.0; // km
}
