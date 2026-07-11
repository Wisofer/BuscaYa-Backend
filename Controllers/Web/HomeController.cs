using Microsoft.AspNetCore.Mvc;
using BuscaYa.Utils;

namespace BuscaYa.Controllers.Web;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var rol = User.FindFirst("Rol")?.Value;
            if (rol == SD.RolAdministrador)
                return Redirect("/admin/dashboard");
        }

        return Redirect("/login");
    }
}
