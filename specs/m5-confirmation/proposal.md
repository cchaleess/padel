# M5 — Confirmation Flow

## Intención y problema

Hasta ahora, crear un `Match` ([m3-match-creation](../m3-match-creation/proposal.md)) y descubrirlo ([m4-discovery](../m4-discovery/proposal.md)) no da a ningún jugador — ni siquiera al organizador — una plaza real en el partido. Esta spec construye esa plaza: el ciclo `Available`→`Held`→`Confirmed` del plan (§11), que convierte "quiero unirme" en una ocupación real, simulando el pago (arquitectura preparada para sustituirlo por uno real más adelante, sin rehacerla).

Backend solamente; las pantallas de mobile son una spec aparte (análoga a M3/M4), que no se empieza hasta que esta cierre.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §11 (estados de plaza, regla de `Held` de 5 minutos, mensaje cuando otro ya la tiene retenida), §22 (transición `Open`→`Full` al llegar a 4/4).
- [m3-match-creation/design.md](../m3-match-creation/design.md): el `Match` que esta spec extiende con plazas; "crear no es unirse" sigue vigente — el organizador no ocupa ninguna plaza por crear el partido.
- [m4-discovery/design.md](../m4-discovery/design.md): el feed que excluirá partidos `Full` una vez ese estado exista de verdad.
- [Confirmación de plaza solo contra pago](../../MVP-PLAN.md) (constitución, principio ya aplicado en M3): ningún atajo (ni para el organizador, ni en tests) salta el flujo `Held`→`Confirmed`.

## Decisión de alcance: las 4 plazas se crean junto con el `Match`

Un partido de pádel tiene siempre 4 plazas. En vez de crearlas de forma perezosa en el primer intento de unirse, `MatchCreationService` ([m3-match-creation](../m3-match-creation/design.md)) se extiende para crear las 4 plazas `Available` en el mismo momento que el `Match`, consistente con "`Open`→`Full` al llegar a 4/4" (el número de plazas nunca cambia, solo su estado). Esto reabre código de M3 — mismo criterio ya aplicado cuando M4 extendió `MatchWithSlotDetails`.

## Decisión de alcance: solo el ciclo de unión, no el abandono tras confirmar

Esta spec implementa `Available`→`Held`→`Confirmed` y la liberación de una plaza `Held` (voluntaria o por expiración de los 5 minutos). **No** implementa que un jugador abandone una plaza ya `Confirmed` — el plan (§23) liga esa salida a la lista de espera (M7), que no existe todavía; añadirla ahora significaría inventar qué pasa con la plaza liberada sin ese modelo.

## Decisión de alcance: se cierra el hueco que dejó `m4-discovery`

`m4-discovery` excluía del feed solo los partidos expirados, documentando explícitamente que "`Full` llega con M5 cuando exista un modelo de participantes". Esa condición ya se cumple: esta spec añade la exclusión de partidos `Full` al feed (`MatchFeedService`/`MatchRepository.FindOpenUpcomingAsync`), cerrando esa decisión diferida en vez de dejarla abierta indefinidamente.

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Plazas al crear | Cada `Match` nuevo nace con 4 plazas `Available`. |
| Retener una plaza | Un jugador autenticado retiene una plaza `Available` del partido; pasa a `Held` con una expiración a 5 minutos. Un jugador no puede retener dos plazas del mismo partido. |
| Plaza ya retenida | Si otro jugador intenta retener/confirmar una plaza ya `Held` (y no expirada) por otro jugador, recibe un error específico ("Esta plaza no está disponible temporalmente"), no uno genérico. |
| Confirmar (pago simulado) | El jugador que tiene una plaza `Held` (no expirada) la confirma; pasa a `Confirmed` de forma inmediata y exitosa (sin pasarela real). |
| Expiración | Una plaza `Held` cuyos 5 minutos ya pasaron se trata como `Available` de nuevo, sin necesidad de un proceso en segundo plano — se resuelve en el momento de la siguiente operación sobre esa plaza. |
| Liberación voluntaria | El jugador que tiene una plaza `Held` puede soltarla antes de que expire; vuelve a `Available` de inmediato. |
| `Open`→`Full` | Al confirmarse la cuarta plaza, el `Match` pasa a `Full` atómicamente, en la misma operación que confirma esa plaza. |
| Concurrencia | Dos jugadores no pueden retener la misma plaza `Available` simultáneamente con éxito ambos; mismo criterio de "vía rápida + backstop de base de datos" que `m3-match-creation` aplicó al `CourtSlot`. |
| Feed sin partidos `Full` | `GET /api/matches/feed` deja de devolver partidos cuyo `Match.Status` es `Full`. |

## Fuera de alcance

- **Abandonar una plaza ya `Confirmed`**: depende de la lista de espera (M7); ver decisión de alcance arriba.
- **Pago real**: sigue simulado; la arquitectura se diseña para no tener que rehacerse (plan §10), pero no se integra ninguna pasarela.
- **Mostrar el contador de confirmados o el detalle de plazas en ninguna pantalla**: esta spec es solo backend; qué se expone y cómo se muestra es de la spec de mobile correspondiente.
- **Reglas de calidad para unirse a un competitivo** (acceso directo / solicitud excepcional / unanimidad): M6.
- **Lista de espera cuando una plaza se libera**: M7.
- **Regla de liberación de pista a 3 horas del inicio sin reserva confirmada** (plan §10): pertenece a un ciclo de vida distinto (el de la reserva de pista, no el de la plaza individual); se evalúa por separado si/cuando se construya.

## Criterios de aceptación

1. Un `Match` recién creado tiene 4 plazas `Available`, incluso para el organizador (que no ocupa ninguna por crear el partido).
2. Un jugador puede retener una plaza `Available`; queda `Held` con expiración a 5 minutos.
3. Un jugador no puede retener una segunda plaza del mismo partido mientras ya tiene una `Held` o `Confirmed` en ese partido.
4. Intentar retener o confirmar una plaza ya `Held` por otro jugador (y no expirada) devuelve un error específico, no genérico.
5. Confirmar una plaza `Held` propia y no expirada la deja `Confirmed` de inmediato.
6. Una plaza `Held` cuyos 5 minutos ya pasaron puede ser retenida por otro jugador como si estuviera `Available`.
7. Un jugador puede soltar su propia plaza `Held` antes de que expire, dejándola `Available`.
8. Al confirmarse la cuarta plaza de un partido, `Match.Status` pasa a `Full`.
9. Dos intentos concurrentes de retener la misma plaza `Available` no pueden tener éxito ambos.
10. El feed (`GET /api/matches/feed`) no devuelve partidos `Full`.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: el nombre y la forma exacta de la nueva entidad de dominio (evitando colisión conceptual con `CourtSlot`), los endpoints y su forma (retener/confirmar/soltar), cómo se resuelve la expiración de `Held` sin un proceso en segundo plano, y el mecanismo de concurrencia (comparado con el ya usado para `CourtSlot` en M3).
