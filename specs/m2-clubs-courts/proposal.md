# M2 — Clubs & Courts (read-only)

## Intención y problema

M1 dejó un jugador autenticado con perfil propio, pero sin nada que consultar más allá de sí mismo. M2 introduce el primer concepto compartido del dominio: clubes, pistas y huecos de pista disponibles (`CourtSlot`), en modo exclusivamente de lectura para el jugador. Es el prerequisito directo de M3 (crear un partido a partir de un hueco concreto): sin poder descubrir un `CourtSlot`, no hay nada sobre lo que crear una partida.

Esta spec también resuelve la primera pieza de geolocalización del plan (§5, diferida explícitamente desde M1): ordenar clubes por cercanía, con la ciudad/zona del jugador como alternativa si no hay coordenadas de dispositivo.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §8 (clubes y geolocalización), §9 (disponibilidad de pista), §34 (`Club`, `Court`, `CourtSlot`, `ClubStatus`, `SlotStatus` en el modelo de dominio), §36 (casos de uso de Discovery: `GetNearbyClubs`, `SearchClubs`, `GetClubDetails`), §37 (referencia orientativa: `GET /api/clubs/nearby`, `/search`, `/{id}`, `/{id}/slots`), §38 M2.
- [Constitución del proyecto](../CONSTITUTION.md): stack y arquitectura modular; política temporal (zona única de España, UTC interno) aplica a `CourtSlot.StartsAt`/`EndsAt`; la geolocalización de dispositivo es opcional con ciudad/zona como fallback (ya vigente desde M1 en `Player.CityOrZone`).
- [m1-players/design.md](../m1-players/design.md): `Player` ya resuelto, esta spec no lo modifica; reutiliza el jugador autenticado como actor de todos los endpoints.
- [m1-mobile-auth/proposal.md](../m1-mobile-auth/proposal.md): precedente directo del patrón "backend primero, mobile aparte" que esta spec repite (ver más abajo).

## Decisión de alcance: backend primero, mobile aparte

**Esta spec entrega solo el contrato de backend.** Repite el patrón que M1 adoptó a posteriori (`m1-players` de backend + `m1-mobile-auth` de UI), esta vez de forma deliberada desde el principio: mantiene la spec enfocada en el modelo de dominio y el contrato de API, permite verificarlo de forma independiente (tests de integración, sin depender de pantallas), y evita una spec desproporcionadamente grande. Las pantallas de `mobile/` para descubrir clubes y consultar huecos se abordan en una spec de mobile aparte, una vez cerrado este contrato — decisión del usuario (2026-09-13).

## Decisión de alcance: origen de los datos de club/pista/hueco

**Seed/fixture de datos de desarrollo, no un flujo de creación de producto.** El plan reserva explícitamente la gestión completa de clubes (crear pistas, definir huecos, políticas, precios) para una fase posterior ("Club Management", §40) y el propio título de M2 en el plan es "read-only". Sin panel de gestión, esta spec no incluye ningún endpoint de creación de `Club`/`Court`/`CourtSlot` pensado para producción: los datos necesarios para construir y verificar el descubrimiento/consulta se cargan mediante un seed de desarrollo (migración de datos EF Core o script), documentado explícitamente como temporal — decisión del usuario (2026-09-13). La única excepción es la aportación de clubes por parte de jugadores (`UserSubmitted`, ver alcance incluido), que el plan sí describe como una capacidad de usuario final, no de gestión de club.

## Alcance incluido

