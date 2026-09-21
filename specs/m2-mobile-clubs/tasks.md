# Tareas — M2 Mobile Clubs

## Constitución y dependencias

- [x] Actualizar `CONSTITUTION.md` (sección "Stack y organización") con la decisión de navegación: React Navigation (bottom tabs + native-stack), cuatro pestañas del plan (§4).
- [x] Instalar `@react-navigation/native`, `@react-navigation/bottom-tabs`, `@react-navigation/native-stack`, `react-native-screens`, `react-native-safe-area-context`, `react-native-gesture-handler` y `expo-location` (`npx expo install`); añadir el plugin `expo-location` a `mobile/app.json` con el texto de permiso iOS.

## Cliente HTTP y tipos

- [x] Añadir a `src/api/types.ts`: `ClubStatus`, `ClubSummary`, `Court`, `ClubDetail`, `CourtSlot`, `SubmitClubRequest`.
- [x] Añadir a `src/api/httpClient.ts`: `getNearbyClubs`, `searchClubs`, `getClubDetails`, `getClubSlots`, `submitClub`.

## Navegación

- [x] Crear `ComingSoonScreen` (`src/components/ui/`), reutilizable para los placeholders de `Partidos`/`Crear`/`Actividad`.
- [x] Crear `AppTabs` (`src/app/`): bottom tabs `Partidos`/`Crear`/`Actividad`/`Perfil`; `Crear` y `Actividad` renderizan `ComingSoonScreen`; `Perfil` conserva el switch local existente `ProfileScreen` ⇄ `LevelSurveyScreen` de `m1-mobile-auth`, sin cambios de comportamiento.
- [x] Crear `PartidosTabScreen` (selector interno `[Partidos][Clubes]`, dos botones con `useState`, por defecto `Clubes`); el segmento `Partidos` renderiza `ComingSoonScreen`.
- [x] Crear `ClubsStackNavigator` (native-stack) con las pantallas `ClubsList`, `ClubDetail`, `CourtSlots`, `SubmitClub`.
- [x] Actualizar `RootNavigator.tsx`: la rama `authenticated` envuelve `AppTabs` en `NavigationContainer`; ramas `loading`/`unauthenticated` sin cambios.

## Pantallas de clubes (`src/features/clubs/`)

- [x] `ClubsListScreen`: permiso de geolocalización opcional (`expo-location`), llamada a `nearby` con o sin coordenadas (el fallback a `CityOrZone` lo resuelve el backend, el cliente no lo envía), campo de búsqueda que cambia a `search` con texto, fila con nombre/`cityOrZone`/distancia, botón para abrir `SubmitClub`.
- [x] `ClubDetailScreen`: nombre, dirección, insignia "no verificado" si `UserSubmitted`, lista de pistas; tocar una pista navega a `CourtSlots`.
- [x] `CourtSlotsScreen`: lista de huecos de una pista (ventana por defecto del backend), fecha/hora/duración formateadas en local.
- [x] `SubmitClubScreen`: formulario (`name`, `address` obligatorios; `cityOrZone` opcional), error inline en 400, éxito vuelve a `ClubsList`.

## Verificación manual (sin infraestructura de pruebas de UI en mobile)

- [x] Las cuatro pestañas navegan; `Partidos` abre en `Clubes` por defecto, `Partidos` muestra el placeholder.
- [x] Con permiso de ubicación concedido, `ClubsList` ordena por distancia (con el seed de desarrollo de `m2-clubs-courts`).
- [x] Con permiso denegado, `ClubsList` sigue mostrando clubes sin error.
- [x] Buscar por nombre encuentra clubes oficiales y aportados por usuarios.
- [x] Detalle de club muestra sus pistas; una pista muestra sus huecos con datos correctos.
- [x] Aportar un club sin nombre/dirección muestra el error del formulario, no un fallo genérico; con datos válidos, el club aparece luego en búsqueda.
- [x] `Crear` y `Actividad` muestran su placeholder sin romper la navegación.
- [x] `npm --prefix mobile run typecheck` y `npx expo-doctor` sin errores nuevos.

## Cierre

- [x] Actualizar el README: nuevas dependencias de navegación y geolocalización, plugin de `app.json`, cómo probar las pantallas de clubes.
- [x] Revisar cambios, instrucciones reproducibles y correspondencia con los criterios de aceptación del proposal.
- [x] Documentar la verificación con `pastiche-rdd`.
