# Tareas — Simulación de otros jugadores

## Backend

- [x] `DevEndpoints` con `POST /api/dev/session`, mapeado solo en `Development`.
- [x] Tests: sesión en Development (mismo jugador por nombre, token válido) y 404 fuera.

## Script

- [x] `scripts/dev-sim.ps1` con `create-match`, `join` y `hold`.
- [x] Probado contra la API local.

## Mobile

- [x] `api.createDevSession` y `signInAsDevPlayer` en `AuthContext`, con el guardado de sesión compartido con Google.
- [x] Bloque «Desarrollo» en `LoginScreen` con `__DEV__`.

## Documentación y verificación

- [x] Sección en el README.
- [x] `dotnet test --filter DevSession|Foundation` (7) y `npm run typecheck` en verde.
- [x] Prueba manual en el móvil (login como Bruno, 2026-10-04).
