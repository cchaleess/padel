# Verification — m3-mobile-matches

<!-- Generado por `pastiche rdd verify` — no editar a mano.
     Volvé a correr el comando para refrescar. Fuente: checks.md. -->

## [PASS] `npm run typecheck`

- exit_code: 0
- duration_ms: 2533
- cwd: mobile
- ran_at: 2026-10-03T21:26:11Z

```

> padelmatch-mobile@0.1.0 typecheck
> tsc --noEmit

```

## [FAIL] `npx expo-doctor`

- exit_code: 1
- duration_ms: 8063
- cwd: mobile
- ran_at: 2026-10-03T21:26:14Z

```
env: load .env
env: export EXPO_PUBLIC_API_BASE_URL EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID
Running 21 checks on your project...
20/21 checks passed. 1 checks failed. Possible issues detected:
Use the --verbose flag to see more details about passed checks.

✖ Check that packages match versions required by installed Expo SDK

🔧 Patch version mismatches
package  expected  found    
expo     ~57.0.26  57.0.25  



1 package out of date.
Advice:
Use 'npx expo install --check' to review and upgrade your dependencies.
To ignore specific packages, add them to "expo.install.exclude" in package.json. Learn more: https://expo.fyi/dependency-validation

```

```
1 check failed, indicating possible issues with the project.
```
