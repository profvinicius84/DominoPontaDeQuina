# API pública — Dominó Ponta de Quina

API .NET 8 para uma partida individual entre dois jogadores. O servidor executa as regras; clientes não acessam banco, Core, Application ou Infrastructure.

## Contrato acessível

Com a API em execução, o documento OpenAPI gerado está em `/swagger/v1/swagger.json` e a interface de consulta em `/swagger`. Esta página é a referência operacional complementar: usa os mesmos campos do JSON e enumera erros e sequência de uso.

O Swagger inclui descrições das operações, exemplos de requisição, modelos de resposta e códigos HTTP. Para executar as rotas protegidas, clique em **Authorize** e cole somente o token recebido por e-mail, sem o prefixo `Bearer`. A solicitação de token é pública; as demais operações exigem autenticação. Dois jogadores da mesma conta usam o mesmo token e perfis/ativações diferentes. Nos exemplos de ações, substitua a versão pela consultada e gere um UUID para cada nova intenção.

As propriedades JSON usam `camelCase`. `lado` é numérico: `0` para esquerda e `1` para direita. `situacao` usa `0` (`aguardandoInicio`), `1` (`emAndamento`) ou `2` (`encerrada`). Cada ação mutável recebe uma `chaveIdempotencia` UUID nova e a `versaoEsperada` retornada pelo estado anterior.

## Acesso

`POST /api/identidade/token` recebe somente `{ "email": "aluno@fiap.com.br" }`.

Esta é a única rota pública de emissão de token. Um e-mail válido `@fiap.com.br` ainda ausente é cadastrado na primeira solicitação, e o token é enviado a esse endereço. Endereços malformados ou fora do domínio recebem `400 Bad Request` com o booleano JSON `false`. Endereços FIAP válidos recebem `202 Accepted` com corpo vazio, sem revelar se a conta já existia. O token nunca aparece na resposta, URL, log ou telemetria da API. Há limites independentes de cinco solicitações por e-mail normalizado e 50 por IP de origem em dez minutos; o excedente recebe `429 Too Many Requests`. Os limites são mantidos em memória por instância da API.

O endereço do remetente SendGrid não é um destinatário; para receber o token, informe o e-mail FIAP desejado no corpo da solicitação.

Todas as demais rotas exigem `Authorization: Bearer <token>`. `POST /api/identidade/tokens/revogacao` revoga o token apresentado e responde `204 No Content`; chamadas posteriores com ele recebem `401 Unauthorized`.

O token é reutilizável até revogação, sem expiração automática. Reabrir o aplicativo ou autenticar outro cliente não exige nova solicitação por e-mail. Emitir outro token não invalida os anteriores; revogar um token bloqueia todos os clientes que usam esse token, preservando os demais tokens da conta. Cada cliente seleciona e ativa um jogador da conta.

## Jogadores e ativação exclusiva

| Operação | Rota | Corpo |
| --- | --- | --- |
| Listar meus perfis | `GET /api/jogadores` | — |
| Criar perfil | `POST /api/jogadores` | `{ "nome": "Meu jogador" }` |
| Ativar perfil | `POST /api/jogadores/{jogadorId}/ativar-cliente` | `{ "chaveAtivacao": "<64 caracteres hexadecimais aleatórios>" }` |
| Liberar perfil | `DELETE /api/jogadores/{jogadorId}/desativar-cliente` | Cabeçalho `X-Jogador-Ativacao` |

O cliente gera uma chave com 32 bytes criptograficamente aleatórios e a guarda antes de ativar o perfil, para poder repetir uma resposta perdida. A API armazena somente o hash. A resposta contém `jogadorId`, `nome`, `ativo` e `ativadoEmUtc`, sem chave ou hash. Ativar novamente com a mesma chave confirma a ativação existente; outra chave recebe `409 JOGADOR_JA_ATIVO`. O perfil precisa pertencer à conta autenticada.

Para operar ou consultar uma partida como jogador, envie `X-Jogador-Ativacao: <chave>` além do Bearer, e informe `jogadorId`. Somente o cliente dono da chave deve guardá-la e usá-la. Não envie chaves ou tokens em URLs. Listar partidas e observar a mesa exige apenas Bearer. IDs de conta e jogador são diferentes: envie o ID do perfil em `jogadorId`.

