using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Tests;

public sealed class PontuacaoDuplaExpostaTests
{
    [Fact(DisplayName = "Jogada com 3 e 6/6 nas quinas concede três pontos")]
    public void Jogada_DuplaExposta_ConcedeTresPontos()
    {
        var alice = new Jogador("Alice");
        var bob = new Jogador("Bob");
        IReadOnlyCollection<(Jogador Jogador, IReadOnlyCollection<Peca> Mao)> maos =
        [
            (alice, new Peca[] { new(6, 6), new(2, 4) }),
            (bob, new Peca[] { new(1, 3) })
        ];
        var rodada = Rodada.Reidratar(maos, Array.Empty<Peca>(),
            [(new Peca(3, 6), LadoTabuleiro.Direita)], alice.Id);

        rodada.RegistrarJogada(new Jogada(alice, new Peca(6, 6), lado: LadoTabuleiro.Direita));

        Assert.Equal(15, rodada.Tabuleiro.SomarPontasExternas());
        Assert.Equal(3, alice.Pontuacao);
        Assert.Equal(0, bob.Pontuacao);
    }
}
