using System.Security.Claims;
using DominoPontaDeQuina.Application.Jogadores;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/partidas")]
[Authorize]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(RespostaErro), StatusCodes.Status500InternalServerError)]
public sealed class PartidasController(IJornadaPartidaService jornada) : ControllerBase
{
    [HttpGet]
    [DescricaoEndpoint("Listar partidas disponíveis", "Retorna partidas aguardando adversário, com exatamente um participante. A vaga é revalidada no ingresso.")]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaDisponivel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await jornada.ListarDisponiveisAsync(ct));

    [HttpPost]
    [DescricaoEndpoint("Criar partida", "Cria uma partida e registra o solicitante como primeiro participante. Retorna seu estado inicial.")]
    [ProducesResponseType(typeof(EstadoPartidaParticipante), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] CriarPartidaRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.CriarAsync(
            new(UsuarioId(), request.PontuacaoAlvo, Acesso(request.JogadorId), request.Webhook), ct));

    [HttpPost("{partidaId:guid}/entrar")]
    [DescricaoEndpoint("Entrar na partida", "Registra o segundo participante na partida aguardando início.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Entrar(Guid partidaId, [FromBody] IngressarRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.IngressarAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId), request.Webhook), ct));

    [HttpPost("{partidaId:guid}/iniciar")]
    [DescricaoEndpoint("Iniciar partida", "Inicia a partida quando as pré-condições estiverem atendidas e distribui as pedras.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Iniciar(Guid partidaId, [FromBody] AcaoRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.IniciarAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpGet("{partidaId:guid}")]
    [DescricaoEndpoint("Consultar meu estado", "Retorna a mão do solicitante, tabuleiro, turno, pontuação e versão. Não revela a mão do adversário.")]
    [ProducesResponseType(typeof(EstadoPartidaParticipante), StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar(Guid partidaId, [FromQuery, BindRequired] Guid jogadorId, CancellationToken ct) =>
        Mapear(await jornada.ConsultarEstadoProprioAsync(new(partidaId, UsuarioId(), Acesso(jogadorId)), ct));

    [HttpGet("{partidaId:guid}/atualizacoes")]
    [DescricaoEndpoint("Consultar atualizações da partida", "Consulta a visão privada do jogador. Informe versaoConhecida: retorna 204 quando nada mudou, ou 200 com o estado atual quando a versão é diferente. Consulte periodicamente enquanto a partida estiver aberta.")]
    [ProducesResponseType(typeof(EstadoPartidaParticipante), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Atualizacoes(Guid partidaId, [FromQuery, BindRequired] Guid jogadorId, [FromQuery] long? versaoConhecida, CancellationToken ct)
    {
        if (versaoConhecida is < 0) return BadRequest(new RespostaErro("ENTRADA_INVALIDA"));
        var resultado = await jornada.ConsultarEstadoProprioAsync(new(partidaId, UsuarioId(), Acesso(jogadorId)), ct);
        return resultado.Sucesso && resultado.Valor!.VersaoEstado == versaoConhecida
            ? NoContent() : Mapear(resultado);
    }

    [HttpGet("{partidaId:guid}/visualizar")]
    [DescricaoEndpoint("Observar partida", "Disponível a qualquer usuário autenticado. Retorna tabuleiro e resumos dos participantes sem mãos privadas.")]
    [ProducesResponseType(typeof(EstadoPartidaObservador), StatusCodes.Status200OK)]
    public async Task<IActionResult> Visualizar(Guid partidaId, CancellationToken ct) =>
        Mapear(await jornada.ConsultarObservacaoAsync(new(partidaId), ct));

    [HttpPost("{partidaId:guid}/jogar")]
    [DescricaoEndpoint("Jogar pedra", "Joga uma pedra da própria mão na ponta indicada: lado 0 = esquerda; 1 = direita. Valores da pedra entre 0 e 6.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Jogar(Guid partidaId, [FromBody] JogarRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.JogarAsync(
            new(partidaId, UsuarioId(), new(request.ValorA, request.ValorB), request.Lado,
                request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpPost("{partidaId:guid}/comprar")]
    [DescricaoEndpoint("Comprar pedra", "Compra uma única pedra quando não há encaixe. Se a pedra encaixar, mantém o turno; caso contrário, ou sem cava, registra passagem e troca o turno. O cliente não escolhe a pedra.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Comprar(Guid partidaId, [FromBody] AcaoRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.ComprarAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpPost("{partidaId:guid}/passar")]
    [DescricaoEndpoint("Passar a vez", "Permitido apenas quando não há cava nem pedra compatível na mão.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Passar(Guid partidaId, [FromBody] AcaoRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.PassarVezAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpPost("{partidaId:guid}/cancelar")]
    [DescricaoEndpoint("Cancelar partida", "Encerra uma partida aberta sem vencedor e preserva placar e histórico. Qualquer participante ativo pode cancelar. Use outra criação para começar de novo.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancelar(Guid partidaId, [FromBody] AcaoRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.CancelarAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpPost("{partidaId:guid}/reiniciar")]
    [DescricaoEndpoint("Reiniciar partida", "Encerra a partida informada e inicia outra com os mesmos participantes, placar zerado e novo partidaId. A partida anterior permanece consultável. Requer dois participantes; o solicitante precisa manter seu perfil ativo.", acao: true)]
    [ProducesResponseType(typeof(ConfirmacaoAcaoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reiniciar(Guid partidaId, [FromBody] AcaoRequest? request, CancellationToken ct) =>
        request is null ? BadRequest() : Mapear(await jornada.ReiniciarAsync(
            new(partidaId, UsuarioId(), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId)), ct));

    [HttpGet("{partidaId:guid}/reinicio")]
    [DescricaoEndpoint("Consultar partida reiniciada", "Retorna o estado da nova partida para um participante da anterior. Retorna 404 enquanto não houver reinício. Use após receber o encerramento da partida anterior.")]
    [ProducesResponseType(typeof(EstadoPartidaParticipante), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsultarReinicio(Guid partidaId, [FromQuery, BindRequired] Guid jogadorId, CancellationToken ct) =>
        Mapear(await jornada.ConsultarReinicioAsync(new(partidaId, UsuarioId(), Acesso(jogadorId)), ct));

    [HttpGet("{partidaId:guid}/resultado")]
    [DescricaoEndpoint("Consultar resultado e histórico", "Retorna situação, vencedor, pontuação e eventos permitidos ao participante solicitante.")]
    [ProducesResponseType(typeof(ResultadoHistoricoPartida), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resultado(Guid partidaId, [FromQuery, BindRequired] Guid jogadorId, CancellationToken ct) =>
        Mapear(await jornada.ConsultarResultadoHistoricoAsync(new(partidaId, UsuarioId(), Acesso(jogadorId)), ct));

    private Guid UsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private AcessoJogador Acesso(Guid jogadorId) => new(jogadorId, Request.Headers["X-Jogador-Ativacao"].ToString());
    private static string CodigoPublico(CodigoResultadoPartida codigo) => string.Concat(codigo.ToString().Select((caractere, indice) =>
        indice > 0 && char.IsUpper(caractere) ? $"_{char.ToUpperInvariant(caractere)}" : char.ToUpperInvariant(caractere).ToString()));

    private IActionResult Mapear<T>(ResultadoPartida<T> resultado) => resultado.Codigo switch
    {
        CodigoResultadoPartida.Sucesso => Ok(resultado.Valor),
        CodigoResultadoPartida.EntradaInvalida => BadRequest(new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo)),
        CodigoResultadoPartida.SemPermissao => StatusCode(StatusCodes.Status403Forbidden, new RespostaErro(CodigoPublico(resultado.Codigo))),
        CodigoResultadoPartida.PartidaIndisponivel => NotFound(new RespostaErro(CodigoPublico(resultado.Codigo))),
        CodigoResultadoPartida.ConflitoDeEstado => Conflict(new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo)),
        _ => StatusCode(StatusCodes.Status422UnprocessableEntity, new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo))
    };
}
