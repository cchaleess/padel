# Diseño — M7 Leave a Match

## Dominio

`MatchSeat.ConfirmedAtUtc` (nuevo, nullable): se fija al confirmar y se borra al abandonar. Sirve para elegir al «confirmado que lleve más tiempo inscrito» (§18). Las plazas confirmadas antes de la migración `AddSeatConfirmedAt` quedan con `null` y cuentan como las más antiguas, ordenadas por posición.

## `MatchSeatService.LeaveSeatAsync`

1. El partido existe (404) y no ha empezado (409 «El partido ya ha empezado.»).
2. No está cerrado: si es `Full`, 409 `MatchClosedException` («El partido está completo: ya no se puede abandonar.»).
3. `TryLeaveAsync`: `UPDATE MatchSeats SET Status='Available', HolderId=NULL, HeldUntilUtc=NULL, ConfirmedAtUtc=NULL WHERE MatchId=@m AND HolderId=@p AND Status='Confirmed' AND EXISTS (partido Open)`. La condición sobre el partido va en el mismo `UPDATE`. Si afecta 0 filas, se distingue entre «se cerró justo ahora» (`MatchClosedException`) y «no tenías plaza» (409 «No tienes una plaza confirmada en este partido.»).
4. Si el que se va es el organizador: `TransferOrganizerAsync` al confirmado con `ConfirmedAtUtc` más antiguo (`UPDATE ... WHERE OrganizerId = @from`).
5. `ReevaluatePendingRequestsAsync` (M6): con el mismo bloqueo `FOR UPDATE` que un voto, aprueba las solicitudes pendientes cuyos votantes restantes ya han aprobado todos. Si no queda ningún confirmado, no aprueba nada por defecto.

El partido sigue `Open`: nunca se abandona un partido `Full`, así que no hay que reabrir nada. La plaza liberada vuelve al feed sin más, porque el feed ya muestra los partidos `Open` con al menos un confirmado.

### Carrera entre abandonar y cerrar

`ConfirmSeatAsync` confirma la 4.ª plaza y luego llama a `MarkFullAsync`. Si alguien abandona justo entre las dos operaciones, el partido podría cerrarse con 3 confirmados. Por eso `MarkFullAsync` comprueba en el mismo `UPDATE` que hay `SeatsPerMatch` confirmadas, y `TryLeaveAsync` exige que el partido siga `Open`. Cada sentencia es atómica, así que el resultado es coherente se ejecuten en el orden que se ejecuten.

## API

```text
POST /api/matches/{id}/leave   [confirmado] → 200
```

## Tests (`MatchLeaveTests`)

Criterios 1–5. Los partidos de estos tests empiezan en 30 minutos, para poder adelantar el reloj más allá del inicio sin que caduquen los tokens de sesión.
