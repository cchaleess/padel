<#
.SYNOPSIS
    Simula a otros jugadores contra la API local (specs/dev-player-simulation).

.DESCRIPTION
    Usa POST /api/dev/session (solo existe con la API en Development) para obtener sesiones de jugadores
    ficticios y, con ellas, llama a los endpoints reales: nada se escribe directamente en la base de datos.

.EXAMPLE
    .\scripts\dev-sim.ps1 create-match
    .\scripts\dev-sim.ps1 create-match -Player Carla
    .\scripts\dev-sim.ps1 join 3f2b...-... -Count 2
    .\scripts\dev-sim.ps1 hold 3f2b...-... -Player Bruno -Position 2
    .\scripts\dev-sim.ps1 create-match -Player Diego -Competitive -MinLevel 2.0 -MaxLevel 3.0
    .\scripts\dev-sim.ps1 request 3f2b...-... -Player Ana
    .\scripts\dev-sim.ps1 vote 3f2b...-... -Reject
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('create-match', 'join', 'hold', 'request', 'vote')]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$MatchId,

    [string]$Player = 'Ana',

    [ValidateRange(1, 4)]
    [int]$Count = 1,

    # Seat for `hold`: 0–1 pair A (the organizer is 0), 2–3 pair B. Omitted, any free seat.
    [ValidateRange(0, 3)]
    [Nullable[int]]$Position,

    # `create-match`: a competitive match instead of a friendly one. The range defaults to the organizer's level ±0.5.
    [switch]$Competitive,
    [Nullable[decimal]]$MinLevel,
    [Nullable[decimal]]$MaxLevel,

    # `vote`: reject instead of approve. Without -Player, every fictional confirmed player votes.
    [switch]$Reject,

    [string]$ApiBaseUrl = 'http://localhost:5080'
)

$ErrorActionPreference = 'Stop'

# Pool used by `join`; skips any player who already has a seat in the match or can't join it directly.
$JoinPool = @('Bruno', 'Carla', 'Diego', 'Elena', 'Fran', 'Gema', 'Hugo')
$FictionalPlayers = @('Ana') + $JoinPool

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Token, $Body)

    $parameters = @{
        Method      = $Method
        Uri         = "$ApiBaseUrl$Path"
        ContentType = 'application/json'
    }
    if ($Token) { $parameters.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $parameters.Body = ($Body | ConvertTo-Json -Compress) }

    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        $title = $_.Exception.Message
        $raw = $null
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $raw = $_.ErrorDetails.Message }
        elseif ($_.Exception.Response) {
            # Windows PowerShell 5.1 often leaves ErrorDetails empty: read the problem body from the stream.
            try { $raw = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()).ReadToEnd() } catch { }
        }
        if ($raw) {
            try { $title = ($raw | ConvertFrom-Json).title } catch { }
        }
        throw [System.Exception]::new("$Method $Path -> $status $title")
    }
}

function Get-DevSession([string]$Name) {
    try {
        return Invoke-Api -Method Post -Path '/api/dev/session' -Body @{ name = $Name }
    }
    catch {
        throw "No se pudo abrir sesión como '$Name'. ¿Está la API arrancada en Development en $ApiBaseUrl? ($($_.Exception.Message))"
    }
}

function Get-DevToken([string]$Name) { (Get-DevSession $Name).sessionToken }

function Join-Match([string]$Id, [string]$Name) {
    $token = Get-DevToken $Name
    $hold = Invoke-Api -Method Post -Path "/api/matches/$Id/hold" -Token $token
    Invoke-Api -Method Post -Path "/api/matches/$Id/confirm" -Token $token | Out-Null
    return $hold
}

function Require-MatchId {
    if (-not $MatchId) { throw "El comando '$Command' necesita el id del partido: dev-sim.ps1 $Command <matchId>" }
}

