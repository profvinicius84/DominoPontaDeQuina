# Dominó Ponta de Quina — Produto

Projeto acadêmico para ensinar regras de domínio, testes e consumo de uma API de jogo.

## Direção confirmada pelo responsável
- API integralmente implementada, validada e documentada antes do consumo pelos alunos a partir de seus próprios clientes.
- Painel Blazor para acompanhar partidas em execução.
- Cliente .NET MAUI de exemplo para demonstrar o consumo da API.

Esses três componentes são escopo futuro; ainda não estão presentes na solução inspecionada.
O README do Core descreve um exercício histórico ou separado de preenchimento de gaps. Na etapa atual, nenhum gap de domínio será delegado aos alunos: a equipe do projeto deve concluí-los e validá-los antes de publicar a API.

## Regras confirmadas

A partida é um embate entre dois jogadores, sem times. Cada jogador inicia a rodada com sete pedras e as quatorze restantes formam a cava. A rodada também é encerrada quando um jogador fica com a mão vazia. A regra de pontuação por soma das pontas externas múltipla de cinco permanece e pontua o jogador que realizou a jogada.

Sem jogada válida, o jogador compra exatamente uma pedra da cava quando ela existir. Se a pedra comprada não permitir jogada, ou se a cava estiver vazia, o jogador passa a vez. Em travamento, os pontos das pedras na mão de um jogador contam para o adversário somente quando o total for múltiplo de cinco; a rodada seguinte reinicia pela maior dupla disponível.

A API determina quem começa pela maior dupla disponível nas mãos, sem expor peças do adversário. Abandono, desconexão e reconexão não exigem tratamento nesta etapa. Para ações concorrentes, vale o timestamp de recebimento no servidor: a ação mais antiga tem prioridade.

O painel Blazor mostra somente pedras jogadas na mesa e a quantidade de pedras de cada jogador. O exemplo MAUI terá suporte a Windows.

## Acesso dos alunos

O professor ou responsável pré-cadastra os e-mails autorizados. A API fornece uma operação pública de solicitação de token: para qualquer e-mail em formato válido, ela responde de maneira genérica, sem confirmar cadastro nem retornar o token. Quando o e-mail estiver cadastrado e a solicitação for elegível, o token de acesso é enviado somente a esse e-mail. Tokens têm validade limitada, podem ser revogados e não podem aparecer em URL, logs, telemetria ou resposta da solicitação.

Os alunos estudam a documentação completa da API, solicitam acesso e desenvolvem seus próprios clientes. A API documentada deve cobrir todas as operações, erros, permissões e a jornada jogável ponta a ponta antes de ser disponibilizada à turma.

Priorizar contratos compreensíveis, exemplos reproduzíveis e facilidade de execução no ambiente acadêmico. Não assumir requisitos comerciais, instituições isoladas, cobrança ou administração empresarial.
