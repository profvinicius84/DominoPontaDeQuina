# Tarefas: partida-individual-plataforma — API completa para partida individual

## Metadados

- Especificação de origem: `sdd/product-spec.md`.
- Estado: plano técnico em waves, com complemento de clientes e webhooks planejado em 2026-09-23.
- Objetivo: entregar uma API .NET 8 completa, documentada e validada para alunos consumirem em clientes próprios. Painel Blazor e cliente MAUI para Windows usam os mesmos contratos públicos.
- Regra de publicação: a API só é disponibilizada à turma após todas as waves aplicáveis e suas evidências serem concluídas.

## Regras de execução

Atualização de escopo em 2026-09-23: [fluxo de clientes e webhooks](fluxo-clientes-webhooks.md) acrescenta AC-CLI-01 a AC-CLI-06. Evidências anteriores não comprovam esses critérios. T-21, T-23 a T-26 e T-30 precisam incorporar os novos contratos; T-32 também depende das adequações abaixo.

### Adequações planejadas — clientes, participação e webhooks

As tarefas T-33 a T-45 substituem o resumo anterior das adequações. A execução avançou até a matriz automatizada da T-44 em 2026-09-24; T-43 mantém pendente o aceite visual Windows e T-45 tem documentação e pipeline atualizados; aceite operacional pendente conforme sua matriz. As evidências estão em [execução T-40 a T-44](execucao-t40-t44.md). A jornada integrada também exigiu corrigir a passagem após compra única sem encaixe, conforme a regra já definida na T-08.

### T-33 — Fechar os contratos de identidade, ativação de jogador e webhook

- Estado: concluída em 2026-09-23; decisões e evidências registradas em [execução T-33 a T-39](execucao-t33-t39.md). A publicação dos endpoints de autenticação direta e solicitação somente por e-mail pertence à T-40.
- Critérios: AC-ID-03, AC-CLI-01, AC-CLI-03, AC-CLI-05 e AC-CLI-06.
- Responsabilidade e arquivos: liderança técnica e segurança; `sdd/product-spec.md`, `sdd/features/partida-individual-plataforma/spec.md`, `fluxo-clientes-webhooks.md`, `DominoPontaDeQuina.Api/README.md` e OpenAPI quando houver contrato aprovado. Não altera produto nesta tarefa.
- Dependências: fluxo confirmado em 2026-09-23.
- Resultado: especificar autenticação direta e a solicitação por e-mail sem contradizer o token reutilizável até revogação. Usuário pode possuir vários jogadores-perfis e escolhe qual ativar em cada cliente. A ativação vincula um jogador a somente um cliente ativo por vez; o ingresso recebe `partidaId`, `jogadorId`, webhook e chave de idempotência quando aplicável, mas nunca participação ou posição. A API valida que o jogador pertence ao usuário autenticado e está ativado para aquele cliente antes de criar a participação.
- Ciclo a decidir e registrar: criação, listagem e seleção de jogadores-perfis da conta; como o servidor representa e autentica internamente a ativação do jogador; ativação concorrente do mesmo jogador; liberação e revinculação explícitas; e quais participações tornam um jogador incompatível com novo ingresso. Desconexão, fechamento do aplicativo e abandono não possuem timeout ou liberação automática definidos; não implementar essa inferência até decisão do responsável.
- Contrato a decidir e registrar: envelope de evento com `eventoId`, `partidaId`, `participacaoId` destinatária, `versaoEstado`, tipo, instante e projeção filtrada; autenticação do callback, confirmação 2xx, timeout, repetição, retenção e consulta de recuperação. É proibido repassar o Bearer para o webhook.
- Validação: revisão de ameaça para token compartilhado, troca indevida de jogador, ativação concorrente, replay de callback, vazamento de mão e SSRF; exemplos de dois jogadores da mesma conta e receptores distintos.
- Evidência: decisão registrada, incluindo as limitações do ambiente acadêmico e o comportamento para cliente desconectado. Não presumir que o token, sozinho, autoriza tomar um jogador já ativo.

### T-34 — Adequar o ciclo de vida do token reutilizável

- Estado: concluída e validada localmente em 2026-09-23 para o ciclo de tokens; os contratos finais de autenticação foram definidos na T-33 e sua publicação continua na T-40.
- Critérios: AC-ID-03 e AC-CLI-01.
- Responsabilidade e arquivos: Application `Identidade/**`; Domain `Entities/TokenAcesso.cs`, `Repositories/IIdentidadeRepository.cs`; Infrastructure `Persistence/Repositories/IdentidadeRepository.cs`, `EntityConfigurations/TokenAcessoConfiguration.cs`; API `Program.cs`, README/OpenAPI; Migrations; `IdentidadeAcessoIntegrationTests.cs`, `ApiHttpIntegrationTests.cs`.
- Dependências: nenhuma para a retirada da expiração; T-33 para o contrato público de autenticação direta.
- Resultado: token recebido por e-mail permanece válido em diversos acessos e clientes até revogação explícita, sem revogar tokens anteriores ao emitir outro e sem campo/regra de expiração automática na validação. A revogação do token atual continua bloqueando todo cliente que o apresente.
- Validação: emitir, reutilizar no mesmo cliente e em dois clientes, simular passagem de tempo, emitir novo token sem invalidar o anterior e revogar cada token; confirmar que respostas, URLs e logs não revelam o segredo.
- Evidência: [execução da T-34](execucao-t34.md). Removidas expiração automática e revogação por nova emissão; suíte completa com 120 testes aprovados, incluindo HTTP em dois clientes, reutilização entre contextos, revogação individual e upgrade SQLite. Migration `20260923131940_TokensReutilizaveisAteRevogacao` aplicada no SQL Server descartável. Não foi alterado banco de uso do usuário nem validado SMTP real.

### T-35 — Modelar jogadores-perfis, ativação e participação separadas do usuário

- Estado: concluída e testada em 2026-09-23. Participação não encerrada torna o jogador incompatível com outra criação/entrada; perfis distintos da mesma conta continuam permitidos.
- Critérios: AC-CLI-01, AC-CLI-03, AC-CLI-06 e AC-AUT-01 a AC-AUT-02.
- Responsabilidade e arquivos: Domain `Entities/Usuario.cs`, `Jogador.cs`, nova entidade de ativação quando aprovada na T-33, `ParticipacaoPartida.cs`, `AcaoPartida.cs`, pedras de mão e repositórios; Application `Partidas/Contracts/**`; Infrastructure configurações, `DominoDbContext.cs` e repositórios; testes de Domain/Application no projeto `DominoPontaDeQuina.Tests`.
- Dependências: T-33.
- Resultado: usuário autentica e pode possuir vários jogadores-perfis; o jogador selecionado identifica o cliente ativo e a participação é criada apenas no servidor. Preservar `JogadorId` como identidade do participante, mão, turno, pontuação e autoria; corrigir a premissa atual de um único jogador por usuário. A ativação é uma associação exclusiva de jogador a cliente, separada da participação.
- Consistência: manter no máximo duas participações por partida e posição única; permitir dois jogadores diferentes da mesma conta na mesma partida; impedir ativação paralela do mesmo jogador e ingresso desse jogador em participação incompatível, segundo a decisão T-33. Preservar os identificadores necessários para históricos sem expor e-mail, token ou mão de adversário.
- Validação: invariantes para dois jogadores da mesma conta em partidas iguais e diferentes; tentativa de usar jogador de outro usuário, jogador não ativado ou já ativo em outro cliente; mão/turno isolados; reidratação do Core com dois `JogadorId` distintos.
- Evidência: testes novos e revisão de modelos/mapeamentos afetados.

