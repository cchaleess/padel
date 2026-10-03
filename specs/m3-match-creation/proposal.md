# M3 — Match creation

## Intención y problema

M2 dejó al jugador con algo concreto sobre lo que actuar: un `CourtSlot` disponible de un club. Pero todavía no existe ningún partido en el sistema — `Match` no existe como entidad. M3 introduce la creación de un partido a partir de un `CourtSlot`: el primer jugador que lo hace se convierte en organizador, elige el tipo de partido (Competitive/Friendly) y, si es competitivo, define un rango de nivel y un mínimo de partidos requerido a partir de su propio nivel (congelado como snapshot). Es el prerequisito directo de todo lo que viene después — descubrimiento (M4), confirmación de plaza (M5), calidad y lista de espera (M6/M7) — pero ninguna de esas piezas existe todavía; esta spec se limita a que el partido pueda nacer correctamente y de forma exclusiva sobre su hueco.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §7 (tipos de partido, Competitive/Friendly), §9 (disponibilidad de pista, ya resuelta en M2), §11 (confirmación de plaza — fuera de esta spec, pero fija el invariante "crear no confirma"), §12 (creación de partido competitivo: organizador, snapshot de nivel, rango, mínimo, nota, congelación al publicar), §22 (estados del partido: `Draft`/`Open` son los que alcanza esta spec), §34 (modelo de dominio: `Match`, `MatchType`, `MatchStatus`, y la existencia futura de `MatchParticipant`/`MatchSlot`), §36 (caso de uso `CreateMatchFromCourtSlot`), §37 (`POST /api/matches`), §38 M3.
- [Constitución del proyecto](../CONSTITUTION.md): "un hueco de pista no puede asignarse simultáneamente a dos partidos" (no negociable de concurrencia); "crear el partido no concede por sí solo una plaza confirmada" — el organizador no queda `Confirmed` solo por crear; snapshots congelados como principio general de histórico (§29 del plan); política temporal (España→UTC) ya vigente en `CourtSlot`.
- [m2-clubs-courts/design.md](../m2-clubs-courts/design.md): `CourtSlot` y su `SlotStatus` (`Available`/`Booked`, este último ya anticipado en el enum) son lo que esta spec consume y transiciona; no se modifica `Club`/`Court`.
- [m1-players/design.md](../m1-players/design.md): `Player.Level`/`MatchesPlayed`/`LevelConfidence` ya existen; esta spec los lee (snapshot del organizador, validar que puede crear un competitivo) sin modificarlos.

## Decisión de alcance: backend primero, mobile aparte

**Esta spec entrega solo el contrato de backend**, repitiendo el patrón ya seguido en M1 y M2 (`m1-players`/`m1-mobile-auth`, `m2-clubs-courts`/`m2-mobile-clubs`): mantiene el foco en el modelo de dominio y la API, verificable con tests de integración sin depender de pantallas. Las pantallas de mobile para crear un partido desde un hueco se abordan en una spec de mobile aparte una vez cerrado este contrato.

## Decisión de alcance: crear no es unirse

El plan (§11) y la constitución son explícitos: la plaza del organizador no queda ocupada por el simple hecho de crear el partido — eso requiere el flujo de confirmación (`Held`→`Confirmed`) que introduce M5 junto con `JoinMatch`. Esta spec, por tanto, **no implementa `JoinMatch`, `MatchParticipant` ni el estado de plazas individuales**; el resultado de M3 es un `Match` en estado `Open`, con su hueco bloqueado, pero sin ningún jugador todavía ocupando una plaza — ni siquiera el organizador. Cómo se representa exactamente "0 plazas confirmadas" en el modelo de datos (¿existen ya cuatro `MatchSlot` vacíos, o esa estructura la introduce M5?) es una decisión técnica que se resuelve en `design.md`.

## Alcance incluido

