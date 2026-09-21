# Verification — m2-mobile-clubs

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `dotnet restore PadelMatch.slnx --disable-parallel`

- exit_code: 0
- duration_ms: 1465
- cwd: .
- ran_at: 2026-09-21T21:29:14Z

```
  Determinando los proyectos que se van a restaurar...
  Todos los proyectos están actualizados para la restauración.
```

## [PASS] `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`

- exit_code: 0
- duration_ms: 6353
- cwd: .
- ran_at: 2026-09-21T21:29:16Z

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

Tiempo transcurrido 00:00:05.97
```

## [PASS] `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`

- exit_code: 0
- duration_ms: 11972
- cwd: .
- ran_at: 2026-09-21T21:29:22Z

```
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Api.Tests\bin\Release\net10.0\PadelMatch.Api.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    20, Omitido:     0, Total:    20, Duración: 8 s - PadelMatch.Api.Tests.dll (net10.0)
Serie de pruebas para C:\repos\padel\backend\PadelMatch.Domain.Tests\bin\Release\net10.0\PadelMatch.Domain.Tests.dll (.NETCoreApp,Version=v10.0)
1 archivos de prueba en total coincidieron con el patrón especificado.

Correctas! - Con error:     0, Superado:    28, Omitido:     0, Total:    28, Duración: 64 ms - PadelMatch.Domain.Tests.dll (net10.0)
```

## [PASS] `npm ci --no-audit --no-fund`

- exit_code: 0
- duration_ms: 41896
- cwd: mobile
- ran_at: 2026-09-21T21:29:34Z

```

added 525 packages in 42s
```

```
npm warn deprecated uuid@7.0.3: uuid@10 and below is no longer supported.  For ESM codebases, update to uuid@latest.  For CommonJS codebases, use uuid@11 (but be aware this version will likely be deprecated in 2028).
```

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 2914
- cwd: mobile
- ran_at: 2026-09-21T21:30:16Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [FAIL] `npx --yes expo-doctor`

- exit_code: 1
- duration_ms: 6707
- cwd: mobile
- ran_at: 2026-09-21T21:30:19Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
Running 21 checks on your project...
20/21 checks passed. 1 checks failed. Possible issues detected:
Use the --verbose flag to see more details about passed checks.

✖ Check that packages match versions required by installed Expo SDK

🔧 Patch version mismatches
package        expected  found    
expo           ~57.0.24  57.0.22  
expo-location  ~57.0.19  57.0.17  

Changelogs:
- expo-location → https://github.com/expo/expo/blob/sdk-57/packages/expo-location/CHANGELOG.md

2 packages out of date.
Advice:
Use 'npx expo install --check' to review and upgrade your dependencies.
To ignore specific packages, add them to "expo.install.exclude" in package.json. Learn more: https://expo.fyi/dependency-validation

```

```
1 check failed, indicating possible issues with the project.
```
