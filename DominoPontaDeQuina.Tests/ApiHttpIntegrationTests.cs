using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DominoPontaDeQuina.Tests;

/// <summary>Exerce os contratos HTTP públicos contra um host real em SQLite descartável.</summary>
public sealed class ApiHttpIntegrationTests : IClassFixture<ApiHttpIntegrationTests.DominoApiFactory>
{
    private const string SenhaAcademica = "3ESC#-CP6";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DominoApiFactory factory;

    public ApiHttpIntegrationTests(DominoApiFactory factory) => this.factory = factory;

    [Fact(DisplayName = "Swagger documenta todas as rotas, autenticação, respostas e exemplos")]
    public async Task Swagger_DescreveContratoPublico()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = document.RootElement;
        Assert.Equal("Dominó Ponta de Quina — API", root.GetProperty("info").GetProperty("title").GetString());
        var components = root.GetProperty("components");
        Assert.Equal("bearer", components.GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var operations = root.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (Path: path.Name, Operation: operation.Value))).ToArray();
        Assert.Equal(21, operations.Length);
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/partidas/{partidaId}/cancelar", out _));
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/partidas/{partidaId}/reiniciar", out _));
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/partidas/{partidaId}/reinicio", out _));
        Assert.False(root.GetProperty("paths").TryGetProperty("/api/identidade/login", out _));
        foreach (var (atual, anterior) in new[]
        {
            ("iniciar", "inicio"), ("jogar", "jogadas"),
            ("comprar", "compras"), ("passar", "passagens")
        })
        {
            Assert.True(root.GetProperty("paths").TryGetProperty($"/api/partidas/{{partidaId}}/{atual}", out _));
            Assert.False(root.GetProperty("paths").TryGetProperty($"/api/partidas/{{partidaId}}/{anterior}", out _));
        }
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/partidas/{partidaId}/visualizar", out _));
        Assert.False(root.GetProperty("paths").TryGetProperty("/api/partidas/{partidaId}/observacao", out _));
        foreach (var (path, operation) in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.NotEmpty(operation.GetProperty("tags").EnumerateArray());
            var responses = operation.GetProperty("responses");
            if (path is "/api/identidade/token")
            {
                Assert.False(operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0);
                Assert.True(responses.TryGetProperty("202", out _));
                Assert.True(responses.TryGetProperty("429", out _));
                Assert.Equal("boolean", responses.GetProperty("400").GetProperty("content")
                    .GetProperty("application/json").GetProperty("schema").GetProperty("type").GetString());
            }
            else
            {
                Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
                Assert.True(responses.TryGetProperty("401", out _));
            }
            if (path.StartsWith("/api/partidas", StringComparison.Ordinal))
            {
                if (operation.GetProperty("summary").GetString() == "Listar partidas disponíveis")
                {
                    Assert.Equal("array", responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("type").GetString());
                    continue;
                }
                var expectedSchema = path.EndsWith("/visualizar", StringComparison.Ordinal) ? "EstadoPartidaObservador"
                    : path.EndsWith("/resultado", StringComparison.Ordinal) ? "ResultadoHistoricoPartida"
                    : path is "/api/partidas" or "/api/partidas/{partidaId}" or "/api/partidas/{partidaId}/atualizacoes" or "/api/partidas/{partidaId}/reinicio" ? "EstadoPartidaParticipante"
                    : "ConfirmacaoAcaoPartida";
                Assert.Equal($"#/components/schemas/{expectedSchema}", responses.GetProperty("200")
                    .GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString());
                Assert.Contains("CONFLITO_DE_ESTADO", responses.GetProperty("409").GetProperty("description").GetString());
            }
        }
        var schemas = components.GetProperty("schemas");
        Assert.False(schemas.GetProperty("EstadoPartidaObservador").GetProperty("properties").TryGetProperty("minhaMao", out _));
        Assert.Contains("0 = aguardar", schemas.GetProperty("ProximaAcaoPartida")
            .GetProperty("description").GetString());
        Assert.Contains("esquerda para a direita", schemas.GetProperty("EstadoPartidaParticipante")
            .GetProperty("properties").GetProperty("tabuleiro").GetProperty("description").GetString());
        Assert.Contains("64 caracteres hexadecimais", schemas.GetProperty("AtivarJogadorRequest")
            .GetProperty("properties").GetProperty("chaveAtivacao").GetProperty("description").GetString());
        Assert.Contains("pecaJogada e lado null", schemas.GetProperty("EventoHistoricoPartida")
            .GetProperty("description").GetString());
        Assert.Equal(0, schemas.GetProperty("JogarRequest").GetProperty("example").GetProperty("lado").GetInt32());
        Assert.Equal(50, schemas.GetProperty("CriarPartidaRequest").GetProperty("example").GetProperty("pontuacaoAlvo").GetInt32());
        Assert.False(schemas.GetProperty("SolicitarTokenRequest").GetProperty("properties").TryGetProperty("senha", out _));
        Assert.False(schemas.TryGetProperty("LoginRequest", out _));
        Assert.True(root.GetProperty("paths").GetProperty("/api/partidas").GetProperty("post").GetProperty("callbacks").TryGetProperty("estadoAlterado", out _));
        Assert.True(root.GetProperty("paths").GetProperty("/api/partidas/{partidaId}/atualizacoes")
            .GetProperty("get").GetProperty("responses").TryGetProperty("204", out _));
    }

    [Fact(DisplayName = "Cliente consulta atualizações por versão sem webhook e só recebe seu estado privado")]
    public async Task Atualizacoes_PermitemPollingSemWebhook()
    {
        using var host = new DominoApiFactory();
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("consulta-a@fiap.com.br")).Sucesso);
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("consulta-b@fiap.com.br")).Sucesso);
        }
        using var client = host.CreateClient();
        async Task<string> TokenAsync(string email)
        {
            Assert.Equal(HttpStatusCode.Accepted,
                (await client.PostAsJsonAsync("/api/identidade/token", new { email })).StatusCode);
            return host.Entrega.ObterToken(email);
        }

        var tokenA = await TokenAsync("consulta-a@fiap.com.br");
        var tokenB = await TokenAsync("consulta-b@fiap.com.br");
        await AtivarPerfilAsync(client, tokenA);
        var jogadorA = Guid.Parse(client.DefaultRequestHeaders.GetValues("X-Jogador-Id").Single());
        var criacao = await client.PostAsJsonAsync("/api/partidas", new { pontuacaoAlvo = 50, jogadorId = jogadorA });
        Assert.Equal(HttpStatusCode.OK, criacao.StatusCode);
        var criada = (await criacao.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(JsonOptions))!;
        var rota = $"/api/partidas/{criada.PartidaId}/atualizacoes?jogadorId={jogadorA}&versaoConhecida={criada.VersaoEstado}";
        var semMudanca = await client.GetAsync(rota);
        Assert.Equal(HttpStatusCode.NoContent, semMudanca.StatusCode);
        Assert.Equal(string.Empty, await semMudanca.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            $"/api/partidas/{criada.PartidaId}/atualizacoes?jogadorId={jogadorA}&versaoConhecida=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            $"/api/partidas/{criada.PartidaId}/atualizacoes?jogadorId={jogadorA}&versaoConhecida=invalida")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            $"/api/partidas/{criada.PartidaId}/atualizacoes?versaoConhecida={criada.VersaoEstado}")).StatusCode);

        await AtivarPerfilAsync(client, tokenB);
        var jogadorB = Guid.Parse(client.DefaultRequestHeaders.GetValues("X-Jogador-Id").Single());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(rota)).StatusCode);
        var entrada = await client.PostAsJsonAsync($"/api/partidas/{criada.PartidaId}/entrar",
            new { versaoEsperada = criada.VersaoEstado, chaveIdempotencia = Guid.NewGuid(), jogadorId = jogadorB });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);

        await AtivarPerfilAsync(client, tokenA);
        var atualizacao = await client.GetAsync(rota);
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        var estado = (await atualizacao.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(JsonOptions))!;
        Assert.Equal(2, estado.Participantes.Count);
        Assert.True(estado.VersaoEstado > criada.VersaoEstado);
        Assert.Equal(criada.ParticipacaoId, estado.ParticipacaoId);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync(
            $"/api/partidas/{criada.PartidaId}/atualizacoes?jogadorId={jogadorA}&versaoConhecida={estado.VersaoEstado}")).StatusCode);
    }

    [Fact(DisplayName = "API HTTP autentica, joga, protege observação, trata conflito, revoga token e limita solicitação")]
    public async Task JornadaPublica_ProtegeContratosEEstados()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("alice@fiap.com.br")).Sucesso);
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("bob@fiap.com.br")).Sucesso);
        }

        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);

        var tokenAlice = await SolicitarTokenAsync(client, "alice@fiap.com.br");
        var tokenBob = await SolicitarTokenAsync(client, "bob@fiap.com.br");

        await AtivarPerfilAsync(client, tokenAlice);
        var criada = await PostarAsync<EstadoPartidaParticipante>(client, "/api/partidas", new { pontuacaoAlvo = 50 });
        var jogadorAliceId = Assert.Single(criada.Participantes).JogadorId;

        await AtivarPerfilAsync(client, tokenBob);
        var ingresso = await PostarAsync<ConfirmacaoAcaoPartida>(client,
            $"/api/partidas/{criada.PartidaId}/entrar", new { versaoEsperada = criada.VersaoEstado, chaveIdempotencia = Guid.NewGuid() });

        await AtivarPerfilAsync(client, tokenAlice);
        var inicio = await PostarAsync<ConfirmacaoAcaoPartida>(client,
            $"/api/partidas/{criada.PartidaId}/iniciar", new { versaoEsperada = ingresso.Estado.VersaoEstado, chaveIdempotencia = Guid.NewGuid() });

        await AtivarPerfilAsync(client, tokenBob);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/partidas/{criada.PartidaId}/observacao")).StatusCode);
        var observacaoResposta = await client.GetAsync($"/api/partidas/{criada.PartidaId}/visualizar");
        Assert.Equal(HttpStatusCode.OK, observacaoResposta.StatusCode);
        var observacaoJson = await observacaoResposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("minhaMao", observacaoJson, StringComparison.OrdinalIgnoreCase);
        var observacao = JsonSerializer.Deserialize<EstadoPartidaObservador>(observacaoJson, JsonOptions);
        Assert.NotNull(observacao);
        Assert.Equal(2, observacao!.Participantes.Count);

        var estadoDoTurno = inicio.Estado.JogadorDoTurnoId == jogadorAliceId
            ? inicio.Estado
            : await ObterEstadoAsync(client, criada.PartidaId);
        if (inicio.Estado.JogadorDoTurnoId == jogadorAliceId)
            await AtivarPerfilAsync(client, tokenAlice);
        var peca = Assert.IsType<PecaPartida>(estadoDoTurno.PedraAberturaObrigatoria);
        var versaoAntiga = estadoDoTurno.VersaoEstado;
        var jogada = await PostarAsync<ConfirmacaoAcaoPartida>(client,
            $"/api/partidas/{criada.PartidaId}/jogar",
            new { valorA = peca.ValorA, valorB = peca.ValorB, lado = 0, versaoEsperada = versaoAntiga, chaveIdempotencia = Guid.NewGuid(), jogadorId = estadoDoTurno.JogadorDoTurnoId });
        Assert.Equal(versaoAntiga + 1, jogada.Estado.VersaoEstado);
        Assert.Single(jogada.Estado.Tabuleiro);

        var conflito = await client.PostAsJsonAsync($"/api/partidas/{criada.PartidaId}/jogar",
            new { valorA = peca.ValorA, valorB = peca.ValorB, lado = 0, versaoEsperada = versaoAntiga, chaveIdempotencia = Guid.NewGuid(), jogadorId = estadoDoTurno.JogadorDoTurnoId });
        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode);
        var erro = await conflito.Content.ReadFromJsonAsync<RespostaErro>(JsonOptions);
        Assert.Equal("CONFLITO_DE_ESTADO", erro!.Codigo);
        Assert.NotNull(erro.EstadoAutoritativo);

        await AtivarPerfilAsync(client, tokenAlice);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/identidade/tokens/revogacao", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identidade/sessao")).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        for (var tentativa = 0; tentativa < 5; tentativa++)
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/identidade/token", new { email = "invalido@fiap.com.br", senha = SenhaAcademica })).StatusCode);
        Assert.Equal((HttpStatusCode)429, (await client.PostAsJsonAsync("/api/identidade/token", new { email = "invalido@fiap.com.br", senha = SenhaAcademica })).StatusCode);
    }

    [Fact(DisplayName = "Mesmo token autentica dois clientes e novos acessos até revogação explícita")]
    public async Task TokenCompartilhado_ReutilizaERevogaNosDoisClientes()
    {
        // Host isolado para não compartilhar a janela de rate limit com outros cenários.
        using var host = new DominoApiFactory();
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
            await identidade.CadastrarUsuarioAutorizadoAsync("reuso@fiap.com.br");
        }
        using var clienteA = host.CreateClient();
        using var clienteB = host.CreateClient();
        var emissao = await clienteA.PostAsJsonAsync("/api/identidade/token",
            new { email = "reuso@fiap.com.br", senha = SenhaAcademica });
        Assert.Equal(HttpStatusCode.Accepted, emissao.StatusCode);
        Assert.Equal(string.Empty, await emissao.Content.ReadAsStringAsync());
        var token = host.Entrega.ObterToken("reuso@fiap.com.br");
        clienteA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        clienteB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        for (var acesso = 0; acesso < 3; acesso++)
        {
            var respostas = await Task.WhenAll(clienteA.GetAsync("/api/identidade/sessao"), clienteB.GetAsync("/api/identidade/sessao"));
            foreach (var resposta in respostas)
            {
                Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
                Assert.DoesNotContain(token, await resposta.Content.ReadAsStringAsync());
                resposta.Dispose();
            }
        }
        using var reaberto = host.CreateClient();
        reaberto.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await reaberto.GetAsync("/api/identidade/sessao")).StatusCode);

        var novaEmissao = await clienteB.PostAsJsonAsync("/api/identidade/token",
            new { email = "reuso@fiap.com.br", senha = SenhaAcademica });
        Assert.Equal(HttpStatusCode.Accepted, novaEmissao.StatusCode);
        var novoToken = host.Entrega.ObterToken("reuso@fiap.com.br");
        Assert.NotEqual(token, novoToken);
        Assert.Equal(HttpStatusCode.OK, (await clienteA.GetAsync("/api/identidade/sessao")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clienteA.PostAsync("/api/identidade/tokens/revogacao", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteA.GetAsync("/api/identidade/sessao")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteB.GetAsync("/api/identidade/sessao")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await reaberto.GetAsync("/api/identidade/sessao")).StatusCode);
        reaberto.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", novoToken);
        Assert.Equal(HttpStatusCode.OK, (await reaberto.GetAsync("/api/identidade/sessao")).StatusCode);
    }

    [Fact(DisplayName = "HTTP cancela partida aberta e permite criar outra com o mesmo perfil")]
    public async Task CancelarPartida_PreservaContratoHttp()
    {
        using var host = new DominoApiFactory();
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("cancelamento@fiap.com.br")).Sucesso);
        }
        using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted,
            (await client.PostAsJsonAsync("/api/identidade/token", new { email = "cancelamento@fiap.com.br" })).StatusCode);
        await AtivarPerfilAsync(client, host.Entrega.ObterToken("cancelamento@fiap.com.br"));
        var criada = await PostarAsync<EstadoPartidaParticipante>(client, "/api/partidas", new { pontuacaoAlvo = 100 });
        var chave = Guid.NewGuid();
        var rota = $"/api/partidas/{criada.PartidaId}/cancelar";
        var cancelada = await PostarAsync<ConfirmacaoAcaoPartida>(client, rota,
            new { versaoEsperada = criada.VersaoEstado, chaveIdempotencia = chave });
        Assert.Equal(SituacaoPartida.Encerrada, cancelada.Estado.Situacao);
        Assert.Null(cancelada.Estado.VencedorId);
        Assert.Equal(criada.VersaoEstado + 1, cancelada.Estado.VersaoEstado);
        var repetida = await PostarAsync<ConfirmacaoAcaoPartida>(client, rota,
            new { versaoEsperada = criada.VersaoEstado, chaveIdempotencia = chave });
        Assert.True(repetida.AcaoIdempotente);
        var outra = await PostarAsync<EstadoPartidaParticipante>(client, "/api/partidas", new { pontuacaoAlvo = 100 });
        Assert.NotEqual(criada.PartidaId, outra.PartidaId);
    }

    [Fact(DisplayName = "HTTP reinicia para ambos os participantes sem reutilizar o placar antigo")]
    public async Task ReiniciarPartida_ContinuidadeDosDoisClientes()
    {
        using var host = new DominoApiFactory();
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var identidade = scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>();
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("reinicio-a@fiap.com.br")).Sucesso);
            Assert.True((await identidade.CadastrarUsuarioAutorizadoAsync("reinicio-b@fiap.com.br")).Sucesso);
        }
        using var client = host.CreateClient();
        foreach (var email in new[] { "reinicio-a@fiap.com.br", "reinicio-b@fiap.com.br" })
            Assert.Equal(HttpStatusCode.Accepted,
                (await client.PostAsJsonAsync("/api/identidade/token", new { email })).StatusCode);
        var tokenA = host.Entrega.ObterToken("reinicio-a@fiap.com.br");
        var tokenB = host.Entrega.ObterToken("reinicio-b@fiap.com.br");
        await AtivarPerfilAsync(client, tokenA);
        var criada = await PostarAsync<EstadoPartidaParticipante>(client, "/api/partidas", new { pontuacaoAlvo = 100 });
        await AtivarPerfilAsync(client, tokenB);
        var ingresso = await PostarAsync<ConfirmacaoAcaoPartida>(client, $"/api/partidas/{criada.PartidaId}/entrar",
            new { versaoEsperada = criada.VersaoEstado, chaveIdempotencia = Guid.NewGuid() });
        await AtivarPerfilAsync(client, tokenA);
        var inicio = await PostarAsync<ConfirmacaoAcaoPartida>(client, $"/api/partidas/{criada.PartidaId}/iniciar",
            new { versaoEsperada = ingresso.Estado.VersaoEstado, chaveIdempotencia = Guid.NewGuid() });
        var reinicio = await PostarAsync<ConfirmacaoAcaoPartida>(client, $"/api/partidas/{criada.PartidaId}/reiniciar",
            new { versaoEsperada = inicio.Estado.VersaoEstado, chaveIdempotencia = Guid.NewGuid() });
        Assert.NotEqual(criada.PartidaId, reinicio.Estado.PartidaId);
        Assert.Equal(SituacaoPartida.EmAndamento, reinicio.Estado.Situacao);
        Assert.All(reinicio.Estado.Participantes, item => Assert.Equal(0, item.Pontuacao));
        await AtivarPerfilAsync(client, tokenB);
        var continuidade = await client.GetFromJsonAsync<EstadoPartidaParticipante>(
            $"/api/partidas/{criada.PartidaId}/reinicio?jogadorId={client.DefaultRequestHeaders.GetValues("X-Jogador-Id").Single()}");
        Assert.Equal(reinicio.Estado.PartidaId, continuidade!.PartidaId);
        Assert.Equal(2, continuidade.Participantes.Count);
    }

    private static async Task AtivarPerfilAsync(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var perfis = await client.GetFromJsonAsync<DominoPontaDeQuina.Application.Jogadores.PerfilJogador[]>("/api/jogadores");
        var jogador = Assert.Single(perfis!);
        var chave = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(jogador.JogadorId.ToString())));
        var resposta = await client.PostAsJsonAsync($"/api/jogadores/{jogador.JogadorId}/ativar-cliente", new { chaveAtivacao = chave });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Jogador-Ativacao");
        client.DefaultRequestHeaders.Add("X-Jogador-Ativacao", chave);
        client.DefaultRequestHeaders.Remove("X-Jogador-Id");
        client.DefaultRequestHeaders.Add("X-Jogador-Id", jogador.JogadorId.ToString());
    }

    private async Task<string> SolicitarTokenAsync(HttpClient client, string email)
    {
        var resposta = await client.PostAsJsonAsync("/api/identidade/token", new { email, senha = SenhaAcademica });
        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", texto, StringComparison.OrdinalIgnoreCase);
        return factory.Entrega.ObterToken(email);
    }

    private static async Task<T> PostarAsync<T>(HttpClient client, string rota, object corpo)
    {
        var payload = JsonSerializer.SerializeToNode(corpo)!.AsObject();
        payload["jogadorId"] = client.DefaultRequestHeaders.GetValues("X-Jogador-Id").Single();
        if (rota == "/api/partidas" || rota.EndsWith("/entrar", StringComparison.Ordinal))
            payload["webhook"] = "https://cliente.example/webhook";
        var resposta = await client.PostAsJsonAsync(rota, payload);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private static async Task<EstadoPartidaParticipante> ObterEstadoAsync(HttpClient client, Guid partidaId)
    {
        var resposta = await client.GetAsync($"/api/partidas/{partidaId}?jogadorId={client.DefaultRequestHeaders.GetValues("X-Jogador-Id").Single()}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(JsonOptions))!;
    }

    public sealed class DominoApiFactory : WebApplicationFactory<Program>
    {
        private readonly string banco = Path.Combine(Path.GetTempPath(), $"domino-http-{Guid.NewGuid():N}.db");
        public EntregaCapturada Entrega { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting("Domino:UsarSqlite", "true");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={banco}");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEntregaTokenEmail>();
                services.AddSingleton(Entrega);
                services.AddScoped<IEntregaTokenEmail>(provider => provider.GetRequiredService<EntregaCapturada>());
            });
        }
    }

    public sealed class EntregaCapturada : IEntregaTokenEmail
    {
        private readonly Dictionary<string, string> tokens = new(StringComparer.OrdinalIgnoreCase);
        public Task EntregarAsync(string emailNormalizado, string token, CancellationToken cancellationToken = default)
        {
            tokens[emailNormalizado] = token;
            return Task.CompletedTask;
        }

        public string ObterToken(string email) => tokens[email];
    }
}