| Área de M3 (plan §38) | Resultado esperado |
| --- | --- |
| Crear partido desde `CourtSlot` | Un jugador autenticado crea un `Match` a partir de un `CourtSlot` `Available` concreto; el hueco pasa a `Booked` y queda exclusivamente asociado a ese partido. |
| Tipo de partido | El creador elige `Competitive` o `Friendly` al crear; el tipo no cambia después. |
| Rango de nivel (competitivo) | El organizador define `MinLevel`/`MaxLevel` a partir de su propio nivel (`OrganizerLevelAtCreation`, snapshot); no necesita ser simétrico. |
| Mínimo de partidos y nota | El organizador puede fijar `MinMatchesRequired` (competitivo) y una `Note` (ambos tipos), opcionales. |
| Snapshots y congelación | `OrganizerLevelAtCreation`, rango, mínimo y nota quedan congelados al pasar de `Draft` a `Open`; ningún endpoint de esta spec permite modificarlos después. |
| Duración y horario heredados | `StartsAt`/`EndsAt`/duración del partido son los del `CourtSlot` elegido; no se vuelven a pedir. |
| Consulta del partido creado | Un jugador autenticado puede consultar el detalle de un partido por id (club, pista, horario, tipo, rango/mínimo/nota si aplica, estado). |
| Exclusividad y concurrencia del hueco | Dos intentos concurrentes de crear un partido sobre el mismo `CourtSlot` no pueden tener éxito ambos (constitución, no negociable de concurrencia). |

## Fuera de alcance

- **Unirse a un partido, retención/confirmación de plaza (`Held`/`Confirmed`), pago simulado** — M5. Esta spec no crea ninguna ocupación de plaza, ni siquiera para el organizador.
- **Feed de descubrimiento, filtros, compatibilidad de nivel para unirse** — M4. `GetMatchDetails` de esta spec es solo para consultar lo ya creado, no un listado.
- **Lista de espera, aprobación excepcional, evaluación de calidad** — M6/M7.
- **Liberación automática del hueco a 3 h del inicio sin reserva confirmada** (§10): depende del flujo de confirmación de M5; esta spec no implementa temporizadores ni expiración.
- **Transiciones automáticas más allá de `Draft`→`Open`** (`Open`→`Full`, →`InProgress`, etc., §22): dependen de jugadores confirmados y del paso del tiempo, ninguno de los dos existe todavía.
- **Edición o cancelación de un partido ya creado**: el plan no describe ningún flujo de edición para M3; una vez `Open`, el partido y sus atributos congelados son inmutables dentro de esta spec.
- **Pantallas de mobile** para crear/consultar un partido — spec aparte, siguiendo el patrón acordado arriba.
- **Partidos abiertos asociados en el detalle de club** (mencionado en el plan §14): se retoma cuando exista una consulta pensada para ello (posiblemente M4).

## Criterios de aceptación

1. Un jugador autenticado puede crear un partido a partir de un `CourtSlot` `Available` concreto, indicando tipo (`Competitive`/`Friendly`) y, para `Competitive`, rango de nivel y opcionalmente mínimo de partidos; en ambos tipos, una nota opcional.
2. Al crear un partido `Competitive`, se guarda el nivel actual del organizador como snapshot (`OrganizerLevelAtCreation`); el rango de nivel se valida en función de ese snapshot pero no necesita ser simétrico.
3. Un jugador sin puntaje (`Level` nulo) no puede crear un partido `Competitive`; sí puede crear uno `Friendly`, que no requiere ni rango de nivel ni mínimo de partidos.
4. El horario y la duración del partido son los del `CourtSlot` elegido; no se solicitan de nuevo al crear.
5. Al crear el partido, el `CourtSlot` pasa a `Booked`; un segundo intento de crear un partido sobre el mismo hueco (incluida una carrera entre dos peticiones concurrentes) falla para todos menos, como mucho, uno.
6. Tras la creación (`Open`), el rango de nivel, el mínimo de partidos y la nota quedan congelados: ningún endpoint de esta spec permite modificarlos.
7. Un jugador autenticado puede consultar el detalle de un partido que existe, incluyendo club, pista, horario, tipo, y rango/mínimo/nota cuando aplique.
8. Crear un partido no ocupa ninguna plaza, ni siquiera la del organizador — verificable porque esta spec no expone ningún dato de "plazas confirmadas" distinto de cero.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: el modelo de datos exacto de `Match`/`MatchType`/`MatchStatus` (EF Core, migración) y si M3 ya crea la estructura de `MatchSlot`/`MatchParticipant` (vacía) o la difiere a M5; el mecanismo concreto de exclusividad del `CourtSlot` ante creación concurrente (concurrencia optimista, transacción con bloqueo, o constraint de base de datos); la forma exacta de `POST /api/matches` y `GET /api/matches/{id}` (payloads, códigos de error); y cómo se valida "requiere puntaje para crear" contra `Player.Level` en el caso de uso.
