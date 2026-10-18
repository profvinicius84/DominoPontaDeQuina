using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Tests;

/// <summary>Garante a compra única sem revelar a cava ao consumidor do motor.</summary>
public sealed class CompraRodadaTests
{
    [Fact(DisplayName = "Compra que cria encaixe mantém turno e pedra na mão")]
    public void ComprarOuPassar_QuandoPedraPermiteJogada_MantemTurno()
    {
        var jogadorA = new Jogador(Guid.NewGuid(), "Alice");
        var jogadorB = new Jogador(Guid.NewGuid(), "Bob");
        var rodada = Rodada.Reidratar(
            [(jogadorA, (IReadOnlyCollection<Peca>)[new(1, 1)]), (jogadorB, (IReadOnlyCollection<Peca>)[new(0, 0)])],
            [new Peca(6, 2)], [(new Peca(6, 6), LadoTabuleiro.Esquerda)], jogadorA.Id);

        var passou = rodada.ComprarOuPassar(jogadorA);

        Assert.False(passou);
        Assert.Equal(jogadorA.Id, rodada.JogadorAtual.Jogador.Id);
        Assert.Equal(2, rodada.JogadorAtual.Pecas.Count);
        Assert.Equal(0, rodada.QuantidadeNaCava);
        Assert.Empty(rodada.HistoricoJogadas);
    }

    [Fact(DisplayName = "Compra única sem encaixe passa o turno mesmo com cava restante")]
    public void ComprarOuPassar_QuandoPedraNaoPermiteJogada_RegistraPassagem()
    {
        var jogadorA = new Jogador(Guid.NewGuid(), "Alice");
        var jogadorB = new Jogador(Guid.NewGuid(), "Bob");
        var rodada = Rodada.Reidratar(
            [(jogadorA, (IReadOnlyCollection<Peca>)[new(1, 1)]), (jogadorB, (IReadOnlyCollection<Peca>)[new(0, 0)])],
            [new Peca(2, 3), new Peca(4, 5)], [(new Peca(6, 6), LadoTabuleiro.Esquerda)], jogadorA.Id);

        Assert.Throws<InvalidOperationException>(() => rodada.RegistrarJogada(new Jogada(jogadorA)));

        var passou = rodada.ComprarOuPassar(jogadorA);

        Assert.True(passou);
        Assert.Equal(1, rodada.QuantidadeNaCava);
        Assert.Equal(jogadorB.Id, rodada.JogadorAtual.Jogador.Id);
        Assert.Equal(2, rodada.MaosJogadores.Single(m => m.Jogador.Id == jogadorA.Id).Pecas.Count);
        Assert.True(rodada.HistoricoJogadas.Single().EhPassarVez());
    }
}
