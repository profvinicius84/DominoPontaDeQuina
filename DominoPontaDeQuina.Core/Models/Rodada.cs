using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Interfaces;
using System.Collections.ObjectModel;

namespace DominoPontaDeQuina.Core.Models;

/// <inheritdoc cref="IRodada"/>
public class Rodada : IRodada
{
    private readonly Stack<Jogada> _jogadas = [];
    private readonly Queue<MaoJogador> _jogadores = [];
    private readonly Queue<Peca> _cava = [];
    private readonly IReadOnlyList<Peca>? _pecasIniciais;
    private Jogador? _vencedor;
    private bool _compraRealizadaNoTurno;

    /// <summary>
    /// Cria uma rodada. Uma coleção completa de 28 peças pode ser informada para
    /// tornar a distribuição reproduzível em validações do motor.
    /// </summary>
    public Rodada(IEnumerable<Peca>? pecasIniciais = null)
    {
        if (pecasIniciais is null)
            return;

        var pecas = pecasIniciais.ToList();
        if (pecas.Count != 28 || pecas.Distinct().Count() != 28)
            throw new ArgumentException("A distribuição determinística exige as 28 peças distintas do conjunto.", nameof(pecasIniciais));

        _pecasIniciais = pecas.AsReadOnly();
    }

    public Tabuleiro Tabuleiro { get; } = new();
    public ReadOnlyCollection<MaoJogador> MaosJogadores => _jogadores.ToList().AsReadOnly();
    public int QuantidadeNaCava => _cava.Count;
    /// <summary>Expõe a ordem atual da cava para persistência e recuperação do motor.</summary>
    public IReadOnlyCollection<Peca> PecasNaCava => _cava.ToList().AsReadOnly();
    public ReadOnlyCollection<Jogada> HistoricoJogadas => _jogadas.Reverse().ToList().AsReadOnly();
    public MaoJogador JogadorAtual => _jogadores.Count > 0
        ? _jogadores.Peek()
        : throw new InvalidOperationException("A rodada não possui jogador atual.");
    public StatusRodada Status { get; private set; } = StatusRodada.NaoIniciada;
    public TipoFinalizacaoRodada? TipoFinalizacao { get; private set; }
    /// <summary>
    /// Obtém a pedra escolhida pelo critério de abertura. Enquanto não houver
    /// jogada no tabuleiro, somente essa pedra pode iniciar a rodada.
    /// </summary>
    public Peca? PecaAberturaObrigatoria { get; private set; }

    public void Iniciar(ReadOnlyCollection<Jogador> jogadores, Rodada? rodadaAnterior = null)
    {
        ArgumentNullException.ThrowIfNull(jogadores);
        if (Status != StatusRodada.NaoIniciada)
            throw new InvalidOperationException("A rodada já foi iniciada.");
        if (jogadores.Count != 2 || jogadores.Select(jogador => jogador.Id).Distinct().Count() != 2)
            throw new InvalidOperationException("Uma rodada exige exatamente dois jogadores distintos.");

        var maos = DistribuirPecas(jogadores);
        var abertura = GetAbertura(maos);
        PecaAberturaObrigatoria = abertura.Peca;
        OrganizaJogadores(maos, abertura.Jogador);
        Status = StatusRodada.EmAndamento;
    }

    /// <summary>Obtém a ação atual, comprando somente uma pedra quando necessário.</summary>
    public Jogada ObterJogadaAtual()
    {
        GarantirEmAndamento();
        if (Tabuleiro.EstaVazio && PecaAberturaObrigatoria.HasValue)
            return new Jogada(JogadorAtual.Jogador, PecaAberturaObrigatoria.Value, lado: LadoTabuleiro.Esquerda);

        var jogada = JogadorAtual.GetJogada(Tabuleiro);
        if (!jogada.EhPassarVez() || _cava.Count == 0)
            return jogada;

        JogadorAtual.AdicionarPeca(_cava.Dequeue());
        _compraRealizadaNoTurno = true;
        return JogadorAtual.GetJogada(Tabuleiro);
    }

