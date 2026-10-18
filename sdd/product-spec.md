# Especificação de produto — Dominó Ponta de Quina

## Atualização de fluxo — 2026-09-24

O cliente acompanha a partida consultando periodicamente o estado privado por versão. `GET /api/partidas/{partidaId}/atualizacoes` exige o token, o perfil e sua chave de ativação; retorna `204` quando a versão conhecida permanece atual e `200` com o estado quando houve mudança. A resposta de cada ação já contém o estado confirmado. Criar e ingressar não exigem URL de webhook, receptor, relay nem segredo compartilhado. O cliente MAUI consulta a cada dois segundos enquanto a partida está aberta e aplica somente versões não anteriores à atual. Esta decisão substitui as exigências de webhook e correlação de callback descritas abaixo; o suporte anterior permanece apenas para compatibilidade.

## Atualização de fluxo — 2026-09-23

O token recebido por e-mail é reutilizável em diversos acessos e aplicativos clientes até ser revogado. Fechar o aplicativo, sair e acessar novamente não consome nem invalida o token; não é necessário solicitar outro a cada acesso. Esta decisão substitui a exigência anterior de validade limitada: não há expiração automática por tempo no fluxo aprovado.

Decisão do responsável: o cliente autentica o usuário na API ou solicita token a partir do e-mail. Autenticado, lista jogos disponíveis com exatamente um participante e pede ingresso em um deles, ou cria uma partida como primeiro participante. A vaga deve ser revalidada no ingresso.

A mesma credencial pode autenticar dois aplicativos clientes, que podem participar de jogos diferentes ou ocupar as duas posições do mesmo jogo. Um usuário pode possuir vários jogadores-perfis; jogadores distintos identificam clientes distintos para o mesmo usuário. Cada jogador é ativado em apenas um cliente por vez. Mão, turno, autorização de ações e webhook pertencem à participação; não se pode identificar a posição apenas pelo usuário.

O cliente solicita ingresso passando o ID da partida, o ID do jogador escolhido e o webhook já previsto. Não envia objeto nem ID prévio de participação. A API valida que o jogador pertence ao usuário autenticado, está ativo para aquele cliente, não possui participação incompatível e que a partida tem vaga; então monta e persiste a participação, atribuindo posição e estado internamente. A distinção entre clientes com a mesma credencial é dada por jogadores-perfis distintos; não deve ser resolvida exigindo que o cliente monte a entidade de participação.

Ao ingressar, o cliente informa o webhook em que receberá automaticamente seu estado. A criação que já inclui ingresso também registra esse endereço. A API informa início da partida, turno, necessidade de cava e encerramentos. Na abertura de cada rodada, informa explicitamente ao participante inicial qual pedra deve jogar, conforme RG-04, e valida essa obrigação ao receber a jogada.

O cliente envia a ação, aguarda o resultado HTTP e depois processa o estado recebido pelo webhook antes de decidir a próxima ação. Como os transportes são independentes, mantém o receptor ativo e guarda eventos que cheguem antes da resposta. Correlaciona partida, participação e versão, sem regredir com eventos antigos ou repetidos. Uma ação rejeitada sem alteração de estado não obriga a emissão de novo evento. Consulta de estado permanece disponível para recuperação.

Essas decisões prevalecem sobre interpretações anteriores de um único jogador por usuário e de acompanhamento somente por consulta. A solicitação por e-mail recebe apenas o e-mail, responde uniformemente e nunca devolve o token no corpo; a autenticação direta recebe as credenciais da conta e devolve um novo token uma única vez sobre TLS. Nos dois casos o token é reutilizável até revogação. As duas formas estão publicadas no contrato HTTP e verificadas na T-40. O início permanece uma operação explícita de um participante depois do segundo ingresso.

Detalhes e lacunas: [fluxo de clientes e webhooks](features/partida-individual-plataforma/fluxo-clientes-webhooks.md). Os requisitos abaixo são rastreados às evidências e limites na [matriz de aceite T-45](features/partida-individual-plataforma/execucao-t45.md).

