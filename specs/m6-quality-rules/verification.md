# Verification — m6-quality-rules

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 3177
- cwd: .
- ran_at: 2026-10-04T13:57:58Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 11668
- cwd: .
- ran_at: 2026-10-04T13:58:02Z

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

Tiempo transcurrido 00:00:10.75
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 45583
- cwd: .
- ran_at: 2026-10-04T13:58:13Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    80, Omitido:     0, Total:    80, Duración: 39 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    51, Omitido:     0, Total:    51, Duración: 113 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`

- exit_code: 0
- duration_ms: 3920
- cwd: .
- ran_at: 2026-10-04T13:58:59Z

```
No changes have been made to the model since the last migration.
```
