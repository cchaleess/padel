# Tareas — M7 Leave a Match

- [x] (Descartado) Lista de espera completa: implementada como `m7-waitlist` y eliminada antes de commitear, por decisión del usuario.
- [x] `MatchSeat.ConfirmedAtUtc` + migración `AddSeatConfirmedAt`.
- [x] `TryLeaveAsync` condicionado a partido `Open`; `MarkFullAsync` condicionado a 4 confirmados.
- [x] `LeaveSeatAsync`: partido cerrado, empezado, traspaso de organizador, reevaluación de solicitudes.
- [x] `POST /api/matches/{id}/leave` (sin devolución ni mención al club: políticas de cancelación pendientes).
- [x] `MatchLeaveTests` (criterios 1–5).
- [x] Suite completa en verde (52 + 87) y sin migraciones pendientes.
- [x] README.