| Critério | Resultado observável obrigatório |
| --- | --- |
| AC-CLI-01 | Dois clientes com a mesma credencial, usando jogadores-perfis distintos e ativos, participam de jogos diferentes e também ocupam as duas posições do mesmo jogo, com participações distintas. Um jogador não fica ativo em dois clientes ao mesmo tempo. |
| AC-CLI-02 | Listagem autenticada mostra jogos aguardando adversário com exatamente um participante; disputa pela última vaga não cria terceiro participante. |
| AC-CLI-03 | Criar/ingressar registra webhook por participação; cada cliente recebe automaticamente sua visão, sem compartilhar mãos privadas. |
| AC-CLI-04 | Eventos indicam início, turno, necessidade de cava e encerramentos; a pedra de abertura é explícita e jogar outra é rejeitado sem mutação. |
| AC-CLI-05 | Cliente trata HTTP e webhook correlacionados, inclusive callback antecipado, duplicado ou fora de ordem, e não fica esperando evento inexistente após rejeição. |
| AC-CLI-06 | Cliente solicita ingresso com ID da partida, ID do jogador escolhido e webhook, sem objeto ou ID prévio de participação; a API valida vínculo autenticado, ativação, compatibilidade e vaga e cria a participação internamente. |

## Status e fontes

- Atualização de implementação em 2026-09-24: API, painel e cliente Windows implementados;
  suíte Release com 165 testes aprovados. Consulte a [matriz de aceite T-45](features/partida-individual-plataforma/execucao-t45.md)
  para a correspondência por critério. Aceite visual Windows, operação SMTP/rede externa e CI remota
  permanecem pendentes. As notas de revisão abaixo preservam o contexto da especificação original.

- Revisão: 2026-09-21, com participação do agente `product_owner`.
- Status: especificação de produto revisada, com decisões confirmadas e bloqueadores internos identificados. A implementação completa da API e sua prontidão para uso pelos alunos ainda não foram comprovadas.
- Esta revisão define o produto e os critérios observáveis. Não declara componentes implementados nem resultados de testes que não tenham sido executados.
- Fontes: decisões do responsável, [README raiz](../README.md), [README do Core](../DominoPontaDeQuina.Core/README.md), [contexto SDD](../.codex/sdd-toolkit/context/product.md), código e projetos existentes na solução.

## Problema e objetivo acadêmico

O produto deve oferecer uma API completa de Dominó Ponta de Quina para que os alunos estudem sua documentação e construam clientes próprios. Antes de ser disponibilizada à turma, a API deve implementar e documentar todo o domínio, todos os fluxos e todos os contratos necessários para jogar uma partida de ponta a ponta. Nenhuma regra ou lacuna funcional da API será transferida aos alunos.

A atividade atual dos alunos consiste em:

1. estudar a documentação publicada da API;
2. obter acesso pelo fluxo documentado;
3. consumir os contratos por meio de um cliente desenvolvido pelo próprio aluno;
4. demonstrar o fluxo completo da partida e o tratamento dos resultados e erros documentados.

O painel Blazor permitirá acompanhar partidas em execução. O cliente .NET MAUI será uma implementação de referência dos mesmos contratos públicos, sem possuir privilégios ou acesso direto à persistência.

## Escopo incluído e excluído

### Decisões confirmadas

