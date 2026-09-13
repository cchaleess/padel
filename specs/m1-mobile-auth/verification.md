# Verification — m1-mobile-auth

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 5537
- cwd: .
- ran_at: 2026-09-13T08:55:49Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 3420
- cwd: .
- ran_at: 2026-09-13T08:55:55Z

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

Tiempo transcurrido 00:00:02.67
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 12503
- cwd: .
- ran_at: 2026-09-13T08:55:58Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    12, Omitido:     0, Total:    12, Duración: 8 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    11, Omitido:     0, Total:    11, Duración: 102 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `npm ci --no-audit --no-fund`

- exit_code: 0
- duration_ms: 199565
- cwd: mobile
- ran_at: 2026-09-13T08:56:11Z

```

added 493 packages in 3m
```

```
npm warn deprecated uuid@7.0.3: uuid@10 and below is no longer supported.  For ESM codebases, update to uuid@latest.  For CommonJS codebases, use uuid@11 (but be aware this version will likely be deprecated in 2028).
```

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 7697
- cwd: mobile
- ran_at: 2026-09-13T08:59:31Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [PASS] `npx --yes expo-doctor`

- exit_code: 0
- duration_ms: 27501
- cwd: mobile
- ran_at: 2026-09-13T08:59:38Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
Running 21 checks on your project...
21/21 checks passed. No issues detected!
```
