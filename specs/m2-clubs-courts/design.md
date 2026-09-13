# Diseño — M2 Clubs & Courts (read-only)

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): un jugador autenticado puede descubrir clubes cercanos, buscar por nombre, consultar el detalle de un club y los huecos de pista disponibles, y aportar un club nuevo. Se mantiene la organización de M0/M1 (`Domain` sin dependencias externas, `Application` con los contratos de caso de uso, `Infrastructure` con la implementación técnica, `Api` componiendo endpoints).

Decisión central: **sin geocodificación real**, la "cercanía" solo puede calcularse cuando el club tiene coordenadas explícitas. Para no dejar el fallback de ciudad/zona (§5 del plan, ya vigente en `Player.CityOrZone`) sin ningún efecto práctico, `Club` incorpora su propio campo libre `CityOrZone` (mismo tratamiento que en `Player`: texto libre, opcional, sin geocodificar). Cuando no hay coordenadas de dispositivo, el backend ordena primero los clubes cuyo `CityOrZone` coincide (case-insensitive) con el del jugador autenticado, después el resto. Esto cierra el criterio de aceptación 6 del proposal sin introducir un servicio de geocodificación.

## Modelo de dominio (`PadelMatch.Domain`)

```text
Club
  Id: Guid
  Name: string
  Address: string
  CityOrZone: string?         (texto libre, igual criterio que Player.CityOrZone; sin geocodificar)
  Latitude: double?            (null si no hay coordenadas conocidas)
  Longitude: double?
  Status: ClubStatus           (Official | UserSubmitted)
  SubmittedByPlayerId: Guid?   (null para Official/seed; Id del Player que lo aportó si UserSubmitted)
  CreatedAtUtc: DateTimeOffset

Court
  Id: Guid
  ClubId: Guid
  Name: string                 (p. ej. "Pista 1"; sin superficie/cubierta en M2, el plan no lo exige)

CourtSlot
  Id: Guid
  CourtId: Guid
  StartsAt: DateTimeOffset      (UTC; política temporal de la constitución)
  Duration: SlotDuration        (enum: SixtyMinutes=60 | NinetyMinutes=90 | OneTwentyMinutes=120)
  EndsAt: DateTimeOffset        (StartsAt + Duration; se guarda calculado para poder filtrar por rango sin computar en cada consulta)
  Status: SlotStatus            (Available | Booked; M2 solo produce Available — Booked queda para cuando M3 bloquee el hueco al crear un partido)

ClubStatus: enum { Official, UserSubmitted }
SlotStatus: enum { Available, Booked }
```

- `Club.Create(name, address, cityOrZone, latitude?, longitude?)` (factoría de dominio) construye un club `Official` sin `SubmittedByPlayerId`; se usa solo desde el seed de desarrollo.
- `Club.SubmitByPlayer(name, address, cityOrZone?, submittedByPlayerId)` construye un club `UserSubmitted`, sin coordenadas (el jugador que lo aporta no las conoce; ver "Fuera de alcance" del proposal). `Name` y `Address` son obligatorios — invariante de dominio, no solo validación de request, conforme al principio de la constitución de proteger reglas críticas en backend.
- `CourtSlot.Create(courtId, startsAt, duration)` valida que `duration` sea uno de los tres valores cerrados del plan (§9) y calcula `EndsAt`; es el único punto de construcción, tanto para el seed como para la futura creación real en la spec de "Club Management".
- No se modela todavía `Match`, `MatchSlot` ni ninguna reserva: `CourtSlot.Status` existe ya (para no rediseñar el esquema cuando M3 empiece a bloquear huecos), pero M2 nunca lo pone en `Booked`.

## Cercanía y orden de `GetNearbyClubs`

Sin PostGIS ni extensión espacial (dataset de MVP pequeño; añadir esa dependencia ahora sería sobre-ingeniería para el volumen actual, principio de constitución "UI simple, dominio ambicioso" aplicado también a infraestructura). Cálculo de distancia con la fórmula de Haversine, evaluada en memoria sobre los clubes ya traídos de PostgreSQL (sin filtro geoespacial en SQL):