| Frente | Resultado obrigatório | Limite de produto |
| --- | --- | --- |
| API | Implementar o fluxo jogável completo, identidade, autorização, persistência necessária, consulta de estado, resultados e histórico necessário à jornada. | Deve estar completa, documentada e validada antes da disponibilização aos alunos. |
| Documentação da API | Publicar contratos completos em OpenAPI ou formato equivalente acessível, com exemplos reproduzíveis, erros e sequência ponta a ponta. | Deve permitir a criação de um cliente independente de Blazor, MAUI e banco de dados. |
| Acesso dos alunos | Permitir solicitação de token por operação pública específica para um e-mail FIAP, criando a conta se ausente. | O token é enviado ao e-mail informado e nunca retornado no corpo da resposta de solicitação. |
| Painel Blazor | Acompanhar partidas em execução mostrando as pedras jogadas na mesa e a quantidade de pedras de cada jogador. | Não concede ao aluno uma forma alternativa de completar lacunas do contrato nem revela mãos privadas. |
| Cliente MAUI | Demonstrar o consumo correto da API pública. | É exemplo de cliente; não define comportamento exclusivo nem substitui a documentação. |
| Exercício Core existente | Permanecer como material existente no repositório e como registro de uma atividade anterior ou separada. | A atividade atual de consumo da API não pede ao aluno alterações no Core. |

Não fazem parte do escopo confirmado cobrança, apostas, torneios, chat, bots, gestão de turmas ou funcionalidades comerciais.

### Condição de fechamento do escopo

Qualquer decisão interna ainda aberta deve ser resolvida pelo responsável e pela equipe do projeto antes da publicação. Ela não pode aparecer na documentação entregue aos alunos como comportamento indefinido, trabalho a completar ou escolha deixada ao cliente.

## Atores e permissões

- **Professor ou responsável:** define as regras ainda ambíguas e decide quando a API está pronta para a turma.
- **Aluno integrador:** estuda a documentação, solicita acesso com seu e-mail FIAP e implementa um cliente que consome exclusivamente a API pública.
- **Usuário cadastrado:** identidade criada na primeira solicitação elegível de token. O cadastro não implica participação automática em qualquer partida.
- **Jogador-perfil:** perfil pertencente a um usuário cadastrado. Pode ser ativado em um único cliente por vez e, quando participar de uma partida, consulta informações e executa somente as ações permitidas à sua participação e ao estado corrente.
- **Professor/observador:** acompanha partidas pelo painel vendo somente as pedras já jogadas e a quantidade de pedras de cada jogador. A observação não concede permissão para jogar.
- **Operação da plataforma:** mantém configuração, entrega de e-mail, revogação de acesso, proteção contra abuso e evidências operacionais, sem expor tokens.

### Autorização confirmada

- Toda operação protegida exige um token válido e vigente.
- A identidade autenticada e a participação na partida são verificações distintas.
- Operações de jogador exigem que o jogador-perfil ativo pertença ao usuário autenticado e participe da partida correspondente.
- Uma ação somente pode ser aceita quando o participante também possuir permissão para aquela ação no estado corrente, inclusive turno e situação da rodada.
- Consultas não podem revelar mão, token ou informação privada de outro participante além da política explicitamente aprovada.

## Estado atual observado

A solução contém Core, Domain, Application, Infrastructure, Migrations e Tests. API, Blazor e MAUI ainda não possuem projetos no estado inspecionado. O Core contém métodos com `NotImplementedException` e marcações `TODO ALUNO`.

Application registra partidas, jogadores e lances, e consulta status, histórico e ranking. Isso não demonstra a execução integral das regras, a integração entre motor e persistência, a autorização por participação nem a existência dos contratos HTTP definidos nesta especificação. O projeto de testes observado referencia somente o Core.

Persistência e migrations existem, mas a revisão documental identificou inconsistências de configuração. Esses fatos não comprovam nem impedem isoladamente uma implementação futura; devem ser tratados no planejamento técnico e validados por execução.

O README do Core descreve um exercício de preenchimento de lacunas. Esse exercício é histórico ou separado da atividade atual. Para a entrega aqui especificada, cabe à equipe do projeto concluir e validar essas lacunas antes da publicação da API; os alunos consumidores não alteram o Core.

## Regras e invariantes do jogo

