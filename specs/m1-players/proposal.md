# M1 — Players

## Intención y problema

M0 dejó una base ejecutable sin entidades de negocio. M1 introduce la primera: `Player`. Debe permitir que una persona entre a la aplicación, tenga un perfil consultable y editable, y pueda opcionalmente completar la encuesta de nivel inicial. Es la base sobre la que se apoyan todos los milestones posteriores (M2 en adelante necesitan un jugador autenticado).

Esta spec fija además una decisión más específica que la del plan original: el acceso se hace mediante login social (Google y, en iOS, también Apple), no mediante email/contraseña. El plan (§5) dejaba abierto "Contraseña o login social"; esta spec resuelve esa ambigüedad para el MVP.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md), §5 (registro y onboarding), §6 (sistema de nivel, en la parte que aplica a M1), §31 (perfil propio, en la parte que aplica a M1), §34 (`Player` en el modelo de dominio), §36 (casos de uso de `Player`), §37 (referencia orientativa de API), §38 M1.
- [Constitución del proyecto](../CONSTITUTION.md): stack, arquitectura modular, y el principio de que las reglas críticas se protegen en backend/base de datos, no solo en UI.
- La constitución no fija un mecanismo de login: lo difiere explícitamente a M1 (ver "Fuera de alcance" de [m0-foundation/proposal.md](../m0-foundation/proposal.md)). Esta spec resuelve esa deferencia; no se propone ninguna modificación a `CONSTITUTION.md`.

## Decisión: alcance del login

**Login social con Google y Apple; sin email/contraseña en el MVP.**

- Google Sign-In es el método principal, disponible en Android e iOS.
- Apple Sign-In se incluye también en esta spec, no se difiere: Apple exige (App Store Review Guideline 4.8) que si se ofrece un login social de terceros en iOS, exista una alternativa equivalente de inicio de sesión privado, y Sign in with Apple es la forma aceptada de cumplirlo. Sin esto, la app arriesga rechazo en la App Store cuando llegue a esa fase.
- No se implementa registro con email/contraseña. Sustituye por completo a la opción "Contraseña" del plan original; no convive como alternativa en el MVP.
- El usuario indicó que en una etapa posterior podría incorporarse login con otras redes sociales (Instagram, Facebook). Esa incorporación queda fuera de esta spec, pero el modelo de identidad de `Player` no debe asumir un único proveedor: debe poder representar que la cuenta proviene de un proveedor externo identificado (`Google` o `Apple` en el MVP) sin necesitar un rediseño para añadir un tercero más adelante. La forma concreta de representarlo se decide en el diseño.

**Por qué es una decisión de esta spec y no de la constitución:** el mecanismo de login es una decisión de producto/UX sobre cómo un jugador entra a la aplicación, no un invariante que cruce todos los milestones (a diferencia de la concurrencia de plazas, la política temporal o el stack). No condiciona las reglas críticas de partidos, pistas o pagos, y la constitución ya había diferido "autenticación funcional" a M1 de forma deliberada. Una futura web de administración de clubes (mencionada en la constitución como evolución) tendrá previsiblemente un actor distinto (personal de club) con necesidades de acceso propias; fijar en la constitución "todo el proyecto usa login social de jugador X" sería anticipar más de lo necesario.

**Consecuencia sobre los datos mínimos de registro (§5):** Google y Apple entregan nombre, email y, opcionalmente, foto — pero no fecha de nacimiento. El plan exige fecha de nacimiento y ciudad/zona como datos mínimos de registro. El flujo de alta pasa entonces a dos pasos: autenticación con el proveedor social, y una compleción de perfil (fecha de nacimiento, ciudad/zona) posterior.

**Decisión del usuario (2026-09-07), añadida tras completar la implementación de backend:** la fecha de nacimiento no bloquea el registro ni el uso general de la app, pero sí es obligatoria para completar la encuesta de nivel — la edad se usará para mejorar la calidad de los emparejamientos en milestones posteriores, y esa señal debe capturarse desde ya. `CityOrZone` no tiene esta exigencia: sigue siendo opcional y postergable indefinidamente, sin ningún bloqueo backend. El detalle técnico está en `design.md` ("Encuesta de nivel inicial").

## Alcance incluido

| Área de M1 (plan §38) | Resultado esperado |
| --- | --- |
| Registro | Un jugador puede autenticarse con Google (Android/iOS) o Apple (iOS) y queda dado de alta como `Player` la primera vez que lo hace. |
| Perfil | Un jugador puede consultar y actualizar su propio perfil: nombre, ciudad/zona, foto opcional, fecha de nacimiento. |
| Ciudad/zona | Se guarda en el perfil y sirve como alternativa a la geolocalización del dispositivo, conforme al §5. La geolocalización en sí (uso para ordenar partidos/clubes cercanos) no se implementa en M1: no hay todavía partidos ni clubes que ordenar (M2+). |
| Nivel opcional | Un jugador puede navegar sin haber completado la encuesta de nivel; su perfil refleja la ausencia de nivel de forma explícita, no con un valor por defecto engañoso. |
| Encuesta inicial | Un jugador puede completar la encuesta de nivel en el momento que quiera tras el registro, obteniendo un `Level` inicial, `LevelConfidence` baja y el resto de conceptos separados de la constitución (§6). El contenido exacto de la encuesta y la fórmula de puntaje inicial se resuelven en el diseño de esta misma spec, no se difieren a otro milestone: el plan (§5) ya advierte que "la encuesta exacta se definirá posteriormente", y ese "posteriormente" es este diseño. |

