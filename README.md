# PadelMatch

Base del MVP de una aplicación móvil para organizar partidos de pádel. M0 incorpora la solución .NET, PostgreSQL/EF Core, migraciones, OpenAPI y una pantalla inicial Expo. M1 añade `Player`: login social con Google y Apple, perfil propio y la encuesta de nivel inicial (backend en `m1-players`, pantallas de mobile en `m1-mobile-auth`). M2 añade clubes y pistas de solo lectura: descubrimiento por cercanía, búsqueda, detalle y huecos disponibles, más la aportación de clubes por jugadores. M3 añade la creación de partidos a partir de un hueco de pista disponible (backend en `m3-match-creation`, pantallas de mobile en `m3-mobile-matches`). M4 añade el feed de descubrimiento de partidos (backend en `m4-discovery`, pantallas de mobile en `m4-mobile-feed`). M5 añade la confirmación de plaza con pago simulado (backend en `m5-confirmation`).

El alcance y las decisiones están en [la constitución](specs/CONSTITUTION.md), y en las specs de cada milestone: [M0](specs/m0-foundation/proposal.md) ([diseño](specs/m0-foundation/design.md), [tareas](specs/m0-foundation/tasks.md)), [M1 backend](specs/m1-players/proposal.md) ([diseño](specs/m1-players/design.md), [tareas](specs/m1-players/tasks.md)), [M1 mobile](specs/m1-mobile-auth/proposal.md) ([diseño](specs/m1-mobile-auth/design.md), [tareas](specs/m1-mobile-auth/tasks.md)), [M2 backend](specs/m2-clubs-courts/proposal.md) ([diseño](specs/m2-clubs-courts/design.md), [tareas](specs/m2-clubs-courts/tasks.md)), [M2 mobile](specs/m2-mobile-clubs/proposal.md) ([diseño](specs/m2-mobile-clubs/design.md), [tareas](specs/m2-mobile-clubs/tasks.md)), [M3 backend](specs/m3-match-creation/proposal.md) ([diseño](specs/m3-match-creation/design.md), [tareas](specs/m3-match-creation/tasks.md)), [M3 mobile](specs/m3-mobile-matches/proposal.md) ([diseño](specs/m3-mobile-matches/design.md), [tareas](specs/m3-mobile-matches/tasks.md)), [M4 backend](specs/m4-discovery/proposal.md) ([diseño](specs/m4-discovery/design.md), [tareas](specs/m4-discovery/tasks.md)), [M4 mobile](specs/m4-mobile-feed/proposal.md) ([diseño](specs/m4-mobile-feed/design.md), [tareas](specs/m4-mobile-feed/tasks.md)) y [M5 backend](specs/m5-confirmation/proposal.md) ([diseño](specs/m5-confirmation/design.md), [tareas](specs/m5-confirmation/tasks.md)).

## Prerrequisitos

- SDK .NET 10.0.400 o parche compatible de esa banda (`global.json`).
- Node.js 24 LTS y npm.
- PostgreSQL 18.6: mediante Docker Compose v2 o binarios locales.
- Para ejecutar en móvil: Expo Go compatible con SDK 57 y un dispositivo Android/iOS; alternativamente un emulador Android o iOS Simulator en macOS.

Los comandos siguientes parten de la raíz del repositorio. Los ejemplos de variables de entorno usan PowerShell; en Bash se utiliza `export NOMBRE='valor'`.

## Base de datos

### Docker Compose

```powershell
Copy-Item .env.example .env
docker compose up -d --wait postgres
```

El puerto es `127.0.0.1:5432`, la base y el usuario son `padelmatch`. La contraseña de ejemplo `padelmatch_local_only` es exclusivamente para desarrollo local. El volumen de Docker conserva los datos al parar el contenedor:

```powershell
docker compose stop postgres
```

`.env` configura Docker Compose. **ASP.NET Core no lee ese archivo**: si cambias usuario, contraseña, base o puerto, cambia también `ConnectionStrings__PadelMatch` para la API y los comandos EF. La configuración Development coincide con los valores de ejemplo.

