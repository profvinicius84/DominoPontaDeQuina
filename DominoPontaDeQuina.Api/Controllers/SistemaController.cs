using DominoPontaDeQuina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SistemaController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Inicio() => Ok(new { Servico = "Domino Ponta de Quina API", Documentacao = "/swagger" });

    [HttpGet("/health")]
    public async Task<IActionResult> Health([FromServices] DominoDbContext db, CancellationToken ct)
    {
        try
        {
            await db.Usuarios.AsNoTracking().AnyAsync(ct);
            return Ok();
        }
        catch (Exception) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
    }
}
