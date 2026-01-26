using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Utils;
using System.Security.Claims;

namespace BuscaYa.Controllers.Web;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Si el usuario está autenticado, redirigir según su rol
        if (User.Identity?.IsAuthenticated == true)
        {
            var rol = User.FindFirst("Rol")?.Value;
            return rol switch
            {
                SD.RolAdministrador => Redirect("/admin/dashboard"),
                SD.RolTiendaOwner => Redirect("/"),
                SD.RolCliente => Redirect("/"),
                _ => Redirect("/login")
            };
        }

        // Si no está autenticado, redirigir al login
        return Redirect("/login");
    }
}
