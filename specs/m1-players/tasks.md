# Tareas — M1 Players

- [x] Añadir `Player`, `PlayerExternalIdentity`, `AuthProvider` y `LevelConfidence` en `PadelMatch.Domain`, sin dependencias externas.
- [x] Implementar `InitialLevelEstimator` (servicio de dominio puro) para la encuesta de nivel, con pruebas unitarias de las combinaciones de respuestas.
- [x] Definir en `PadelMatch.Application` los contratos: `IExternalIdentityVerifier`, `IPlayerSessionTokenIssuer`, y los casos de uso de autenticación, perfil y encuesta.
- [x] Configurar EF Core: mapeo de `Player`/`PlayerExternalIdentity`, índice único `(Provider, ProviderSubjectId)`, migración `AddPlayers`.
- [x] Implementar `GoogleIdentityVerifier` (Google.Apis.Auth) en `PadelMatch.Infrastructure`.
- [x] Implementar `AppleIdentityVerifier` (validación manual OIDC contra `appleid.apple.com`) en `PadelMatch.Infrastructure`.
- [x] Implementar `IPlayerSessionTokenIssuer` con JWT propio (HMAC, expiración 30 días) y configurar `AddAuthentication().AddJwtBearer(...)` en la API.
- [x] Exponer `POST /api/auth/google` y `POST /api/auth/apple`, incluyendo el caso de alta vs. reconocimiento y el manejo de `displayName` opcional de Apple.
- [x] Exponer `GET /api/players/me`, `PUT /api/players/me` y `POST /api/players/me/level-survey`, protegidos con el JWT propio.
- [x] Configurar valores de desarrollo (`Auth:Google:Audience`, `Auth:Apple:Audience`, `Auth:SessionSigningKey`) en `appsettings.Development.json`/`.env.example`, exigidos fuera de Development igual que la cadena de conexión de M0.
- [x] Crear una implementación de prueba de `IExternalIdentityVerifier` sustituible vía `WebApplicationFactory` para no depender de red real en los tests.
- [x] Pruebas de integración: alta y reconocimiento de `Player` por proveedor, no fusión de cuentas entre proveedores con el mismo email, autorización 401/200 de los endpoints de perfil, encuesta de nivel y su efecto en `Level`/`LevelConfidence`.
- [x] Actualizar el README con los nuevos prerrequisitos de configuración (audiencias de Google/Apple, clave de firma de sesión) y cómo ejecutar las pruebas de M1.
- [x] Revisar cambios, instrucciones reproducibles y correspondencia con los criterios de aceptación del proposal.
- [x] Exigir `DateOfBirth` para completar la encuesta de nivel (decisión añadida a posteriori: la edad mejora la calidad de emparejamientos futuros). `Player.CompleteLevelSurvey` rechaza con `PlayerProfileIncompleteException` → 400 si falta; `CityOrZone` no se ve afectado. Cubierto con pruebas de dominio e integración.
