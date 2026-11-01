using DominoPontaDeQuina.Application;
using DominoPontaDeQuina.Api;
using DominoPontaDeQuina.Application.Identidade.Contracts;
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
    else options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(20), errorNumbersToAdd: null));
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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LimiteSolicitacaoTokenPorEmail>();
builder.Services.AddControllers(options => options.Filters.Add<RejeitarModeloInvalidoFilter>())
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
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
    options.AddPolicy("solicitacao-token", context => RateLimitPartition.GetSlidingWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "origem-desconhecida",
        _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 50,
            Window = TimeSpan.FromMinutes(10),
            SegmentsPerWindow = 10,
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
app.MapControllers();
app.Run();
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