Não há expiração automática de ativação ou tomada de um jogador usando apenas o Bearer. Retome com a mesma chave. Para transferir, o cliente atual libera explicitamente com a chave correta e o novo cliente ativa com uma chave nova. O protocolo comprova posse da chave; não identifica fisicamente um dispositivo se o usuário copiar deliberadamente o segredo para outro cliente.

O ingresso informa ID da partida na rota e `jogadorId` no corpo, além de versão e chave de idempotência. A API cria, identifica e posiciona a participação. O cliente nunca envia objeto, posição ou ID de participação. Um jogador com participação não encerrada não pode criar nem ingressar em outra partida.

## Rotas da jornada

| Operação | Rota | Corpo |
| --- | --- | --- |
| Listar partidas com uma vaga | `GET /api/partidas` | — |
| Criar partida | `POST /api/partidas` | `{ "pontuacaoAlvo": 50, "jogadorId": "<perfil>" }` |
| Entrar | `POST /api/partidas/{id}/entrar` | ação |
| Iniciar | `POST /api/partidas/{id}/iniciar` | ação |
| Estado próprio | `GET /api/partidas/{id}?jogadorId={perfil}` | — |
| Atualizações | `GET /api/partidas/{id}/atualizacoes?jogadorId={perfil}&versaoConhecida={versao}` | — |
| Observação segura | `GET /api/partidas/{id}/visualizar` | — |
| Jogar | `POST /api/partidas/{id}/jogar` | ação + `valorA`, `valorB`, `lado` |
| Comprar | `POST /api/partidas/{id}/comprar` | ação |
| Passar | `POST /api/partidas/{id}/passar` | ação |
| Cancelar | `POST /api/partidas/{id}/cancelar` | ação |
| Resultado e histórico | `GET /api/partidas/{id}/resultado?jogadorId={perfil}` | — |

Corpo de ação: `{ "jogadorId": "<perfil>", "versaoEsperada": 2, "chaveIdempotencia": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" }`. Inclua o cabeçalho `X-Jogador-Ativacao` nas operações de jogador.

Em `jogar`, acrescente `valorA`, `valorB` e `lado` ao corpo de ação. A listagem retorna `partidaId`, `pontuacaoAlvo` e `versaoEstado`, sem mãos privadas. A vaga é revalidada no ingresso.

`comprar` efetua uma única compra quando não há encaixe: se a pedra comprada encaixar, a mão aumenta e o turno permanece com o jogador; se não encaixar (ou não houver cava), registra a passagem e troca o turno. O cliente jamais informa nem adivinha uma pedra da cava. `passar` só é aceito se não existe cava e não há peça compatível; caso contrário é rejeitado sem mutação.

As respostas de sucesso de ação são `{ "chaveIdempotencia", "acaoIdempotente", "estado" }`. Repetir exatamente a mesma intenção devolve o estado anterior com `acaoIdempotente: true`, sem duplicar pontos, turno ou jogada.

Em `GET /resultado`, cada item de `eventos` conserva `pecaJogada`, `lado` e `passouVez` no JSON. Para uma jogada, pedra e lado têm valor e `passouVez` é `false`. Para uma passagem explícita, pedra e lado são `null` e `passouVez` é `true`. Para uma compra, pedra e lado são `null` e `passouVez` é `false`, inclusive quando a compra causa passagem automática; consulte o estado para identificar o turno resultante. `pontosConcedidos` representa pontos da jogada, não a transferência de pontos por travamento no fim da rodada. O cancelamento não concede pontos.

O estado de participante contém `participacaoId`, `minhaMao`, tabuleiro orientado, participantes com contagem de pedras, turno, pontuação, versão, vencedor e `proximaAcao`: `aguardar`, `jogarAbertura`, `jogar`, `comprar`, `passar` ou `encerrada`. Somente `jogarAbertura` inclui `pedraAberturaObrigatoria`; enviar outra pedra é rejeitado sem mutação. A rota de observação não contém `minhaMao`, a pedra obrigatória nem qualquer mão privada.

