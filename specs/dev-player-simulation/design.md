# Diseño — Simulación de otros jugadores

## Backend

### Identidad de los jugadores ficticios

No se añade ningún `AuthProvider` nuevo al dominio. Un jugador ficticio se registra como `AuthProvider.Google` con subject `dev:<slug>` (p. ej. `dev:ana`) y email `<slug>@dev.padelmatch.local`. Google usa subjects numéricos, así que un `dev:` nunca puede chocar con una cuenta real. Se reutiliza por nombre con `IPlayerRepository.FindByExternalIdentityAsync(Google, "dev:<slug>")`.

### `POST /api/dev/session`

`PadelMatch.Api/Dev/DevEndpoints.cs`, mapeado en `Program.cs` **solo dentro de** `if (app.Environment.IsDevelopment())`. Fuera de ese bloque la ruta no existe: 404, no 403.

```text
POST /api/dev/session  { "name": "Ana" }  →  200 AuthResponse (mismo contrato que /api/auth/google)
```

Busca o registra el jugador (`Player.Register`, como `PlayerAuthenticator`) y emite el token con el `IPlayerSessionTokenIssuer` de siempre. Si el jugador no tiene nivel (nuevo, o creado antes de este cambio), se le pone una fecha de nacimiento ficticia y se le pasa por `Player.CompleteLevelSurvey` con respuestas derivadas de un hash estable del nombre (no `string.GetHashCode`, que cambia en cada proceso). Es anónimo, como el login real. Un nombre vacío devuelve 400.

### Tests

- `Development`: la sesión funciona, devuelve el mismo jugador para el mismo nombre y su token sirve en `GET /api/players/me`.
- Otro entorno: 404. La factoría de tests hoy arranca en `Development`; el test crea una factoría derivada con `UseEnvironment("Production")`.

## Script `scripts/dev-sim.ps1`

PowerShell (el entorno local es Windows). Base URL por defecto `http://localhost:5080`, sobrescribible con `-ApiBaseUrl`.

```text
dev-sim.ps1 create-match [-Player Ana]           # crea un amistoso en el primer hueco libre y lo paga
dev-sim.ps1 join <matchId> [-Count 1]           # Count jugadores ficticios retienen y pagan
dev-sim.ps1 hold <matchId> [-Player Bruno] [-Position 0-3]  # retiene sin pagar (probar «plaza no disponible»)
```

- `create-match`: `GET /api/clubs/nearby` → primer club con hueco en `GET /api/clubs/{id}/slots` → `POST /api/matches` (amistoso) → `POST /confirm`. Imprime id, club y horario.
- `join`: recorre un pool fijo de nombres (`Bruno`, `Carla`, `Diego`, `Elena`, ...) y para cada uno hace `hold` + `confirm`. Si `hold` da 409 (ese jugador ya está o el partido está lleno), pasa al siguiente o termina.
- Errores de la API: imprime el `title` del problem y sale con código ≠ 0.

## Mobile

`LoginScreen`: con `__DEV__`, debajo del botón de Google, un bloque «Desarrollo» con cuatro botones (`Ana`, `Bruno`, `Carla`, `Diego`). Cada uno llama a `api.createDevSession(name)` y entra por el mismo camino que Google: `AuthContext` gana `signInAsDevPlayer`, que comparte con Google el guardado de sesión (`completeSignIn`).

`__DEV__` es `false` en builds de release, así que el bloque y la llamada desaparecen ahí. En cualquier caso, el backend de producción no tiene la ruta.
