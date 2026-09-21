# Verification — m2-mobile-clubs

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 1107
- cwd: .
- ran_at: 2026-09-21T21:31:11Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 2899
- cwd: .
- ran_at: 2026-09-21T21:31:12Z

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

Tiempo transcurrido 00:00:02.34
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 12046
- cwd: .
- ran_at: 2026-09-21T21:31:15Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    20, Omitido:     0, Total:    20, Duración: 8 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    28, Omitido:     0, Total:    28, Duración: 71 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `npm ci --no-audit --no-fund`

- exit_code: 0
- duration_ms: 52679
- cwd: mobile
- ran_at: 2026-09-21T21:31:27Z

```

added 525 packages in 52s
```

```
npm warn deprecated uuid@7.0.3: uuid@10 and below is no longer supported.  For ESM codebases, update to uuid@latest.  For CommonJS codebases, use uuid@11 (but be aware this version will likely be deprecated in 2028).
```

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 4243
- cwd: mobile
- ran_at: 2026-09-21T21:32:19Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [PASS] `npx --yes expo-doctor`

- exit_code: 0
- duration_ms: 15764
- cwd: mobile
- ran_at: 2026-09-21T21:32:24Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
Running 21 checks on your project...
21/21 checks passed. No issues detected!
```
