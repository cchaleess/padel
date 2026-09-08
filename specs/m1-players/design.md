# Diseño — M1 Players

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): un jugador se autentica con Google o Apple, queda registrado como `Player`, puede consultar/editar su perfil y puede completar opcionalmente la encuesta de nivel inicial. Se mantiene la organización de M0 (`Domain` sin dependencias externas, `Application` con los contratos de caso de uso, `Infrastructure` con la implementación técnica, `Api` componiendo endpoints).

Decisión central: el backend **no delega la sesión al proveedor externo**. Google/Apple solo se usan para probar la identidad en el momento del login (verificación del ID token que entrega su SDK nativo en el móvil); a partir de ahí, la API emite su propio token de sesión (JWT firmado por PadelMatch) que el cliente usa en el resto de llamadas. Esto aísla el resto del backend del proveedor concreto, es coherente con la decisión del proposal de no atarse a un único proveedor, y evita reverificar contra Google/Apple en cada request.

## Stack añadido

| Necesidad | Elección |
| --- | --- |
| Verificación de ID token de Google | `Google.Apis.Auth` (valida firma, emisor, audiencia y expiración contra las claves públicas de Google) |
| Verificación de ID token de Apple | Validación manual con `Microsoft.IdentityModel.Tokens` + `System.IdentityModel.Tokens.Jwt`, usando `ConfigurationManager<OpenIdConnectConfiguration>` contra el descubrimiento OIDC de `https://appleid.apple.com/.well-known/openid-configuration` (no existe paquete oficial de Apple para .NET) |
| Sesión propia de la API | `Microsoft.AspNetCore.Authentication.JwtBearer`, token simétrico firmado con una clave de configuración |
| Persistencia | Mismo `Npgsql`/EF Core 10 ya fijado en M0; nueva migración `AddPlayers` |

No se añade una librería de identidad completa (Identity, Duende, etc.): la única sesión que existe es la de `Player` autenticado externamente; no hay roles, ni recuperación de contraseña, ni flujos que esas librerías resuelven y que aquí no existen.

## Identidad externa y sesión

### Verificación del ID token

- `POST /api/auth/google` y `POST /api/auth/apple` reciben `{ "idToken": string }` (Apple añade opcionalmente `{ "displayName": string }`, ver más abajo) y devuelven `{ "sessionToken": string, "player": PlayerProfileResponse }`.
- Cada endpoint delega en `IExternalIdentityVerifier.VerifyAsync(AuthProvider, idToken)` (`PadelMatch.Application`), que devuelve `ExternalIdentity(string Subject, string? Email, string? DisplayName)` o lanza si el token no es válido para ese proveedor (firma, emisor, audiencia o expiración incorrectos).
- La audiencia de Google es el **Web Client ID** de PadelMatch en Google Cloud Console; el cliente móvil se configura con ese mismo id como `serverClientId`/`webClientId` en el SDK nativo (tanto Android como iOS), de modo que el `aud` del ID token es siempre el mismo sin importar la plataforma. La audiencia de Apple es el bundle identifier de la app iOS; el emisor esperado es siempre `https://appleid.apple.com`.
- Ambas claves de audiencia (y, si aplica, el Team ID de Apple) se configuran como `Auth:Google:Audience` y `Auth:Apple:Audience`, con el mismo tratamiento que `ConnectionStrings__PadelMatch` en M0: valor de desarrollo en `appsettings.Development.json`, exigido explícitamente fuera de Development.

### Alta o reconocimiento del jugador

- `Player` no se identifica por proveedor+id en la tabla principal; existe `PlayerExternalIdentity` (`PlayerId`, `Provider`, `ProviderSubjectId`, `LinkedAtUtc`) con índice único en `(Provider, ProviderSubjectId)`. Esto es lo que permite, según el proposal, añadir un proveedor más adelante sin rediseñar `Player`.
- Flujo del endpoint de login: buscar `PlayerExternalIdentity` por `(Provider, Subject)`. Si existe, es el `Player` que se autentica. Si no existe, se crea un `Player` nuevo y su primera `PlayerExternalIdentity`.
- **No se fusionan cuentas por email.** Si la misma persona inicia sesión primero con Google y luego con Apple, se crean dos `Player` distintos salvo vínculo explícito futuro. Motivo: Apple puede entregar una dirección de relay ("Hide My Email") distinta del email real, y confiar en la coincidencia de email entre proveedores para fusionar cuentas sería una suposición de seguridad no verificada. Vincular cuentas manualmente queda fuera de esta spec.
- El nombre de Apple solo llega la primera vez que la persona autoriza la app (comportamiento documentado de Sign in with Apple); en autorizaciones posteriores el ID token no lo incluye. Por eso el endpoint de Apple acepta `displayName` como campo aparte, enviado por el cliente solo cuando Apple se lo entrega, y se usa únicamente para poblar `Player.DisplayName` en el alta.
- Un mismo `Player` puede en el futuro tener más de una `PlayerExternalIdentity`; M1 no ofrece todavía una acción para vincular una segunda desde un `Player` ya autenticado, solo el modelo lo permite.

