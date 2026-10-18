using System.Security.Claims;
using DominoPontaDeQuina.Application.Jogadores;

namespace DominoPontaDeQuina.Api;

internal static class EndpointsJogadores
{
    public static void MapearJogadores(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/jogadores").RequireAuthorization().WithTags("Jogadores");
        grupo.MapGet("", async (ClaimsPrincipal usuario, IPerfilJogadorService perfis, CancellationToken ct) =>
            Results.Ok(await perfis.ListarAsync(UsuarioId(usuario), ct)))
            .WithSummary("Listar meus jogadores").WithDescription("Lista somente os perfis da conta autenticada, com o estado de ativação e sem segredos.")
            .Produces<IReadOnlyList<PerfilJogador>>().Produces<RespostaErro>(401);
        grupo.MapPost("", async (CriarJogadorRequest request, ClaimsPrincipal usuario, IPerfilJogadorService perfis, CancellationToken ct) =>
            Mapear(await perfis.CriarAsync(UsuarioId(usuario), request.Nome, ct)))
            .WithSummary("Criar jogador").WithDescription("Cria um perfil da conta autenticada. Nome entre 1 e 150 caracteres; a API atribui o identificador.")
            .Produces<PerfilJogador>().Produces<RespostaErro>(400).Produces<RespostaErro>(401).Produces<RespostaErro>(403);
        grupo.MapPost("/{jogadorId:guid}/ativar-cliente", async (Guid jogadorId, AtivarJogadorRequest request,
            ClaimsPrincipal usuario, IPerfilJogadorService perfis, CancellationToken ct) =>
            Mapear(await perfis.AtivarAsync(UsuarioId(usuario), jogadorId, request.ChaveAtivacao, ct)))
            .WithSummary("Ativar jogador neste cliente")
            .WithDescription("O cliente gera e guarda 32 bytes aleatórios em 64 caracteres hexadecimais antes do envio. A mesma chave permite repetir uma ativação; outra chave recebe JOGADOR_JA_ATIVO. Não há expiração ou tomada automática do jogador. Envie essa chave em X-Jogador-Ativacao nas operações do jogador.")
            .Produces<PerfilJogador>().Produces<RespostaErro>(400).Produces<RespostaErro>(401).Produces<RespostaErro>(403).Produces<RespostaErro>(409);
        grupo.MapDelete("/{jogadorId:guid}/desativar-cliente", async (Guid jogadorId, HttpRequest request,
            ClaimsPrincipal usuario, IPerfilJogadorService perfis, CancellationToken ct) =>
            Mapear(await perfis.LiberarAsync(UsuarioId(usuario), jogadorId, request.Headers["X-Jogador-Ativacao"].FirstOrDefault(), ct)))
            .WithSummary("Liberar jogador deste cliente")
            .WithDescription("Libera explicitamente a ativação usando a chave atual. Não há liberação por fechamento, desconexão ou tempo; após a liberação outro cliente pode ativar o jogador.")
            .Produces<PerfilJogador>().Produces<RespostaErro>(400).Produces<RespostaErro>(401).Produces<RespostaErro>(403);
    }

    private static Guid UsuarioId(ClaimsPrincipal usuario) => Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Mapear(ResultadoPerfil<PerfilJogador> resultado) => resultado.Codigo switch
    {
        CodigoPerfil.Sucesso => Results.Ok(resultado.Valor),
        CodigoPerfil.EntradaInvalida => Results.BadRequest(new RespostaErro("ENTRADA_INVALIDA")),
        CodigoPerfil.SemPermissao => Results.Json(new RespostaErro("SEM_PERMISSAO"), statusCode: 403),
        CodigoPerfil.JogadorJaAtivo => Results.Conflict(new RespostaErro("JOGADOR_JA_ATIVO")),
        _ => Results.StatusCode(500)
    };
}

public sealed record CriarJogadorRequest(string? Nome);
public sealed record AtivarJogadorRequest(string? ChaveAtivacao);
