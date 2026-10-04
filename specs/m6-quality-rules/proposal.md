# M6 — Quality Rules

## Intención y problema

Hoy cualquier jugador puede retener y pagar una plaza de un partido competitivo, aunque su nivel esté fuera del rango o no cumpla el mínimo de partidos que fijó el organizador. Esta spec aplica las reglas de calidad del plan (§16): quien cumple los criterios entra directamente; quien no los cumple puede pedir acceso, y entra solo si **todos** los jugadores confirmados lo aprueban.

Solo backend, como en M3–M5. La spec de mobile (pestaña `Actividad`: solicitudes que tengo que votar y mis solicitudes) va después.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §16 (calidad del partido, aprobación excepcional), §12 (rango y mínimo de partidos congelados al publicar), §13 (fuera de rango: «Requiere aprobación»), §18 («las excepciones relacionadas con calidad requieren unanimidad del grupo»), §36–37 (`RequestExceptionalJoin`, `ApproveExceptionalJoin`, `POST /api/matches/{id}/exception-requests/{playerId}/approve`).
- [Constitución](../CONSTITUTION.md): principio 3 (el nivel orienta, admite excepciones humanas), principio 4 (las excepciones de calidad requieren unanimidad de los confirmados), reglas críticas protegidas en backend y base de datos, transiciones idempotentes.
- [m5-confirmation](../m5-confirmation/design.md) y [m5-mobile-confirmation](../m5-mobile-confirmation/design.md): el flujo de plaza (`hold` → `confirm`) que esta spec condiciona, sin cambiarlo.

## Decisiones de alcance (confirmadas con el usuario, 2026-10-04)

| Pregunta | Respuesta |
| --- | --- |
| ¿Qué obtiene un jugador al aprobarse su solicitud? | **Permiso para unirse.** Después se une como cualquiera: elige plaza libre, la retiene y paga. Durante la votación no se le reserva nada; si el partido se llena antes, se queda fuera. |
| ¿Quién vota? | **Los confirmados en cada momento.** La solicitud se aprueba cuando todos los jugadores confirmados en ese momento la han aprobado. Si alguien confirma plaza mientras está pendiente, también tiene que votar. |
| ¿Se puede repetir una solicitud rechazada? | **No.** Una solicitud por jugador y partido; el rechazo es definitivo. |
| Alcance | **Backend primero**; mobile en una spec aparte. |

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Criterios | En un competitivo, un jugador cumple los criterios si tiene nivel, su nivel está dentro de `[MinLevel, MaxLevel]` y sus partidos jugados alcanzan `MinMatchesRequired`. Los amistosos no tienen criterios. |
| Acceso directo | Quien cumple los criterios retiene plaza como hasta ahora. |
| Bloqueo | Quien no cumple los criterios y no tiene una solicitud aprobada recibe un error específico al intentar retener plaza, con el motivo. |
| Solicitud | Un jugador que no cumple los criterios puede pedir acceso a un partido `Open` en el que no tiene plaza. Una por jugador y partido. |
| Votación | Cada jugador confirmado aprueba o rechaza. Un rechazo cierra la solicitud como rechazada; cuando aprueban todos los confirmados, queda aprobada. Votar dos veces lo mismo no cambia nada; cambiar el voto no está permitido. |
| Unanimidad bajo concurrencia | Dos votos simultáneos no pueden dejar la solicitud sin resolver ni resolverla dos veces. |
| Detalle | El detalle del partido dice si el jugador puede unirse directamente, por qué no (si no puede) y el estado de su solicitud; a los confirmados les muestra las solicitudes pendientes y su propio voto. |
| Actividad | Un endpoint con las solicitudes que el jugador tiene que votar y las suyas propias, para la futura pestaña `Actividad`. |
| Feed | «Partidos para ti» pasa a ser «puedes unirte directamente» (incluye el mínimo de partidos, no solo el nivel); el resto va a «Otros partidos cercanos». |

## Fuera de alcance

- **Pantallas de mobile**: spec siguiente.
- **Notificaciones** de solicitud pendiente o resuelta (§21): M8 o posterior.
- **Partidos jugados reales**: `MatchesPlayed` vale 0 hasta que existan resultados (M10). Hasta entonces, cualquier competitivo con mínimo de partidos exige solicitud a todo el mundo. Es lo esperado, no un error.
- **Retirar una solicitud** pendiente.
- **Cerrar automáticamente** las solicitudes pendientes de un partido que se completa o empieza: siguen pendientes, pero ya no se muestran en `Actividad` y retener plaza fallará igualmente.

## Criterios de aceptación

1. En un amistoso, cualquier jugador retiene plaza sin solicitud.
2. En un competitivo, un jugador con nivel dentro del rango y partidos suficientes retiene plaza sin solicitud.
3. Un jugador sin nivel, con nivel fuera del rango o sin partidos suficientes recibe un error específico (con el motivo) al retener plaza.
4. Ese jugador puede crear una solicitud; una segunda solicitud al mismo partido falla.
5. Un jugador que puede unirse directamente no puede crear una solicitud.
6. Solo los jugadores confirmados votan; el solicitante y los no confirmados no pueden.
7. Un rechazo deja la solicitud rechazada y el jugador sigue sin poder retener plaza.
8. Cuando todos los confirmados aprueban, la solicitud queda aprobada y el jugador puede retener y pagar plaza.
9. Si un jugador confirma plaza con una solicitud pendiente, la solicitud necesita también su voto.
10. Dos aprobaciones simultáneas de los dos últimos votantes dejan la solicitud aprobada exactamente una vez.
11. El detalle y `Actividad` reflejan criterios, solicitudes y votos.
12. El feed coloca en «Partidos para ti» solo los partidos a los que el jugador puede unirse directamente.
