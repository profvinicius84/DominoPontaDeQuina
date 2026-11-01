using System.Security.Claims;
using DominoPontaDeQuina.Application.Jogadores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/jogadores")]
[Authorize]
public sealed class JogadoresController(IPerfilJogadorService perfis) : ControllerBase
{
    [HttpGet]
    [DescricaoEndpoint("Listar meus jogadores", "Lista somente os perfis da conta autenticada, com o estado de ativação e sem segredos.")]
    [ProducesResponseType(typeof(IReadOnlyList<PerfilJogador>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await perfis.ListarAsync(UsuarioId(), ct));

    [HttpPost]
    [DescricaoEndpoint("Criar jogador", "Cria um perfil da conta autenticada. Nome entre 1 e 150 caracteres; a API atribui o identificador.")]
    [ProducesResponseType(typeof(PerfilJogador), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Criar([FromBody] CriarJogadorRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await perfis.CriarAsync(UsuarioId(), request.Nome, ct));

    [HttpPost("{jogadorId:guid}/ativar-cliente")]
    [DescricaoEndpoint("Ativar jogador neste cliente", "O cliente gera e guarda 32 bytes aleatórios em 64 caracteres hexadecimais antes do envio. A mesma chave permite repetir uma ativação; outra chave recebe JOGADOR_JA_ATIVO. Não há expiração ou tomada automática do jogador. Envie essa chave em X-Jogador-Ativacao nas operações do jogador.")]
    [ProducesResponseType(typeof(PerfilJogador), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ativar(Guid jogadorId, [FromBody] AtivarJogadorRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await perfis.AtivarAsync(UsuarioId(), jogadorId, request.ChaveAtivacao, ct));

    [HttpDelete("{jogadorId:guid}/desativar-cliente")]
    [DescricaoEndpoint("Liberar jogador deste cliente", "Libera explicitamente a ativação usando a chave atual. Não há liberação por fechamento, desconexão ou tempo; após a liberação outro cliente pode ativar o jogador.")]
    [ProducesResponseType(typeof(PerfilJogador), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Desativar(Guid jogadorId, CancellationToken ct) =>
        Mapear(await perfis.LiberarAsync(UsuarioId(), jogadorId, Request.Headers["X-Jogador-Ativacao"].FirstOrDefault(), ct));

    private Guid UsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult Mapear(ResultadoPerfil<PerfilJogador> resultado) => resultado.Codigo switch
    {
        CodigoPerfil.Sucesso => Ok(resultado.Valor),
        CodigoPerfil.EntradaInvalida => BadRequest(new RespostaErro("ENTRADA_INVALIDA")),
        CodigoPerfil.SemPermissao => StatusCode(StatusCodes.Status403Forbidden, new RespostaErro("SEM_PERMISSAO")),
        CodigoPerfil.JogadorJaAtivo => Conflict(new RespostaErro("JOGADOR_JA_ATIVO")),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
