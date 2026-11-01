using System.Security.Claims;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/identidade")]
[Authorize]
public sealed class IdentidadeController(IIdentidadeAcessoService identidade, LimiteSolicitacaoTokenPorEmail limitePorEmail) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting("solicitacao-token")]
    [DescricaoEndpoint("Solicitar token por e-mail", "Recebe somente e-mail. Retorna 400 com false para endereço inválido ou fora de @fiap.com.br. Endereços FIAP válidos recebem 202 sem corpo, sem revelar se a conta já existia. O token é reutilizável até revogação. Limite em dez minutos: cinco solicitações por e-mail e 50 por IP de origem.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SolicitarToken([FromBody] SolicitarTokenRequest? request, CancellationToken ct)
    {
        string emailNormalizado;
        try { emailNormalizado = Usuario.NormalizarEmailFiap(request?.Email ?? string.Empty); }
        catch (ArgumentException) { return BadRequest(false); }
        if (!limitePorEmail.TentarRegistrar(emailNormalizado))
            return StatusCode(StatusCodes.Status429TooManyRequests, new RespostaErro("LIMITE_EXCEDIDO"));
        await identidade.SolicitarTokenPorEmailAsync(emailNormalizado, ct);
        return Accepted();
    }

    [HttpGet("sessao")]
    [DescricaoEndpoint("Consultar sessão autenticada", "Retorna o identificador do usuário associado ao token Bearer apresentado. O token pode ser reutilizado em diversos acessos e clientes até revogação, sem expiração automática.")]
    [ProducesResponseType(typeof(SessaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status500InternalServerError)]
    public IActionResult Sessao() => Ok(new SessaoResponse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

    [HttpPost("tokens/revogacao")]
    [DescricaoEndpoint("Revogar o token atual", "Revoga o token Bearer do cabeçalho Authorization. Após a revogação, chamadas com esse token recebem 401.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevogarToken(CancellationToken ct)
    {
        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..] : string.Empty;
        var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var revogado = await identidade.RevogarTokenAsync(usuarioId, token, ct);
        return revogado ? NoContent() : BadRequest(new RespostaErro("ENTRADA_INVALIDA"));
    }
}