| Área de M2 (plan §38) | Resultado esperado |
| --- | --- |
| Descubrimiento de clubes cercanos | Un jugador puede consultar una lista de clubes ordenados por cercanía (coordenadas de dispositivo, o ciudad/zona del perfil como alternativa), con los clubes aportados por usuarios apareciendo cuando no hay señal de cercanía disponible, conforme al orden del §8. |
| Búsqueda de clubes por nombre | Un jugador puede buscar clubes por nombre, incluyendo tanto oficiales como aportados por usuarios. |
| Detalle de club | Un jugador puede consultar nombre, dirección, estado (`Official`/`UserSubmitted`) y las pistas de un club. |
| Huecos de pista disponibles | Un jugador puede consultar los `CourtSlot` disponibles de una pista/club, con club, pista, fecha, hora de inicio y duración (60/90/120 min, 90 por defecto); `EndsAt` se calcula automáticamente. |
| Clubes aportados por usuarios | Un jugador autenticado puede aportar un club nuevo (nombre + dirección como mínimo); queda marcado `UserSubmitted`, es reutilizable, aparece en búsquedas, se muestra como no verificado, sin detección de duplicados (conforme al §8). |

## Fuera de alcance

- **Pantallas de mobile** (descubrimiento, búsqueda, detalle de club, listado de huecos). Spec aparte, siguiendo el patrón acordado arriba.
- **Gestión completa de clubes**: crear/editar pistas, definir huecos de producto, políticas, precios, cancelaciones — "Club Management", fase posterior (§40). Los `CourtSlot` de esta spec existen únicamente vía seed de desarrollo.
- **Creación de partidos sobre un `CourtSlot`** (bloqueo de hueco, concurrencia de reserva, `MatchSlot`) — M3 (Match creation) en adelante.
- **Orden "clubes usados recientemente"** del §8: depende del histórico de partidos jugados por el jugador, que no existe hasta M3+. Esta spec implementa cercanía y aportados por usuarios; el criterio de "usados recientemente" se añade cuando exista esa señal.
- **"Partidos abiertos asociados"** en el detalle de club (mencionado en §14 del plan): depende de `Match`, fuera de alcance hasta M3+.
- **Geocodificación de direcciones**: un club aportado por un jugador que no indique coordenadas no participa del orden por cercanía (queda en el bucket "aportados por usuarios" del §8); no se integra ningún servicio de geocodificación real en esta spec.
- **Detección de clubes duplicados** — explícitamente fuera del MVP (§8).
- Cualquier endpoint de administración de club/pista/hueco pensado para producción.

## Criterios de aceptación

1. Un jugador autenticado puede consultar una lista de clubes cercanos, ordenados por proximidad cuando hay coordenadas disponibles (dispositivo o ciudad/zona del perfil), con los clubes aportados por usuarios visibles cuando no hay señal de cercanía.
2. Un jugador puede buscar clubes por nombre y encontrar tanto clubes oficiales como aportados por usuarios.
3. Un jugador puede consultar el detalle de un club: nombre, dirección, estado (oficial/aportado) y sus pistas.
4. Un jugador puede consultar los huecos disponibles de una pista/club, con fecha, hora de inicio, duración y hora de fin calculada correctamente en la zona horaria de la constitución.
5. Un jugador autenticado puede aportar un club nuevo con nombre y dirección; queda marcado como no verificado, es reutilizable en búsquedas y descubrimiento futuros.
6. Sin coordenadas de dispositivo y sin ciudad/zona en el perfil, el descubrimiento y la búsqueda de clubes siguen funcionando (no fallan ni devuelven error), aunque sin ordenar por cercanía.
7. Los datos de club/pista/hueco de esta spec provienen de un seed de desarrollo documentado como tal en el README; no existe ningún flujo de creación pensado para producción todavía.
8. Las reglas de qué hace válido un club aportado por usuario (campos mínimos, estado `UserSubmitted`) están protegidas en backend, no solo en la futura UI del cliente móvil.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: el modelo de datos exacto de `Club`/`Court`/`CourtSlot`/`ClubStatus`/`SlotStatus` (EF Core, migraciones), cómo se almacenan y consultan las coordenadas (tipo de columna, cálculo de distancia), el contenido y mecanismo concreto del seed de desarrollo, la forma exacta de los endpoints (paginación, parámetros de búsqueda y cercanía, rango de fechas de huecos), y la zona horaria/tipo UTC concreto de `CourtSlot` conforme a la política temporal de la constitución.