### Sesión propia

- Al reconocer o crear el `Player`, `IPlayerSessionTokenIssuer` (Application) emite un JWT firmado (HMAC, clave `Auth:SessionSigningKey`) con `sub = Player.Id`, expiración de 30 días. No hay refresco de token en M1: al expirar, el cliente vuelve a autenticarse de forma silenciosa con el SDK nativo del proveedor y llama de nuevo al endpoint de login. Es una simplificación consciente (principio de constitución "UI simple, dominio ambicioso"); si la duración resulta un problema de UX se revisita en una spec posterior, no se sobre-diseña ahora un flujo de refresh token.
- El resto de endpoints de `Player` (`/api/players/me*`) exigen ese JWT vía autenticación estándar de ASP.NET Core (`AddAuthentication().AddJwtBearer(...)`, `RequireAuthorization()`), igual que cualquier API protegida; no se reinventa un mecanismo propio de sesión.

## Modelo de dominio (`PadelMatch.Domain`)

```text
Player
  Id: Guid
  DisplayName: string
  Email: string?            (del proveedor; puede repetirse entre Players, ver arriba)
  DateOfBirth: DateOnly?     (null hasta completar perfil)
  CityOrZone: string?        (null hasta completar perfil)
  PhotoUrl: string?
  Level: decimal?            (null hasta completar encuesta; 1.0–5.0, un decimal)
  LevelConfidence: LevelConfidence   (None hasta completar encuesta)
  MatchesPlayed: int         (0; solo M3+ lo incrementa)
  CreatedAtUtc: DateTimeOffset

PlayerExternalIdentity
  Id: Guid
  PlayerId: Guid
  Provider: AuthProvider    (Google | Apple)
  ProviderSubjectId: string
  LinkedAtUtc: DateTimeOffset

AuthProvider: enum { Google, Apple }
LevelConfidence: enum { None, Low, Medium, High }
```

`Level`, `MatchesPlayed`, `LevelConfidence` y el futuro histórico son conceptos separados, conforme a la constitución (§29 / plan §6); M1 solo produce el primero de ellos vía encuesta y dos son placeholders (`MatchesPlayed = 0`, sin histórico) hasta que existan partidos.

No se modela todavía perfil de "otro jugador" ni ningún dato que dependa de un partido compartido (fuera de alcance del proposal).

## Encuesta de nivel inicial

Consciente de que el plan (§5) difiere el contenido exacto ("la encuesta exacta se definirá posteriormente"), M1 fija una versión mínima y explícitamente reemplazable, sin pretender ser el algoritmo definitivo (`RatingEngine`, fuera del MVP):

- Tres preguntas cerradas de opción única: años jugando pádel, frecuencia semanal actual, autopercepción de nivel (`Principiante`/`Intermedio`/`Avanzado`/`Competitivo`).
- Cada opción tiene una puntuación fija predefinida; la suma se normaliza a la escala `1.0–5.0` con un paso de `0.1`, mediante `InitialLevelEstimator` (servicio de dominio puro, sin dependencias de infraestructura) para que sustituirlo por el `RatingEngine` futuro no toque el contrato de la API.
- El resultado siempre fija `LevelConfidence = Low`: una encuesta autodeclarada no da confianza alta; subir la confianza depende de evidencia de partidos jugados, fuera de M1.
- `POST /api/players/me/level-survey` recibe las tres respuestas, guarda `Level`/`LevelConfidence` en el `Player` autenticado y los devuelve. Repetir la encuesta sobrescribe el resultado anterior (no hay histórico de encuestas en el MVP).
- **`DateOfBirth` es obligatoria para completar la encuesta.** Decisión añadida a posteriori (2026-09-07): la edad se usará para mejorar la calidad de los emparejamientos (fuera de M1, pero la señal debe capturarse ya). `Player.CompleteLevelSurvey` (dominio) rechaza la operación con `PlayerProfileIncompleteException` si `DateOfBirth` es `null`; el endpoint la traduce a 400 `ProblemDetails`. El cliente debe completar `PUT /api/players/me` con la fecha de nacimiento antes de poder enviar la encuesta. `CityOrZone` no se ve afectado por esta decisión: sigue siendo opcional, sin bloqueo backend.