`tabuleiro` é uma lista espacial **da esquerda para a direita**, com cada pedra já orientada. Quando há pedras, a ponta livre esquerda é `tabuleiro[0].valorA` e a direita é `tabuleiro[tabuleiro.length - 1].valorB`. A lista vazia não possui pontas. Por exemplo, `[{"valorA":3,"valorB":5},{"valorA":5,"valorB":6},{"valorA":6,"valorB":6}]` tem encaixes livres em `3` e `6`. Para **pontuar**, a dupla exposta `6/6` contribui com as duas faces: nesse exemplo, `3 + 6 + 6 = 15`, ou 3 pontos para quem fez a jogada. O servidor valida o encaixe e concede os pontos; o cliente usa as pontas para escolher `lado`.

Exemplo de trecho do formato de resposta após três pedras, mostrando a orientação publicada pela API:

```json
{
  "tabuleiro": [
    { "valorA": 3, "valorB": 5 },
    { "valorA": 5, "valorB": 6 },
    { "valorA": 6, "valorB": 6 }
  ],
  "proximaAcao": 2
}
```

O trecho ilustra os campos relevantes; a resposta completa inclui também versão, participantes, mão própria e situação. `proximaAcao` depende do jogador que consulta.

Qualquer participante com perfil ativo pode cancelar uma partida aguardando início ou em andamento. O cancelamento encerra a partida sem vencedor (`vencedorId: null`), preserva placar, pedras e histórico e libera ambos os jogadores para criar ou ingressar em outra partida. Envie versão e chave de idempotência como nas demais ações. Repetir a mesma intenção não cancela duas vezes; uma nova tentativa após encerramento recebe `ESTADO_INVALIDO`. Para começar novamente, crie outra partida. O cancelamento aparece no estado e no evento `partida-cancelada` para clientes de webhook; a lista `eventos` do resultado continua restrita a jogadas, compras e passagens.

## Consulta de atualizações

O cliente consulta `/api/partidas/{id}/atualizacoes` periodicamente enquanto acompanha a partida. Envie `jogadorId`, Bearer e `X-Jogador-Ativacao` como na consulta de estado próprio. `versaoConhecida` é opcional: sem ela, a API retorna `200` e o estado atual; com a versão atual, retorna `204 No Content`; quando a versão difere, retorna `200` com a visão privada atual do jogador. O cliente MAUI consulta a cada dois segundos e aplica imediatamente o estado retornado por uma ação.

As respostas de atualização exigem a posse do perfil ativo e não revelam a mão do adversário. Depois de uma desconexão, a próxima consulta recupera a versão atual. O cliente deve aplicar apenas versões mais novas para evitar regressão.

## Compatibilidade com clientes de webhook anteriores

No HTTP, `proximaAcao` usa `0 = aguardar`, `1 = jogarAbertura`, `2 = jogar`, `3 = comprar`, `4 = passar`, `5 = encerrada`. No callback, os mesmos nomes são strings conforme o esquema de callback no OpenAPI. O cliente compartilhado aceita as duas representações.

Cada transição aceita grava um evento privado na mesma transação e o entrega depois do commit. O comando HTTP confirma a operação; falha do callback não desfaz ingresso, início, jogada, compra ou passagem. Rejeições sem mudança de versão não geram evento.

O corpo contém `eventoId`, `partidaId`, `participacaoId`, `versaoEstado`, `tipo`, `ocorridoEmUtc` e `estado`. O receptor responde qualquer 2xx para confirmar, deduplica por `eventoId` e só aplica estado cuja versão seja maior que a já processada. A entrega é ao menos uma vez e pode ocorrer antes da resposta do comando. A consulta de estado próprio recupera notificações perdidas.

A assinatura segue em `X-Domino-Assinatura: sha256=<hex>`, calculada com HMAC-SHA256 sobre `<X-Domino-Instante>.<corpo JSON>`. `X-Domino-Evento` repete o identificador para correlação. Configure o segredo por `Webhook__AssinaturaSegredo`; ele deve ser compartilhado com receptores autorizados por canal externo e nunca registrado. O Bearer do usuário não é enviado.

O worker usa timeout padrão de cinco segundos, redirecionamentos desabilitados e até oito tentativas com backoff exponencial. Resolve DNS a cada tentativa e recusa loopback, faixas privadas, link-local, multicast, não especificadas e CGNAT. `Webhook__PermitirLoopbackEmDesenvolvimento=true` existe somente para receptores HTTPS locais controlados. Eventos entregues permanecem sete dias e abandonados, trinta dias.

