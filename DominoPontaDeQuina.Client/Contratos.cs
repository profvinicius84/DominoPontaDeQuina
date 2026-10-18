namespace DominoPontaDeQuina.Client;

public sealed record PerfilJogador(Guid JogadorId, string Nome, bool Ativo, DateTime? AtivadoEmUtc);
public sealed record PartidaDisponivel(Guid PartidaId, int PontuacaoAlvo, long VersaoEstado);

public enum SituacaoPartida { AguardandoInicio, EmAndamento, Encerrada }
public enum LadoJogavel { Esquerda, Direita }
public enum ProximaAcao { Aguardar, JogarAbertura, Jogar, Comprar, Passar, Encerrada }
public sealed record EventoEstado(Guid EventoId, Guid PartidaId, Guid ParticipacaoId, long VersaoEstado,
    string Tipo, DateTime OcorridoEmUtc, EstadoPartida Estado);
public sealed record Pedra(int ValorA, int ValorB)
{
    public override string ToString() => $"{ValorA} | {ValorB}";
}
public sealed record Participante(Guid JogadorId, string Nome, int Pontuacao, int QuantidadePedrasNaMao, bool EhVez);
public sealed record EstadoPartida(Guid PartidaId, long VersaoEstado, SituacaoPartida Situacao,
    int NumeroRodada, Guid? JogadorDoTurnoId, IReadOnlyList<Pedra> MinhaMao,
    IReadOnlyList<Pedra> Tabuleiro, IReadOnlyList<Participante> Participantes, Guid? VencedorId,
    ProximaAcao ProximaAcao = ProximaAcao.Aguardar, Pedra? PedraAberturaObrigatoria = null, Guid ParticipacaoId = default);
// A visão do painel deliberadamente não possui campo de mão privada.
public sealed record ObservacaoPartida(Guid PartidaId, long VersaoEstado, SituacaoPartida Situacao,
    int NumeroRodada, IReadOnlyList<Pedra> Tabuleiro, IReadOnlyList<Participante> Participantes, Guid? VencedorId);
public sealed record ConfirmacaoAcao(Guid ChaveIdempotencia, bool AcaoIdempotente, EstadoPartida Estado);
public sealed record Evento(Guid EventoId, Guid JogadorId, Pedra? PecaJogada, LadoJogavel? Lado,
    bool PassouVez, int PontosConcedidos, DateTime RecebidoEmUtc);
public sealed record ResultadoPartida(Guid PartidaId, SituacaoPartida Situacao, long VersaoEstado,
    Guid? VencedorId, IReadOnlyList<Participante> Participantes, IReadOnlyList<Evento> Eventos);
public sealed record Sessao(Guid UsuarioId);
