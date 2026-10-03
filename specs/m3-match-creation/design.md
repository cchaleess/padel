# Diseño — M3 Match creation

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): un jugador autenticado crea un `Match` a partir de un `CourtSlot` `Available`, eligiendo tipo (`Competitive`/`Friendly`) y, si es competitivo, un rango de nivel y opcionalmente un mínimo de partidos y una nota. Se mantiene la organización ya usada en M1/M2 (`Domain` sin dependencias externas, `Application` con los contratos de caso de uso y su implementación de orquestación, `Infrastructure` con la persistencia, `Api` componiendo endpoints).

Dos decisiones centrales, ambas consecuencia directa del alcance ya acordado en el proposal ("crear no es unirse"):

1. **No se crea ningún `MatchParticipant`/`MatchSlot` en M3.** El `Match` nace sin ningún jugador ocupando plaza, ni siquiera el organizador — esas estructuras y el flujo `Held`→`Confirmed` los introduce M5. Modelarlas ahora sería construir para un requisito que esta spec explícitamente no cubre (principio de la constitución: dominio ambicioso pero sin sobre-construir por adelantado, mismo criterio que M2 aplicó a `SlotStatus.Booked`).
2. **`POST /api/matches` crea el partido ya `Open`, en una sola llamada.** El proposal excluye explícitamente cualquier flujo de edición o publicación posterior; sin un endpoint que edite un `Draft`, mantenerlo como estado intermedio persistible no tendría ningún consumidor. `MatchStatus` en M3 solo produce `Open`. El enum se ampliará (`Full`, `InProgress`, ...) en los milestones que de verdad los necesiten, mismo criterio incremental que `SlotStatus` en M2. (`Draft` del plan §22 se entiende como el estado transitorio de un formulario en el cliente, no como una fila persistida — si una spec de mobile posterior necesita guardar un borrador entre sesiones, se revisita entonces.)

## Preguntas resueltas

Confirmadas por el usuario tras el borrador de este diseño:

1. **¿Se crean ya `MatchParticipant`/`MatchSlot` (aunque vacíos) en M3?** No — se difieren por completo a M5. Confirma la decisión 1 de arriba.
2. **¿`POST /api/matches` publica directamente como `Open`, o hay un `Draft` persistido con endpoint de publicación aparte?** Publica directamente en una sola llamada, sin `Draft` persistido. Confirma la decisión 2 de arriba.

## Modelo de dominio (`PadelMatch.Domain.Matches`)

```text
Match
  Id: Guid
  CourtSlotId: Guid
  OrganizerId: Guid                    (Player.Id)
  Type: MatchType                      (Competitive | Friendly)
  Status: MatchStatus                  (Open — único valor que M3 produce)
  OrganizerLevelAtCreation: decimal?   (snapshot; null en Friendly)
  MinLevel: decimal?                   (null en Friendly)
  MaxLevel: decimal?                   (null en Friendly)
  MinMatchesRequired: int?             (opcional también en Competitive)
  Note: string?                        (opcional en ambos tipos; máx. 500 caracteres)
  CreatedAtUtc: DateTimeOffset

MatchType: enum { Competitive, Friendly }
MatchStatus: enum { Open }
```

- No se duplican club/pista/horario en `Match`: `CourtSlot` (M2) ya es inmutable una vez creado y no tiene ningún flujo de edición, así que referenciarlo por `CourtSlotId` y resolver club/pista/horario con un `Join` en la consulta (mismo patrón que `ClubRepository.GetSlotsAsync`) es suficiente — no hace falta el snapshot que sí exige el plan para el nivel (§29), porque aquí no hay nada que pueda cambiar por debajo.
- `Match.CreateFriendly(courtSlotId, organizerId, note, nowUtc)`: sin nivel ni mínimo de partidos.
- `Match.CreateCompetitive(courtSlotId, organizerId, organizerLevelAtCreation, minLevel, maxLevel, minMatchesRequired, note, nowUtc)`: valida en el propio dominio (no solo en el request, principio de la constitución de proteger reglas críticas en backend):
  - `minLevel <= organizerLevelAtCreation <= maxLevel` (el nivel base del organizador debe caer dentro del rango que él mismo define; no exige simetría).
  - `minLevel <= maxLevel`.
  - `minMatchesRequired`, si se indica, `>= 0`.
