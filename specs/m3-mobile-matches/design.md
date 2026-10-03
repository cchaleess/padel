# Diseño — M3 Mobile Matches

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): hacer accionable un hueco disponible (`CourtSlots`) para crear un partido, y mostrar su detalle tras crearlo. Se reutiliza `ClubsStackNavigator` (`m2-mobile-clubs`) en vez de construir un árbol de pantallas nuevo, y se añaden las pantallas nuevas al feature `matches` (`src/features/matches/`), consistente con la organización por feature ya fijada en la constitución.

## Preguntas resueltas

Confirmadas por el usuario antes de fijar este diseño (recomendaciones aceptadas sin cambios):

1. **¿Un único árbol de pantallas de clubes, accionable en cualquier sitio, o un selector aparte solo para `Crear`?** Un único árbol: `ClubsStackNavigator` se monta tal cual bajo la pestaña `Crear` (segunda instancia, estado de navegación independiente) y también sigue montado bajo `Partidos`→`Clubes`. Tocar un hueco disponible siempre lleva a crear un partido, en cualquiera de los dos puntos de entrada. La restricción de solo-lectura de `m2-mobile-clubs` queda retirada.
2. **¿A qué pantalla se navega tras crear el partido?** A una nueva `MatchDetailScreen` (`GET /api/matches/{id}`), con `navigation.replace` (no `navigate`) para que el botón atrás no vuelva al formulario.
3. **¿Se deshabilita la opción `Competitive` cuando el jugador no tiene `Level`?** Sí, con una nota explicativa (`useAuth().player.level`); el backend sigue siendo la autoridad real de la validación.

## Navegación

```text
AppTabs
├─ "Partidos" → PartidosTabScreen
│    ├─ segmento "Partidos" → ComingSoonScreen (sin cambios, M4)
│    └─ segmento "Clubes" (por defecto) → ClubsStackNavigator (instancia A)
├─ "Crear" → ClubsStackNavigator (instancia B, antes ComingSoonScreen)
├─ "Actividad" → ComingSoonScreen (sin cambios, M7+)
└─ "Perfil" → ProfileTab (sin cambios)

ClubsStackNavigator (ambas instancias, mismo árbol)
├─ ClubsList
├─ ClubDetail       (clubId)
├─ CourtSlots        (clubId, courtId, courtName)   — cada fila de hueco pasa a ser accionable
├─ CreateMatch        (courtSlotId, courtName, startsAt, endsAt, durationMinutes)   [nuevo]
└─ MatchDetail         (matchId)                                                   [nuevo]
```

- `AppTabs.tsx`: `CrearTab` deja de ser `<ComingSoonScreen title="Crear" />` y pasa a `<ClubsStackNavigator />` — el mismo componente que ya usa `PartidosTabScreen` para el segmento `Clubes`. Al ser dos usos independientes del mismo componente en dos `Tab.Screen` distintos, React Navigation les da estados de pila independientes (uno no interfiere con el otro).
- `ClubsStackNavigator.tsx`: se añaden `CreateMatch` y `MatchDetail` al `ClubsStackParamList` y sus `Stack.Screen`, importando las pantallas desde `../matches/`.
- `CourtSlotsScreen.tsx`: la fila de cada hueco pasa de `View` a `Pressable`, navegando a `CreateMatch` con `{ courtSlotId: item.id, courtName, startsAt: item.startsAt, endsAt: item.endsAt, durationMinutes: item.durationMinutes }`.

**Límite aceptado de la reutilización** (documentado, no corregido en esta spec): al montar el mismo `ClubsStackNavigator` bajo `Crear`, su pantalla inicial sigue titulada "Clubes" y conserva el botón de "aportar club" — coherente con "un único árbol accionable en cualquier sitio" (pregunta 1), pero un jugador que entra por `Crear` ve primero una lista de clubes con ese título y esa acción, no un texto ajustado a "elegir dónde crear un partido". Ajustar el título/acciones por punto de entrada exigiría parametrizar el navegador; se difiere hasta que el volumen de pantallas lo justifique (mismo criterio que el resto del design system mínimo viable de la constitución).

