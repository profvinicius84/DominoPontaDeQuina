# Dominó Ponta de Quina — Regras do jogo

Fonte: `DominoPontaDeQuina.Core/README.md`. São regras esperadas do exercício, não uma declaração de que todas estão implementadas.

- Hierarquia: partida, rodadas e jogadas.
- Partida é um embate entre dois jogadores, sem times.
- Cada jogador recebe sete peças no início da rodada; as quatorze restantes formam a cava.
- Rodada começa com distribuição e termina quando uma mão fica vazia ou quando o tabuleiro trava.
- Cada rodada inicia com quem possui a maior dupla disponível em sua mão; a API determina o início sem revelar as peças do adversário.
- Turnos seguem sentido horário.
- Peça deve ser compatível com a ponta escolhida.
- Soma das pontas externas múltipla de cinco gera pontos para o jogador que realizou a jogada: soma / 5.
- Jogador sem peça compatível compra uma única pedra da cava quando ela existir; se ainda não puder jogar, ou não houver cava, passa a vez.
- No travamento, o valor das pedras restantes na mão de um jogador conta para o adversário somente quando for múltiplo de cinco. A próxima rodada aplica novamente o critério da maior dupla.
- Partida termina quando um jogador alcança ou ultrapassa a pontuação alvo.

## Pontos a esclarecer nas especificações
Critério de início quando nenhum jogador receber uma dupla e formato público da resposta para conflito de concorrência.

Não escolher variantes de dominó por analogia. Confrontar README, implementação e testes, registrando divergências. O padrão 50 de PartidaService é comportamento observado, não pontuação universal obrigatória.
