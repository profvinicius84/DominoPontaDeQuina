namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa uma partida persistida e seu histórico de atividade.</summary>
public class Partida
{
    /// <summary>Obtém ou define o identificador da partida.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define a pontuação necessária para vencer.</summary>
    public int PontuacaoAlvo { get; set; }
    /// <summary>Obtém ou define o status atual da partida.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Obtém ou define a data de criação da partida em UTC.</summary>
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Obtém ou define a versão monotônica do estado. A comparação e o incremento
    /// atômicos são responsabilidade da persistência e da Application.
    /// </summary>
    public long VersaoEstado { get; set; }
    /// <summary>Obtém ou define o jogador vencedor quando a partida for encerrada.</summary>
    public Guid? VencedorJogadorId { get; set; }
    /// <summary>Obtém ou define o instante de encerramento em UTC, quando aplicável.</summary>
    public DateTime? EncerradaEmUtc { get; set; }
    /// <summary>Obtém as participações registradas na partida.</summary>
    public ICollection<ParticipacaoPartida> Participacoes { get; set; } = [];
    /// <summary>Obtém os lances registrados na partida.</summary>
    public ICollection<Lance> Lances { get; set; } = [];
    /// <summary>Obtém as rodadas persistidas em ordem de criação.</summary>
    public ICollection<RodadaPartida> Rodadas { get; set; } = [];
    /// <summary>Obtém as tentativas de ação recebidas pelo servidor.</summary>
    public ICollection<AcaoPartida> Acoes { get; set; } = [];

    /// <summary>Adiciona um participante, preservando o limite da partida individual.</summary>
    public void AdicionarParticipacao(ParticipacaoPartida participacao)
    {
        ArgumentNullException.ThrowIfNull(participacao);

        if (participacao.JogadorId == Guid.Empty || participacao.Posicao is < 1 or > 2)
            throw new InvalidOperationException("Participação deve informar jogador e posição válida.");
        if (Participacoes.Any(item => item.JogadorId == participacao.JogadorId))
            throw new InvalidOperationException("O jogador já participa desta partida.");
        if (Participacoes.Any(item => item.Posicao == participacao.Posicao))
            throw new InvalidOperationException("A posição da partida já está ocupada.");
        if (Participacoes.Count >= 2)
            throw new InvalidOperationException("Uma partida individual aceita exatamente dois jogadores.");

        participacao.PartidaId = Id;
        Participacoes.Add(participacao);
    }

    /// <summary>Adiciona uma rodada mantendo sua numeração contínua.</summary>
    public void AdicionarRodada(RodadaPartida rodada)
    {
        ArgumentNullException.ThrowIfNull(rodada);

        if (rodada.Numero != Rodadas.Count + 1)
            throw new InvalidOperationException("A numeração da rodada deve ser contínua.");

        rodada.PartidaId = Id;
        Rodadas.Add(rodada);
    }

    /// <summary>Registra uma tentativa recebida mantendo a ordem interna da partida.</summary>
    public void RegistrarAcao(AcaoPartida acao)
    {
        ArgumentNullException.ThrowIfNull(acao);
        acao.ValidarCoerencia();

        if (acao.PartidaId != Id)
            throw new InvalidOperationException("A ação deve pertencer à partida informada.");
        if (Acoes.Any(item => item.OrdemRecebimento >= acao.OrdemRecebimento))
            throw new InvalidOperationException("A ordem de recebimento deve ser estritamente crescente na partida.");

        Acoes.Add(acao);
    }
}
