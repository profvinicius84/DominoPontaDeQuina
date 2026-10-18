using DominoPontaDeQuina.Application.Partidas.Contracts;

namespace DominoPontaDeQuina.Tests;

/// <summary>Valida os contratos puros da jornada de Application.</summary>
public class ApplicationContractsTests
{
    [Fact(DisplayName = "Comando de jogada preserva versão e chave de idempotência.")]
    public void JogarPartidaComando_PreservaDadosDeConsistencia()
    {
        var chave = Guid.NewGuid();
        var comando = new JogarPartidaComando(Guid.NewGuid(), Guid.NewGuid(), new PecaPartida(6, 1), LadoJogavel.Direita, 12, chave);

        Assert.Equal(12, comando.VersaoEsperada);
        Assert.Equal(chave, comando.ChaveIdempotencia);
        Assert.Equal(new PecaPartida(6, 1), comando.Peca);
    }

    [Fact(DisplayName = "Conflito de estado devolve somente a visão autorizada do solicitante.")]
    public void ResultadoPartida_Conflito_ContemEstadoAutoritativoSemMaoAdversaria()
    {
        var estado = CriarEstadoParticipante();
        var resultado = ResultadoPartida<ConfirmacaoAcaoPartida>.CriarFalha(CodigoResultadoPartida.ConflitoDeEstado, estado);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CodigoResultadoPartida.ConflitoDeEstado, resultado.Codigo);
        Assert.Same(estado, resultado.EstadoAutoritativo);
        Assert.DoesNotContain(typeof(EstadoPartidaParticipante).GetProperties(), propriedade =>
            propriedade.Name.Contains("Advers", StringComparison.OrdinalIgnoreCase)
            || propriedade.Name.Contains("OutraMao", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Resultado idempotente mantém a confirmação da ação original.")]
    public void ConfirmacaoAcaoPartida_ExplicitaRepeticaoIdempotente()
    {
        var confirmacao = new ConfirmacaoAcaoPartida(Guid.NewGuid(), true, CriarEstadoParticipante());

        Assert.True(confirmacao.AcaoIdempotente);
        Assert.True(confirmacao.Estado.VersaoEstado > 0);
    }

    private static EstadoPartidaParticipante CriarEstadoParticipante() => new(
        Guid.NewGuid(),
        7,
        SituacaoPartida.EmAndamento,
        2,
        Guid.NewGuid(),
        new List<PecaPartida> { new(6, 1) },
        new List<PecaPartida> { new(6, 6) },
        new List<ResumoParticipantePartida> { new(Guid.NewGuid(), "Alice", 5, 1, true) },
        null);
}
