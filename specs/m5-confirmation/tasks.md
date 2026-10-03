# Tareas — M5 Confirmation Flow

- [x] Crear `SeatStatus` y `MatchSeat` en `PadelMatch.Domain.Matches`; añadir `Full` a `MatchStatus`.
- [x] Crear `MatchSeatConfiguration` (tabla `MatchSeats`, índice único parcial `(MatchId, HolderId) WHERE HolderId IS NOT NULL`, índice en `MatchId`); registrar `DbSet<MatchSeat>` en `PadelMatchDbContext`.
- [x] Crear `IMatchSeatRepository`/`MatchSeatRepository`: `AddSeatAsync`, `TryHoldAsync`, `TryConfirmAsync`, `TryReleaseAsync`, `CountConfirmedAsync`, `HasActiveSeatAsync` (todas vía `ExecuteUpdateAsync`/consultas atómicas, sin cargar-mutar-guardar).
- [x] Extender `MatchCreationService` para crear las 4 `MatchSeat` `Available` junto con el `Match`.
- [x] Crear excepciones `SeatUnavailableException`, `PlayerAlreadyHasSeatException`, `SeatNotHeldException`.
- [x] Crear `IMatchSeatService`/`MatchSeatService` (`HoldSeatAsync`, `ConfirmSeatAsync`, `ReleaseSeatAsync`) orquestando `HasActiveSeatAsync`→`TryHoldAsync`, `TryConfirmAsync`→`CountConfirmedAsync`→`MarkFullAsync` si llega a 4, `TryReleaseAsync`.
- [x] Añadir `MarkFullAsync` a `IMatchRepository`/`MatchRepository` (`ExecuteUpdateAsync` condicionado a `Status == Open`).
- [x] Añadir `POST /api/matches/{id}/hold`, `/confirm`, `/release` a `MatchEndpoints.cs`.
- [x] Registrar `IMatchSeatRepository`/`IMatchSeatService` en `DependencyInjection.cs`.
- [x] Migración EF (`AddMatchSeats`), aplicada a la BD local.
- [x] Tests de dominio: `MatchSeat.CreateAvailable` (estado inicial).
- [x] Tests de integración (`MatchSeatTests.cs`, 12 casos): 4 plazas `Available` al crear un `Match`; retener plaza `Available`→`Held`; confirmar plaza propia `Held`→`Confirmed`; soltar plaza `Held`→`Available`; retener una segunda plaza del mismo partido falla (409); confirmar/soltar sin plaza `Held` falla (409); las 4 plazas ocupadas bloquean un 5º intento (409); una plaza `Held` caducada puede ser retenida por otro (se añadió un `TimeProvider` controlable en los tests, `MutableTimeProvider`, para no depender de una espera real de 5 minutos); confirmar la 4ª plaza pasa el `Match` a `Full`; un partido `Full` no aparece en el feed; concurrencia real (`Task.WhenAll`, 3 jugadores por la última plaza) solo permite un éxito; autenticación requerida en las 3 rutas. Bug de test encontrado y corregido (no del código de producción): el primer intento de `SeatEndpointsRequireAuthentication` reutilizaba el `Client` ya autenticado como organizador por `CreateFriendlyMatchAsync`; corregido usando un `HttpClient` nuevo sin autenticar.
- [x] `dotnet test` sin fallos (41 dominio + 48 integración). Se observó fragilidad preexistente de la suite completa en paralelo bajo carga (timeouts de conexión a Postgres / el test de concurrencia de M3 también flaqueó de forma intermitente en estas corridas) — no es un bug de esta spec; ver nota en memoria de sesión.
- [x] Actualizar el README: sección de M5 backend.
- [x] Documentar la verificación con `pastiche-rdd`. `pastiche rdd verify m5-confirmation` → Verified 4/4.
