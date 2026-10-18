using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Core.Tests;

[Trait("Categoria", "Gap")]
public class PartidaGapTests
{
    /// <summary>
    /// <b>Objetivo:</b> Validar retorno da pontuação acumulada por jogador na partida.
    /// </summary>
    [Fact(DisplayName = "Deve retornar pontuação dos jogadores da partida.")]
    public void GetPontuacaoJogadores_DeveRetornarPontuacaoDosParticipantes()
    {
        var partida = new Partida();
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");

        jogadorA.SomarPontos(12);
        jogadorB.SomarPontos(7);

        partida.AdicionarJogador(jogadorA);
        partida.AdicionarJogador(jogadorB);

        var pontuacoes = partida.GetPontuacaoJogadores();

        Assert.Equal(2, pontuacoes.Count);
        Assert.Equal(12, pontuacoes[jogadorA]);
        Assert.Equal(7, pontuacoes[jogadorB]);
    }

    /// <summary>
    /// <b>Objetivo:</b> Validar detecção de alcance da pontuação alvo por algum jogador.
    /// <br/><b>Critério:</b> Deve retornar true quando ao menos um time atingir o alvo.
    /// </summary>
    [Fact(DisplayName = "Deve retornar true quando algum jogador atingir a pontuação alvo.")]
    public void VerificaPontuacaoAlvoAtingida_DeveRetornarTrue_QuandoAlgumJogadorAtingirAlvo()
    {
        var partida = new Partida(pontuacaoAlvo: 15);
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");

        jogadorA.SomarPontos(10);
        jogadorB.SomarPontos(15);

        partida.AdicionarJogador(jogadorA);
        partida.AdicionarJogador(jogadorB);

        var pontuacaoAtingida = partida.VerificaPontuacaoAlvoAtingida();

        Assert.True(pontuacaoAtingida);
    }

    /// <summary>
    /// <b>Objetivo:</b> Validar seleção do jogador vencedor com base na pontuação alvo.
    /// <br/><b>Critério:</b> Deve retornar o time que atingiu primeiro ou corretamente o alvo.
    /// </summary>
    [Fact(DisplayName = "Deve retornar o jogador vencedor ao atingir pontuação alvo.")]
    public void GetJogadorVencedor_DeveRetornarJogadorQueAtingiuPontuacaoAlvo()
    {
        var partida = new Partida(pontuacaoAlvo: 20);
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");

        jogadorA.SomarPontos(20);
        jogadorB.SomarPontos(18);

        partida.AdicionarJogador(jogadorA);
        partida.AdicionarJogador(jogadorB);

        var vencedor = partida.GetJogadorVencedor();

        Assert.Same(jogadorA, vencedor);
    }
}
