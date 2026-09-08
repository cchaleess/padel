# Diseño — M1 Mobile Auth

## Objetivo y decisión central

Construir en `mobile/` el login de Google, la persistencia de sesión, el perfil propio y la encuesta de nivel descritos en el [proposal](proposal.md), consumiendo el contrato de backend ya implementado y probado en [m1-players](../m1-players/design.md).

Decisión que reemplaza la premisa original del proposal: el proyecto pasa de Expo Go a un **development build local de Android** (`npx expo run:android`). Motivo (confirmado contra la documentación actual de Expo, no por memoria): Expo Go no soporta esquemas de URL personalizados y la propia guía de Expo la descarta para probar OAuth/OIDC real; el SDK nativo de Google exige un development build. Sigue sin hacer falta Mac ni EAS de pago — solo Android Studio (SDK + emulador, o un dispositivo físico con depuración USB). iOS queda fuera de esta spec por falta de hardware Apple, igual que ya fijaba el proposal.

## Stack añadido

| Necesidad | Elección |
| --- | --- |
| Login nativo de Google | `@react-native-google-signin/google-signin` (recomendada por la propia documentación de Expo para este caso; usa Google Play Services, no navegador ni redirect URI) |
| Persistencia de sesión en el dispositivo | `expo-secure-store` (Keystore de Android) |
| Estado de sesión en la app | Contexto de React (`AuthContext`) sin librería adicional |
| Navegación entre login/app | Conmutación de estado local en un componente raíz; no se añade `@react-navigation` para 3 pantallas — se revisita cuando el número de pantallas lo justifique |
| Cliente HTTP | `fetch` envuelto en un helper propio (`src/api/httpClient.ts`); no se añade Axios/React Query para esta superficie |

No se usa `expo-auth-session` para Google: exigiría un flujo basado en navegador con redirect URI, que es justo lo que la documentación de Expo desaconseja para este caso y añade complejidad (PKCE, deep link) sin necesidad, cuando el SDK nativo ya resuelve la obtención del `idToken` de forma directa una vez hay development build.

## Autenticación

### Por qué el SDK nativo encaja con el contrato de backend existente

`GoogleSignin.configure({ webClientId })` + `GoogleSignin.signIn()` devuelve un `idToken` cuyo `aud` es el **Web Client ID** pasado como `webClientId` — el mismo valor, sin importar la plataforma. Esto coincide exactamente con `Auth:Google:Audience` en el backend (ver [m1-players/design.md](../m1-players/design.md)): no hace falta ningún cambio de contrato ni de configuración en `PadelMatch.Api`, solo apuntar el cliente móvil al mismo Web Client ID ya usado para las pruebas manuales de login.

Para que `GoogleSignin.signIn()` funcione en Android hace falta además un **cliente OAuth de tipo Android** registrado en el mismo proyecto de Google Cloud (`app-padel-507914`), identificado por el nombre de paquete de la app y la huella SHA-1 del certificado de firma del build de desarrollo. Es un cliente adicional al Web ya creado — Google los usa juntos: el cliente Android autoriza la app instalada concreta; el Web Client ID sigue siendo la audiencia que verifica el backend.

### Configuración de Google Cloud (manual, por el usuario)

1. `mobile/app.json`: añadir `expo.android.package` (p. ej. `com.padelmatch.app`) — necesario tanto para `expo run:android` como para registrar el cliente Android en Google Cloud.
2. `npx expo prebuild --platform android` genera `mobile/android/` con un keystore de depuración.
3. Obtener el SHA-1 de ese keystore: `cd mobile/android && ./gradlew signingReport` (Windows: `gradlew.bat signingReport`), leer la huella de la variante `debug`.
4. En Google Cloud Console → Clientes → Crear cliente → tipo **Android** → paquete = el mismo de `app.json` → SHA-1 del paso anterior.
5. El Web Client ID ya creado (`191288540153-....apps.googleusercontent.com`, el mismo configurado como `Auth:Google:Audience`) se reutiliza tal cual como `webClientId` de `GoogleSignin.configure`. No se crea un segundo Web Client.

### Configuración del cliente móvil

