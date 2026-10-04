# M6 — Mobile Quality Rules

## Intención y problema

[m6-quality-rules](../m6-quality-rules/proposal.md) dejó en el backend los criterios de acceso a competitivos, las solicitudes excepcionales y la votación unánime. En la app nada de eso se ve todavía: un jugador fuera de rango pulsa «Unirme» y recibe un error, sin opción de pedir acceso; y nadie puede votar. Esta spec lo lleva a mobile.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §4 (`Actividad`: solicitudes pendientes y listas de espera), §13 (fuera de rango: «Requiere aprobación»), §16 (aprobación excepcional).
- [m6-quality-rules/design.md](../m6-quality-rules/design.md): `myAccess` y `pendingRequests` en el detalle, `exception-requests` y `GET /api/activity`, sin cambios de backend.
- [dev-player-simulation](../dev-player-simulation/design.md): se amplía `dev-sim.ps1` para simular solicitudes y votos de otros jugadores.

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Detalle: no cumplo criterios | Si `canJoinDirectly` es falso y no tengo solicitud aprobada, el detalle explica por qué (sin nivel, nivel por debajo o por encima del rango, pocos partidos) y las plazas vacías dicen **«Solicitar acceso»** en vez de «Plaza libre»: tocar una envía la solicitud. Sin botón aparte (decisión del usuario, 2026-10-04, en la verificación en dispositivo). La solicitud **guarda la plaza** desde la que se pidió y solo esa muestra «Solicitud enviada» (corrección del usuario: al principio se marcaban todas). No es una reserva: aprobada, el jugador se une en cualquier plaza libre. Los votantes ven en qué pareja quiere jugar. |
| Detalle: estado de mi solicitud | «Solicitud enviada, esperando a los jugadores confirmados», «Tu solicitud fue rechazada» o, si está aprobada, «Unirme» normal en las plazas libres. |
| Detalle: votar | Los confirmados ven las solicitudes pendientes del partido: jugador, nivel, motivo, votos (`x/y`) y botones **Aprobar / Rechazar** (o su voto ya emitido). |
| Pestaña Actividad | Deja de ser un placeholder: «Solicitudes que tienes que votar» (con Aprobar/Rechazar y acceso al partido) y «Tus solicitudes» con su estado. Se recarga al entrar y con pull-to-refresh. |
| Solicitud que se queda sin hueco | Caso señalado por el usuario (2026-10-04): mientras se vota, otros jugadores que sí cumplen los criterios se apuntan. **Si el partido se completa, las solicitudes pendientes pasan a «Expirada»** en el mismo momento en que se confirma la 4.ª plaza, y el detalle y Actividad lo dicen. **Si solo se ocupa la plaza pedida**, la solicitud sigue pendiente y el detalle avisa de que, si la aprueban, podrá elegir otra libre. Cierra el hueco que m6-quality-rules había dejado fuera de alcance. |
| Aviso | **Contador en la pestaña Actividad** con las solicitudes pendientes de mi voto (añadido 2026-10-04, a petición del usuario: el votante no sabía dónde le llegaba la solicitud). Sustituye a las notificaciones hasta §21. |
| Feed | La sección de partidos no unibles directamente pasa a llamarse **«Requieren aprobación»** (§13), en vez de «Otros partidos cercanos». |
| Simulación | `dev-sim.ps1` gana `create-match -Competitive` (con rango), `request` (un ficticio pide acceso) y `vote` (los ficticios confirmados votan las solicitudes pendientes). |

## Fuera de alcance

- Notificaciones push de solicitudes (§21).
- Retirar una solicitud.
- Cambios de backend, salvo dos: guardar la plaza solicitada (`RequestedPosition`, migración `AddAccessRequestPosition`) y el estado `Expired`.

## Criterios de aceptación

1. En un competitivo cuyo rango no me incluye, el detalle dice por qué y las plazas vacías dicen «Solicitar acceso»; tocar una envía la solicitud y solo esa plaza pasa a «Solicitud enviada».
2. Tras pedir acceso, el detalle muestra la solicitud como pendiente y aparece en Actividad → «Tus solicitudes».
3. Si un jugador confirmado la rechaza, el detalle y Actividad la muestran rechazada y sigo sin poder unirme.
4. Si todos los confirmados la aprueban, el detalle vuelve a ofrecer «Unirme» y puedo pagar plaza.
5. Como confirmado, veo en el detalle y en Actividad las solicitudes pendientes y puedo aprobarlas o rechazarlas; tras votar, salen de «tienes que votar».
6. El feed titula «Requieren aprobación» la sección de partidos no unibles directamente.
7. La pestaña Actividad muestra un contador con las solicitudes que tengo que votar, que baja al votar.
8. Si el partido se completa con mi solicitud pendiente, pasa a «Expirada» y lo veo en el detalle y en Actividad; si solo se ocupa la plaza que pedí, sigue pendiente con un aviso.
