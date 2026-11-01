using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DominoPontaDeQuina.Client;

/// <summary>Cliente independente: conhece apenas JSON, rotas e códigos HTTP públicos.</summary>
public sealed class DominoApiClient(HttpClient http)
{
    private Guid jogadorId;
    private string? chaveAtivacao;
    private string? webhook;

    /// <summary>Seleciona o perfil e a prova de ativação deste cliente para as próximas requisições.</summary>
    public void SelecionarJogador(Guid id, string chave, string? enderecoWebhook = null)
    {
        if (id == Guid.Empty || chave.Length != 64 || !chave.All(Uri.IsHexDigit))
            throw new ArgumentException("Informe jogador e chave de ativação válidos.");
        jogadorId = id;
        chaveAtivacao = chave;
        webhook = enderecoWebhook;
    }

    public static string GerarChaveAtivacao() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    public Task<IReadOnlyList<PerfilJogador>> ListarJogadoresAsync(string token, CancellationToken ct = default) =>
        EnviarAsync<IReadOnlyList<PerfilJogador>>(HttpMethod.Get, "api/jogadores", token, null, ct);
    public Task<IReadOnlyList<PartidaDisponivel>> ListarPartidasAsync(string token, CancellationToken ct = default) =>
        EnviarAsync<IReadOnlyList<PartidaDisponivel>>(HttpMethod.Get, "api/partidas", token, null, ct);
    public Task<PerfilJogador> CriarJogadorAsync(string token, string nome, CancellationToken ct = default) =>
        EnviarAsync<PerfilJogador>(HttpMethod.Post, "api/jogadores", token, new { nome }, ct);
    public Task<PerfilJogador> AtivarJogadorAsync(string token, Guid id, string chave, CancellationToken ct = default) =>
        EnviarAsync<PerfilJogador>(HttpMethod.Post, $"api/jogadores/{id}/ativar-cliente", token, new { chaveAtivacao = chave }, ct);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Uri ValidarEndereco(string endereco)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("Use HTTPS; HTTP é permitido somente em localhost.");
        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }

    public Task SolicitarTokenAsync(string email, string senha, CancellationToken ct = default) =>
        SolicitarTokenAsync(email, ct);
    public Task SolicitarTokenAsync(string email, CancellationToken ct = default) =>
        EnviarSemRespostaAsync(HttpMethod.Post, "api/identidade/token", null, new { email }, ct);
    public Task<PerfilJogador> LiberarJogadorAsync(string token, CancellationToken ct = default) =>
        EnviarAsync<PerfilJogador>(HttpMethod.Delete, $"api/jogadores/{jogadorId}/desativar-cliente", token, null, ct);
    public Task<Sessao> SessaoAsync(string token, CancellationToken ct = default) =>
        EnviarAsync<Sessao>(HttpMethod.Get, "api/identidade/sessao", token, null, ct);
    public Task<EstadoPartida> CriarAsync(string token, int pontuacaoAlvo, CancellationToken ct = default) =>
        EnviarAsync<EstadoPartida>(HttpMethod.Post, "api/partidas", token,
            webhook is null ? new { pontuacaoAlvo, jogadorId } : (object)new { pontuacaoAlvo, jogadorId, webhook }, ct);
    public Task<EstadoPartida> EstadoAsync(string token, Guid partidaId, CancellationToken ct = default) =>
        EnviarAsync<EstadoPartida>(HttpMethod.Get, $"api/partidas/{partidaId}?jogadorId={jogadorId}", token, null, ct);
    public Task<EstadoPartida> ReinicioAsync(string token, Guid partidaId, CancellationToken ct = default) =>
        EnviarAsync<EstadoPartida>(HttpMethod.Get, $"api/partidas/{partidaId}/reinicio?jogadorId={jogadorId}", token, null, ct);
    public async Task<EstadoPartida?> AtualizacoesAsync(string token, Guid partidaId, long versaoConhecida, CancellationToken ct = default)
    {
        if (versaoConhecida < 0) throw new ArgumentOutOfRangeException(nameof(versaoConhecida));
        using var resposta = await EnviarAsync(HttpMethod.Get,
            $"api/partidas/{partidaId}/atualizacoes?jogadorId={jogadorId}&versaoConhecida={versaoConhecida}", token, null, ct);
        if (resposta.StatusCode == HttpStatusCode.NoContent) return null;
        try
        {
            return await resposta.Content.ReadFromJsonAsync<EstadoPartida>(Json, ct)
                ?? throw new ApiException(502, "RESPOSTA_INVALIDA");
        }
        catch (JsonException) { throw new ApiException(502, "RESPOSTA_INVALIDA"); }
    }
    public Task<ObservacaoPartida> ObservarAsync(string token, Guid partidaId, CancellationToken ct = default) =>
        EnviarAsync<ObservacaoPartida>(HttpMethod.Get, $"api/partidas/{partidaId}/visualizar", token, null, ct);
    public Task<ConfirmacaoAcao> AcaoAsync(string token, Guid partidaId, string acao, long versaoEsperada,
        Guid chaveIdempotencia, CancellationToken ct = default)
    {
        if (acao is not ("entrar" or "iniciar" or "passar" or "comprar" or "cancelar" or "reiniciar"))
            throw new ArgumentException("Ação desconhecida.", nameof(acao));
        return EnviarAsync<ConfirmacaoAcao>(HttpMethod.Post, $"api/partidas/{partidaId}/{acao}", token,
            acao == "entrar" && webhook is not null
                ? new { versaoEsperada, chaveIdempotencia, jogadorId, webhook }
                : (object)new { versaoEsperada, chaveIdempotencia, jogadorId }, ct);
    }
    public Task<ConfirmacaoAcao> JogarAsync(string token, Guid partidaId, Pedra pedra, LadoJogavel lado,
        long versaoEsperada, Guid chaveIdempotencia, CancellationToken ct = default) =>
        EnviarAsync<ConfirmacaoAcao>(HttpMethod.Post, $"api/partidas/{partidaId}/jogar", token,
            new { pedra.ValorA, pedra.ValorB, lado = (int)lado, versaoEsperada, chaveIdempotencia, jogadorId }, ct);
    public Task<ResultadoPartida> ResultadoAsync(string token, Guid partidaId, CancellationToken ct = default) =>
        EnviarAsync<ResultadoPartida>(HttpMethod.Get, $"api/partidas/{partidaId}/resultado?jogadorId={jogadorId}", token, null, ct);

    private async Task EnviarSemRespostaAsync(HttpMethod metodo, string rota, string? token, object? corpo, CancellationToken ct)
    {
        using var resposta = await EnviarAsync(metodo, rota, token, corpo, ct);
    }
    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string rota, string? token, object? corpo, CancellationToken ct)
    {
        using var resposta = await EnviarAsync(metodo, rota, token, corpo, ct);
        try
        {
            return await resposta.Content.ReadFromJsonAsync<T>(Json, ct)
                ?? throw new ApiException(502, "RESPOSTA_INVALIDA");
        }
        catch (JsonException) { throw new ApiException(502, "RESPOSTA_INVALIDA"); }
    }
    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string rota, string? token, object? corpo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(metodo, rota);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        if (chaveAtivacao is not null && (rota.StartsWith("api/partidas", StringComparison.Ordinal) || metodo == HttpMethod.Delete))
            request.Headers.Add("X-Jogador-Ativacao", chaveAtivacao);
        if (corpo is not null) request.Content = JsonContent.Create(corpo);
        HttpResponseMessage resposta;
        try { resposta = await http.SendAsync(request, ct); }
        catch (HttpRequestException) { throw new ApiException(503, "SERVICO_INDISPONIVEL"); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new ApiException(503, "TEMPO_ESGOTADO"); }
        if (resposta.IsSuccessStatusCode) return resposta;
        using (resposta)
        {
            var codigo = resposta.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "ACESSO_INVALIDO",
                HttpStatusCode.Forbidden => "SEM_PERMISSAO",
                HttpStatusCode.NotFound => "PARTIDA_INDISPONIVEL",
                HttpStatusCode.Conflict => "CONFLITO_DE_ESTADO",
                HttpStatusCode.TooManyRequests => "LIMITE_EXCEDIDO",
                _ => "REQUISICAO_REJEITADA"
            };
            EstadoPartida? estado = null;
            try
            {
                using var documento = await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                if (documento.RootElement.TryGetProperty("codigo", out var campo))
                {
                    var valor = campo.ToString();
                    // Nunca exibe mensagens ou conteúdo arbitrário da resposta (pode conter segredos).
                    if (valor.Length is > 0 and < 80 && valor.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) codigo = valor;
                }
                if (documento.RootElement.TryGetProperty("estadoAutoritativo", out var atual) && atual.ValueKind == JsonValueKind.Object)
                    estado = atual.Deserialize<EstadoPartida>(Json);
            }
            catch (JsonException) { }
            throw new ApiException((int)resposta.StatusCode, codigo, estado);
        }
    }
}

public sealed class ApiException(int status, string codigo, EstadoPartida? estado = null)
    : Exception($"{codigo} (HTTP {status})")
{
    public int Status { get; } = status;
    public string Codigo { get; } = codigo;
    public EstadoPartida? EstadoAutoritativo { get; } = estado;
}
