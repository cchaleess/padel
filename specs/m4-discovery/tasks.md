# Tareas — M4 Discovery

- [x] Extender `MatchWithSlotDetails` con `ClubLatitude`/`ClubLongitude`/`ClubCityOrZone`; actualizar `MatchRepository` (helper `JoinSlotDetails` compartido entre `FindDetailsByIdAsync` y la nueva consulta).
- [x] Crear `MatchCompatibility.IsCompatibleWithLevel(Match, decimal?)` en `PadelMatch.Domain.Matches`.
- [x] Añadir `IMatchRepository.FindOpenUpcomingAsync(DateTimeOffset, CancellationToken)`; implementar en `MatchRepository` (join Matches→CourtSlots→Courts→Clubs, filtro `Status == Open && StartsAt > now`).
- [x] Crear `IMatchFeedService`/`MatchFeedService` en `PadelMatch.Application.Matches`: particiona por compatibilidad, ordena cada grupo por proximidad (lat/lng → cityOrZone → `StartsAt`).
- [x] Añadir `GET /api/matches/feed` a `MatchEndpoints.cs` con `MatchFeedResponse`/`MatchFeedItemResponse` en `MatchResponses.cs`.
- [x] Registrar `IMatchFeedService` en `DependencyInjection.cs`.
- [x] Tests de dominio: `MatchCompatibility` (Friendly siempre compatible; Competitive dentro/fuera de rango; sin `Level`).
- [x] Tests de integración: feed vacío sin partidos; partido `Friendly` en `ForYou`; `Competitive` compatible en `ForYou` y fuera de rango en `OutOfRange`; jugador sin `Level` con un `Competitive` cae en `OutOfRange`; partido expirado no aparece en ningún grupo; orden por `lat`/`lng` y por `cityOrZone`; autenticación requerida. Bug encontrado y corregido: `FindOpenUpcomingAsync` no traducía a SQL porque el filtro `StartsAt > now` se aplicaba sobre el record `MatchWithSlotDetails` ya proyectado — EF Core no puede traducir un `Where` sobre una propiedad computada de un tipo ya proyectado; se aplica ahora sobre el join intermedio, antes del `Select` final.
- [x] `dotnet test` sin fallos (40 dominio + 36 integración); actualizar README (sección M4 backend).
- [x] Documentar la verificación con `pastiche-rdd`. `pastiche rdd verify m4-discovery` → Verified 4/4.
