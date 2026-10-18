using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Interfaces;
using System.Collections.ObjectModel;

namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa o nivel de partida na hierarquia Partida -> Rodadas -> Jogadas.
/// Neste nivel ficam o estado global, os dois jogadores participantes e o historico das rodadas.
/// </summary>
/// <param name="pontuacaoAlvo">
/// Define a pontuacao minima que um jogador deve atingir para encerrar a partida como vencedor.
/// </param>
public class Partida(int pontuacaoAlvo = 50) : IPartida
{
    /// <summary>
    /// Armazena as rodadas registradas neste nivel da hierarquia.
    /// </summary>
    Stack<Rodada> _rodadas = [];

    /// <inheritdoc />
    public int PontuacaoAlvo { get; } = pontuacaoAlvo;

    /// <inheritdoc />
    public StatusPartida Status { get; protected set; } = StatusPartida.NaoIniciada;

    /// <summary>Obtém os dois participantes individuais da partida, na ordem de participação.</summary>
    public List<Jogador> Jogadores { get; } = [];

    /// <summary>
    /// Compatibilidade temporária para o exercício histórico. O motor da partida não usa times.
    /// </summary>
    public List<Time> Times { get; } = [];

    /// <inheritdoc />
    public ReadOnlyCollection<Rodada> HistoricoRodadas => _rodadas.ToList().AsReadOnly();

    /// <inheritdoc />
    public Rodada? RodadaAtual => _rodadas.TryPeek(out var rodada) ? rodada : null;

    /// <summary>Registra um participante individual, limitado a dois jogadores distintos.</summary>
    public void AdicionarJogador(Jogador jogador)
    {
        ArgumentNullException.ThrowIfNull(jogador);

        if (Jogadores.Any(participante => participante.Id == jogador.Id))
            throw new InvalidOperationException("O jogador já participa da partida.");
        if (Jogadores.Count == 2)
            throw new InvalidOperationException("Uma partida comporta exatamente dois jogadores.");

        Jogadores.Add(jogador);
    }

    /// <summary>Obtém a pontuação acumulada dos participantes individuais.</summary>
    public Dictionary<Jogador, int> GetPontuacaoJogadores() =>
        Jogadores.ToDictionary(jogador => jogador, jogador => jogador.Pontuacao);

    /// <summary>Obtém o jogador que alcançou a pontuação alvo, quando houver.</summary>
    public Jogador? GetJogadorVencedor() =>
        Jogadores.FirstOrDefault(jogador => jogador.Pontuacao >= PontuacaoAlvo);

    /// <inheritdoc />
    public Dictionary<Time, int> GetPontuacaoTimes()
    {
        return Times.ToDictionary(time => time, time => time.Pontuacao);
    }

    /// <inheritdoc />
    public Time? GetTimeVencedor()
    {
        return Times.FirstOrDefault(time => time.Pontuacao >= PontuacaoAlvo);
    }

    /// <inheritdoc />
    public bool VerificaPontuacaoAlvoAtingida()
    {
        return Jogadores.Any(jogador => jogador.Pontuacao >= PontuacaoAlvo)
            || Times.Any(time => time.Pontuacao >= PontuacaoAlvo);
    }

    /// <inheritdoc />
    public void IniciarNovaRodada()
    {
        if (Status is StatusPartida.Finalizada)
            throw new InvalidOperationException("Não é possível iniciar uma nova rodada em uma partida finalizada.");

        if (RodadaAtual?.Status is StatusRodada.EmAndamento)
            throw new InvalidOperationException("Não é possível iniciar uma rodada enquanto a rodada atual está em andamento.");

        if (Jogadores.Count != 2)
            throw new InvalidOperationException("Uma rodada exige exatamente dois jogadores participantes.");

        Status = StatusPartida.EmAndamento;
        var rodada = new Rodada();
        rodada.Iniciar(Jogadores.AsReadOnly(), RodadaAtual);
        _rodadas.Push(rodada);
    }

    /// <inheritdoc />
    public void FinalizarPartida()
    {
        if (Status is not StatusPartida.EmAndamento)
            throw new InvalidOperationException("Não é possível finalizar uma partida que não está em andamento.");
        if (VerificaPontuacaoAlvoAtingida())
            Status = StatusPartida.Finalizada;
    }
}
