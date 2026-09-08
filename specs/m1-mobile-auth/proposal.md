# M1 — Mobile Auth

## Intención y problema

[m1-players/proposal.md](../m1-players/proposal.md) entregó el contrato de backend completo de `Player` (login social, sesión propia, perfil, encuesta de nivel), pero dejó explícitamente fuera de alcance la UI de `mobile/` que lo consume. Sin esa UI, ningún usuario real puede autenticarse ni usar la app: solo se puede validar el backend directamente (Postman/curl, o los tests con doble determinista).

Esta spec cierra esa laguna: construye en `mobile/` las pantallas mínimas para que una persona real pueda autenticarse con Google (y, donde sea posible verificarlo, con Apple), ver y editar su propio perfil, y completar la encuesta de nivel inicial.

## Referencias y restricciones

- [m1-players/proposal.md](../m1-players/proposal.md) y [design.md](../m1-players/design.md): contrato de backend (`POST /api/auth/google`, `POST /api/auth/apple`, `GET/PUT /api/players/me`, `POST /api/players/me/level-survey`), formas exactas de request/response, invariante de `DateOfBirth` obligatoria solo para la encuesta de nivel.
- [CONSTITUTION.md](../CONSTITUTION.md): organización de `mobile/` por feature (`src/features/auth/`, `src/components/ui/`, `src/theme/`) y design system mínimo viable, ambos fijados el 2026-09-08 específicamente para esta spec.
- MVP-PLAN.md §5 (registro y onboarding), §31 (perfil propio).

## Decisión de alcance: limitaciones del entorno de desarrollo

- El entorno de desarrollo es Windows, sin Mac ni Xcode disponibles. Apple Sign-In no se puede validar de extremo a extremo aquí: requiere un dispositivo iOS o macOS real y una cuenta de Apple Developer activa. Esta spec construye el flujo de Google primero y de forma completa, verificable en este entorno. La pantalla de login incluye también la opción de Apple (el backend ya la soporta desde M1), pero su verificación manual queda pendiente de acceso a hardware Apple; no bloquea el resto del alcance.
- **Corrección (2026-09-08, durante el diseño):** se descarta la restricción original de "debe funcionar dentro de Expo Go". La documentación actual de Expo indica explícitamente que Expo Go no admite esquemas de URL personalizados y no es apta para probar flujos OAuth/OIDC reales, y que el SDK nativo de Google (`@react-native-google-signin/google-signin`) requiere un development build. Login de Google real y verificable, y "solo Expo Go", son objetivos incompatibles hoy. Se adopta en su lugar un **development build local de Android** (`npx expo run:android`, sin EAS ni cuenta de pago), decisión del usuario. Sigue sin requerir Mac: el desarrollo continúa en Windows. `design.md` concreta la librería y el flujo sobre esta base.

## Alcance incluido

| Pantalla / capacidad | Resultado esperado |
| --- | --- |
| Login | Una persona sin sesión ve un botón "Continuar con Google" (y, en iOS, también "Continuar con Apple"); al completarlo con éxito, la app llama al backend, recibe el JWT de sesión y el perfil, y navega a la app autenticada. |
| Persistencia de sesión | El JWT de sesión se guarda en el dispositivo; al reabrir la app, si hay sesión válida, se salta el login. Un 401 de la API fuerza a volver a login. |
| Perfil propio | Un jugador autenticado puede ver su perfil (`GetPlayerProfile`) y editar ciudad/zona y fecha de nacimiento (`UpdateProfile`). |
| Encuesta de nivel | Un jugador puede completar la encuesta de nivel en cualquier momento tras el login; si `DateOfBirth` no está definida, la app lo indica y pide completarla antes de enviarla, reflejando el 400 que ya protege el backend. |
| Estado sin nivel | Un jugador sin encuesta completada puede navegar con normalidad; el perfil muestra explícitamente "sin nivel", no un valor inventado. |
| Cierre de sesión | Un jugador autenticado puede cerrar sesión, borrando el JWT local. |

## Fuera de alcance

- Validación real de Apple Sign-In en dispositivo — sin hardware disponible en este entorno (ver arriba). El botón existe y usa el mismo contrato de backend, pero no se considera verificado hasta que se pruebe en iOS/macOS real.
- Subida de foto de perfil: el backend solo acepta `PhotoUrl` como URL de texto (`UpdateProfileRequest.PhotoUrl`), no un endpoint de subida de imagen. Esta spec no añade un flujo de carga de imágenes; como mucho, un campo de texto para pegar una URL, si se decide incluirlo en `design.md`.
- Recuperación de cuenta, borrado, revocación de acceso del proveedor social, RGPD/exportación — igual que en `m1-players`, fuera de alcance salvo indicación contraria.
- Cualquier pantalla o dato de otros milestones: partidos, clubes, perfiles de otros jugadores.
- Rediseño visual o pulido de marca: se usan los tokens mínimos ya extraídos a `src/theme/` (colores y tipografía de la pantalla de M0); no se amplía el design system en esta spec.
- Development build nativo / EAS Build: fuera de alcance mientras Expo Go sea suficiente para el flujo elegido.

## Criterios de aceptación

1. Una persona sin sesión puede autenticarse con su cuenta de Google desde la app corriendo en Expo Go (Android o iOS) y queda registrada como `Player` en su primer acceso, verificado manualmente en este entorno.
2. La app conserva la sesión entre reinicios sin pedir login de nuevo, y fuerza login de nuevo ante un 401.
3. Un jugador autenticado ve su perfil real (no datos de ejemplo) y puede editar ciudad/zona y fecha de nacimiento, reflejándose el cambio al recargar el perfil.
4. Un jugador puede completar la encuesta de nivel y ver después `Level`/`LevelConfidence` actualizados en su perfil; si falta `DateOfBirth`, la app se lo explica en vez de mostrar un error genérico de red.
5. Un jugador puede cerrar sesión y vuelve a ver la pantalla de login.
6. El botón de Apple Sign-In existe y usa el mismo contrato, aunque su verificación end-to-end quede pendiente de hardware Apple (no es criterio de aceptación bloqueante de esta spec, se documenta como limitación conocida).

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: la librería/flujo OAuth compatible con Expo Go para Google (y el mismo mecanismo de token para Apple), dónde y cómo se persiste el JWT de sesión en el dispositivo, la forma del cliente HTTP hacia la API (base URL configurable para dispositivo físico en la misma red, manejo de 401), la composición de pantallas dentro de `src/features/auth/`, y el contenido concreto de los formularios de perfil y encuesta de nivel.
