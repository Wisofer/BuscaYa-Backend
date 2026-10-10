using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;

namespace BuscaYa.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RatesController : ControllerBase
{
    private readonly IVenezuelaExchangeRateService _rateService;

    public RatesController(IVenezuelaExchangeRateService rateService)
    {
        _rateService = rateService;
    }

    /// <summary>
    /// Consulta las tasas oficiales de cambio para Venezuela (BCV y Paralelo).
    /// GET /api/rates/venezuela?force=true
    /// </summary>
    [HttpGet("venezuela")]
    public async Task<IActionResult> GetVenezuelaRates(
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var rates = await _rateService.GetLatestRatesAsync(force, cancellationToken);
        return Ok(rates);
    }
}