    public void RegistrarJogada(Jogada jogada)
    {
        ArgumentNullException.ThrowIfNull(jogada);
        GarantirEmAndamento();

        if (jogada.Jogador.Id != JogadorAtual.Jogador.Id)
            throw new InvalidOperationException("A jogada não pertence ao jogador do turno.");

        if (Tabuleiro.EstaVazio && (!jogada.Peca.HasValue || !PecaAberturaObrigatoria.HasValue
            || !jogada.Peca.Value.Equals(PecaAberturaObrigatoria.Value)))
            throw new InvalidOperationException("A primeira jogada deve usar a pedra obrigatória de abertura.");

        if (jogada.EhPassarVez())
        {
            if ((_cava.Count != 0 && !_compraRealizadaNoTurno) || PossuiJogadaCompativel(JogadorAtual))
                throw new InvalidOperationException("A passagem não é permitida neste estado.");
        }
        else
        {
            if (jogada.Peca is null || jogada.Lado is null || !JogadorAtual.PossuiPeca(jogada.Peca.Value))
                throw new InvalidOperationException("A peça informada não pertence à mão do jogador atual.");
            if (!Tabuleiro.PodeColar(jogada.Peca.Value, jogada.Lado.Value))
                throw new InvalidOperationException("A peça não é compatível com a ponta escolhida.");

            if (!JogadorAtual.AplicarJogada(jogada))
                throw new InvalidOperationException("Não foi possível retirar a peça da mão do jogador atual.");
            Tabuleiro.Colar(jogada.Peca.Value, jogada.Lado.Value);
        }

        jogada.MarcarComoAplicada();
        _compraRealizadaNoTurno = false;
        _jogadas.Push(jogada);
        if (!jogada.EhPassarVez())
            CalcularPontuacao(jogada.Jogador);

        if (!VerificarBatida() && !VerificarTabuleiroTravado())
            AvancarTurno();
    }

    public bool VerificarBatida()
    {
        if (Status != StatusRodada.EmAndamento)
            return false;

        var maoVencedora = _jogadores.FirstOrDefault(mao => mao.EstaSemPecas());
        if (maoVencedora is null)
            return false;

        _vencedor = maoVencedora.Jogador;
        Status = StatusRodada.Finalizada;
        TipoFinalizacao = TipoFinalizacaoRodada.JogadorBateu;
        return true;
    }

    public bool VerificarTabuleiroTravado()
    {
        if (Status != StatusRodada.EmAndamento || _cava.Count != 0 || !Tabuleiro.EstaTravado(_jogadores))
            return false;

        var maos = _jogadores.ToList();
        _vencedor = maos.OrderBy(mao => mao.SomarPecasNaMao()).First().Jogador;
        foreach (var mao in maos)
        {
            var pontosTransferidos = mao.SomarPecasNaMao();
            if (pontosTransferidos % 5 == 0)
                maos.First(maoAdversaria => maoAdversaria.Jogador.Id != mao.Jogador.Id)
                    .Jogador.SomarPontos(pontosTransferidos);
        }

        Status = StatusRodada.Finalizada;
        TipoFinalizacao = TipoFinalizacaoRodada.TabuleiroTravado;
        return true;
    }

    public Jogador? GetVencedor() => _vencedor;

    /// <summary>
    /// Reidrata uma rodada já iniciada sem redistribuir pedras nem alterar a ordem da cava.
    /// O histórico de peças jogadas deve estar na mesma ordem em que foi aceito pelo servidor.
    /// </summary>
    public static Rodada Reidratar(
        IReadOnlyCollection<(Jogador Jogador, IReadOnlyCollection<Peca> Mao)> maos,
        IReadOnlyCollection<Peca> cava,
        IEnumerable<(Peca Peca, LadoTabuleiro Lado)> pecasNoTabuleiro,
        Guid jogadorDoTurnoId,
        bool encerrada = false,
        TipoFinalizacaoRodada? tipoFinalizacao = null,
        Guid? vencedorId = null,
        Peca? pecaAberturaObrigatoria = null)
    {
        if (maos.Count != 2 || maos.Select(item => item.Jogador.Id).Distinct().Count() != 2)
            throw new ArgumentException("A recuperação exige exatamente dois jogadores distintos.", nameof(maos));

        var rodada = new Rodada();
        foreach (var item in maos)
        {
            var mao = new MaoJogador(item.Jogador);
            foreach (var peca in item.Mao)
                mao.AdicionarPeca(peca);
            rodada._jogadores.Enqueue(mao);
        }

        foreach (var peca in cava)
            rodada._cava.Enqueue(peca);
        foreach (var (peca, lado) in pecasNoTabuleiro)
            rodada.Tabuleiro.Colar(peca, lado);

        while (rodada._jogadores.Peek().Jogador.Id != jogadorDoTurnoId)
            rodada.AvancarTurno();

        rodada.Status = encerrada ? StatusRodada.Finalizada : StatusRodada.EmAndamento;
        rodada.TipoFinalizacao = tipoFinalizacao;
        rodada.PecaAberturaObrigatoria = pecaAberturaObrigatoria;
        rodada._vencedor = vencedorId is null
            ? null
            : rodada._jogadores.Single(item => item.Jogador.Id == vencedorId.Value).Jogador;
        return rodada;
    }

    /// <summary>
    /// Executa uma jogada escolhida pelo participante. Quando não existir encaixe
    /// na mão, compra somente a próxima pedra da cava se ela for exatamente a peça
    /// informada e puder ser jogada.
    /// </summary>
    public void RegistrarJogadaComCompra(Jogador jogador, Peca peca, LadoTabuleiro lado)
    {
        GarantirEmAndamento();
        if (jogador.Id != JogadorAtual.Jogador.Id)
            throw new InvalidOperationException("A jogada não pertence ao jogador do turno.");

        if (!JogadorAtual.PossuiPeca(peca) && !PossuiJogadaCompativel(JogadorAtual) && _cava.Count > 0
            && _cava.Peek().Equals(peca) && Tabuleiro.PodeColar(peca, lado))
            JogadorAtual.AdicionarPeca(_cava.Dequeue());

        RegistrarJogada(new Jogada(jogador, peca, lado: lado));
    }

