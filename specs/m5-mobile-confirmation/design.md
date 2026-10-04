# Diseño — M5 Mobile Confirmation

## Objetivo

Implementar el [proposal](proposal.md): ampliar dos respuestas del backend con el estado de las plazas y construir en `mobile/` la acción "Unirme" y la pantalla de pago simulado.

## Backend

### `confirmedSeats` en `MatchWithSlotDetails`

`MatchWithSlotDetails` gana un campo `int ConfirmedSeats`. Se calcula en `MatchRepository.JoinSlotDetails` con una subconsulta correlacionada dentro de la proyección final:

```csharp
dbContext.MatchSeats.Count(s => s.MatchId == x.match.Id && s.Status == SeatStatus.Confirmed)
```

EF Core lo traduce a un `(SELECT COUNT(*) ...)` dentro del mismo `SELECT`, así que detalle y feed lo obtienen sin una segunda consulta por partido. `JoinSlotDetails` es el único sitio que construye `MatchWithSlotDetails`, de modo que el cambio no se propaga más allá. Solo cuentan las `Confirmed`: las `Held` no se muestran al resto (§11).

### `mySeat` en el detalle

Nuevo método en `IMatchSeatRepository`:

```csharp
/// null si el jugador no tiene plaza activa (nunca la tuvo, la soltó o su Held expiró).
Task<PlayerSeat?> FindActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

public sealed record PlayerSeat(SeatStatus Status, DateTimeOffset? HeldUntilUtc);
```

Mismo criterio de "activa" que `HasActiveSeatAsync` (`Confirmed`, o `Held` con `HeldUntilUtc >= now`). Una `Held` caducada devuelve `null`: coincide con lo que haría el backend si el jugador intentara confirmarla.

`MatchDetailResponse` gana `int ConfirmedSeats` y `MySeatResponse? MySeat` (`record MySeatResponse(SeatStatus Status, DateTimeOffset? HeldUntilUtc)`). `MatchDetailResponse.From` recibe el `PlayerSeat?` como segundo argumento. Los endpoints `GET /api/matches/{id}` y `POST /api/matches` inyectan `IMatchSeatRepository` y `TimeProvider` para resolverlo. En `POST` siempre será `null` (crear no es unirse), pero se resuelve igual para que la respuesta tenga una sola forma.

`MatchFeedItemResponse` gana `int ConfirmedSeats`, tomado de `MatchWithSlotDetails`.

### Plaza del organizador y filtro del feed (añadido 2026-10-04, ver proposal)

- `MatchSeat.CreateHeldBy(matchId, holderId, now)`: nueva factoría de dominio. `MatchCreationService` crea la plaza del organizador con ella y las otras 3 con `CreateAvailable`, todo antes del mismo `SaveChangesAsync`. La duración de 5 minutos pasa a `MatchSeat.HoldDuration`, compartida con `MatchSeatService`.
- La respuesta de `POST /api/matches` lleva ya `mySeat` `Held` con su `heldUntilUtc`; mobile lo usa para navegar al pago sin una llamada extra a `hold`.
- `FindOpenUpcomingAsync` añade `MatchSeats.Any(Confirmed)` sobre la consulta de `Matches`, antes de la proyección (mismo cuidado que el bug de EF de m4-discovery).
- Tests existentes que asumían 4 plazas libres al crear pasan a contar con la del organizador; el helper de `MatchFeedTests` confirma la plaza del organizador para que el partido sea visible.

### Feed solo unibles y «Próximos» (añadido 2026-10-04, ver proposal)

- `FindOpenUpcomingAsync` pasa a `FindJoinableUpcomingAsync(viewerId, now)`: además de lo anterior, excluye `OrganizerId == viewerId` y los partidos con una plaza activa del espectador (`Confirmed`, o `Held` con `HeldUntilUtc >= now`). Una `Held` caducada no excluye: el partido vuelve a ser unible para él.
- `FindUpcomingConfirmedForPlayerAsync(playerId, now)`: partidos `Open` o `Full` futuros con plaza `Confirmed` del jugador, ordenados por hora en memoria (misma limitación de EF que en m4-discovery: no se puede ordenar sobre el record ya proyectado).
- ~~`GET /api/matches/mine`~~: se añadió para «Próximos» y se retiró cuando esos partidos pasaron al feed.
- Mobile: `MatchSummaryRow` (fila extraída de `FeedScreen`). La sección «Próximos» de Perfil y su `ProfileStackNavigator` se retiraron después: Perfil vuelve a ser como antes de esta spec.

