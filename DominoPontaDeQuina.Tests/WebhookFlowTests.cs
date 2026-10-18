using System.Net;
using DominoPontaDeQuina.Api.Webhooks;
using DominoPontaDeQuina.Application.Partidas;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Infrastructure.Persistence;
using DominoPontaDeQuina.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoPontaDeQuina.Tests;

public sealed class WebhookFlowTests
{
    [Fact]
    public async Task TransicoesPersistemOutboxPrivadaNaMesmaConfirmacao()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();
        var opcoes = new DbContextOptionsBuilder<DominoDbContext>().UseSqlite(conexao).Options;
        var a = new Jogador { Nome = "A" }; var b = new Jogador { Nome = "B" };
        Guid partidaId;
        await using (var db = new DominoDbContext(opcoes))
        {
            await db.Database.EnsureCreatedAsync();
            db.Jogadores.AddRange(a, b); await db.SaveChangesAsync();
            var jornada = new JornadaPartidaService(new EstadoPartidaRepository(db), new JogadorRepository(db));
            var criada = await jornada.CriarAsync(new(a.Id, 50, Webhook: "https://a.example/webhook"));
            partidaId = criada.Valor!.PartidaId;
            var ingresso = await jornada.IngressarAsync(new(partidaId, b.Id, 0, Guid.NewGuid(), Webhook: "https://b.example/webhook"));
            Assert.True(ingresso.Sucesso);
            var inicio = await jornada.IniciarAsync(new(partidaId, a.Id, 1, Guid.NewGuid()));
            Assert.True(inicio.Sucesso);
        }

