using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;
using System.Collections.ObjectModel;

namespace DominoPontaDeQuina.Core;

/// <summary>
/// Controla o fluxo principal no topo da hierarquia Partida -> Rodadas -> Jogadas.
/// Neste nivel fica a orquestracao da partida atual, da sequencia de rodadas e da execucao das jogadas.
/// </summary>
public class Jogo()
{
    /// <summary>
    /// Mantem internamente o historico das partidas iniciadas pelo jogo.
    /// </summary>
    Stack<Partida> _partidas = [];

    /// <summary>
    /// Obtem o historico das partidas controladas por esta instancia.
    /// </summary>
    public ReadOnlyCollection<Partida> HistoricoPartidas => _partidas.ToList().AsReadOnly();

    /// <summary>
    /// Obtem a partida atual controlada pelo jogo.
    /// </summary>
    public Partida? PartidaAtual => _partidas.TryPeek(out var partidaAtual) ? partidaAtual : null;

    /// <summary>
    /// Registra os times da partida atual.
    /// </summary>
    public Task RegistrarTimesAsync()
    {
        // TODO ALUNO: registrar os times e jogadores da partida antes do inicio da primeira rodada.
        throw new NotImplementedException();
    }

    /// <summary>
    /// Inicia uma nova partida e executa suas rodadas ate a finalizacao.
    /// </summary>
    public async Task IniciarNovaPartida()
    {
        if (PartidaAtual?.Status is StatusPartida.EmAndamento)
            throw new InvalidOperationException("Nao e possivel iniciar uma nova partida enquanto a partida atual estiver em andamento.");

        _partidas.Push(new());

        await RegistrarTimesAsync();

        PartidaAtual.IniciarNovaRodada();

        while (PartidaAtual.Status is StatusPartida.EmAndamento)
        {
            await ExecutarRodadaPartidaAsync();

            if (PartidaAtual.VerificaPontuacaoAlvoAtingida())
            {
                PartidaAtual.FinalizarPartida();
            }
            else
            {
                PartidaAtual.IniciarNovaRodada();
            }
        }
    }

    /// <summary>
    /// Executa o fluxo da rodada atual enquanto ela estiver em andamento.
    /// </summary>
    public async Task ExecutarRodadaPartidaAsync()
    {
        if (PartidaAtual?.Status is not StatusPartida.EmAndamento)
            return;
        if (PartidaAtual.RodadaAtual?.Status is not StatusRodada.EmAndamento)
            return;

        var rodadaAtual = PartidaAtual.RodadaAtual;

        while (rodadaAtual.Status is StatusRodada.EmAndamento)
        {
            await ExecutarJogadaAsync();
            rodadaAtual.VerificarBatida();
            rodadaAtual.VerificarTabuleiroTravado();
        }
    }

    /// <summary>
    /// Executa a jogada do jogador atual na rodada em andamento.
    /// </summary>
    public async Task ExecutarJogadaAsync()
    {
        if (PartidaAtual?.Status is not StatusPartida.EmAndamento)
            throw new InvalidOperationException("Nao e possivel executar uma jogada em uma partida que nao esta em andamento.");
        if (PartidaAtual.RodadaAtual?.Status is not StatusRodada.EmAndamento)
            throw new InvalidOperationException("Nao e possivel executar uma jogada em uma rodada que nao esta em andamento.");

        var jogada = await GetJogadaAsync();

        if (!ValidarJogada(jogada))
        {
            PartidaAtual.RodadaAtual.JogadorAtual.DefazerJogada(jogada);
            jogada.MarcarComoInvalida();
            throw new InvalidOperationException("A jogada realizada e invalida.");
        }

        PartidaAtual.RodadaAtual.RegistrarJogada(jogada);
    }

    /// <summary>
    /// Obtem a jogada definida pelo jogador atual com base no estado do tabuleiro.
    /// </summary>
    /// <returns>A jogada escolhida pelo jogador atual.</returns>
    public Task<Jogada> GetJogadaAsync()
    {
        if (PartidaAtual?.RodadaAtual is null)
            throw new InvalidOperationException("Nao ha rodada atual para obter jogada.");

        return Task.FromResult(PartidaAtual.RodadaAtual.ObterJogadaAtual());
    }

    /// <summary>
    /// Valida a jogada no contexto da rodada atual.
    /// </summary>
    /// <param name="jogada">A jogada a ser validada.</param>
    /// <returns><see langword="true"/> quando a jogada for valida; caso contrario, <see langword="false"/>.</returns>
    public bool ValidarJogada(Jogada jogada)
    {
        if (PartidaAtual?.RodadaAtual?.Status is not StatusRodada.EmAndamento || jogada is null)
            return false;

        var rodada = PartidaAtual.RodadaAtual;
        if (jogada.Jogador.Id != rodada.JogadorAtual.Jogador.Id)
            return false;
        if (jogada.EhPassarVez())
            return rodada.QuantidadeNaCava == 0
                && !rodada.JogadorAtual.Pecas.Any(peca => rodada.Tabuleiro.PodeColar(peca, LadoTabuleiro.Esquerda)
                    || rodada.Tabuleiro.PodeColar(peca, LadoTabuleiro.Direita));

        return jogada.Peca is not null
            && jogada.Lado is not null
            && rodada.JogadorAtual.PossuiPeca(jogada.Peca.Value)
            && rodada.Tabuleiro.PodeColar(jogada.Peca.Value, jogada.Lado.Value);
    }

    /// <summary>
    /// Obtem os jogadores registrados nos times da partida atual.
    /// </summary>
    /// <returns>A colecao somente leitura dos jogadores da partida.</returns>
    private ReadOnlyCollection<Jogador> ObterJogadoresDaPartida()
    {
        if (PartidaAtual is null)
            throw new InvalidOperationException("Nao ha partida atual para obter jogadores.");

        return PartidaAtual.Jogadores.AsReadOnly();
    }

    /// <summary>
    /// Obtem a rodada anterior a rodada atual, quando houver.
    /// </summary>
    /// <returns>A rodada anterior, ou <see langword="null"/> quando a rodada atual for a primeira da partida.</returns>
    private Rodada? ObterRodadaAnterior()
    {
        if (PartidaAtual is null || PartidaAtual.HistoricoRodadas.Count < 2)
            return null;

        return PartidaAtual.HistoricoRodadas[1];
    }
}
