# M3 — Mobile Matches

## Intención y problema

[m3-match-creation/proposal.md](../m3-match-creation/proposal.md) entregó el contrato de backend para crear un partido a partir de un `CourtSlot` disponible, deliberadamente sin ninguna pantalla de `mobile/` que lo consuma (mismo patrón que M1/M2). Esta spec construye esas pantallas: elegir un club, una pista y un hueco disponible, definir el tipo de partido (y, si es competitivo, su rango de nivel y mínimo de partidos), crearlo, y consultar el detalle del partido creado.

El plan (§4) ya reserva la pestaña `Crear` exactamente para esto: "Crear partido desde un hueco disponible de club/pista." Hasta ahora esa pestaña es un placeholder (`ComingSoonScreen`, `m2-mobile-clubs`), y el segmento `Clubes` de `Partidos` lista huecos sin ninguna acción sobre ellos — `m2-mobile-clubs/proposal.md` marcó explícitamente "actuar sobre un hueco" como fuera de su alcance, remitiéndolo a esta spec.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §4 (pestaña `Crear`: "Crear partido desde un hueco disponible de club/pista"), §7 (tipos de partido), §12 (creación de partido competitivo: organizador, snapshot de nivel, rango, mínimo, nota).
- [m3-match-creation/design.md](../m3-match-creation/design.md): contrato de API que consume esta spec (`POST /api/matches`, `GET /api/matches/{id}`) y sus invariantes (crear no es unirse; el hueco pasa a `Booked`; rango de nivel validado contra el snapshot del organizador).
- [m2-mobile-clubs/design.md](../m2-mobile-clubs/design.md): pantallas y navegación de clubes ya existentes (`ClubsStackNavigator`: `ClubsList`→`ClubDetail`→`CourtSlots`), que esta spec extiende en vez de duplicar.
- [Constitución del proyecto](../CONSTITUTION.md): organización por feature (`src/features/matches/`), React Navigation como navegación transversal ya fijada, design system mínimo viable.

## Decisión de alcance: un único árbol de pantallas de clubes, ahora accionable

`ClubsStackNavigator` (`ClubsList`→`ClubDetail`→`CourtSlots`) se reutiliza tal cual bajo la pestaña `Crear`, en vez de construir un selector de club/pista/hueco distinto. La restricción de `m2-mobile-clubs` ("`CourtSlots` solo lista, no permite ninguna acción sobre un hueco") queda retirada porque dejó de tener sentido: ahora que el backend de M3 existe, tocar un hueco lleva a crear un partido **en cualquier punto de entrada**, tanto desde `Crear` como desde `Partidos`→`Clubes`. Evita duplicar código de descubrimiento/búsqueda/detalle de clubes por una separación de pestañas que el propio plan no exige (§4 solo dice qué acción vive en qué pestaña, no que el código deba estar aislado).

## Decisión de alcance: tras crear, se muestra el detalle del partido

`POST /api/matches` navega a una nueva pantalla de detalle (`MatchDetailScreen`, `GET /api/matches/{id}`) en vez de simplemente volver a la lista de huecos. Cierra el ciclo con el endpoint que `m3-match-creation` construyó explícitamente para esto (criterio de aceptación 7 de esa spec) y le da al jugador una confirmación clara de lo que acaba de crear (club, pista, horario, tipo, rango/mínimo/nota).

## Decisión de alcance: sin nivel, la opción Competitive se deshabilita en el formulario

El backend rechaza (400) un partido `Competitive` si el organizador no tiene `Level` (encuesta de nivel sin completar). El formulario deshabilita esa opción y muestra una nota explicativa en vez de dejar que el jugador la elija y reciba el error tras rellenar el resto del formulario — mejora de UX que no reemplaza la validación real, que sigue en el backend (constitución: "las comprobaciones de UI no constituyen una garantía de integridad").

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Acción sobre un hueco | Al tocar un hueco disponible en `CourtSlots` (desde `Crear` o desde `Partidos`→`Clubes`), el jugador navega a un formulario de creación de partido para ese hueco concreto. |
| Elegir tipo de partido | El jugador elige `Friendly` o `Competitive`; `Competitive` aparece deshabilitado con nota si el jugador no tiene `Level`. |
| Rango y mínimo (competitivo) | Para `Competitive`, el jugador introduce `minLevel`/`maxLevel` (obligatorios) y opcionalmente un mínimo de partidos jugados. |
| Nota | El jugador puede añadir una nota opcional (máx. 500 caracteres), en ambos tipos. |
| Crear el partido | Al enviar, se llama a `POST /api/matches`; éxito navega al detalle del partido creado; errores de validación (400) y de hueco no disponible (409) se muestran inline, sin mensajes genéricos. |
| Detalle del partido | Nueva pantalla que muestra club, pista, horario, tipo, y rango/mínimo/nota si aplica, consumiendo `GET /api/matches/{id}`. |

## Fuera de alcance

- **Unirse a un partido, feed de partidos, lista de partidos propios**: fuera de esta spec (M4/M5); `Partidos` sigue siendo un placeholder salvo por la acción de crear que ya vive en `Clubes`.
- **Editar o cancelar un partido ya creado**: el backend de `m3-match-creation` no lo permite; esta spec tampoco lo anticipa en la UI.
- **Selector de fechas en `CourtSlots`**: sigue fuera de alcance, igual que en `m2-mobile-clubs` — la ventana por defecto del backend (14 días) es suficiente.
- **Notificaciones o confirmaciones push tras crear un partido**: fuera del MVP de mobile por ahora.
- **Cualquier UI relacionada con plazas confirmadas, pago simulado o lista de espera**: pertenece a M5+; esta spec no muestra ni sugiere ninguna plaza ocupada, ni siquiera la del organizador (mismo invariante que el backend).

## Criterios de aceptación

1. Un jugador puede tocar un hueco disponible (desde `Crear` o desde `Partidos`→`Clubes`) y llegar a un formulario de creación de partido para ese hueco.
2. Un jugador puede crear un partido `Friendly` sin necesidad de tener `Level`.
3. Un jugador sin `Level` ve la opción `Competitive` deshabilitada, con una nota que explica por qué.
4. Un jugador con `Level` puede crear un partido `Competitive` indicando un rango de nivel válido y, opcionalmente, un mínimo de partidos y una nota.
5. Tras crear el partido con éxito, el jugador ve su detalle (club, pista, horario, tipo, rango/mínimo/nota, estado).
6. Un error de validación (rango inválido, nota demasiado larga) o de hueco ya no disponible se muestra de forma específica, no como un fallo genérico.
7. Crear un partido no sugiere en ningún momento que el jugador ya ocupa una plaza confirmada.

## Decisiones técnicas reservadas para el diseño

`design.md` concretará: los cambios exactos a `ClubsStackNavigator`/`CourtSlotsScreen` para hacer accionable un hueco, la estructura del formulario de creación (`CreateMatchScreen`), los tipos y funciones nuevas de `src/api/`, si se extrae un componente `OptionGroup` compartido (ya usado de forma no compartida en `LevelSurveyScreen`), y el checklist de verificación manual.