try {
    switch ($Command) {
        'create-match' {
            $session = Get-DevSession $Player
            $token = $session.sessionToken
            $now = [DateTimeOffset]::UtcNow.AddMinutes(1)

            $slot = $null
            $club = $null
            foreach ($candidate in (Invoke-Api -Method Get -Path '/api/clubs/nearby' -Token $token)) {
                $slots = Invoke-Api -Method Get -Path "/api/clubs/$($candidate.id)/slots" -Token $token
                $slot = $slots | Where-Object { [DateTimeOffset]::Parse($_.startsAt) -gt $now } |
                    Sort-Object { [DateTimeOffset]::Parse($_.startsAt) } | Select-Object -First 1
                if ($slot) { $club = $candidate; break }
            }
            if (-not $slot) { throw 'No hay ningún hueco libre en ningún club. ¿Seed caducado?' }

            $body = @{ courtSlotId = $slot.id; type = 'Friendly'; note = "Partido simulado de $Player" }
            if ($Competitive) {
                $level = [decimal]$session.player.level
                $body.type = 'Competitive'
                $body.minLevel = if ($null -ne $MinLevel) { $MinLevel } else { [Math]::Max(1.0, $level - 0.5) }
                $body.maxLevel = if ($null -ne $MaxLevel) { $MaxLevel } else { [Math]::Min(5.0, $level + 0.5) }
            }
            $match = Invoke-Api -Method Post -Path '/api/matches' -Token $token -Body $body
            # The organizer's seat is born Held; paying it is what makes the match visible in others' feeds.
            Invoke-Api -Method Post -Path "/api/matches/$($match.id)/confirm" -Token $token | Out-Null

            $local = [DateTimeOffset]::Parse($slot.startsAt).ToLocalTime()
            Write-Output "Partido creado y pagado por $Player"
            Write-Output "  id:      $($match.id)"
            Write-Output "  club:    $($club.name) · $($slot.courtName)"
            Write-Output "  horario: $($local.ToString('ddd dd/MM HH:mm'))"
            if ($Competitive) { Write-Output "  tipo:    competitivo $($body.minLevel)–$($body.maxLevel) (nivel de $Player`: $($session.player.level))" }
            Write-Output "  plazas:  1/4"
        }

        'join' {
            Require-MatchId
            $joined = 0
            foreach ($name in $JoinPool) {
                if ($joined -ge $Count) { break }
                try {
                    Join-Match $MatchId $name | Out-Null
                    $joined++
                    Write-Output "$name se ha unido y ha pagado"
                }
                catch {
                    $message = $_.Exception.Message
                    if ($message -match 'hold -> 403') {
                    Write-Output "$name no cumple los criterios del partido; se salta"
                    continue
                }
                # 409 on hold: this player already has a seat, or the match has none left.
                    if ($message -match 'hold -> 409') {
                        $detail = (Invoke-Api -Method Get -Path "/api/matches/$MatchId" -Token (Get-DevToken $name))
                        if ($detail.status -eq 'Full' -or $detail.mySeat -eq $null) {
                            Write-Output "No quedan plazas libres en el partido."
                            break
                        }
                        continue
                    }
                    throw
                }
            }
            $final = Invoke-Api -Method Get -Path "/api/matches/$MatchId" -Token (Get-DevToken $JoinPool[0])
            Write-Output "Plazas confirmadas: $($final.confirmedSeats)/4 (estado: $($final.status))"
        }

        'request' {
            Require-MatchId
            Invoke-Api -Method Post -Path "/api/matches/$MatchId/exception-requests" -Token (Get-DevToken $Player) | Out-Null
            Write-Output "$Player ha pedido acceso al partido"
        }

        'vote' {
            Require-MatchId
            $action = if ($Reject) { 'reject' } else { 'approve' }
            $any = (Invoke-Api -Method Get -Path "/api/matches/$MatchId" -Token (Get-DevToken $FictionalPlayers[0]))
            $voters = if ($PSBoundParameters.ContainsKey('Player')) { @($Player) } else {
                @($any.confirmedPlayers | ForEach-Object { $_.displayName } | Where-Object { $FictionalPlayers -contains $_ })
            }
            if ($voters.Count -eq 0) { throw 'No hay jugadores ficticios confirmados en el partido que puedan votar.' }

            foreach ($voter in $voters) {
                $token = Get-DevToken $voter
                # pendingRequests is only filled for confirmed players, so each voter reads it with their own session.
                $detail = Invoke-Api -Method Get -Path "/api/matches/$MatchId" -Token $token
                foreach ($request in $detail.pendingRequests) {
                    $result = Invoke-Api -Method Post -Path "/api/matches/$MatchId/exception-requests/$($request.requester.playerId)/$action" -Token $token
                    Write-Output "$voter vota $action a $($request.requester.displayName) -> $($result.status)"
                }
            }
        }

        'hold' {
            Require-MatchId
            $token = Get-DevToken $Player
            $body = if ($null -ne $Position) { @{ position = $Position } } else { $null }
            $hold = Invoke-Api -Method Post -Path "/api/matches/$MatchId/hold" -Token $token -Body $body
            $until = [DateTimeOffset]::Parse($hold.heldUntilUtc).ToLocalTime()
            Write-Output "$Player retiene una plaza sin pagar hasta las $($until.ToString('HH:mm:ss'))"
        }
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
