# Dev — Simulación de otros jugadores

## Intención y problema

PadelMatch es una app donde interactúan muchos jugadores. Desde M5, casi todo lo que hay que probar en el móvil depende de que *otro* jugador haga algo: crear y pagar un partido para que aparezca en tu feed, unirse para que el contador suba o completar el partido. En local solo hay una cuenta de Google y la API solo acepta tokens reales de Google, así que esos escenarios no se pueden reproducir.

Esta spec añade una herramienta de desarrollo transversal (la usarán M6, M7 y siguientes) para actuar como otros jugadores sin cuentas reales.

## Decisiones (confirmadas con el usuario, 2026-10-04)

| Pregunta | Respuesta |
| --- | --- |
| Alcance | Endpoint de sesión de desarrollo **+** script **+** login de prueba en la app. |
| Ubicación | Spec propia, no dentro de una spec de feature. |

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Sesión de desarrollo | `POST /api/dev/session { name }` crea (o reutiliza por nombre) un jugador ficticio y devuelve la misma respuesta que el login con Google. Solo existe en el entorno `Development`. |
| Script | `scripts/dev-sim.ps1` con comandos para que jugadores ficticios creen y paguen un partido, se unan a uno existente o retengan una plaza sin pagar. Todo pasa por los endpoints reales. |
| Nivel | Los jugadores ficticios hacen la encuesta de nivel al crearse (fecha de nacimiento ficticia + respuestas derivadas del nombre), así que muestran un nivel estable y variado como un jugador real. Añadido 2026-10-04 a petición del usuario. |
| Login de prueba | En builds de desarrollo, la pantalla de login ofrece entrar como jugador de prueba, para ver la app desde la perspectiva de otro jugador. |

## Restricciones

- **Nada de esto existe fuera de desarrollo**: el endpoint solo se mapea con `IsDevelopment()`, y el botón de mobile solo se muestra con `__DEV__`.
- **Sin atajos de dominio**: los jugadores ficticios retienen y pagan a través de los mismos endpoints que un jugador real (constitución: ninguna plaza se ocupa sin pasar por `Held`→`Confirmed`). El script no escribe en la base de datos.

## Fuera de alcance

- Partidos competitivos desde el script (los ficticios ya tienen nivel, pero `create-match` solo crea amistosos). Se añadirá cuando M6 lo necesite.
- Cualquier uso en staging o producción.

## Criterios de aceptación

1. Con la API en `Development`, `POST /api/dev/session` devuelve un token válido para un jugador ficticio, siempre el mismo para el mismo nombre.
2. Fuera de `Development`, esa ruta devuelve 404.
3. `dev-sim.ps1 create-match` deja un partido amistoso con 1/4 confirmados, visible en el feed del usuario real.
4. `dev-sim.ps1 join <matchId>` suma confirmados a ese partido, hasta completarlo si se pide.
5. En un build de desarrollo, el login permite entrar como jugador de prueba sin Google.
