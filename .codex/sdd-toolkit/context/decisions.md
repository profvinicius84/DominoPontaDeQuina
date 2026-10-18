# Dominó Ponta de Quina — Decisões e pendências

## Direção confirmada
O responsável definiu o propósito acadêmico e os três componentes futuros: API para alunos, painel Blazor para acompanhamento e cliente MAUI de exemplo.

O jogo foi simplificado para um embate entre dois jogadores, sem times. Cada jogador inicia a rodada com sete pedras e as quatorze restantes compõem a cava. Mão vazia encerra a rodada. A soma das pontas externas múltipla de cinco continua gerando `soma / 5` pontos para o jogador que realizou a jogada.

A API é a entrega obrigatória desta etapa: deve estar completamente implementada, documentada e validada antes de chegar aos alunos. Eles consomem os contratos publicados em clientes próprios; não recebem gaps de domínio ou implementação do Core.

O responsável pré-cadastra os e-mails dos alunos. A operação pública de solicitação de token devolve confirmação genérica uniforme e envia o token somente ao e-mail cadastrado elegível. O token não é retornado nessa resposta, possui validade limitada, pode ser revogado e não deve ser exposto em URL, logs ou telemetria.

## Baseline observado
README e projetos documentam .NET 8, organização em camadas, EF Core com Fluent API, migrations separadas e SQL Server LocalDB. As relações são configuradas com shadow properties. Não há ADR formal identificado nesta revisão.

## Decisões futuras
- Detalhes técnicos de token e entrega de e-mail, sem alterar o comportamento de produto já aprovado.
- Integração Core/Application/Domain e tratamento dos modelos homônimos.
- Destino do projeto Repository fora da solução.
- Formato público de resposta para conflito de concorrência e estratégia de versão do estado. A ação com timestamp de recebimento mais antigo tem prioridade.
- Critério de início se nenhum jogador receber uma dupla.

Não há tratamento específico de abandono, desconexão ou reconexão nesta etapa. O painel mostra somente pedras jogadas e quantidade de pedras por jogador. O cliente MAUI tem como plataforma suportada Windows.
- Regras de jogo ainda ambíguas no contexto business-rules.md.

Registrar decisões técnicas relevantes junto à especificação afetada; criar ADR quando a abrangência justificar. Não atribuir aprovações, números de ADR, resultados de testes ou histórico de entregas sem evidência.
