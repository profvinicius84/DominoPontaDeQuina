# Fluxo de clientes e webhooks

- Data: 2026-09-23.
- Origem: fluxo confirmado pelo responsável e `sdd/product-spec.md`.
- Estado: T-33 a T-39 implementadas e validadas em 2026-09-23. Consulte a [evidência consolidada](execucao-t33-t39.md) e o README da API.

## Identidade e participação

O token recebido por e-mail pode ser guardado e reutilizado em novos acessos, inclusive por dois clientes, até revogação. Não é de uso único nem expira automaticamente por tempo; abrir uma nova sessão não exige solicitar outro token. Revogar esse token impede novos acessos com ele em todos os clientes.

Usuário representa quem autentica e pode possuir vários jogadores-perfis. Um jogador identifica o perfil escolhido para um cliente ativo; participação representa a posição desse jogador em uma partida. Dois jogadores diferentes podem pertencer ao mesmo usuário e ocupar as duas posições da mesma partida, sem compartilhar mão, turno ou destino de notificação.

Antes de criar ou ingressar, o cliente ativa um jogador-perfil da conta pelo ciclo definido pela API. Um jogador só pode estar ativo em um cliente por vez. O cliente solicita ingresso informando `partidaId`, `jogadorId` e webhook. Não envia objeto nem ID prévio de participação. A API valida que o jogador pertence ao usuário autenticado, está ativo para aquele cliente, não possui participação incompatível e que há vaga; então monta e persiste a participação, atribuindo posição e estado internamente. O cliente não define posição, mão, pontuação ou turno no ingresso.

Ações, consultas privadas, histórico, idempotência e notificações resolvem o jogador e a participação no servidor. Para ações de participação já criada, a chave lógica de repetição usa partida, jogador autor e chave de idempotência; não usa somente o usuário. No ingresso, a participação ainda não existe: o servidor reconhece repetições por partida, jogador validado e chave da requisição, sem exigir uma entidade de participação do cliente.

## Estado enviado ao cliente

Cada evento precisa identificar evento, partida, participação destinatária e versão do estado. O estado inclui situação, rodada, tabuleiro, mão própria, contagens públicas, pontuação, turno e próxima ação permitida ou obrigatória: aguardar, jogar a pedra de abertura indicada, jogar, comprar ou passar conforme a regra, ou reconhecer encerramento. Os nomes finais dos campos e estados pertencem ao contrato técnico.

A pedra de abertura é escolhida pelo servidor conforme RG-04. A API informa essa pedra ao jogador inicial e exige seu uso; o adversário não recebe antecipadamente a mão ou a pedra privada por esse motivo. Distribuição e notificação não jogam a pedra automaticamente pelo cliente. O mesmo critério se aplica às novas rodadas.

O webhook é informado ao ingressar, inclusive quando criar já inclui ingresso. Cada transição aceita gera estados filtrados para os destinatários após sua persistência. Uma falha de entrega não desfaz a ação aceita. Repetir um comando não executa novamente a jogada.

O cliente mantém o receptor ativo enquanto aguarda HTTP. Eventos antecipados são guardados; eventos repetidos ou antigos não fazem o estado regredir. Após sucesso, usa o estado correspondente à versão confirmada ou posterior. Após rejeição, trata o erro e o estado autoritativo disponível, sem exigir um evento que não existe. A consulta continua como recuperação em caso de falha de entrega.

## Decisões técnicas fechadas

A ativação usa chave aleatória de 32 bytes gerada e guardada pelo cliente; a API persiste somente SHA-256. A mesma chave retoma a ativação e outra recebe conflito. A liberação é explícita, exige Bearer, propriedade do perfil e a chave atual; somente depois outro cliente pode ativar com nova chave. Desconexão, fechamento e passagem de tempo não liberam o perfil. Um jogador com participação não encerrada não cria nem ingressa em outra partida; jogadores distintos da mesma conta permanecem independentes.