## Pantallas nuevas (`src/features/matches/`)

### `CreateMatchScreen`

Recibe por parámetros de ruta el hueco elegido (`courtSlotId`, `courtName`, `startsAt`, `endsAt`, `durationMinutes`) — mismo patrón que `CourtSlotsScreen`, sin volver a pedir horario/duración (ya son los del hueco).

- Cabecera: pista y horario formateados con `formatSlotSchedule` (nuevo, ver abajo).
- Tipo de partido: `OptionGroup` (nuevo componente compartido, ver abajo) con dos opciones, `Friendly` y `Competitive`, **sin ninguna seleccionada por defecto** (corrección 2026-10-03, confirmada por el usuario durante la verificación manual: el jugador debe tocar una opción explícitamente); `Competitive` deshabilitada si `useAuth().player?.level == null`, con una nota debajo ("Completa la encuesta de nivel para crear partidos competitivos."). El botón de envío permanece deshabilitado hasta que se elige un tipo.
- Si `type === 'Competitive'`: dos `TextInput` (`keyboardType="decimal-pad"`) para `minLevel`/`maxLevel`, obligatorios; un `TextInput` (`keyboardType="number-pad"`) opcional para `minMatchesRequired`.
- `TextInput` multilínea opcional para `note` (ambos tipos), `maxLength={500}` para evitar de raíz el 400 por longitud.
- `canSubmit`: siempre para `Friendly`; para `Competitive`, además exige `minLevel`/`maxLevel` no vacíos y parseables (`Number(value.replace(',', '.'))`, tolerando coma decimal).
- Al enviar: `api.createMatch({ courtSlotId, type, minLevel, maxLevel, minMatchesRequired, note })` (números `undefined` cuando no aplica/no se ha rellenado; `note` recortado, `null` si vacío). Éxito → `navigation.replace('MatchDetail', { matchId: created.id })`. Error → mensaje inline igual que `SubmitClubScreen` (`err instanceof ApiError ? err.message : 'No se ha podido crear el partido.'`); el mensaje del backend (400 de validación, 409 de hueco no disponible) se muestra tal cual, mismo patrón ya existente — las cadenas de error del backend están en inglés, límite ya presente en `m2-mobile-clubs` y no corregido por esta spec.

### `MatchDetailScreen`

Recibe `matchId` por parámetro de ruta; al montar, `GET /api/matches/{id}` (`api.getMatchDetails`). Estados `isLoading`/`error`, mismo patrón que `ClubDetailScreen`. Muestra: nombre de club, pista, horario (`formatSlotSchedule`), tipo (`Amistoso`/`Competitivo`), y si es `Competitive`: rango de nivel, mínimo de partidos si lo hay, nivel del organizador en el momento de crear; nota si la hay. No muestra ni sugiere ninguna plaza ocupada — el `MatchDetailResponse` del backend no expone ese dato (criterio de aceptación 7 del proposal).

## Componentes compartidos nuevos

- **`src/components/ui/OptionGroup.tsx`**: extraído del componente `OptionGroup` ya existente, hoy definido localmente en `LevelSurveyScreen.tsx`. Segundo uso real (tipo de partido) que justifica moverlo a compartido, mismo criterio incremental que el resto del design system ("pantalla a pantalla hasta que el volumen lo justifique"). Añade soporte de opciones deshabilitadas (`options[].disabled?: boolean`) que `LevelSurveyScreen` no necesitaba. `LevelSurveyScreen.tsx` pasa a importarlo en vez de definirlo, sin cambiar su comportamiento.
- **`src/features/clubs/slotFormatting.ts`**: extrae `formatSlotSchedule(startsAt: string, endsAt: string): string` de la lógica ya existente (hoy duplicada conceptualmente si se repitiera) en `CourtSlotsScreen.tsx`. La usan `CourtSlotsScreen`, `CreateMatchScreen` y `MatchDetailScreen` — tercer punto de uso real de la misma lógica de formato.

