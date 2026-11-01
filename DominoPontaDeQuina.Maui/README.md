# Cliente MAUI para Windows

Requer Windows 10 1809 ou posterior, SDK .NET e workload `maui-windows` compatível
com o destino .NET 8. Foi compilado com SDK 10.0.401/workload instalado e controles
MAUI 8.0.100. O projeto fica em `DominoPontaDeQuina.Windows.slnx` para não impor
Windows/MAUI a quem compila somente servidor e testes.

```powershell
dotnet restore DominoPontaDeQuina.Windows.slnx
dotnet build DominoPontaDeQuina.Windows.slnx --no-restore
dotnet run --project DominoPontaDeQuina.Maui
```

Na interface, o login, a escolha do jogador e a partida aparecem em três telas sequenciais. Durante
a partida, a tela destaca a mesa verde, o placar, o turno e as pedras da sua mão:

1. Na tela de login, informe o e-mail autorizado para solicitar um token ou entre com um token existente. O endereço inicial é o App Service `domino-quina-api-b1-pf2261.azurewebsites.net`; ele pode ser alterado em **Configurar servidor**. HTTPS é obrigatório fora de localhost.
2. Após um login válido, escolha um jogador na segunda tela. **Criar novo jogador** abre o campo de cadastro quando necessário.
3. Na terceira tela, crie uma partida ou escolha uma partida disponível. Depois de entrar, a tela mostra apenas a espera e o início da partida. No segundo cliente, use outro perfil (podendo compartilhar a conta/token).
4. Inicie a partida. Cada cliente recebe somente a própria mão e a próxima ação. A pedra obrigatória de abertura aparece destacada para quem abre a rodada.
5. Toque em uma pedra da mão e escolha **Jogar na esquerda** ou **Jogar na direita**. Sem encaixe, use **Comprar pedra**; o servidor compra uma
   pedra e decide se mantém o turno ou passa. Todas as decisões são da API.
6. O cliente aplica a resposta de cada ação e consulta atualizações a cada dois segundos enquanto a partida está aberta. **Atualizar partida** permite uma consulta manual. Consulte resultado e jogadas ao terminar.
7. **Cancelar partida** está disponível antes do início e durante o jogo. Após a confirmação, a API encerra a partida sem vencedor, preserva o placar e libera os jogadores. Use **Voltar à sala** e **Criar partida** para começar outra.

O endereço da última API e o token são recuperados ao abrir o aplicativo. Se o token salvo ainda for válido, o login é pulado e o aplicativo abre na escolha do jogador. Um token inválido leva de volta à tela de login.
Ao selecionar e ativar o jogador, o aplicativo também consulta a última partida desse perfil e retoma o estado atual, inclusive após fechar e reabrir a janela. O identificador da partida é salvo por endereço da API, conta e jogador. Se a preferência local não existir, **Tenho um código de partida → Retomar minha partida pelo código** consulta a participação existente. **Voltar à sala** depois do encerramento limpa somente a lembrança local da partida.

O cliente não referencia Core, Application, Infrastructure ou banco. Usa o mesmo
`DominoPontaDeQuina.Client` HTTP do painel, com DTOs próprios. Nenhuma regra é
replicada para decidir sucesso. Conflitos exibem rejeição e recuperam o estado
autoritativo; falha de rede nunca é anunciada como sucesso. Uma repetição de
intenção incerta conserva a chave de idempotência. Consulte o estado antes de
alterar uma intenção após falha de comunicação. A criação de partida não possui
idempotência: não repita automaticamente uma criação de resposta incerta.

O token é guardado em `SecureStorage`, separado por endereço da API. A senha é apagada após autenticação bem-sucedida. A chave de ativação do jogador é guardada em `SecureStorage`, separada por endereço da API, conta e perfil, antes da primeira solicitação. Isso permite retomar a ativação no mesmo dispositivo e repetir uma resposta perdida. Um arquivo de bloqueio vazio impede duas janelas deste aplicativo de reutilizar simultaneamente a mesma ativação armazenada. Ele não contém segredos. O bloqueio local é liberado ao sair ou encerrar o processo; a ativação no servidor permanece válida.

**Sair e limpar acesso** apaga o token salvo e limpa a sessão; não revoga o token na API nem transfere o jogador. A chave de ativação permanece no armazenamento seguro. **Liberar jogador** libera a ativação na API e remove a chave local para permitir ativação em outro cliente. Alterar identidade ou endereço descarta o estado local da partida. Não há gravação de segredos em preferências ou logs.

Na consulta de atualizações, a API responde `204` se a versão do estado não mudou e `200` com a visão privada do jogador quando há mudança. Versões antigas não fazem a tela regredir.

Para o aceite manual, abra duas instâncias, use o mesmo token e perfis distintos. Verifique abertura obrigatória, atualização automática do adversário, compra única, rejeição e encerramento. Desconecte temporariamente um cliente para verificar a recuperação na próxima consulta. Tente ativar o mesmo perfil na segunda janela e confirme a recusa. Confira mascaramento dos campos e saída sem dados privados.

Em 2026-09-24, o teste de transporte HTTPS com dois clientes chegou ao encerramento com o fluxo anterior. O aceite visual da consulta periódica em duas instâncias permanece pendente.

A validação automatizada do transporte está em `ClientTests`; o aceite visual e a
jornada manual Windows devem registrar evidências próprias, sem inferir aprovação
somente a partir do build.
