using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using DominoPontaDeQuina.Api.Webhooks;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Tests;

/// <summary>Consumidor escrito com rotas e JSON do README público, sem DTOs ou cliente do projeto.</summary>
public sealed class AceiteClienteIndependenteTests
{
    [Fact]
    public async Task ContratoPublico_DoisReceptoresMesmoToken_AteResultadoERevogacao()
    {
        const string segredo = "segredo-exclusivo-do-aceite-publico";
        var estados = new JsonNode?[2];
        using var transporte = new HttpClient(new Receptores(estados, segredo));
        using var original = new ApiHttpIntegrationTests.DominoApiFactory();
        using var host = original.WithWebHostBuilder(web => web.ConfigureTestServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(WebhookOutboxWorker)).ToArray()) services.Remove(d);
            services.Configure<WebhookOptions>(o => o.AssinaturaSegredo = segredo);
            services.AddTransient(p => new WebhookSender(transporte, p.GetRequiredService<WebhookDestinoValidator>(),
                p.GetRequiredService<IOptions<WebhookOptions>>()));
        }));
        // Preparação operacional do ambiente: não faz parte do consumidor público.
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>().CadastrarUsuarioAutorizadoAsync("aceite@fiap.com.br");
        using var a = host.CreateClient(); using var b = host.CreateClient();
        var clientes = new[] { a, b };
        using var email = await a.PostAsJsonAsync("api/identidade/token", new { email = "aceite@fiap.com.br" });
        Assert.Equal(HttpStatusCode.Accepted, email.StatusCode);
        Assert.Equal("", await email.Content.ReadAsStringAsync());
        var token = original.Entrega.ObterToken("aceite@fiap.com.br");
        var perfis = new string[2];
        for (var i = 0; i < 2; i++)
        {
            clientes[i].DefaultRequestHeaders.Authorization = new("Bearer", token);
            var perfil = await Enviar(clientes[i], "api/jogadores", new { nome = $"Independente {i}" });
            perfis[i] = perfil["jogadorId"]!.GetValue<string>();
            var chave = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await Enviar(clientes[i], $"api/jogadores/{perfis[i]}/ativar-cliente", new { chaveAtivacao = chave });
            clientes[i].DefaultRequestHeaders.Add("X-Jogador-Ativacao", chave);
        }
        var criada = await Enviar(a, "api/partidas", new { jogadorId = perfis[0], pontuacaoAlvo = 1, webhook = "https://8.8.8.8/0" });
        var id = criada["partidaId"]!.GetValue<string>();
        var lista = await Enviar(b, "api/partidas");
        Assert.Contains(lista.AsArray(), p => p!["partidaId"]!.GetValue<string>() == id);
        await Enviar(b, $"api/partidas/{id}/entrar", new { jogadorId = perfis[1], webhook = "https://8.8.8.8/1", versaoEsperada = 0, chaveIdempotencia = Guid.NewGuid() });
        await Enviar(a, $"api/partidas/{id}/iniciar", new { jogadorId = perfis[0], versaoEsperada = 1, chaveIdempotencia = Guid.NewGuid() });
        var worker = new WebhookOutboxWorker(host.Services.GetRequiredService<IServiceScopeFactory>(),
            host.Services.GetRequiredService<IOptions<WebhookOptions>>(), NullLogger<WebhookOutboxWorker>.Instance);
        async Task Entregar() { for (var n = 0; n < 16 && await worker.ProcessarUmAsync(default); n++) { } }
        await Entregar();
        Assert.NotEqual(estados[0]!["participacaoId"]!.ToString(), estados[1]!["participacaoId"]!.ToString());
        for (var n = 0; n < 300 && estados[0]!["situacao"]!.ToString() != "encerrada"; n++)
        {
            var i = Array.IndexOf(perfis, estados[0]!["jogadorDoTurnoId"]!.GetValue<string>());
            var estado = estados[i]!;
            var acao = estado["proximaAcao"]!.ToString();
            var versao = estado["versaoEstado"]!.GetValue<long>();
            if (acao is "comprar" or "passar")
                await Enviar(clientes[i], $"api/partidas/{id}/{(acao == "comprar" ? "comprar" : "passar")}",
                    new { jogadorId = perfis[i], versaoEsperada = versao, chaveIdempotencia = Guid.NewGuid() });
            else
            {
                var pedras = estado["pedraAberturaObrigatoria"] is { } abertura ? new[] { abertura } : estado["minhaMao"]!.AsArray().ToArray();
                var aceita = false;
                foreach (var pedra in pedras)
                {
                    for (var lado = 0; lado < 2; lado++)
                    {
                        using var resposta = await clientes[i].PostAsJsonAsync($"api/partidas/{id}/jogar", new
                        { jogadorId = perfis[i], valorA = pedra!["valorA"]!.GetValue<int>(), valorB = pedra["valorB"]!.GetValue<int>(), lado, versaoEsperada = versao, chaveIdempotencia = Guid.NewGuid() });
                        if (resposta.IsSuccessStatusCode) { aceita = true; break; }
                        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
                    }
                    if (aceita) break;
                }
                Assert.True(aceita);
            }
            await Entregar();
        }
        Assert.All(estados, e => Assert.Equal("encerrada", e!["situacao"]!.ToString()));
        var resultado = await Enviar(a, $"api/partidas/{id}/resultado?jogadorId={perfis[0]}");
        Assert.Equal(2, resultado["situacao"]!.GetValue<int>());
        Assert.NotEmpty(resultado["eventos"]!.AsArray());
        var observacao = await Enviar(b, $"api/partidas/{id}/visualizar");
        Assert.Null(observacao["minhaMao"]);
        Assert.Null(observacao["pedraAberturaObrigatoria"]);
        using var revogacao = await a.PostAsync("api/identidade/tokens/revogacao", null);
        Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);
        foreach (var cliente in clientes)
        {
            using var resposta = await cliente.GetAsync("api/identidade/sessao");
            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }
    }

    private static async Task<JsonNode> Enviar(HttpClient http, string rota, object? corpo = null)
    {
        using var resposta = corpo is null ? await http.GetAsync(rota) : await http.PostAsJsonAsync(rota, corpo);
        resposta.EnsureSuccessStatusCode();
        return JsonNode.Parse(await resposta.Content.ReadAsStringAsync())!;
    }

    // Dois destinos simulados: nenhum pacote é enviado ao IP público usado no contrato.
    private sealed class Receptores(JsonNode?[] estados, string segredo) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Null(request.Headers.Authorization);
            var corpo = await request.Content!.ReadAsByteArrayAsync(ct);
            var instante = request.Headers.GetValues("X-Domino-Instante").Single();
            var prefixo = Encoding.UTF8.GetBytes(instante + ".");
            var dados = prefixo.Concat(corpo).ToArray();
            var assinatura = Convert.FromHexString(request.Headers.GetValues("X-Domino-Assinatura").Single()[7..]);
            Assert.True(CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), dados), assinatura));
            var evento = JsonNode.Parse(corpo)!;
            var i = int.Parse(request.RequestUri!.AbsolutePath.Trim('/'));
            var estado = evento["estado"]!;
            if (estados[i] is null || estado["versaoEstado"]!.GetValue<long>() > estados[i]!["versaoEstado"]!.GetValue<long>()) estados[i] = estado.DeepClone();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