1. Si la request incluye `lat`/`lng` (coordenadas de dispositivo): se calcula la distancia a todos los clubes con `Latitude`/`Longitude` no nulos y se ordenan ascendente por distancia; los clubes sin coordenadas se añaden al final, ordenados por nombre.
2. Si no hay `lat`/`lng` pero el jugador autenticado tiene `CityOrZone` en su perfil (o la request pasa un `cityOrZone` explícito, que tiene prioridad sobre el del perfil — por si el jugador quiere consultar otra zona puntualmente): los clubes cuyo `CityOrZone` coincide (case-insensitive, comparación exacta de texto — sin fuzzy matching en M2) se listan primero, el resto después, ambos grupos ordenados por nombre.
3. Si no hay ninguna señal de cercanía: todos los clubes ordenados por nombre. Cumple el criterio 6 del proposal: la llamada nunca falla, solo pierde el orden por cercanía.

No se pagina `GetNearbyClubs` ni `SearchClubs` en M2: el volumen esperado de clubes en esta etapa (seed de desarrollo + aportes de usuarios tempranos) no lo justifica. Se documenta como límite explícito, no como omisión ("Alternativas y límites").

## Huecos disponibles (`GetClubDetails/{id}/slots`)

- Parámetros: `from`/`to` (UTC, opcionales) y `courtId` (opcional, filtra a una sola pista del club). Si no se indican `from`/`to`, el backend usa por defecto `[ahora, ahora + 14 días]` — ventana suficiente para planear una partida sin devolver un histórico ilimitado de huecos futuros.
- Solo devuelve `CourtSlot.Status = Available`; en M2 son todos, pero el filtro ya queda listo para cuando M3 empiece a reservar huecos.
- Ordenado por `StartsAt` ascendente, agrupable por pista en el cliente (la respuesta incluye `courtId`/`courtName`, no anida por pista: mantiene el contrato simple).

## Seed de desarrollo

- `PadelMatch.Infrastructure` añade `DevelopmentClubSeeder`, invocado desde `Program.cs` solo cuando `app.Environment.IsDevelopment()` (mismo criterio que ya distingue `appsettings.Development.json` en M0/M1), tras aplicar migraciones.
- Idempotente: comprueba `await db.Clubs.AnyAsync()` antes de insertar; si ya hay clubes (por ejemplo, tras un `UserSubmitted` real durante desarrollo manual), no vuelve a sembrar. No es una migración EF (`HasData`) porque `HasData` exige claves fijas para siempre y este fixture está pensado para cambiar libremente mientras se construyen M2/M3, sin generar una nueva migración cada vez que se ajusta un dato de prueba.
- Contenido mínimo: al menos dos clubes `Official` con coordenadas reales (para poder probar el orden por cercanía) y ciudades distintas, cada uno con 2–3 `Court`, y huecos (`CourtSlot`) cubriendo los próximos días con las tres duraciones posibles. Se documenta en el README como datos de desarrollo, no aptos para producción — mismo tratamiento que ya reciben los placeholders de `Auth:Apple:Audience` en M1.

## Persistencia

- Nueva migración `AddClubsAndCourts` sobre `PadelMatchDbContext`: tablas `Clubs`, `Courts`, `CourtSlots`.
- Índices: `Clubs(Name)` (búsqueda `ILIKE`), `Clubs(CityOrZone)` (orden por coincidencia), `CourtSlots(CourtId, StartsAt)` (rango de fechas por pista).
- `Latitude`/`Longitude` como `double precision` nullable; sin tipo `geography`/`geometry` (evita añadir PostGIS ahora, ver arriba).
- `StartsAt`/`EndsAt` como `timestamp with time zone` en UTC, mismo tratamiento que `CreatedAtUtc` en M0/M1.

## API y contratos