| ID | Regra confirmada | Fonte |
| --- | --- | --- |
| RG-01 | Partida contém rodadas; cada rodada contém jogadas. | README do Core. |
| RG-02 | A partida é um embate entre dois jogadores, sem times. | Decisão do responsável em 2026-09-21. |
| RG-03 | Cada jogador recebe sete peças no início da rodada; as demais permanecem na cava. | Decisão do responsável em 2026-09-21. |
| RG-04 | Cada rodada inicia com o jogador que possui a maior dupla disponível em sua mão. Sem dupla nas duas mãos, inicia quem possui a pedra de maior valor; o servidor compara as pedras pela face mais alta e, em seguida, pela outra face, ambas em ordem decrescente. Portanto, `6:1` prevalece sobre `5:2` e sobre `6:0`. A API determina esse jogador sem expor as peças de um participante ao outro. | Decisão do responsável em 2026-09-21. |
| RG-05 | Os turnos seguem a ordem definida para os dois jogadores. | README do Core, adaptado ao embate individual. |
| RG-06 | A peça deve encaixar na ponta escolhida. | README do Core. |
| RG-07 | Soma das pontas externas múltipla de cinco gera `soma / 5` pontos para o jogador que realizou a jogada. | Decisão do responsável; regra de múltiplos de cinco preservada. |
| RG-08 | Sem jogada válida, o jogador compra exatamente uma peça da cava quando ela existir; se a peça comprada não permitir jogada, ou se a cava estiver vazia, passa a vez. | Decisão do responsável em 2026-09-21. |
| RG-09 | A rodada termina quando um jogador fica com a mão vazia ou quando o tabuleiro trava. No travamento, o valor das pedras na mão de um jogador gera pontos para o adversário somente quando for múltiplo de cinco; a rodada seguinte reinicia pelo critério da maior dupla. | Decisão do responsável em 2026-09-21. |
| RG-10 | Novas rodadas continuam até um jogador alcançar ou ultrapassar a pontuação alvo definida para a partida. | README do Core. |

São requisitos do produto, não evidência de implementação. A API entregue deve apresentar um único comportamento definitivo para cada regra e validá-lo no servidor. O cliente não pode decidir distribuição, turno, validade de jogada, pontuação, encerramento ou vencedor.

## Fluxos principais, alternativos e erros

### Solicitação e recebimento do token

1. O aluno envia seu e-mail FIAP à operação pública específica de solicitação de token.
2. A API cria a conta se o e-mail elegível ainda não existir.
3. A API valida a requisição e aplica limites de abuso sem revelar se o e-mail existe.
4. Para qualquer solicitação processável, a API devolve uma confirmação genérica uniforme, sem token no corpo.
5. Para e-mail FIAP elegível, a plataforma emite um token reutilizável até revogação e o envia exclusivamente ao endereço solicitado. E-mail fora do domínio não recebe token.
6. O corpo da resposta HTTP, a URL e os logs não contêm o token.
7. O aluno reutiliza o token recebido nas operações protegidas e em novos acessos, inclusive em dois clientes, até sua revogação. Não precisa solicitar novo token a cada acesso.

Solicitação malformada pode ser rejeitada conforme o contrato documentado, desde que o erro não revele a existência de um cadastro. Falha na entrega do e-mail deve ser observável pela operação da plataforma sem registrar o token e sem transformar a resposta pública em mecanismo de enumeração.

### Ciclo de vida do acesso

- O token permanece válido até revogação, sem expiração automática por tempo. A documentação informa como o cliente reconhece acesso ausente, inválido ou revogado.
- Token revogado não pode voltar a ser aceito nem ser reutilizado para emitir outro acesso sem passar pelo fluxo autorizado.
- Deve existir capacidade operacional de revogar o token; a revogação impede seu uso em todos os clientes que o compartilham.
- Novas solicitações obedecem à política documentada de emissão e invalidação, definida tecnicamente sem expor detalhes que facilitem abuso.
- Tentativas excessivas são limitadas e monitoradas de forma a proteger o serviço e preservar a resposta pública uniforme.

### Ordem de ações simultâneas