## Erros estáveis

Erros de domínio retornam `{ "codigo": "...", "estadoAutoritativo": { ... } }` quando o chamador pode receber uma visão atual da própria partida.

Respostas `400` possuem três formatos existentes: uma entrada de domínio inválida retorna, por exemplo, `{"codigo":"ENTRADA_INVALIDA","estadoAutoritativo":null}`; uma falha de desserialização ou modelo pode retornar `400` com corpo vazio; e um e-mail inválido em `POST /api/identidade/token` retorna o booleano JSON `false`. O cliente deve verificar status e tipo do corpo antes de ler `codigo`. Esses formatos são mantidos por compatibilidade.

| HTTP | `codigo` | Significado e reação do cliente |
| --- | --- | --- |
| 400 | `ENTRADA_INVALIDA` | Corrija formato, UUID, versão ou pedra. |
| 403 | `SEM_PERMISSAO` | A identidade não pode executar a ação. |
| 404 | `PARTIDA_INDISPONIVEL` | A partida não existe ou não está disponível ao participante. |
| 409 | `CONFLITO_DE_ESTADO` | Substitua a cópia local por `estadoAutoritativo` e tente nova intenção. |
| 409 | `JOGADOR_JA_ATIVO` | Perfil reservado por outra chave de ativação; selecione outro jogador ou retome com a chave original. |
| 422 | `ESTADO_INVALIDO`, `FORA_DO_TURNO`, `PECA_AUSENTE`, `ENCAIXE_INVALIDO` ou `PASSAGEM_NAO_PERMITIDA` | Não houve alteração; apresente a rejeição. |
| 401 | `NAO_AUTENTICADO` | Token ausente, inválido ou revogado. |
| 429 | `LIMITE_EXCEDIDO` | Aguarde a janela de limitação antes de solicitar token novamente. |

## Sequência reproduzível

1. O cliente informa seu e-mail FIAP em `POST /api/identidade/token`; a API cria a conta se necessário e envia o token.
2. Os clientes usam o token recebido, listam/criam perfis distintos e ativam cada jogador com sua própria chave. Também podem usar contas diferentes.
3. O primeiro cria a partida com seu perfil; o segundo lista as partidas com vaga e entra com seu perfil e versão; um participante inicia usando a próxima versão.
4. Cada participante consulta atualizações do próprio estado. Jogar, comprar ou passar usa a versão consultada e UUID novo.
5. O painel usa somente `/visualizar`. Resultado e histórico são consultados por participante em `/resultado`.
6. Em `409`, o cliente substitui seu estado pelo `estadoAutoritativo`; em `401`, verifica o token apresentado e solicita outro se necessário; em `422`, não informa sucesso.

## Configuração e entrega de e-mail

Para desenvolvimento, o `appsettings.json` local da API configura SQL Server com `Domino:UsarSqlite=false` e `ConnectionStrings:DefaultConnection`. O arquivo é ignorado pelo Git. Para usar SQLite em um ambiente descartável, configure `Domino:UsarSqlite=true` e uma conexão SQLite. O adaptador de e-mail depende de configuração própria.

Ao atualizar uma base SQLite que ainda possui `TokensAcesso.ExpiraEmUtc`, siga o [procedimento de atualização](../DominoPontaDeQuina.Migrations/README.md) antes de iniciar esta versão. `EnsureCreated` cria bases novas, mas não migra bases antigas.

O remetente local é `profvinicius-domino@orisun.com.br`. O arquivo local `appsettings.json` é ignorado pelo Git porque pode conter uma chave real. Para enviar e-mails, use uma chave emitida pela conta com permissão Mail Send; configure-a por variável de ambiente ou cofre de segredos:

```text
Email__SendGrid__Habilitado=true
Email__SendGrid__ApiKey=<chave-de-api>
```

O adaptador SendGrid envia pela API Mail Send e não registra o token. Antes de disponibilizar a API, valide a entrega em caixa de teste, reutilização/revogação, limites, logs e telemetria sem expor segredo. A resposta 202 do SendGrid confirma o aceite da requisição, não a chegada à caixa de entrada.

`GET /health` retorna `200` apenas quando a API consulta a tabela de usuários com sucesso; `503` indica problema de conexão, permissão ou esquema.
