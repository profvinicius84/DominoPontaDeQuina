using DominoPontaDeQuina.Client;
using Microsoft.Maui.Controls.Shapes;

namespace DominoPontaDeQuina.Maui;

/// <summary>Interface de referência: nenhuma regra de jogo é executada no cliente.</summary>
public sealed class MainPage : ContentPage
{
    private const string EnderecoPadrao = "https://domino-quina-api-b1-pf2261.azurewebsites.net";
    private readonly Entry endereco = new() { Text = EnderecoPadrao, Placeholder = "Endereço HTTPS da API" };
    private readonly Entry email = new() { Placeholder = "E-mail autorizado", Keyboard = Keyboard.Email };
    private readonly Entry token = new() { Placeholder = "Token recebido por e-mail", IsPassword = true };
    private readonly Entry nomePerfil = new() { Placeholder = "Nome do novo jogador" };
    private readonly Picker perfis = new() { Title = "Escolha seu jogador", ItemDisplayBinding = new Binding(nameof(PerfilJogador.Nome)) };
    private readonly FluxoEstadoCliente fluxo = new() { EsperaCallback = TimeSpan.Zero };
    private readonly Picker partidasDisponiveis = new() { Title = "Partidas com uma vaga", ItemDisplayBinding = new Binding(nameof(PartidaDisponivel.PartidaId)) };
    private (Guid JogadorId, string Chave, string Token, string Endereco)? ativacao;
    private FileStream? travaPerfil;
    private string? escopoTrava;
    private string? chavePartida;
    private readonly Entry identificador = new() { Placeholder = "Identificador da partida" };
    private readonly Entry alvo = new() { Text = "20", Placeholder = "Pontuação-alvo", Keyboard = Keyboard.Numeric };
    private readonly Entry versaoIngresso = new() { Text = "0", Placeholder = "Versão para ingresso", Keyboard = Keyboard.Numeric };
    private readonly Picker pedras = new() { Title = "Escolha uma pedra da sua mão" };
    private readonly Picker lado = new() { Title = "Ponta", ItemsSource = new[] { "Esquerda", "Direita" }, SelectedIndex = 0 };
    private readonly Label mensagem = new() { Text = "Entre para começar a jogar." };
    private readonly Label resumo = new();
    private readonly Label mesa = new();
    private readonly Label historico = new();
    private readonly Label tituloPartida = new();
    private readonly Label turno = new();
    private readonly Label selecao = new() { Text = "Toque em uma pedra da sua mão." };
    private readonly Label placar = new();
    private readonly Label cabecalho = new();
    private readonly Label descricaoEtapa = new();
    private readonly HorizontalStackLayout pedrasDaMesa = new() { Spacing = 7 };
    private readonly HorizontalStackLayout pedrasDaMao = new() { Spacing = 10 };
    private readonly VerticalStackLayout painelAcesso = new() { Spacing = 16 };
    private readonly VerticalStackLayout painelCredenciais = new() { Spacing = 12 };
    private readonly VerticalStackLayout painelPerfil = new() { Spacing = 12 };
    private readonly VerticalStackLayout painelSala = new() { Spacing = 16 };
    private readonly VerticalStackLayout entradaPorCodigo = new() { Spacing = 8, IsVisible = false };
    private readonly VerticalStackLayout configuracaoServidor = new() { Spacing = 8, IsVisible = false };
    private readonly VerticalStackLayout criacaoJogador = new() { Spacing = 10, IsVisible = false };
    private readonly VerticalStackLayout escolhaPartida = new() { Spacing = 20 };
    private readonly VerticalStackLayout partidaAguardando = new() { Spacing = 12 };
    private readonly VerticalStackLayout painelJogo = new() { Spacing = 16 };
    private Border? cartaoAcesso;
    private Border? cartaoPerfil;
    private Border? cartaoSala;
    private Button? botaoSair;
    private Button? botaoEsquerda;
    private Button? botaoDireita;
    private Button? botaoComprar;
    private Button? botaoPassar;
    private Button? botaoVoltarSala;
    private Button? botaoCancelarPartida;
    private Button? botaoReiniciarPartida;
    private Pedra? pedraSelecionada;
    private bool autenticado;
    private bool recuperacaoInicialTentada;
    private readonly List<Button> botoes = [];
    private readonly ActivityIndicator indicador = new();
    private EstadoPartida? estado;
    private HttpClient? http;
    private DominoApiClient? api;
    private string? enderecoAtual;
    private string? tokenAtual;
    private bool ocupado;
    private bool consultando;
    private DateTime proximaConsultaReinicioUtc;
    private int geracaoConsulta;
    // Uma tentativa ambígua conserva chave e intenção para uma repetição idempotente.
    private (string Intencao, Guid Chave)? tentativa;