Quando duas ações disputarem o mesmo estado da partida, a API considera primeiro a ação cujo timestamp de recebimento no servidor for mais antigo. Ações posteriores para aquele estado são rejeitadas segundo o contrato de conflito e não podem pontuar, avançar turno ou alterar a partida. Timestamps enviados pelo cliente não definem prioridade.

Abandono, desconexão e reconexão não exigem tratamento específico nesta etapa.

### Jornada jogável ponta a ponta

A API deve fornecer, com permissões explícitas, todas as operações necessárias para que um cliente independente consiga:

1. obter acesso pelo fluxo de e-mail;
2. criar uma partida ou localizar uma partida que possa receber o jogador, conforme o modelo definitivo de criação;
3. entrar na partida e identificar os dois participantes;
4. iniciar a partida quando as condições e a permissão documentadas forem satisfeitas;
5. consultar o estado permitido ao participante, inclusive mão própria, tabuleiro, pontuação, turno e situação da partida;
6. realizar uma jogada informando a peça e o lado conforme o contrato definitivo;
7. passar a vez somente quando a regra definitiva permitir;
8. consultar o estado resultante sem depender da resposta de uma interface específica;
9. identificar fim de rodada, vencedor, pontuação concedida, próxima rodada e fim de partida;
10. consultar o resultado final e o histórico necessário para compreender a sequência da partida.

Os nomes finais de rotas e a divisão técnica entre recursos serão definidos pela liderança técnica. A cobertura funcional acima é obrigatória e não autoriza omitir uma operação necessária ao fluxo.

### Falhas e estados alternativos

Os contratos devem distinguir, sem vazar informações privadas, pelo menos:

- requisição inválida;
- autenticação ausente, inválida ou revogada;
- usuário autenticado sem participação ou permissão para a partida;
- partida inexistente ou indisponível conforme a política de visibilidade;
- estado que ainda não permite iniciar;
- ação fora do turno;
- peça inexistente na mão do jogador;
- encaixe inválido;
- passagem não permitida;
- ação repetida, concorrente ou baseada em estado desatualizado segundo a política definitiva;
- rodada ou partida já encerrada;
- indisponibilidade temporária do serviço.

Cada erro público deve possuir estrutura estável, significado documentado e orientação suficiente para o cliente reagir, sem depender da mensagem textual como único identificador.

## Contratos e documentação

### Contrato funcional mínimo

Esta tabela fixa a capacidade funcional mínima sem definir nomes de rotas, códigos HTTP ou formatos finais. Esses detalhes pertencem à especificação técnica, mas devem estar completos e estáveis antes da entrega à turma.

