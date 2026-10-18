using DominoPontaDeQuina.Application;
using DominoPontaDeQuina.Application.Jogadores;
using DominoPontaDeQuina.Api;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using DominoPontaDeQuina.Infrastructure;
using DominoPontaDeQuina.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;
using DominoPontaDeQuina.Api.Webhooks;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);
var usarSqlite = builder.Configuration.GetValue<bool>("Domino:UsarSqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!usarSqlite && string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection é obrigatória para SQL Server.");
connectionString ??= "Data Source=domino-api-dev.db";
var diretorioChaves = builder.Configuration["Domino:DataProtectionDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, ".data-protection");
Directory.CreateDirectory(diretorioChaves);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(diretorioChaves));
builder.Services.AddDominoApplication();
builder.Services.AddDominoInfrastructure(options =>
{
    if (usarSqlite) options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString);
});
var sendGridHabilitado = builder.Configuration.GetValue<bool>("Email:SendGrid:Habilitado");
builder.Services.AddOptions<EntregaTokenEmailSendGridOptions>()
    .Bind(builder.Configuration.GetSection("Email:SendGrid"))
    .Validate(opcoes => !opcoes.Habilitado ||
        (!string.IsNullOrWhiteSpace(opcoes.ApiKey) && !string.IsNullOrWhiteSpace(opcoes.Remetente)),
        "Email:SendGrid requer ApiKey e Remetente quando habilitado.")
    .ValidateOnStart();
if (sendGridHabilitado)
    builder.Services.AddHttpClient<IEntregaTokenEmail, EntregaTokenEmailSendGrid>(cliente =>
        cliente.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddAuthentication(TokenAuthenticationHandler.TokenScheme)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(TokenAuthenticationHandler.TokenScheme, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDocumentacaoSwagger();
builder.Services.AddOptions<WebhookOptions>().Bind(builder.Configuration.GetSection(WebhookOptions.Secao))
    .Validate(o => o.AssinaturaSegredo.Length >= 32, "Webhook:AssinaturaSegredo deve possuir ao menos 32 caracteres.")
    .Validate(o => o.TimeoutSegundos is >= 1 and <= 30 && o.MaximoTentativas is >= 1 and <= 20,
        "Timeout e número de tentativas do webhook estão fora dos limites permitidos.")
    .ValidateOnStart();
builder.Services.AddSingleton<WebhookDestinoValidator>();
builder.Services.AddHttpClient<WebhookSender>((provider, client) =>
{
    var opcoes = provider.GetRequiredService<IOptions<WebhookOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opcoes.TimeoutSegundos, 1, 30));
}).ConfigurePrimaryHttpMessageHandler(provider => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    UseCookies = false,
    UseProxy = false,
    ConnectCallback = async (context, ct) =>
    {
        var validador = provider.GetRequiredService<WebhookDestinoValidator>();
        var enderecos = await validador.ResolverPermitidosAsync(context.DnsEndPoint.Host, ct);
        Exception? ultimaFalha = null;
        foreach (var endereco in enderecos)
        {
            var socket = new Socket(endereco.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(endereco, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                ultimaFalha = ex;
                socket.Dispose();
            }
        }
        throw new HttpRequestException("Destino do webhook não pôde ser conectado com endereço permitido.", ultimaFalha);
    }
});
builder.Services.AddHostedService<WebhookOutboxWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
        await Results.Json(new RespostaErro("LIMITE_EXCEDIDO"), statusCode: StatusCodes.Status429TooManyRequests)
            .ExecuteAsync(context.HttpContext);
    options.AddPolicy("solicitacao-token", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "origem-desconhecida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var app = builder.Build();
if (usarSqlite)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DominoPontaDeQuina.Infrastructure.Persistence.DominoDbContext>();
    await db.Database.EnsureCreatedAsync();
}
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Json(new RespostaErro("ERRO_INTERNO"), statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context)));
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Ok(new { Servico = "Domino Ponta de Quina API", Documentacao = "/swagger" }))
    .ExcludeFromDescription();
app.MapGet("/health", async (DominoPontaDeQuina.Infrastructure.Persistence.DominoDbContext db, CancellationToken ct) =>
{
    try
    {
        await db.Usuarios.AsNoTracking().AnyAsync(ct);
        return Results.Ok();
    }
    catch (Exception) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
}).ExcludeFromDescription();
app.MapPost("/api/identidade/token", async (SolicitarTokenRequest? request, IIdentidadeAcessoService identidade, CancellationToken cancellationToken) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new RespostaErro("ENTRADA_INVALIDA"));
    await identidade.SolicitarTokenPorEmailAsync(request.Email, cancellationToken);
    return Results.Accepted();
}).RequireRateLimiting("solicitacao-token")
    .WithTags("Identidade").WithSummary("Solicitar token por e-mail")
    .WithDescription("Recebe somente e-mail. Responde 202 uniforme e sem token no corpo; entrega no e-mail autorizado. O token é reutilizável até revogação. Limite: cinco solicitações por origem a cada dez minutos.")
    .Produces(202).Produces<RespostaErro>(400).Produces<RespostaErro>(429).Produces<RespostaErro>(500);