```text
GET  /api/clubs/nearby   [autenticado] ?lat?&lng?&cityOrZone?   -> ClubSummaryResponse[]
GET  /api/clubs/search   [autenticado] ?q                        -> ClubSummaryResponse[]
GET  /api/clubs/{id}     [autenticado]                           -> ClubDetailResponse
GET  /api/clubs/{id}/slots [autenticado] ?from?&to?&courtId?      -> CourtSlotResponse[]
POST /api/clubs          [autenticado] { name, address, cityOrZone? } -> ClubDetailResponse (201, Status=UserSubmitted)
```

- `ClubSummaryResponse`: `id`, `name`, `cityOrZone`, `status`, `distanceKm` (nulo si no se pudo calcular).
- `ClubDetailResponse`: lo anterior más `address`, `courts: { id, name }[]`.
- `CourtSlotResponse`: `id`, `courtId`, `courtName`, `startsAt`, `endsAt`, `durationMinutes`.
- `POST /api/clubs` reutiliza la sesión del jugador autenticado (mismo `sub` que M1) como `SubmittedByPlayerId`; no se expone ese campo en la respuesta (no hay todavía necesidad de mostrar "aportado por X" en el plan).
- Todos los endpoints exigen el JWT de sesión de M1 (`RequireAuthorization()`), igual que `/api/players/me*`; no hay ningún endpoint público de solo lectura en esta spec — coherente con que el plan no pide un catálogo público de clubes sin login.

## Verificación

Mismo patrón que M0/M1 (`WebApplicationFactory` + PostgreSQL real). Los tests siembran sus propios clubes/pistas/huecos por caso (no dependen del `DevelopmentClubSeeder`, que solo corre en Development). Cobertura:

- `GetNearbyClubs` con coordenadas de dispositivo ordena por distancia real (Haversine) entre un conjunto de clubes con coordenadas conocidas.
- `GetNearbyClubs` sin coordenadas pero con `CityOrZone` del jugador prioriza los clubes de esa misma zona.
- `GetNearbyClubs` sin ninguna señal no falla y devuelve todos los clubes ordenados por nombre.
- `SearchClubs` encuentra clubes `Official` y `UserSubmitted` por coincidencia parcial de nombre.
- `POST /api/clubs` crea un club `UserSubmitted` válido; rechaza con 400 si falta `name` o `address` (invariante de dominio, no solo de request).
- `GET /api/clubs/{id}/slots` respeta `from`/`to`/`courtId` y excluye huecos fuera de rango; usa la ventana por defecto de 14 días cuando no se indican.
- `CourtSlot.Create` (dominio, sin HTTP) rechaza una duración fuera de `{60, 90, 120}`.
- 401 en todos los endpoints sin token válido, mismo patrón que M1.

Se documentará con `pastiche-rdd` al terminar `tasks.md`, igual que en M0/M1.

## Alternativas y límites

- No se usa PostGIS ni ningún tipo espacial: Haversine en memoria es suficiente para el volumen de clubes esperado en esta etapa; se revisita si el catálogo de clubes crece lo bastante para que el escaneo completo sea un problema real (no hoy).
- No hay paginación en `nearby`/`search`: mismo argumento de volumen; se añade cuando haga falta, no antes.
- No hay geocodificación de direcciones: un club `UserSubmitted` nunca participa del orden por distancia real hasta que alguien (una spec futura) le añada coordenadas por otra vía.
- `CityOrZone` de club es texto libre sin normalizar; la coincidencia con el del jugador es exacta (case-insensitive), no difusa — puede fallar por variantes de escritura ("Madrid" vs "Madrid Centro"). Aceptado como límite del MVP, igual que el resto de coincidencias de texto libre ya existentes en `Player.CityOrZone`.
- `CourtSlot.Status` existe pero M2 nunca produce `Booked`; queda preparado para M3, no implementado de más.
- Sin panel de gestión de club: el único punto de entrada de `Club`/`Court`/`CourtSlot` en esta spec son el seed de desarrollo y `POST /api/clubs` (solo para `UserSubmitted`, sin pistas ni huecos propios todavía).
