using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Application.Jogadores;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using DominoPontaDeQuina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DominoPontaDeQuina.Tests;

public sealed class PerfisJogadorHttpTests
{
    [Fact]
    public async Task DoisPerfisDaMesmaConta_JogamComMaosSeparadasEWebhookProprio()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "perfis@fiap.com.br");
        using var a = Cliente(host, token);
        using var b = Cliente(host, token);
        var jogadorA = await CriarPerfilAsync(a, "Perfil A");
        var jogadorB = await CriarPerfilAsync(b, "Perfil B");
        Assert.NotEqual(jogadorA.JogadorId, jogadorB.JogadorId);
        var chaveA = Chave(); var chaveB = Chave();
        await AtivarAsync(a, jogadorA.JogadorId, chaveA);
        await AtivarAsync(b, jogadorB.JogadorId, chaveB);
        var partida = await OkAsync<EstadoPartidaParticipante>(await a.PostAsJsonAsync("/api/partidas",
            new { pontuacaoAlvo = 50, jogadorId = jogadorA.JogadorId, webhook = "https://a.example/webhook" }));
        Assert.Single(partida.Participantes);
        var lista = await b.GetFromJsonAsync<PartidaDisponivel[]>("/api/partidas");
        Assert.Contains(lista!, p => p.PartidaId == partida.PartidaId);
        var chaveIngresso = Guid.NewGuid();
        var corpo = new { jogadorId = jogadorB.JogadorId, versaoEsperada = partida.VersaoEstado,
            chaveIdempotencia = chaveIngresso, webhook = "https://b.example/webhook" };
        Assert.Equal(HttpStatusCode.NotFound,
            (await b.PostAsJsonAsync($"/api/partidas/{partida.PartidaId}/participacoes", corpo)).StatusCode);
        var ingresso = await OkAsync<ConfirmacaoAcaoPartida>(await b.PostAsJsonAsync($"/api/partidas/{partida.PartidaId}/entrar", corpo));
        Assert.Equal(2, ingresso.Estado.Participantes.Count);
        var repetida = await OkAsync<ConfirmacaoAcaoPartida>(await b.PostAsJsonAsync($"/api/partidas/{partida.PartidaId}/entrar", corpo));
        Assert.True(repetida.AcaoIdempotente);
        Assert.Equal(ingresso.Estado.VersaoEstado, repetida.Estado.VersaoEstado);
        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsJsonAsync($"/api/partidas/{partida.PartidaId}/entrar",
            new { corpo.jogadorId, corpo.versaoEsperada, corpo.chaveIdempotencia, webhook = "https://outro.example/webhook" })).StatusCode);
        Assert.DoesNotContain((await b.GetFromJsonAsync<PartidaDisponivel[]>("/api/partidas"))!, p => p.PartidaId == partida.PartidaId);
        await OkAsync<ConfirmacaoAcaoPartida>(await a.PostAsJsonAsync($"/api/partidas/{partida.PartidaId}/iniciar",
            new { jogadorId = jogadorA.JogadorId, versaoEsperada = ingresso.Estado.VersaoEstado, chaveIdempotencia = Guid.NewGuid() }));
        var estadoA = await a.GetFromJsonAsync<EstadoPartidaParticipante>($"/api/partidas/{partida.PartidaId}?jogadorId={jogadorA.JogadorId}");
        var estadoB = await b.GetFromJsonAsync<EstadoPartidaParticipante>($"/api/partidas/{partida.PartidaId}?jogadorId={jogadorB.JogadorId}");
        Assert.Equal(7, estadoA!.MinhaMao.Count);
        Assert.Equal(7, estadoB!.MinhaMao.Count);
        Assert.Empty(estadoA.MinhaMao.Intersect(estadoB.MinhaMao));
        Assert.Equal(estadoA.JogadorDoTurnoId, estadoB.JogadorDoTurnoId);
        // Saber o jogadorId e compartilhar o Bearer não permite assumir a ativação do outro.
        Assert.Equal(HttpStatusCode.Forbidden, (await a.GetAsync($"/api/partidas/{partida.PartidaId}?jogadorId={jogadorB.JogadorId}")).StatusCode);
        a.DefaultRequestHeaders.Remove("X-Jogador-Ativacao");
        Assert.Equal(HttpStatusCode.Forbidden, (await a.GetAsync($"/api/partidas/{partida.PartidaId}?jogadorId={jogadorA.JogadorId}")).StatusCode);
        var observacao = await a.GetStringAsync($"/api/partidas/{partida.PartidaId}/visualizar");
        Assert.DoesNotContain("minhaMao", observacao, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook", observacao, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(chaveA, observacao);

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DominoDbContext>();
        var participantes = await db.ParticipacoesPartida.Where(p => p.PartidaId == partida.PartidaId).OrderBy(p => p.Posicao).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, participantes.Select(p => p.Posicao));
        Assert.Equal("https://a.example/webhook", participantes[0].WebhookUrl);
        Assert.Equal("https://b.example/webhook", participantes[1].WebhookUrl);
        Assert.All(await db.Jogadores.Where(j => j.AtivacaoHash != null).ToListAsync(), j =>
        {
            Assert.NotEqual(chaveA, j.AtivacaoHash);
            Assert.NotEqual(chaveB, j.AtivacaoHash);
        });
    }

    [Fact]
    public async Task AtivacaoConcorrente_TemUmVencedorERepeticaoDaMesmaChave()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "ativacao@fiap.com.br");
        using var a = Cliente(host, token); using var b = Cliente(host, token);
        var perfil = await CriarPerfilAsync(a, "Exclusivo");
        var chaveA = Chave(); var chaveB = Chave();
        var rota = $"/api/jogadores/{perfil.JogadorId}/ativar-cliente";
        var rotaAntiga = $"/api/jogadores/{perfil.JogadorId}/ativacao";
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync(rotaAntiga, new { chaveAtivacao = chaveA })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.DeleteAsync(rotaAntiga)).StatusCode);
        var respostas = await Task.WhenAll(a.PostAsJsonAsync(rota, new { chaveAtivacao = chaveA }),
            b.PostAsJsonAsync(rota, new { chaveAtivacao = chaveB }));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
        var vencedor = respostas[0].StatusCode == HttpStatusCode.OK ? chaveA : chaveB;
        var antes = await OkAsync<PerfilJogador>(respostas.Single(r => r.StatusCode == HttpStatusCode.OK));
        var repetida = await OkAsync<PerfilJogador>(await a.PostAsJsonAsync(rota, new { chaveAtivacao = vencedor }));
        Assert.Equal(antes.AtivadoEmUtc, repetida.AtivadoEmUtc);
        using var reaberto = Cliente(host, token);
        Assert.Equal(HttpStatusCode.Conflict, (await reaberto.PostAsJsonAsync(rota, new { chaveAtivacao = Chave() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reaberto.PostAsJsonAsync(rota, new { chaveAtivacao = vencedor })).StatusCode);
        var lista = await reaberto.GetStringAsync("/api/jogadores");
        Assert.DoesNotContain(vencedor, lista);
        Assert.DoesNotContain("hash", lista, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chaveAtivacao", lista, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LiberacaoExplicita_ExigeChaveAtualEPermiteRevincular()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "liberacao@fiap.com.br");
        using var atual = Cliente(host, token);
        using var novo = Cliente(host, token);
        var perfil = await CriarPerfilAsync(atual, "Transferível");
        var chaveAtual = Chave();
        await AtivarAsync(atual, perfil.JogadorId, chaveAtual);
        Assert.Equal(HttpStatusCode.MethodNotAllowed,
            (await atual.DeleteAsync($"/api/jogadores/{perfil.JogadorId}/ativar-cliente")).StatusCode);

        novo.DefaultRequestHeaders.Add("X-Jogador-Ativacao", Chave());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await novo.DeleteAsync($"/api/jogadores/{perfil.JogadorId}/desativar-cliente")).StatusCode);
        var liberada = await OkAsync<PerfilJogador>(await atual.DeleteAsync($"/api/jogadores/{perfil.JogadorId}/desativar-cliente"));
        Assert.False(liberada.Ativo);

        var novaChave = Chave();
        Assert.Equal(HttpStatusCode.OK, (await novo.PostAsJsonAsync($"/api/jogadores/{perfil.JogadorId}/ativar-cliente",
            new { chaveAtivacao = novaChave })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await atual.PostAsJsonAsync($"/api/jogadores/{perfil.JogadorId}/ativar-cliente",
            new { chaveAtivacao = chaveAtual })).StatusCode);
    }

    [Fact]
    public async Task JogadorComPartidaNaoEncerrada_NaoCriaOutraPartida()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var cliente = Cliente(host, await PrepararContaAsync(host, "incompativel@fiap.com.br"));
        var perfil = await CriarPerfilAsync(cliente, "Único");
        await AtivarAsync(cliente, perfil.JogadorId, Chave());
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/partidas",
            new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "https://jogador.example/webhook" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsJsonAsync("/api/partidas",
            new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "https://jogador.example/outro" })).StatusCode);
    }

    [Fact]
    public async Task ContaAlheiaEEntradasInvalidas_NaoAtivamNemUsamJogador()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var dono = Cliente(host, await PrepararContaAsync(host, "dono@fiap.com.br"));
        using var outro = Cliente(host, await PrepararContaAsync(host, "outro@fiap.com.br"));
        var perfil = await CriarPerfilAsync(dono, "Reservado");
        Assert.Equal(HttpStatusCode.BadRequest, (await dono.PostAsJsonAsync("/api/jogadores", new { nome = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await dono.PostAsJsonAsync($"/api/jogadores/{perfil.JogadorId}/ativar-cliente", new { chaveAtivacao = "curta" })).StatusCode);
        var chave = Chave();
        Assert.Equal(HttpStatusCode.Forbidden, (await outro.PostAsJsonAsync($"/api/jogadores/{perfil.JogadorId}/ativar-cliente", new { chaveAtivacao = chave })).StatusCode);
        Assert.DoesNotContain((await outro.GetFromJsonAsync<PerfilJogador[]>("/api/jogadores"))!, p => p.JogadorId == perfil.JogadorId);
        outro.DefaultRequestHeaders.Add("X-Jogador-Ativacao", chave);
        Assert.Equal(HttpStatusCode.Forbidden, (await outro.PostAsJsonAsync("/api/partidas", new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "https://a.example/webhook" })).StatusCode);
        await AtivarAsync(dono, perfil.JogadorId, chave);
        Assert.Equal(HttpStatusCode.Forbidden, (await outro.PostAsJsonAsync("/api/partidas", new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "https://a.example/webhook" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await dono.PostAsJsonAsync("/api/partidas", new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "file:///teste" })).StatusCode);
    }

    [Fact]
    public async Task UltimaVagaConcorrente_TemUmVencedorESemTerceiroParticipante()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "vaga@fiap.com.br");
        using var criador = Cliente(host, token); using var a = Cliente(host, token); using var b = Cliente(host, token);
        var primeiro = await CriarPerfilAsync(criador, "Primeiro");
        var segundo = await CriarPerfilAsync(a, "Segundo");
        var terceiro = await CriarPerfilAsync(b, "Terceiro");
        await AtivarAsync(criador, primeiro.JogadorId, Chave());
        await AtivarAsync(a, segundo.JogadorId, Chave());
        await AtivarAsync(b, terceiro.JogadorId, Chave());
        var partida = await OkAsync<EstadoPartidaParticipante>(await criador.PostAsJsonAsync("/api/partidas",
            new { jogadorId = primeiro.JogadorId, pontuacaoAlvo = 50, webhook = "https://primeiro.example/webhook" }));
        var rota = $"/api/partidas/{partida.PartidaId}/entrar";
        var respostas = await Task.WhenAll(
            a.PostAsJsonAsync(rota, new { jogadorId = segundo.JogadorId, versaoEsperada = 0, chaveIdempotencia = Guid.NewGuid(), webhook = "https://segundo.example/webhook" }),
            b.PostAsJsonAsync(rota, new { jogadorId = terceiro.JogadorId, versaoEsperada = 0, chaveIdempotencia = Guid.NewGuid(), webhook = "https://terceiro.example/webhook" }));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
        var estado = await criador.GetFromJsonAsync<EstadoPartidaParticipante>($"/api/partidas/{partida.PartidaId}?jogadorId={primeiro.JogadorId}");
        Assert.Equal(2, estado!.Participantes.Count);
        Assert.Equal(1, estado.VersaoEstado);
        Assert.DoesNotContain((await criador.GetFromJsonAsync<PartidaDisponivel[]>("/api/partidas"))!, p => p.PartidaId == partida.PartidaId);
        // O perfil que perdeu a disputa continua ativo e pode criar uma partida distinta.
        var perdedor = respostas[0].StatusCode == HttpStatusCode.Conflict ? a : b;
        var perfilPerdedor = respostas[0].StatusCode == HttpStatusCode.Conflict ? segundo : terceiro;
        var outra = await OkAsync<EstadoPartidaParticipante>(await perdedor.PostAsJsonAsync("/api/partidas",
            new { jogadorId = perfilPerdedor.JogadorId, pontuacaoAlvo = 50, webhook = "https://outra.example/webhook" }));
        Assert.NotEqual(partida.PartidaId, outra.PartidaId);
    }

    [Fact]
    public async Task UpgradeSqlitePerfis_PreservaContaJogadorEPartida()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "upgradeperfil@fiap.com.br");
        Guid jogadorId; Guid partidaId;
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DominoDbContext>();
            var jogador = await db.Jogadores.SingleAsync();
            jogadorId = jogador.Id;
            var jornada = scope.ServiceProvider.GetRequiredService<IJornadaPartidaService>();
            var criada = await jornada.CriarAsync(new(jogador.UsuarioId!.Value, 50));
            partidaId = criada.Valor!.PartidaId;
            // Somente no banco descartável: reproduz a versão anterior sem as novas colunas.
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Jogadores DROP COLUMN AtivacaoHash;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Jogadores DROP COLUMN AtivadoEmUtc;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE ParticipacoesPartida DROP COLUMN WebhookUrl;");
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sqlite", "PerfisJogadorAtivacaoWebhook.sql"));
            await db.Database.ExecuteSqlRawAsync(script);
        }
        using var cliente = Cliente(host, token);
        var perfil = Assert.Single((await cliente.GetFromJsonAsync<PerfilJogador[]>("/api/jogadores"))!);
        Assert.Equal(jogadorId, perfil.JogadorId);
        Assert.False(perfil.Ativo);
        await AtivarAsync(cliente, jogadorId, Chave());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/partidas/{partidaId}?jogadorId={jogadorId}")).StatusCode);
    }

    private static string Chave() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    [Fact]
    public async Task MesmoPerfilCriandoSimultaneamente_SomenteUmaPartidaPersiste()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        var token = await PrepararContaAsync(host, "corridaperfil@fiap.com.br");
        using var a = Cliente(host, token); using var b = Cliente(host, token);
        var perfil = await CriarPerfilAsync(a, "Mesmo perfil"); var chave = Chave();
        await AtivarAsync(a, perfil.JogadorId, chave); await AtivarAsync(b, perfil.JogadorId, chave);
        var corpo = new { jogadorId = perfil.JogadorId, pontuacaoAlvo = 50, webhook = "https://example.com/webhook" };
        var respostas = await Task.WhenAll(a.PostAsJsonAsync("/api/partidas", corpo), b.PostAsJsonAsync("/api/partidas", corpo));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DominoDbContext>();
        Assert.Equal(1, await db.ParticipacoesPartida.CountAsync(p => p.JogadorId == perfil.JogadorId));
        Assert.Equal(1, await db.EventosWebhook.CountAsync(e => e.JogadorId == perfil.JogadorId));
    }

    private static HttpClient Cliente(ApiHttpIntegrationTests.DominoApiFactory host, string token)
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    private static async Task<string> PrepararContaAsync(ApiHttpIntegrationTests.DominoApiFactory host, string email)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
        await identidade.CadastrarUsuarioAutorizadoAsync(email);
        await identidade.SolicitarTokenAsync(email, "3ESC#-CP6");
        return host.Entrega.ObterToken(email);
    }
    private static async Task<PerfilJogador> CriarPerfilAsync(HttpClient client, string nome) =>
        await OkAsync<PerfilJogador>(await client.PostAsJsonAsync("/api/jogadores", new { nome }));
    private static async Task AtivarAsync(HttpClient client, Guid jogadorId, string chave)
    {
        var resposta = await client.PostAsJsonAsync($"/api/jogadores/{jogadorId}/ativar-cliente", new { chaveAtivacao = chave });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.DoesNotContain(chave, await resposta.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-Jogador-Ativacao", chave);
    }
    private static async Task<T> OkAsync<T>(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<T>())!;
    }
}