## Cliente HTTP y tipos (`src/api/`)

```ts
// types.ts (añade)
type MatchType = 'Competitive' | 'Friendly';
type MatchStatus = 'Open';
interface MatchDetail {
  id: string; clubId: string; clubName: string; courtId: string; courtName: string;
  startsAt: string; endsAt: string; durationMinutes: number;
  type: MatchType; status: MatchStatus; organizerId: string;
  organizerLevelAtCreation: number | null; minLevel: number | null; maxLevel: number | null;
  minMatchesRequired: number | null; note: string | null;
}
interface CreateMatchRequest {
  courtSlotId: string; type: MatchType;
  minLevel?: number | null; maxLevel?: number | null; minMatchesRequired?: number | null; note?: string | null;
}

// httpClient.ts (añade a `api`)
createMatch(body: CreateMatchRequest): Promise<MatchDetail>   // POST /api/matches
getMatchDetails(id: string): Promise<MatchDetail>              // GET /api/matches/{id}
```

Mismo `request<T>` genérico ya existente (base URL, cabecera `Authorization`, manejo de 401 y de errores no-2xx vía `ApiError`): no se toca `httpClient.ts` más que añadir estas dos funciones.

## Verificación

Sin infraestructura de pruebas de UI en `mobile/` (igual que `m1-mobile-auth`/`m2-mobile-clubs`): verificación manual en dispositivo físico Android, más `npm run typecheck` y `npx expo-doctor`. Checklist:

- Desde `Crear`, se ve la lista de clubes (mismo contenido que `Partidos`→`Clubes`); elegir club→pista→hueco lleva al formulario de creación.
- Desde `Partidos`→`Clubes`, tocar un hueco también lleva al mismo formulario (mismo comportamiento en ambos puntos de entrada).
- Sin `Level` (jugador sin encuesta completada): `Competitive` aparece deshabilitada con su nota; crear un `Friendly` funciona sin pedir nivel ni rango.
- Con `Level` (completar la encuesta desde `Perfil` primero si hace falta): crear un `Competitive` con rango válido funciona; un rango inválido (`minLevel > maxLevel`) muestra el error del backend inline, sin fallo genérico.
- Tras crear cualquier partido, se navega a su detalle con los datos correctos (club, pista, horario, tipo, rango/mínimo/nota); el botón atrás desde el detalle no vuelve al formulario.
- Crear un segundo partido sobre el mismo hueco (recargando `CourtSlots`, que ya no debería listarlo como disponible tras la primera creación) no es necesario forzarlo manualmente: basta con comprobar que el hueco ya creado desaparece de la lista de huecos disponibles al volver atrás y recargar.
- `npm run typecheck` y `npx expo-doctor` sin errores nuevos.

## Corrección de diseño (2026-10-03): ClubDetail y CourtSlots se fusionan, sin selector de pista

Durante la verificación manual, el usuario planteó que la pista importa menos que el horario disponible: forzar a elegir pista antes de ver huecos es un paso de más cuando lo relevante es "¿qué hora tengo libre?". Se confirmó con el usuario (recomendaciones aceptadas):

