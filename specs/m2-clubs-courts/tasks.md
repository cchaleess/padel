# Tareas — M2 Clubs & Courts (read-only)

- [x] Añadir `Club`, `Court`, `CourtSlot`, `ClubStatus` y `SlotStatus` en `PadelMatch.Domain`, sin dependencias externas; factorías `Club.Create` (seed/Official), `Club.SubmitByPlayer` (UserSubmitted) y `CourtSlot.Create` con las invariantes del diseño (nombre/dirección obligatorios, duración restringida a 60/90/120, `EndsAt` calculado).
- [x] Pruebas de dominio: `CourtSlot.Create` rechaza una duración fuera de `{60, 90, 120}` y calcula `EndsAt` correctamente; `Club.SubmitByPlayer` rechaza nombre/dirección vacíos.
- [x] Implementar el cálculo de distancia Haversine como servicio de dominio puro, con pruebas unitarias sobre pares de coordenadas conocidas.
- [x] Definir en `PadelMatch.Application` los contratos de repositorio/consulta y los casos de uso: `GetNearbyClubs`, `SearchClubs`, `GetClubDetails`, `GetCourtSlots`, `SubmitClub`.
- [x] Configurar EF Core: mapeo de `Club`/`Court`/`CourtSlot`, índices (`Clubs.Name`, `Clubs.CityOrZone`, `CourtSlots(CourtId, StartsAt)`), migración `AddClubsAndCourts`.
- [x] Implementar `DevelopmentClubSeeder` en `PadelMatch.Infrastructure`, invocado solo en `IsDevelopment()` y con `Development:SeedClubs=true` (desactivado explícitamente en los tests de integración para no contaminarlos), idempotente; datos mínimos: 2 clubes `Official` con coordenadas y ciudades distintas, 2–3 `Court` cada uno, huecos cubriendo los próximos 5 días con las tres duraciones.
- [x] Exponer `GET /api/clubs/nearby` (orden por Haversine si hay `lat`/`lng`; fallback por `CityOrZone` del jugador o del query param; sin ninguna señal, orden por nombre), protegido con el JWT de sesión de M1.
- [x] Exponer `GET /api/clubs/search?q=`, `GET /api/clubs/{id}` y `GET /api/clubs/{id}/slots` (con `from`/`to`/`courtId`, ventana por defecto de 14 días, solo `Status = Available`), protegidos igual.
- [x] Exponer `POST /api/clubs` para clubes `UserSubmitted` (jugador autenticado como `SubmittedByPlayerId`), devolviendo 201/`ClubDetailResponse` o 400 si faltan campos obligatorios.
- [x] Pruebas de integración (`WebApplicationFactory` + PostgreSQL real, clubes/pistas/huecos sembrados por caso, no por el seeder de desarrollo): orden por distancia real, fallback por `CityOrZone`, orden por nombre sin ninguna señal, búsqueda por nombre (`Official` y `UserSubmitted`), creación de club vía `POST /api/clubs` y su validación, filtrado de huecos por rango/pista y por `Status = Available`, 401 en todos los endpoints sin token válido.
- [x] Actualizar el README: nuevos endpoints, cómo y cuándo corre el seed de desarrollo (y que no es apto para producción), variables/parámetros nuevos si los hay.
- [x] Revisar cambios, instrucciones reproducibles y correspondencia con los criterios de aceptación del proposal.
- [x] Documentar la verificación con `pastiche-rdd`.
