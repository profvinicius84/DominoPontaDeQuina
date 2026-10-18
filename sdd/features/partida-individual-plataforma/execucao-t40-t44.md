# Execução T-40 a T-44

Execução iniciada em 2026-09-23 e consolidada em 2026-09-24, Windows, SDK .NET 10.0.401,
projetos net8.0 e MAUI net8.0-windows10.0.19041.0. Alterações mantidas no workspace.

## Entregas

- T-40: token solicitado somente por e-mail,
  credencial reutilizável até revogação, 17 operações OpenAPI, cabeçalho de ativação
  e callback privado descritos nos endpoints de criação/ingresso.
- T-41: biblioteca HTTP com solicitação de token e liberação; receptor local HMAC com limite de corpo
  e janela de tempo; buffer durante HTTP, descarte de duplicados/versões inferiores,
  recuperação após entrega perdida e invalidação de resposta tardia após saída.
- T-42: painel permanece observador autenticado, com projeção pública e polling;
  respostas antigas não fazem a versão regredir e campos privados não entram na tela.
- T-43: MAUI recebe snapshots, exibe próxima ação e abertura obrigatória, retoma token
  do SecureStorage, libera jogador e utiliza adaptador HTTPS para dois receptores locais.
  Implementação/build concluídos; aceite visual ainda pendente.
- T-44: matriz automatizada executada; encontrou e corrigiu concorrência entre criações
  do mesmo perfil, commit após liberação da ativação e passagem após compra única sem
  encaixe quando ainda havia pedras na cava. Timeout agora inclui resolução de destino;
  filtros de endereços e limites de corpo foram reforçados.

## Matriz e evidências

| Risco/contrato | Evidência automatizada |
| --- | --- |
| Solicitação somente e-mail, token sem expiração e revogação independente | `AutenticacaoDiretaHttpTests`, `IdentidadeAcessoIntegrationTests`, `ApiHttpIntegrationTests` |
| Mesma conta com dois perfis, mãos e callbacks separados | `PerfisJogadorHttpTests`, `JornadaWebhookPontaAPontaTests` |
| Ativação exclusiva, repetição, liberação e nova ativação | `PerfisJogadorHttpTests` |
| Requisição antiga não confirma após liberação | `EstadoPartidaRepositoryIntegrationTests.LiberacaoAposAutorizacao_ImpedeCommitEEventoComChaveAntiga` |
| Última vaga concorrente e duas criações do mesmo perfil | `PerfisJogadorHttpTests` (SQLite descartável) |
| Idempotência, versão, autorização e isolamento de mãos | `ApiHttpIntegrationTests`, `JornadaPartidaServiceIntegrationTests`, `WebhookFlowTests` |
| Abertura obrigatória e compra de uma pedra com passagem | `AberturaObrigatoriaRodadaTests`, `CompraRodadaTests` |
| Outbox, rejeição sem evento, reserva, 500/repetição e confirmação 2xx | `WebhookFlowTests` |
| HMAC adulterado/expirado, POST real aceito/recusado, timeout e destinos internos | `FluxoClienteWebhookTests`, `MatrizWebhookSegurancaTests`, `WebhookFlowTests` |
| Callback antes/depois de HTTP, duplicado/antigo, destinatário errado, perda e saída durante HTTP | `FluxoClienteWebhookTests` |
| Observação pública, erro, encerramento e versão antiga | `PainelTests` |
| Dois clientes chegam ao encerramento com eventos privados assinados via TLS | `JornadaWebhookPontaAPontaTests` |

O teste de jornada usa HTTP da API em TestServer com SQLite, dois `DominoApiClient`
e um receptor Kestrel HTTPS real em loopback. Usa certificado efêmero com pin apenas
no transporte do teste; a validação TLS de produção não foi desabilitada. O worker é
acionado explicitamente após comandos para tornar a entrega reproduzível. O teste
confere ausência de Bearer no callback, mãos distintas, abertura e evento final.
O receptor `HttpListener` usado pelo MAUI também recebeu POST real em teste separado.
Esses testes não abrem janelas MAUI nem atravessam o executável do relay.

## Comandos executados

```powershell
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore
dotnet build DominoPontaDeQuina.Windows.slnx --no-restore
dotnet build DominoPontaDeQuina.slnx --no-restore
git diff --check
```

Resultado final: **164 aprovados, 0 falhas, 0 ignorados**, duração informada de 9 segundos.
Build Windows e build da solução: **0 erros e 0 avisos** nas execuções finais incrementais.
Uma recompilação anterior do Core emitiu os avisos já existentes CS8625 em `IRodada`
e CS9113 em `Jogada`; não foram tratados como parte desta entrega.

## Limites e aceite pendente

- A ferramenta de controle do Windows retornou `Computer Use app approval timed out`
  ao tentar iniciar o MAUI. Nenhuma evidência visual foi obtida. Executar o roteiro de
  duas instâncias no README do MAUI, incluindo campos mascarados, conflito, compra,
  passagem e recuperação com relay interrompido. T-43 não tem aceite integral.
- Relay compilado; sua execução integrada com as duas janelas ainda deve ser validada.
- Entrega SMTP real e callbacks em rede externa não foram exercitados. O ambiente de
  teste usa adaptador de e-mail e loopback expressamente permitido em Development.
- Corridas desta rodada foram exercitadas com SQLite; concorrência em SQL Server e
  aplicação de migrations não foram repetidas. Consultar as evidências anteriores.
- T-45 (aceite final, pipeline e consolidação de rastreabilidade) permanece pendente.

Guias: README da API, README do cliente compartilhado, README do MAUI e README do relay.
