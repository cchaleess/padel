# Tareas — M4 Mobile Feed

- [x] Añadir a `mobile/src/api/types.ts`: `MatchFeedItem`, `MatchFeed`.
- [x] Añadir a `mobile/src/api/httpClient.ts`: `api.getMatchFeed(params?)` (`GET /api/matches/feed`, mismo patrón que `getNearbyClubs`).
- [x] Extraer `src/features/matches/matchTypeLabel.ts` (`getMatchTypeLabel`) desde el `typeLabels` local de `MatchDetailScreen.tsx`; actualizar `MatchDetailScreen.tsx` para usarlo.
- [x] Desacoplar `MatchDetailScreen.tsx` de `ClubsStackParamList` (tipar `route.params` directamente).
- [x] Crear `src/features/matches/FeedScreen.tsx`: permiso de ubicación (mismo patrón que `ClubsListScreen`), `useFocusEffect` para recargar al entrar, `RefreshControl` para pull-to-refresh, `SectionList` con `forYou`/`outOfRange` (sección vacía omitida), card con club/horario/tipo/rango, navega a `MatchDetail`.
- [x] Crear `src/features/matches/PartidosStackNavigator.tsx` (`Feed`→`MatchDetail`).
- [x] Actualizar `PartidosTabScreen.tsx`: segmento por defecto `partidos`; monta `PartidosStackNavigator` en vez de `ComingSoonScreen` para ese segmento; retirar el comentario obsoleto.
- [x] Verificación manual en dispositivo: checklist de design.md. Hecha 2026-10-04 en dispositivo Android físico: feed cargado al entrar (3 partidos, 2 `Competitive` + 1 `Friendly`, todos en "Partidos para ti" — sección "Otros partidos cercanos" correctamente omitida al no haber ninguno fuera de rango); card con club/horario/tipo/rango; navegación al detalle y vuelta al feed correctas; pull-to-refresh OK; permiso de ubicación denegado no rompe el feed. Bug de entorno encontrado (no de la feature): Metro llevaba corriendo desde antes de un `npm install` de la sesión anterior y no había recogido los paquetes actualizados (`Unable to resolve "expo-modules-core"`, error 500 del bundler en el dispositivo) — corregido reiniciando Metro con `--clear` tras liberar el puerto 8081 de un proceso colgado.
- [x] `npm run typecheck` y `npx expo-doctor` sin errores nuevos (21/21).
- [x] Actualizar el README: sección de mobile para M4.
- [x] Revisar cambios y correspondencia con los criterios de aceptación del proposal. Los 6 criterios quedan cubiertos: 1 (segmento `Partidos` por defecto, feed sin tocar nada), 2 (dos secciones, una omitida correctamente al estar vacía), 3 (card mínima: club/horario/tipo/rango), 4 (navegación al detalle), 5 (funciona con y sin permiso de ubicación), 6 (recarga al entrar/volver y con pull-to-refresh).
- [x] Documentar la verificación con `pastiche-rdd`.
