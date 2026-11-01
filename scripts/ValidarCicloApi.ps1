param(
    [Parameter(Mandatory)] [string] $BaseUrl,
    [Parameter(Mandatory)] [string] $Token,
    [int] $TargetScore = 50,
    [string] $ResumeGameId
)

$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')
$headers = @{ Authorization = "Bearer $Token" }
$checks = [System.Collections.Generic.List[object]]::new()
$keys = @{}
$players = @{}
$gameId = $null

function Call-Api {
    param([string] $Method, [string] $Path, [object] $Body, [string] $Player)
    $h = $headers.Clone()
    if ($Player) { $h['X-Jogador-Ativacao'] = $keys[$Player] }
    $args = @{ Uri = "$base$Path"; Method = $Method; Headers = $h; SkipHttpErrorCheck = $true; TimeoutSec = 90 }
    if ($null -ne $Body) {
        $args.ContentType = 'application/json'
        $args.Body = ConvertTo-Json $Body -Compress -Depth 10
    }
    $response = Invoke-WebRequest @args
    $value = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
    return @{ Status = [int]$response.StatusCode; Value = $value }
}

function Check {
    param([string] $Step, [hashtable] $Response, [int[]] $Expected)
    $checks.Add([pscustomobject]@{ Step = $Step; Status = $Response.Status; Code = $Response.Value.codigo })
    if ($Response.Status -notin $Expected) {
        throw "$Step retornou $($Response.Status), esperado $($Expected -join ','). Codigo: $($Response.Value.codigo)"
    }
    return $Response.Value
}

function Action-Body {
    param([string] $Player, [long] $Version)
    return @{ jogadorId = $players[$Player]; versaoEsperada = $Version; chaveIdempotencia = [guid]::NewGuid() }
}

function Own-State {
    param([string] $Player)
    $response = Call-Api GET "/api/partidas/$gameId`?jogadorId=$($players[$Player])" $null $Player
    return Check "estado-$Player" $response @(200)
}