## Fuera de alcance

- Email/contraseña como método de registro o login, en cualquier forma.
- Otras redes sociales (Instagram, Facebook, etc.) como proveedor de login — mencionadas por el usuario como posible evolución futura, no de esta spec.
- Geolocalización del dispositivo y su uso para ordenar resultados (no hay nada que ordenar todavía sin clubes/partidos de M2+).
- `GetRelatedPlayerProfile` y perfil de otros jugadores (§32): solo tienen sentido en el contexto de un partido compartido, que no existe hasta M3+. Se retoma cuando exista ese contexto.
- Evaluaciones de nivel y sociales post-partido (`PlayerLevelAssessment`, `PlayerSocialAssessment`), histórico de partidos y estadísticas de perfil (`MatchesPlayed` real, victorias/derrotas): dependen de partidos jugados, fuera de alcance hasta M8–M13.
- El algoritmo definitivo de recálculo de nivel (RatingEngine): explícitamente fuera del MVP según el plan (§40) y la constitución.
- Borrado de cuenta, revocación de acceso del proveedor social, RGPD/exportación de datos: no mencionados por el plan para el MVP; se dejan pendientes salvo que el usuario indique lo contrario.
- Paneles de administración, roles de club o staff: no existen todavía actores distintos de `Player`.
- **Pantallas de mobile (login, perfil, encuesta de nivel).** Esta spec entrega el contrato de backend completo (verificación de identidad, sesión propia, perfil, encuesta) pero no la UI de `mobile/` que lo consume. Sin esa UI, ningún usuario final puede autenticarse todavía: solo se puede validar el backend directamente (Postman/curl, o los tests con doble determinista). Se retoma en una spec de mobile aparte. Nota añadida a posteriori (2026-09-07), tras completar la implementación de backend, al detectar que `tasks.md` nunca incluyó tareas de mobile pese a que los criterios de aceptación de abajo dan por hecho un cliente móvil funcional.

## Criterios de aceptación

Los criterios 1–8 describen la capacidad completa (incluyendo cliente móvil) que persigue el modelo de `Player`. Con el alcance actual (backend únicamente, ver "Fuera de alcance"), se validan a nivel de API/backend; la validación end-to-end con un usuario real en un dispositivo queda pendiente de la spec de mobile.

1. Una persona puede autenticarse con su cuenta de Google desde el cliente móvil (Android e iOS) y queda registrada como `Player` en su primer acceso.
2. En iOS, una persona puede alternativamente autenticarse con Apple Sign-In, con el mismo resultado de alta como `Player`.
3. Si el proveedor social no entrega fecha de nacimiento o ciudad/zona, el jugador puede completarlas desde la aplicación como parte del alta o de su perfil, y esos datos quedan persistidos.
4. Un jugador autenticado puede consultar su propio perfil (`GetPlayerProfile`) y actualizar los campos editables (`UpdateProfile`): ciudad/zona, foto opcional, y los datos personales que correspondan.
5. Un jugador sin encuesta de nivel completada puede usar la aplicación con normalidad dentro del alcance de M1; su perfil no muestra un nivel inventado.
6. Un jugador puede completar la encuesta de nivel (`CompleteLevelSurvey`) en cualquier momento tras el registro y su perfil refleja después `Level`, `LevelConfidence` y `MatchesPlayed` como conceptos separados, conforme a la constitución (§29 de la constitución / §6 del plan).
7. Un intento de autenticación con un proveedor no soportado, o sin completar los datos obligatorios pendientes, se rechaza o se guía de forma explícita; no se crea un `Player` con datos mínimos incompletos de forma silenciosa.
8. Las reglas de qué hace válido a un `Player` (proveedor soportado, datos obligatorios) están protegidas en backend, no solo en la UI del cliente móvil, conforme al principio de la constitución.

## Aclaraciones y ambigüedades del plan

Estas quedan resueltas para esta spec; el detalle técnico de cómo implementarlas se define en `design.md`.

### ¿Login único proveedor o convive con email/contraseña?

**Decisión del usuario:** por ahora solo Google; en una etapa más avanzada podría incorporarse alguna red social adicional (Instagram, Facebook). Email/contraseña no se implementa.

### ¿Se incluye Apple Sign-In en esta spec o se difiere?

**Decisión del usuario:** se incluye en M1, no se difiere, precisamente para no arriesgar un rediseño del modelo de identidad ni un problema de cumplimiento de App Store más adelante.

## Decisiones técnicas reservadas para el diseño

El diseño de esta spec concretará: cómo se representa la identidad externa de `Player` (proveedor + identificador, preparado para más de uno sin ser genérico en exceso), el mecanismo de verificación de los tokens de Google/Apple desde el backend, la forma del contrato de autenticación (sustituye a `POST /api/auth/register` de la referencia orientativa del plan §37), el momento exacto en que se exige completar fecha de nacimiento/ciudad-zona, y el contenido y fórmula de la encuesta de nivel inicial. No se fijan aquí versiones de librerías ni SDKs de los proveedores.

Este documento define intención, alcance y resultados verificables. El enfoque técnico y la ejecución se desarrollarán en `design.md` y `tasks.md` una vez revisado este proposal.
