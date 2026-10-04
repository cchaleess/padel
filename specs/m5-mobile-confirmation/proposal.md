# M5 — Mobile Confirmation

## Intención y problema

[m5-confirmation](../m5-confirmation/proposal.md) entregó el backend del ciclo de plaza (`Available`→`Held`→`Confirmed`, `POST /api/matches/{id}/hold|confirm|release`) sin ninguna pantalla que lo use. Esta spec construye esa parte en `mobile/`: desde el detalle de un partido, el jugador se une, pasa por un pago simulado con cuenta atrás de 5 minutos y queda confirmado.

Hay un hueco previo: `GET /api/matches/{id}` no dice nada de las plazas (m5-confirmation lo dejó explícitamente "para la spec de mobile correspondiente"). Sin eso, mobile no sabe si el partido está completo, cuántos confirmados hay ni si el propio jugador ya tiene plaza. Esta spec cierra ese hueco con un cambio pequeño de backend.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md) §11 (estados de plaza; `Held` "no se muestra visualmente como estado al resto"; mensaje "Esta plaza no está disponible temporalmente"), §14 (card del feed), §22 (`Open`→`Full`).
- [Constitución](../CONSTITUTION.md): el reloj del servidor es la autoridad de los vencimientos y la cuenta atrás móvil se basa en el tiempo comunicado por el servidor; abandonar libera la retención; nadie, ni el organizador, ocupa plaza sin pasar por el pago.
- [m5-confirmation/design.md](../m5-confirmation/design.md): endpoints que esta spec consume sin cambiar su comportamiento.
- [m4-mobile-feed](../m4-mobile-feed/proposal.md): el feed y la card que esta spec amplía con el contador.

## Decisiones de alcance (confirmadas con el usuario, 2026-10-04)

| Pregunta | Respuesta |
| --- | --- |
| ¿Cómo sabe mobile el estado de las plazas? | Se amplía `GET /api/matches/{id}` con `confirmedSeats` (0–4) y `mySeat` (`{ status, heldUntilUtc }` o `null`). Las `Held` de otros jugadores no se exponen ni cuentan (§11). |
| ¿Dónde ocurre el pago simulado? | En una pantalla aparte: "Unirme" retiene la plaza y navega a ella. Tiene cuenta atrás, "Pagar" (confirmar) y "Cancelar" (soltar). Salir hacia atrás sin pagar también suelta la plaza. |
| ¿Contador de confirmados en el feed? | Sí, en el detalle **y** en las cards del feed (`confirmedSeats` también en `MatchFeedItemResponse`). |

## Decisión añadida durante la verificación manual (2026-10-04)

Al ver en el móvil un partido del feed con «0/4», el usuario señaló que no cuadra: si alguien ha abierto el partido, ese alguien va a jugar. Se decidió, sin romper la regla de "nadie ocupa plaza sin pagar":

- **Crear lleva al pago**: al crear el partido, la plaza del organizador nace `Held` en la misma transacción, y mobile navega directamente a la pantalla de pago simulado. Refina el "crear no es unirse" de [m3-match-creation](../m3-match-creation/proposal.md): crear *inicia* la unión del organizador, pero solo pagar la confirma.
- **El feed solo muestra partidos con al menos 1 confirmado**: mientras el organizador no paga, el partido no es una oportunidad real y no aparece (principio 1 de la constitución). En la práctica, ningún partido del feed muestra «0/4».

Segunda observación, tras crear y pagar un partido: el usuario lo veía en su propio feed. El feed responde «¿a qué partidos puedo unirme?» (plan §13), así que:

- **El feed excluye los partidos que organizó el jugador y aquellos en los que ya tiene plaza activa** (`Confirmed` o `Held` vigente).
- **Los partidos con plaza confirmada se ven en Perfil → «Próximos»**, no en `Actividad`. El usuario propuso `Actividad`, pero el plan reserva esa pestaña para solicitudes y listas de espera (§4) y coloca «Próximos» en el perfil (§30). Se eligió seguir el plan y adelantar ahora una versión mínima de «Próximos» (solo partidos futuros con plaza confirmada), sin «Pendientes de cerrar» ni «Finalizados», que siguen en M13.

Tercera observación, al probar «Cancelar» tras crear un partido: volver al detalle no tiene sentido; quien cancela el pago abandona. **«Cancelar» en la pantalla de pago lleva a Partidos → feed** (y suelta la plaza como antes). El botón atrás de Android y el gesto de volver siguen llevando al detalle, que es lo esperado de "atrás".

Cuarta observación: el detalle debe mostrar quién juega. Siguiendo el plan, **el detalle muestra las 4 plazas**: las confirmadas con nombre y nivel individual (organizador primero, marcando «Tú» y «Organizador») y el resto como «Plaza libre» (§15). **La card del feed sigue sin nombres**, solo «x/4» (§14). Las plazas `Held` se ven como libres (§11).

Quinta observación: el pádel se juega 2 contra 2, y el jugador debe poder elegir si juega *con* el organizador o *contra* él. **Cada plaza tiene una posición (0–3): 0–1 son la pareja A, 2–3 la pareja B; el organizador nace en la 0.** El detalle muestra las dos parejas y cada plaza libre tiene su propio «Unirme», que retiene exactamente esa plaza. Si estaba retenida por otro (se ve libre, §11), el servidor responde «Esta plaza no está disponible temporalmente». El plan ya distingue equipo A y B para el resultado (§26), pero no decía cómo se forman; esta decisión lo fija al unirse. Cambiar de pareja tras confirmar queda fuera de alcance.

