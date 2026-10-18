using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;
using System.Reflection;

namespace DominoPontaDeQuina.Core.Tests;

public class PartidaFluxoTests
{
    /// <summary>
    /// <b>Objetivo:</b> Garantir que a partida seja finalizada quando algum time atinge a pontuação alvo.
    /// <br/><b>Critério:</b> Status deve mudar para Finalizada após FinalizarPartida.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve finalizar a partida quando a pontuação alvo for atingida (Critério: Status = Finalizada).")]
    public void FinalizarPartida_DeveMarcarComoFinalizada_QuandoPontuacaoAlvoForAtingida()
    {
        var partida = CriarPartidaComDoisJogadores(pontuacaoAlvo: 10);
        partida.Jogadores[0].SomarPontos(10);
        partida.IniciarNovaRodada();

        partida.FinalizarPartida();

        Assert.Equal(StatusPartida.Finalizada, partida.Status);
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que a partida permaneça em andamento quando a pontuação alvo não for atingida.
    /// <br/><b>Critério:</b> Status deve permanecer EmAndamento após FinalizarPartida.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Não deve finalizar a partida quando a pontuação alvo não for atingida (Critério: Status = EmAndamento).")]
    public void FinalizarPartida_NaoDeveFinalizar_QuandoPontuacaoAlvoNaoForAtingida()
    {
        var partida = CriarPartidaComDoisJogadores(pontuacaoAlvo: 10);
        partida.Jogadores[0].SomarPontos(9);
        partida.IniciarNovaRodada();

        partida.FinalizarPartida();

        Assert.Equal(StatusPartida.EmAndamento, partida.Status);
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que finalizar uma partida fora de andamento lance exceção do domínio do projeto.
    /// <br/><b>Critério:</b> Deve lançar exceção e o namespace da exceção deve pertencer ao projeto.
    /// </summary>
    [Trait("Categoria", "Excecao")]
    [Fact(DisplayName = "Deve lançar exceção do projeto ao tentar finalizar partida fora de andamento (Critério: namespace inicia com DominoPontaDeQuina).")]
    public void FinalizarPartida_DeveLancarExcecao_QuandoPartidaNaoEstiverEmAndamento()
    {
        var partida = new Partida();

        Assert.Throws<InvalidOperationException>(() => partida.FinalizarPartida());
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que iniciar nova rodada em partida finalizada lance exceção do domínio do projeto.
    /// <br/><b>Critério:</b> Deve lançar exceção e o namespace da exceção deve pertencer ao projeto.
    /// </summary>
    [Trait("Categoria", "Excecao")]
    [Fact(DisplayName = "Deve lançar exceção do projeto ao iniciar rodada em partida finalizada (Critério: namespace inicia com DominoPontaDeQuina).")]
    public void IniciarNovaRodada_DeveLancarExcecao_QuandoPartidaEstiverFinalizada()
    {
        var partida = new Partida();
        var statusField = typeof(Partida).GetField("<Status>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;
        statusField.SetValue(partida, StatusPartida.Finalizada);

        Assert.Throws<InvalidOperationException>(() => partida.IniciarNovaRodada());
    }

    private static Partida CriarPartidaComDoisJogadores(int pontuacaoAlvo)
    {
        var partida = new Partida(pontuacaoAlvo);
        partida.AdicionarJogador(new Jogador("Alice"));
        partida.AdicionarJogador(new Jogador("Bob"));
        return partida;
    }
}
