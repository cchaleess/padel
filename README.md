# PadelMatch

Base del MVP de una aplicación móvil para organizar partidos de pádel. M0 incorpora la solución .NET, PostgreSQL/EF Core, migraciones, OpenAPI y una pantalla inicial Expo. Los flujos de jugadores y partidos se implementan en milestones posteriores.

El alcance y las decisiones están en [la constitución](specs/CONSTITUTION.md), [el proposal](specs/m0-foundation/proposal.md), [el diseño](specs/m0-foundation/design.md) y [las tareas de M0](specs/m0-foundation/tasks.md).

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

La migración `InitialFoundation` no crea tablas de negocio: establece `__EFMigrationsHistory` como punto de partida. Repetir `database update` sin cambios no añade otra entrada. La API no aplica migraciones automáticamente.

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

Se verifica migración repetible, readiness antes/después de migrar, indisponibilidad de base, liveness independiente y contrato OpenAPI. Consulta el [informe RDD generado](specs/m0-foundation/verification.md) y las [notas de revisión con evidencia visual y limitaciones](specs/m0-foundation/review-notes.md).

## Estructura

```text
backend/
  PadelMatch.Api/             API y composición
  PadelMatch.Application/     Contratos de aplicación
  PadelMatch.Domain/          Núcleo de dominio, todavía sin entidades
  PadelMatch.Infrastructure/  EF Core, PostgreSQL y migraciones
  PadelMatch.Api.Tests/       Pruebas de integración
mobile/                      React Native + Expo
scripts/                     Herramientas locales de desarrollo
specs/                       Constitución y specs SDD
```

`.local/`, `.pastiche/`, credenciales, dependencias y resultados de compilación quedan excluidos de Git.
