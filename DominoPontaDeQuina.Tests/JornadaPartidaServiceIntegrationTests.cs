using DominoPontaDeQuina.Application.Partidas;
using DominoPontaDeQuina.Application.Partidas.Contracts;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Infrastructure.Persistence;
using DominoPontaDeQuina.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Tests;

/// <summary>Valida a orquestração transacional entre Application, Core e EF.</summary>
public sealed class JornadaPartidaServiceIntegrationTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly Guid jogadorAId = Guid.NewGuid();
    private readonly Guid jogadorBId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await using var db = CriarContexto();
        await db.Database.EnsureCreatedAsync();
        db.Jogadores.AddRange(new Jogador { Id = jogadorAId, Nome = "Alice" }, new Jogador { Id = jogadorBId, Nome = "Bob" });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => connection.DisposeAsync().AsTask();

    [Fact(DisplayName = "Jornada autoriza participantes, executa jogada no Core e recupera estado privado")]
    public async Task Jornada_ExecutaJogadaEProtegeMaoAdversaria()
    {
        Guid partidaId;
        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db);
            var criada = await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50));
            Assert.True(criada.Sucesso);
            partidaId = criada.Valor!.PartidaId;

            var ingresso = await servico.IngressarAsync(new IngressarPartidaComando(partidaId, jogadorBId, 0, Guid.NewGuid()));
            Assert.True(ingresso.Sucesso);
            var inicio = await servico.IniciarAsync(new IniciarPartidaComando(partidaId, jogadorAId, 1, Guid.NewGuid()));
            Assert.True(inicio.Sucesso);
        }

        EstadoPartidaParticipante estadoDoTurno;
        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db);
            var estadoA = await servico.ConsultarEstadoProprioAsync(new ConsultarEstadoProprioConsulta(partidaId, jogadorAId));
            var estadoB = await servico.ConsultarEstadoProprioAsync(new ConsultarEstadoProprioConsulta(partidaId, jogadorBId));
            Assert.True(estadoA.Sucesso);
            Assert.True(estadoB.Sucesso);
            Assert.Equal(7, estadoA.Valor!.MinhaMao.Count);
            Assert.Equal(7, estadoB.Valor!.MinhaMao.Count);
            Assert.All(estadoA.Valor.Participantes, participante => Assert.Equal(7, participante.QuantidadePedrasNaMao));
            estadoDoTurno = estadoA.Valor.JogadorDoTurnoId == jogadorAId ? estadoA.Valor : estadoB.Valor;
            var estadoEmEspera = estadoA.Valor.JogadorDoTurnoId == jogadorAId ? estadoB.Valor : estadoA.Valor;
            Assert.Equal(ProximaAcaoPartida.JogarAbertura, estadoDoTurno.ProximaAcao);
            Assert.NotNull(estadoDoTurno.PedraAberturaObrigatoria);
            Assert.Equal(ProximaAcaoPartida.Aguardar, estadoEmEspera.ProximaAcao);
            Assert.Null(estadoEmEspera.PedraAberturaObrigatoria);

            var foraDoTurno = await servico.PassarVezAsync(new PassarVezPartidaComando(partidaId,
                estadoDoTurno.JogadorDoTurnoId == jogadorAId ? jogadorBId : jogadorAId, estadoDoTurno.VersaoEstado, Guid.NewGuid()));
            Assert.Equal(CodigoResultadoPartida.ForaDoTurno, foraDoTurno.Codigo);

            var passagem = await servico.PassarVezAsync(new PassarVezPartidaComando(partidaId,
                estadoDoTurno.JogadorDoTurnoId!.Value, estadoDoTurno.VersaoEstado, Guid.NewGuid()));
            Assert.Equal(CodigoResultadoPartida.PassagemNaoPermitida, passagem.Codigo);

            var peca = estadoDoTurno.PedraAberturaObrigatoria!;
            var pedraDiferente = estadoDoTurno.MinhaMao.First(item => item != peca);
            var tentativaAberturaInvalida = await servico.JogarAsync(new JogarPartidaComando(partidaId,
                estadoDoTurno.JogadorDoTurnoId.Value, pedraDiferente, LadoJogavel.Esquerda,
                estadoDoTurno.VersaoEstado, Guid.NewGuid()));
            Assert.Equal(CodigoResultadoPartida.EstadoInvalido, tentativaAberturaInvalida.Codigo);
            Assert.Equal(estadoDoTurno.VersaoEstado, tentativaAberturaInvalida.EstadoAutoritativo!.VersaoEstado);
            Assert.Empty(tentativaAberturaInvalida.EstadoAutoritativo.Tabuleiro);
            Assert.Equal(ProximaAcaoPartida.JogarAbertura, tentativaAberturaInvalida.EstadoAutoritativo.ProximaAcao);
            var chaveJogada = Guid.NewGuid();
            var comandoJogada = new JogarPartidaComando(partidaId, estadoDoTurno.JogadorDoTurnoId.Value,
                peca, LadoJogavel.Esquerda, estadoDoTurno.VersaoEstado, chaveJogada);
            var jogada = await servico.JogarAsync(comandoJogada);
            Assert.True(jogada.Sucesso);
            Assert.Single(jogada.Valor!.Estado.Tabuleiro);
            Assert.Equal(6, jogada.Valor.Estado.MinhaMao.Count);

            var repetida = await servico.JogarAsync(comandoJogada);
            Assert.True(repetida.Sucesso);
            Assert.True(repetida.Valor!.AcaoIdempotente);
            Assert.Single(repetida.Valor.Estado.Tabuleiro);

            var chaveReutilizada = await servico.JogarAsync(comandoJogada with { Lado = LadoJogavel.Direita });
            Assert.Equal(CodigoResultadoPartida.EntradaInvalida, chaveReutilizada.Codigo);

            var concorrente = await servico.JogarAsync(comandoJogada with { ChaveIdempotencia = Guid.NewGuid() });
            Assert.Equal(CodigoResultadoPartida.ConflitoDeEstado, concorrente.Codigo);
            Assert.Equal(jogada.Valor.Estado.VersaoEstado, concorrente.EstadoAutoritativo!.VersaoEstado);
        }

        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db);
            var estado = await servico.ConsultarEstadoProprioAsync(new ConsultarEstadoProprioConsulta(partidaId, jogadorAId));
            Assert.True(estado.Sucesso);
            Assert.Single(estado.Valor!.Tabuleiro);
            Assert.Contains(estado.Valor.Participantes, item => item.JogadorId == jogadorAId && item.QuantidadePedrasNaMao is 6 or 7);
            Assert.Equal(2, estado.Valor.Participantes.Count);
        }
    }

    [Theory(DisplayName = "Cancelamento encerra sem vencedor, é idempotente e libera os jogadores")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelarPartida_LiberaJogadores(bool iniciar)
    {
        await using var db = CriarContexto();
        var servico = CriarServico(db);
        var criada = await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50,
            Webhook: "https://alice.example/callback"));
        var partidaId = criada.Valor!.PartidaId;
        var ingresso = await servico.IngressarAsync(new IngressarPartidaComando(partidaId, jogadorBId, 0, Guid.NewGuid(),
            Webhook: "https://bob.example/callback"));
        Assert.True(ingresso.Sucesso);
        var versao = ingresso.Valor!.Estado.VersaoEstado;
        if (iniciar)
        {
            var inicio = await servico.IniciarAsync(new IniciarPartidaComando(partidaId, jogadorAId, versao, Guid.NewGuid()));
            Assert.True(inicio.Sucesso);
            versao = inicio.Valor!.Estado.VersaoEstado;
        }

        var intruso = await servico.CancelarAsync(new CancelarPartidaComando(partidaId, Guid.NewGuid(), versao, Guid.NewGuid()));
        Assert.Equal(CodigoResultadoPartida.SemPermissao, intruso.Codigo);

        var comando = new CancelarPartidaComando(partidaId, jogadorAId, versao, Guid.NewGuid());
        var cancelada = await servico.CancelarAsync(comando);
        Assert.True(cancelada.Sucesso);
        Assert.Equal(SituacaoPartida.Encerrada, cancelada.Valor!.Estado.Situacao);
        Assert.Null(cancelada.Valor.Estado.VencedorId);
        Assert.Equal(versao + 1, cancelada.Valor.Estado.VersaoEstado);
        Assert.Equal(2, await db.EventosWebhook.CountAsync(evento =>
            evento.PartidaId == partidaId && evento.Tipo == "partida-cancelada"));
        Assert.Equal(TipoAcaoPartida.CancelamentoPartida,
            (await db.AcoesPartida.SingleAsync(acao => acao.PartidaId == partidaId && acao.ChaveIdempotencia == comando.ChaveIdempotencia)).Tipo);
        if (iniciar)
        {
            var rodada = await db.RodadasPartida.SingleAsync(item => item.PartidaId == partidaId);
            Assert.Equal(SituacaoRodadaPartida.Encerrada, rodada.Situacao);
            Assert.Equal(MotivoEncerramentoRodada.CancelamentoPartida, rodada.MotivoEncerramento);
        }

        var repetida = await servico.CancelarAsync(comando);
        Assert.True(repetida.Sucesso);
        Assert.True(repetida.Valor!.AcaoIdempotente);
        var concorrente = await servico.CancelarAsync(comando with { ChaveIdempotencia = Guid.NewGuid() });
        Assert.Equal(CodigoResultadoPartida.ConflitoDeEstado, concorrente.Codigo);
        var novaTentativa = await servico.CancelarAsync(comando with
        {
            VersaoEsperada = versao + 1,
            ChaveIdempotencia = Guid.NewGuid()
        });
        Assert.Equal(CodigoResultadoPartida.EstadoInvalido, novaTentativa.Codigo);
        Assert.Equal(SituacaoPartida.Encerrada,
            (await servico.ConsultarEstadoProprioAsync(new ConsultarEstadoProprioConsulta(partidaId, jogadorBId))).Valor!.Situacao);

        Assert.True((await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50))).Sucesso);
        Assert.True((await servico.CriarAsync(new CriarPartidaComando(jogadorBId, 50))).Sucesso);
    }

    [Theory(DisplayName = "Reinício cria outra partida com os dois jogadores e placar zerado")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReiniciarPartida_CriaNovaMesaEPermiteDescoberta(bool cancelarAntes)
    {
        await using var db = CriarContexto();
        var servico = CriarServico(db);
        var criada = await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50,
            Webhook: "https://alice.example/callback"));
        var antigaId = criada.Valor!.PartidaId;
        var ingresso = await servico.IngressarAsync(new IngressarPartidaComando(antigaId, jogadorBId, 0, Guid.NewGuid(),
            Webhook: "https://bob.example/callback"));
        var inicio = await servico.IniciarAsync(new IniciarPartidaComando(antigaId, jogadorAId,
            ingresso.Valor!.Estado.VersaoEstado, Guid.NewGuid()));
        var versao = inicio.Valor!.Estado.VersaoEstado;
        (await db.ParticipacoesPartida.SingleAsync(item => item.PartidaId == antigaId && item.JogadorId == jogadorAId)).Pontos = 7;
        await db.SaveChangesAsync();
        if (cancelarAntes)
        {
            var cancelamento = await servico.CancelarAsync(new CancelarPartidaComando(antigaId, jogadorAId, versao, Guid.NewGuid()));
            Assert.True(cancelamento.Sucesso);
            versao = cancelamento.Valor!.Estado.VersaoEstado;
        }

        var comando = new ReiniciarPartidaComando(antigaId, jogadorAId, versao, Guid.NewGuid());
        var reinicio = await servico.ReiniciarAsync(comando);
        Assert.True(reinicio.Sucesso);
        var nova = reinicio.Valor!.Estado;
        Assert.NotEqual(antigaId, nova.PartidaId);
        Assert.Equal(SituacaoPartida.EmAndamento, nova.Situacao);
        Assert.Equal(1, nova.NumeroRodada);
        Assert.All(nova.Participantes, item => Assert.Equal(0, item.Pontuacao));
        Assert.All(nova.Participantes, item => Assert.Equal(7, item.QuantidadePedrasNaMao));
        Assert.Equal(2, await db.EventosWebhook.CountAsync(item =>
            item.PartidaId == antigaId && item.Tipo == "partida-reiniciada"));
        Assert.Equal(2, await db.EventosWebhook.CountAsync(item =>
            item.PartidaId == nova.PartidaId && item.Tipo == "partida-iniciada"));
        var antiga = await db.Partidas.Include(item => item.Participacoes).SingleAsync(item => item.Id == antigaId);
        Assert.Equal("Encerrada", antiga.Status);
        Assert.Equal(7, antiga.Participacoes.Single(item => item.JogadorId == jogadorAId).Pontos);
        Assert.Equal(cancelarAntes ? MotivoEncerramentoRodada.CancelamentoPartida : MotivoEncerramentoRodada.ReinicioPartida,
            (await db.RodadasPartida.SingleAsync(item => item.PartidaId == antigaId)).MotivoEncerramento);

        var descoberta = await servico.ConsultarReinicioAsync(new ConsultarReinicioConsulta(antigaId, jogadorBId));
        Assert.True(descoberta.Sucesso);
        Assert.Equal(nova.PartidaId, descoberta.Valor!.PartidaId);
        var repetida = await servico.ReiniciarAsync(comando);
        Assert.True(repetida.Sucesso);
        Assert.True(repetida.Valor!.AcaoIdempotente);
        Assert.Equal(nova.PartidaId, repetida.Valor.Estado.PartidaId);
        var concorrente = await servico.ReiniciarAsync(comando with { ChaveIdempotencia = Guid.NewGuid() });
        Assert.Equal(CodigoResultadoPartida.ConflitoDeEstado, concorrente.Codigo);
    }

    [Fact(DisplayName = "Reinício não duplica jogador que já entrou em outra partida")]
    public async Task ReiniciarPartida_RejeitaParticipanteEmOutraPartida()
    {
        await using var db = CriarContexto();
        var servico = CriarServico(db);
        var antiga = (await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50))).Valor!;
        var ingresso = await servico.IngressarAsync(new IngressarPartidaComando(
            antiga.PartidaId, jogadorBId, antiga.VersaoEstado, Guid.NewGuid()));
        var cancelamento = await servico.CancelarAsync(new CancelarPartidaComando(
            antiga.PartidaId, jogadorAId, ingresso.Valor!.Estado.VersaoEstado, Guid.NewGuid()));
        Assert.True(cancelamento.Sucesso);
        Assert.True((await servico.CriarAsync(new CriarPartidaComando(jogadorBId, 50))).Sucesso);

        var tentativa = await servico.ReiniciarAsync(new ReiniciarPartidaComando(
            antiga.PartidaId, jogadorAId, cancelamento.Valor!.Estado.VersaoEstado, Guid.NewGuid()));

        Assert.Equal(CodigoResultadoPartida.EstadoInvalido, tentativa.Codigo);
        Assert.Equal(2, await db.Partidas.CountAsync());
    }

    [Fact(DisplayName = "Não participante não consulta nem altera a jornada")]
    public async Task Jornada_NaoParticipante_EhRejeitado()
    {
        var intruso = Guid.NewGuid();
        Guid partidaId;
        await using (var db = CriarContexto())
        {
            db.Jogadores.Add(new Jogador { Id = intruso, Nome = "Carol" });
            await db.SaveChangesAsync();
            var servico = CriarServico(db);
            partidaId = (await servico.CriarAsync(new CriarPartidaComando(jogadorAId, 50))).Valor!.PartidaId;
            var consulta = await servico.ConsultarEstadoProprioAsync(new ConsultarEstadoProprioConsulta(partidaId, intruso));
            Assert.Equal(CodigoResultadoPartida.PartidaIndisponivel, consulta.Codigo);
        }
    }

    private JornadaPartidaService CriarServico(DominoDbContext db) => new(new EstadoPartidaRepository(db), new JogadorRepository(db));

    private DominoDbContext CriarContexto() => new(new DbContextOptionsBuilder<DominoDbContext>().UseSqlite(connection).Options);
}
