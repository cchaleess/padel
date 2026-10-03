# Verification — m5-confirmation

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 1956
- cwd: .
- ran_at: 2026-10-03T23:30:28Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 3135
- cwd: .
- ran_at: 2026-10-03T23:30:30Z

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

Tiempo transcurrido 00:00:02.47
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 54810
- cwd: .
- ran_at: 2026-10-03T23:30:33Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    48, Omitido:     0, Total:    48, Duración: 47 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    41, Omitido:     0, Total:    41, Duración: 204 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`

- exit_code: 0
- duration_ms: 9928
- cwd: .
- ran_at: 2026-10-03T23:31:28Z

```
No changes have been made to the model since the last migration.
```