### T-36 — Persistir ativação, idempotência de ingresso e estados de entrega

- Estado: concluída em 2026-09-23. Migration de ativação/webhook e migration `FluxoClientesWebhookConfiavel` validadas no SQL Server descartável; outbox, reserva, repetição e retenção estão persistidas.
- Critérios: AC-CLI-01 a AC-CLI-03, AC-CLI-05, AC-CLI-06 e AC-API-08.
- Responsabilidade e arquivos: Domain repositórios de estado/entrega; Infrastructure `DominoDbContext.cs`, `Persistence/EntityConfigurations/**`, `Persistence/Repositories/**`; Migrations em `DominoPontaDeQuina.Migrations/Persistence/Migrations/**` e snapshot; testes SQLite e aplicação de migration em banco descartável.
- Dependências: T-33 e T-35.
- Resultado: migration introduz o estado de ativação exclusiva do jogador, destino de webhook por participação e caixa de saída de eventos. Preservar as chaves atuais por jogador quando suportarem o modelo: participação e autoria de ação usam `JogadorId`; não criar chave paralela para o cliente. Para ingresso, que precede a participação, persistir uma intenção idempotente indexada por partida, jogador validado, operação e chave, com hash do conteúdo para rejeitar reuso divergente.
- Consistência: a verificação da ativação, a reserva da segunda vaga, a participação, a ação aceita, a nova versão e os eventos de saída são confirmados na transação exigida pelo fluxo. A restrição de banco impede terceira participação e duas ativações simultâneas do mesmo jogador; conflito EF volta à leitura autoritativa. A migração não libera jogador por tempo ou desconexão sem decisão da T-33.
- Validação: upgrade de banco descartável a partir da migration atual, ativações concorrentes do mesmo jogador, duas tentativas simultâneas pela última vaga, repetição do ingresso antes/depois da confirmação e recuperação após conflito. Verificar índices, FKs e que a projeção de outro participante não carrega pedra ou segredo de callback.
- Evidência: `dotnet ef database update` em banco descartável identificado e testes de integração no projeto existente.

### T-37 — Orquestrar ativação, criação, listagem e ingresso por jogador

- Estado: concluída e validada por HTTP em 2026-09-23, incluindo liberação explícita, revinculação e bloqueio de participação incompatível.
- Critérios: AC-CLI-01, AC-CLI-02, AC-CLI-03 e AC-CLI-06.
- Responsabilidade e arquivos: Application `Partidas/JornadaPartidaService.cs`, `Partidas/Contracts/ComandosPartida.cs`, `ResultadosPartida.cs`, `IJornadaPartidaService.cs`; Domain/infrastructure somente nos contratos definidos em T-35/T-36; testes de Application e integração.
- Dependências: T-35 e T-36.
- Resultado: implementar o ciclo aprovado de jogadores-perfis e sua ativação exclusiva; criar inclui o ingresso inicial do jogador ativo e seu webhook; listar retorna somente partidas abertas com exatamente uma participação e dados públicos mínimos; ingressar recebe ID da partida, ID do jogador, webhook, versão e chave. A Application valida vínculo usuário-jogador, ativação, disponibilidade e idempotência, cria internamente a participação e atribui posição. Não aceitar payload de participação, posição, mão, turno ou ID de participação preexistente.
- Consistência: revalidar a vaga dentro da transação; repetição idêntica devolve a participação já criada para aquele jogador sem duplicá-la; chave igual com conteúdo diferente falha; ação subsequente resolve e autoriza a participação pelo jogador ativo, não apenas pelo usuário. Liberar ou revincular jogador somente pela política T-33; não inferir abandono.
- Validação: lista exclui partidas vazias, cheias ou encerradas; dois jogadores do mesmo token entram em partidas distintas e na mesma partida; corrida pela vaga; ingresso repetido; jogador de outra conta, não ativado ou ativado em outro cliente recusado.
- Evidência: testes de casos de uso e integração SQLite, sem declarar endpoint HTTP concluído nesta tarefa.

### T-38 — Expor a próxima ação e tornar obrigatória a pedra de abertura

- Estado: concluída e validada em 2026-09-23; pedra obrigatória e próxima ação são persistidas e projetadas sem vazamento ao adversário.
- Critérios: RG-04, AC-CLI-04 e AC-API-04 a AC-API-06.
- Responsabilidade e arquivos: Core `Models/Rodada.cs`, `Models/Partida.cs`, modelos/testes diretamente necessários; Domain `RodadaPartida.cs`; Application projeções e reidratação; Infrastructure mapeamentos/migration se houver campo novo; testes de Core, Application e integração.
- Dependências: T-35 e T-37.
- Resultado: o Core torna observável a pedra exata que determinou o jogador inicial e rejeita, sem mutar, primeira jogada com pedra diferente. A Application persiste e projeta apenas para a participação inicial a instrução `jogar-abertura` com a pedra; para os demais, projeta `aguardar`, `jogar`, `comprar`, `passar` ou encerramento conforme o estado autoritativo. Repetir esse comportamento em cada nova rodada, sem revelar a pedra ou a mão privada ao adversário.
- Validação: maior dupla, ausência de dupla, tentativa da pedra inicial errada, início correto, nova rodada e privacidade da instrução. Confirmar que a regra não joga automaticamente a pedra.
- Evidência: testes de regra existentes ampliados e testes de projeção; nenhuma duplicação de regra no cliente.

### T-39 — Implementar entrega confiável de eventos por webhook

- Estado: concluída e validada em 2026-09-23; outbox transacional, assinatura HMAC, worker com reserva, timeout, backoff, retenção, bloqueio SSRF e entrega ao menos uma vez.
- Critérios: AC-CLI-03, AC-CLI-04 e AC-CLI-05.
- Responsabilidade e arquivos: Application novo contrato/publicador de eventos; Infrastructure adaptador HTTP, repositório/outbox e worker hospedado; API composição/telemetria segura; Domain somente entidades de entrega definidas na T-36; testes de integração com receptor controlado.
- Dependências: T-33, T-36 a T-38.
- Resultado: após cada transição persistida, publicar um evento por participação com a projeção filtrada e versão correspondente. A confirmação HTTP só confirma o comando; o worker entrega a caixa de saída depois do commit, com timeout limitado, tentativas agendadas, backoff, registro de última falha sem segredo e retenção definida em T-33. Falha de callback não desfaz jogada, compra, passagem ou início aceitos.
- Segurança: aceitar apenas destinos permitidos pela política definida em T-33, revalidar resolução DNS, bloquear faixas locais/privadas e redirecionamentos, limitar método, tamanho e tempo da requisição e assinar o envelope pelo mecanismo definido. Não enviar `Authorization`, e-mail, token, mão alheia ou stack trace. A entrega é ao menos uma vez; o receptor deve deduplicar por `eventoId` e ordenar por `versaoEstado`.
- Validação: receptor recebe estado inicial, turno, cava, abertura e encerramento; 2xx confirma; timeout/5xx agenda repetição; callback malicioso é recusado; duplicação e ordem fora de ordem são possíveis sem regredir o cliente; indisponibilidade não reverte a transação.
- Evidência: testes com servidor local controlado, registros sem segredos e política de entrega documentada.

### T-40 — Publicar os endpoints e o contrato OpenAPI do fluxo