A solicitação por e-mail terá corpo somente com e-mail, confirmação pública uniforme e entrega exclusiva no endereço cadastrado. A autenticação direta terá e-mail e senha da conta e devolverá o token uma única vez sobre TLS. Ambos emitem tokens reutilizáveis até revogação e não invalidam tokens anteriores. A T-40 publica esses contratos; a rota acadêmica anterior permanece até essa adequação.

O início é explícito depois de duas participações. Cada transição aceita grava na mesma transação um evento por participação. O envelope usa `eventoId`, `partidaId`, `participacaoId`, `versaoEstado`, `tipo`, `ocorridoEmUtc` e `estado` filtrado. HTTP 2xx confirma; os demais resultados repetem com backoff exponencial, até oito tentativas e limite de quinze minutos entre tentativas. Eventos entregues ficam sete dias e abandonados ficam trinta dias. Consulta própria recupera evento perdido.

A entrega usa POST JSON, timeout padrão de cinco segundos e `X-Domino-Assinatura: sha256=<HMAC-SHA256>` calculado sobre `<X-Domino-Instante>.<corpo>`. O evento também segue em `X-Domino-Evento`. O Bearer nunca é encaminhado. Redirecionamentos são recusados e o DNS é resolvido em cada tentativa; loopback, endereços privados, link-local, multicast, não especificados e CGNAT são bloqueados. Loopback HTTPS pode ser habilitado apenas em desenvolvimento para receptor controlado.

## Estado implementado em 2026-09-23

A inspeção anterior foi superada pela implementação das T-33 a T-39: perfis, ativação, seleção explícita, lista, ingresso, próxima ação, pedra obrigatória, outbox e entrega automática estão presentes. A T-40 ainda precisa consolidar no OpenAPI os detalhes do callback e publicar as duas formas finais de autenticação.

| Local | Comportamento observado | Adequação necessária |
| --- | --- | --- |
| `DominoPontaDeQuina.Api/Program.cs` e `EndpointsJogadores.cs` | Expõem perfis, ativação/liberação, lista, criação, ingresso e ações com Bearer e prova de ativação. | T-40 documenta o callback e publica a autenticação final. |
| `DominoPontaDeQuina.Application/Partidas/JornadaPartidaService.cs` | Autoriza por jogador ativo, bloqueia participação incompatível e grava projeções privadas na outbox junto da transição. | Sem lacuna para T-33 a T-39. |
| `DominoPontaDeQuina.Application/Partidas/Contracts/**` | Recebem jogador/webhook sem participação do cliente e expõem próxima ação, participação e envelope versionado. | Sem lacuna para T-33 a T-39. |
| `DominoPontaDeQuina.Api/Webhooks/**` | Entrega assinada com repetição, retenção e defesa de destino. | T-41 implementa deduplicação e ordenação no cliente oficial. |

As evidências anteriores da jornada não comprovam AC-CLI-01 a AC-CLI-06. Validar por HTTP a ativação exclusiva e o ingresso com `partidaId`, `jogadorId` e webhook, sem objeto ou ID prévio de participação, incluindo rejeição de jogador sem vínculo, não ativado ou ativo em cliente incompatível. Usar dois clientes com o mesmo token, jogadores-perfis e receptores separados, cobrindo partidas iguais/diferentes, disputa de vaga, repetição do ingresso sem duplicação, privacidade, abertura, cava, resposta rejeitada, callback antecipado, duplicado, fora de ordem e indisponível.

## Planejamento técnico rastreável

As tarefas T-33 a T-45 em [tasks.md](tasks.md) desdobram este fluxo por contrato, token, jogadores-perfis, ativação, migration, jornada, motor, entrega, API, clientes e aceite. A T-33 precede implementação porque a credencial Bearer compartilhada autentica a conta, mas a exclusividade do jogador exige um ciclo de ativação controlado pela API. O ingresso é identificado pelo jogador-perfil. A entrega é planejada com caixa de saída persistente após o commit e sem garantia de ordem única: o cliente usa `eventoId` e `versaoEstado` para deduplicar e recuperar a visão por consulta.