| Operação | Entrada funcional mínima | Saída funcional mínima | Erros e autorização mínimos |
| --- | --- | --- | --- |
| Solicitar token | E-mail do aluno em formato válido. | Confirmação genérica uniforme, sem token; conta FIAP criada se ausente. | Formato inválido e limitação de abuso. E-mail FIAP elegível recebe token por e-mail; endereço fora do domínio não recebe token. |
| Ativar jogador-perfil | Identidade autenticada e identificação de jogador pertencente à conta. | Vínculo exclusivo do jogador ao cliente, conforme contrato técnico. | Jogador de outra conta ou já ativo em cliente incompatível; liberação e revinculação seguem a política aprovada. |
| Criar partida | Identidade autenticada, jogador-perfil ativo e dados configuráveis aprovados para a partida. | Identificador, estado inicial e participação criada pela API para o jogador. | Token inválido; jogador inativo/incompatível; entrada inválida; criação não permitida. A política final de ativação e criação deve constar no contrato técnico. |
| Ingressar na partida | Identidade autenticada, identificação da partida, `jogadorId`, versão e chave de ingresso. | Participação criada pela API e estado permitido ao jogador. | Jogador sem vínculo/ativação, partida indisponível, lotada, encerrada ou ingresso não autorizado; repetição tratada de forma definida. |
| Iniciar partida | Identidade autenticada, participação, identificação da partida e referência de estado quando exigida. | Estado iniciado, participantes, turno e rodada conforme a visibilidade permitida. | Sem participação ou permissão; quantidade de jogadores ou estado inválido; conflito de concorrência. |
| Consultar estado próprio | Identidade autenticada e identificação da partida. | Estado autoritativo permitido, incluindo mão própria, tabuleiro, pontuação, turno, rodada, situação e versão quando adotada. | Sem participação; partida indisponível; informações privadas do adversário nunca são incluídas. |
| Consultar visão de observador | Identidade e permissão definidas para observação, além da identificação da partida. | Pedras já jogadas na mesa e quantidade de pedras na mão de cada jogador. | Observação não autorizada ou partida não visível; nunca concede ação de jogador nem revela as peças nas mãos. |
| Realizar jogada | Identidade participante, partida, peça, lado e versão ou chave de consistência conforme a política interna definitiva. | Confirmação da ação e estado ou referência ao estado resultante, com turno e pontuação coerentes. | Fora do turno, peça ausente, lado ou encaixe inválido, partida encerrada, repetição ou conflito; rejeição sem mutação parcial. |
| Passar a vez | Identidade participante, partida e versão ou chave de consistência conforme a política interna definitiva. | Confirmação da passagem e estado ou referência ao estado resultante. | Passagem não permitida pela regra de cava/jogada válida; fora do turno; partida encerrada; repetição ou conflito. |
| Consultar resultado e histórico necessário | Identidade autorizada, identificação da partida e parâmetros de consulta documentados. | Resultado, rodadas, pontuação e sequência de eventos necessária para compreender ações aceitas e encerramento. | Sem participação ou permissão; partida indisponível; dados privados filtrados conforme a política aprovada. |

Antes da entrega aos alunos, a documentação deve estar acessível em OpenAPI ou formato equivalente e cobrir todas as operações públicas. Para cada operação, deve registrar:

- finalidade, permissão e pré-condições;
- parâmetros, cabeçalhos, corpo e regras de validação;
- resposta de sucesso e todos os erros públicos relevantes;
- estrutura e significado de cada campo, inclusive nulabilidade, enumerações e datas;
- efeito da operação sobre partida, rodada, turno, pontuação e histórico;
- idempotência ou tratamento de repetição quando aplicável;
- exemplos reproduzíveis de requisição e resposta, sem credenciais reais;
- relação com as operações anterior e seguinte da jornada.

A documentação deve incluir uma sequência ponta a ponta executável por ferramenta HTTP ou código independente: solicitar acesso, autenticar, criar ou entrar, iniciar, consultar, jogar ou passar, acompanhar o estado e consultar resultado/histórico. Deve também explicar como configurar o endereço da API e enviar o token sem colocá-lo na URL.

Blazor e MAUI devem consumir os mesmos contratos documentados. Comportamento necessário ao jogo não pode existir apenas nesses clientes, em acesso direto ao banco ou em conhecimento não publicado.

## Segurança e informações privadas

- Tokens nunca aparecem em URLs, corpos da resposta da operação de solicitação, exemplos com valores reais, logs de aplicação, telemetria ou mensagens de erro.
- Tokens são tratados como segredo durante armazenamento, transporte e diagnóstico.
- A resposta à solicitação de token não permite enumerar e-mails cadastrados por conteúdo público ou diferença funcional intencional.
- A emissão é limitada contra abuso por controles adequados ao risco; os parâmetros serão definidos tecnicamente e validados antes da publicação.
- A API aplica autenticação e autorização no servidor em todas as operações protegidas.
- A mão de um jogador é privada para o próprio participante. O painel de observação mostra somente as pedras já jogadas na mesa e a quantidade de pedras de cada jogador.
- Histórico, estado e resultado respeitam a mesma autorização por participação e a política de observação aprovada.
- Dados de autenticação e identidade não são inferidos de valores fornecidos pelo cliente sem validação do servidor.

Esta especificação não escolhe fornecedor de e-mail, formato de token, JWT, SMTP ou serviço de identidade.