- Estado: concluída em 2026-09-24; 17 operações, solicitação somente por e-mail e callback OpenAPI verificados por testes HTTP. Evidências em `execucao-t40-t44.md`.
- Critérios: AC-ID-03, AC-CLI-01 a AC-CLI-06, AC-DOC-01 e AC-DOC-02.
- Responsabilidade e arquivos: API `Program.cs`, modelos HTTP, configuração de autenticação, OpenAPI/Swagger e `DominoPontaDeQuina.Api/README.md`; Application contratos de T-37/T-38; testes HTTP no projeto existente.
- Dependências: T-34, T-37 a T-39.
- Resultado: expor autenticação direta conforme decisão T-33, solicitação/revogação de token reutilizável, ciclo de jogadores-perfis, ativação de jogador, listagem de partidas abertas e criação/ingresso com os três dados obrigatórios. Documentar que o servidor cria a participação e que respostas HTTP não substituem evento de estado. Estados próprios e comandos identificam o jogador ativo autorizado sem permitir escolher posição ou participação alheia.
- Contratos mínimos: documentar criação/listagem/seleção de jogadores-perfis, ativação, estado ativo, liberação/revinculação conforme decisão, corpo e exemplos de criação/ingresso com `jogadorId` e URL de webhook, versão, UUID de idempotência, erros de vínculo/ativação/vaga/conflito, envelope do callback, assinatura, semântica ao menos uma vez, recuperação por consulta e ausência de evento após rejeição sem mutação.
- Validação: testes HTTP com dois jogadores que apresentam o mesmo token, leitores webhook separados, ativação exclusiva, lista, corrida, criação, ingresso, resposta rejeitada, autorização e privacidade. OpenAPI deve ser validado pelos exemplos, sem acessar banco interno.
- Evidência: resultados dos testes HTTP e exemplos reproduzidos em ambiente de desenvolvimento.

### T-41 — Adaptar o cliente HTTP compartilhado e a documentação de consumo

