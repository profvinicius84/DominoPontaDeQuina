using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DominoPontaDeQuina.Api.Webhooks;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Tests;

public sealed class JornadaWebhookPontaAPontaTests
{
    [Fact]
    public async Task DoisClientesMesmoToken_RecebemEstadosPrivadosPorHttpsAteEncerramento()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback);
        req.CertificateExtensions.Add(san.Build());
        using var certificadoOriginal = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        using var certificado = new X509Certificate2(certificadoOriginal.Export(X509ContentType.Pfx));
        var segredo = new string('z', 32);
        var fluxos = new[] { new FluxoEstadoCliente(), new FluxoEstadoCliente() };
        var eventos = new List<EventoEstado>();
        var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0, o => o.UseHttps(certificado)));
        await using var receptor = builder.Build();
        receptor.MapPost("/{indice:int}", async (int indice, HttpRequest request) =>
        {
            Assert.Null(request.Headers.Authorization.FirstOrDefault());
            using var memoria = new MemoryStream(); await request.Body.CopyToAsync(memoria);
            var evento = ReceptorWebhook.Verificar(memoria.ToArray(), request.Headers["X-Domino-Instante"],
                request.Headers["X-Domino-Assinatura"], segredo, DateTimeOffset.UtcNow);
            if (evento is null) return Results.Unauthorized();
            lock (eventos) eventos.Add(evento);
            return fluxos[indice].Receber(evento) ? Results.NoContent() : Results.BadRequest();
        });
        await receptor.StartAsync();
        var endereco = receptor.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var transporte = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            // Pin do certificado efêmero exclusivo deste teste; produção usa validação normal.
            ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert?.Thumbprint == certificado.Thumbprint
        });
        using var original = new ApiHttpIntegrationTests.DominoApiFactory();
        using var host = original.WithWebHostBuilder(web => web.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(WebhookOutboxWorker)).ToArray()) services.Remove(descriptor);
            services.Configure<WebhookOptions>(o => { o.AssinaturaSegredo = segredo; o.PermitirLoopbackEmDesenvolvimento = true; });
            services.AddTransient(p => new WebhookSender(transporte, p.GetRequiredService<WebhookDestinoValidator>(), p.GetRequiredService<IOptions<WebhookOptions>>()));
        }));
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>().CadastrarUsuarioAutorizadoAsync("e2e@fiap.com.br");
        using var httpA = host.CreateClient(); using var httpB = host.CreateClient();
        var clientes = new[] { new DominoApiClient(httpA), new DominoApiClient(httpB) };
        await clientes[0].SolicitarTokenAsync("e2e@fiap.com.br");
        var token = original.Entrega.ObterToken("e2e@fiap.com.br");
        var jogadores = new Guid[2];
        for (var i = 0; i < 2; i++)
        {
            jogadores[i] = (await clientes[i].CriarJogadorAsync(token, $"Perfil {i}")).JogadorId;
            var chave = DominoApiClient.GerarChaveAtivacao();
            await clientes[i].AtivarJogadorAsync(token, jogadores[i], chave);
            clientes[i].SelecionarJogador(jogadores[i], chave, endereco + "/" + i);
        }
        var worker = new WebhookOutboxWorker(host.Services.GetRequiredService<IServiceScopeFactory>(),
            host.Services.GetRequiredService<IOptions<WebhookOptions>>(), NullLogger<WebhookOutboxWorker>.Instance);
        async Task Entregar()
        {
            for (var n = 0; n < 16 && await worker.ProcessarUmAsync(default); n++) { }
        }
        var criada = await clientes[0].CriarAsync(token, 1);
        fluxos[0].Recuperar(criada);
        var ingresso = await clientes[1].AcaoAsync(token, criada.PartidaId, "entrar", 0, Guid.NewGuid());
        fluxos[1].Recuperar(ingresso.Estado);
        await clientes[0].AcaoAsync(token, criada.PartidaId, "iniciar", 1, Guid.NewGuid());
        await Entregar();
        Assert.All(fluxos, f => Assert.Equal(2, f.Estado!.VersaoEstado));
        Assert.Empty(fluxos[0].Estado!.MinhaMao.Intersect(fluxos[1].Estado!.MinhaMao));
        Assert.Single(fluxos, f => f.Estado!.PedraAberturaObrigatoria is not null);
        Assert.NotEqual(fluxos[0].Estado!.ParticipacaoId, fluxos[1].Estado!.ParticipacaoId);
        for (var n = 0; n < 300 && fluxos[0].Estado!.Situacao != SituacaoPartida.Encerrada; n++)
        {
            var i = Array.FindIndex(jogadores, id => id == fluxos[0].Estado!.JogadorDoTurnoId);
            var estado = fluxos[i].Estado!; var cliente = clientes[i];
            if (estado.ProximaAcao is ProximaAcao.Comprar or ProximaAcao.Passar)
                await cliente.AcaoAsync(token, estado.PartidaId, estado.ProximaAcao == ProximaAcao.Comprar ? "comprar" : "passar", estado.VersaoEstado, Guid.NewGuid());
            else
            {
                var pedras = estado.PedraAberturaObrigatoria is { } abertura ? new[] { abertura } : estado.MinhaMao;
                var aceita = false;
                foreach (var pedra in pedras)
                {
                    foreach (var lado in Enum.GetValues<LadoJogavel>())
                    {
                        try { await cliente.JogarAsync(token, estado.PartidaId, pedra, lado, estado.VersaoEstado, Guid.NewGuid()); aceita = true; break; }
                        catch (ApiException ex) when (ex.Status == 422) { }
                    }
                    if (aceita) break;
                }
                Assert.True(aceita);
            }
            await Entregar();
        }
        Assert.All(fluxos, f => Assert.Equal(SituacaoPartida.Encerrada, f.Estado!.Situacao));
        Assert.Contains(eventos, e => e.Tipo == "partida-encerrada");
        var observacao = System.Text.Json.JsonSerializer.Serialize(await clientes[0].ObservarAsync(token, criada.PartidaId));
        Assert.DoesNotContain("MinhaMao", observacao);
        Assert.DoesNotContain("PedraAberturaObrigatoria", observacao);
        Assert.DoesNotContain(segredo, observacao);
    }
}