app.MapGet("/api/identidade/sessao", (ClaimsPrincipal user) => Results.Ok(new SessaoResponse(user.FindFirstValue(ClaimTypes.NameIdentifier)!)))
    .RequireAuthorization().WithTags("Identidade").WithSummary("Consultar sessão autenticada")
    .WithDescription("Retorna o identificador do usuário associado ao token Bearer apresentado. O token pode ser reutilizado em diversos acessos e clientes até revogação, sem expiração automática.")
    .Produces<SessaoResponse>().Produces<RespostaErro>(401).Produces<RespostaErro>(500);
app.MapPost("/api/identidade/tokens/revogacao", async (ClaimsPrincipal user, HttpRequest request, IIdentidadeAcessoService identidade, CancellationToken cancellationToken) =>
{
    var token = request.Headers.Authorization.ToString();
    var revogado = await identidade.RevogarTokenAsync(UsuarioId(user), token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token[7..] : string.Empty, cancellationToken);
    return revogado ? Results.NoContent() : Results.BadRequest(new RespostaErro("ENTRADA_INVALIDA"));
}).RequireAuthorization().WithTags("Identidade").WithSummary("Revogar o token atual")
    .WithDescription("Revoga o token Bearer do cabeçalho Authorization. Após a revogação, chamadas com esse token recebem 401.")
    .Produces(204).Produces<RespostaErro>(400).Produces<RespostaErro>(401).Produces<RespostaErro>(500);

app.MapearJogadores();
var partidas = app.MapGroup("/api/partidas").RequireAuthorization().WithTags("Partidas");
partidas.MapGet("", async (IJornadaPartidaService jornada, CancellationToken ct) => Results.Ok(await jornada.ListarDisponiveisAsync(ct)))
    .WithSummary("Listar partidas disponíveis").WithDescription("Retorna partidas aguardando adversário, com exatamente um participante. A vaga é revalidada no ingresso.")
    .Produces<IReadOnlyList<PartidaDisponivel>>().Produces<RespostaErro>(401);
partidas.MapPost("", async (CriarPartidaRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.CriarAsync(new(UsuarioId(user), request.PontuacaoAlvo, Acesso(request.JogadorId, http), request.Webhook), ct)))
    .DocumentarPartida<EstadoPartidaParticipante>("Criar partida", "Cria uma partida e registra o solicitante como primeiro participante. Retorna seu estado inicial.");
partidas.MapPost("/{partidaId:guid}/entrar", async (Guid partidaId, IngressarRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.IngressarAsync(new(partidaId, UsuarioId(user), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId, http), request.Webhook), ct)))
    .DocumentarPartida<ConfirmacaoAcaoPartida>("Entrar na partida", "Registra o segundo participante na partida aguardando início.", acao: true);
partidas.MapPost("/{partidaId:guid}/iniciar", async (Guid partidaId, AcaoRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.IniciarAsync(new(partidaId, UsuarioId(user), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId, http)), ct)))
    .DocumentarPartida<ConfirmacaoAcaoPartida>("Iniciar partida", "Inicia a partida quando as pré-condições estiverem atendidas e distribui as pedras.", acao: true);
partidas.MapGet("/{partidaId:guid}", async (Guid partidaId, Guid jogadorId, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.ConsultarEstadoProprioAsync(new(partidaId, UsuarioId(user), Acesso(jogadorId, http)), ct)))
    .DocumentarPartida<EstadoPartidaParticipante>("Consultar meu estado", "Retorna a mão do solicitante, tabuleiro, turno, pontuação e versão. Não revela a mão do adversário.");