- `Note`, si se indica, no puede superar 500 caracteres en ningún tipo — límite arbitrario razonable, no viene del plan; se documenta como decisión, no como hallazgo.
- Ambas factorías dejan `Status = Open` directamente (ver decisión 2 arriba).

## Exclusividad del `CourtSlot` bajo concurrencia

La constitución fija como no negociable que un hueco no puede generar dos partidos, también ante peticiones simultáneas. Mecanismo, sin introducir tokens de concurrencia optimista (sin precedente en el proyecto, y un índice único basta para esta invariante):

1. `MatchCreationService` carga el `CourtSlot` (vía `IClubRepository`, que ya lo persiste) y falla rápido con `CourtSlotNotFoundException` (404) o `CourtSlotUnavailableException` (409) si no es `Available` — cubre el caso no concurrente con un error claro.
2. Si pasa la comprobación, marca el `CourtSlot` como `Booked` (`CourtSlot.Book()`, nuevo método de dominio) y construye el `Match`.
3. `IMatchRepository.AddMatchAsync` + una única `SaveChangesAsync` (misma `DbContext` con ámbito de request que ya usa `IClubRepository`, así que ambos cambios — `CourtSlot.Status` y el nuevo `Match` — se confirman en la misma transacción implícita de EF Core).
4. **Backstop real ante la carrera:** índice único `Matches(CourtSlotId)`. Si dos peticiones concurrentes pasan ambas el paso 1 (ambas ven `Available`), solo una gana la inserción; la otra recibe una violación de restricción única de PostgreSQL (`Npgsql.PostgresException`, `SqlState = 23505`) envuelta en `DbUpdateException`. El servicio la detecta por el nombre de la restricción y la traduce a `CourtSlotUnavailableException` (409) — mismo resultado que el caso no concurrente, indistinguible para el cliente.

## Persistencia

- Nueva migración `AddMatches` sobre `PadelMatchDbContext`: tabla `Matches`.
- `Type`/`Status` como texto (`HasConversion<string>().HasMaxLength(20)`), mismo tratamiento que `ClubStatus`/`SlotStatus` en M2.
- `OrganizerLevelAtCreation`/`MinLevel`/`MaxLevel` como `numeric(3,1)` nullable, mismo tipo de columna que `Player.Level`.
- `Note` como `character varying(500)` nullable.
- Claves foráneas: `CourtSlotId` → `CourtSlots.Id`, `OrganizerId` → `Players.Id` (sin cascada: un partido no debe desaparecer si se borra el jugador, aunque el MVP no tiene borrado de jugadores — coherente con no añadir comportamiento para un caso que no existe).
- Índice único `IX_Matches_CourtSlotId` sobre `CourtSlotId` — es el mecanismo real de la invariante de concurrencia, no solo una optimización de consulta.

## API y contratos

```text
POST /api/matches         [autenticado] { courtSlotId, type, minLevel?, maxLevel?, minMatchesRequired?, note? } -> MatchDetailResponse (201)
GET  /api/matches/{id}    [autenticado]                                                                          -> MatchDetailResponse
```

