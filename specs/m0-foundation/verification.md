# Verification — m0-foundation

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 855
- cwd: .
- ran_at: 2026-09-05T22:04:59Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet tool restore`

- exit_code: 0
- duration_ms: 275
- cwd: .
- ran_at: 2026-09-05T22:05:00Z

```
Se restauró la herramienta "dotnet-ef" (versión "10.0.11"). Comandos disponibles: dotnet-ef

Restauración realizada correctamente.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 1542
- cwd: .
- ran_at: 2026-09-05T22:05:00Z

```
  PadelMatch.Domain -> C:\repos\padelche\backend\PadelMatch.Domain\bin\Release\net10.0\PadelMatch.Domain.dll
  PadelMatch.Application -> C:\repos\padelche\backend\PadelMatch.Application\bin\Release\net10.0\PadelMatch.Application.dll
  PadelMatch.Infrastructure -> C:\repos\padelche\backend\PadelMatch.Infrastructure\bin\Release\net10.0\PadelMatch.Infrastructure.dll
  PadelMatch.Api -> C:\repos\padelche\backend\PadelMatch.Api\bin\Release\net10.0\PadelMatch.Api.dll
  PadelMatch.Api.Tests -> C:\repos\padelche\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll

Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:01.25
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 10316
- cwd: .
- ran_at: 2026-09-05T22:05:02Z

```
Serie de pruebas para C:\repos\padelche\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:     4, Omitido:     0, Total:     4, Duración: 8 s - PadelMatch.Api.Tests.dll (net10.0)
```

## [PASS] `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`

- exit_code: 0
- duration_ms: 1990
- cwd: .
- ran_at: 2026-09-05T22:05:12Z

```
No changes have been made to the model since the last migration.
```

## [PASS] `npm ci --no-audit --no-fund`

- exit_code: 0
- duration_ms: 37654
- cwd: mobile
- ran_at: 2026-09-05T22:05:14Z

```

added 490 packages in 37s
```

```
npm warn deprecated uuid@7.0.3: uuid@10 and below is no longer supported.  For ESM codebases, update to uuid@latest.  For CommonJS codebases, use uuid@11 (but be aware this version will likely be deprecated in 2028).
```

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 3042
- cwd: mobile
- ran_at: 2026-09-05T22:05:52Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [PASS] `npx expo install --check`

- exit_code: 0
- duration_ms: 12876
- cwd: mobile
- ran_at: 2026-09-05T22:05:55Z

```
Dependencies are up to date
```

## [PASS] `npx --yes expo-doctor`

- exit_code: 0
- duration_ms: 34098
- cwd: mobile
- ran_at: 2026-09-05T22:06:08Z

```
Running 21 checks on your project...
21/21 checks passed. No issues detected!
```

## [PASS] `npm run export -- --max-workers 2`

- exit_code: 0
- duration_ms: 38556
- cwd: mobile
- ran_at: 2026-09-05T22:06:42Z

```

> padelmatch-mobile@0.1.0 export
> expo export --platform all --max-workers 2

Starting Metro Bundler

Web Bundled 7261ms index.ts (183 modules)
Android Bundled 10540ms index.ts (580 modules)
iOS Bundled 13658ms index.ts (580 modules)

› web bundles (1):
_expo/static/js/web/index-4d7aedd65791125a476b8d553a0056e1.js (340KB)

› android bundles (1):
_expo/static/js/android/index-4a4ede0e9b984fbe6959a7eba4e07c4d.hbc (1.4MB)

› ios bundles (1):
_expo/static/js/ios/index-16ac0a21ec2d907a3b6cb40f25148235.hbc (1.4MB)

› Files (2):
index.html (1.2KB)
metadata.json (244B)

Exported: dist
```