### Windows sin Docker

Descarga el ZIP de PostgreSQL 18.6 para Windows x64 desde los [binarios EDB enlazados por PostgreSQL](https://www.postgresql.org/download/windows/). Extráelo bajo `.local/`, de forma que exista `.local/pgsql/bin/pg_ctl.exe`.

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
# Sustituye la ruta por la del ZIP que descargaste.
Expand-Archive -LiteralPath 'C:/ruta/postgresql-18.6-1-windows-x64-binaries.zip' -DestinationPath .local
./scripts/Start-LocalPostgres.ps1
```

El script inicializa `.local/postgres-data` con las mismas credenciales de desarrollo, escucha solo en `127.0.0.1` y crea `padelmatch` si falta. Se puede volver a ejecutar sin reinicializar los datos. No instala un servicio del sistema. No ejecutes a la vez esta instancia y Compose en el mismo puerto; el script admite `-Port 5433` y `-BinariesPath` si necesitas otra ubicación.

Para detener esta instancia preservando sus datos:

```powershell
./.local/pgsql/bin/pg_ctl.exe stop -D .local/postgres-data -m fast -w
```

## Backend y migraciones

```powershell
dotnet restore PadelMatch.slnx
dotnet tool restore
dotnet build PadelMatch.slnx -c Release --no-restore

$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api
dotnet run --project backend/PadelMatch.Api --launch-profile http
```

La API escucha en `http://localhost:5080`. Desde otra terminal:

```powershell
Invoke-RestMethod http://localhost:5080/health/live
Invoke-RestMethod http://localhost:5080/health/ready
Invoke-RestMethod http://localhost:5080/openapi/v1.json
```

`/health/live` comprueba el proceso. `/health/ready` devuelve 200 cuando PostgreSQL es accesible y las migraciones están aplicadas, y 503 si falta la base o alguna migración. OpenAPI se publica solo en Development; puede abrirse directamente en un navegador o importarse en un cliente HTTP.

La migración `InitialFoundation` no crea tablas de negocio: establece `__EFMigrationsHistory` como punto de partida. `AddPlayers` (M1) crea `Players` y `PlayerExternalIdentities`, con índice único en `(Provider, ProviderSubjectId)`. `AddClubsAndCourts` (M2) crea `Clubs`, `Courts` y `CourtSlots`. `AddMatches` (M3) crea `Matches`, con índice único en `CourtSlotId` (backstop de la exclusividad del hueco ante concurrencia). `AddMatchSeats` (M5) crea `MatchSeats` (4 por partido), con índice único parcial en `(MatchId, HolderId) WHERE HolderId IS NOT NULL` (backstop de que un jugador no acabe con dos plazas activas en el mismo partido). Repetir `database update` sin cambios no añade otra entrada. La API no aplica migraciones automáticamente.

Para una futura modificación real del modelo:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add NombreDelCambio --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --output-dir Persistence/Migrations
dotnet ef database update --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api
```

Para usar otra conexión, define esta variable antes de ejecutar API, migraciones o comprobaciones del modelo:

```powershell
$env:ConnectionStrings__PadelMatch = 'Host=localhost;Port=5433;Database=padelmatch;Username=padelmatch;Password=TU_PASSWORD;Timeout=5'
```

Fuera de Development es obligatorio proporcionar la conexión. No guardes credenciales reales en archivos versionados. El reloj del servidor y los instantes UTC rigen los plazos; la zona prevista de presentación es `Europe/Madrid`.

## Autenticación (M1)

El login es social: Google (Android/iOS) y Apple (iOS). La API no delega la sesión al proveedor: verifica el ID token que entrega el SDK nativo y emite su propio JWT (HMAC, 30 días) para el resto de llamadas. Requiere tres valores de configuración, con el mismo tratamiento que `ConnectionStrings__PadelMatch`: valor de desarrollo en `appsettings.Development.json`, obligatorios fuera de Development.

| Configuración | Significado |
| --- | --- |
| `Auth:Google:Audience` | Web Client ID de PadelMatch en Google Cloud Console (mismo id configurado como `serverClientId`/`webClientId` en el cliente móvil, en Android e iOS). |
| `Auth:Apple:Audience` | Bundle identifier de la app iOS. |
| `Auth:SessionSigningKey` | Clave simétrica con la que la API firma sus propios JWT de sesión. |

`appsettings.Development.json` trae valores de ejemplo; `Auth:Google:Audience` y `Auth:Apple:Audience` son placeholders (`REPLACE_WITH_...`) porque dependen de cuentas de Google Cloud/Apple Developer propias del proyecto — sustitúyelos por los reales para probar el login social de verdad. `Auth:SessionSigningKey` sí trae un valor local utilizable. Fuera de Development:

```powershell
$env:Auth__Google__Audience = 'TU_GOOGLE_WEB_CLIENT_ID.apps.googleusercontent.com'
$env:Auth__Apple__Audience = 'com.tuempresa.padelmatch'
$env:Auth__SessionSigningKey = 'TU_CLAVE_DE_FIRMA'
```

## Clubes y pistas (M2)

Catálogo de solo lectura: un jugador autenticado descubre clubes cercanos, busca por nombre, consulta el detalle de un club (con sus pistas) y los huecos de pista disponibles. También puede aportar un club nuevo (queda marcado como no verificado). No hay todavía gestión de clubes ni creación de pistas/huecos pensada para producción — ver [design.md](specs/m2-clubs-courts/design.md).

```text
GET  /api/clubs/nearby   [autenticado] ?lat?&lng?&cityOrZone?
GET  /api/clubs/search   [autenticado] ?q
GET  /api/clubs/{id}     [autenticado]
GET  /api/clubs/{id}/slots [autenticado] ?from?&to?&courtId?
POST /api/clubs          [autenticado] { name, address, cityOrZone? }
```

`GET /api/clubs/nearby` ordena por distancia (fórmula de Haversine, sin PostGIS) cuando se indican `lat`/`lng`; si no, usa `cityOrZone` (del query o, por defecto, el del perfil del jugador) como alternativa; sin ninguna señal, ordena por nombre — nunca falla por falta de ubicación. `GET /api/clubs/{id}/slots` usa una ventana de 14 días por defecto si no se indican `from`/`to`, y solo devuelve huecos `Available`.

### Datos de desarrollo

No hay panel de gestión de clubes en el MVP: los clubes/pistas/huecos de ejemplo se cargan mediante `DevelopmentClubSeeder`, que se ejecuta al arrancar la API solo si **ambas** condiciones se cumplen: `ASPNETCORE_ENVIRONMENT=Development` y `Development:SeedClubs=true` (ya activo en `appsettings.Development.json`). Es idempotente (no vuelve a sembrar si ya hay clubes) y **no es apto para producción** — el conjunto de datos (2 clubes oficiales con coordenadas reales, sus pistas y huecos de los próximos días) es deliberadamente mínimo y puede cambiar sin aviso. Los tests de integración desactivan explícitamente esta variable para no depender de estos datos.

## Creación de partidos (M3)

Un jugador autenticado crea un `Match` a partir de un `CourtSlot` `Available` concreto, convirtiéndose en organizador. El tipo (`Competitive`/`Friendly`) no cambia después; horario y duración son los del hueco elegido, sin volver a pedirlos. Crear el partido **no ocupa ninguna plaza** — ni siquiera la del organizador: esa confirmación (contra pago, aunque simulado en el MVP) es un flujo de M5 que esta spec no implementa. Ver [design.md](specs/m3-match-creation/design.md) para el detalle de exclusividad del hueco ante concurrencia.

```text
POST /api/matches      [autenticado] { courtSlotId, type, minLevel?, maxLevel?, minMatchesRequired?, note? }
GET  /api/matches/{id} [autenticado]
```

- `Competitive` exige que el organizador tenga puntaje (`Player.Level` no nulo) y un rango `minLevel`/`maxLevel` que contenga ese nivel (snapshot, `OrganizerLevelAtCreation`); `minMatchesRequired` es opcional. `Friendly` no exige nada de eso; ambos tipos aceptan una `note` opcional (máx. 500 caracteres).
- El `CourtSlot` pasa a `Booked` al crear el partido. Dos intentos concurrentes sobre el mismo hueco no pueden tener éxito ambos: la comprobación de estado es la vía rápida, y un índice único sobre `Matches.CourtSlotId` es el backstop real ante la carrera (ambos casos responden 409).

## Descubrimiento de partidos (M4)

Un jugador autenticado pide el feed de partidos a los que puede unirse: partidos `Open` cuyo hueco todavía no ha empezado, agrupados en `forYou` (`Friendly` + `Competitive` compatible con su `Level`) y `outOfRange` (`Competitive` incompatible, o cualquier `Competitive` si el jugador no tiene `Level` todavía). Mismo patrón de proximidad que `GET /api/clubs/nearby`. Sin acción de unirse todavía (M5) ni calidad estimada/contador de confirmados (sin esos datos aún) — ver [design.md](specs/m4-discovery/design.md).

```text
GET /api/matches/feed?lat=&lng=&cityOrZone=   [autenticado]
```

- Cada grupo se ordena por proximidad (coordenadas del dispositivo, o `cityOrZone` del jugador si no hay coordenadas) y luego por horario.
- La card del feed trae solo club, pista, horario, tipo y rango de nivel si aplica; el detalle completo sigue siendo `GET /api/matches/{id}`.

## Confirmación de plaza (M5)

Cada `Match` nace con 4 `MatchSeat`: la del organizador ya `Held` (crear lleva directamente al pago, ver [m5-mobile-confirmation](specs/m5-mobile-confirmation/proposal.md)) y las otras 3 `Available`. Nadie, ni el organizador, ocupa plaza hasta pagar. Un jugador retiene una plaza (`Held`, expira a los 5 minutos sin que haga falta ningún proceso en segundo plano: la expiración se resuelve perezosamente, en el momento en que alguien vuelve a intentar reclamar esa plaza), la confirma (pago simulado, éxito inmediato) o la suelta antes de que expire. Al confirmarse la cuarta, el `Match` pasa a `Full` y deja de aparecer en el feed. El feed tiene cuatro secciones: los partidos propios completos («Tienes estas partidas confirmadas»), los propios aún abiertos («Partidas pendientes de confirmación») y, después, los partidos a los que el jugador puede unirse (con al menos 1 confirmado, no organizados por él y sin plaza activa suya) en «Partidos para ti» y «Otros partidos cercanos». Ver [design.md](specs/m5-confirmation/design.md) para el mecanismo de concurrencia (`ExecuteUpdateAsync` condicionado, sin cargar-mutar-guardar).

```text
POST /api/matches/{id}/hold     [autenticado] → { heldUntilUtc }
POST /api/matches/{id}/confirm  [autenticado]
POST /api/matches/{id}/release  [autenticado]
```

`GET /api/matches/{id}` incluye `confirmedSeats` (solo `Confirmed`; las `Held` de otros no se muestran) y `mySeat` (`{ status, heldUntilUtc }` o `null`). Los items del feed incluyen `confirmedSeats`.

- Un jugador no puede tener dos plazas activas (`Held` no caducada, o `Confirmed`) en el mismo partido; un índice único parcial en `MatchSeats` es el backstop ante concurrencia.
- Retener/confirmar/soltar una plaza que ya no está en el estado esperado (agotadas, caducada reclamada por otro, no es la suya) devuelve 409, no un error genérico.
- Sin pasarela de pago real todavía (simulado, arquitectura preparada para sustituirlo); sin abandonar una plaza ya `Confirmed` (depende de la lista de espera, M7).

## Reglas de calidad (M6)

En un partido competitivo, un jugador entra directamente si tiene nivel, su nivel está dentro del rango y ha jugado al menos el mínimo de partidos. Si no cumple algún criterio, retener plaza devuelve 403 con los motivos (`shortfalls`) y puede pedir acceso. La solicitud se aprueba cuando la aprueban **todos** los jugadores confirmados en ese momento; un solo rechazo la rechaza y es definitivo. Aprobada, el jugador se une como cualquiera: retiene y paga. Los amistosos no tienen criterios. Ver [design.md](specs/m6-quality-rules/design.md).

```text
POST /api/matches/{id}/exception-requests                      [autenticado] → 201
POST /api/matches/{id}/exception-requests/{playerId}/approve   [confirmado]  → { status }
POST /api/matches/{id}/exception-requests/{playerId}/reject    [confirmado]  → { status }
GET  /api/activity                                             [autenticado] → { toVote, myRequests }
```

- Los votos a una solicitud se serializan con un bloqueo de fila (`SELECT ... FOR UPDATE`). Sin él, las dos últimas aprobaciones simultáneas podrían dejarla pendiente para siempre; hay un test que lo comprueba.
- El detalle del partido incluye `myAccess` (si puedo unirme directamente, por qué no y el estado de mi solicitud) y, solo para confirmados, `pendingRequests`.
- `MatchesPlayed` vale 0 hasta M10 (resultados), así que un competitivo con mínimo de partidos exige solicitud a todo el mundo por ahora.
- La solicitud guarda la plaza desde la que se pidió (`{ position }`), sin reservarla. Si el partido se completa mientras está pendiente, pasa a `Expired`.

## Simular otros jugadores (solo desarrollo)

Casi todo lo que se prueba desde M5 necesita que *otro* jugador actúe. Con la API en `Development` existe `POST /api/dev/session { name }`, que entra como un jugador ficticio (se crea la primera vez y se reutiliza por nombre). Fuera de `Development` la ruta no existe. Ver [specs/dev-player-simulation](specs/dev-player-simulation/design.md).

`scripts/dev-sim.ps1` la usa para actuar a través de los endpoints reales, sin escribir en la base de datos:

```powershell
.\scripts\dev-sim.ps1 create-match                 # Ana crea un amistoso en el primer hueco libre y lo paga
.\scripts\dev-sim.ps1 create-match -Player Carla
.\scripts\dev-sim.ps1 join <matchId> -Count 2       # 2 jugadores ficticios se unen y pagan
.\scripts\dev-sim.ps1 hold <matchId> -Player Bruno -Position 2  # retiene la plaza 2 (pareja B) sin pagar (5 min)
.\scripts\dev-sim.ps1 create-match -Player Diego -Competitive -MinLevel 2.3 -MaxLevel 3.3
.\scripts\dev-sim.ps1 request <matchId> -Player Ana             # Ana pide acceso (fuera de criterios)
.\scripts\dev-sim.ps1 vote <matchId> [-Reject] [-Player Bruno]  # los ficticios confirmados votan las solicitudes
```

En un build de desarrollo de la app, la pantalla de login muestra además «Entrar como jugador de prueba» (Ana, Bruno, Carla, Diego) para ver la app desde la perspectiva de otro jugador.

## Cliente Expo

```powershell
npm --prefix mobile ci
npm --prefix mobile start
```

Escanea el QR desde Expo Go con móvil y equipo en la misma red. En iPhone puedes abrirlo desde la cámara; en Android, desde Expo Go. La app muestra una pantalla inicial de PadelMatch.

Otros comandos:

```powershell
npm --prefix mobile run android  # Requiere emulador Android o dispositivo compatible conectado.
npm --prefix mobile run ios      # Requiere macOS con iOS Simulator.
npm --prefix mobile run web      # Vista web local para desarrollo.
npm --prefix mobile run typecheck
npm --prefix mobile run export   # Genera bundles Android, iOS y web en mobile/dist.
```

Windows permite trabajar con Expo Go en un iPhone físico, pero no ejecutar iOS Simulator. Exportar bundles nativos comprueba el código JavaScript; no acredita ejecución en dispositivo ni genera por sí solo un APK/IPA.

### Login de Google (M1, development build de Android)

Expo Go **no sirve** para probar el login de Google: no soporta esquemas de URL personalizados y el SDK nativo (`@react-native-google-signin/google-signin`) exige un development build (ver [specs/m1-mobile-auth/design.md](specs/m1-mobile-auth/design.md)). Este flujo usa en su lugar un development build local de Android, sin EAS ni Mac.

Prerrequisitos adicionales:
- Android SDK Platform Tools (`adb`) y, para compilar, `platforms;android-36.1` + `build-tools;36.1.0` (o versiones equivalentes) — no hace falta instalar Android Studio completo.
- Un dispositivo Android físico con depuración USB activada, o un emulador (AVD) si prefieres usar Android Studio.
- Un cliente OAuth de tipo **Android** en el mismo proyecto de Google Cloud del Web Client ID ya usado por la API (paquete `com.padelmatch.app` + huella SHA-1 del keystore de depuración — obtenla con `cd mobile/android && ./gradlew signingReport` tras el primer `npx expo prebuild --platform android`).

Configuración:

```powershell
Copy-Item mobile/.env.example mobile/.env
# Rellena en mobile/.env:
#   EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID = el mismo Web Client ID que Auth:Google:Audience
#   EXPO_PUBLIC_API_BASE_URL = http://10.0.2.2:5080 (emulador) o http://<IP-LAN-del-PC>:5080 (dispositivo físico)
```

Para que la API sea accesible desde el dispositivo/emulador, arráncala escuchando en todas las interfaces (y permite el puerto 5080 en el Firewall de Windows si te lo pide la primera vez):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/PadelMatch.Api --launch-profile http --urls http://0.0.0.0:5080
```

Con el dispositivo conectado (`adb devices` debe listarlo), desde dentro de `mobile/` (el comando no admite `--prefix` desde la raíz):

```powershell
cd mobile
npx expo run:android
```

Este comando compila e instala el development build; las siguientes veces basta con `npm --prefix mobile start` y reabrir la app instalada (usa Metro igual que Expo Go).

En dispositivo físico por USB, si al abrir la app aparece "Unable to load script. Make sure you're running Metro", falta reenviar el puerto de Metro:

```powershell
adb reverse tcp:8081 tcp:8081
```

Con dispositivo físico, además, el móvil necesita **WiFi activo y en la misma red que el PC** (no basta con datos móviles) para poder llegar a `EXPO_PUBLIC_API_BASE_URL`; si no, el login se queda colgado tras la pantalla de Google porque la llamada a la API nunca responde.

### Clubes y navegación (M2, development build de Android)

Añade navegación por pestañas ([React Navigation](https://reactnavigation.org/): `@react-navigation/native`, `@react-navigation/bottom-tabs`, `@react-navigation/native-stack`, más `react-native-screens`, `react-native-safe-area-context` y `react-native-gesture-handler`) y geolocalización opcional (`expo-location`, con el plugin correspondiente y su texto de permiso ya añadidos a `mobile/app.json`). No requiere configuración adicional en `mobile/.env`: reutiliza `EXPO_PUBLIC_API_BASE_URL`.

Cuatro pestañas: `Partidos` (con selector interno `Partidos`/`Clubes`), `Crear` y `Actividad` (placeholders), `Perfil` (sin cambios respecto a `m1-mobile-auth`). Dentro de `Clubes`:

- Lista de clubes cercanos (si se concede el permiso de ubicación) o por ciudad/zona/nombre (si se deniega); buscador con debounce que filtra por nombre.
- Detalle de club con la lista de horarios disponibles de todas sus pistas (ordenada por hora y luego por duración; la pista se muestra como dato secundario de cada hueco).
- "Aportar club" (nombre y dirección obligatorios) — el club queda marcado como no verificado y aparece luego en la búsqueda.

Para probarlo manualmente, con la API y Metro arrancados y el dispositivo conectado: deniega el permiso de ubicación desde Ajustes del sistema para comprobar que la lista sigue mostrando clubes sin error, y usa el seed de desarrollo de `m2-clubs-courts` para probar la búsqueda por nombre.

### Crear partido (M3, development build de Android)

La pestaña `Crear` deja de ser un placeholder: monta el mismo `ClubsStackNavigator` que `Partidos`→`Clubes` (segunda instancia, con su propio estado de navegación), así que elegir club→horario disponible es idéntico desde cualquiera de los dos puntos de entrada. El detalle de club ya no obliga a elegir pista primero: lista directamente los horarios disponibles de todas las pistas del club. Tocar un hueco disponible — ahora accionable en ambos sitios, ya no es de solo lectura — lleva al formulario de creación, sin ningún tipo preseleccionado (`Amistoso`/`Competitivo`; `Competitivo` aparece deshabilitado con una nota si el jugador no ha completado la encuesta de nivel, y el botón de crear permanece deshabilitado hasta elegir uno). Al crear, la app navega al detalle del partido (club, pista, horario, tipo, y rango/mínimo/nota si aplica), sin sugerir en ningún momento que el jugador ya ocupa una plaza — esa confirmación es un flujo posterior (M5).

No requiere configuración adicional en `mobile/.env`. Límite conocido y documentado en [design.md](specs/m3-mobile-matches/design.md): al reutilizar el mismo árbol de pantallas bajo `Crear`, su primera pantalla sigue titulada "Clubes" y conserva el botón de aportar club, en vez de un título ajustado a "elegir dónde crear un partido".

### Feed de partidos (M4, development build de Android)

La pestaña `Partidos` arranca ahora en su propio segmento (antes `Clubes` por defecto): al entrar, el jugador ve directamente el feed de `GET /api/matches/feed`, sin tocar nada — mismo permiso de ubicación opcional que el catálogo de clubes. Dos secciones, "Partidos para ti" y "Otros partidos cercanos" (se omite la que esté vacía); cada card muestra club, horario y tipo, y el rango de nivel si es `Competitive`. El feed se recarga solo al entrar/volver a la pestaña, y también con el gesto de deslizar hacia abajo. Tocar una card navega al detalle ya existente del partido (`m3-mobile-matches`), reutilizado tal cual.

No requiere configuración adicional en `mobile/.env`. Sin acción de unirse todavía (M5), sin calidad estimada ni contador de confirmados (el backend no los expone, ver [m4-discovery](specs/m4-discovery/design.md)).

### Confirmación de plaza (M5, development build de Android)

El detalle de un partido muestra «N/4 confirmados» y una acción según tu plaza: «Unirme», «Continuar pago», «Tienes plaza confirmada» o «Partido completo». «Unirme» retiene una plaza y abre la pantalla de pago simulado con una cuenta atrás hasta el vencimiento que fija el servidor. «Pagar» confirma; «Cancelar», el botón atrás o el gesto de volver sueltan la plaza. Al crear un partido, el organizador llega directamente a esa pantalla. Las cards del feed muestran «N/4»; el feed empieza por los partidos propios (confirmados y pendientes de confirmación).

Para probar como otro jugador, usa `scripts/dev-sim.ps1` o el login de prueba (ver «Simular otros jugadores»).

### Reglas de calidad (M6, development build de Android)

En un competitivo que no te incluye, el detalle explica por qué y las plazas vacías dicen «Solicitar acceso». Tocar una envía la solicitud, que se muestra en esa plaza. Los confirmados votan desde el detalle o desde la pestaña **Actividad**, que lista lo que tienes que votar y tus propias solicitudes, con un contador en la pestaña. Aprobada, «Unirme» vuelve a las plazas libres; rechazada o expirada, el detalle lo dice. El feed agrupa los partidos no unibles directamente bajo «Requieren aprobación».

## Verificación

Con PostgreSQL arrancado:

```powershell
dotnet test PadelMatch.slnx -c Release --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api
npm --prefix mobile run typecheck
Push-Location mobile
npx expo install --check
npx expo-doctor
Pop-Location
npm --prefix mobile run export
```

Las pruebas backend usan PostgreSQL real. Crean bases temporales `padelmatch_test_<identificador>` y eliminan únicamente esas bases al terminar; no migran ni borran `padelmatch`. El usuario de pruebas necesita permisos para crear bases. Por defecto usan las credenciales locales anteriores. Para otra instancia:

```powershell
$env:PADELMATCH_TEST_CONNECTION = 'Host=localhost;Port=5433;Database=postgres;Username=padelmatch;Password=TU_PASSWORD;Timeout=5'
```

Se verifica migración repetible, readiness antes/después de migrar, indisponibilidad de base, liveness independiente y contrato OpenAPI (M0); alta/reconocimiento de `Player` por proveedor, no fusión de cuentas entre proveedores con el mismo email, autorización 401/200 de `/api/players/me*` y el cálculo de la encuesta de nivel (M1); orden por distancia/ciudad-zona/nombre de `GET /api/clubs/nearby`, búsqueda por nombre, detalle, filtrado de huecos por rango/pista/disponibilidad y aportación de clubes por jugadores (M2); creación de partidos `Friendly`/`Competitive`, snapshot del nivel del organizador, rechazo de rango inválido o de un `Competitive` sin puntaje, transición del `CourtSlot` a `Booked`, rechazo (409) de un segundo intento sobre el mismo hueco incluida una prueba de concurrencia real con `Task.WhenAll`, y `GET /api/matches/{id}` (M3); el feed de descubrimiento (`GET /api/matches/feed`): agrupación `forYou`/`outOfRange` según compatibilidad de nivel, sin `Level` todo `Competitive` cae fuera de rango, exclusión de partidos cuyo hueco ya empezó, y orden por coordenadas/`cityOrZone` (M4); y la confirmación de plaza (M5): 4 `MatchSeat` `Available` al crear un `Match`, retener/confirmar/soltar, rechazo de una segunda plaza del mismo jugador o de una ya retenida por otro, reutilización de una plaza `Held` caducada, transición a `Full` al confirmarse la cuarta, exclusión del feed al llegar a `Full`, y concurrencia real con `Task.WhenAll` sobre la última plaza disponible — con un `TimeProvider` controlable en los tests (`MutableTimeProvider`) para verificar la expiración de 5 minutos sin una espera real. Las pruebas de `PadelMatch.Api.Tests` sustituyen `IExternalIdentityVerifier` por un doble determinista (`FakeExternalIdentityVerifier`): no llaman a Google/Apple reales; también desactivan `Development:SeedClubs` para no depender de los datos de desarrollo de M2. `PadelMatch.Domain.Tests` cubre `InitialLevelEstimator` y `HaversineDistanceCalculator` sin necesitar PostgreSQL. Consulta el [informe RDD de M0](specs/m0-foundation/verification.md) y las [notas de revisión con evidencia visual y limitaciones](specs/m0-foundation/review-notes.md).

## Estructura

```text
backend/
  PadelMatch.Api/             API y composición (endpoints, autenticación JWT)
  PadelMatch.Application/     Contratos y casos de uso de aplicación
  PadelMatch.Domain/          Núcleo de dominio (Player, InitialLevelEstimator, Club/Court/CourtSlot, HaversineDistanceCalculator, Match, MatchSeat)
  PadelMatch.Infrastructure/  EF Core, PostgreSQL, migraciones, verificación de identidad externa y seed de desarrollo de clubes
  PadelMatch.Api.Tests/       Pruebas de integración (WebApplicationFactory + PostgreSQL real)
  PadelMatch.Domain.Tests/    Pruebas unitarias de dominio, sin dependencias externas
mobile/                      React Native + Expo
scripts/                     Herramientas locales de desarrollo
specs/                       Constitución y specs SDD
```

`.local/`, `.pastiche/`, credenciales, dependencias y resultados de compilación quedan excluidos de Git.
