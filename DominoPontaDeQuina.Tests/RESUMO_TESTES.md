# Sumário dos Testes

## Visão geral
A suíte de testes valida o comportamento do Core para partidas entre dois jogadores, fluxo de rodada e regras de regressão da Wave 1.

## Arquivos e cobertura funcional

### `Models/PecaTests.cs`
- Valida regras da entidade `Peca`:
  - `SomaValores`
  - `EhSena`
  - `PossuiValor`
  - `Inverter`
  - `ToString`
- Inclui cenário composto para coerência entre soma e inversão.

### `Models/TabuleiroTests.cs`
- Valida estado e regras do `Tabuleiro`:
  - pontas esquerda/direita
  - soma de pontas externas
  - limpeza do tabuleiro
  - sequência de jogadas com mudança de estado
- Inclui cenários para gaps:
  - `PodeColar`
  - `Colar`
  - `EstaTravado`

### `Models/MaoJogadorTests.cs`
- Valida regras da `MaoJogador`:
  - soma de peças
  - identificação de sena
  - cenário de mão distribuída com consistência de estado
- Inclui cenários de gap:
  - seleção de jogada (`GetJogada`)
  - passar vez
  - desfazer jogada (`DefazerJogada`)
  - transição de estado (jogar e desfazer)

### `Models/PartidaTests.cs`
- Valida comportamento base da `Partida`:
  - mudança de status ao iniciar rodada
  - criação e histórico de rodadas
  - manutenção de estado em múltiplas rodadas
  - persistência de pontuação alvo

### `Models/RodadaGapTests.cs`
- Testa resultados esperados de implementação futura em `Rodada`:
  - finalização por batida com vencedor correto
  - finalização por travamento com vencedor por menor soma
- Esses testes exercitam comportamento de negócio esperado para métodos ainda não implementados.

### `PartidaFluxoTests.cs`
- Valida fluxo de finalização da `Partida`:
  - finaliza ao atingir pontuação alvo
  - não finaliza sem atingir alvo
  - lança exceção ao finalizar em estado inválido
- Critério de exceção: `Assert.Throws<Exception>` com validação de namespace `DominoPontaDeQuina`.

### `PartidaGapTests.cs`
- Testa a pontuação por jogador, alcance da pontuação alvo e obtenção do jogador vencedor.

### `MaoJogadorGapTests.cs`
- Testa resultados esperados para gaps de `MaoJogador`:
  - passar vez quando não há peça compatível
  - restauração de estado ao desfazer jogada

### `TabuleiroGapTests.cs`
- Testa resultados esperados para gaps de `Tabuleiro`:
  - validação de jogada em tabuleiro vazio
  - compatibilidade com pontas
  - colagem no lado correto
  - detecção de travamento/destravamento

### `Wave1RegrasTests.cs`
- Regressão determinística da Wave 1:
  - distribuição de sete pedras para cada jogador e cava de quatorze;
  - abertura pela maior dupla e, sem dupla, pela ordem lexicográfica das faces;
  - jogada válida, pontuação por múltiplos de cinco e rotação do turno;
  - compra de exatamente uma pedra antes da passagem.

### `ApplicationContractsTests.cs`

- Valida contratos de Application para versão esperada, chave de idempotência, conflito de estado e ausência de mão adversária na visão do participante.

### `EstadoPersistentePartidaTests.cs`

- Valida o modelo persistente da Wave 2: limite de participantes individuais, numeração de rodadas, ordem/versão de ações e contrato de recuperação/idempotência.

### `EstadoPartidaRepositoryIntegrationTests.cs`

- Valida, com SQLite descartável em memória, a gravação/leitura do agregado, ordenação de rodadas e ações e filtragem de mãos para participante e observador.

### `RodadaFinalizacaoGapTests.cs`
- Valida finalização de rodada em cenários de:
  - batida
  - tabuleiro travado
- Verifica status final, tipo de finalização e vencedor.

## Diretrizes aplicadas na suíte
- Evitar testes triviais que apenas confirmam dados criados no próprio setup.
- Evitar testes que apenas validem `NotImplementedException` como estado atual.
- Priorizar cenários de comportamento observável e distribuição reproduzível.
- Para exceções de contrato, validar o tipo .NET efetivamente exposto.

## Evidência atual

Em 2026-09-24, a suíte Release aprovou **165 testes**, sem falhas ou ignorados.
Além do Core, cobre identidade, persistência SQLite, API HTTP, perfis/ativação,
outbox, assinatura e segurança de webhook, sincronização de cliente e componentes Blazor.
`AceiteClienteIndependenteTests` exercita HTTP/JSON público sem biblioteca cliente do projeto;
`JornadaWebhookPontaAPontaTests` exercita callbacks HTTPS reais em loopback.
Ambiente, matriz e limitações: [execução T-45](../sdd/features/partida-individual-plataforma/execucao-t45.md).

### Evidência histórica

Em 2026-09-21, `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 93 de 93 testes, incluindo a integração EF com SQLite descartável em memória. `dotnet build DominoPontaDeQuina.slnx --no-restore` também foi concluído sem avisos ou erros.