    /// <summary>
    /// Compra exatamente uma pedra quando não houver encaixe na mão. Mantém o
    /// turno se a pedra comprada permitir jogada; caso contrário, registra a
    /// passagem e avança o turno.
    /// </summary>
    /// <returns><see langword="true"/> quando a compra resultou em passagem.</returns>
    public bool ComprarOuPassar(Jogador jogador)
    {
        GarantirEmAndamento();
        if (jogador.Id != JogadorAtual.Jogador.Id)
            throw new InvalidOperationException("A compra não pertence ao jogador do turno.");
        if (PossuiJogadaCompativel(JogadorAtual))
            throw new InvalidOperationException("A compra não é permitida quando há jogada compatível.");

        if (_cava.Count > 0)
        {
            JogadorAtual.AdicionarPeca(_cava.Dequeue());
            _compraRealizadaNoTurno = true;
            if (PossuiJogadaCompativel(JogadorAtual))
                return false;
        }

        RegistrarJogada(new Jogada(jogador));
        return true;
    }

    /// <summary>Compatibilidade para o fluxo anterior de compra seguida de passagem.</summary>
    public void PassarVezComCompra(Jogador jogador) => ComprarOuPassar(jogador);

    private List<MaoJogador> DistribuirPecas(ReadOnlyCollection<Jogador> jogadores)
    {
        var pecas = _pecasIniciais?.ToList() ?? CriarConjuntoCompleto();
        if (_pecasIniciais is null)
            Embaralhar(pecas);
        var maos = jogadores.Select(jogador => new MaoJogador(jogador)).ToList();

        for (var indice = 0; indice < 14; indice++)
            maos[indice % 2].AdicionarPeca(pecas[indice]);
        for (var indice = 14; indice < pecas.Count; indice++)
            _cava.Enqueue(pecas[indice]);

        return maos;
    }

    private static List<Peca> CriarConjuntoCompleto()
    {
        var pecas = new List<Peca>(28);
        for (var valorA = 0; valorA <= 6; valorA++)
            for (var valorB = valorA; valorB <= 6; valorB++)
                pecas.Add(new Peca(valorA, valorB));
        return pecas;
    }

    private static void Embaralhar(IList<Peca> pecas)
    {
        for (var indice = pecas.Count - 1; indice > 0; indice--)
        {
            var outroIndice = Random.Shared.Next(indice + 1);
            (pecas[indice], pecas[outroIndice]) = (pecas[outroIndice], pecas[indice]);
        }
    }

    private static (Jogador Jogador, Peca Peca) GetAbertura(List<MaoJogador> maos)
    {
        var candidatas = maos
            .SelectMany(mao => mao.Pecas.Where(peca => peca.EhDupla)
                .Select(peca => new { mao.Jogador, Peca = peca }));

        if (!candidatas.Any())
            candidatas = maos.SelectMany(mao => mao.Pecas.Select(peca => new { mao.Jogador, Peca = peca }));

        return candidatas
            .OrderByDescending(candidata => candidata.Peca.MaiorFace)
            .ThenByDescending(candidata => candidata.Peca.MenorFace)
            .ThenBy(candidata => maos.FindIndex(mao => mao.Jogador.Id == candidata.Jogador.Id))
            .Select(candidata => (candidata.Jogador, candidata.Peca))
            .First();
    }

    private void OrganizaJogadores(List<MaoJogador> maos, Jogador primeiroJogador)
    {
        var indiceInicial = maos.FindIndex(mao => mao.Jogador.Id == primeiroJogador.Id);
        for (var deslocamento = 0; deslocamento < maos.Count; deslocamento++)
            _jogadores.Enqueue(maos[(indiceInicial + deslocamento) % maos.Count]);
    }

    private bool PossuiJogadaCompativel(MaoJogador mao) =>
        mao.Pecas.Any(peca => Tabuleiro.PodeColar(peca, LadoTabuleiro.Esquerda)
            || Tabuleiro.PodeColar(peca, LadoTabuleiro.Direita));

    private void CalcularPontuacao(Jogador jogador)
    {
        var soma = Tabuleiro.SomarPontasExternas();
        if (soma > 0 && soma % 5 == 0)
            jogador.SomarPontos(soma / 5);
    }

    private void AvancarTurno() => _jogadores.Enqueue(_jogadores.Dequeue());

    private void GarantirEmAndamento()
    {
        if (Status != StatusRodada.EmAndamento)
            throw new InvalidOperationException("A rodada não está em andamento.");
    }
}