partidas.MapGet("/{partidaId:guid}/atualizacoes", async (Guid partidaId, Guid jogadorId, long? versaoConhecida, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
{
    if (versaoConhecida is < 0) return Results.BadRequest(new RespostaErro("ENTRADA_INVALIDA"));
    var resultado = await jornada.ConsultarEstadoProprioAsync(new(partidaId, UsuarioId(user), Acesso(jogadorId, http)), ct);
    return resultado.Sucesso && resultado.Valor!.VersaoEstado == versaoConhecida
        ? Results.NoContent() : Mapear(resultado);
}).DocumentarPartida<EstadoPartidaParticipante>("Consultar atualizações da partida",
    "Consulta a visão privada do jogador. Informe versaoConhecida: retorna 204 quando nada mudou, ou 200 com o estado atual quando a versão é diferente. Consulte periodicamente enquanto a partida estiver aberta.")
    .Produces(204);
partidas.MapGet("/{partidaId:guid}/visualizar", async (Guid partidaId, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.ConsultarObservacaoAsync(new(partidaId), ct)))
    .DocumentarPartida<EstadoPartidaObservador>("Observar partida", "Disponível a qualquer usuário autenticado. Retorna tabuleiro e resumos dos participantes sem mãos privadas.");
partidas.MapPost("/{partidaId:guid}/jogar", async (Guid partidaId, JogarRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.JogarAsync(new(partidaId, UsuarioId(user), new(request.ValorA, request.ValorB), request.Lado, request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId, http)), ct)))
    .DocumentarPartida<ConfirmacaoAcaoPartida>("Jogar pedra", "Joga uma pedra da própria mão na ponta indicada: lado 0 = esquerda; 1 = direita. Valores da pedra entre 0 e 6.", acao: true);
partidas.MapPost("/{partidaId:guid}/comprar", async (Guid partidaId, AcaoRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.ComprarAsync(new(partidaId, UsuarioId(user), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId, http)), ct)))
    .DocumentarPartida<ConfirmacaoAcaoPartida>("Comprar pedra", "Compra uma única pedra quando não há encaixe. Se a pedra encaixar, mantém o turno; caso contrário, ou sem cava, registra passagem e troca o turno. O cliente não escolhe a pedra.", acao: true);
partidas.MapPost("/{partidaId:guid}/passar", async (Guid partidaId, AcaoRequest request, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.PassarVezAsync(new(partidaId, UsuarioId(user), request.VersaoEsperada, request.ChaveIdempotencia, Acesso(request.JogadorId, http)), ct)))
    .DocumentarPartida<ConfirmacaoAcaoPartida>("Passar a vez", "Permitido apenas quando não há cava nem pedra compatível na mão.", acao: true);
partidas.MapGet("/{partidaId:guid}/resultado", async (Guid partidaId, Guid jogadorId, ClaimsPrincipal user, HttpRequest http, IJornadaPartidaService jornada, CancellationToken ct) =>
    Mapear(await jornada.ConsultarResultadoHistoricoAsync(new(partidaId, UsuarioId(user), Acesso(jogadorId, http)), ct)))
    .DocumentarPartida<ResultadoHistoricoPartida>("Consultar resultado e histórico", "Retorna situação, vencedor, pontuação e eventos permitidos ao participante solicitante.");
static AcessoJogador Acesso(Guid jogadorId, HttpRequest http) => new(jogadorId, http.Headers["X-Jogador-Ativacao"].ToString());
static Guid UsuarioId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
static string CodigoPublico(CodigoResultadoPartida codigo) => string.Concat(codigo.ToString().Select((caractere, indice) =>
    indice > 0 && char.IsUpper(caractere) ? $"_{char.ToUpperInvariant(caractere)}" : char.ToUpperInvariant(caractere).ToString()));
static IResult Mapear<T>(ResultadoPartida<T> resultado) => resultado.Codigo switch
{
    CodigoResultadoPartida.Sucesso => Results.Ok(resultado.Valor),
    CodigoResultadoPartida.EntradaInvalida => Results.BadRequest(new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo)),
    CodigoResultadoPartida.SemPermissao => Results.Json(new RespostaErro(CodigoPublico(resultado.Codigo)), statusCode: StatusCodes.Status403Forbidden),
    CodigoResultadoPartida.PartidaIndisponivel => Results.NotFound(new RespostaErro(CodigoPublico(resultado.Codigo))),
    CodigoResultadoPartida.ConflitoDeEstado => Results.Conflict(new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo)),
    _ => Results.UnprocessableEntity(new RespostaErro(CodigoPublico(resultado.Codigo), resultado.EstadoAutoritativo))
};
app.Run();
public sealed record SolicitarTokenRequest(string Email);
public sealed record CriarPartidaRequest(int PontuacaoAlvo, Guid JogadorId, string? Webhook);
public sealed record IngressarRequest(long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId, string? Webhook);
public sealed record AcaoRequest(long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId);
public sealed record JogarRequest(int ValorA, int ValorB, LadoJogavel Lado, long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId);
public sealed record RespostaErro(string Codigo, EstadoPartidaParticipante? EstadoAutoritativo = null);
public sealed record SessaoResponse(string UsuarioId);
public sealed class TokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IIdentidadeAcessoService identidade) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TokenScheme = "DominoToken";
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var usuarioId = await identidade.ValidarTokenAsync(authorization[7..], Context.RequestAborted);
        if (usuarioId is null) return AuthenticateResult.Fail("Token inválido ou revogado.");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString())], TokenScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), TokenScheme));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Results.Json(new RespostaErro("NAO_AUTENTICADO"), statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(Context);
}

public partial class Program;