- `mobile/app.json`: añadir el plugin `["@react-native-google-signin/google-signin"]` (sin `iosUrlScheme`, porque no hay build iOS en esta spec).
- El Web Client ID no es secreto (viaja igualmente dentro del APK), pero para no hardcodearlo se expone vía variable de entorno nativa de Expo: `mobile/.env` (gitignorado, con `mobile/.env.example` versionado) define `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID`. `GoogleSignin.configure` lo lee de `process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID` al arrancar la app.
- No se usa `google-services.json` ni Firebase: el flujo de `idToken` para verificación en un backend propio no lo necesita.

### Flujo de login

`src/features/auth/`:
- `GoogleSignInButton` invoca `GoogleSignin.hasPlayServices()` → `GoogleSignin.signIn()` → toma `idToken` del resultado.
- Envía `{ idToken }` a `POST /api/auth/google` (contrato ya probado manualmente contra el backend).
- Respuesta `{ sessionToken, player }`: `sessionToken` se guarda con `SecureStore.setItemAsync`; `player` puebla el `AuthContext`.
- Cualquier error de `GoogleSignin` (usuario cancela, Play Services no disponible) se muestra como mensaje simple en la pantalla, sin reintentos automáticos.

El botón "Continuar con Apple" del proposal **no se renderiza** mientras no exista build iOS (`Platform.OS !== 'ios'` en esta fase): mostrarlo en Android sin implementación sería un botón muerto. Cuando exista hardware Apple y se aborde esa build, se añade condicionado a iOS — no bloquea el criterio de aceptación 6 del proposal, que ya lo documenta como pendiente.

### Sesión y `AuthContext`

- `src/features/auth/session.ts`: `saveSession`, `loadSession`, `clearSession` sobre `expo-secure-store`, guardando `{ sessionToken }` (el perfil se vuelve a pedir a la API, no se cachea junto al token para no arrastrar datos obsoletos).
- `src/features/auth/AuthContext.tsx`: al montar, intenta `loadSession()`; si hay token, llama `GET /api/players/me` para validarlo y obtener el perfil actual. Un 401 en cualquier llamada (incluida esta) dispara `clearSession()` y vuelve a `login`. Expone `{ status: 'loading' | 'authenticated' | 'unauthenticated', player, signIn, signOut, refreshProfile }`.
- `signOut()` limpia SecureStore y el estado en memoria; no hay revocación remota del token de sesión propio en M1 (ya documentado como límite en [m1-players/design.md](../m1-players/design.md), sección de alternativas).

### Cliente HTTP y 401

- `src/api/httpClient.ts`: `request(path, options)` añade `Authorization: Bearer <token>` cuando hay sesión, usa `EXPO_PUBLIC_API_BASE_URL` como base, y en cualquier `401` invoca un callback de "sesión expirada" registrado por `AuthContext` (evita una dependencia circular entre el cliente HTTP y el contexto).
- `EXPO_PUBLIC_API_BASE_URL` (en `mobile/.env`) apunta a la API accesible desde el dispositivo/emulador usado:
  - Emulador Android (AVD): `http://10.0.2.2:5080` (alias fijo de Android hacia el `localhost` del host).
  - Dispositivo físico por USB o misma red Wi-Fi: `http://<IP-LAN-del-PC>:5080`.
- La API debe escuchar en todas las interfaces para el caso de dispositivo físico, no solo `localhost`: se documenta en el README arrancarla con `dotnet run --project backend/PadelMatch.Api --launch-profile http --urls http://0.0.0.0:5080` cuando se vaya a probar desde el móvil, y abrir el puerto 5080 en el Firewall de Windows si lo bloquea la primera vez.

## Perfil propio y encuesta de nivel

`src/features/players/` (separado de `src/features/auth/`, seguía la organización por dominio ya fijada en la constitución):

