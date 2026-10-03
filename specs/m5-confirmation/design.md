# Diseño — M5 Confirmation Flow

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): una nueva entidad `MatchSeat` (4 por partido, creadas junto con el `Match`) con el ciclo `Available`→`Held`→`Confirmed`, tres acciones nuevas (retener/confirmar/soltar) y la transición `Open`→`Full` al confirmarse la cuarta.

## Por qué `MatchSeat`, no "plaza" ni "MatchSlot"

El dominio ya tiene `CourtSlot` (el hueco de pista/horario). Llamar "plaza" a la nueva entidad en inglés sería `MatchSlot`, demasiado parecido a `CourtSlot` para ser seguro en código (mismo riesgo que `MatchType` vs `System.IO.MatchType` en M3, pero esta vez semántico, no de compilador). `MatchSeat` es inequívoco: una de las cuatro plazas de un partido.

## Dominio (`PadelMatch.Domain.Matches`)

```csharp
public enum SeatStatus { Available, Held, Confirmed }

public sealed class MatchSeat
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public SeatStatus Status { get; private set; }
    public Guid? HolderId { get; private set; }
    public DateTimeOffset? HeldUntilUtc { get; private set; }

    public static MatchSeat CreateAvailable(Guid matchId) => new()
    {
        Id = Guid.NewGuid(), MatchId = matchId, Status = SeatStatus.Available
    };
}
```

Sin métodos de mutación (`Hold`/`Confirm`/`Release`) en la entidad: a diferencia de `CourtSlot.Book()` (una transición sencilla sobre una entidad ya cargada), las tres operaciones de esta spec son **reclamaciones atómicas condicionadas** ("solo si sigue en el estado que yo espero"), y EF Core ya tiene el mecanismo correcto para eso — ver siguiente sección. Cargar la entidad, mutarla en memoria y guardar reintroduciría la misma ventana de carrera que se busca evitar.

`MatchStatus` (ya existente) gana un valor: `{ Open, Full }`.

## `MatchCreationService` crea las 4 plazas (reabre M3)

```csharp
// tras AddMatchAsync(match, ...), antes de SaveChangesAsync:
foreach (var _ in Enumerable.Range(0, 4))
{
    await matchRepository.AddSeatAsync(MatchSeat.CreateAvailable(match.Id), cancellationToken);
}
```

Misma transacción que crea el `Match` y reserva el `CourtSlot`: si cualquiera de las tres falla, no se persiste nada (comportamiento ya garantizado por `SaveChangesAsync` siendo la única escritura real).

## Las tres operaciones: `ExecuteUpdateAsync` como reclamación atómica

Cada operación es un único `UPDATE ... WHERE ...` que solo tiene efecto si la fila sigue en el estado esperado — el `WHERE` es la comprobación de concurrencia, no una comprobación previa en memoria. Si el `UPDATE` afecta 0 filas, la operación falló por una condición de carrera (o porque nunca se cumplía) y se traduce en un error de negocio. Esto es distinto del patrón de `CourtSlot` en M3 (ahí el backstop era un índice único sobre un `INSERT` nuevo, porque "reservar" un hueco significa crear un `Match`); aquí no hay ningún `INSERT`, así que el backstop natural es la propia cláusula `WHERE` del `UPDATE`, atómica por definición en PostgreSQL.

```csharp
public interface IMatchSeatRepository
{
    Task AddSeatAsync(MatchSeat seat, CancellationToken cancellationToken); // usado solo al crear el Match

    /// <returns>true si la reclamación tuvo éxito.</returns>
    Task<bool> TryHoldAsync(Guid matchId, Guid playerId, DateTimeOffset heldUntilUtc, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> TryConfirmAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> TryReleaseAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    Task<int> CountConfirmedAsync(Guid matchId, CancellationToken cancellationToken);
    Task<bool> HasActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);
}
```

- **`HasActiveSeatAsync`**: vía rápida (criterio de aceptación 3) — ¿el jugador ya tiene una plaza `Held` (no expirada) o `Confirmed` en este partido? Comprobación en memoria antes de intentar reclamar; el backstop real es el índice único de abajo.
- **`TryHoldAsync`**: elige **una** plaza candidata (`Available`, o `Held` con `HeldUntilUtc < now`) y ejecuta:
  ```sql
  UPDATE "MatchSeats" SET "Status"='Held', "HolderId"=@playerId, "HeldUntilUtc"=@heldUntilUtc
  WHERE "Id"=@candidateId AND ("Status"='Available' OR ("Status"='Held' AND "HeldUntilUtc" < @now))
  ```
  Así se resuelve la expiración de `Held` **sin ningún proceso en segundo plano** (proposal.md): una plaza "retenida pero caducada" se trata como disponible en el momento exacto en que alguien intenta reclamarla, nunca antes. Si el `UPDATE` afecta 0 filas (otro jugador la reclamó primero), se reintenta una vez sobre la siguiente candidata; si no queda ninguna, la operación falla.