    public MainPage()
    {
        Title = "Dominó Ponta de Quina";
        BackgroundColor = Cor("#101B18");
        fluxo.Atualizado += atual => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (fluxo.Estado == atual) Mostrar(atual);
        });
        var conteudo = new VerticalStackLayout { Padding = new Thickness(24, 28), Spacing = 20, MaximumWidthRequest = 760 };
        conteudo.Add(new Label { Text = "PONTA DE QUINA", TextColor = Cor("#E7BD71"), FontSize = 15, FontAttributes = FontAttributes.Bold, CharacterSpacing = 3 });
        cabecalho.TextColor = Colors.White; cabecalho.FontSize = 34; cabecalho.FontAttributes = FontAttributes.Bold;
        descricaoEtapa.TextColor = Cor("#AFC0B5"); descricaoEtapa.FontSize = 16;
        conteudo.Add(cabecalho);
        conteudo.Add(descricaoEtapa);
        mensagem.TextColor = Cor("#D3DED6");
        mensagem.FontSize = 15;
        conteudo.Add(mensagem);
        indicador.Color = Cor("#E7BD71");
        conteudo.Add(indicador);

        painelAcesso.Add(new Label { Text = "Receber um token", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        endereco.TextColor = email.TextColor = token.TextColor = nomePerfil.TextColor = Colors.White;
        identificador.TextColor = alvo.TextColor = versaoIngresso.TextColor = Colors.White;
        painelCredenciais.Add(email);
        painelCredenciais.Add(Botao("Enviar token por e-mail", async () =>
        {
            await Cliente().SolicitarTokenAsync(email.Text ?? "");
            mensagem.Text = "Solicitação recebida. Se elegível, você receberá o token por e-mail.";
        }));
        painelCredenciais.Add(token);
        painelCredenciais.Add(Botao("Entrar com token", async () =>
        {
            await Cliente().SessaoAsync(Token());
            await SecureStorage.Default.SetAsync("token-" + EscopoApi(), Token());
            await ListarPerfisAsync();
            autenticado = true; SalvarEndereco(); AtualizarPaineis();
            mensagem.Text = "Acesso válido. Escolha seu jogador.";
        }));
        painelCredenciais.Add(Botao("Configurar servidor", async () =>
        {
            configuracaoServidor.IsVisible = !configuracaoServidor.IsVisible;
            await Task.CompletedTask;
        }, false));
        configuracaoServidor.Add(new Label { Text = "Endereço da API", TextColor = Cor("#AFC0B5"), FontSize = 12 });
        configuracaoServidor.Add(endereco);
        painelCredenciais.Add(configuracaoServidor);
        painelAcesso.Add(painelCredenciais);
        painelPerfil.Add(new Label { Text = "Escolha seu jogador", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        painelPerfil.Add(perfis);
        painelPerfil.Add(Botao("Continuar com este jogador", async () =>
        {
            await AtivarPerfilAsync();
        }));
        painelPerfil.Add(Botao("Criar novo jogador", async () =>
        {
            criacaoJogador.IsVisible = !criacaoJogador.IsVisible;
            await Task.CompletedTask;
        }, false));
        criacaoJogador.Add(nomePerfil);
        criacaoJogador.Add(Botao("Criar e selecionar", async () =>
        {
            var criado = await Cliente().CriarJogadorAsync(Token(), nomePerfil.Text ?? "");
            await ListarPerfisAsync();
            perfis.SelectedItem = (perfis.ItemsSource as IEnumerable<PerfilJogador>)?.FirstOrDefault(p => p.JogadorId == criado.JogadorId);
            criacaoJogador.IsVisible = false;
            mensagem.Text = "Jogador criado e selecionado. Continue para a partida.";
        }));
        painelPerfil.Add(criacaoJogador);
        painelPerfil.Add(Botao("Atualizar lista", ListarPerfisAsync, false));

        painelSala.Add(new Label { Text = "Sua próxima partida", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        escolhaPartida.Add(new Label { Text = "Criar partida", TextColor = Cor("#E7BD71"), FontSize = 18, FontAttributes = FontAttributes.Bold });
        escolhaPartida.Add(new Label { Text = "Pontos para vencer", TextColor = Cor("#AFC0B5"), FontSize = 12 });
        escolhaPartida.Add(alvo);
        escolhaPartida.Add(Botao("Criar partida", async () =>
        {
            if (!int.TryParse(alvo.Text, out var pontos) || pontos <= 0) throw new ArgumentException("Informe uma pontuação positiva.");
            var cliente = ClienteJogador(); var acesso = Token();
            await fluxo.ExecutarAsync(async () =>
            {
                var criada = await cliente.CriarAsync(acesso, pontos);
                identificador.Text = criada.PartidaId.ToString(); return criada;
            }, () => cliente.EstadoAsync(acesso, PartidaId()));
            mensagem.Text = "Mesa criada. Compartilhe o código com o outro jogador.";
        }));
        escolhaPartida.Add(new BoxView { Color = Cor("#365449"), HeightRequest = 1, Margin = new Thickness(0, 8) });
        escolhaPartida.Add(new Label { Text = "Entrar em uma partida", TextColor = Cor("#E7BD71"), FontSize = 18, FontAttributes = FontAttributes.Bold });
        escolhaPartida.Add(Botao("Ver partidas com vaga", async () =>
        {
            partidasDisponiveis.ItemsSource = (await Cliente().ListarPartidasAsync(Token())).ToList();
            mensagem.Text = "Selecione uma partida com vaga.";
        }, false));
        escolhaPartida.Add(partidasDisponiveis);
        partidasDisponiveis.SelectedIndexChanged += (_, _) =>
        {
            if (partidasDisponiveis.SelectedItem is PartidaDisponivel partida)
            {
                identificador.Text = partida.PartidaId.ToString();
                versaoIngresso.Text = partida.VersaoEstado.ToString();
            }
        };
        escolhaPartida.Add(Botao("Entrar na partida selecionada", () => AcaoAsync("entrar")));
        escolhaPartida.Add(Botao("Tenho um código de partida", async () =>
        {
            entradaPorCodigo.IsVisible = !entradaPorCodigo.IsVisible;
            await Task.CompletedTask;
        }, false));
        entradaPorCodigo.Add(new Label { Text = "Código da partida", TextColor = Cor("#AFC0B5"), FontSize = 12 });
        entradaPorCodigo.Add(identificador);
        entradaPorCodigo.Add(new Label { Text = "Versão para entrar", TextColor = Cor("#AFC0B5"), FontSize = 12 });
        entradaPorCodigo.Add(versaoIngresso);
        entradaPorCodigo.Add(Botao("Entrar com código", () => AcaoAsync("entrar")));
        entradaPorCodigo.Add(Botao("Retomar minha partida pelo código", AtualizarAsync, false));
        escolhaPartida.Add(entradaPorCodigo);
        painelSala.Add(escolhaPartida);

        partidaAguardando.Add(new Label { Text = "Aguardando na mesa", TextColor = Cor("#E7BD71"), FontSize = 20, FontAttributes = FontAttributes.Bold });
        resumo.TextColor = Cor("#D3DED6");
        partidaAguardando.Add(resumo);
        partidaAguardando.Add(Botao("Iniciar partida", () => AcaoAsync("iniciar")));
        partidaAguardando.Add(Botao("Atualizar mesa", AtualizarAsync, false));
        partidaAguardando.Add(Botao("Cancelar partida", CancelarPartidaAsync, false));
        painelSala.Add(partidaAguardando);
        painelSala.Add(Botao("Trocar jogador", async () =>
        {
            await ClienteJogador().LiberarJogadorAsync(Token());
            if (escopoTrava is not null) SecureStorage.Default.Remove(escopoTrava);
            ativacao = null; travaPerfil?.Dispose(); travaPerfil = null; escopoTrava = null; chavePartida = null;
            LimparEstado(); AtualizarPaineis(); mensagem.Text = "Escolha outro jogador.";
        }, false));

        tituloPartida.TextColor = Colors.White; tituloPartida.FontSize = 28; tituloPartida.FontAttributes = FontAttributes.Bold;
        turno.TextColor = Cor("#E7BD71"); turno.FontSize = 18; turno.FontAttributes = FontAttributes.Bold;
        placar.TextColor = Colors.White; placar.FontSize = 17;
        painelJogo.Add(tituloPartida);
        painelJogo.Add(placar);
        painelJogo.Add(turno);
        painelJogo.Add(new Label { Text = "MESA", TextColor = Cor("#AFC0B5"), CharacterSpacing = 2, FontAttributes = FontAttributes.Bold });
        painelJogo.Add(new Border { BackgroundColor = Cor("#176143"), Stroke = Cor("#418665"), StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) }, Padding = new Thickness(18), MinimumHeightRequest = 170,
            Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = pedrasDaMesa } });
        painelJogo.Add(mesa);
        mesa.IsVisible = false;
        painelJogo.Add(new Label { Text = "SUA MÃO", TextColor = Cor("#AFC0B5"), CharacterSpacing = 2, FontAttributes = FontAttributes.Bold });
        painelJogo.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = pedrasDaMao, HeightRequest = 116 });
        selecao.TextColor = Cor("#E7BD71");
        painelJogo.Add(selecao);
        var pontas = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 12 };
        botaoEsquerda = Botao("← Jogar na esquerda", async () => { lado.SelectedIndex = 0; await JogarAsync(); });
        botaoDireita = Botao("Jogar na direita →", async () => { lado.SelectedIndex = 1; await JogarAsync(); });
        pontas.Add(botaoEsquerda, 0, 0); pontas.Add(botaoDireita, 1, 0);
        painelJogo.Add(pontas);
        botaoComprar = Botao("Comprar pedra", () => AcaoAsync("comprar")); painelJogo.Add(botaoComprar);
        botaoPassar = Botao("Passar a vez", () => AcaoAsync("passar")); painelJogo.Add(botaoPassar);
        painelJogo.Add(Botao("Atualizar partida", AtualizarAsync, false));
        botaoCancelarPartida = Botao("Cancelar partida", CancelarPartidaAsync, false);
        painelJogo.Add(botaoCancelarPartida);
        botaoReiniciarPartida = Botao("Reiniciar com os mesmos jogadores", ReiniciarPartidaAsync, false);
        painelJogo.Add(botaoReiniciarPartida);
        painelJogo.Add(Botao("Ver resultado e jogadas", async () =>
        {
            var resultado = await ClienteJogador().ResultadoAsync(Token(), PartidaId());
            historico.Text = $"{resultado.Situacao} · vencedor: {resultado.VencedorId?.ToString() ?? "ainda não definido"}\n" +
                string.Join("\n", resultado.Eventos.Select(e => $"{e.RecebidoEmUtc:HH:mm:ss} UTC · {e.JogadorId} · {(e.PassouVez ? "passou" : e.PecaJogada?.ToString() ?? "compra")} · +{e.PontosConcedidos}"));
            mensagem.Text = "Resultado consultado.";
        }, false));
        historico.TextColor = Cor("#D3DED6"); painelJogo.Add(historico);
        botaoVoltarSala = Botao("Voltar à sala", async () =>
        {
            if (chavePartida is not null) Preferences.Default.Remove(chavePartida);
            LimparEstado(); AtualizarPaineis(); await Task.CompletedTask;
        }, false);
        painelJogo.Add(botaoVoltarSala);

        cartaoAcesso = Cartao(painelAcesso);
        cartaoPerfil = Cartao(painelPerfil);
        cartaoSala = Cartao(painelSala);
        conteudo.Add(cartaoAcesso);
        conteudo.Add(cartaoPerfil);
        conteudo.Add(cartaoSala);
        conteudo.Add(painelJogo);
        botaoSair = Botao("Sair da conta", async () =>
        {
            SecureStorage.Default.Remove("token-" + EscopoApi());
            token.Text = ""; tokenAtual = null; ativacao = null; LimparEstado();
            travaPerfil?.Dispose(); travaPerfil = null; escopoTrava = null; chavePartida = null;
            perfis.ItemsSource = null; autenticado = false;
            http?.Dispose(); http = null; api = null; enderecoAtual = null;
            AtualizarPaineis();
            mensagem.Text = "Acesso removido da tela. Token e ativação continuam válidos; use o mesmo jogador neste dispositivo para retomar.";
        }, false);
        conteudo.Add(botaoSair);
        Content = new ScrollView { Content = conteudo };
        AtualizarPaineis();
    }

    private async Task AtivarPerfilAsync()
    {
        var cliente = Cliente(); var acesso = Token();
        if (perfis.SelectedItem is not PerfilJogador perfil) throw new ArgumentException("Selecione um jogador.");
        var sessao = await cliente.SessaoAsync(acesso);
        var escopo = $"{enderecoAtual}/{sessao.UsuarioId}/{perfil.JogadorId}";
        var escopoHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(escopo)));
        var armazenamento = "ativacao-" + escopoHash;
        FileStream? novaTrava = null;
        try
        {
            if (escopoTrava != armazenamento)
            {
                try { novaTrava = new FileStream(System.IO.Path.Combine(FileSystem.AppDataDirectory, armazenamento + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) { throw new ArgumentException("Outro cliente neste dispositivo já está usando esse jogador."); }
            }
            var chave = await SecureStorage.Default.GetAsync(armazenamento) ?? DominoApiClient.GerarChaveAtivacao();
            await SecureStorage.Default.SetAsync(armazenamento, chave);
            await cliente.AtivarJogadorAsync(acesso, perfil.JogadorId, chave);
            ativacao = (perfil.JogadorId, chave, acesso, enderecoAtual!);
            chavePartida = "partida-" + escopoHash;
            if (novaTrava is not null)
            {
                travaPerfil?.Dispose();
                travaPerfil = novaTrava;
                novaTrava = null;
                escopoTrava = armazenamento;
            }
        }
        finally { novaTrava?.Dispose(); }
        LimparEstado();
        AtualizarPaineis();
        var partidaSalva = Preferences.Default.Get(chavePartida!, string.Empty);
        if (Guid.TryParse(partidaSalva, out var id) && id != Guid.Empty)
        {
            identificador.Text = id.ToString();
            try
            {
                fluxo.Recuperar(await ClienteJogador().EstadoAsync(acesso, id));
                mensagem.Text = "Partida retomada.";
            }
            catch (ApiException erro) when (erro.Status == 404)
            {
                Preferences.Default.Remove(chavePartida!);
                mensagem.Text = "A partida anterior não está mais disponível. Escolha outra partida.";
            }
        }
        else mensagem.Text = "Jogador pronto. Escolha uma partida.";
    }

    private static Color Cor(string codigo) => Color.FromArgb(codigo);
    private static Border Cartao(View conteudo) => new()
    {
        BackgroundColor = Cor("#1B3029"), Stroke = Cor("#365449"), StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
        Padding = new Thickness(22), Content = conteudo
    };
    private void AtualizarPaineis()
    {
        var emJogo = autenticado && ativacao is not null && estado?.Situacao is (SituacaoPartida.EmAndamento or SituacaoPartida.Encerrada);
        var aguardando = autenticado && ativacao is not null && estado?.Situacao == SituacaoPartida.AguardandoInicio;
        cabecalho.Text = !autenticado ? "Bem-vindo ao jogo" : ativacao is null ? "Quem vai jogar?" : emJogo ? "Na mesa" : "Escolha a partida";
        descricaoEtapa.Text = !autenticado ? "Entre com seu token para começar." : ativacao is null ? "Use um jogador existente ou crie o seu." : emJogo ? "Sua mão, o tabuleiro e a próxima jogada." : "Crie uma mesa ou entre em uma disponível.";
        painelAcesso.IsVisible = !autenticado;
        if (cartaoAcesso is not null) cartaoAcesso.IsVisible = painelAcesso.IsVisible;
        painelPerfil.IsVisible = autenticado && ativacao is null;
        if (cartaoPerfil is not null) cartaoPerfil.IsVisible = painelPerfil.IsVisible;
        painelSala.IsVisible = autenticado && ativacao is not null && estado?.Situacao is not (SituacaoPartida.EmAndamento or SituacaoPartida.Encerrada);
        if (cartaoSala is not null) cartaoSala.IsVisible = painelSala.IsVisible;
        escolhaPartida.IsVisible = !aguardando;
        partidaAguardando.IsVisible = aguardando;
        painelJogo.IsVisible = emJogo;
        if (botaoSair is not null) botaoSair.IsVisible = autenticado;
    }
    private void SalvarEndereco() => Preferences.Default.Set("ultimo-endereco-api", enderecoAtual!);
    private async Task RecuperarLoginInicialAsync()
    {
        if (recuperacaoInicialTentada) return;
        recuperacaoInicialTentada = true;
        try
        {
            endereco.Text = Preferences.Default.Get("ultimo-endereco-api", EnderecoPadrao);
            var salvo = await SecureStorage.Default.GetAsync("token-" + EscopoApi());
            if (string.IsNullOrWhiteSpace(salvo)) return;
            ocupado = true; indicador.IsRunning = true;
            mensagem.Text = "Retomando sua sessão…";
            token.Text = salvo;
            await Cliente().SessaoAsync(Token());
            await ListarPerfisAsync();
            autenticado = true;
            mensagem.Text = "Bem-vindo de volta. Escolha seu jogador.";
        }
        catch (ApiException erro) when (erro.Status == 401)
        {
            SecureStorage.Default.Remove("token-" + EscopoApi());
            token.Text = "";
            mensagem.Text = "Seu token salvo não é mais válido. Entre novamente.";
        }
        catch (Exception)
        {
            mensagem.Text = "Não foi possível retomar o acesso. Entre com seu token.";
        }
        finally
        {
            ocupado = false; indicador.IsRunning = false; AtualizarPaineis();
        }
    }

    private Button Botao(string texto, Func<Task> acao, bool principal = true)
    {
        var botao = new Button { Text = texto, HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = principal ? Cor("#D6A95B") : Cor("#274338"),
            TextColor = principal ? Cor("#17231E") : Cor("#E1E9DF"), FontAttributes = FontAttributes.Bold,
            CornerRadius = 12, HeightRequest = 48, Padding = new Thickness(14, 8) };
        botao.Clicked += async (_, _) =>
        {
            if (ocupado) return;
            ocupado = true;
            foreach (var item in botoes) item.IsEnabled = false;
            endereco.IsEnabled = token.IsEnabled = identificador.IsEnabled = false;
            indicador.IsRunning = true;
            var mensagemAnterior = mensagem.Text;
            mensagem.Text = "Aguardando resposta…";
            try
            {
                await acao();
                if (mensagem.Text == "Aguardando resposta…") mensagem.Text = mensagemAnterior;
            }
            catch (ApiException erro)
            {
                if (erro.EstadoAutoritativo is not null) fluxo.Recuperar(erro.EstadoAutoritativo);
                if (erro.Status is 401 or 403 or 404) LimparEstado();
                if (erro.Status == 401)
                {
                    token.Text = ""; autenticado = false; ativacao = null;
                    travaPerfil?.Dispose(); travaPerfil = null; escopoTrava = null;
                    AtualizarPaineis();
                }
                if (erro.Status < 500) tentativa = null;
                mensagem.Text = erro.Status switch
                {
                    401 => "Token ausente, inválido ou revogado. Verifique o token informado ou solicite outro.",
                    409 when erro.Codigo == "JOGADOR_JA_ATIVO" => "Esse jogador já está ativo em outro cliente. Escolha outro perfil.",
                    409 => "Conflito: a ação foi rejeitada. Consulte o estado atual antes de tentar novamente.",
                    >= 500 => "Serviço indisponível ou resposta incerta. Consulte o estado; repetir a mesma ação conserva a chave.",
                    _ => $"Ação rejeitada: {erro.Codigo} (HTTP {erro.Status})."
                };
            }
            catch (ArgumentException erro) { mensagem.Text = erro.Message; }
            catch (Exception) { mensagem.Text = "Não foi possível concluir a operação ou acessar o armazenamento seguro. Tente novamente."; }
            finally
            {
                ocupado = false; indicador.IsRunning = false;
                endereco.IsEnabled = token.IsEnabled = identificador.IsEnabled = true;
                foreach (var item in botoes) item.IsEnabled = true;
                AtualizarComandos();
            }
        };
        botoes.Add(botao);
        return botao;
    }
    private DominoApiClient Cliente()
    {
        var uri = DominoApiClient.ValidarEndereco(endereco.Text ?? "");
        if (enderecoAtual != uri.AbsoluteUri)
        {
            http?.Dispose();
            http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(15) };
            api = new DominoApiClient(http);
            enderecoAtual = uri.AbsoluteUri;
            LimparEstado();
        }
        return api!;
    }
    private string Token()
    {
        var valor = token.Text?.Trim();
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("Informe o token recebido por e-mail.");
        if (valor != tokenAtual) { LimparEstado(); tokenAtual = valor; }
        return valor;
    }
    private async Task ListarPerfisAsync()
    {
        perfis.ItemsSource = (await Cliente().ListarJogadoresAsync(Token())).ToList();
        mensagem.Text = "Selecione um jogador da sua conta.";
    }
    private DominoApiClient ClienteJogador()
    {
        var cliente = Cliente(); var acesso = Token();
        if (ativacao is not { } atual || atual.Token != acesso || atual.Endereco != enderecoAtual)
            throw new ArgumentException("Ative um jogador neste cliente antes de jogar.");
        cliente.SelecionarJogador(atual.JogadorId, atual.Chave);
        return cliente;
    }
    private Guid PartidaId() => Guid.TryParse(identificador.Text, out var id) && id != Guid.Empty ? id
        : throw new ArgumentException("Informe o identificador válido da partida.");
    private long Versao(string acao, Guid id)
    {
        if (estado?.PartidaId == id) return estado.VersaoEstado;
        if (acao == "entrar" && long.TryParse(versaoIngresso.Text, out var valor) && valor >= 0) return valor;
        throw new ArgumentException("Consulte o estado da partida antes de executar a ação.");
    }
    private Guid Chave(string intencao)
    {
        if (tentativa?.Intencao != intencao) tentativa = (intencao, Guid.NewGuid());
        return tentativa.Value.Chave;
    }
    private async Task AcaoAsync(string acao)
    {
        var cliente = ClienteJogador(); var segredo = Token(); var id = PartidaId(); var versao = Versao(acao, id);
        await fluxo.ExecutarAsync(async () => (await cliente.AcaoAsync(segredo, id, acao, versao, Chave($"{id}/{acao}/{versao}"))).Estado,
            () => cliente.EstadoAsync(segredo, id));
        tentativa = null; mensagem.Text = "Ação confirmada e estado sincronizado.";
    }
    private async Task CancelarPartidaAsync()
    {
        if (!await DisplayAlert("Cancelar partida", "A partida será encerrada sem vencedor. O placar e o histórico serão preservados. Deseja continuar?", "Cancelar partida", "Voltar"))
            return;
        await AcaoAsync("cancelar");
        mensagem.Text = "Partida cancelada. Volte à sala para criar outra.";
    }
    private async Task ReiniciarPartidaAsync()
    {
        if (!await DisplayAlert("Reiniciar partida", "A partida atual será encerrada e uma nova começará com os mesmos jogadores e placar zerado. Deseja continuar?", "Reiniciar", "Voltar"))
            return;
        var cliente = ClienteJogador(); var segredo = Token(); var id = PartidaId(); var versao = Versao("reiniciar", id);
        var confirmacao = await cliente.AcaoAsync(segredo, id, "reiniciar", versao,
            Chave($"{id}/reiniciar/{versao}"));
        fluxo.Limpar();
        fluxo.Recuperar(confirmacao.Estado);
        tentativa = null;
        mensagem.Text = "Nova partida iniciada com os mesmos jogadores.";
    }
    private async Task JogarAsync()
    {
        var cliente = ClienteJogador(); var segredo = Token(); var id = PartidaId(); var versao = Versao("jogadas", id);
        if (pedras.SelectedItem is not Pedra pedra) throw new ArgumentException("Selecione uma pedra da sua mão.");
        var ponta = (LadoJogavel)lado.SelectedIndex;
        await fluxo.ExecutarAsync(async () => (await cliente.JogarAsync(segredo, id, pedra, ponta, versao,
            Chave($"{id}/jogadas/{versao}/{pedra}/{ponta}"))).Estado, () => cliente.EstadoAsync(segredo, id));
        tentativa = null; mensagem.Text = "Jogada confirmada e estado sincronizado.";
    }
    private async Task AtualizarAsync()
    {
        var atual = await ClienteJogador().EstadoAsync(Token(), PartidaId());
        fluxo.Recuperar(atual);
        mensagem.Text = atual.Situacao == SituacaoPartida.Encerrada ? "Partida encerrada." : "Estado atualizado.";
    }
    private void Mostrar(EstadoPartida atual)
    {
        estado = atual;
        identificador.Text = atual.PartidaId.ToString();
        if (chavePartida is not null) Preferences.Default.Set(chavePartida, atual.PartidaId.ToString());
        versaoIngresso.Text = atual.VersaoEstado.ToString();
        resumo.Text = $"Código: {atual.PartidaId}\n" +
            (atual.Participantes.Count < 2 ? "Aguardando outro jogador entrar." : "Dois jogadores na mesa. A partida pode começar.");
        tituloPartida.Text = atual.Situacao == SituacaoPartida.Encerrada ? "Fim de partida" : $"Rodada {atual.NumeroRodada}";
        placar.Text = string.Join("     •     ", atual.Participantes.Select(p => $"{p.Nome}  {p.Pontuacao} pts  ·  {p.QuantidadePedrasNaMao} pedras"));
        turno.Text = atual.Situacao == SituacaoPartida.Encerrada ? "Partida encerrada" : atual.ProximaAcao switch
        {
            ProximaAcao.JogarAbertura => "Sua vez: jogue a pedra de abertura",
            ProximaAcao.Jogar => "Sua vez: escolha uma pedra e uma ponta",
            ProximaAcao.Comprar => "Sua vez: compre uma pedra",
            ProximaAcao.Passar => "Sua vez: passe",
            _ => "Aguardando o outro jogador…"
        };
        mesa.Text = atual.Tabuleiro.Count == 0 ? "A primeira pedra abre a mesa." : "";
        pedras.ItemsSource = atual.MinhaMao.ToList();
        if (pedraSelecionada is not null && !atual.MinhaMao.Contains(pedraSelecionada)) pedraSelecionada = null;
        if (atual.PedraAberturaObrigatoria is { } abertura)
        {
            pedras.SelectedItem = atual.MinhaMao.FirstOrDefault(p => p == abertura);
            pedraSelecionada = abertura;
        }
        else pedras.SelectedItem = pedraSelecionada;
        DesenharPedras(atual);
        AtualizarComandos();
        AtualizarPaineis();
        historico.Text = "";
    }
    private void DesenharPedras(EstadoPartida atual)
    {
        pedrasDaMesa.Clear();
        if (atual.Tabuleiro.Count == 0)
            pedrasDaMesa.Add(new Label { Text = "A mesa espera a primeira pedra", TextColor = Cor("#DBE9DF"), FontSize = 18,
                VerticalOptions = LayoutOptions.Center, Margin = new Thickness(12, 38) });
        foreach (var pedra in atual.Tabuleiro)
            pedrasDaMesa.Add(PedraVisual(pedra, false, false));
        pedrasDaMao.Clear();
        foreach (var pedra in atual.MinhaMao)
            pedrasDaMao.Add(PedraVisual(pedra, true, pedra == pedraSelecionada));
        selecao.Text = pedraSelecionada is null ? "Toque em uma pedra da sua mão." : $"Pedra escolhida: {pedraSelecionada}. Escolha a ponta.";
    }
    private View PedraVisual(Pedra pedra, bool naMao, bool selecionada)
    {
        var visual = new Button
        {
            Text = $"{Pontos(pedra.ValorA)}\n────────\n{Pontos(pedra.ValorB)}",
            FontSize = 20, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.NoWrap,
            TextColor = Cor("#17231E"), BackgroundColor = selecionada ? Cor("#F4C56E") : Cor("#F5F1DF"),
            BorderColor = selecionada ? Cor("#E88D40") : Cor("#B8B8A3"), BorderWidth = selecionada ? 3 : 1,
            CornerRadius = 10, WidthRequest = 86, HeightRequest = naMao ? 105 : 108,
            Padding = new Thickness(4), VerticalOptions = LayoutOptions.Center,
            IsEnabled = true
        };
        if (naMao) visual.Clicked += (_, _) =>
        {
            pedraSelecionada = pedra;
            pedras.SelectedItem = pedra;
            if (estado is not null) DesenharPedras(estado);
            AtualizarComandos();
        };
        return visual;
    }
    private static string Pontos(int valor) => valor == 0 ? "○" : string.Concat(Enumerable.Repeat("●", valor));
    private void AtualizarComandos()
    {
        if (ocupado) return;
        var proxima = estado?.ProximaAcao;
        var podeJogar = estado?.Situacao == SituacaoPartida.EmAndamento && proxima is (ProximaAcao.Jogar or ProximaAcao.JogarAbertura);
        var jogar = podeJogar && pedraSelecionada is not null;
        if (botaoEsquerda is not null) { botaoEsquerda.IsVisible = podeJogar; botaoEsquerda.IsEnabled = jogar; }
        if (botaoDireita is not null) { botaoDireita.IsVisible = podeJogar; botaoDireita.IsEnabled = jogar; }
        if (botaoComprar is not null) { botaoComprar.IsVisible = proxima == ProximaAcao.Comprar; botaoComprar.IsEnabled = botaoComprar.IsVisible; }
        if (botaoPassar is not null) { botaoPassar.IsVisible = proxima == ProximaAcao.Passar; botaoPassar.IsEnabled = botaoPassar.IsVisible; }
        if (botaoVoltarSala is not null) botaoVoltarSala.IsVisible = estado?.Situacao == SituacaoPartida.Encerrada;
        if (botaoCancelarPartida is not null) botaoCancelarPartida.IsVisible = estado?.Situacao == SituacaoPartida.EmAndamento;
        if (botaoReiniciarPartida is not null) botaoReiniciarPartida.IsVisible = estado?.Participantes.Count == 2;
    }
    private void LimparEstado()
    {
        fluxo.Limpar();
        estado = null; tentativa = null; pedraSelecionada = null;
        resumo.Text = mesa.Text = historico.Text = ""; pedras.ItemsSource = null;
        pedrasDaMesa.Clear(); pedrasDaMao.Clear();
        AtualizarComandos(); AtualizarPaineis();
    }
    private string EscopoApi() => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(DominoApiClient.ValidarEndereco(endereco.Text ?? "").AbsoluteUri)));
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RecuperarLoginInicialAsync();
        var geracao = ++geracaoConsulta;
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            if (geracao != geracaoConsulta) return false;
            _ = ConsultarAtualizacoesAsync();
            return true;
        });
    }

    protected override void OnDisappearing()
    {
        geracaoConsulta++;
        base.OnDisappearing();
    }

    private async Task ConsultarAtualizacoesAsync()
    {
        if (consultando || ocupado || estado is null) return;
        var ativacaoConsultada = ativacao;
        var partidaConsultada = estado.PartidaId;
        consultando = true;
        try
        {
            if (estado.Situacao == SituacaoPartida.Encerrada)
            {
                if (DateTime.UtcNow < proximaConsultaReinicioUtc) return;
                proximaConsultaReinicioUtc = DateTime.UtcNow.AddSeconds(5);
                try
                {
                    var reinicio = await ClienteJogador().ReinicioAsync(Token(), partidaConsultada);
                    if (ativacao == ativacaoConsultada && estado?.PartidaId == partidaConsultada)
                    {
                        fluxo.Limpar();
                        fluxo.Recuperar(reinicio);
                    }
                }
                catch (ApiException erro) when (erro.Status == 404) { }
            }
            else
            {
                var atual = await ClienteJogador().AtualizacoesAsync(Token(), partidaConsultada, estado.VersaoEstado);
                if (atual is not null && ativacao == ativacaoConsultada && estado?.PartidaId == partidaConsultada)
                    fluxo.Recuperar(atual);
            }
        }
        catch (ApiException erro) when (erro.Status is 401 or 403 or 404) { LimparEstado(); }
        catch (Exception) { mensagem.Text = "Não foi possível atualizar a partida. A próxima consulta será automática."; }
        finally { consultando = false; }
    }
}
