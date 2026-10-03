# M4 — Discovery

## Intención y problema

[m3-match-creation](../m3-match-creation/proposal.md) permite crear un `Match` a partir de un `CourtSlot` disponible, pero hoy la única forma de verlo es `GET /api/matches/{id}` con el `id` ya conocido (el organizador lo recibe al crear). Ningún otro jugador puede descubrir qué partidos existen. Esta spec construye el backend de un feed que responda la pregunta del plan (§13): **¿a qué partidos puedo unirme?**

Es la mitad de backend de M4; las pantallas de mobile que lo consumen son una spec aparte (`m4-mobile-feed`, análoga a `m3-mobile-matches`), siguiendo el mismo patrón que M1/M2/M3.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §13 (descubrimiento de partidos: feed principal, prioridad, exclusiones, sección de competitivos fuera de rango), §14 (card de partido), §15 (detalle del partido).
- [m3-match-creation/design.md](../m3-match-creation/design.md): modelo de `Match` que consume esta spec (`Type`, `Status`, `MinLevel`/`MaxLevel`, `OrganizerLevelAtCreation`).
- [m2-clubs-courts/design.md](../m2-clubs-courts/design.md): patrón de proximidad ya existente (`GET /api/clubs/nearby`: `lat`/`lng` opcionales, `cityOrZone` del jugador como alternativa, Haversine para ordenar) — esta spec lo reutiliza para el feed en vez de inventar uno nuevo.
- [Constitución del proyecto](../CONSTITUTION.md) §84: el algoritmo definitivo de nivel y el design system siguen pendientes para fases posteriores; ninguna de las dos cosas se inventa aquí.

## Decisión de alcance: solo se excluyen partidos expirados

El plan (§13) pide excluir del feed partidos `Full`, finalizados, cancelados internamente y expirados. Hoy `MatchStatus` solo tiene `Open` — `Full` llega con [M5 (confirmación de plaza)](../../MVP-PLAN.md) cuando exista un modelo de participantes, y no hay ningún concepto de partido cancelado o finalizado todavía. Esta spec solo implementa la exclusión que ya tiene sentido con los datos actuales: un partido cuyo `CourtSlot` ya empezó (`StartsAt <= now`) deja de aparecer en el feed. Las demás exclusiones se añaden cuando los estados que las motivan existan.

## Decisión de alcance: sin "calidad estimada" ni contador de confirmados

El plan (§14/§15) pide mostrar una "calidad estimada" del partido y "jugadores confirmados x/4" en la card y el detalle. Ninguno de los dos existe todavía: la calidad no tiene algoritmo definido (fuera del MVP, constitución §84) y el contador de confirmados depende del modelo de participantes de M5. Esta spec no inventa ni un número de calidad ni un placeholder de confirmados — la card/detalle de M4 muestra únicamente los datos que `Match`/`Club`/`CourtSlot` ya tienen: club, horario, tipo, y rango de nivel si es competitivo. Se añaden en M5/M6 cuando los datos existan de verdad.

## Decisión de alcance: sin `Level`, todo lo competitivo es "fuera de rango"

Un jugador sin encuesta de nivel completada no tiene forma de evaluar compatibilidad con ningún rango `minLevel`/`maxLevel`. En vez de ocultar los partidos `Competitive` o tratarlos como un caso especial, caen en la misma sección que un jugador con nivel incompatible: "Otros partidos cercanos" (fuera de rango). Es la misma regla ("¿el nivel del jugador cae dentro de `[MinLevel, MaxLevel]`?") aplicada de forma consistente, sin un caso aparte para "sin nivel".

## Decisión de alcance: solo lectura, sin unirse

Tocar un partido del feed lleva a su detalle (`GET /api/matches/{id}`, ya existente) pero no hay ninguna acción de "unirse" o "solicitar acceso" todavía — eso depende del flujo `Held`→`Confirmed` de M5, que esta spec no construye. El feed resuelve descubrimiento, no participación.

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Feed de partidos | Nuevo endpoint que lista partidos `Open` no expirados, cerca del jugador (mismo patrón de `lat`/`lng`/`cityOrZone` que `clubs/nearby`). |
| Prioridad/secciones | Los partidos se devuelven agrupados: "para ti" (`Friendly` + `Competitive` compatible con el `Level` del jugador) y "fuera de rango" (`Competitive` incompatible o jugador sin `Level`), cada grupo ordenado por proximidad y luego por `StartsAt`. |
| Exclusión de expirados | Un partido cuyo `CourtSlot.StartsAt` ya pasó no aparece en ninguna sección. |
| Datos del partido en el feed | Club, horario, tipo, rango de nivel si aplica — sin calidad estimada ni contador de confirmados. |

## Fuera de alcance

- **Unirse, solicitar acceso, lista de espera**: M5/M6/M7.
- **Exclusión de partidos `Full`, cancelados o finalizados**: esos estados no existen todavía; se añaden junto con M5/M6.
- **Calidad estimada**: sin algoritmo definido (constitución §84), no se inventa uno aquí.
- **"Mis partidos" (los que organizo o en los que participo)**: esta spec es el feed público de descubrimiento, no un listado personal.
- **Pantallas de mobile**: spec aparte (`m4-mobile-feed`), igual que M1/M2/M3.

## Criterios de aceptación

1. Un jugador autenticado puede pedir el feed y recibe partidos `Open` cuyo hueco todavía no ha empezado.
2. El feed devuelve dos grupos: "para ti" y "fuera de rango", cada uno ordenado por proximidad (si hay `lat`/`lng` o `cityOrZone`) y luego por horario.
3. Un partido `Competitive` cuyo rango `[MinLevel, MaxLevel]` contiene el `Level` del jugador aparece en "para ti"; si no lo contiene, o el jugador no tiene `Level`, aparece en "fuera de rango".
4. Un partido `Friendly` siempre aparece en "para ti".
5. Un partido cuyo `CourtSlot.StartsAt` ya pasó no aparece en ningún grupo.
6. La respuesta no incluye ningún dato de calidad estimada ni de jugadores confirmados.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: la forma exacta del endpoint (ruta, parámetros, forma de la respuesta agrupada), qué repositorio/consulta reutiliza del catálogo de clubes para la proximidad, y cómo se calcula "compatible" de forma reutilizable entre el feed y una futura validación de unión (M5).
