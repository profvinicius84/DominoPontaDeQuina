# Execução — perfis, ativação e ingresso por jogador

Data: 2026-09-23. Incremento implementado e validado localmente; não é o aceite integral das T-33 a T-45.

## Contrato técnico implementado

- O Bearer autentica a conta. `GET/POST /api/jogadores` lista/cria perfis da conta. O nome tem de 1 a 150 caracteres após remoção de espaços externos, sem caracteres de controle.
- O cliente gera 32 bytes criptograficamente aleatórios em hexadecimal e guarda a chave antes de `POST /api/jogadores/{jogadorId}/ativar-cliente`. Não há identificador público adicional de cliente.
- O servidor armazena SHA-256 da chave e o instante da ativação na linha do jogador. Um `UPDATE` condicionado à ausência de ativação garante exclusividade entre processos/hosts. Repetir a mesma chave confirma a ativação; outra chave recebe `409 JOGADOR_JA_ATIVO`.
- Operações de jogador enviam `jogadorId` e `X-Jogador-Ativacao`, além do Bearer. A API sempre constrói o contexto explícito de jogador; a Application verifica conta proprietária, perfil e hash. Ausência de chave ou chave de outro perfil não permite agir/ler mão privada. Compatibilidade de casos de uso internos antigos não é exposta pelo host HTTP.
- Criação e ingresso recebem perfil e webhook HTTPS, sem objeto de participação. A API atribui posição e persiste `WebhookUrl`. Idempotência de ingresso também rejeita alteração do webhook usando a mesma chave.
- `GET /api/partidas` devolve partidas aguardando adversário com uma participação. Disputa pela vaga é protegida pela versão e pelo índice de posição; um erro de persistência só vira conflito quando há evidência de outra versão confirmada.
- O mesmo usuário pode usar dois perfis em clientes diferentes na mesma partida; cada consulta privada é projetada pelo jogador, mantendo mão/turno separados.

A chave de ativação é uma credencial local, não um objeto de participação ou um `instanciaClienteId`. O protocolo comprova posse desse segredo; copiar deliberadamente a chave entre dispositivos equivale a compartilhar a ativação e não permite distinguir fisicamente essas cópias. O MAUI guarda a chave em `SecureStorage` por API/conta/perfil e usa bloqueio de arquivo vazio para impedir duas janelas locais de reutilizá-la ao mesmo tempo.

## Evidência executada

| Comando/cenário | Resultado |
| --- | --- |
| `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` | 125 aprovados, zero falhas, zero ignorados. |
| `dotnet build DominoPontaDeQuina.Maui/DominoPontaDeQuina.Maui.csproj --no-restore` | Sucesso, zero avisos e erros; não equivale a teste visual ou de armazenamento seguro em execução. |
| `dotnet build DominoPontaDeQuina.Migrations --no-restore` | Sucesso, zero avisos e erros. |
| `dotnet ef migrations has-pending-model-changes --no-build --project DominoPontaDeQuina.Migrations` | Nenhuma divergência entre modelo e snapshot. |
| Atualização do SQL Server descartável `DominoPontaDeQuina_T34_20260923_131940` | Aplicada `20260923164646_PerfisJogadorAtivacaoWebhook` sobre a migration de tokens; `Done.`. Nenhum banco principal alterado. |
| `git diff --check` | Sem erros de whitespace; avisos de normalização LF/CRLF nos arquivos existentes. |

`PerfisJogadorHttpTests` cobre dois perfis do mesmo Bearer em uma partida com mãos separadas; cadastro/listagem; tentativa de usar perfil alheio; chave de outro perfil/ausente; ativação concorrente com um vencedor; repetição com a mesma chave; ingresso repetido; callback divergente; disputa pela última vaga; criação de outra partida pelo perdedor; consulta pública sem mão, chave ou webhook; registro dos dois destinos; upgrade SQLite preservando conta, perfil e partida.

`ClientTests` verifica envio do perfil e do cabeçalho de ativação sem segredo na URL ou corpo de jogada. A suíte HTTP existente foi adaptada aos contratos explícitos; Swagger documenta 16 operações.

## Persistência e clientes

Migration acrescenta colunas opcionais `Jogadores.AtivacaoHash`, `Jogadores.AtivadoEmUtc` e `ParticipacoesPartida.WebhookUrl`, preservando jogadores e participações existentes. `UsuarioId` usa explicitamente a coluna antes mapeada como shadow property, sem recriar a relação. Bases SQLite antigas usam o script versionado de upgrade, uma vez, com API parada e backup; ele foi validado em banco descartável, não aplicado em dados de uso.

O cliente HTTP e a tela MAUI permitem listar/criar/ativar perfil, listar partidas, criar/ingressar e consultar/jogar com perfil explícito. O painel Blazor continua usando somente a observação por Bearer.

## Trabalho restante

- Liberação/troca de ativação para outra chave e critérios de participação incompatível aguardam política. A pergunta sobre liberação explícita fora de partida foi enviada; nenhuma resposta foi presumida. Não há timeout, tomada de perfil ou liberação automática. A retomada usa a mesma chave.
- Autenticação direta e solicitação somente por e-mail permanecem em T-33/T-40.
- Outbox, assinatura, defesa SSRF no envio, tentativas e receptor do webhook não foram implementados. O endereço é somente registrado; nenhum callback é disparado por este incremento. A validação sintática HTTPS não constitui uma defesa de envio contra SSRF.
- A indicação e obrigação da pedra inicial e a projeção da próxima ação permanecem na T-38.
- T-36 ainda precisa da caixa de saída e da política de participações incompatíveis; T-37 também depende dessa política. A compatibilidade interna dos testes legados deve ser removida após migração dos consumidores internos, sem abrir fallback público.
- T-41/T-43 têm apenas adaptação de perfis/contratos: cliente ainda consulta o estado e não executa o ciclo HTTP + webhook completo. Execução visual/manual MAUI, bloqueio entre janelas e recuperação real do `SecureStorage` não foram verificados.
- T-44/T-45 continuam pendentes de matriz completa e aceite independente. A API não está declarada pronta para publicação aos alunos.
