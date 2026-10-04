# Diseño — M6 Mobile Quality Rules

## Backend: plaza solicitada (añadido 2026-10-04)

`MatchAccessRequest.RequestedPosition` (`int?`, validada 0–3), migración `AddAccessRequestPosition`. `POST .../exception-requests` acepta un cuerpo opcional `{ position }` (400 si no es 0–3). `myAccess.requestedPosition` y `pendingRequests[].requestedPosition` la exponen. Sigue sin reservar nada: la decisión de m6-quality-rules («la aprobación es un permiso») no cambia.

## Backend: expiración al completarse (añadido 2026-10-04)

`AccessRequestStatus.Expired`. `MatchSeatService.ConfirmSeatAsync`, justo después de `MarkFullAsync`, llama a `IMatchAccessRepository.ExpirePendingRequestsAsync`: un `UPDATE ... WHERE Status = 'Pending'` atómico. Si coincide con un voto que tiene la fila bloqueada (`FOR UPDATE`), el `UPDATE` espera a que ese voto termine: si lo aprobó, la solicitud ya no está pendiente y no se toca. Un voto posterior la encuentra resuelta (409). Sin migración: el estado se guarda como texto.

## API (`src/api/`)

- `types.ts`: `AccessShortfall`, `AccessRequestStatus`, `MyAccess`, `PendingAccessRequest`, `AccessRequester`, `Activity` (`toVote`, `myRequests`); `MatchDetail` gana `myAccess` y `pendingRequests`.
- `httpClient.ts`: `requestAccess(matchId)`, `voteAccess(matchId, playerId, approve)` → `{ status }`, `getActivity()`.
- `accessLabels.ts` (en `features/matches/`): textos en español de cada `AccessShortfall` y de cada estado de solicitud, compartidos por el detalle y Actividad.

## `MatchDetailScreen`

Nuevo valor derivado: `canJoin = status === 'Open' && mySeat === null && (myAccess.canJoinDirectly || myAccess.requestStatus === 'Approved')`. Las plazas libres solo muestran «Unirme» si `canJoin`. Antes, `canJoin` no miraba el acceso.

Bajo las parejas, un bloque de acceso cuando no puedo unirme directamente y no tengo plaza:
- Motivos («Tu nivel (4.1) está por encima del rango 2.0–3.0», «Necesitas nivel», «Mínimo N partidos jugados»).
- Sin solicitud → las plazas vacías son botones «Solicitar acceso» (`requestAccess` y recarga), sin botón aparte. Pendiente → las plazas vacías dicen «Solicitud enviada» y el bloque, el texto de espera. Rechazada → texto final. Aprobada → nada: las plazas ya ofrecen «Unirme».

Si soy confirmado y hay `pendingRequests`: sección «Solicitudes de acceso», una tarjeta por solicitud (nombre, nivel, motivos, `approvals/votersNeeded`) con Aprobar/Rechazar si `myVote` es nulo, o «Has aprobado» / «Has rechazado». Tras votar, se recarga el detalle.

El componente de tarjeta de solicitud (`AccessRequestCard`) se comparte con Actividad.

También se quita el «(tu nivel al crear: X)» de la sección Nivel, heredado de m3-mobile-matches: se escribió pensando solo en el organizador y, visto por otro jugador, parece su propio nivel (corrección del usuario, 2026-10-04). El snapshot `organizerLevelAtCreation` sigue en la API.

## Pestaña Actividad

`ActivityStackNavigator` (`Activity` → `MatchDetail` → `SeatPayment`, mismos nombres de ruta que los otros stacks) sustituye al `ComingSoonScreen` de `AppTabs`. `ActivityScreen`:

- Carga `GET /api/activity` con `useFocusEffect` y pull-to-refresh, como el feed.
- `SectionList` con «Solicitudes que tienes que votar» (`AccessRequestCard` con club y horario del partido y Aprobar/Rechazar; tocar el partido abre su detalle) y «Tus solicitudes» (club, horario, estado; abre el detalle). Las secciones vacías no se muestran, y si las dos lo están: «No tienes solicitudes pendientes».

## Contador de Actividad

`PendingVotesContext` (en `features/activity/`) guarda `count` = `toVote.length` de `GET /api/activity`. Se refresca al montar las pestañas, cuando la app vuelve a primer plano (`AppState`), al enfocar cualquier pestaña (`screenListeners.focus` en `AppTabs`) y después de votar desde el detalle. `ActivityScreen` informa del número que ya ha cargado (`report`) para no repetir la petición. `AppTabs` lo pinta como `tabBarBadge` de Actividad.

## Feed

`FeedScreen`: el título de `outOfRange` pasa de «Otros partidos cercanos» a «Requieren aprobación».

## `dev-sim.ps1`

- `create-match -Competitive [-MinLevel 2.0] [-MaxLevel 3.0]`: crea un competitivo. El organizador ficticio necesita estar dentro de su propio rango para que tenga sentido, pero el backend no lo exige.
- `request <matchId> -Player Ana`: el ficticio pide acceso.
- `vote <matchId> [-Reject] [-Player Bruno]`: el ficticio indicado, o todos los ficticios confirmados en el partido si se omite, vota todas las solicitudes pendientes. Los confirmados se leen de `confirmedPlayers` del detalle y se cruzan con la lista de nombres ficticios.
