# T-45 — Aceite, pipeline e rastreabilidade

Data: 2026-09-24. Ambiente local: Windows, SDK 10.0.401, projetos net8.0.
Esta matriz complementa as [evidências T-40 a T-44](execucao-t40-t44.md).

## Entregas

- README raiz atualizado para a arquitetura e contratos implementados, com links aos guias públicos.
- Especificação alinhada ao acesso por token recebido por e-mail e à liberação explícita já implementados.
- Pipeline deixa de tolerar falhas de testes, compila Migrations e WebhookRelay e publica TRX com `if: always()`.
- `AceiteClienteIndependenteTests` consome rotas e JSON sem usar Client, DTOs de Application ou regras do Core.
  Solicita acesso por e-mail (202 vazio), autentica, cria dois perfis da mesma conta, ativa, lista,
  cria/ingressa, inicia, joga até encerramento, consulta histórico/observação e revoga o token de ambos.

## Matriz de aceite

| Critério | Evidência | Resultado/limite |
| --- | --- | --- |
| AC-ID-01 | IdentidadeAcessoIntegrationTests; solicitação no cliente independente | Adaptador de teste; entrega SMTP real pendente |
| AC-ID-02, AC-ID-04 | AutenticacaoDiretaHttpTests, ApiHttpIntegrationTests | Resposta uniforme e limite de emissão automatizados |
| AC-ID-03 | IdentidadeAcessoIntegrationTests, AceiteClienteIndependenteTests | Reutilização e revogação nos dois clientes |
| AC-ID-05 | Respostas inspecionadas nos testes; callback sem Bearer | Varredura de logs/telemetria de produção pendente |
| AC-AUT-01, AC-AUT-02 | PerfisJogadorHttpTests, EstadoPartidaRepositoryIntegrationTests, PainelTests | Vínculo, ativação e mãos privadas cobertos localmente |
| AC-CLI-01, AC-CLI-02, AC-CLI-06 | PerfisJogadorHttpTests e cliente independente | Dois perfis, listagem, ingresso e última vaga; participação montada pela API |
| AC-CLI-03, AC-CLI-04 | JornadaWebhookPontaAPontaTests, WebhookFlowTests, AberturaObrigatoriaRodadaTests | TLS real em loopback, snapshots privados, abertura e encerramento |
| AC-CLI-05 | FluxoClienteWebhookTests | Antes/depois de HTTP, duplicação, ordem, perda e rejeição |
| AC-API-01, AC-API-02, AC-API-03, AC-API-07 | AceiteClienteIndependenteTests | Consumidor HTTP/JSON puro até resultado; API em TestServer, e-mail simulado |
| AC-API-04, AC-API-05, AC-API-06 | ApiHttpIntegrationTests, JornadaPartidaServiceIntegrationTests, testes Core | Regras, idempotência e rejeições; jornada independente usa pontuação-alvo 1 |
| AC-API-08 | Testes HTTP de concorrência e repositório | Disputas locais SQLite; prioridade temporal entre múltiplos hosts não certificada |
| AC-DOC-01, AC-DOC-02 | Swagger validado por ApiHttpIntegrationTests; guias públicos e cliente independente | Contratos exercitados; não certifica todos os exemplos em ambiente publicado |
| AC-DOC-03 | PainelTests, ClientTests, cliente independente | HTTP público; build MAUI aprovado na T-44, jornada visual pendente |
| AC-WEB-01 | PainelTests | Carregamento, erro, privacidade, versão e encerramento |
| AC-MAUI-01 | Build Windows e testes do transporte T-44 | Aceite visual de duas instâncias pendente |

O terceiro consumidor foi escrito a partir do README da API, com HTTP/JSON e verificação
HMAC própria. A preparação do ambiente cadastra a conta internamente, como responsabilidade
operacional do professor, e aciona o worker de forma determinística. Dois destinos de callback
são simulados em um HttpMessageHandler; nenhum pacote é enviado ao IP usado no exemplo.
Não equivale a uma avaliação por um aluno externo nem a uma execução remota. A evidência
HTTPS real é o teste separado `JornadaWebhookPontaAPontaTests`.

## Verificação reproduzível

```powershell
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --configuration Release --no-restore --logger "trx;LogFileName=tests.trx" --results-directory TestResults
dotnet build DominoPontaDeQuina.Migrations/DominoPontaDeQuina.Migrations.csproj --configuration Release --no-restore
dotnet build DominoPontaDeQuina.WebhookRelay/DominoPontaDeQuina.WebhookRelay.csproj --configuration Release --no-restore
git diff --check
```

Migrations e WebhookRelay compilaram com zero erros e avisos. A recompilação inicial da suíte
emitiu os avisos preexistentes CS8625 e CS9113 do Core. O resultado TRX fica em `TestResults/tests.trx`
(artefato local ignorado pelo Git); a CI publica o mesmo formato.

Resultado final: **165 testes aprovados, zero falhas, zero ignorados**, em Release, duração
de 7 segundos. A primeira execução encontrou uma comparação incorreta de enum no novo
consumidor (PascalCase em vez do camelCase publicado no callback); o consumidor foi
corrigido e a suíte inteira repetida com sucesso. `git diff --check` retornou zero,
somente com avisos de conversão LF/CRLF. Nenhuma execução remota de CI foi alegada.

## Pendências para aceite integral

1. Executar duas janelas MAUI com o relay, conforme README, e registrar o aceite visual T-43.
2. Confirmar SMTP, logs/telemetria, TLS e entrega externa no ambiente de publicação.
3. Executar o workflow no GitHub; nesta tarefa não houve commit/push nem execução remota.
4. Validar concorrência SQL Server e requisitos de prioridade entre hosts no ambiente operacional.

A T-45 entrega documentação, matriz e configuração de pipeline. O aceite integral do produto
permanece pendente dos itens acima; T-31 e T-32 não são marcadas como integralmente aceitas.