## Persistencia

- Nueva migración `AddPlayers` sobre `PadelMatchDbContext`: tablas `Players` y `PlayerExternalIdentities`, índice único en `(Provider, ProviderSubjectId)`.
- `DateOfBirth` como `date`; el resto de instantes (`CreatedAtUtc`, `LinkedAtUtc`) como `timestamp with time zone` en UTC, conforme a la política temporal de la constitución. `TimeProvider.System` (ya registrado en M0) sigue siendo la fuente de esos instantes.
- No se añade todavía ninguna tabla de Match/Club: sigue sin anticiparse el modelo de milestones posteriores, igual que en M0.

## API y contratos

```text
POST /api/auth/google          { idToken }                    -> { sessionToken, player }
POST /api/auth/apple           { idToken, displayName? }       -> { sessionToken, player }
GET  /api/players/me           [autenticado]                   -> PlayerProfileResponse
PUT  /api/players/me           [autenticado] { cityOrZone?, dateOfBirth?, photoUrl? } -> PlayerProfileResponse
POST /api/players/me/level-survey [autenticado] { yearsPlaying, weeklyFrequency, selfPerceivedLevel } -> PlayerProfileResponse (400 si falta dateOfBirth)
```

- Sustituye a la referencia orientativa `POST /api/auth/register` del plan (§37): el registro ya no es una acción separada, es la primera vez que un login social tiene éxito.
- `PlayerProfileResponse` expone `id`, `displayName`, `cityOrZone`, `dateOfBirth`, `photoUrl`, `level`, `levelConfidence`, `matchesPlayed`; no expone `email` de vuelta en cada respuesta más allá de lo estrictamente necesario para no filtrar datos del proveedor sin necesidad.
- `PUT /api/players/me` solo permite editar los campos listados; `DisplayName` no es editable en M1 (viene del proveedor); ampliarlo es una decisión de una spec posterior si se pide.
- Todos los endpoints de `Player` documentan sus respuestas 401 y 400 vía `ProblemDetails`, igual que M0 hizo con `/health/ready` y 503; no se filtran detalles internos de verificación de token en el cuerpo de error.

## Verificación

Se sigue el patrón de M0 (`WebApplicationFactory` + PostgreSQL real), con una diferencia: los tests no pueden llamar a Google/Apple reales. `IExternalIdentityVerifier` se sustituye en pruebas por una implementación de prueba registrada vía `ConfigureServices` en el `WebApplicationFactory`, que valida tokens sintéticos deterministas en lugar de red real. Los tests cubren, con PostgreSQL real:

- Primer login con Google crea un `Player`; un segundo login con el mismo `sub` reconoce al mismo `Player` en vez de duplicarlo.
- Login con Apple sin `displayName` no falla; con `displayName` en el primer alta lo persiste.
- Un mismo email en Google y en Apple produce dos `Player` distintos (documenta la decisión de no fusionar).
- `GET/PUT /api/players/me` sin token válido devuelve 401; con token válido devuelve/actualiza el perfil esperado.
- La encuesta de nivel calcula el `Level` esperado para combinaciones de respuestas conocidas y siempre deja `LevelConfidence = Low`.
- Un `Player` sin encuesta completada expone `level = null`, `levelConfidence = None`, nunca un valor inventado.
- La encuesta se rechaza con 400 si `DateOfBirth` no está establecida, y se completa con éxito una vez que el jugador la ha fijado vía `PUT /api/players/me`. `PadelMatch.Domain.Tests` cubre además la invariante directamente sobre `Player.CompleteLevelSurvey`, sin pasar por HTTP.

Se documentarán con `pastiche-rdd` al terminar `tasks.md`, igual que en M0.

## Alternativas y límites

- No se usa `ASP.NET Core Identity`: no hay contraseñas, roles ni recuperación de cuenta que justifiquen su superficie.
- No se implementa refresh token en M1; se acepta como límite documentado, no como omisión silenciosa.
- No se fusionan identidades por email entre proveedores; vincular cuentas manualmente queda para una spec posterior si se necesita.
- La encuesta de nivel es deliberadamente sencilla y sustituible; no es el `RatingEngine` definitivo (fuera del MVP según constitución y plan §40).
- Geolocalización real, perfil de otros jugadores e histórico siguen fuera, como fija el proposal.