### Jugadores confirmados en el detalle (añadido 2026-10-04, ver proposal)

`IMatchSeatRepository.GetConfirmedPlayersAsync(matchId)` une las plazas `Confirmed` con `Players` y devuelve `ConfirmedPlayer(PlayerId, DisplayName, Level)`. `MatchDetailResponse.ConfirmedPlayers` las ordena con el organizador primero y luego por nombre. El nivel es el actual del jugador: es un partido futuro; el snapshot de nivel para el histórico es cosa de M13. En mobile, `MatchDetailScreen` pinta 4 filas (jugador o «Plaza libre») y pasa a `ScrollView` porque el contenido ya no cabe siempre en pantalla.

### Parejas: posición de plaza (añadido 2026-10-04, ver proposal)

- `MatchSeat.Position` (0–3, validada en las factorías); el organizador en la 0. Índice único `(MatchId, Position)`.
- Migración `AddMatchSeatPosition`: añade la columna y numera las plazas existentes por partido antes de crear el índice (organizador primero, luego ocupadas, luego libres).
- `POST /hold` acepta un cuerpo opcional `{ position }`. Con posición, `TryHoldAsync` tiene una única candidata y un solo intento: si está ocupada o pierde la carrera, 409 («no disponible temporalmente»). Sin posición, el comportamiento anterior (cualquier libre). Posición fuera de 0–3: 400.
- `mySeat` y `confirmedPlayers` incluyen `position`; `confirmedPlayers` se ordena por posición (sustituye a "organizador primero").
- Mobile: el detalle muestra dos columnas, «Pareja A» | «Pareja B», con tarjetas de plaza de alto fijo para que las plazas 1ª y 2ª de cada pareja queden alineadas en la misma fila (pedido por el usuario); cada plaza libre es un botón «Unirme» si el jugador no tiene plaza y el partido está `Open`; la propia `Held` se muestra como «Tú · pendiente de pago».

### Feed con partidos propios (añadido 2026-10-04, ver proposal)

`MatchFeed`/`MatchFeedResponse` ganan `Confirmed` y `PendingConfirmation`, delante de `ForYou`/`OutOfRange`. `MatchFeedService` los obtiene con `FindUpcomingConfirmedForPlayerAsync` (el mismo de «Próximos») y los separa por `Match.Status` (`Full` → confirmadas, `Open` → pendientes), en orden de hora. Mobile añade las dos secciones al principio y, como antes, omite las vacías.

### Tests (`PadelMatch.Api.Tests`)

- Detalle de un partido nuevo: `confirmedSeats = 0`, `mySeat = null`.
- Tras `hold`: `mySeat.status = Held` con `heldUntilUtc`; otro jugador ve `confirmedSeats = 0` y `mySeat = null`.
- Tras `confirm`: `confirmedSeats = 1`, `mySeat.status = Confirmed`.
- `Held` caducada (con `MutableTimeProvider`): `mySeat = null`.
- Feed: el item refleja `confirmedSeats` tras una confirmación.

## Mobile

### API (`src/api/`)

- `types.ts`: `MatchStatus = 'Open' | 'Full'`; `SeatStatus = 'Held' | 'Confirmed'` (lo que puede llegar en `mySeat`); `MatchDetail` gana `confirmedSeats` y `mySeat: { status; heldUntilUtc: string | null } | null`; `MatchFeedItem` gana `confirmedSeats`.
- `httpClient.ts`:
  - `holdSeat(id)` → `{ heldUntilUtc }`, `confirmSeat(id)`, `releaseSeat(id)`.
  - `request` hoy hace `response.json()` en todo 2xx que no sea 204. `confirm` y `release` devuelven `200` sin cuerpo (`TypedResults.Ok()`), así que `json()` lanzaría. Se cambia a leer el texto y parsear solo si no está vacío. Es un cambio genérico y no afecta a las llamadas que sí devuelven cuerpo.

