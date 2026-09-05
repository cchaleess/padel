# Revisión de M0

## Entorno y evidencia adicional

Comprobaciones realizadas entre el 5 y el 6 de septiembre de 2026 en Windows 11, con SDK .NET 10.0.400, Node 24.19.0 y PostgreSQL 18.6 portable. Los binarios de PostgreSQL proceden del ZIP de EDB enlazado por la página oficial de PostgreSQL. La instancia escucha en loopback y conserva sus datos bajo `.local/`.

El informe [verification.md](verification.md) lo genera exclusivamente `pastiche rdd verify m0-foundation`, ejecutando los comandos de [checks.md](checks.md). Requiere PostgreSQL arrancado con las credenciales de desarrollo del README o `PADELMATCH_TEST_CONNECTION` configurada para un usuario con permiso de crear bases.

Además de las comprobaciones automatizadas del informe:

- Se aplicó `InitialFoundation` a la base local vacía y se repitió `database update`: la segunda ejecución indicó que no había migraciones pendientes.
- Se arrancó Kestrel en `http://localhost:5080`. Liveness, readiness y OpenAPI devolvieron HTTP 200. Las respuestas de salud incluyeron instantes UTC.
- Metro sirvió la app en `http://localhost:8081`. Playwright con Chrome headless comprobó el título, el texto y la ausencia de desbordamiento horizontal a 320, 390 y 768 píxeles. No hubo errores JavaScript de página. Se revisó visualmente la [captura a 390 píxeles](evidence/mobile-web-390.png); los datos de la comprobación están en [browser-check.json](evidence/browser-check.json).

## Correspondencia con el proposal

| Criterio | Evidencia |
| --- | --- |
| 1. Restaurar, compilar y arrancar API | Restore/build en RDD y ejecución Kestrel con HTTP 200. |
| 2. Cuatro proyectos y cliente | `PadelMatch.slnx`, referencias entre proyectos y estructura `mobile/`. |
| 3. EF Core con PostgreSQL real | Pruebas de integración y `/health/ready` en la instancia portable. |
| 4. Migración inicial repetible | Prueba que migra dos veces una base temporal y verifica una sola entrada de historia; repetición adicional del comando EF local. |
| 5. OpenAPI válido y consultable | Prueba del contrato JSON y consulta HTTP al documento generado por ASP.NET Core. |
| 6. Arranque Expo y pantalla mínima | Metro y Chrome a tres anchos; exportaciones Android/iOS/web. Límites nativos descritos debajo. |
| 7. Instrucciones reproducibles | README, SDK fijado, manifiesto local EF, lockfile npm, Compose y script portable Windows. |

## Límites de esta verificación

- No se ejecutó la app en Android/iOS nativos: este equipo no dispone de emuladores ni dispositivos conectados. Los bundles de ambas plataformas se exportaron correctamente; esto no equivale a probar un APK/IPA o Expo Go en un teléfono.
- Docker no está instalado. Se revisó la configuración Compose, pero la ejecución real de PostgreSQL se verificó mediante los binarios Windows. No se declara probado el arranque del contenedor.
- La integración con pagos reales y las políticas de clubes siguen fuera de M0.

## Reproducir RDD

Con Pastiche disponible en `PATH`, PostgreSQL arrancado y los prerrequisitos del README:

```powershell
pastiche rdd verify m0-foundation
pastiche rdd status
```

En este equipo la CLI está compilada en `C:/repos/pastiche/src/Pastiche.Cli/bin/Release/net10.0-windows/Pastiche.Cli.exe`; puede invocarse por esa ruta desde la raíz de este repositorio. No es una dependencia del producto.
