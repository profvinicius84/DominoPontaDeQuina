using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Tests;

/// <summary>Regressão determinística das regras introduzidas na Wave 1.</summary>
public class Wave1RegrasTests
{
    [Fact(DisplayName = "Distribui sete pedras para cada jogador e mantém quatorze na cava.")]
    public void Iniciar_DistribuiSetePedrasEConservaCava()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(MontarConjunto(
            new Peca[] { new(6, 1), new(6, 0), new(5, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2) },
            new Peca[] { new(5, 1), new(5, 0), new(4, 1), new(4, 0), new(3, 2), new(3, 1), new(3, 0) }));

        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        Assert.Equal(7, rodada.MaosJogadores.Single(mao => mao.Jogador == jogadorA).Pecas.Count);
        Assert.Equal(7, rodada.MaosJogadores.Single(mao => mao.Jogador == jogadorB).Pecas.Count);
        Assert.Equal(14, rodada.QuantidadeNaCava);
        Assert.Same(jogadorA, rodada.JogadorAtual.Jogador);
    }

    [Fact(DisplayName = "Sem dupla, abre a maior pedra em ordem lexicográfica das faces.")]
    public void Iniciar_SemDupla_AbreMaiorPedraLexicografica()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(MontarConjunto(
            new Peca[] { new(6, 1), new(6, 0), new(5, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2) },
            new Peca[] { new(5, 1), new(5, 0), new(4, 1), new(4, 0), new(3, 2), new(3, 1), new(3, 0) }));

        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        Assert.Same(jogadorA, rodada.JogadorAtual.Jogador); // 6:1 > 5:2 e 6:0
    }

    [Fact(DisplayName = "Uma dupla maior prevalece sobre qualquer pedra não dupla na abertura.")]
    public void Iniciar_ComDuplas_AbreMaiorDupla()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(MontarConjunto(
            new Peca[] { new(5, 5), new(5, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2), new(4, 1) },
            new Peca[] { new(6, 6), new(5, 1), new(5, 0), new(4, 0), new(3, 2), new(3, 1), new(3, 0) }));

        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        Assert.Same(jogadorB, rodada.JogadorAtual.Jogador);
    }

    [Fact(DisplayName = "Jogada válida soma pontos pelas pontas e avança o turno.")]
    public void RegistrarJogada_Valida_PontuaEAvancaTurno()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(MontarConjunto(
            new Peca[] { new(5, 5), new(5, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2), new(4, 1) },
            new Peca[] { new(6, 1), new(6, 0), new(5, 1), new(5, 0), new(4, 0), new(3, 2), new(3, 1) }));
        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        var jogada = rodada.ObterJogadaAtual();
        rodada.RegistrarJogada(jogada);

        Assert.Equal(StatusJogada.Aplicada, jogada.Status);
        Assert.Equal(2, jogadorA.Pontuacao);
        Assert.Same(jogadorB, rodada.JogadorAtual.Jogador);
    }

    [Fact(DisplayName = "Sem encaixe, compra exatamente uma pedra e joga a peça comprada quando compatível.")]
    public void ObterJogadaAtual_SemEncaixe_CompraUmaPeca()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(MontarConjunto(
            new Peca[] { new(6, 6), new(5, 4), new(5, 3), new(5, 2), new(4, 3), new(4, 2), new(4, 1) },
            new Peca[] { new(5, 5), new(5, 1), new(5, 0), new(4, 0), new(3, 2), new(3, 1), new(3, 0) },
            new Peca(6, 0)));
        rodada.Iniciar(new List<Jogador> { jogadorA, jogadorB }.AsReadOnly());

        rodada.RegistrarJogada(rodada.ObterJogadaAtual());
        var jogadaDoJogadorB = rodada.ObterJogadaAtual();

        Assert.False(jogadaDoJogadorB.EhPassarVez());
        Assert.Equal(new Peca(6, 0), jogadaDoJogadorB.Peca);
        Assert.Equal(13, rodada.QuantidadeNaCava);
    }

    private static IReadOnlyCollection<Peca> MontarConjunto(
        IReadOnlyList<Peca> maoA,
        IReadOnlyList<Peca> maoB,
        Peca? primeiraDaCava = null)
    {
        var conjunto = new List<Peca>();
        for (var indice = 0; indice < 7; indice++)
        {
            conjunto.Add(maoA[indice]);
            conjunto.Add(maoB[indice]);
        }

        if (primeiraDaCava is not null)
            conjunto.Add(primeiraDaCava.Value);

        var todas = Enumerable.Range(0, 7)
            .SelectMany(valorA => Enumerable.Range(valorA, 7 - valorA)
                .Select(valorB => new Peca(valorA, valorB)));
        conjunto.AddRange(todas.Where(peca => !conjunto.Contains(peca)));
        return conjunto;
    }
}