Sexta observación: el feed debe mostrar también los partidos propios. **El feed queda en cuatro secciones, en este orden: «Tienes estas partidas confirmadas»** (partidos futuros completos, 4/4, donde tengo plaza confirmada), **«Partidas pendientes de confirmación»** (tengo plaza confirmada pero el partido aún no está completo), **«Partidos para ti»** y **«Otros partidos cercanos»** (unibles, como antes). Matiza la decisión anterior de "el feed solo muestra unibles": las dos primeras secciones son partidos propios, no oportunidades. Encaja con el plan (§13): un partido completo desaparece del feed público pero «permanece visible para sus participantes». Después, el usuario pidió **quitar «Próximos» del Perfil**, porque esos partidos ya están en el feed. Se aparta del §30 del plan, que vuelve a revisarse en M13, cuando el perfil gane «Pendientes de cerrar» y «Finalizados».

Decisión explícita sobre partidos que se solapan en horario: **no se bloquean**. Se llegó a implementar un 409 «Ya tienes un partido a esa hora», pero el usuario lo descartó tras comprobar que Playtomic permite tener una plaza confirmada y crear otro partido a la misma hora. Es decisión del jugador, y el pago es el freno natural: nadie paga una plaza a la que no va a ir. Un test (`APlayerCanHoldSeatsInOverlappingMatches`) deja constancia para que no se reintroduzca por accidente.

Hueco aceptado y aplazado: si el organizador crea el partido y nunca paga, el `CourtSlot` queda bloqueado por un partido invisible. Se resolverá con la regla de liberación de pista del plan (§10), fuera de esta spec.

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Backend: detalle | `GET /api/matches/{id}` (y la respuesta de `POST /api/matches`) incluyen `confirmedSeats` y `mySeat` del jugador autenticado. Una `Held` propia caducada se trata como `mySeat: null`. |
| Backend: creación | La plaza del organizador nace `Held` (5 min) junto con el partido; las otras 3, `Available`. |
| Backend: feed | Cada item del feed incluye `confirmedSeats`; solo aparecen partidos con al menos 1 confirmado. |
| Feed: solo unibles | El feed no muestra partidos organizados por el jugador ni con plaza activa suya. |
| Feed: mis partidos | Mis partidos futuros con plaza confirmada aparecen al principio del feed: completos en «Tienes estas partidas confirmadas» y aún abiertos en «Partidas pendientes de confirmación». |
| Crear partido | Tras crearlo, mobile lleva al organizador directamente a la pantalla de pago, con el detalle debajo. |
| Detalle | Muestra "N/4 confirmados" y una acción según el estado: "Unirme" (sin plaza, partido `Open`), "Continuar pago" (tengo una `Held` vigente), "Tienes plaza confirmada" (sin acción), "Partido completo" (`Full`, sin plaza). Se recarga al volver a la pantalla. |
| Pago simulado | Cuenta atrás hasta `heldUntilUtc`; "Pagar" confirma y vuelve al detalle; "Cancelar" o salir hacia atrás sueltan la plaza. Al llegar a 0, se desactiva "Pagar" y se avisa de que la retención expiró. |
| Errores | Los 409 del backend (plaza no disponible, ya tienes plaza, retención expirada) se muestran con el mensaje del servidor. |
| Feed | Cada card muestra "N/4". |

## Fuera de alcance

- **Abandonar una plaza ya `Confirmed`**: depende de la lista de espera (M7), igual que en el backend.
- **Ver quiénes son los jugadores confirmados**: el plan lo liga al contexto del partido, pero ningún endpoint lo expone; se decide cuando haga falta (chat, M8+).
- **Pasarela de pago real**: el pago sigue simulado; la pantalla solo llama a `confirm`.
- **Reglas de calidad para unirse a un competitivo fuera de rango**: M6. Mientras tanto, el backend no bloquea unirse por nivel y esta spec tampoco lo hace en mobile.

## Criterios de aceptación

1. El detalle de un partido muestra el número de confirmados sobre 4.
2. Un jugador sin plaza en un partido `Open` puede pulsar "Unirme" y llega a la pantalla de pago con una cuenta atrás de ~5 minutos.
3. Pulsar "Pagar" deja la plaza confirmada: el detalle pasa a "Tienes plaza confirmada" y el contador sube en uno.
4. Pulsar "Cancelar" en la pantalla de pago suelta la plaza y lleva al feed; salir hacia atrás también la suelta y vuelve al detalle, que ofrece de nuevo "Unirme".
5. Si el jugador sale de la app con una plaza `Held` vigente y vuelve al detalle, ve "Continuar pago" con el tiempo restante correcto.
6. Al confirmarse la cuarta plaza, el detalle muestra "Partido completo" para quien no tiene plaza y el partido desaparece del feed.
7. Las cards del feed muestran "N/4".
8. Al crear un partido, el organizador llega directamente a la pantalla de pago; si paga, el partido aparece con «1/4» en el feed de otros jugadores; si cancela, llega a su feed y el partido no aparece en el de nadie.
9. Un partido que el jugador organizó o en el que tiene plaza no aparece entre los unibles de su feed; si tiene plaza confirmada, aparece en sus secciones propias.
10. El detalle muestra las dos parejas (A: plazas 1–2, B: 3–4) con los confirmados por posición, con nombre y nivel, y el resto como «Plaza libre».
11. Cada plaza libre tiene su «Unirme»: retiene exactamente esa plaza; si otro la tiene retenida, se muestra «Esta plaza no está disponible temporalmente».
12. El feed muestra, por este orden, «Tienes estas partidas confirmadas» (mis partidos 4/4), «Partidas pendientes de confirmación» (mis partidos aún abiertos), «Partidos para ti» y «Otros partidos cercanos».
