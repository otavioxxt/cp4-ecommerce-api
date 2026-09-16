using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

namespace CP4.ECommerce.API.Controllers;

[Route("api/health")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthService;

    public HealthController(HealthCheckService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>Liveness probe</summary>
    [HttpGet("live")]
    [SwaggerOperation(
        Summary = "Liveness probe",
        Description = """
        Verifica apenas se o processo da API esta respondendo, **sem tocar em dependencias externas**.
        E o sinal que o orquestrador usa para decidir se o container precisa ser reiniciado.

        * **200 (OK):** aplicacao saudavel.
        * **503 (Service Unavailable):** aplicacao travada ou degradada.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Aplicacao saudavel")]
    [SwaggerResponse(statusCode: 503, description: "Aplicacao indisponivel")]
    public async Task<IActionResult> Live(CancellationToken ct)
    {
        var report = await _healthService.CheckHealthAsync(r => r.Tags.Contains("live"), ct);

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(result)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }

    /// <summary>Readiness probe</summary>
    [HttpGet("db")]
    [SwaggerOperation(
        Summary = "Readiness probe",
        Description = """
        Verifica a conectividade com o **banco Oracle**.
        E o sinal que o load balancer usa para decidir se a instancia pode receber trafego.

        * **200 (OK):** banco acessivel, instancia pronta.
        * **503 (Service Unavailable):** banco fora do ar - a instancia sai do pool.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Banco de dados acessivel")]
    [SwaggerResponse(statusCode: 503, description: "Banco de dados indisponivel")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var report = await _healthService.CheckHealthAsync(r => r.Tags.Contains("db"), ct);

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(result)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