- `ProfileScreen.tsx`: pantalla mostrada tras el login. Llama `GET /api/players/me` vía `AuthContext.player` (ya cargado). Muestra `displayName`, `cityOrZone`, `dateOfBirth`, `photoUrl`, y el nivel: si `levelConfidence === 'None'`, un estado explícito "Sin nivel todavía" (nunca un valor inventado, por la constitución), con acción para ir a la encuesta. Formulario de edición inline para `cityOrZone`, `dateOfBirth` (selector nativo de fecha) y `photoUrl` (campo de texto para pegar una URL, sin subida de imagen — fuera de alcance según el proposal) que llama `PUT /api/players/me` y refresca el contexto.
- `LevelSurveyScreen.tsx`: formulario con tres selectores de opción única para `YearsPlayingPadel`, `WeeklyFrequency`, `SelfPerceivedLevel` (mismos enums que el backend, ver [m1-players/design.md](../m1-players/design.md)). Al enviar, llama `POST /api/players/me/level-survey`. Si la API devuelve 400 (falta `DateOfBirth`), se muestra el mensaje explicando que hace falta completar la fecha de nacimiento en el perfil antes de poder enviar la encuesta — no un error genérico de red, según el criterio de aceptación 4 del proposal.
- Botón "Cerrar sesión" en `ProfileScreen` llama `AuthContext.signOut()`.

## Navegación

`src/app/RootNavigator.tsx`: componente único que lee `AuthContext.status` y renderiza `LoginScreen`, `ProfileScreen` o `LevelSurveyScreen` según un estado de ruta local (`useState`). `App.tsx` pasa a envolver la app en `AuthProvider` y renderizar `RootNavigator` en vez de `HomeScreen` directamente. `HomeScreen` (placeholder de M0) deja de usarse como pantalla de entrada; no se borra el archivo por si una spec posterior de "descubrir partidos" lo retoma como base, pero no se referencia desde `App.tsx`.

Se pospone deliberadamente `@react-navigation`: con 3 pantallas y un flujo lineal (login → perfil ⇄ encuesta), una librería de navegación completa (stacks, deep linking) añadiría superficie sin resolver un problema real todavía, en línea con "UI simple" de la constitución. Se reconsidera cuando M2+ añada más pantallas con navegación no lineal (feed, detalle de partido, etc.).

## Verificación

Sin infraestructura de pruebas automatizadas de UI en `mobile/` (no existe en M0/M1-players; añadir Detox/RNTL end-to-end no está justificado para esta spec según el principio de "no sobre-diseñar"). La verificación es manual, en el emulador o dispositivo Android del development build:

1. Login con la cuenta de Google de prueba ya usada manualmente contra el backend → aparece como `Player` nuevo la primera vez.
2. Cerrar y reabrir la app → sesión persiste sin pedir login de nuevo.
3. Editar ciudad/zona y fecha de nacimiento → persiste al recargar el perfil.
4. Completar la encuesta de nivel → `Level`/`LevelConfidence` se actualizan en el perfil.
5. Intentar la encuesta sin fecha de nacimiento → mensaje explicativo, no error genérico.
6. Cerrar sesión → vuelve a login; un 401 simulado (token caducado o revocado manualmente en backend) también fuerza vuelta a login.
7. `npm --prefix mobile run typecheck` y `npx expo-doctor` sin errores nuevos.

Se documentará con `pastiche-rdd` al terminar `tasks.md`, igual que en M0 y m1-players.

## Alternativas y límites

- No se usa `expo-auth-session`/flujo basado en navegador para Google: la documentación de Expo lo desaconseja para Expo Go y no aporta nada frente al SDK nativo una vez hay development build.
- No se implementa Apple Sign-In en esta spec: sin hardware iOS no es verificable: el botón no se renderiza en Android en vez de simularse.
- No se añade `@react-navigation` ni gestión de estado global (Redux/Zustand): el volumen de pantallas no lo justifica todavía.
- No se cachea el perfil en `SecureStore` junto al token: se prefiere pedirlo siempre a la API para evitar mostrar datos obsoletos tras editarlo desde otro sitio.
- El JWT de sesión propio sigue sin refresh token (límite ya documentado en m1-players): al expirar a los 30 días, `AuthContext` lo trata igual que un 401 y pide login de nuevo, que en Android es instantáneo vía `GoogleSignin.signInSilently()` si la sesión de Google del dispositivo sigue activa — se evalúa usarlo como mejora, no es requisito de esta spec.
