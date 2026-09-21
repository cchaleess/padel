# M2 — Mobile Clubs

## Intención y problema

[m2-clubs-courts/proposal.md](../m2-clubs-courts/proposal.md) entregó el contrato de backend de clubes, pistas y huecos disponibles, en modo de solo lectura, pero — deliberadamente, siguiendo el patrón adoptado tras M1 — sin la UI de `mobile/` que lo consume. Esta spec construye esas pantallas: descubrir clubes cercanos, buscar por nombre, ver el detalle de un club y sus huecos disponibles, y aportar un club nuevo.

Esta spec introduce además algo nuevo: es la primera vez que el plan exige una navegación con más de una pantalla simultánea (§4, barra de pestañas). Hasta ahora `mobile/` no tiene ninguna librería de navegación — `RootNavigator` es un switch manual de estado entre tres pantallas (`m1-mobile-auth`). Esta spec resuelve esa carencia de infraestructura a la vez que entrega las pantallas de clubes.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §4 (navegación principal: pestañas Partidos/Crear/Actividad/Perfil), §5 (geolocalización opcional, ciudad/zona como alternativa), §8 (clubes y geolocalización, incluyendo el selector interno Partidos/Clubes dentro de la pestaña Partidos), §9 (disponibilidad de pista).
- [m2-clubs-courts/design.md](../m2-clubs-courts/design.md): contrato de API que consume esta spec (`GET /api/clubs/nearby`, `/search`, `/{id}`, `/{id}/slots`, `POST /api/clubs`).
- [Constitución del proyecto](../CONSTITUTION.md): organización de `mobile/` por feature (`src/features/clubs/`), design system mínimo viable. Esta spec añade una decisión de navegación que también se incorpora a la constitución (ver más abajo).
- [m1-mobile-auth/proposal.md](../m1-mobile-auth/proposal.md) y [design.md](../m1-mobile-auth/design.md): precedente del patrón backend/mobile separado, y de las piezas ya existentes que esta spec reutiliza (`AuthContext`, `httpClient`, `Player.CityOrZone` del perfil).

## Decisión de alcance: navegación con pestañas (afecta a la constitución)

**Se construye ya la barra de navegación completa del plan, con React Navigation.** Decisión del usuario (2026-09-13): en vez de esperar a que Match (M3/M4) obligue a introducir navegación, esta spec instala **React Navigation** (bottom tabs + stacks) y monta las cuatro pestañas del §4 (`Partidos`, `Crear`, `Actividad`, `Perfil`). Dentro de `Partidos` se construye también el selector interno `[Partidos][Clubes]` del §8: la vista `Partidos` (feed de partidos) es un placeholder explícito ("Próximamente") hasta que M4 la implemente; `Clubes` es el contenido real de esta spec. `Crear` y `Actividad` son placeholders equivalentes (M3 y M7+ respectivamente). `Perfil` envuelve las pantallas ya existentes de `m1-mobile-auth` (`ProfileScreen`, `LevelSurveyScreen`) sin cambiar su comportamiento.

Se elige React Navigation sobre Expo Router (decisión del usuario, 2026-09-13) porque no exige reorganizar `src/app/` a una convención de enrutado por archivos para una app que hoy solo tiene tres pantallas; se integra directamente sobre la organización por feature ya fijada en la constitución.

**Por qué esto es también una decisión de constitución:** es una decisión de arquitectura mobile transversal, del mismo tipo que la organización por feature fijada durante M1 — todas las specs de M3 en adelante (Match, Actividad) construirán sobre esta misma navegación. Se documenta en `CONSTITUTION.md`, no solo aquí, para que esas specs no tengan que redescubrirla ni reabrir el debate de qué librería usar.

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Navegación con pestañas | Un jugador ve la barra de pestañas del plan; `Partidos` muestra el selector interno `[Partidos][Clubes]`, con `Partidos` como placeholder. |
| Descubrimiento de clubes cercanos | Un jugador ve una lista de clubes; si concede el permiso de geolocalización, se ordenan por distancia; si lo rechaza o el dispositivo no puede resolverlo, la app sigue funcionando usando `CityOrZone` del perfil (ya existente desde M1) como alternativa, igual que decide el backend. |
| Búsqueda de clubes | Un jugador puede buscar clubes por nombre y ver tanto oficiales como aportados por usuarios. |
| Detalle de club | Un jugador puede abrir un club y ver su nombre, dirección, estado (oficial/no verificado) y sus pistas. |
| Huecos disponibles | Un jugador puede consultar los huecos de una pista/club, con fecha, hora de inicio, duración y hora de fin, legibles en la zona horaria de la constitución. |
| Aportar un club | Un jugador autenticado puede aportar un club nuevo (nombre, dirección, ciudad/zona opcional) y verlo después en búsqueda/descubrimiento, marcado como no verificado. |

## Fuera de alcance

- **Contenido real de `Partidos`, `Crear` y `Actividad`**: placeholders explícitos hasta M3/M4/M7+. Esta spec no anticipa esas pantallas.
- **Actuar sobre un hueco** (seleccionarlo para crear un partido): esta spec solo lista huecos, no permite ninguna acción sobre ellos — eso es M3.
- **Mapa interactivo de clubes**: fuera del MVP (plan §40); la lista se muestra en lista, conforme al §8.
- **Filtros de búsqueda avanzados o paginación**: el backend de `m2-clubs-courts` no los tiene todavía; el dataset esperado no lo justifica.
- **Editar o eliminar un club aportado**: solo alta, sin gestión posterior.
- **Geocodificación de direcciones**: igual que en el backend, un club aportado sin coordenadas nunca participa del orden por distancia real.

## Criterios de aceptación

1. Un jugador ve la barra de pestañas del plan (`Partidos`/`Crear`/`Actividad`/`Perfil`); `Partidos` muestra el selector `[Partidos][Clubes]`, con `Partidos` como placeholder que no sugiere una funcionalidad inexistente.
2. Un jugador puede conceder o denegar el permiso de geolocalización; en ambos casos la lista de clubes cercanos se muestra (con o sin orden por distancia), sin bloquear la navegación ni mostrar un error.
3. Un jugador puede buscar clubes por nombre y ver tanto oficiales como aportados por usuarios.
4. Un jugador puede abrir el detalle de un club y ver sus pistas.
5. Un jugador puede consultar los huecos disponibles de una pista/club, con fecha/hora/duración legibles.
6. Un jugador autenticado puede aportar un club nuevo desde la app y verlo aparecer después en búsqueda/descubrimiento, marcado como no verificado.
7. `Crear` y `Actividad` muestran su placeholder sin errores ni pantallas en blanco.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: la estructura exacta de navegadores (qué pestañas llevan su propio stack interno), el manejo del permiso de geolocalización (`expo-location`), los estados de carga/error de cada pantalla, el formulario de aportar club, y el contenido mínimo de los placeholders de `Partidos`/`Crear`/`Actividad`. También concretará la actualización de `CONSTITUTION.md` con la decisión de navegación.
