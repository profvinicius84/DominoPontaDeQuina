# Painel de acompanhamento

```powershell
$env:DominoApi__BaseUrl = 'http://localhost:5180'
dotnet run --project DominoPontaDeQuina.Blazor --urls http://localhost:5194
```

Abra `http://localhost:5194`, informe um token válido e o identificador compartilhado
da partida. O painel usa exclusivamente `GET /api/partidas/{id}/visualizar` com
`Authorization: Bearer`, através do cliente HTTP independente. O servidor define
a permissão de observação. O DTO do painel não contém mãos privadas.

Decisão técnica: polling HTTP de cinco segundos, sem requisições sobrepostas,
interrompido ao sair, encerrar a partida ou destruir o componente. Falhas transitórias
mantêm a última visão acompanhada do aviso de desatualização. Respostas 401/403/404
limpam a visão. A tela distingue ausência, carregamento, erro e encerramento.

O token fica somente na memória do circuito Blazor, nunca em URL ou armazenamento
do navegador. Configure HTTPS tanto para o painel quanto para a API ao disponibilizar
fora de localhost. `DominoApi:BaseUrl` exige HTTPS para destinos não locais.

O observador não ativa jogador nem registra webhook. Mantém polling da projeção pública,
descarta respostas com versão inferior à exibida e não contém campos de próxima ação,
pedra obrigatória, mão, assinatura ou segredo. `PainelTests` também cobre uma resposta
antiga e campos privados extras recebidos por engano, que não entram no DTO nem na tela.

Testes: `PainelTests` cobre carregamento, ausência, acesso não autorizado, encerramento, contagens,
privacidade e falha posterior. `ClientTests` cobre cabeçalho, contratos, conflitos e
indisponibilidade. Execute pelo projeto `DominoPontaDeQuina.Tests`.
