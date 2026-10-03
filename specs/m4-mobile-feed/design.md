# Diseño — M4 Mobile Feed

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): el segmento `Partidos` de la pestaña del mismo nombre pasa de `ComingSoonScreen` a un feed real que consume `GET /api/matches/feed` (m4-discovery), con sus propias dos secciones y navegación al detalle ya existente.

## Navegación

```text
PartidosTabScreen
├─ segmento "Partidos" (por defecto, antes "Clubes") → PartidosStackNavigator [nuevo]
│    ├─ Feed              → FeedScreen [nuevo]
│    └─ MatchDetail        → MatchDetailScreen (reutilizada, ver abajo)
└─ segmento "Clubes" → ClubsStackNavigator (sin cambios)
```

- `PartidosTabScreen.tsx`: `useState<Segment>('clubes')` pasa a `useState<Segment>('partidos')`; el segmento `partidos` monta `<PartidosStackNavigator />` en vez de `<ComingSoonScreen title="Partidos" />`. Se retira el comentario que explicaba el valor por defecto anterior (ya no aplica).
- Nuevo `src/features/matches/PartidosStackNavigator.tsx`: dos pantallas, `Feed` (inicial, título "Partidos") y `MatchDetail` (título "Partido").

## `MatchDetailScreen` se desacopla de `ClubsStackParamList`

Hoy `MatchDetailScreen` tipa sus props como `NativeStackScreenProps<ClubsStackParamList, 'MatchDetail'>`, pero solo usa `route.params.matchId` — no usa `navigation`. Se reutiliza desde dos árboles de navegación distintos (`ClubsStackNavigator` y el nuevo `PartidosStackNavigator`), así que su tipo de props deja de depender de `ClubsStackParamList`:

```ts
type Props = { route: { params: { matchId: string } } };
```

Sin cambios de comportamiento ni de importación de `ClubsStackParamList`. Ambos navegadores declaran `MatchDetail: { matchId: string }` en su propio `ParamList` — la forma coincide, así que React Navigation tipa correctamente la navegación desde cualquiera de los dos.

## `FeedScreen` (nuevo, `src/features/matches/FeedScreen.tsx`)

- Permiso de ubicación: mismo patrón que `ClubsListScreen` (`expo-location`, `requestForegroundPermissionsAsync`). Si se concede, pide el feed con `lat`/`lng`; si no, lo pide sin ellos (el backend cae a la `cityOrZone` del jugador).
- Recarga automática: `useFocusEffect` (ya usado indirectamente por React Navigation en otras pantallas del proyecto a través de la navegación por pestañas/stack; aquí se usa explícitamente) vuelve a pedir el feed cada vez que la pantalla gana foco — cubre "entro a la app y ya veo los partidos" sin que el jugador tenga que hacer nada.
- Pull-to-refresh: `RefreshControl` sobre la lista, llama a la misma función de carga.
- Estructura: `SectionList` con hasta dos secciones, `{ title: 'Partidos para ti', data: forYou }` y `{ title: 'Otros partidos cercanos', data: outOfRange }` — se omite una sección si su `data` está vacío (evita una cabecera sin contenido debajo). Si ambas están vacías, un único mensaje de estado vacío ("No hay partidos disponibles por ahora.") en vez de las dos secciones.
- Cada fila (card): club (`item.clubName`), horario (`formatSlotSchedule(item.startsAt, item.endsAt)`, reutilizado de `src/features/clubs/slotFormatting.ts`), tipo (`Amistoso`/`Competitivo`, vía `getMatchTypeLabel`, ver abajo) y, si `type === 'Competitive'`, el rango `minLevel`–`maxLevel`. Nada de distancia, calidad ni contador de confirmados (el backend no los expone). `Pressable` → `navigation.navigate('MatchDetail', { matchId: item.id })`.

## Etiqueta de tipo compartida (nuevo, pequeño)

`MatchDetailScreen` ya tiene un `typeLabels` local (`Friendly`→"Amistoso", `Competitive`→"Competitivo"). `FeedScreen` necesita la misma traducción — segundo uso real, mismo criterio que `OptionGroup`/`slotFormatting` en `m3-mobile-matches`. Se extrae a `src/features/matches/matchTypeLabel.ts`:

```ts
export function getMatchTypeLabel(type: MatchType): string {
  return type === 'Competitive' ? 'Competitivo' : 'Amistoso';
}
```

`MatchDetailScreen.tsx` pasa a importarla en vez de definir su propio objeto, sin cambiar lo que muestra.

## Cliente HTTP y tipos (`src/api/`)

```ts
// types.ts (añade)
interface MatchFeedItem {
  id: string; clubId: string; clubName: string; courtName: string;
  startsAt: string; endsAt: string; durationMinutes: number;
  type: MatchType; minLevel: number | null; maxLevel: number | null; distanceKm: number | null;
}
interface MatchFeed {
  forYou: MatchFeedItem[];
  outOfRange: MatchFeedItem[];
}

// httpClient.ts (añade a `api`)
getMatchFeed(params?: { lat?: number; lng?: number }): Promise<MatchFeed>   // GET /api/matches/feed
```

Mismo patrón que `getNearbyClubs`: con `lat`/`lng` los añade como query params; sin ellos, pide el feed sin parámetros (el backend ya resuelve la `cityOrZone` del jugador por su cuenta — mobile no necesita conocerla ni enviarla).

## Verificación

Sin infraestructura de pruebas de UI en `mobile/` (igual que M1/M2/M3): verificación manual en dispositivo físico, más `npm run typecheck` y `npx expo-doctor`. Checklist:

- Al entrar a la app (o volver a la pestaña `Partidos` desde otra), se ve el segmento `Partidos` por defecto, con el feed ya cargado sin tocar nada.
- Con al menos un partido `Friendly` y uno `Competitive` creados (reutilizar el flujo de `m3-mobile-matches`), ambos aparecen en la sección que corresponda según el `Level` del jugador que verifica.
- Sin `Level`, cualquier partido `Competitive` aparece en "Otros partidos cercanos", nunca en "Partidos para ti".
- Tocar una card navega al detalle correcto (mismos datos que al crear el partido); el botón atrás vuelve al feed, no al formulario de creación.
- Deslizar hacia abajo en el feed lo recarga (indicador de refresco visible).
- Denegar el permiso de ubicación no rompe el feed (sigue devolviendo partidos, solo sin orden por distancia).
- `npm run typecheck` y `npx expo-doctor` sin errores nuevos.

## Alternativas y límites

- **Sin filtros manuales ni buscador**: el feed ya llega pre-agrupado/ordenado desde el backend; no hay volumen todavía que justifique añadir controles en mobile (proposal.md).
- **Sección vacía se omite en vez de mostrar "no hay partidos de este tipo"**: con el catálogo de datos de desarrollo tan pequeño, es fácil que una de las dos secciones esté vacía casi siempre; un mensaje por sección añadiría ruido visual sin aportar información nueva frente a que la sección simplemente no aparezca.
- **Sin paginación**: igual que el backend (m4-discovery), no se justifica todavía con el volumen esperado del MVP.
