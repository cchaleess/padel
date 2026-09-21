# Verification — m1-mobile-auth

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 1673
- cwd: .
- ran_at: 2026-09-13T08:20:16Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 5615
- cwd: .
- ran_at: 2026-09-13T08:20:18Z

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

Tiempo transcurrido 00:00:04.88
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 10260
- cwd: .
- ran_at: 2026-09-13T08:20:23Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    12, Omitido:     0, Total:    12, Duración: 6 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    11, Omitido:     0, Total:    11, Duración: 75 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [FAIL] `npm ci --no-audit --no-fund`

- exit_code: -4048
- duration_ms: 3714
- cwd: mobile
- ran_at: 2026-09-13T08:20:34Z

```

```

```
npm error code EPERM
npm error syscall unlink
npm error path C:\repos\padel\mobile\node_modules\fb-dotslash\bin\windows\dotslash.exe
npm error errno -4048
npm error [Error: EPERM: operation not permitted, unlink 'C:\repos\padel\mobile\node_modules\fb-dotslash\bin\windows\dotslash.exe'] {
npm error   errno: -4048,
npm error   code: 'EPERM',
npm error   syscall: 'unlink',
npm error   path: 'C:\\repos\\padel\\mobile\\node_modules\\fb-dotslash\\bin\\windows\\dotslash.exe'
npm error }
npm error
npm error The operation was rejected by your operating system.
npm error It's possible that the file was already in use (by a text editor or antivirus),
npm error or that you lack permissions to access it.
npm error
npm error If you believe this might be a permissions issue, please double-check the
npm error permissions of the file and its containing directories, or try running
npm error the command again as root/Administrator.
npm error A complete log of this run can be found in: C:\Users\cmp\AppData\Local\npm-cache\_logs\2026-09-13T08_20_34_449Z-debug-0.log
```

## [FAIL] `npm run typecheck`

- exit_code: 1
- duration_ms: 667
- cwd: mobile
- ran_at: 2026-09-13T08:20:37Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

```
"tsc" no se reconoce como un comando interno o externo,
programa o archivo por lotes ejecutable.
```

## [PASS] `npx --yes expo-doctor`

- exit_code: 0
- duration_ms: 4659
- cwd: mobile
- ran_at: 2026-09-13T08:20:38Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
```

```
Error: npx expo config --json --full exited with non-zero code: 1
```
