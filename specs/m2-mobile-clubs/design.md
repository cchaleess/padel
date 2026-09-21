# Diseño — M2 Mobile Clubs

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): navegación con pestañas (React Navigation) y las pantallas de clubes que consumen [m2-clubs-courts](../m2-clubs-courts/design.md). Se mantiene la organización por feature ya fijada en la constitución (`src/features/clubs/`), y se reutiliza sin cambios `AuthContext`, `httpClient` y `Player.CityOrZone` de `m1-mobile-auth`.

Decisión central de UX: **descubrimiento y búsqueda de clubes viven en una sola pantalla** (`ClubsListScreen`), no en dos. Un campo de búsqueda arriba: vacío, muestra el descubrimiento por cercanía; con texto, cambia a búsqueda por nombre. Evita una navegación extra para una distinción que, desde el punto de vista del jugador, es solo "encontrar un club" — coherente con el principio de constitución "UI simple, dominio ambicioso".

## Actualización de la constitución: navegación

Se añade a `CONSTITUTION.md` (sección "Stack y organización") la decisión de esta spec: **React Navigation** (`@react-navigation/native`, `bottom-tabs`, `native-stack`) es la librería de navegación de `mobile/`, con las cuatro pestañas del plan (§4) como estructura de nivel superior. Es una decisión transversal — M3+ construye sobre ella — del mismo tipo que la organización por feature fijada durante M1, y se documenta igual: en la constitución, no solo en esta spec.

## Estructura de navegación

```text
NavigationContainer
└─ AppTabs (bottom tabs)
   ├─ "Partidos" → PartidosTabScreen (selector interno, no es un navigator aparte)
   │    ├─ segmento "Partidos" → ComingSoonScreen("Partidos") — placeholder hasta M4
   │    └─ segmento "Clubes" (por defecto) → ClubsStackNavigator (native-stack)
   │         ├─ ClubsList
   │         ├─ ClubDetail       (clubId)
   │         ├─ CourtSlots       (clubId, courtId, courtName)
   │         └─ SubmitClub
   ├─ "Crear" → ComingSoonScreen("Crear") — placeholder hasta M3
   ├─ "Actividad" → ComingSoonScreen("Actividad") — placeholder hasta M7+
   └─ "Perfil" → ProfileTab (switch local existente de m1-mobile-auth: ProfileScreen ⇄ LevelSurveyScreen, sin cambios)
```

- `RootNavigator.tsx` (M1) cambia solo en su rama `authenticated`: en vez de renderizar directamente `ProfileScreen`/`LevelSurveyScreen`, envuelve `AppTabs` en `NavigationContainer`. Las ramas `loading`/`unauthenticated` no cambian.
- El selector `PartidosTabScreen` es un componente propio con `useState<'partidos' | 'clubes'>('clubes')` — dos botones simples (mismo patrón visual que las opciones de `LevelSurveyScreen`), no una librería de tabs adicional: son solo dos segmentos, no justifica `@react-navigation/material-top-tabs`. **Por defecto abre en "Clubes"**, no en "Partidos" (aunque el plan §8 da prioridad a partidos): mostrar por defecto un placeholder vacío sería peor UX que mostrar el contenido real disponible. Se documenta como decisión explícita, revertida cuando M4 dé contenido real a "Partidos".
- `ComingSoonScreen` (`src/components/ui/ComingSoonScreen.tsx`): componente compartido, recibe `title`, muestra los tokens de `src/theme/` ya existentes (eyebrow + título + nota "Próximamente"). Un solo componente para los tres placeholders, no tres pantallas casi idénticas.

## Pantallas (`src/features/clubs/`)

- **`ClubsListScreen`**: al montar, pide permiso de geolocalización (`expo-location`, `requestForegroundPermissionsAsync`); si se concede y `getCurrentPositionAsync` resuelve, llama a `GET /api/clubs/nearby?lat&lng`; si se rechaza o falla, llama a `GET /api/clubs/nearby` sin coordenadas — **el backend ya resuelve el fallback a `CityOrZone` del jugador autenticado sin que el cliente tenga que enviarlo** (`ClubDiscoveryService.GetNearbyClubsAsync` lee el perfil del jugador vía el JWT de sesión). El cliente nunca necesita conocer ni enviar el `CityOrZone`: una simplificación real que evita duplicar esa lógica en mobile. Un campo de búsqueda arriba filtra en vivo (`GET /api/clubs/search?q=`) con un debounce de 350 ms mientras tenga texto; al vaciarlo, vuelve automáticamente al descubrimiento por cercanía — un único `useEffect` sobre el texto de búsqueda gobierna ambos casos, sin una acción de "enviar" separada (revisado tras la primera verificación manual: la versión inicial buscaba solo al enviar y no revertía al vaciar el campo; se sustituyó por este único mecanismo reactivo). Cada fila muestra nombre, `cityOrZone`, y la distancia si `distanceKm` no es nulo. Un botón (header derecho) abre `SubmitClub`.
- **`ClubDetailScreen`**: `GET /api/clubs/{id}`; muestra nombre, dirección, insignia "no verificado" si `status = UserSubmitted`, y la lista de pistas; tocar una pista navega a `CourtSlots`.
- **`CourtSlotsScreen`**: `GET /api/clubs/{clubId}/slots?courtId={courtId}` (ventana por defecto del backend, 14 días — sin selector de fechas en esta spec, no hay todavía ninguna acción sobre un hueco que lo justifique); lista `startsAt`/`endsAt`/duración formateados en local (el dispositivo ya está en `Europe/Madrid` según la constitución).
- **`SubmitClubScreen`**: formulario mínimo (`name`, `address` obligatorios; `cityOrZone` opcional) contra `POST /api/clubs`; éxito vuelve a `ClubsList` (`navigation.goBack()`); error de validación (400) se muestra igual que en `ProfileScreen`/`LevelSurveyScreen` (mensaje inline, no genérico).

