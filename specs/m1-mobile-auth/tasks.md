# Tareas — M1 Mobile Auth

## Preparación del entorno (development build Android)

- [x] Añadir `expo.android.package` a `mobile/app.json`.
- [x] `npx expo prebuild --platform android` para generar `mobile/android/`.
- [x] Obtener el SHA-1 de depuración (`5E:8F:16:06:2E:A3:CD:2C:4A:0D:54:78:76:BA:A6:F3:8C:AB:F6:25`, variante `debug` de `mobile/android/app/debug.keystore`).
- [x] Crear el cliente OAuth de tipo **Android** en Google Cloud Console (`app-padel-507914`), con el paquete y el SHA-1 anteriores. (En curso, por el usuario.)
- [x] Confirmar que el Web Client ID ya creado (`Auth:Google:Audience`) es el que se reutiliza como `webClientId` — no crear un segundo cliente Web.

## Dependencias y configuración de mobile

- [x] Instalar `@react-native-google-signin/google-signin` y `expo-secure-store` en `mobile/` (también `@react-native-community/datetimepicker`, necesario para el selector de fecha de nacimiento).
- [x] Añadir el plugin `@react-native-google-signin/google-signin` a `mobile/app.json`.
- [x] Crear `mobile/.env.example` con `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID` y `EXPO_PUBLIC_API_BASE_URL`; documentar en el README que `mobile/.env` es local y no se versiona.
- [x] Documentar en el README cómo levantar la API accesible desde el dispositivo/emulador (`--urls http://0.0.0.0:5080`, apertura de puerto en el Firewall de Windows si aplica) y los valores de `EXPO_PUBLIC_API_BASE_URL` para emulador (`10.0.2.2`) vs. dispositivo físico (IP de LAN).

## Implementación

- [x] `src/api/httpClient.ts`: helper de `fetch` con base URL configurable, cabecera `Authorization`, y callback de sesión expirada en 401.
- [x] `src/features/auth/session.ts`: guardar/leer/borrar el `sessionToken` en `expo-secure-store`.
- [x] `src/features/auth/AuthContext.tsx`: estado de sesión (`loading`/`authenticated`/`unauthenticated`), restauración al arrancar, `signIn`, `signOut`, `refreshProfile`, enganchado al callback de 401 del cliente HTTP.
- [x] `src/features/auth/LoginScreen.tsx` + `useGoogleSignIn`: `GoogleSignin.configure`, botón "Continuar con Google", llamada a `POST /api/auth/google`, manejo de error de cancelación/Play Services.
- [x] `src/features/players/ProfileScreen.tsx`: lectura de perfil desde `AuthContext`, estado explícito "sin nivel todavía", formulario de edición (`cityOrZone`, `dateOfBirth`, `photoUrl`) contra `PUT /api/players/me`, botón de cierre de sesión.
- [x] `src/features/players/LevelSurveyScreen.tsx`: formulario de las tres preguntas, envío a `POST /api/players/me/level-survey`, mensaje explicativo específico si la API responde 400 por falta de `DateOfBirth`.
- [x] `src/app/RootNavigator.tsx`: conmutación entre `LoginScreen`, `ProfileScreen` y `LevelSurveyScreen` según `AuthContext.status` y una ruta local.
- [x] Actualizar `App.tsx` para envolver en `AuthProvider` y renderizar `RootNavigator` en vez de `HomeScreen`.
- [x] Ocultar el botón de Apple mientras no exista build iOS (`Platform.OS !== 'ios'`), tal como fija `design.md` — no se añade ningún componente de Apple todavía, en vez de código muerto condicionado.

## Verificación manual (sin infraestructura de pruebas de UI en mobile)

- [x] `npx expo run:android` arranca la app en emulador/dispositivo con el development build.
- [x] Login con la cuenta de Google de prueba registra un `Player` nuevo la primera vez (contrastar con el `id` ya visto en las pruebas manuales de backend).
- [x] Cerrar y reabrir la app conserva la sesión sin pedir login de nuevo.
- [x] Editar ciudad/zona y fecha de nacimiento persiste al recargar el perfil.
- [x] Completar la encuesta de nivel actualiza `Level`/`LevelConfidence` visibles en el perfil.
- [x] Intentar la encuesta sin fecha de nacimiento muestra el mensaje explicativo, no un error genérico.
- [x] Cerrar sesión vuelve a la pantalla de login; un token inválido/caducado también fuerza vuelta a login.
- [x] `npm --prefix mobile run typecheck` y `npx expo-doctor` sin errores nuevos.

## Cierre

- [x] Actualizar el README (sección "Cliente Expo") con los pasos de development build Android, sustituyendo o complementando las instrucciones actuales de Expo Go para este flujo.
- [x] Revisar cambios, instrucciones reproducibles y correspondencia con los criterios de aceptación del proposal.
- [x] Documentar la verificación con `pastiche-rdd`.