Decisão operacional posterior (2026-09-24): o envio de e-mail da API utiliza SendGrid Mail Send.

## Concorrência e consistência

O servidor é a autoridade sobre o estado. A implementação deve impedir que duas requisições válidas para o mesmo estado apliquem ações incompatíveis, que uma repetição pontue duas vezes ou que o turno avance incorretamente. A resposta deve permitir ao cliente reconhecer se a ação foi aplicada ou rejeitada e obter o estado autoritativo seguinte.

A ordem das ações simultâneas é definida pelo timestamp de recebimento no servidor: a ação mais antiga para o estado vence. A estratégia de versão do estado e o formato da resposta de conflito devem ser documentados e testados antes da entrega. Desconexão, fechamento do aplicativo e abandono não liberam automaticamente jogador-perfil nesta etapa; a liberação exige a chave de ativação atual; outro cliente pode ativar o perfil com uma nova chave após a liberação explícita, conforme a decisão T-33.

## Critérios de aceitação

Todos os critérios abaixo exigem evidência de implementação e validação. Sua presença nesta especificação não constitui aceite antecipado.

| ID | Cenário e resultado observável |
| --- | --- |
| AC-ID-01 | Dado um e-mail FIAP elegível, cadastrado ou não, a solicitação cria a conta se ausente, não devolve token na resposta e entrega o token válido exclusivamente ao endereço solicitado. |
| AC-ID-02 | Solicitações processáveis para e-mail FIAP e para endereço fora do domínio produzem confirmação pública uniforme; endereço fora do domínio não recebe token. |
| AC-ID-03 | Token ausente, inválido ou revogado não autoriza operação protegida; token revogado não volta a ser aceito. O mesmo token válido autoriza acessos repetidos, inclusive após reabrir o cliente e em dois clientes simultâneos, sem nova emissão e sem expiração automática; após revogação, ambos perdem acesso. |
| AC-ID-04 | Tentativas excessivas de solicitação são limitadas e deixam evidência operacional sem registrar token ou permitir enumeração de e-mails. |
| AC-ID-05 | Varredura de respostas, URLs, logs e telemetria de cenários de emissão e uso não encontra tokens expostos. |
| AC-AUT-01 | Um usuário autenticado não executa ação de jogador nem consulta informação privada quando o jogador-perfil selecionado não lhe pertence, não está ativo ou não participa da partida. |
| AC-AUT-02 | Um participante consulta somente sua própria mão e os dados públicos permitidos; a mão do adversário não é exposta durante a partida. |
| AC-API-01 | Um aluno, usando apenas a documentação publicada e um cliente HTTP independente de Blazor e MAUI, solicita acesso e conclui a jornada documentada sem acessar banco ou código interno. |
| AC-API-02 | O cliente independente consegue criar ou localizar, entrar e iniciar uma partida segundo as permissões e pré-condições documentadas. |
| AC-API-03 | O participante consulta mão própria, tabuleiro, pontuação, turno e situação corrente por contratos documentados e coerentes entre si. |
| AC-API-04 | Uma jogada válida altera uma única vez o estado, o turno e a pontuação conforme as regras; a resposta e a consulta seguinte refletem o mesmo resultado. |
| AC-API-05 | Jogada fora do turno, peça ausente, encaixe inválido e passagem indevida são rejeitados sem alterar o estado válido. |
| AC-API-06 | O fluxo documentado representa compra única da cava, passagem, mão vazia, travamento, pontos do adversário por múltiplos de cinco, próxima rodada, vencedor e fim de partida. |
| AC-API-07 | Resultado e histórico necessários à jornada podem ser consultados e correspondem às ações aceitas pelo servidor. |
| AC-API-08 | Para ações concorrentes, a API aceita a ação com timestamp de recebimento mais antigo e rejeita as posteriores sem duplicar jogada, pontuação ou avanço de turno. |
| AC-DOC-01 | A documentação acessível descreve toda operação pública com entrada, saída, erros, permissões, exemplos reproduzíveis e sequência ponta a ponta. |
| AC-DOC-02 | Todos os estados e códigos públicos usados pela implementação constam na documentação, e os exemplos passam contra um ambiente de validação sem correções manuais ocultas. |
| AC-DOC-03 | Blazor, MAUI e um terceiro cliente de teste usam os mesmos contratos públicos; nenhum depende de banco, biblioteca interna ou regra não documentada. |
| AC-WEB-01 | O painel distingue carregamento, ausência de partidas, falha de atualização, estado desatualizado e partida encerrada, mostrando somente pedras jogadas e quantidade de pedras de cada jogador. |
| AC-MAUI-01 | O exemplo MAUI para Windows reproduz a jornada definida e apresenta sucessos, rejeições e indisponibilidade sem informar sucesso indevido. |