        await using (var db = new DominoDbContext(opcoes))
        {
            var eventos = await db.EventosWebhook.AsNoTracking().Where(e => e.PartidaId == partidaId)
                .OrderBy(e => e.VersaoEstado).ThenBy(e => e.JogadorId).ToListAsync();
            Assert.Equal(5, eventos.Count);
            Assert.Equal(new long[] { 0, 1, 1, 2, 2 }, eventos.Select(e => e.VersaoEstado));
            Assert.All(eventos, evento =>
            {
                Assert.Contains("participacaoId", evento.ConteudoJson);
                Assert.Contains("proximaAcao", evento.ConteudoJson);
                Assert.DoesNotContain("webhook", evento.ConteudoJson, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("authorization", evento.ConteudoJson, StringComparison.OrdinalIgnoreCase);
            });
            var inicio = eventos.Where(e => e.VersaoEstado == 2).ToArray();
            Assert.Single(inicio, e => e.ConteudoJson.Contains("jogarAbertura", StringComparison.OrdinalIgnoreCase));
            Assert.Single(inicio, e => e.ConteudoJson.Contains("aguardar", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task RepositorioReservaEReagendaSemDuplicarReserva()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:"); await conexao.OpenAsync();
        var opcoes = new DbContextOptionsBuilder<DominoDbContext>().UseSqlite(conexao).Options;
        await using var db = new DominoDbContext(opcoes); await db.Database.EnsureCreatedAsync();
        var jogador = new Jogador { Nome = "A" }; var partida = new Partida { Status = "AguardandoInicio", PontuacaoAlvo = 50 };
        db.AddRange(jogador, partida); await db.SaveChangesAsync();
        var evento = new EventoWebhook { PartidaId = partida.Id, JogadorId = jogador.Id, VersaoEstado = 0,
            Tipo = "teste", Destino = "https://example.com/webhook", ConteudoJson = "{}" };
        db.EventosWebhook.Add(evento); await db.SaveChangesAsync();
        var repositorio = new EventoWebhookRepository(db); var agora = DateTime.UtcNow.AddSeconds(1);
        var reservado = await repositorio.ReservarProximoAsync(agora, TimeSpan.FromMinutes(1));
        Assert.Equal(evento.Id, reservado!.Id); Assert.Equal(1, reservado.Tentativas);
        Assert.Null(await repositorio.ReservarProximoAsync(agora, TimeSpan.FromMinutes(1)));
        await repositorio.MarcarFalhaAsync(evento.Id, agora.AddMinutes(2), "HTTP_500", false);
        Assert.Null(await repositorio.ReservarProximoAsync(agora.AddMinutes(1), TimeSpan.FromMinutes(1)));
        Assert.Equal(evento.Id, (await repositorio.ReservarProximoAsync(agora.AddMinutes(3), TimeSpan.FromMinutes(1)))!.Id);
    }

    [Fact]
    public async Task SenderBloqueiaDestinoPrivadoAntesDoPost()
    {
        var handler = new CapturaHandler();
        var options = Options.Create(new WebhookOptions { AssinaturaSegredo = new string('s', 32) });
        var ambiente = new AmbienteTeste { EnvironmentName = Environments.Production };
        var sender = new WebhookSender(new HttpClient(handler), new WebhookDestinoValidator(ambiente, options), options);
        var resultado = await sender.EnviarAsync(new EventoWebhook { Destino = "https://127.0.0.1/callback", ConteudoJson = "{}" }, default);
        Assert.False(resultado.Sucesso); Assert.Equal("DESTINO_NAO_PERMITIDO", resultado.Falha); Assert.Equal(0, handler.Chamadas);
    }

    [Fact]
    public async Task WorkerReagendaHttp500EConfirmaSomenteApos2xx()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:"); await conexao.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<DominoDbContext>(o => o.UseSqlite(conexao));
        services.AddScoped<DominoPontaDeQuina.Domain.Repositories.IEventoWebhookRepository, EventoWebhookRepository>();
        var opcoes = Options.Create(new WebhookOptions
        {
            AssinaturaSegredo = new string('x', 32), PermitirLoopbackEmDesenvolvimento = true,
            TimeoutSegundos = 2, MaximoTentativas = 3
        });
        services.AddSingleton<IOptions<WebhookOptions>>(opcoes);
        services.AddSingleton<IHostEnvironment>(new AmbienteTeste { EnvironmentName = Environments.Development });
        services.AddSingleton<WebhookDestinoValidator>();
        var handler = new CapturaHandler { Status = HttpStatusCode.InternalServerError };
        services.AddScoped(provider => new WebhookSender(new HttpClient(handler),
            provider.GetRequiredService<WebhookDestinoValidator>(), opcoes));
        await using var provider = services.BuildServiceProvider();
        Guid eventoId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DominoDbContext>(); await db.Database.EnsureCreatedAsync();
            var jogador = new Jogador { Nome = "A" }; var partida = new Partida { Status = "AguardandoInicio", PontuacaoAlvo = 50 };
            db.AddRange(jogador, partida); await db.SaveChangesAsync();
            var evento = new EventoWebhook { PartidaId = partida.Id, JogadorId = jogador.Id, Tipo = "teste",
                Destino = "https://localhost/callback", ConteudoJson = "{}", ProximaTentativaEmUtc = DateTime.UtcNow.AddSeconds(-1) };
            db.EventosWebhook.Add(evento); await db.SaveChangesAsync(); eventoId = evento.Id;
        }
        var worker = new WebhookOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), opcoes,
            NullLogger<WebhookOutboxWorker>.Instance);
        Assert.True(await worker.ProcessarUmAsync(default));
        await using (var scope = provider.CreateAsyncScope())
        {
            var falha = await scope.ServiceProvider.GetRequiredService<DominoDbContext>().EventosWebhook.AsNoTracking().SingleAsync(e => e.Id == eventoId);
            Assert.Equal(1, falha.Tentativas); Assert.Equal("HTTP_500", falha.UltimaFalha); Assert.Null(falha.EntregueEmUtc);
            await scope.ServiceProvider.GetRequiredService<DominoDbContext>().EventosWebhook.Where(e => e.Id == eventoId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ProximaTentativaEmUtc, DateTime.UtcNow.AddSeconds(-1)));
        }
        handler.Status = HttpStatusCode.NoContent;
        Assert.True(await worker.ProcessarUmAsync(default));
        await using (var scope = provider.CreateAsyncScope())
        {
            var entregue = await scope.ServiceProvider.GetRequiredService<DominoDbContext>().EventosWebhook.AsNoTracking().SingleAsync(e => e.Id == eventoId);
            Assert.Equal(2, entregue.Tentativas); Assert.NotNull(entregue.EntregueEmUtc); Assert.Null(entregue.UltimaFalha);
        }
        Assert.Equal(2, handler.Chamadas);
        Assert.All(handler.Autorizacoes, valor => Assert.Null(valor));
        Assert.All(handler.Assinaturas, valor => Assert.StartsWith("sha256=", valor));
    }

    private sealed class CapturaHandler : HttpMessageHandler
    {
        public int Chamadas { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public List<System.Net.Http.Headers.AuthenticationHeaderValue?> Autorizacoes { get; } = [];
        public List<string> Assinaturas { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++; Autorizacoes.Add(request.Headers.Authorization);
            Assinaturas.Add(request.Headers.GetValues("X-Domino-Assinatura").Single());
            return Task.FromResult(new HttpResponseMessage(Status));
        }
    }

    private sealed class AmbienteTeste : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
