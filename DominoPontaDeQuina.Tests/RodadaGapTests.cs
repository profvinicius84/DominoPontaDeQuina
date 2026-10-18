using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DominoPontaDeQuina.Core.Tests;

[Trait("Categoria", "Gap")]
public class RodadaGapTests
{
    /// <summary>
    /// <b>Objetivo:</b> Validar transição inicial de estado ao iniciar a rodada.
    /// <br/><b>Critério:</b> Status deve ser EmAndamento e JogadorAtual deve ser definido.
    /// </summary>
    [Fact(DisplayName = "Deve iniciar a rodada em andamento e definir jogador atual (Critério: Status = EmAndamento e JogadorAtual não nulo).")]
    public void Iniciar_DeveColocarRodadaEmAndamentoEDefinirJogadorAtual()
    {
        var rodada = new Rodada();
        var jogadores = new List<Jogador> { new("Alice"), new("Bob") }.AsReadOnly();

        rodada.Iniciar(jogadores);

        Assert.Equal(StatusRodada.EmAndamento, rodada.Status);
        Assert.NotNull(rodada.JogadorAtual);
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que registrar jogada aplique estado e histórico corretamente.
    /// <br/><b>Critério:</b> Jogada deve ficar como Aplicada e entrar no histórico da rodada.
    /// </summary>
    [Fact(DisplayName = "Deve marcar jogada como aplicada e adicionar ao histórico (Critério: StatusJogada = Aplicada e histórico atualizado).")]
    public void RegistrarJogada_DeveMarcarJogadaComoAplicadaEAdicionarAoHistorico()
    {
        var jogadorA = new Jogador("Alice");
        var jogadorB = new Jogador("Bob");
        var rodada = new Rodada(CriarConjuntoComAbertura(jogadorA));
        rodada.Iniciar(new[] { jogadorA, jogadorB }.ToList().AsReadOnly());
        var jogada = rodada.ObterJogadaAtual();

        rodada.RegistrarJogada(jogada);

        Assert.Equal(StatusJogada.Aplicada, jogada.Status);
        Assert.Single(rodada.HistoricoJogadas);
        Assert.Same(jogada, rodada.HistoricoJogadas[0]);
    }

    private static IEnumerable<Peca> CriarConjuntoComAbertura(Jogador jogadorA)
    {
        var conjunto = Enumerable.Range(0, 7)
            .SelectMany(valorA => Enumerable.Range(valorA, 7 - valorA)
                .Select(valorB => new Peca(valorA, valorB)))
            .ToList();
        var abertura = new Peca(6, 6);
        conjunto.Remove(abertura);
        conjunto.Insert(0, abertura);
        return conjunto;
    }
}