## Decisões confirmadas e dependências técnicas

### Fatos e decisões de produto

- A API completa e sua documentação são responsabilidade da equipe do projeto.
- Os alunos desenvolvem clientes consumidores e não completam Core, regras, endpoints ou contratos nesta atividade.
- A operação pública de solicitação de token cadastra automaticamente e-mails FIAP ausentes.
- O acesso é solicitado por operação pública específica e entregue por e-mail somente ao endereço FIAP informado.
- A solicitação processável recebe confirmação genérica e nunca devolve o token no corpo.
- A autorização de ações de jogo depende da participação na partida.
- OpenAPI ou documentação equivalente acessível, exemplos reproduzíveis e uma jornada completa são parte da entrega.

### Encaminhamentos à liderança técnica

Cabe à liderança técnica escolher e documentar, sem alterar as decisões de produto:

- nomes e organização final das rotas, códigos HTTP, esquemas e recursos;
- formato e gestão segura dos tokens;
- fornecedor e mecanismo de entrega de e-mail;
- controles concretos de limitação de abuso, revogação e observabilidade;
- estratégia de consistência, concorrência e repetição;
- transporte de atualização do painel;
- integração entre Core, Application, persistência e contratos públicos.

Esta especificação não fixa JWT ou SignalR. A decisão posterior de entrega de e-mail pelo SendGrid está registrada na seção de segurança.

## Riscos e questões abertas

As questões abaixo são bloqueadores internos. Devem ser encerradas pela equipe antes da disponibilização da API e não serão apresentadas aos alunos como decisões ou implementações a cargo deles.

| ID | Decisão interna necessária | Responsáveis | Impacto para publicação |
| --- | --- | --- | --- |
| Q-01 | Resolvida: sem dupla nas duas mãos, inicia quem possuir a pedra de maior valor, comparada pela maior face e depois pela menor face, em ordem decrescente. | Responsável pelo produto | Não bloqueia mais a escolha determinística de quem inicia. |
| Q-02 | Resolvida tecnicamente em `sdd/features/partida-individual-plataforma/spec.md`: conflito semântico estável, versão monotônica por partida e idempotência por autor/partida, preservando a prioridade do timestamp do servidor. | Liderança técnica | Testes locais cobrem conflitos, idempotência e última vaga. Concorrência no ambiente SQL Server operacional ainda requer validação. |

## Condição de prontidão e evidências de aceitação

A API somente pode ser disponibilizada aos alunos quando:

1. todas as questões internas Q-01 e Q-02 estiverem decididas e refletidas nas regras e contratos;
2. todas as operações necessárias à jornada estiverem implementadas e documentadas;
3. os critérios de identidade, segurança, autorização, jogo, concorrência e documentação possuírem evidência de validação;
4. a sequência ponta a ponta tiver sido reproduzida por um cliente independente construído apenas com a documentação pública;
5. exemplos, erros e contratos publicados corresponderem ao comportamento observado no ambiente disponibilizado.

As evidências devem registrar comandos ou procedimentos executados, resultados reais, limitações e ambiente. Pipeline verde, existência de código ou aprovação documental isolada não comprovam prontidão funcional.