try {
    $session = Check 'sessao' (Call-Api GET '/api/identidade/sessao' $null $null) @(200)
    $existing = Check 'listar-perfis' (Call-Api GET '/api/jogadores' $null $null) @(200)
    $original = @($existing | Where-Object { $_.nome -eq 'profvinicius.costa' -and -not $_.ativo }) | Select-Object -First 1
    if (-not $original) { throw 'Perfil original indisponivel ou ja ativo.' }
    $players['A'] = $original.jogadorId
    $new = @($existing | Where-Object { $_.nome -like 'Teste API *' -and -not $_.ativo }) | Select-Object -First 1
    if (-not $new) {
        $new = Check 'criar-perfil-B' (Call-Api POST '/api/jogadores' @{ nome = "Teste API $(Get-Date -Format yyyyMMdd-HHmmss)" } $null) @(200)
    }
    $players['B'] = $new.jogadorId
    foreach ($player in @('A', 'B')) {
        $keys[$player] = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        $null = Check "ativar-$player" (Call-Api POST "/api/jogadores/$($players[$player])/ativar-cliente" @{ chaveAtivacao = $keys[$player] } $null) @(200)
    }
    if ($ResumeGameId) {
        $gameId = $ResumeGameId
        $last = Own-State 'A'
    } else {
        $created = Check 'criar-partida' (Call-Api POST '/api/partidas' @{ pontuacaoAlvo = $TargetScore; jogadorId = $players['A'] } 'A') @(200)
        $gameId = $created.partidaId
        $available = Check 'listar-partidas' (Call-Api GET '/api/partidas' $null $null) @(200)
        if ($gameId -notin @($available | ForEach-Object partidaId)) { throw 'Partida criada ausente na listagem.' }
        $premature = Call-Api POST "/api/partidas/$gameId/iniciar" (Action-Body 'A' $created.versaoEstado) 'A'
        $null = Check 'iniciar-sem-adversario' $premature @(422)
        $joinBody = Action-Body 'B' $created.versaoEstado
        $joined = Check 'ingressar' (Call-Api POST "/api/partidas/$gameId/entrar" $joinBody 'B') @(200)
        $duplicate = Check 'ingressar-idempotente' (Call-Api POST "/api/partidas/$gameId/entrar" $joinBody 'B') @(200)
        if (-not $duplicate.acaoIdempotente) { throw 'Repeticao de ingresso nao foi idempotente.' }
        $started = Check 'iniciar' (Call-Api POST "/api/partidas/$gameId/iniciar" (Action-Body 'A' $joined.estado.versaoEstado) 'A') @(200)
        $null = Check 'observar' (Call-Api GET "/api/partidas/$gameId/visualizar" $null $null) @(200)
        $unchanged = Call-Api GET "/api/partidas/$gameId/atualizacoes?jogadorId=$($players['A'])&versaoConhecida=$($started.estado.versaoEstado)" $null 'A'
        $null = Check 'atualizacao-sem-mudanca' $unchanged @(204)
        $last = $started.estado
    }
    $moves = 0
    $buys = 0
    $passes = 0
    $rounds = 0
    while ([int]$last.situacao -ne 2 -and $rounds -lt 1000) {
        $rounds++
        $turn = if ($last.jogadorDoTurnoId -eq $players['A']) { 'A' } elseif ($last.jogadorDoTurnoId -eq $players['B']) { 'B' } else { throw 'Turno sem jogador conhecido.' }
        $state = Own-State $turn
        $body = Action-Body $turn $state.versaoEstado
        $action = [int]$state.proximaAcao
        if ($action -eq 1) {
            $piece = $state.pedraAberturaObrigatoria
            $body.valorA = $piece.valorA; $body.valorB = $piece.valorB; $body.lado = 0
            $result = Check 'jogar-abertura' (Call-Api POST "/api/partidas/$gameId/jogar" $body $turn) @(200)
            $moves++
        } elseif ($action -eq 2) {
            $left = $state.tabuleiro[0].valorA
            $right = $state.tabuleiro[-1].valorB
            $choice = $null
            foreach ($piece in $state.minhaMao) {
                if ($piece.valorA -eq $left -or $piece.valorB -eq $left) { $choice = @{ Piece = $piece; Side = 0 }; break }
                if ($piece.valorA -eq $right -or $piece.valorB -eq $right) { $choice = @{ Piece = $piece; Side = 1 }; break }
            }
            if (-not $choice) { throw 'API indicou jogada, mas nenhuma pedra da mao encaixa.' }
            $body.valorA = $choice.Piece.valorA; $body.valorB = $choice.Piece.valorB; $body.lado = $choice.Side
            $result = Check 'jogar' (Call-Api POST "/api/partidas/$gameId/jogar" $body $turn) @(200)
            $moves++
        } elseif ($action -eq 3) {
            $result = Check 'comprar' (Call-Api POST "/api/partidas/$gameId/comprar" $body $turn) @(200)
            $buys++
        } elseif ($action -eq 4) {
            $result = Check 'passar' (Call-Api POST "/api/partidas/$gameId/passar" $body $turn) @(200)
            $passes++
        } else { throw "Acao inesperada: $action" }
        $last = $result.estado
        if ($rounds -eq 1 -and -not $ResumeGameId) {
            $stale = Action-Body $turn $state.versaoEstado
            $conflict = Check 'versao-antiga' (Call-Api POST "/api/partidas/$gameId/comprar" $stale $turn) @(409)
            if ($conflict.codigo -ne 'CONFLITO_DE_ESTADO') { throw 'Codigo de conflito inesperado.' }
        }
        if ($rounds % 10 -eq 0) { Write-Output "Progresso: $rounds transicoes, versao $($last.versaoEstado), rodada $($last.numeroRodada)" }
    }
    if ([int]$last.situacao -ne 2) { throw 'Partida nao encerrou em 1000 transicoes.' }
    $resultA = Check 'resultado-A' (Call-Api GET "/api/partidas/$gameId/resultado?jogadorId=$($players['A'])" $null 'A') @(200)
    $resultB = Check 'resultado-B' (Call-Api GET "/api/partidas/$gameId/resultado?jogadorId=$($players['B'])" $null 'B') @(200)
    if ($resultA.vencedorId -ne $resultB.vencedorId -or [int]$resultA.situacao -ne 2) { throw 'Resultados divergentes.' }
    [pscustomobject]@{ Success = $true; UserId = $session.usuarioId; GameId = $gameId; Moves = $moves; Buys = $buys; Passes = $passes; Transitions = $rounds; WinnerId = $resultA.vencedorId; Events = @($resultA.eventos).Count; Checks = @($checks).Count; Statuses = @($checks | Group-Object Status | ForEach-Object { "$($_.Name):$($_.Count)" }) -join ',' } | ConvertTo-Json -Compress
} finally {
    foreach ($player in @('A', 'B')) {
        if ($players[$player] -and $keys[$player]) {
            try { $null = Call-Api DELETE "/api/jogadores/$($players[$player])/desativar-cliente" $null $player } catch { }
        }
    }
}
