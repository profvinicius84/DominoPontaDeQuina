using DominoPontaDeQuina.Core.Interfaces;
using DominoPontaDeQuina.Core.Enums;

namespace DominoPontaDeQuina.Core.Models;

/// <inheritdoc cref="IMaoJogador"/>
public class MaoJogador(Jogador jogador) : IMaoJogador
{
    /// <summary>
    /// Obtem as pecas atualmente armazenadas na mao do jogador.
    /// </summary>
    List<Peca> _pecas = [];
    List<Peca> _pecasReservadas = [];

    /// <summary>Obtém uma visão somente leitura das peças atualmente na mão.</summary>
    public IReadOnlyCollection<Peca> Pecas => _pecas.AsReadOnly();

    /// <inheritdoc />
    public Jogador Jogador { get; } = jogador ?? throw new ArgumentNullException(nameof(jogador));

    /// <inheritdoc />
    public void AdicionarPeca(Peca peca) => _pecas.Add(peca);

    /// <inheritdoc />
    public int SomarPecasNaMao() => _pecas.Sum(peca => peca.SomaValores);

    /// <summary>Indica se a peça está disponível ou reservada por uma jogada pendente da própria mão.</summary>
    public bool PossuiPeca(Peca peca) => _pecas.Contains(peca) || _pecasReservadas.Contains(peca);

    /// <summary>Confirma a retirada de uma peça após a validação integral da jogada.</summary>
    public bool AplicarJogada(Jogada jogada)
    {
        if (jogada.Jogador.Id != Jogador.Id || jogada.Peca is null)
            return false;

        return _pecasReservadas.Remove(jogada.Peca.Value) || _pecas.Remove(jogada.Peca.Value);
    }

    /// <inheritdoc />
    public bool PossuiSena() => _pecas.Any(peca => peca.EhSena);

    /// <inheritdoc />
    public bool EstaSemPecas() => _pecas.Count == 0;

    /// <inheritdoc />
    public Jogada GetJogada(Tabuleiro tabuleiro)
    {
        ArgumentNullException.ThrowIfNull(tabuleiro);

        var encontrada = _pecas
            .Select(peca => (Peca: (Peca?)peca, Lado: LadoTabuleiro.Esquerda))
            .FirstOrDefault(candidata => tabuleiro.PodeColar(candidata.Peca!.Value, candidata.Lado));

        if (encontrada.Peca is null)
        {
            encontrada = _pecas
                .Select(peca => (Peca: (Peca?)peca, Lado: LadoTabuleiro.Direita))
                .FirstOrDefault(candidata => tabuleiro.PodeColar(candidata.Peca!.Value, candidata.Lado));
        }

        if (encontrada.Peca is null)
            return new Jogada(Jogador);

        _pecas.Remove(encontrada.Peca.Value);
        _pecasReservadas.Add(encontrada.Peca.Value);
        return new Jogada(Jogador, encontrada.Peca.Value, lado: encontrada.Lado);
    }

    /// <inheritdoc />
    public void DefazerJogada(Jogada jogada)
    {
        ArgumentNullException.ThrowIfNull(jogada);

        if (jogada.Jogador.Id != Jogador.Id || jogada.Peca is null)
            return;

        _pecasReservadas.Remove(jogada.Peca.Value);
        if (!_pecas.Contains(jogada.Peca.Value))
            _pecas.Add(jogada.Peca.Value);
    }
}