- Estado: concluída em 2026-09-24; receptor autenticado, correlação HTTP/callback, recuperação e documentação de consumo implementados e testados.
- Critérios: AC-CLI-05, AC-DOC-01 a AC-DOC-03 e AC-MAUI-01.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Client/**`, README da API e exemplos independentes; testes `ClientTests.cs` e de contrato.
- Dependências: T-40.
- Resultado: o cliente de referência envia uma intenção e aguarda sua resposta HTTP sem desativar o receptor. Armazena evento antecipado, descarta duplicado/versão inferior, aplica somente versão confirmada ou posterior e consulta estado quando houver lacuna ou entrega perdida. Para erro sem mutação, mostra a resposta e não aguarda callback inexistente.
- Validação: matriz de resposta antes/depois do evento, duplicado, versão fora de ordem, falha de callback, conflito, chave de idempotência repetida e revogação. Não persistir token ou segredo de webhook em logs/URL.
- Evidência: testes de serialização/ordenação e guia com exemplo de receptor autenticado.

### T-42 — Adequar o painel Blazor ao contrato público

- Estado: concluída em 2026-09-24; projeção pública, polling sem regressão de versão e privacidade validados por testes de componente.
- Critérios: AC-WEB-01, AC-AUT-02 e AC-DOC-03.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Blazor/**`, `DominoPontaDeQuina.Client/**`, `PainelTests.cs` e README do painel.
- Dependências: T-40 e T-41.
- Resultado: manter o painel como observador autenticado, consumindo somente projeção pública. Ajustar DTOs/erros que mudarem no contrato sem registrar webhook de jogador nem exibir mãos, token, assinatura ou segredos. A estratégia atual de polling só pode ser mantida se continuar compatível com as consultas de recuperação documentadas.
- Validação: carregamento, atualização, erro, encerramento e ausência de dados privados após a mudança de contrato.
- Evidência: testes de componente e procedimento de execução atualizado.

### T-43 — Adaptar o MAUI Windows ao ciclo HTTP e webhook

- Estado: implementação concluída e build Windows aprovado em 2026-09-24; receptor, relay HTTPS local, ciclo de eventos, retomada de token e liberação implementados. Aceite visual de duas instâncias pendente: ferramenta de controle do Windows expirou aguardando autorização. Não confundir o teste HTTPS de dois clientes com validação da interface MAUI.
- Critérios: AC-MAUI-01 e AC-CLI-01 a AC-CLI-05.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Maui/**`, `DominoPontaDeQuina.Client/**`, testes existentes e README do MAUI.
- Dependências: T-40 e T-41.
- Resultado: reutilizar token até revogação, permitir executar o ciclo de jogadores-perfis aprovado na T-33, escolher e ativar um perfil da conta, listar/criar/ingressar, e apresentar o estado recebido pelo callback sem executar regra local. Se o app não puder hospedar endereço alcançável no ambiente acadêmico, documentar e implementar o adaptador/receptor de desenvolvimento aprovado na T-33 antes de afirmar suporte ao webhook.
- Validação: duas instâncias MAUI com mesmo token e jogadores-perfis distintos em posições distintas, abertura, turno, compra, passagem, rejeição e recuperação após evento perdido. Confirmar que os segredos não aparecem na tela, preferências, logs ou URL.
- Evidência: execução Windows reproduzível e testes de estados pertinentes.

### T-44 — Executar a matriz de segurança, concorrência e contratos

- Estado: matriz automatizada concluída em 2026-09-24, 164 testes aprovados sem falhas/ignorados, incluindo jornada com callback HTTPS real até encerramento. Aceite operacional permanece condicionado à validação visual da T-43; SMTP, rede externa e concorrência SQL Server não foram exercitados nesta rodada. Evidências e limites em `execucao-t40-t44.md`.
- Critérios: AC-ID-03, AC-AUT-01 a AC-AUT-02, AC-CLI-01 a AC-CLI-06 e AC-API-08.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Tests/**`, evidência em `sdd/features/partida-individual-plataforma/`; sem criar projetos de teste separados por obrigação.
- Dependências: T-34 a T-43.
- Resultado: casos unitários, integração, HTTP e ponta a ponta cobrem os riscos introduzidos pelo fluxo. Registrar o que não puder ser validado automaticamente, especialmente entrega em rede externa e políticas de URL.
- Validação: token sem expiração e revogação; associação usuário-jogador, ativação exclusiva, liberação/revinculação aprovada e ausência de timeout implícito; duas participações de jogadores da mesma conta; ingresso e última vaga concorrentes; idempotência antes/depois do ingresso; privacidade; abertura/cava; outbox/2xx/timeout/repetição; assinatura/SSRF; HTTP+webhook em todas as ordens.
- Evidência: comandos, ambiente, resultados e limitações reais. Não substituir essa matriz por build verde.

### T-45 — Atualizar aceite, pipeline e rastreabilidade

- Estado: documentação, matriz e configuração de CI entregues em 2026-09-24. Consumidor HTTP/JSON independente adicionado. Aceite integral condicionado à T-43 e ao ambiente operacional; ver [execução T-45](execucao-t45.md).
- Critérios: AC-ID-03, AC-CLI-01 a AC-CLI-06, AC-DOC-01 a AC-DOC-03, AC-WEB-01 e AC-MAUI-01.
- Responsabilidade e arquivos: `README.md`, READMEs de API/Blazor/MAUI, `sdd/product-spec.md`, este plano, `.github/workflows/validate-tests.yml` quando a política de CI estiver no escopo, e evidências de aceite.
- Dependências: T-44 e tarefas anteriores T-31/T-32.
- Resultado: documentação só descreve contratos comprovados; a matriz de aceite liga cada cenário aos testes/evidências; pipeline executa as verificações aprovadas sem ocultar falhas obrigatórias.
- Validação: terceiro cliente usando apenas documentação pública, dois receptores e dois jogadores-perfis da mesma credencial; reproduzir a jornada até encerramento.
- Evidência: matriz preenchida com ambiente e resultados, sem marcar como concluído o que não foi executado.

### Regras gerais

- Cada task deve produzir um resultado pequeno, verificável e rastreável aos critérios da especificação.
- As waves são sequenciais; tasks de uma mesma wave só podem ser paralelizadas quando não alterarem os mesmos contratos, modelos ou migrations.
- Alunos não implementam gaps de domínio. A equipe do projeto conclui o motor, a API e a documentação antes da publicação.
- O início sem dupla está definido: começa quem possuir a pedra de maior valor, comparada pela maior face e depois pela menor face, em ordem decrescente.
- A prioridade entre ações concorrentes é o timestamp de recebimento no servidor. T-02 define somente a forma pública do conflito e a versão do estado.
- Não escolher JWT, SMTP, SignalR, fornecedor de e-mail ou mecanismo de atualização sem decisão técnica registrada.

## Wave 0 — Decisões e baseline executável

### T-01 — Definir início sem dupla

- Estado: concluída em especificação; implementação na Wave 1.
- Critérios: RG-04; AC-API-06.
- Responsabilidade e arquivos: responsável do produto; `sdd/product-spec.md`, `README.md`, `DominoPontaDeQuina.Core/README.md` e contextos SDD afetados.
- Dependências: nenhuma.
- Resultado: critério determinístico para iniciar a rodada quando nenhuma mão tiver dupla, aplicado pelo servidor sem revelar a mão adversária.
- Validação: cenários documentados de distribuição com e sem dupla, revisados pelo responsável.
- Evidência: em 2026-09-21, o responsável confirmou que partidas são exclusivamente entre dois jogadores solo e, sem dupla nas duas mãos, inicia quem possuir a pedra de maior valor. Para manter a regra determinística, o servidor compara a face mais alta e, em seguida, a outra face, em ordem decrescente. A implementação pertence às tarefas T-05/T-06 da Wave 1.

### T-02 — Definir contrato de conflito e versão do estado

- Estado: concluída em especificação; implementação pendente nas Waves 2 e 3.
- Critérios: AC-API-04, AC-API-05, AC-API-08, AC-DOC-01 e AC-DOC-02.
- Responsabilidade e arquivos: liderança técnica; `sdd/features/partida-individual-plataforma/spec.md`, `sdd/product-spec.md` e documentação de contratos quando criada.
- Dependências: nenhuma.
- Resultado: resposta pública estável para conflito, estratégia de versão/idempotência e procedimento pelo qual o cliente recupera o estado autoritativo. A ação recebida primeiro pelo servidor prevalece; posteriores não mudam estado, turno ou pontuação.
- Validação: sequência esperada para ação repetida, concorrente e desatualizada.
- Evidência: contrato técnico registrado em `spec.md`, com versão monotônica por partida, chave UUID de idempotência por autor/partida, timestamp e ordem de recebimento gerados pelo servidor e conflito semântico `CONFLITO_DE_ESTADO`. Implementação e validação transacional/HTTP ficam para Waves 2 e 3; não há validação executada nesta task.

### T-03 — Corrigir a factory de migrations

- Estado: concluída com limitação de ambiente.
- Critérios: suporte técnico às operações persistentes da API.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Migrations/DominoDbContextFactory.cs`, `Persistence/DominoDbContextFactory.cs`, referências de projeto e documentação correspondente.
- Dependências: nenhuma.
- Resultado: uma única factory de design-time válida, sem dependência implícita de `DominoPontaDeQuina.Repository`, e origem previsível para `appsettings.json`.
- Validação: listagem de migrations sem aplicar banco compartilhado.
- Evidência: em Windows, com SDK 10.0.401 e projetos net8.0, foi removida a factory duplicada que referenciava `DominoPontaDeQuina.Repository`; permaneceu apenas `Persistence/DominoDbContextFactory.cs`, com `appsettings.json` resolvido no diretório de saída. `dotnet ef migrations list --project DominoPontaDeQuina.Migrations --startup-project DominoPontaDeQuina.Migrations --context DominoPontaDeQuina.Infrastructure.Persistence.DominoDbContext --no-build` listou `20260901060409_InitialCreate`, sem aplicar alterações. A consulta ao LocalDB não pôde determinar o status aplicado porque a instância não está disponível; a ferramenta EF instalada é 8.0.8 e o runtime é 8.0.19.

### T-04 — Tornar a solução e pipeline verificáveis

- Estado: concluída no escopo local autorizado; CI/CD excluído por decisão do responsável.
- Critérios: suporte para todas as entregas.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Migrations/Program.cs`, `.csproj` afetados, `DominoPontaDeQuina.slnx` e documentação técnica. O workflow CI/CD, inclusive `continue-on-error`, está explicitamente fora deste escopo.
- Dependências: T-03.
- Resultado: restore e build da solução executáveis localmente. Não houve alteração nem avaliação de CI/CD por decisão do responsável.
- Validação: `dotnet restore DominoPontaDeQuina.slnx`, `dotnet build DominoPontaDeQuina.slnx --no-restore` e execução do projeto de testes.
- Evidência: `dotnet restore DominoPontaDeQuina.slnx` e `dotnet build DominoPontaDeQuina.slnx --no-restore` concluíram com êxito, sem avisos ou erros, em Windows com SDK 10.0.401 e projetos net8.0. `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-build --no-restore` executou 78 testes: 44 aprovados e 34 falhos por gaps preexistentes do Core (Partida, Rodada, Tabuleiro e MaoJogador); testes não foram adequados por decisão do responsável.

## Wave 1 — Motor de regras individual

### T-05 — Substituir a representação de times por dois jogadores

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-01, RG-02 e RG-10.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Partida.cs`, `Models/Time.cs` quando aplicável e testes diretamente afetados.
- Dependências: T-01 e T-04.
- Resultado: representação de partida, participantes e pontuação acumulada compatível com dois jogadores, sem semântica residual de times.
- Validação: testes de criação de partida, participantes e encerramento por pontuação-alvo.
- Evidência: `Partida` passou a ter `Jogadores`, limite de dois participantes, pontuação individual e vencedor individual. `Times` e métodos associados foram mantidos somente como compatibilidade temporária com a interface e a suíte histórica, sem uso pelo fluxo novo; sua remoção ou adequação será tratada na consolidação da T-12. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-06 — Distribuir pedras, manter cava e determinar abertura

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-03 e RG-04.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Rodada.cs`, `Models/MaoJogador.cs` e testes de rodada.
- Dependências: T-05.
- Resultado: 28 pedras distribuídas em duas mãos de sete e cava de quatorze; abertura definida pela maior dupla ou pelo critério de T-01, sem exposição de mãos entre jogadores.
- Validação: cenários determinísticos de distribuição, conservação das 28 pedras e início com/sem dupla.
- Evidência: `Rodada` cria o conjunto duplo-seis de 28 peças, distribui sete para cada jogador e conserva quatorze na cava. A abertura usa maior dupla e, sem dupla, as faces em ordem lexicográfica descendente. O construtor aceita conjunto completo para distribuição reproduzível futura. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-07 — Validar e aplicar a jogada no tabuleiro

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-05 e RG-06.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Tabuleiro.cs`, `Models/Jogada.cs`, validadores/serviços de Core permitidos e testes.
- Dependências: T-06.
- Resultado: uma peça da mão é aceita somente no lado compatível, com orientação correta e sem alteração parcial para ação inválida.
- Validação: encaixes válidos, lados inválidos, peça ausente e reversão de tentativas inválidas.
- Evidência: `Tabuleiro` valida ponta e orienta a peça antes de alterar a coleção; `MaoJogador` reserva a peça até a confirmação; `Rodada` valida jogador, mão e lado antes de remover e colar. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-08 — Executar compra única, passagem e rotação do turno

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-05 e RG-08.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Rodada.cs`, `Models/MaoJogador.cs`, `Models/Tabuleiro.cs` e testes.
- Dependências: T-06 e T-07.
- Resultado: jogador sem jogada válida compra uma única peça da cava; se continuar sem opção, ou se a cava estiver vazia, passa a vez e o turno avança corretamente.
- Validação: compra com jogada possível, compra sem jogada possível, cava vazia e sequência de turnos.
- Evidência: `Rodada.ObterJogadaAtual` compra no máximo uma peça e retorna passagem somente quando não houver encaixe após a compra ou não existir cava; a rodada rotaciona a fila após ação aceita. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-09 — Calcular pontuação das pontas após jogada

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-07.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Rodada.cs`, `Models/Partida.cs` e testes de pontuação.
- Dependências: T-07.
- Resultado: soma das pontas externas múltipla de cinco concede `soma / 5` ao jogador que realizou a jogada; os demais valores não pontuam.
- Validação: valores múltiplos e não múltiplos de cinco, incluindo mudanças de ponta.
- Evidência: após uma jogada aceita, a soma das pontas externas múltipla de cinco soma `soma / 5` ao jogador autor. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-10 — Encerrar por mão vazia e por travamento

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-09.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Rodada.cs`, `Models/Partida.cs` e testes de término.
- Dependências: T-08 e T-09.
- Resultado: mão vazia encerra a rodada; travamento encerra a rodada e transfere ao adversário apenas a soma múltipla de cinco da mão do jogador.
- Validação: mão vazia, travamento com soma múltipla e não múltipla de cinco, e imutabilidade após encerramento.
- Evidência: mão vazia finaliza por batida; sem cava e sem encaixe, finaliza por travamento e cada soma de mão múltipla de cinco é transferida ao adversário. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-11 — Iniciar rodada seguinte e encerrar partida

- Estado: implementada; validação de regressão pendente na T-12.
- Critérios: RG-04, RG-09 e RG-10.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Core/Models/Partida.cs`, `Models/Rodada.cs` e testes de fluxo.
- Dependências: T-10.
- Resultado: nova rodada redistribui as pedras e aplica novamente o critério de abertura; partida encerra ao alcançar a pontuação-alvo.
- Validação: sequência de múltiplas rodadas, abertura renovada e término de partida.
- Evidência: `Partida.IniciarNovaRodada` exige os dois participantes, cria e inicia uma nova rodada; a pontuação individual encerra a partida ao atingir o alvo. Build da solução executado sem erros; testes não executados por decisão do responsável.

### T-12 — Consolidar a suíte de regressão do Core

- Estado: concluída.
- Critérios: RG-01 a RG-10 e AC-API-04 a AC-API-06.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Tests/**`, documentação de testes afetada.
- Dependências: T-05 a T-11.
- Resultado: cobertura determinística para todas as regras confirmadas, sem testes orientados a times e sem ocultar falhas preexistentes.
- Validação: `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj`.
- Evidência: em Windows, com SDK 10.0.401 e projetos net8.0, `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 83 de 83 testes, sem falhas ou ignorados. A suíte foi adequada de times para dois jogadores e passou a cobrir distribuição 7+7+cava 14, maior dupla, abertura lexicográfica sem dupla, jogada válida, pontuação, turno e compra única. Permanecem dois avisos de compilação preexistentes: nulabilidade em `IRodada` e parâmetro `valorColado` não utilizado em `Jogada`.

## Wave 2 — Casos de uso e persistência

### T-13 — Definir contratos de aplicação para a jornada jogável

- Estado: concluída em contratos; orquestração pendente na T-16.
- Critérios: AC-API-02 a AC-API-07.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Application/Partidas/Contracts/**`, contratos/DTOs necessários, `IPartidaService.cs` e testes de contrato no projeto existente.
- Dependências: T-02 e T-12.
- Resultado: intenções e resultados para criar, ingressar, iniciar, consultar, jogar, passar, obter resultado e histórico, sem tipos HTTP ou de interface.
- Validação: testes de aplicação para validação de entrada e contratos de resultado.
- Evidência: contratos puros de Application definem comandos para criar, ingressar, iniciar, consultar estado próprio, jogar, passar e consultar resultado/histórico; as visões expõem somente `MinhaMao`, tabuleiro, participantes resumidos e versão. Conflitos usam `ConflitoDeEstado`, `VersaoEsperada`, `ChaveIdempotencia` e estado autoritativo filtrado, conforme T-02. Em Windows, com SDK 10.0.401 e projetos net8.0, `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 86 de 86 testes, incluindo três cenários de contratos. A orquestração, a autorização e a persistência desses contratos ficam nas T-14 a T-17.

### T-14 — Modelar estado persistente de rodada e ação

- Estado: concluída no Domain; mapeamento EF e repositórios concretos pendentes na T-15.
- Critérios: AC-API-03, AC-API-04 e AC-API-07.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Domain/Entities/**`, `Repositories/**` e testes de modelo.
- Dependências: T-13.
- Resultado: entidades e contratos suficientes para recuperar partida, participantes, rodadas, ações ordenadas, pontuação e resultado, sem duplicar a lógica do Core.
- Validação: testes de invariantes e contratos de repositório.
- Evidência: `Partida` agora representa versão, vencedor, encerramento, participantes, rodadas e ações recebidas. `RodadaPartida` preserva número, turno, cava, pontas, término e ações; `AcaoPartida` guarda autor, chave de idempotência, versões, recebimento UTC/ordem, resultado e dados de jogada, sem validar regra do motor. `PedraMaoPartida` permite recuperar a mão privada exclusivamente para o participante autorizado. `IEstadoPartidaRepository` define leitura do agregado e busca por chave/autor para idempotência; sua implementação e os mapeamentos permanecem na T-15. Em Windows, com SDK 10.0.401 e projetos net8.0, `dotnet build DominoPontaDeQuina.slnx --no-restore` concluiu sem avisos ou erros e `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 91 de 91 testes. Os cinco testes de modelo cobrem participantes, rodadas, ações e contrato de repositório. CI/CD não foi alterado.

### T-15 — Mapear o estado e repositórios EF Core

- Estado: concluída; migration do novo modelo permanece na T-18.
- Critérios: AC-API-03, AC-API-04, AC-API-07 e AC-AUT-02.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Infrastructure/Persistence/**`.
- Dependências: T-14.
- Resultado: mapeamentos Fluent API, repositórios e consultas que preservam ordenação e separam visão de participante da visão de observador.
- Validação: testes de integração de gravação, leitura, ordenação e filtragem de mãos privadas.
- Evidência: `DominoDbContext` agora expõe rodadas, ações e pedras de mão. Os mapeamentos Fluent configuram chave composta e posição única de participação, rodadas únicas por partida/número, versão como token de concorrência, e chaves únicas de idempotência `(PartidaId, AutorJogadorId, ChaveIdempotencia)` e ordem `(PartidaId, OrdemRecebimento)`. `EstadoPartidaRepository` recupera o agregado ordenado por rodada/recebimento, carrega a mão apenas da participação solicitada e deixa as mãos vazias na visão de observador; autorização e transação não foram implementadas. Em Windows, com SDK 10.0.401 e projetos net8.0, `dotnet restore DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --ignore-failed-sources` concluiu usando o cache local, e `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 93 de 93 testes. Dois testes de integração usaram SQLite descartável em memória, com `EnsureCreated`, para gravar/ler participantes, mãos, rodadas e ações, verificar ordem e confirmar que a consulta de participante não carrega a mão adversária e a de observador não carrega nenhuma mão. CI/CD não foi alterado. Não foi gerada migration; isso permanece na T-18.

### T-16 — Orquestrar ação autorizada e transacional

- Estado: concluída; a política de concorrência por versão/timestamp permanece na T-17.
- Critérios: AC-AUT-01, AC-AUT-02 e AC-API-04 a AC-API-07.
- Responsabilidade e arquivos: Application, Domain e Infrastructure afetados por T-13 a T-15.
- Dependências: T-02, T-12 e T-15.
- Resultado: aplicação valida participante, turno e estado; executa o motor uma vez; persiste ação e novo estado sem mutação parcial; devolve visão permitida ao ator.
- Validação: sucesso, não participante, turno inválido, peça/lado inválidos, passagem indevida e consulta de estado.
- Evidência: a `JornadaPartidaService` implementa criar, ingressar, iniciar, consultar estado próprio, jogar, passar e consultar histórico usando o motor do Core, sem mover regras para clientes. A recuperação preserva mãos privadas, ordem da cava, ações do tabuleiro, turno e pontuação; a escrita usa uma confirmação EF única por transição aceita. A visão do participante contém somente a própria mão e a quantidade persistida da mão adversária. Em 2026-09-21, `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 95 de 95 testes, incluindo dois cenários SQLite para jornada, autorização, turno, passagem indevida, jogada e recuperação. Não houve alteração de migration, pipeline ou implementação da T-17.

### T-17 — Aplicar concorrência por timestamp e versão

- Estado: concluída na Application/Infrastructure; a migration necessária ao banco de desenvolvimento permanece na T-18.
- Critérios: AC-API-08.
- Responsabilidade e arquivos: Application, Infrastructure, repositórios e entidades definidos em T-02.
- Dependências: T-02 e T-16.
- Resultado: o servidor registra timestamp de recebimento e aplica somente a ação mais antiga para o mesmo estado; repetidas ou posteriores recebem conflito sem nova pontuação ou turno.
- Validação: ações concorrentes, repetidas e desatualizadas com ordem controlada.
- Evidência: antes de invocar o Core, a jornada compara a versão recebida com a versão autoritativa e consulta a chave `(partida, autor, UUID)`. Uma repetição idêntica devolve a confirmação original com `AcaoIdempotente = true`; reuso da chave para tipo, versão, pedra ou lado divergente devolve `EntradaInvalida`. Ações posteriores recebem `ConflitoDeEstado` com a visão autorizada atual e não alcançam o motor. A confirmação EF captura conflitos do token de versão, descarta o grafo local e devolve a leitura autoritativa; ações aceitas preservam `RecebidaEmUtc` e ordem monotônica do servidor. Em 2026-09-21, `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou a suíte, incluindo SQLite para ação repetida, chave divergente e duas ações com a mesma versão, das quais somente a primeira atualiza tabuleiro/versão. Pipeline e migrations não foram alterados.

### T-18 — Gerar migration e validar atualização do banco

- Estado: migration gerada e validada estruturalmente; aplicação em banco descartável permanece pendente por limitação de autenticação do ambiente.
- Critérios: suporte à API completa.
- Responsabilidade e arquivos: `DominoPontaDeQuina.Migrations/**`, snapshot e README de migrations.
- Dependências: T-15 a T-17.
- Resultado: migration nomeada para o modelo aprovado e aplicação reprodutível em banco de desenvolvimento identificado.
- Validação: criar/listar/aplicar migration em banco descartável; não usar banco compartilhado.
- Evidência: foram criados `20260921201546_EstadoPartidaRecuperavel`, seu designer e o snapshot atualizado. A migration inclui versão da partida, posição/contagem de mão, rodadas, ações idempotentes/ordenadas, pedras de mão e cava; antes do índice único de posição, numera participações existentes por `ROW_NUMBER` para não colidir em bases históricas. Em 2026-09-21, `dotnet ef migrations list ... --no-build --no-connect` listou a migration inicial e a nova; `dotnet ef migrations script 20260901060409_InitialCreate 20260921201546_EstadoPartidaRecuperavel --idempotent ... --no-build` gerou o SQL esperado. O alvo descartável tentado foi `localhost`, banco `DominoPontaDeQuina_T18_20260921`, com autenticação integrada: LocalDB não iniciou e a instância SQL Server local falhou com `Failed to generate SSPI context`; portanto, `database update` não foi confirmado e nenhum banco foi criado ou alterado. A ferramenta EF instalada é 8.0.8, enquanto o runtime é 8.0.19. CI/CD não foi alterado.

## Wave 3 — Identidade, API e documentação pública

### T-19 — Preparar cadastro de usuários FIAP

- Estado: concluída.
- Critérios: AC-ID-01 e AC-ID-02.
- Responsabilidade e arquivos: Domain, Application, Infrastructure, migrations e documentação operacional estritamente necessários.
- Dependências: T-18.
- Resultado: cadastro automático de e-mail FIAP ausente durante a solicitação pública de token, com normalização e unicidade.
- Validação: e-mail elegível e não elegível em persistência controlada; unicidade e normalização aprovadas.
- Evidência: `Usuario.NormalizarEmailFiap` normaliza o e-mail e restringe a elegibilidade ao domínio `@fiap.com.br`; `IdentidadeAcessoService` registra somente usuários elegíveis e preserva unicidade no índice de e-mail. A política acadêmica de senha fixa é interna ao serviço e não é exposta na documentação pública. `IdentidadeAcessoIntegrationTests` valida normalização, domínio não elegível e autenticação. A suíte concluiu com 97 testes aprovados.

### T-20 — Implementar emissão e entrega de token por e-mail

- Estado: concluída com adaptador de entrega a configurar.
- Critérios: AC-ID-01, AC-ID-02, AC-ID-04 e AC-ID-05.
- Responsabilidade e arquivos: Application, Infrastructure e adaptadores de e-mail/identidade; contratos internos e testes.
- Dependências: T-19.
- Resultado: token revogável emitido para e-mail FIAP elegível, com cadastro automático se ausente; entrega exclusivamente por e-mail; confirmação pública uniforme; nenhuma exposição em URL, logs, telemetria ou resposta. A duração e o reuso são definidos e validados na T-21/T-34.
- Validação: e-mail cadastrado/não cadastrado, entrega simulada, limitação de abuso e varredura de logs.
- Evidência: `IdentidadeAcessoService` gera token criptograficamente aleatório, persiste somente seu hash e encaminha o valor original ao contrato `IEntregaTokenEmail`; respostas não contêm token. `EntregaTokenEmailNaoConfigurada` é um adaptador explícito para desenvolvimento e não representa entrega real; a configuração de SMTP/provedor pertence à operação futura. Testes usam adaptador capturado, sem registrar token em logs ou URLs.

### T-21 — Implementar validação, reutilização e revogação de token

- Estado: reaberta; a adequação executável é T-34.
- Critérios: AC-ID-03 e AC-ID-05.
- Responsabilidade e arquivos: Application, Infrastructure e configuração de identidade.
- Dependências: T-20.
- Resultado: token válido é reutilizável em diversos acessos e clientes até revogação, sem expiração automática. Token ausente, inválido ou revogado não acessa operações protegidas; revogação é operacionalmente possível.
- Validação: acessos repetidos com o mesmo token, reabertura do aplicativo, dois clientes simultâneos, passagem do tempo sem expiração e revogação bloqueando ambos os clientes.
- Atualização em 2026-09-23: adequação ao AC-ID-03 revisado concluída na T-34; consultar [evidência atual](execucao-t34.md). O relato anterior abaixo permanece como histórico e não representa mais a política vigente.
- Evidência: tokens expiram em 15 minutos, são validados por hash e podem ser revogados pelo titular. Os testes de integração cobrem emissão, validação, revogação, expiração persistida e tentativa de reuso; `dotnet build DominoPontaDeQuina.slnx --no-restore` concluiu com 0 avisos/erros e `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` aprovou 97 de 97 testes.

### T-22 — Criar host e endpoints de identidade

- Atualização em 2026-09-24: o adaptador SMTP foi substituído pelo SendGrid Mail Send, configurado por `Email__SendGrid__*`. Testes com transporte HTTP simulado validam a requisição e a rejeição sem expor o token. A entrega externa ainda depende de validação operacional com chave e remetente verificado.
- Estado: implementada e validada localmente; entrega SendGrid real em ambiente operacional permanece pendente.
- Critérios: AC-ID-01 a AC-ID-05 e AC-DOC-01.
- Responsabilidade e arquivos: novo projeto API .NET 8, incluído na solução; composição, endpoint público de solicitação e tratamento de erro.
- Dependências: T-20 e T-21.
- Resultado: operação pública de solicitação de token e infraestrutura de autenticação para endpoints protegidos, com respostas estáveis e sem enumeração de e-mails.
- Validação: testes HTTP por cliente independente para formato válido/inválido, elegibilidade, limitação e segurança de resposta.
- Evidência: `DominoPontaDeQuina.Api` expõe solicitação pública uniforme, autenticação Bearer, revogação e limitação de cinco solicitações por origem em dez minutos. O adaptador SendGrid é configurável por `Email__SendGrid__*`; desenvolvimento mantém adaptador explícito sem entrega. `ApiHttpIntegrationTests` usa host HTTP real e SQLite descartável para validar confirmação sem token, autenticação, revogação e limitação. A entrega SendGrid real, logs e telemetria operacionais não foram executados.

### T-23 — Criar endpoints de partida e participação

- Estado: implementada e validada localmente.
- Critérios: AC-API-01 a AC-API-03 e AC-AUT-01.
- Responsabilidade e arquivos: projeto API, contratos HTTP, mapeamento de resultados e testes de contrato.
- Dependências: T-16, T-21 e T-22.
- Resultado: operações autenticadas de criar/localizar, ingressar, iniciar e consultar estado próprio, com autorização por participação e sem mão adversária.
- Validação: cliente HTTP independente para fluxos permitidos, não participante, partida indisponível e estado inválido.
- Evidência: a API expõe criação, participação, início e estado próprio por Bearer; a Application converte o `UsuarioId` autenticado no `JogadorId` persistido antes das verificações de participação. O teste HTTP cria, ingressa e inicia a partida usando dois tokens, sem acesso direto do cliente ao banco.

### T-24 — Criar endpoints de jogada, passagem e resultado

- Estado: implementada e validada localmente.
- Critérios: AC-API-04 a AC-API-08 e AC-AUT-01.
- Responsabilidade e arquivos: projeto API, contratos HTTP, mapeamento de erros e testes.
- Dependências: T-17 e T-23.
- Resultado: operações de jogar, passar, consultar resultado e histórico, com erros estáveis para turno, encaixe, passagem, término e conflito.
- Validação: sucesso, ação inválida, repetida, concorrente, partida encerrada e leitura posterior coerente.
- Evidência: rotas de jogada, compra, passagem e resultado mapeiam os códigos semânticos para respostas HTTP estáveis; `409` contém a visão autoritativa permitida. A compra única foi explicitada em `/comprar`, para que o cliente não adivinhe pedra da cava. Testes do Core cobrem compra que mantém turno e compra seguida de passagem; o teste HTTP valida jogada, conflito e estado resultante. O aceite de uma partida integral contra ambiente operacional permanece na T-32.

### T-25 — Publicar documentação completa e exemplos executáveis

- Estado: publicada localmente; validação por cliente externo permanece pendente.
- Critérios: AC-DOC-01, AC-DOC-02 e AC-API-01.
- Responsabilidade e arquivos: OpenAPI ou equivalente, README/API documentation e exemplos de cliente HTTP.
- Dependências: T-22 a T-24.
- Resultado: documentação de todas as operações, permissões, entradas, saídas, erros, campos, sequência ponta a ponta e uso seguro do token; exemplos funcionam sem banco ou código interno no cliente.
- Validação: executar os exemplos contra ambiente de validação com usuários de teste pré-cadastrados.
- Evidência: `DominoPontaDeQuina.Api/README.md` documenta OpenAPI/Swagger, rotas, corpos, estados, erros, compra, observação segura, sequência e configuração SendGrid. O host publica `/swagger/v1/swagger.json`; essa rota é verificada pelo teste HTTP. Ainda é necessária a reprodução da documentação por cliente independente em ambiente de validação.

### T-26 — Validar segurança e interoperabilidade da API

- Estado: parcialmente validada localmente.
- Critérios: AC-ID-01 a AC-ID-05, AC-AUT-01, AC-AUT-02, AC-API-01 a AC-API-08 e AC-DOC-03.
- Responsabilidade e arquivos: testes HTTP, documentação, configuração e correções estritamente necessárias.
- Dependências: T-25.
- Resultado: um terceiro cliente de teste, criado apenas com a documentação pública, completa a jornada; API não revela tokens, mãos privadas ou dados de participação indevidos.
- Validação: testes de contrato, inspeção de logs/telemetria permitida, varredura de respostas/URLs e jornada ponta a ponta.
- Evidência: `ApiHttpIntegrationTests` cobre por HTTP autenticação, criação/ingresso/início, jogada, conflito, observação sem `minhaMao`, revogação e limitação; a suíte completa aprovou 116 testes em 2026-09-21. Permanecem sem evidência: entrega SMTP real, inspeção de logs/telemetria operacionais e jornada por terceiro fora dos projetos da solução.

## Wave 4 — Clientes de referência

### T-27 — Criar fundação do painel Blazor

- Estado: pendente.
- Critérios: AC-WEB-01 e AC-DOC-03.
- Responsabilidade e arquivos: novo projeto Blazor .NET 8, cliente HTTP e configuração.
- Dependências: T-23 e T-25.
- Resultado: painel autenticado consome contratos públicos e apresenta lista ou identificação de partidas sem lógica de jogo local.
- Validação: testes de componentes para carregamento, ausência e falha.
- Evidência: testes e procedimento de execução.

### T-28 — Exibir visão restrita e atualização do painel

- Estado: pendente.
- Critérios: AC-WEB-01 e AC-AUT-02.
- Responsabilidade e arquivos: componentes Blazor, cliente da API e testes de interface.
- Dependências: T-24 e T-27.
- Resultado: mostra pedras na mesa e contagem de pedras de cada jogador, sem mãos privadas; trata estado desatualizado e encerrado. Transporte e cadência de atualização são documentados sem criar contrato exclusivo.
- Validação: estados de interface, ausência de campos privados e falha de atualização.
- Evidência: testes e revisão visual com dados de exemplo.

### T-29 — Criar fundação do cliente MAUI para Windows

- Estado: pendente.
- Critérios: AC-MAUI-01 e AC-DOC-03.
- Responsabilidade e arquivos: novo projeto MAUI .NET 8 para Windows, cliente HTTP e configuração.
- Dependências: T-25.
- Resultado: endereço da API configurável, token tratado sem ser exposto e clientes para os contratos públicos.
- Validação: testes de serialização, reutilização do token, autenticação ausente/inválida/revogada e indisponibilidade.
- Evidência: testes e passos de execução no Windows.

### T-30 — Implementar jornada de jogo no MAUI

- Estado: pendente.
- Critérios: AC-MAUI-01 e AC-API-01 a AC-API-08.
- Responsabilidade e arquivos: telas e estados do projeto MAUI, sem referências a Infrastructure ou banco.
- Dependências: T-24 e T-29.
- Resultado: o cliente demonstra acesso, criação/ingresso/início, consulta, jogada ou passagem e resultado; informa rejeições e conflitos sem sucesso indevido.
- Validação: execução manual reproduzível no Windows contra API de desenvolvimento e testes dos estados principais.
- Evidência: versão Windows, ambiente, passos e resultados.

## Wave 5 — Aceitação e publicação

### T-31 — Consolidar a pipeline de validação

- Estado: configuração atualizada na T-45, sem tolerância a falhas obrigatórias e com artefatos TRX; execução remota em branch de validação pendente.
- Critérios: suporte a todos os critérios aplicáveis.
- Responsabilidade e arquivos: `.github/workflows/validate-tests.yml`, projetos de teste e documentação de execução.
- Dependências: T-12, T-18, T-26, T-28, T-30 e T-44.
- Resultado: pipeline executa verificações relevantes e falha quando uma regressão coberta ocorre; não usa `continue-on-error` para ocultar falhas obrigatórias.
- Validação: execução da pipeline em branch de validação e reprodução local dos comandos.
- Evidência: logs e resultados reais.

### T-32 — Executar aceite independente da jornada pública

- Estado: consumidor HTTP/JSON independente e matriz adicionados na T-45; aceite operacional completo pendente conforme `execucao-t45.md`.
- Critérios: AC-ID-01 a AC-ID-05, AC-AUT-01 a AC-AUT-02, AC-API-01 a AC-API-08, AC-CLI-01 a AC-CLI-06, AC-DOC-01 a AC-DOC-03, AC-WEB-01 e AC-MAUI-01.
- Responsabilidade e arquivos: evidências de aceitação, documentação e correções de defeitos encontrados.
- Dependências: T-31 e T-44.
- Resultado: cliente independente, painel Blazor e MAUI Windows usam os mesmos contratos; toda a jornada é reproduzida sem lacunas entregues aos alunos.
- Validação: emissão de token, jogo completo, observação, erros, conflitos, resultado, exemplos e clientes em ambiente de validação.
- Evidência: matriz de aceite, comandos/procedimentos, ambiente, resultados e limitações.

## Aplicabilidade dos testes

| Tipo | Aplicável? | Justificativa e local |
| --- | --- | --- |
| Unitário | Sim | Core para pedra de abertura e Application para projeções, ativação de jogador e idempotência. |
| Integração | Sim | EF Core, migration, reserva de vaga, transação de outbox, identidade e entrega HTTP simulada. |
| Contrato HTTP | Sim | Identidade, ativação de jogador, listagem, ingresso, comandos, erros, privacidade, assinatura e exemplos públicos. |
| Interface / ponta a ponta | Sim | Receptor webhook, MAUI Windows, painel e terceiro cliente no ciclo HTTP + callback. |
| Carga | Condicional | Medir somente se retry/outbox ou limite de callbacks tiver requisito operacional; T-33 define a política antes. |

## Revisão técnica de pendências — 2026-09-21

### Evidência executada

- `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` concluiu com 97 aprovados, 0 falhos e 0 ignorados. Essa evidência cobre o projeto de testes existente, inclusive Core, Application e integração SQLite adicionada à suíte; não substitui testes HTTP, de interface, de migration aplicada ou aceite independente.
- `dotnet build DominoPontaDeQuina.slnx --no-restore` não concluiu. O compilador reportou `CS1529` em `DominoPontaDeQuina.Blazor/Program.cs:8`, pois há um `using` após instruções de nível superior. A mesma execução também não conseguiu copiar o executável da API porque `DominoPontaDeQuina.Api.exe` estava em uso (`MSB3021`). A falha de cópia não é evidência de defeito do código da API, mas impede validar o build integral enquanto o processo estiver ativo.
- `git diff --check` não reportou erro de espaço em branco; o Git emitiu somente avisos de normalização LF/CRLF nos arquivos já modificados.

### Situação observada e próximo responsável

| Área | Situação observada | Pendência rastreada |
| --- | --- | --- |
| Core | A suíte aprovada dá evidência do motor no estado atual. | T-38 acrescenta a obrigação observável da pedra inicial e sua projeção; a evidência anterior não a cobre. |
| Domain, Application e Infrastructure | Há contratos, persistência, jornada e testes na árvore de trabalho; a suíte aprovada é evidência parcial. | Permanecem as validações de banco real da T-18 e o aceite de concorrência/privacidade via HTTP das T-24 e T-26. |
| Migrations | A migration de estado recuperável está presente, mas a atualização de banco descartável não foi comprovada nesta revisão. | T-18 continua pendente quanto à aplicação em banco descartável. |
| API | Há host e rotas de identidade/partida na árvore de trabalho, sem OpenAPI e sem testes HTTP localizados nesta revisão. | T-22 a T-25 continuam pendentes de testes de contrato, documentação completa e exemplos executáveis. T-26 depende dessas evidências. |
| Blazor | Existe projeto esqueleto, mas o build falha em `Program.cs`; a página atual não comprova consumo autenticado, visão restrita ou estados exigidos. | Corrigir o bloqueio de compilação dentro de T-27 antes de iniciar a validação de T-28. |
| MAUI | Não foi localizado projeto MAUI na solução ou na árvore de trabalho revisada. | T-29 e T-30 permanecem pendentes. |
| Testes e CI/CD | A suíte local passou; o workflow ainda usa `continue-on-error: true` e valida apenas o projeto de testes. | T-31 permanece pendente e só deve ser alterada após as dependências declaradas. |

Não marcar T-22 a T-45 como concluída sem as evidências indicadas em cada tarefa. Não foram feitas alterações em código de produto nesta revisão técnica.

## Verificações de conclusão

- Revisão: Wave 1 revisada por build, análise estática e regressão do Core; integração com Application/Infrastructure permanece nas waves posteriores.
- Testes pertinentes: T-12 aprovada; 83 de 83 testes do Core aprovados.
- Aceitação: pendente; nenhuma wave posterior inicia sem a evidência de suas dependências.
- Segurança: pendente; revisar tokens, enumeração de e-mails, participação, mãos privadas, logs e concorrência.
- Documentação: pendente; contratos e exemplos devem refletir somente comportamento implementado e validado.