- `MatchDetailResponse`: `id`, `clubId`, `clubName`, `courtId`, `courtName`, `startsAt`, `endsAt`, `durationMinutes` (resueltos desde el `CourtSlot`/`Court`/`Club` asociados), `type`, `status`, `organizerId`, `organizerLevelAtCreation?`, `minLevel?`, `maxLevel?`, `minMatchesRequired?`, `note?`.
- `POST /api/matches` usa el `sub` del JWT de sesión como `organizerId` (mismo patrón que `POST /api/clubs` con `SubmittedByPlayerId`); el body no lo incluye.
- Validación de "requiere puntaje para crear un competitivo": el servicio carga el `Player` autenticado (`IPlayerRepository.FindByIdAsync`, ya existente) y, si `Type = Competitive` y `Player.Level` es `null`, responde 400 antes de tocar el `CourtSlot` — no se bloquea el hueco por una petición que iba a fallar de todas formas.
- Errores: `CourtSlotNotFoundException` → 404; `CourtSlotUnavailableException` → 409; validaciones de dominio (`ArgumentException`: rango inválido, nivel requerido, nota demasiado larga, `minMatchesRequired` negativo) → 400; `MatchNotFoundException` (nuevo, mismo patrón que `ClubNotFoundException`) → 404 en `GET /api/matches/{id}`.
- `GET /api/matches/{id}` no se restringe al organizador: cualquier jugador autenticado puede consultarlo (mismo criterio de autorización que el resto de la API — sesión válida, sin control de propiedad adicional). No expone todavía ninguna lista/feed; ese es el alcance de M4.
- Todos los endpoints exigen el JWT de sesión de M1 (`RequireAuthorization()`), igual que Clubs y Players.

## Verificación

Mismo patrón que M0/M1/M2 (`WebApplicationFactory` + PostgreSQL real). Cobertura:

- Crear un `Friendly` sin nivel del organizador tiene éxito; crear un `Competitive` sin nivel del organizador falla con 400.
- Crear un `Competitive` válido guarda `OrganizerLevelAtCreation` igual al `Player.Level` del creador en el momento de la llamada.
- Rechaza (400) un `Competitive` con `minLevel > maxLevel`, o con el nivel del organizador fuera de `[minLevel, maxLevel]`.
- Al crear, el `CourtSlot` pasa a `Booked`.
- Un segundo intento de crear sobre el mismo `CourtSlot` ya `Booked` falla con 409, tanto en secuencia como con dos llamadas concurrentes reales contra el mismo hueco (test de concurrencia con `Task.WhenAll`, verificando que exactamente una tiene éxito).
- `GET /api/matches/{id}` devuelve club/pista/horario/tipo/rango/mínimo/nota correctos; 404 si no existe.
- `Match.CreateCompetitive`/`CreateFriendly` (dominio, sin HTTP) cubren las validaciones de rango, nota y `minMatchesRequired` directamente.
- 401 en ambos endpoints sin token válido.

Se documentará con `pastiche-rdd` al terminar `tasks.md`, igual que en M0/M1/M2.

## Alternativas y límites

- Sin tokens de concurrencia optimista (`xmin`/`RowVersion`): el índice único sobre `Matches.CourtSlotId` basta para la invariante exigida y no introduce un mecanismo nuevo en el proyecto sin necesidad real.
- `MatchStatus` solo tiene `Open` en esta spec; se amplía cuando la spec que de verdad produce cada estado lo necesite (mismo criterio incremental que `SlotStatus`/`SlotDuration` en M2), en vez de fijar ahora los diez valores del plan §22 sin ningún caso de uso que los produzca o consuma todavía.
- No se modela `Draft` como fila persistida (ver "Decisión 2"); si una futura spec de mobile necesita conservar un borrador entre sesiones sin publicarlo, se añade entonces.
- `Note` limitada a 500 caracteres: el plan no fija un máximo; es un límite defensivo razonable, no una regla del producto.
- No hay paginación ni listado de partidos en esta spec: `GET /api/matches/{id}` es consulta puntual; el listado/feed es M4.
- `GET /api/matches/{id}` es visible para cualquier jugador autenticado, no solo el organizador: no hay razón todavía para restringirlo (no expone nada sensible que no vaya a ser público en el feed de M4), y restringirlo ahora solo para reabrirlo en M4 sería trabajo doble.
