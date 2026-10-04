# Verification — m6-mobile-quality-rules

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 3061
- cwd: mobile
- ran_at: 2026-10-04T15:02:14Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [PASS] `npx expo-doctor`

- exit_code: 0
- duration_ms: 8823
- cwd: mobile
- ran_at: 2026-10-04T15:02:17Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
Running 21 checks on your project...
21/21 checks passed. No issues detected!
```

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 2901
- cwd: .
- ran_at: 2026-10-04T15:02:26Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 6524
- cwd: .
- ran_at: 2026-10-04T15:02:28Z

```
  PadelMatch.Domain -> C:\repos\padel\backend\PadelMatch.Domain\bin\Release\net10.0\PadelMatch.Domain.dll
  PadelMatch.Application -> C:\repos\padel\backend\PadelMatch.Application\bin\Release\net10.0\PadelMatch.Application.dll
  PadelMatch.Infrastructure -> C:\repos\padel\backend\PadelMatch.Infrastructure\bin\Release\net10.0\PadelMatch.Infrastructure.dll
  PadelMatch.Api -> C:\repos\padel\backend\PadelMatch.Api\bin\Release\net10.0\PadelMatch.Api.dll
  PadelMatch.Api.Tests -> C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll
  PadelMatch.Domain.Tests -> C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll

Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:05.91
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 62375
- cwd: .
- ran_at: 2026-10-04T15:02:35Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    82, Omitido:     0, Total:    82, Duración: 55 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    52, Omitido:     0, Total:    52, Duración: 180 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`

- exit_code: 0
- duration_ms: 7462
- cwd: .
- ran_at: 2026-10-04T15:03:37Z

```
No changes have been made to the model since the last migration.
```

## [PASS] `powershell -NoProfile -Command "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 }"`

- exit_code: 0
- duration_ms: 581
- cwd: .
- ran_at: 2026-10-04T15:03:45Z

```
 = ; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref], [ref]); if () { ; exit 1 }
```