Todas siguen el patrón ya existente en `m1-mobile-auth` (estado local `isLoading`/`error`, `useEffect` para la carga inicial, sin librería de estado global nueva).

## Cliente HTTP y tipos (`src/api/`)

```ts
// types.ts (añade)
type ClubStatus = 'Official' | 'UserSubmitted';
interface ClubSummary { id: string; name: string; cityOrZone: string | null; status: ClubStatus; distanceKm: number | null; }
interface Court { id: string; name: string; }
interface ClubDetail { id: string; name: string; address: string; cityOrZone: string | null; status: ClubStatus; courts: Court[]; }
interface CourtSlot { id: string; courtId: string; courtName: string; startsAt: string; endsAt: string; durationMinutes: number; }
interface SubmitClubRequest { name: string; address: string; cityOrZone?: string | null; }

// httpClient.ts (añade a `api`)
getNearbyClubs(params?: { lat?: number; lng?: number }): Promise<ClubSummary[]>   // GET /api/clubs/nearby
searchClubs(query: string): Promise<ClubSummary[]>                                // GET /api/clubs/search
getClubDetails(id: string): Promise<ClubDetail>                                   // GET /api/clubs/{id}
getClubSlots(clubId: string, courtId?: string): Promise<CourtSlot[]>              // GET /api/clubs/{id}/slots
submitClub(body: SubmitClubRequest): Promise<ClubDetail>                          // POST /api/clubs
```

Mismo `request<T>` genérico ya existente (base URL, cabecera `Authorization`, manejo de 401): no se toca `httpClient.ts` más que añadir estas cinco funciones.

## Geolocalización (`expo-location`)

- Nuevo plugin en `app.json`: `expo-location`, con el texto de permiso iOS (`NSLocationWhenInUseUsageDescription` vía la config del plugin) explicando que se usa para ordenar clubes cercanos.
- Si el usuario deniega el permiso, no se vuelve a preguntar automáticamente en esa sesión (comportamiento estándar del SDK); `ClubsListScreen` simplemente sigue sin coordenadas. No hay una pantalla de "activa tu ubicación desde ajustes": fuera de alcance, es una mejora de UX, no un bloqueo funcional (criterio de aceptación 2 del proposal ya está cubierto sin ella).

## Verificación

Sin infraestructura de pruebas de UI en `mobile/` (igual que `m1-mobile-auth`): verificación manual en dispositivo físico Android, más `npm run typecheck` y `npx expo-doctor`. Checklist:

- Las cuatro pestañas se ven y navegan; `Partidos` abre en `Clubes` por defecto, con el segmento `Partidos` mostrando el placeholder.
- Con permiso de ubicación concedido, `ClubsList` muestra clubes ordenados por distancia (usando el seed de desarrollo de `m2-clubs-courts`, con clubes en ciudades distintas).
- Con permiso denegado, `ClubsList` sigue mostrando clubes (orden por `CityOrZone`/nombre), sin error visible.
- Buscar por nombre en `ClubsList` encuentra clubes oficiales y aportados por usuarios.
- Abrir un club muestra sus pistas; tocar una pista muestra sus huecos con fecha/hora/duración correctas.
- Aportar un club con nombre/dirección lo deja buscable después; sin nombre o dirección, el formulario muestra el error, no un fallo genérico.
- `Crear` y `Actividad` muestran su placeholder sin romper la navegación.
- `npm run typecheck` y `npx expo-doctor` sin errores nuevos.

## Alternativas y límites

- No se usa `@react-navigation/material-top-tabs` para el selector `Partidos`/`Clubes`: son dos segmentos fijos, un componente propio con `useState` es más simple y no añade una dependencia para tan poco.
- No hay pantalla de ajustes de ubicación ni reintento guiado si se deniega el permiso: mejora de UX diferida, no bloqueante.
- Sin selector de fechas en `CourtSlots`: la ventana por defecto del backend (14 días) es suficiente mientras no exista ninguna acción real sobre un hueco (M3).
- Sin mapa de clubes, sin filtros avanzados, sin paginación: mismos límites ya documentados en `m2-clubs-courts/design.md`, ahora también en el cliente.
