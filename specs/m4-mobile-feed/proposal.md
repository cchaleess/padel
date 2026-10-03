# M4 — Mobile Feed

## Intención y problema

[m4-discovery](../m4-discovery/proposal.md) entregó el backend del feed de partidos (`GET /api/matches/feed`), deliberadamente sin ninguna pantalla de `mobile/` que lo consuma (mismo patrón que M1/M2/M3). Esta spec construye esa pantalla: el jugador entra a la pestaña `Partidos` y ve, sin tocar nada más, los partidos a los que puede unirse — igual que el feed de una red social.

El segmento `Partidos` de esa pestaña es hoy un `ComingSoonScreen` (comentario en el código: "porque Partidos no tiene contenido real hasta M4"). Esta spec lo sustituye.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §13 (descubrimiento: dos secciones, "para ti" y "fuera de rango/requiere aprobación"), §14 (card de partido: información mínima, sin distancia ni nombres de jugadores).
- [m4-discovery/design.md](../m4-discovery/design.md): forma exacta de `GET /api/matches/feed` (`forYou`/`outOfRange`, parámetros `lat`/`lng`/`cityOrZone`) que esta spec consume sin cambios.
- [m2-mobile-clubs/design.md](../m2-mobile-clubs/design.md): patrón ya existente de permiso de ubicación (`expo-location`, `ClubsListScreen`) que esta spec reutiliza tal cual para pedir `lat`/`lng` antes de pedir el feed.
- [Constitución del proyecto](../CONSTITUTION.md) §84: design system pendiente para fases posteriores — esta spec no introduce ninguno, usa los mismos estilos inline ya presentes en el resto de `mobile/`.

## Decisión de alcance: el feed es automático, no una pestaña que hay que "activar"

Hoy la pestaña `Partidos` arranca en el segmento `Clubes` por defecto (el comentario en `PartidosTabScreen.tsx` explica por qué: `Partidos` no tenía contenido real). Con el feed ya construido, el segmento por defecto pasa a ser `Partidos`: en cuanto el jugador entra a la app (o vuelve a esa pestaña), ve el feed sin tocar nada — confirmado explícitamente con el usuario, que lo comparó con el comportamiento esperado de un feed de red social. `Clubes` sigue accesible tocando el otro segmento, sin cambios en su contenido.

El feed también se recarga solo al entrar o volver a la pestaña (sin que el jugador tenga que refrescar manualmente), más un gesto de "pull to refresh" para el caso en que quiera forzar una recarga sin salir y volver a entrar.

## Decisión de alcance: reutilizar el detalle de partido ya existente

Tocar una card del feed navega al detalle del partido — la misma `MatchDetailScreen` que ya construyó `m3-mobile-matches` para el organizador tras crear un partido (`GET /api/matches/{id}`, sin cambios). No se construye un detalle nuevo ni distinto para "partidos de otros".

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Feed automático | La pestaña `Partidos` arranca en el segmento del mismo nombre y pide el feed sin interacción del jugador. |
| Permiso de ubicación | Mismo patrón que el catálogo de clubes: si se concede, se pide el feed con `lat`/`lng`; si no, sin ellos (el backend cae a la `cityOrZone` del jugador). |
| Dos secciones | "Partidos para ti" (`forYou`) y "Otros partidos cercanos" (`outOfRange`), en ese orden, cada una con los partidos que devuelve el backend ya ordenados. |
| Card de partido | Club, horario, tipo, y rango de nivel si es `Competitive` — sin distancia, sin calidad estimada, sin contador de confirmados (el backend no los expone todavía). |
| Ver detalle | Tocar una card navega al detalle existente del partido. |
| Recarga | Automática al entrar/volver a la pestaña, más pull-to-refresh manual. |

## Fuera de alcance

- **Unirse, solicitar acceso**: M5. El feed es de solo lectura, igual que el backend que consume.
- **Calidad estimada, contador de confirmados, distancia en la card**: no los expone el backend todavía (m4-discovery); no se inventan en mobile.
- **Filtros manuales** (por tipo, por club, por fecha): el plan no los pide para esta fase; el feed ya llega pre-agrupado y ordenado desde el backend.
- **"Mis partidos" (los que organizo)**: fuera de alcance, igual que en el backend.

## Criterios de aceptación

1. Al entrar a la pestaña `Partidos`, el jugador ve el feed sin tocar nada (segmento `Partidos` por defecto).
2. El feed muestra dos secciones, "Partidos para ti" y "Otros partidos cercanos", con los partidos que devuelve el backend.
3. Cada card muestra club, horario, tipo, y rango de nivel si aplica — nada más.
4. Tocar una card navega al detalle del partido (club, pista, horario, tipo, rango/mínimo/nota si aplica).
5. Si se concede el permiso de ubicación, el feed se pide con `lat`/`lng`; si se deniega, el feed sigue funcionando sin ellos.
6. Volver a la pestaña `Partidos` recarga el feed; deslizar hacia abajo también lo recarga.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: la estructura de pantallas/navegación (`PartidosTabScreen` ya no monta `ComingSoonScreen` para el segmento `Partidos`), cómo se desacopla `MatchDetailScreen` de `ClubsStackParamList` para reutilizarse desde dos árboles de navegación distintos, los tipos/funciones nuevos de `src/api/`, y la estructura del componente de card.
