# Diseño — M6 Quality Rules

## Objetivo

Implementar el [proposal](proposal.md): criterios de acceso directo en competitivos, solicitud excepcional, votación unánime de los confirmados y su reflejo en detalle, feed y un endpoint de actividad.

## Dominio (`PadelMatch.Domain.Matches`)

### Criterios: `MatchCompatibility`

```csharp
public enum AccessShortfall { NoLevel, LevelBelowRange, LevelAboveRange, NotEnoughMatches }

public static class MatchCompatibility
{
    /// Vacía en amistosos y cuando el jugador cumple todo.
    public static IReadOnlyList<AccessShortfall> GetShortfalls(Match match, decimal? level, int matchesPlayed);
    public static bool CanJoinDirectly(Match match, decimal? level, int matchesPlayed);
}
```

Sustituye a `IsCompatibleWithLevel` (M4), que solo miraba el nivel. Devuelve **todos** los motivos, no solo el primero, para que mobile pueda explicar por qué hace falta aprobación. `MinMatchesRequired` nulo o 0 no exige nada.

### Solicitud y voto

```csharp
public enum AccessRequestStatus { Pending, Approved, Rejected }

public sealed class MatchAccessRequest   // una por (MatchId, PlayerId): índice único
{
    Guid Id; Guid MatchId; Guid PlayerId; AccessRequestStatus Status;
    DateTimeOffset CreatedAtUtc; DateTimeOffset? ResolvedAtUtc;
    static Create(matchId, playerId, now); void Approve(now); void Reject(now);   // solo desde Pending
}

public sealed class MatchAccessVote      // uno por (RequestId, VoterId): índice único
{
    Guid Id; Guid RequestId; Guid VoterId; bool Approve; DateTimeOffset CastAtUtc;
}
```

Nombre «access request» en código; en la API se mantiene el término del plan, `exception-requests` (§37).

## Reglas en `Application`

### Retener plaza (`MatchSeatService.HoldSeatAsync`)

Antes de reclamar la plaza: si el partido es competitivo y `GetShortfalls` no está vacío, el jugador necesita una solicitud `Approved` para ese partido. Si no la tiene: `AccessRequiresApprovalException` → **403** con `title` «Este partido requiere la aprobación de los jugadores confirmados.» y una extensión `shortfalls` con los motivos.

El organizador no pasa por esta comprobación al crear el partido: define él los criterios y su plaza nace `Held` en `MatchCreationService`, que no llama a `HoldSeatAsync`.

### Crear solicitud (`MatchAccessService.RequestAccessAsync`)

`POST /api/matches/{id}/exception-requests` → 201. En orden:

1. El partido existe (404) y está `Open` y sin empezar (409 «Este partido ya no admite solicitudes.»).
2. El jugador no tiene plaza activa en él (409 «Ya tienes una plaza en este partido.»).
3. No cumple los criterios (si los cumple: 409 «Puedes unirte directamente, no necesitas solicitud.»).
4. No tiene ya una solicitud para ese partido (409 «Ya has solicitado acceso a este partido.»). El índice único `(MatchId, PlayerId)` es el respaldo ante dos peticiones simultáneas.

### Votar (`MatchAccessService.VoteAsync`)

`POST /api/matches/{id}/exception-requests/{playerId}/approve` y `/reject` → 200 con el estado resultante de la solicitud.

Todo dentro de una transacción que **bloquea la fila de la solicitud** (`SELECT ... FOR UPDATE`). Así, los votos a una misma solicitud se aplican de uno en uno. Sin ese bloqueo, dos aprobaciones simultáneas de los dos últimos votantes podrían ver cada una solo su propio voto y dejar la solicitud pendiente para siempre.

1. La solicitud existe (404).
2. El votante es un jugador confirmado del partido en este momento (403 «Solo los jugadores confirmados pueden votar.»).
3. Si ya votó: mismo sentido → no hace nada y devuelve el estado (idempotente); sentido distinto → 409 «Ya has votado esta solicitud.».
4. La solicitud sigue `Pending` (si no, 409 «Esta solicitud ya está resuelta.»).
5. Se guarda el voto. Si es un rechazo → `Reject`. Si es una aprobación y **todos los confirmados actuales** tienen voto de aprobación → `Approve`.

Los confirmados solo crecen (abandonar una plaza confirmada es M7), así que una solicitud pendiente no puede quedar aprobada sin que se emita un voto. Una solicitud pendiente nunca pasa a aprobada por la entrada de otro jugador; como mucho, necesita un voto más.

Límite aceptado: si un jugador confirma plaza en el mismo instante en que se emite el último voto que faltaba, la solicitud puede aprobarse sin su voto. `confirm` no toma el bloqueo de la solicitud. Cerrar esa ventana exigiría serializar las confirmaciones con todas las solicitudes del partido, y no compensa para un caso tan improbable.

## Infraestructura

- `MatchAccessRequestConfiguration` / `MatchAccessVoteConfiguration`: estados como texto; FKs a `Matches` (cascade), `Players` (restrict) y `MatchAccessRequests` (cascade); índices únicos `(MatchId, PlayerId)` y `(RequestId, VoterId)`.
- `IMatchAccessRepository` / `MatchAccessRepository`:
  - `FindRequestAsync`, `FindRequestForUpdateAsync` (`FromSql ... FOR UPDATE` + `ToListAsync`, sin componer: PostgreSQL no admite `FOR UPDATE` dentro de la subconsulta que generaría `FirstOrDefault`).
  - `BeginTransactionAsync`, `AddRequestAsync`, `AddVoteAsync`, `GetVotesAsync`, `SaveChangesAsync`. La violación del índice único de solicitudes se traduce a «ya has solicitado».
  - Lecturas para detalle y actividad (ver abajo).
- Migración `AddMatchAccessRequests`.

## API

### Detalle (`GET /api/matches/{id}`)

`MatchDetailResponse` gana:

```text
myAccess: { canJoinDirectly: bool, shortfalls: AccessShortfall[], requestStatus: Pending|Approved|Rejected|null }
pendingRequests: [ { playerId, displayName, level, matchesPlayed, shortfalls, approvals, votersNeeded, myVote: bool|null } ]
```

`pendingRequests` solo se rellena si quien consulta es un jugador confirmado; para el resto va vacío (las solicitudes de otros no son públicas). `votersNeeded` = confirmados actuales.

### Actividad (`GET /api/activity`)

```text
{ toVote: [ { matchId, clubName, startsAt, endsAt, type, requester: { playerId, displayName, level, matchesPlayed } } ],
  myRequests: [ { matchId, clubName, startsAt, endsAt, type, status } ] }
```

- `toVote`: solicitudes `Pending` de partidos `Open` y futuros donde soy confirmado y aún no he votado.
- `myRequests`: mis solicitudes de partidos futuros, en cualquier estado.

### Feed

`MatchFeedService` clasifica con `CanJoinDirectly(match, player.Level, player.MatchesPlayed)` en vez de solo el nivel. Un partido para el que el jugador tiene una solicitud `Approved` también va a «Partidos para ti», porque puede unirse directamente.

## Tests

- Dominio: `GetShortfalls` (amistoso, dentro de rango, por debajo, por encima, sin nivel, mínimo de partidos, varios motivos a la vez); transiciones de `MatchAccessRequest` solo desde `Pending`.
- Integración (`MatchAccessTests`): criterios 1–12 del proposal, incluido el voto concurrente de los dos últimos votantes (`Task.WhenAll`) que debe dejar `Approved`. Los jugadores con nivel se crean vía la encuesta real (`SetLevelAsync`, como en `MatchFeedTests`).
