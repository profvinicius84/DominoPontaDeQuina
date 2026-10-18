using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Tests;

/// <summary>Regressões da escolha explícita e obrigatória da pedra de abertura.</summary>
public sealed class AberturaObrigatoriaRodadaTests
{
    [Fact(DisplayName = "Abertura expõe a maior dupla e rejeita outra pedra sem mutar a rodada")]
    public void RegistrarJogada_PrimeiraPedraDiferente_RejeitaSemMutacao()
    {
        var jogadorA = new Jogador(Guid.NewGuid(), "Alice");
        var jogadorB = new Jogador(Guid.NewGuid(), "Bob");
        var rodada = new Rodada(MontarConjunto(
            [new(6, 5), new(6, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2), new(4, 1)],
            [new(6, 6), new(5, 1), new(5, 0), new(3, 2), new(3, 1), new(3, 0), new(2, 1)]));
        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        Assert.Equal(new Peca(6, 6), rodada.PecaAberturaObrigatoria);
        Assert.Equal(jogadorB.Id, rodada.JogadorAtual.Jogador.Id);
        var pedrasAntes = rodada.JogadorAtual.Pecas.ToList();
        var pedraIncorreta = pedrasAntes.First(peca => !peca.Equals(rodada.PecaAberturaObrigatoria!.Value));

        var excecao = Assert.Throws<InvalidOperationException>(() =>
        {
            rodada.RegistrarJogada(new Jogada(jogadorB, pedraIncorreta, lado: LadoTabuleiro.Esquerda));
        });

        Assert.Contains("obrigatória de abertura", excecao.Message);
        Assert.Empty(rodada.HistoricoJogadas);
        Assert.True(rodada.Tabuleiro.EstaVazio);
        Assert.Equal(jogadorB.Id, rodada.JogadorAtual.Jogador.Id);
        Assert.Equal(pedrasAntes, rodada.JogadorAtual.Pecas);

        rodada.RegistrarJogada(new Jogada(jogadorB, rodada.PecaAberturaObrigatoria!.Value, lado: LadoTabuleiro.Esquerda));

        Assert.Single(rodada.HistoricoJogadas);
        Assert.Equal(new Peca(6, 6), rodada.Tabuleiro.Pecas.Single());
        Assert.Equal(jogadorA.Id, rodada.JogadorAtual.Jogador.Id);
    }

    [Fact(DisplayName = "Rodada reidratada não volta a exigir a abertura depois da primeira pedra")]
    public void Reidratar_AposAbertura_PermiteJogadaCompativelSeguinte()
    {
        var jogadorA = new Jogador(Guid.NewGuid(), "Alice");
        var jogadorB = new Jogador(Guid.NewGuid(), "Bob");
        var rodada = Rodada.Reidratar(
            [(jogadorA, (IReadOnlyCollection<Peca>)[new(6, 1), new(3, 3)]), (jogadorB, (IReadOnlyCollection<Peca>)[new(1, 1)])],
            [], [(new Peca(6, 6), LadoTabuleiro.Esquerda)], jogadorA.Id,
            pecaAberturaObrigatoria: new Peca(6, 6));

        rodada.RegistrarJogada(new Jogada(jogadorA, new Peca(6, 1), lado: LadoTabuleiro.Direita));

        Assert.Equal(2, rodada.Tabuleiro.Pecas.Count);
        Assert.Equal(jogadorB.Id, rodada.JogadorAtual.Jogador.Id);
    }

    private static IReadOnlyCollection<Peca> MontarConjunto(IReadOnlyList<Peca> maoA, IReadOnlyList<Peca> maoB)
    {
        var conjunto = new List<Peca>();
        for (var indice = 0; indice < 7; indice++)
        {
            conjunto.Add(maoA[indice]);
            conjunto.Add(maoB[indice]);
        }

        conjunto.AddRange(Enumerable.Range(0, 7)
            .SelectMany(valorA => Enumerable.Range(valorA, 7 - valorA).Select(valorB => new Peca(valorA, valorB)))
            .Where(peca => !conjunto.Contains(peca)));
        return conjunto;
    }
}
