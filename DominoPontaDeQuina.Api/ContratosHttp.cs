using DominoPontaDeQuina.Application.Partidas.Contracts;

public sealed record SolicitarTokenRequest(string Email);
public sealed record CriarPartidaRequest(int PontuacaoAlvo, Guid JogadorId, string? Webhook);
public sealed record IngressarRequest(long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId, string? Webhook);
public sealed record AcaoRequest(long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId);
public sealed record JogarRequest(int ValorA, int ValorB, LadoJogavel Lado, long VersaoEsperada, Guid ChaveIdempotencia, Guid JogadorId);
public sealed record RespostaErro(string Codigo, EstadoPartidaParticipante? EstadoAutoritativo = null);
public sealed record SessaoResponse(string UsuarioId);
