# Verification — dev-player-simulation

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 1500
- cwd: .
- ran_at: 2026-10-04T13:29:34Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 2937
- cwd: .
- ran_at: 2026-10-04T13:29:35Z

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

Tiempo transcurrido 00:00:02.35
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1 --filter FullyQualifiedName~DevSession`

- exit_code: 0
- duration_ms: 8237
- cwd: .
- ran_at: 2026-10-04T13:29:38Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:     3, Omitido:     0, Total:     3, Duración: 4 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.
Ninguna prueba coincide con el filtro de casos de prueba proporcionado "FullyQualifiedName~DevSession" en C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll

```

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 2371
- cwd: mobile
- ran_at: 2026-10-04T13:29:47Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [PASS] `powershell -NoProfile -Command "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 }"`

- exit_code: 0
- duration_ms: 325
- cwd: .
- ran_at: 2026-10-04T13:29:49Z

```
 = ; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref], [ref]); if () { ; exit 1 }
```