1. **El cambio se documenta en esta misma spec** (no cerrada, sin RDD verify todavía), con nota fechada — mismo patrón que la corrección in-place de `m1-mobile-auth`. Aunque la pantalla afectada (`ClubDetailScreen`) nació en `m2-mobile-clubs` (ya cerrada), el cambio real vive en el código de `mobile/src/features/clubs/`, no en esa spec histórica.
2. **`ClubDetailScreen` y `CourtSlotsScreen` se fusionan en una sola pantalla.** `ClubDetailScreen` pasa a mostrar la cabecera del club (nombre, estado, dirección) seguida de la lista única de horarios disponibles de **todas las pistas del club**, eliminando el paso intermedio de elegir pista. `CourtSlotsScreen.tsx` se borra; su lógica de lista (formateo, `Pressable`→`CreateMatch`) se incorpora a `ClubDetailScreen.tsx`.
3. **Orden de la lista: por hora de inicio, luego por duración — sin cabeceras de grupo visuales.** Las filas con la misma hora de inicio quedan adyacentes por el propio orden (ej. `08:00-09:00`, `08:00-09:30`, `08:00-10:00`, luego `08:30-...`), igual que el ejemplo que dio el usuario. Cada fila sigue mostrando el nombre de la pista como dato secundario (sigue siendo relevante para el jugador, solo que ya no es el criterio de navegación).

### Cambios concretos

- **Backend: ninguno.** `GET /clubs/{id}/slots` ya acepta `courtId` opcional (`ClubEndpoints.cs:50-51`) y cada `CourtSlotResponse` ya incluye `courtName` (`ClubResponses.cs:29`); `api.getClubSlots(clubId, courtId?)` en el cliente mobile ya tenía `courtId` como opcional. Basta con no pasarlo.
- **`ClubsStackNavigator.tsx`**: se retira `CourtSlots` de `ClubsStackParamList` y de los `Stack.Screen`. `ClubDetail` sigue siendo la única entrada entre `ClubsList` y `CreateMatch`.
- **`ClubDetailScreen.tsx`**: sustituye la sección "Pistas" (lista de `club.courts`, navega a `CourtSlots`) por una lista de horarios: `api.getClubSlots(clubId)` (sin `courtId`), ordenada con `.sort((a, b) => a.startsAt === b.startsAt ? a.durationMinutes - b.durationMinutes : a.startsAt.localeCompare(b.startsAt))`; cada fila (`Pressable`) muestra `formatSlotSchedule(startsAt, endsAt)` + nombre de pista, y navega a `CreateMatch` con los mismos parámetros que antes (`courtSlotId`, `courtName: item.courtName`, `startsAt`, `endsAt`, `durationMinutes`).
- **`CourtSlotsScreen.tsx`**: se elimina.
- **Sin cambios**: `CreateMatchScreen.tsx`, `MatchDetailScreen.tsx`, `OptionGroup.tsx`, `slotFormatting.ts`, tipos/cliente HTTP (`api.getClubSlots` ya soportaba esta llamada).

### Checklist de verificación (sustituye al punto 1-2 del checklist original)

- Desde `Crear` y desde `Partidos`→`Clubes`, entrar a un club muestra directamente la lista de horarios (sin selector de pista intermedio), ordenada por hora y luego por duración, con la pista visible en cada fila.
- El resto del checklist original (sin `Level`, con `Level`, rango inválido, navegación tras crear, hueco ya no listado) se mantiene igual, ahora partiendo de esta pantalla fusionada.

## Alternativas y límites

- No se parametriza el título/las acciones de `ClubsStackNavigator` según si se entra por `Crear` o por `Partidos`→`Clubes`: ver "Límite aceptado de la reutilización" arriba.
- No se valida en cliente que `organizerLevelAtCreation` (el nivel propio del jugador, ya conocido vía `useAuth().player.level`) caiga dentro del rango `minLevel`/`maxLevel` antes de enviar: el backend ya lo hace y devuelve un 400 claro; añadir esa validación en cliente sería duplicar una regla sin beneficio de UX proporcional al esfuerzo en esta primera versión.
- Los mensajes de error del backend se muestran tal cual (en inglés): límite de idioma ya presente en `m2-mobile-clubs`, no introducido ni corregido por esta spec.
- Sin listado de "mis partidos creados" ni feed: fuera de alcance (M4/M5), ver proposal.md.
