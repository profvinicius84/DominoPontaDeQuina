using DominoPontaDeQuina.Application.Jogadores;

namespace DominoPontaDeQuina.Application.Partidas.Contracts;

/// <summary>Representa uma pedra do contrato de aplicação, sem orientação implícita de interface.</summary>
public sealed record PecaPartida(int ValorA, int ValorB);

/// <summary>Indica a ponta do tabuleiro escolhida pelo participante.</summary>
public enum LadoJogavel
{
    /// <summary>Ponta esquerda.</summary>
    Esquerda,

    /// <summary>Ponta direita.</summary>
    Direita
}

/// <summary>Solicita a criação de uma partida por uma identidade autenticada.</summary>
public sealed record CriarPartidaComando(Guid SolicitanteId, int PontuacaoAlvo, AcessoJogador? Acesso = null, string? Webhook = null);

/// <summary>Solicita o ingresso de um jogador em uma partida.</summary>
public sealed record IngressarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null, string? Webhook = null);

/// <summary>Solicita o início da partida pelos participantes autorizados.</summary>
public sealed record IniciarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Solicita uma jogada com peça, lado, versão observada e chave de repetição.</summary>
public sealed record JogarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    PecaPartida Peca,
    LadoJogavel Lado,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Solicita a compra única obrigatória antes de uma eventual passagem.</summary>
public sealed record ComprarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Solicita a passagem de vez quando permitida pela regra do motor.</summary>
public sealed record PassarVezPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Solicita o cancelamento voluntário de uma partida ainda aberta.</summary>
public sealed record CancelarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Encerra a partida atual e inicia outra com os mesmos participantes e placar zerado.</summary>
public sealed record ReiniciarPartidaComando(
    Guid PartidaId,
    Guid SolicitanteId,
    long VersaoEsperada,
    Guid ChaveIdempotencia, AcessoJogador? Acesso = null);

/// <summary>Consulta o estado permitido ao próprio participante.</summary>
public sealed record ConsultarEstadoProprioConsulta(Guid PartidaId, Guid SolicitanteId, AcessoJogador? Acesso = null);

/// <summary>Consulta a nova partida iniciada a partir da partida informada, quando houver.</summary>
public sealed record ConsultarReinicioConsulta(Guid PartidaId, Guid SolicitanteId, AcessoJogador? Acesso = null);

/// <summary>Consulta a projeção pública para acompanhamento sem mãos privadas.</summary>
public sealed record ConsultarObservacaoConsulta(Guid PartidaId);

/// <summary>Consulta o resultado e o histórico permitido da partida.</summary>
public sealed record ConsultarResultadoHistoricoConsulta(Guid PartidaId, Guid SolicitanteId, AcessoJogador? Acesso = null);
