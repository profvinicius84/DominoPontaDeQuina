using DominoPontaDeQuina.Application.Partidas.Contracts;
using DominoPontaDeQuina.Application.Jogadores;
using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Models;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using CorePartida = DominoPontaDeQuina.Core.Models.Partida;
using CoreRodada = DominoPontaDeQuina.Core.Models.Rodada;
using EntidadePartida = DominoPontaDeQuina.Domain.Entities.Partida;
using JogadorPersistido = DominoPontaDeQuina.Domain.Entities.Jogador;
using JogadorCore = DominoPontaDeQuina.Core.Models.Jogador;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DominoPontaDeQuina.Application.Partidas;

/// <summary>
/// Orquestra os contratos da jornada sem duplicar as regras do motor. Cada
/// alteração aceita é montada no Core e persistida em uma única confirmação.
/// </summary>
public sealed class JornadaPartidaService(
    IEstadoPartidaRepository estados,
    IJogadorRepository jogadores,
    IPerfilJogadorRepository? perfis = null) : IJornadaPartidaService
{
    public async Task<IReadOnlyList<PartidaDisponivel>> ListarDisponiveisAsync(CancellationToken cancellationToken = default) =>
        (await estados.ListarDisponiveisAsync(cancellationToken))
            .Select(p => new PartidaDisponivel(p.Id, p.PontuacaoAlvo, p.VersaoEstado)).ToArray();
    /// <inheritdoc />
    public async Task<ResultadoPartida<EstadoPartidaParticipante>> CriarAsync(
        CriarPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (comando.SolicitanteId == Guid.Empty || comando.PontuacaoAlvo <= 0
            || (comando.Webhook is not null && !WebhookValido(comando.Webhook)))
            return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.EntradaInvalida);

        var jogador = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogador is null)
            return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.SemPermissao);
        if (await estados.PossuiParticipacaoNaoEncerradaAsync(jogador.Id, null, cancellationToken))
            return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.EstadoInvalido);

        var partida = new EntidadePartida
        {
            PontuacaoAlvo = comando.PontuacaoAlvo,
            Status = nameof(SituacaoPartida.AguardandoInicio),
            CriadaEm = DateTime.UtcNow,
            VersaoEstado = 0
        };
        var participacao = new ParticipacaoPartida
        {
            JogadorId = jogador.Id,
            Jogador = jogador,
            Posicao = 1,
            WebhookUrl = comando.Webhook
        };
        partida.AdicionarParticipacao(participacao);
        EnfileirarEventos(partida, "partida-criada");
        try { await estados.AdicionarAsync(partida, cancellationToken); }
        catch (ConflitoParticipacaoException)
        { return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.ConflitoDeEstado); }
        return ResultadoPartida<EstadoPartidaParticipante>.CriarSucesso(CriarEstado(partida, jogador.Id));
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> IngressarAsync(
        IngressarPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (!ComandoValido(comando.PartidaId, comando.SolicitanteId, comando.ChaveIdempotencia)
            || (comando.Webhook is not null && !WebhookValido(comando.Webhook)))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaAtualizacaoAsync(comando.PartidaId, cancellationToken);
        if (partida is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PartidaIndisponivel);
        var jogador = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogador is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.SemPermissao);
        var solicitanteId = jogador.Id;
        if (await estados.PossuiParticipacaoNaoEncerradaAsync(jogador.Id, comando.PartidaId, cancellationToken))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido);
        var repeticao = await VerificarIdempotenciaAsync(partida, solicitanteId, comando.ChaveIdempotencia,
            TipoAcaoPartida.Ingresso, comando.VersaoEsperada, null, null, cancellationToken);
        if (repeticao is not null)
        {
            if (repeticao.Sucesso && partida.Participacoes.Single(p => p.JogadorId == solicitanteId).WebhookUrl != comando.Webhook)
                return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);
            return repeticao;
        }
        if (partida.Participacoes.Any(item => item.JogadorId == solicitanteId))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);
        if (partida.VersaoEstado != comando.VersaoEsperada)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado);
        if (!EhAguardandoInicio(partida) || partida.Participacoes.Count != 1)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);

        var participacao = new ParticipacaoPartida
        {
            JogadorId = jogador.Id,
            Jogador = jogador,
            Posicao = 2,
            WebhookUrl = comando.Webhook
        };
        partida.AdicionarParticipacao(participacao);
        estados.AdicionarAoContexto(participacao);
        var acao = RegistrarAcao(partida, jogador.Id, TipoAcaoPartida.Ingresso, comando.ChaveIdempotencia);
        estados.AdicionarAoContexto(acao);
        EnfileirarEventos(partida, "participante-ingressou");
        if (!await estados.TentarSalvarAsync(cancellationToken))
            return await CriarConflitoAsync(comando.PartidaId, solicitanteId, cancellationToken);
        return SucessoAcao(partida, jogador.Id, comando.ChaveIdempotencia);
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> IniciarAsync(
        IniciarPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (!ComandoValido(comando.PartidaId, comando.SolicitanteId, comando.ChaveIdempotencia))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaAtualizacaoAsync(comando.PartidaId, cancellationToken);
        if (partida is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PartidaIndisponivel);
        var jogador = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogador is null || !EhParticipante(partida, jogador.Id))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.SemPermissao);
        var solicitanteId = jogador.Id;
        var repeticao = await VerificarIdempotenciaAsync(partida, solicitanteId, comando.ChaveIdempotencia,
            TipoAcaoPartida.Inicio, comando.VersaoEsperada, null, null, cancellationToken);
        if (repeticao is not null)
            return repeticao;
        if (partida.VersaoEstado != comando.VersaoEsperada)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado, partida, solicitanteId);
        if (!EhAguardandoInicio(partida) || partida.Participacoes.Count != 2)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);

        var motor = CriarPartidaCore(partida);
        motor.IniciarNovaRodada();
        var rodadaCore = motor.RodadaAtual!;
        partida.Status = nameof(SituacaoPartida.EmAndamento);
        var rodada = new RodadaPartida { Numero = 1, IniciadaEmUtc = DateTime.UtcNow };
        partida.AdicionarRodada(rodada);
        AtualizarRodada(partida, rodada, rodadaCore);
        estados.AdicionarAoContexto(rodada);
        RegistrarPedrasNovas(partida, rodada);
        var acao = RegistrarAcao(partida, solicitanteId, TipoAcaoPartida.Inicio, comando.ChaveIdempotencia, rodada.Id);
        estados.AdicionarAoContexto(acao);
        EnfileirarEventos(partida, "partida-iniciada");
        if (!await estados.TentarSalvarAsync(cancellationToken))
            return await CriarConflitoAsync(comando.PartidaId, solicitanteId, cancellationToken);
        return SucessoAcao(partida, solicitanteId, comando.ChaveIdempotencia);
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<EstadoPartidaParticipante>> ConsultarEstadoProprioAsync(
        ConsultarEstadoProprioConsulta consulta, CancellationToken cancellationToken = default)
    {
        if (consulta.PartidaId == Guid.Empty || consulta.SolicitanteId == Guid.Empty)
            return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.EntradaInvalida);

        var jogador = await ObterJogadorAsync(consulta.SolicitanteId, consulta.Acesso, cancellationToken);
        if (jogador is null)
            return ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.SemPermissao);
        var partida = await estados.ObterParaParticipanteAsync(consulta.PartidaId, jogador.Id, cancellationToken);
        return partida is null
            ? ResultadoPartida<EstadoPartidaParticipante>.CriarFalha(CodigoResultadoPartida.PartidaIndisponivel)
            : ResultadoPartida<EstadoPartidaParticipante>.CriarSucesso(CriarEstado(partida, jogador.Id));
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<EstadoPartidaObservador>> ConsultarObservacaoAsync(
        ConsultarObservacaoConsulta consulta, CancellationToken cancellationToken = default)
    {
        if (consulta.PartidaId == Guid.Empty)
            return ResultadoPartida<EstadoPartidaObservador>.CriarFalha(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaObservadorAsync(consulta.PartidaId, cancellationToken);
        return partida is null
            ? ResultadoPartida<EstadoPartidaObservador>.CriarFalha(CodigoResultadoPartida.PartidaIndisponivel)
            : ResultadoPartida<EstadoPartidaObservador>.CriarSucesso(CriarObservacao(partida));
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> JogarAsync(
        JogarPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (!ComandoValido(comando.PartidaId, comando.SolicitanteId, comando.ChaveIdempotencia)
            || !PecaValida(comando.Peca) || !Enum.IsDefined(comando.Lado))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaAtualizacaoAsync(comando.PartidaId, cancellationToken);
        if (partida is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PartidaIndisponivel);
        var jogadorPersistido = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogadorPersistido is null || !EhParticipante(partida, jogadorPersistido.Id))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.SemPermissao);
        var solicitanteId = jogadorPersistido.Id;
        var repeticao = await VerificarIdempotenciaAsync(partida, solicitanteId, comando.ChaveIdempotencia,
            TipoAcaoPartida.Jogada, comando.VersaoEsperada, comando.Peca, comando.Lado, cancellationToken);
        if (repeticao is not null)
            return repeticao;
        if (partida.VersaoEstado != comando.VersaoEsperada)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado, partida, solicitanteId);
        if (!EhEmAndamento(partida))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);

        var rodadaPersistida = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        if (rodadaPersistida is null || rodadaPersistida.Situacao != SituacaoRodadaPartida.EmAndamento)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);
        if (rodadaPersistida.JogadorDoTurnoId != solicitanteId)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ForaDoTurno, partida, solicitanteId);

        var motor = ReidratarRodada(partida, rodadaPersistida);
        var jogador = motor.JogadorAtual.Jogador;
        var peca = new Peca(comando.Peca.ValorA, comando.Peca.ValorB);
        var pontosAntes = partida.Participacoes.Single(item => item.JogadorId == solicitanteId).Pontos;
        try
        {
            motor.RegistrarJogadaComCompra(jogador, peca, ConverterLadoCore(comando.Lado));
        }
        catch (InvalidOperationException excecao)
        {
            return Falha<ConfirmacaoAcaoPartida>(MapearFalhaJogada(excecao), partida, solicitanteId);
        }

        var rodadaNova = AtualizarAposAcao(partida, rodadaPersistida, motor);
        if (rodadaNova is not null)
        {
            estados.AdicionarAoContexto(rodadaNova);
            RegistrarPedrasNovas(partida, rodadaNova);
        }
        RegistrarPedrasNovas(partida, rodadaPersistida);
        var acao = RegistrarAcao(partida, solicitanteId, TipoAcaoPartida.Jogada, comando.ChaveIdempotencia,
            rodadaPersistida.Id, comando.Peca, comando.Lado,
            partida.Participacoes.Single(item => item.JogadorId == solicitanteId).Pontos - pontosAntes);
        estados.AdicionarAoContexto(acao);
        EnfileirarEventos(partida, partida.Status == nameof(SituacaoPartida.Encerrada) ? "partida-encerrada" : "jogada-realizada");
        if (!await estados.TentarSalvarAsync(cancellationToken))
            return await CriarConflitoAsync(comando.PartidaId, solicitanteId, cancellationToken);
        return SucessoAcao(partida, solicitanteId, comando.ChaveIdempotencia);
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> PassarVezAsync(
        PassarVezPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (!ComandoValido(comando.PartidaId, comando.SolicitanteId, comando.ChaveIdempotencia))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaAtualizacaoAsync(comando.PartidaId, cancellationToken);
        if (partida is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PartidaIndisponivel);
        var jogadorPersistido = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogadorPersistido is null || !EhParticipante(partida, jogadorPersistido.Id))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.SemPermissao);
        var solicitanteId = jogadorPersistido.Id;
        var repeticao = await VerificarIdempotenciaAsync(partida, solicitanteId, comando.ChaveIdempotencia,
            TipoAcaoPartida.Passagem, comando.VersaoEsperada, null, null, cancellationToken);
        if (repeticao is not null)
            return repeticao;
        if (partida.VersaoEstado != comando.VersaoEsperada)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado, partida, solicitanteId);
        if (!EhEmAndamento(partida))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);

        var rodadaPersistida = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        if (rodadaPersistida is null || rodadaPersistida.Situacao != SituacaoRodadaPartida.EmAndamento)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);
        if (rodadaPersistida.JogadorDoTurnoId != solicitanteId)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ForaDoTurno, partida, solicitanteId);

        var motor = ReidratarRodada(partida, rodadaPersistida);
        try
        {
            motor.RegistrarJogada(new Jogada(motor.JogadorAtual.Jogador));
        }
        catch (InvalidOperationException)
        {
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PassagemNaoPermitida, partida, solicitanteId);
        }

        var rodadaNova = AtualizarAposAcao(partida, rodadaPersistida, motor);
        if (rodadaNova is not null)
        {
            estados.AdicionarAoContexto(rodadaNova);
            RegistrarPedrasNovas(partida, rodadaNova);
        }
        RegistrarPedrasNovas(partida, rodadaPersistida);
        var acao = RegistrarAcao(partida, solicitanteId, TipoAcaoPartida.Passagem, comando.ChaveIdempotencia, rodadaPersistida.Id);
        estados.AdicionarAoContexto(acao);
        EnfileirarEventos(partida, partida.Status == nameof(SituacaoPartida.Encerrada) ? "partida-encerrada" : "vez-passada");
        if (!await estados.TentarSalvarAsync(cancellationToken))
            return await CriarConflitoAsync(comando.PartidaId, solicitanteId, cancellationToken);
        return SucessoAcao(partida, solicitanteId, comando.ChaveIdempotencia);
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> ComprarAsync(
        ComprarPartidaComando comando, CancellationToken cancellationToken = default)
    {
        if (!ComandoValido(comando.PartidaId, comando.SolicitanteId, comando.ChaveIdempotencia))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida);

        var partida = await estados.ObterParaAtualizacaoAsync(comando.PartidaId, cancellationToken);
        if (partida is null)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.PartidaIndisponivel);
        var jogadorPersistido = await ObterJogadorAsync(comando.SolicitanteId, comando.Acesso, cancellationToken);
        if (jogadorPersistido is null || !EhParticipante(partida, jogadorPersistido.Id))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.SemPermissao);
        var solicitanteId = jogadorPersistido.Id;
        var repeticao = await VerificarIdempotenciaAsync(partida, solicitanteId, comando.ChaveIdempotencia,
            TipoAcaoPartida.Compra, comando.VersaoEsperada, null, null, cancellationToken);
        if (repeticao is not null)
            return repeticao;
        if (partida.VersaoEstado != comando.VersaoEsperada)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado, partida, solicitanteId);
        if (!EhEmAndamento(partida))
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);

        var rodadaPersistida = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        if (rodadaPersistida is null || rodadaPersistida.Situacao != SituacaoRodadaPartida.EmAndamento)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);
        if (rodadaPersistida.JogadorDoTurnoId != solicitanteId)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ForaDoTurno, partida, solicitanteId);

        var motor = ReidratarRodada(partida, rodadaPersistida);
        try
        {
            motor.ComprarOuPassar(motor.JogadorAtual.Jogador);
        }
        catch (InvalidOperationException)
        {
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EstadoInvalido, partida, solicitanteId);
        }

        var rodadaNova = AtualizarAposAcao(partida, rodadaPersistida, motor);
        if (rodadaNova is not null)
        {
            estados.AdicionarAoContexto(rodadaNova);
            RegistrarPedrasNovas(partida, rodadaNova);
        }
        RegistrarPedrasNovas(partida, rodadaPersistida);
        var acao = RegistrarAcao(partida, solicitanteId, TipoAcaoPartida.Compra, comando.ChaveIdempotencia, rodadaPersistida.Id);
        estados.AdicionarAoContexto(acao);
        EnfileirarEventos(partida, partida.Status == nameof(SituacaoPartida.Encerrada) ? "partida-encerrada" : "pedra-comprada");
        if (!await estados.TentarSalvarAsync(cancellationToken))
            return await CriarConflitoAsync(comando.PartidaId, solicitanteId, cancellationToken);
        return SucessoAcao(partida, solicitanteId, comando.ChaveIdempotencia);
    }

    /// <inheritdoc />
    public async Task<ResultadoPartida<ResultadoHistoricoPartida>> ConsultarResultadoHistoricoAsync(
        ConsultarResultadoHistoricoConsulta consulta, CancellationToken cancellationToken = default)
    {
        var estado = await ConsultarEstadoProprioAsync(new(consulta.PartidaId, consulta.SolicitanteId, consulta.Acesso), cancellationToken);
        if (!estado.Sucesso || estado.Valor is null)
            return ResultadoPartida<ResultadoHistoricoPartida>.CriarFalha(estado.Codigo, estado.EstadoAutoritativo);

        var jogador = await ObterJogadorAsync(consulta.SolicitanteId, consulta.Acesso, cancellationToken);
        if (jogador is null)
            return ResultadoPartida<ResultadoHistoricoPartida>.CriarFalha(CodigoResultadoPartida.SemPermissao);
        var partida = await estados.ObterParaParticipanteAsync(consulta.PartidaId, jogador.Id, cancellationToken);
        var eventos = partida!.Acoes.Where(acao => acao.FoiAceita && acao.Tipo is TipoAcaoPartida.Jogada or TipoAcaoPartida.Passagem or TipoAcaoPartida.Compra)
            .OrderBy(acao => acao.OrdemRecebimento)
            .Select(acao => new EventoHistoricoPartida(acao.Id, acao.AutorJogadorId,
                acao.ValorA is null ? null : new PecaPartida(acao.ValorA.Value, acao.ValorB!.Value),
                acao.Lado is null ? null : ConverterLadoContrato(acao.Lado.Value), acao.Tipo == TipoAcaoPartida.Passagem,
                acao.PontosConcedidos, acao.RecebidaEmUtc)).ToList();
        return ResultadoPartida<ResultadoHistoricoPartida>.CriarSucesso(new ResultadoHistoricoPartida(
            partida.Id, ConverterSituacao(partida.Status), partida.VersaoEstado, partida.VencedorJogadorId,
            CriarResumos(partida, jogador.Id), eventos));
    }

    private async Task<JogadorPersistido?> ObterJogadorAsync(Guid usuarioId, AcessoJogador? acesso, CancellationToken cancellationToken)
    {
        if (acesso is not null)
        {
            if (perfis is null || acesso.JogadorId == Guid.Empty || !PerfilJogadorService.ChaveValida(acesso.ChaveAtivacao)) return null;
            return await perfis.ObterAtivoAsync(usuarioId, acesso.JogadorId,
                PerfilJogadorService.CalcularHash(acesso.ChaveAtivacao!), cancellationToken);
        }
        // Compatibilidade restrita aos casos de uso internos antigos. HTTP sempre informa acesso.
        var unico = await jogadores.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (unico is not null) return unico;
        var legado = await jogadores.ObterPorIdAsync([usuarioId], cancellationToken);
        return legado is { UsuarioId: null, AtivacaoHash: null } ? legado : null;
    }

    private static bool ComandoValido(Guid partidaId, Guid solicitanteId, Guid chave) =>
        partidaId != Guid.Empty && solicitanteId != Guid.Empty && chave != Guid.Empty;
    private static bool WebhookValido(string? webhook) => webhook is { Length: > 0 and <= 2048 }
        && Uri.TryCreate(webhook, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0;
    private static bool PecaValida(PecaPartida peca) => peca.ValorA is >= 0 and <= 6 && peca.ValorB is >= 0 and <= 6;
    private static bool EhParticipante(EntidadePartida partida, Guid jogadorId) => partida.Participacoes.Any(item => item.JogadorId == jogadorId);
    private static bool EhAguardandoInicio(EntidadePartida partida) => partida.Status == nameof(SituacaoPartida.AguardandoInicio);
    private static bool EhEmAndamento(EntidadePartida partida) => partida.Status == nameof(SituacaoPartida.EmAndamento);

    private static ResultadoPartida<T> Falha<T>(CodigoResultadoPartida codigo, EntidadePartida? partida = null, Guid solicitanteId = default) =>
        ResultadoPartida<T>.CriarFalha(codigo, partida is null || solicitanteId == Guid.Empty ? null : CriarEstado(partida, solicitanteId));

    private static ResultadoPartida<ConfirmacaoAcaoPartida> SucessoAcao(EntidadePartida partida, Guid jogadorId, Guid chave) =>
        ResultadoPartida<ConfirmacaoAcaoPartida>.CriarSucesso(new ConfirmacaoAcaoPartida(chave, false, CriarEstado(partida, jogadorId)));

    private async Task<ResultadoPartida<ConfirmacaoAcaoPartida>?> VerificarIdempotenciaAsync(
        EntidadePartida partida,
        Guid autorJogadorId,
        Guid chaveIdempotencia,
        TipoAcaoPartida tipo,
        long versaoEsperada,
        PecaPartida? peca,
        LadoJogavel? lado,
        CancellationToken cancellationToken)
    {
        var existente = await estados.ObterPorChaveIdempotenciaAsync(partida.Id, autorJogadorId, chaveIdempotencia, cancellationToken);
        if (existente is null)
            return null;

        var mesmaIntencao = existente.Tipo == tipo
            && existente.VersaoEsperada == versaoEsperada
            && existente.ValorA == peca?.ValorA
            && existente.ValorB == peca?.ValorB
            && existente.Lado == (lado is null ? null : ConverterLadoPersistido(lado.Value));
        if (!mesmaIntencao)
            return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.EntradaInvalida, partida, autorJogadorId);

        return ResultadoPartida<ConfirmacaoAcaoPartida>.CriarSucesso(new ConfirmacaoAcaoPartida(
            chaveIdempotencia, true, CriarEstado(partida, autorJogadorId)));
    }

    private async Task<ResultadoPartida<ConfirmacaoAcaoPartida>> CriarConflitoAsync(
        Guid partidaId,
        Guid solicitanteId,
        CancellationToken cancellationToken)
    {
        var partida = await estados.ObterParaParticipanteAsync(partidaId, solicitanteId, cancellationToken);
        return Falha<ConfirmacaoAcaoPartida>(CodigoResultadoPartida.ConflitoDeEstado, partida, solicitanteId);
    }

    private static CorePartida CriarPartidaCore(EntidadePartida partida)
    {
        var motor = new CorePartida(partida.PontuacaoAlvo);
        foreach (var participacao in partida.Participacoes.OrderBy(item => item.Posicao))
            motor.AdicionarJogador(new JogadorCore(participacao.JogadorId, participacao.Jogador.Nome, participacao.Pontos));
        return motor;
    }

    private static CoreRodada ReidratarRodada(EntidadePartida partida, RodadaPartida rodada)
    {
        var jogadoresCore = partida.Participacoes.OrderBy(item => item.Posicao)
            .Select(item => new JogadorCore(item.JogadorId, item.Jogador.Nome, item.Pontos)).ToDictionary(item => item.Id);
        var maos = partida.Participacoes.OrderBy(item => item.Posicao).Select(item =>
            (jogadoresCore[item.JogadorId], (IReadOnlyCollection<Peca>)item.Mao.Select(pedra => new Peca(pedra.ValorA, pedra.ValorB)).ToList())).ToList();
        var pecasTabuleiro = rodada.Acoes.Where(acao => acao.FoiAceita && acao.Tipo == TipoAcaoPartida.Jogada)
            .OrderBy(acao => acao.OrdemRecebimento)
            .Select(acao => (new Peca(acao.ValorA!.Value, acao.ValorB!.Value), ConverterLadoCore(acao.Lado!.Value)));
        return CoreRodada.Reidratar(maos, rodada.Cava.OrderBy(pedra => pedra.Ordem).Select(pedra => new Peca(pedra.ValorA, pedra.ValorB)).ToList(),
            pecasTabuleiro, rodada.JogadorDoTurnoId ?? throw new InvalidOperationException("Rodada sem turno persistido."),
            rodada.Situacao == SituacaoRodadaPartida.Encerrada, ConverterTipo(rodada.MotivoEncerramento), rodada.VencedorJogadorId,
            rodada.PedraAberturaValorA is null ? null : new Peca(rodada.PedraAberturaValorA.Value, rodada.PedraAberturaValorB!.Value));
    }

    private static RodadaPartida? AtualizarAposAcao(EntidadePartida partida, RodadaPartida rodada, CoreRodada motor)
    {
        AtualizarRodada(partida, rodada, motor);
        AtualizarPontuacoes(partida, motor);

        if (partida.Participacoes.Any(item => item.Pontos >= partida.PontuacaoAlvo))
        {
            partida.Status = nameof(SituacaoPartida.Encerrada);
            partida.VencedorJogadorId = partida.Participacoes.OrderByDescending(item => item.Pontos).ThenBy(item => item.Posicao).First().JogadorId;
            partida.EncerradaEmUtc = DateTime.UtcNow;
            return null;
        }

        if (motor.Status != StatusRodada.Finalizada)
            return null;

        var proxima = CriarPartidaCore(partida);
        proxima.IniciarNovaRodada();
        var rodadaNova = new RodadaPartida { Numero = rodada.Numero + 1, IniciadaEmUtc = DateTime.UtcNow };
        partida.AdicionarRodada(rodadaNova);
        AtualizarRodada(partida, rodadaNova, proxima.RodadaAtual!);
        return rodadaNova;
    }

    private static void AtualizarPontuacoes(EntidadePartida partida, CoreRodada motor)
    {
        foreach (var mao in motor.MaosJogadores)
            partida.Participacoes.Single(item => item.JogadorId == mao.Jogador.Id).Pontos = mao.Jogador.Pontuacao;
    }

    private static void AtualizarRodada(EntidadePartida partida, RodadaPartida destino, CoreRodada origem)
    {
        destino.Situacao = origem.Status == StatusRodada.Finalizada ? SituacaoRodadaPartida.Encerrada : SituacaoRodadaPartida.EmAndamento;
        destino.JogadorDoTurnoId = origem.JogadorAtual.Jogador.Id;
        destino.QuantidadeNaCava = origem.QuantidadeNaCava;
        destino.PontaEsquerda = origem.Tabuleiro.PontaEsquerda;
        destino.PontaDireita = origem.Tabuleiro.PontaDireita;
        destino.PedraAberturaValorA = origem.PecaAberturaObrigatoria?.ValorA;
        destino.PedraAberturaValorB = origem.PecaAberturaObrigatoria?.ValorB;
        destino.VencedorJogadorId = origem.GetVencedor()?.Id;
        destino.MotivoEncerramento = origem.TipoFinalizacao switch
        {
            TipoFinalizacaoRodada.JogadorBateu => MotivoEncerramentoRodada.MaoVazia,
            TipoFinalizacaoRodada.TabuleiroTravado => MotivoEncerramentoRodada.TabuleiroTravado,
            _ => null
        };
        destino.EncerradaEmUtc = origem.Status == StatusRodada.Finalizada ? DateTime.UtcNow : null;

        foreach (var participacao in partida.Participacoes)
        {
            participacao.Mao.Clear();
            var mao = origem.MaosJogadores.Single(item => item.Jogador.Id == participacao.JogadorId);
            participacao.QuantidadePedrasNaMao = mao.Pecas.Count;
            foreach (var peca in mao.Pecas)
                participacao.Mao.Add(new PedraMaoPartida { PartidaId = partida.Id, JogadorId = participacao.JogadorId, ValorA = peca.ValorA, ValorB = peca.ValorB });
        }
        destino.Cava.Clear();
        var ordem = 1;
        foreach (var peca in origem.PecasNaCava)
            destino.Cava.Add(new PedraCavaPartida { RodadaPartidaId = destino.Id, Ordem = ordem++, ValorA = peca.ValorA, ValorB = peca.ValorB });
    }

    private void RegistrarPedrasNovas(EntidadePartida partida, RodadaPartida rodada)
    {
        foreach (var participacao in partida.Participacoes)
            foreach (var pedra in participacao.Mao)
                estados.AdicionarAoContexto(pedra);
        foreach (var pedra in rodada.Cava)
            estados.AdicionarAoContexto(pedra);
    }

    private static AcaoPartida RegistrarAcao(EntidadePartida partida, Guid autor, TipoAcaoPartida tipo, Guid chave,
        Guid? rodadaId = null, PecaPartida? peca = null, LadoJogavel? lado = null, int pontos = 0)
    {
        var versaoAnterior = partida.VersaoEstado;
        var acao = new AcaoPartida
        {
            PartidaId = partida.Id,
            AutorJogadorId = autor,
            RodadaPartidaId = rodadaId,
            Tipo = tipo,
            ChaveIdempotencia = chave,
            OrdemRecebimento = partida.Acoes.Count == 0 ? 1 : partida.Acoes.Max(item => item.OrdemRecebimento) + 1,
            VersaoEsperada = versaoAnterior,
            VersaoResultante = versaoAnterior + 1,
            RecebidaEmUtc = DateTime.UtcNow,
            FoiAceita = true,
            ValorA = peca?.ValorA,
            ValorB = peca?.ValorB,
            Lado = lado is null ? null : ConverterLadoPersistido(lado.Value),
            PontosConcedidos = pontos
        };
        partida.RegistrarAcao(acao);
        partida.VersaoEstado++;
        return acao;
    }

    private static EstadoPartidaParticipante CriarEstado(EntidadePartida partida, Guid solicitanteId)
    {
        var rodada = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        var mao = partida.Participacoes.SingleOrDefault(item => item.JogadorId == solicitanteId)?.Mao
            .OrderBy(pedra => pedra.ValorA).ThenBy(pedra => pedra.ValorB)
            .Select(pedra => new PecaPartida(pedra.ValorA, pedra.ValorB)).ToList() ?? [];
        var tabuleiro = CriarTabuleiro(rodada);
        var (proximaAcao, pedraAbertura) = CalcularProximaAcao(partida, rodada, solicitanteId, mao, tabuleiro);
        var participacao = partida.Participacoes.SingleOrDefault(item => item.JogadorId == solicitanteId);
        return new EstadoPartidaParticipante(partida.Id, partida.VersaoEstado, ConverterSituacao(partida.Status), rodada?.Numero ?? 0,
            rodada?.JogadorDoTurnoId, mao, tabuleiro, CriarResumos(partida, solicitanteId), partida.VencedorJogadorId,
            proximaAcao, pedraAbertura, participacao?.Id ?? Guid.Empty);
    }

    private void EnfileirarEventos(EntidadePartida partida, string tipo)
    {
        var ocorridoEm = DateTime.UtcNow;
        foreach (var participacao in partida.Participacoes.Where(item => !string.IsNullOrWhiteSpace(item.WebhookUrl)))
        {
            var eventoId = Guid.NewGuid();
            var envelope = new EventoEstadoPartidaWebhook(eventoId, partida.Id, participacao.Id,
                partida.VersaoEstado, tipo, ocorridoEm, CriarEstado(partida, participacao.JogadorId));
            estados.AdicionarAoContexto(new EventoWebhook
            {
                Id = eventoId,
                PartidaId = partida.Id,
                JogadorId = participacao.JogadorId,
                VersaoEstado = partida.VersaoEstado,
                Tipo = tipo,
                Destino = participacao.WebhookUrl!,
                ConteudoJson = JsonSerializer.Serialize(envelope, OpcoesJsonWebhook),
                CriadoEmUtc = ocorridoEm,
                ProximaTentativaEmUtc = ocorridoEm
            });
        }
    }

    private static readonly JsonSerializerOptions OpcoesJsonWebhook = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static EstadoPartidaObservador CriarObservacao(EntidadePartida partida)
    {
        var rodada = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        return new EstadoPartidaObservador(partida.Id, partida.VersaoEstado, ConverterSituacao(partida.Status), rodada?.Numero ?? 0,
            rodada?.JogadorDoTurnoId, CriarTabuleiro(rodada), CriarResumos(partida, Guid.Empty), partida.VencedorJogadorId);
    }

    private static IReadOnlyList<PecaPartida> CriarTabuleiro(RodadaPartida? rodada)
    {
        if (rodada is null)
            return [];

        var tabuleiro = new Tabuleiro();
        foreach (var acao in rodada.Acoes.Where(acao => acao.FoiAceita && acao.Tipo == TipoAcaoPartida.Jogada)
                     .OrderBy(acao => acao.OrdemRecebimento))
            tabuleiro.Colar(new Peca(acao.ValorA!.Value, acao.ValorB!.Value), ConverterLadoCore(acao.Lado!.Value));
        return tabuleiro.Pecas.Select(peca => new PecaPartida(peca.ValorA, peca.ValorB)).ToList();
    }

    private static (ProximaAcaoPartida Acao, PecaPartida? PedraAbertura) CalcularProximaAcao(
        EntidadePartida partida,
        RodadaPartida? rodada,
        Guid solicitanteId,
        IReadOnlyList<PecaPartida> mao,
        IReadOnlyList<PecaPartida> tabuleiro)
    {
        if (ConverterSituacao(partida.Status) == SituacaoPartida.Encerrada)
            return (ProximaAcaoPartida.Encerrada, null);
        if (rodada is null || rodada.Situacao != SituacaoRodadaPartida.EmAndamento || rodada.JogadorDoTurnoId != solicitanteId)
            return (ProximaAcaoPartida.Aguardar, null);

        if (tabuleiro.Count == 0 && rodada.PedraAberturaValorA is not null)
            return (ProximaAcaoPartida.JogarAbertura,
                new PecaPartida(rodada.PedraAberturaValorA.Value, rodada.PedraAberturaValorB!.Value));

        var possuiEncaixe = tabuleiro.Count == 0 || mao.Any(peca =>
            peca.ValorA == rodada.PontaEsquerda || peca.ValorB == rodada.PontaEsquerda
            || peca.ValorA == rodada.PontaDireita || peca.ValorB == rodada.PontaDireita);
        if (possuiEncaixe)
            return (ProximaAcaoPartida.Jogar, null);
        return rodada.QuantidadeNaCava > 0
            ? (ProximaAcaoPartida.Comprar, null)
            : (ProximaAcaoPartida.Passar, null);
    }

    private static IReadOnlyList<ResumoParticipantePartida> CriarResumos(EntidadePartida partida, Guid solicitanteId)
    {
        var rodada = partida.Rodadas.OrderByDescending(item => item.Numero).FirstOrDefault();
        return partida.Participacoes.OrderBy(item => item.Posicao).Select(item => new ResumoParticipantePartida(
            item.JogadorId, item.Jogador.Nome, item.Pontos,
            item.JogadorId == solicitanteId ? item.Mao.Count : item.QuantidadePedrasNaMao,
            rodada?.JogadorDoTurnoId == item.JogadorId)).ToList();
    }

    private static CodigoResultadoPartida MapearFalhaJogada(InvalidOperationException excecao) =>
        excecao.Message.Contains("compatível", StringComparison.OrdinalIgnoreCase) ? CodigoResultadoPartida.EncaixeInvalido :
        excecao.Message.Contains("pertence", StringComparison.OrdinalIgnoreCase) ? CodigoResultadoPartida.PecaAusente :
        CodigoResultadoPartida.EstadoInvalido;
    private static LadoTabuleiro ConverterLadoCore(LadoJogavel lado) => lado == LadoJogavel.Esquerda ? LadoTabuleiro.Esquerda : LadoTabuleiro.Direita;
    private static LadoTabuleiro ConverterLadoCore(LadoTabuleiroPartida lado) => lado == LadoTabuleiroPartida.Esquerda ? LadoTabuleiro.Esquerda : LadoTabuleiro.Direita;
    private static LadoJogavel ConverterLadoContrato(LadoTabuleiroPartida lado) => lado == LadoTabuleiroPartida.Esquerda ? LadoJogavel.Esquerda : LadoJogavel.Direita;
    private static LadoTabuleiroPartida ConverterLadoPersistido(LadoJogavel lado) => lado == LadoJogavel.Esquerda ? LadoTabuleiroPartida.Esquerda : LadoTabuleiroPartida.Direita;
    private static TipoFinalizacaoRodada? ConverterTipo(MotivoEncerramentoRodada? motivo) => motivo switch
    {
        MotivoEncerramentoRodada.MaoVazia => TipoFinalizacaoRodada.JogadorBateu,
        MotivoEncerramentoRodada.TabuleiroTravado => TipoFinalizacaoRodada.TabuleiroTravado,
        _ => null
    };
    private static SituacaoPartida ConverterSituacao(string status) => status switch
    {
        nameof(SituacaoPartida.EmAndamento) => SituacaoPartida.EmAndamento,
        nameof(SituacaoPartida.Encerrada) => SituacaoPartida.Encerrada,
        _ => SituacaoPartida.AguardandoInicio
    };
}
