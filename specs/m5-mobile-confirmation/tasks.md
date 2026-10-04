# Tareas — M5 Mobile Confirmation

## Backend

- [x] `MatchWithSlotDetails.ConfirmedSeats` calculado con subconsulta en `JoinSlotDetails`.
- [x] `IMatchSeatRepository.FindActiveSeatAsync` + `PlayerSeat` + implementación.
- [x] `MatchDetailResponse` con `ConfirmedSeats` y `MySeat`; `GET /{id}` y `POST` los resuelven.
- [x] `MatchFeedItemResponse.ConfirmedSeats`.
- [x] Tests de integración (detalle nuevo, tras hold, vista de otro jugador, tras confirm, Held caducada, feed).

## Mobile

- [x] Tipos (`MatchStatus`, `SeatStatus`, `MatchDetail`, `MatchFeedItem`).
- [x] `httpClient`: tolerar 200 sin cuerpo; `holdSeat`/`confirmSeat`/`releaseSeat`.
- [x] `SeatPaymentScreen` (cuenta atrás, Pagar, Cancelar, `beforeRemove` → release).
- [x] Registrar `SeatPayment` en `ClubsStackNavigator` y `PartidosStackNavigator`.
- [x] `MatchDetailScreen`: `useFocusEffect`, contador y bloque de acción.
- [x] `FeedScreen`: `N/4` en la card.

## Plaza del organizador (decisión de la verificación manual)

- [x] `MatchSeat.CreateHeldBy` + `HoldDuration` en dominio; `MatchCreationService` crea la plaza del organizador `Held`.
- [x] Feed filtra partidos sin ningún confirmado.
- [x] Tests adaptados y nuevos (creación con plaza retenida, feed oculta hasta confirmar).
- [x] `CreateMatchScreen` navega al pago tras crear.

## Feed solo unibles y «Próximos» (decisión de la verificación manual)

- [x] `FindJoinableUpcomingAsync` excluye partidos propios y con plaza activa del espectador.
- [x] `FindUpcomingConfirmedForPlayerAsync` (el endpoint `/mine` se retiró al pasar «Próximos» al feed).
- [x] Tests (feed oculta propios y con Held vigente, reaparece al caducar; solo confirmados cuentan como propios).
- [x] Mobile: `MatchSummaryRow`. «Próximos» en Perfil añadido y luego retirado por decisión del usuario.

## Jugadores en el detalle (decisión de la verificación manual)

- [x] `GetConfirmedPlayersAsync` + `ConfirmedPlayers` en el detalle (organizador primero, sin `Held`).
- [x] Test de orden y exclusión de `Held`.
- [x] `MatchDetailScreen`: 4 plazas con nombre/nivel o «Plaza libre»; `ScrollView`.
- [x] «Cancelar» del pago lleva al feed.

## Parejas (decisión de la verificación manual)

- [x] `MatchSeat.Position` + migración con relleno + índice único.
- [x] `hold` con posición opcional (409 si ocupada, 400 si inválida) y tests.
- [x] Detalle por parejas con «Unirme» por plaza libre.

## Feed con partidos propios

- [x] `Confirmed`/`PendingConfirmation` en el feed + test (pendiente → confirmada al llenarse).
- [x] Secciones en `FeedScreen`.

## Solapes de horario

- [x] Decidido no bloquear (pago como freno natural); test que lo fija.

## Verificación

- [x] `dotnet test --filter Match` (suite completa: 44 + 66) y `npm run typecheck` en verde. La suite completa se ejecuta en `pastiche rdd verify` al cerrar.
- [x] Verificación manual en dispositivo (criterios 1–12 del proposal), hecha por el usuario el 2026-10-04.