### Navegación

Nueva pantalla `SeatPaymentScreen` con parámetros `{ matchId, heldUntilUtc }`. `MatchDetailScreen` se monta en dos stacks (`ClubsStackNavigator` y `PartidosStackNavigator`, decisión de m4-mobile-feed), así que `SeatPayment` se registra en ambos con el mismo nombre de ruta, igual que ya se hizo con `MatchDetail`.

### `MatchDetailScreen`

- Carga con `useFocusEffect` en vez de `useEffect`: al volver de la pantalla de pago, el detalle se recarga solo y refleja el nuevo estado.
- Línea "N/4 confirmados".
- Bloque de acción, por orden de prioridad:
  1. `mySeat.status === 'Confirmed'` → texto "Tienes plaza confirmada".
  2. `mySeat.status === 'Held'` → botón "Continuar pago" que navega a `SeatPayment` con el `heldUntilUtc` existente.
  3. `status === 'Full'` → texto "Partido completo".
  4. En otro caso → botón "Unirme": `holdSeat`, y si va bien navega a `SeatPayment`. Un 409 muestra el mensaje del servidor bajo el botón.
- El organizador ve lo mismo que cualquier jugador: también tiene que pulsar "Unirme" y pagar (constitución).

### `SeatPaymentScreen`

- Cuenta atrás `mm:ss` calculada como `heldUntilUtc - Date.now()` cada segundo. El vencimiento lo fija el servidor y el servidor vuelve a comprobarlo al confirmar; un desfase del reloj del dispositivo solo puede adelantar o retrasar unos segundos lo que se muestra, nunca permitir una confirmación fuera de plazo. Se acepta ese límite para no añadir sincronización de reloj.
- "Pagar (simulado)" → `confirmSeat`, luego `goBack()`. Un 409 (retención expirada) se muestra y desactiva el botón.
- "Cancelar" → navega a la pestaña `Partidos` con `{ segment: 'partidos', requestedAt }` (la acción sube desde cualquier stack hasta el tab navigator; `PartidosTabScreen` fuerza el segmento con ese parámetro) y después `popToTop()` del stack actual. La liberación la hace el listener de abajo, igual que si se sale con el gesto o el botón atrás (que sí vuelven al detalle). Cambio decidido en la verificación manual (2026-10-04).
- Listener `beforeRemove`: si la pantalla se cierra sin haber confirmado, llama a `releaseSeat` sin esperar la respuesta e ignorando errores (si ya expiró, el backend responde 409 y no pasa nada). Cubre "Cancelar", botón atrás de Android y gesto de volver con un solo punto de liberación.
- Al llegar a 0: se desactiva "Pagar" y se muestra "La retención ha expirado". No hace falta llamar al backend: la `Held` caducada ya se trata como libre.
- Si el jugador cierra la app desde esta pantalla, no se dispara `beforeRemove`; la plaza expira sola a los 5 minutos (expiración perezosa del backend) y, si vuelve antes, ve "Continuar pago" en el detalle (criterio 5).

### `CreateMatchScreen`

Tras crear: `replace('MatchDetail')` y, si `mySeat` viene `Held`, `navigate('SeatPayment')` con su `heldUntilUtc`. Así, pagar o cancelar devuelve al detalle del partido recién creado.

### `FeedScreen`

La línea secundaria de la card añade ` · N/4`.

## Alternativas descartadas

- **Endpoint separado para el estado de plazas**: obliga a dos llamadas por detalle. Descartado por el usuario.
- **Pago inline en el detalle**: abandonar es menos claro. Con una pantalla propia, salir de ella es un evento preciso (`beforeRemove`).
- **Liberar la plaza al pasar la app a segundo plano** (`AppState`): soltaría la plaza si el jugador cambia un momento de app para consultar algo, que no es abandonar. La expiración de 5 minutos ya cubre el abandono real.