- **`TryConfirmAsync`** / **`TryReleaseAsync`**: mismo patrón, acotado por `"MatchId"=@matchId AND "HolderId"=@playerId AND "Status"='Held'` (más `"HeldUntilUtc" >= @now` para confirmar). Solo el jugador que tiene la plaza puede confirmarla o soltarla.

## Backstop de concurrencia: índice único parcial

```csharp
// MatchSeatConfiguration.cs
builder.HasIndex(s => new { s.MatchId, s.HolderId })
    .IsUnique()
    .HasFilter("\"HolderId\" IS NOT NULL");
```

Impide que un mismo jugador termine con dos plazas `Held`/`Confirmed` en el mismo partido aunque dos solicitudes de ese jugador lleguen en paralelo y ambas pasen `HasActiveSeatAsync` antes de que ninguna se complete (la misma clase de carrera que el índice único de `Matches.CourtSlotId` resuelve en M3, aplicada aquí al par `(MatchId, HolderId)` en vez de a `CourtSlotId`). Una violación se traduce a la misma excepción de negocio que agotar las plazas candidatas (criterio de aceptación 3).

## `Open`→`Full`

Tras un `TryConfirmAsync` exitoso:

```csharp
if (await seatRepository.CountConfirmedAsync(matchId, cancellationToken) == 4)
{
    await matchRepository.MarkFullAsync(matchId, cancellationToken); // UPDATE ... WHERE Status = 'Open', idempotente
}
```

`MarkFullAsync` es también un `ExecuteUpdateAsync` condicionado (`WHERE "Status"='Open'`), así que ejecutarlo más de una vez (en el caso improbable de que el recuento coincida en dos confirmaciones casi simultáneas, cosa que no debería pasar porque solo una puede ser "la cuarta") no tiene efecto adicional.

## Excepciones de negocio (`PadelMatch.Application.Matches`)

- `SeatUnavailableException` (409, "Esta plaza no está disponible temporalmente.") — agotadas las candidatas en `TryHoldAsync`, o violación del índice único.
- `PlayerAlreadyHasSeatException` (409) — `HasActiveSeatAsync` ya era cierto antes de intentar.
- `SeatNotHeldException` (409, "No tienes ninguna plaza retenida en este partido.") — `TryConfirmAsync`/`TryReleaseAsync` devuelven 0 filas afectadas (no la tenía, no era suya, o ya expiró).

## Endpoints

```text
POST /api/matches/{id}/hold     [autenticado] → 200 { heldUntilUtc }
POST /api/matches/{id}/confirm  [autenticado] → 200
POST /api/matches/{id}/release  [autenticado] → 200
```

Sin cuerpo de petición: el jugador actúa sobre "mi plaza en este partido", nunca sobre una plaza ajena ni elegida por id — la entidad `MatchSeat` ni siquiera se expone por id en la API. `GET /api/matches/{id}` no cambia en esta spec (proposal.md, "fuera de alcance": qué se expone de las plazas es decisión de la spec de mobile correspondiente).

## Cierre del hueco de `m4-discovery`

`IMatchRepository.FindOpenUpcomingAsync` cambia su filtro de `match.Status == MatchStatus.Open` a `match.Status == MatchStatus.Open` **sigue siendo el único valor no terminal** — con solo dos valores (`Open`/`Full`) el filtro ya existente excluye `Full` automáticamente sin tocar código: `Status == MatchStatus.Open` nunca incluyó `Full` ni lo incluirá. No se necesita ningún cambio en `MatchFeedService` ni en el repositorio — la decisión de m4-discovery ya estaba filtrando por `Open` explícitamente, no por "no expirado solamente". Se documenta aquí porque m4-discovery dejó constancia explícita de la duda; la respuesta es que ya estaba resuelta por construcción.

## Migración

Nueva tabla `MatchSeats` (`Id` PK, `MatchId` FK a `Matches` con `OnDelete(Cascade)` — si un `Match` se borra, no debería quedar ninguna plaza huérfana, aunque hoy nada borra un `Match`), `Status`, `HolderId` (FK a `Players`, nullable, `OnDelete(Restrict)`), `HeldUntilUtc` (nullable). Índice único parcial `(MatchId, HolderId) WHERE HolderId IS NOT NULL`, más un índice simple en `MatchId` (consulta de recuento y de "plazas de este partido").

## Alternativas y límites

- **Sin proceso en segundo plano para expirar `Held`**: la expiración es perezosa (se resuelve en el momento en que alguien intenta reclamar la plaza). Una plaza `Held` y caducada que nadie vuelve a tocar permanece así en la base de datos indefinidamente — inofensivo, porque cualquier lectura que importe (`CountConfirmedAsync`, el próximo intento de retenerla) ya trata `Held`+caducada como no confirmada/disponible según corresponda.
- **El organizador no tiene ningún trato especial**: no se le asigna ninguna plaza automáticamente ni se le exime de retener/confirmar como cualquier otro jugador (ya decidido en M3, reafirmado aquí).
- **Sin límite de plazas `Held` simultáneas por jugador entre partidos distintos**: el plan no lo pide; el índice único solo protege "una plaza por partido por jugador", no "una plaza en total".
