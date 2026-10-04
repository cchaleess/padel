# M7 — Leave a Match

## Intención y problema

Un jugador confirmado no puede salirse de un partido: se aplazó en [m5-confirmation](../m5-confirmation/proposal.md) porque el plan liga la salida a la lista de espera (M7, §17 y §23). Esta spec construye la salida, pero **sin lista de espera**, por decisión del usuario.

Solo backend, como en M3–M6. Mobile va en una spec aparte.

## Decisión de alcance: sin lista de espera (2026-10-04)

La spec empezó como `m7-waitlist` y se implementó entera: lista de hasta 2, plaza reservada para la lista y elección del organizador con pago al ser elegido. Al revisarla, el usuario la descartó. Playtomic no tiene ese concepto: te apuntas si hay plaza y, si no, el partido ni aparece en tu feed. Se eliminó todo antes de commitear nada.

Reglas que fijó el usuario en su lugar:

- **Abandonar se puede mientras el partido no esté cerrado.** Cerrado = las 4 plazas pagadas (`Full`). Y siempre antes del inicio (§23).
- **Abandonar solo libera la plaza.** Sin aviso de devolución ni mensajes sobre el club: las políticas de cancelación y devolución aún no están pensadas (corrección del usuario sobre una primera versión que las anunciaba). La constitución las deja a la capa de clubes, sin spec todavía.

Se mantiene del plan: si el organizador abandona, el rol pasa al confirmado más antiguo (§18).

Se aparta del plan en el §17 (no hay lista de espera ni elección del organizador). M7 queda como «abandonar plaza».

## Alcance incluido

| Área | Resultado esperado |
| --- | --- |
| Abandonar | `POST /api/matches/{id}/leave`: un confirmado libera su plaza si el partido no está cerrado ni ha empezado. |
| Partido cerrado | 409 «El partido está completo: ya no se puede abandonar.» |
| Vuelta al feed | La plaza liberada vuelve a estar disponible para cualquiera y el partido sigue en el feed de los demás. |
| Organizador | Si abandona, el rol pasa al confirmado con plaza pagada desde hace más tiempo. Si no queda nadie, conserva el organizador. |
| Solicitudes de acceso (M6) | Con un votante menos, una solicitud pendiente queda aprobada si todos los que siguen ya la aprobaron. |

## Fuera de alcance

- **Lista de espera** (§17): descartada; ver arriba.
- **Baja con el partido cerrado**, **devoluciones** y **políticas de cancelación**: sin pensar todavía (capa de clubes y pagos).
- **Después del inicio**, lesión y no-show: M11.
- **Partido sin confirmados** (todos abandonan): sigue existiendo, invisible y con el hueco bloqueado. Es el mismo hueco ya aceptado en m5-mobile-confirmation y se resolverá con la liberación de pista (§10).

## Criterios de aceptación

1. Un confirmado abandona un partido abierto antes del inicio: su plaza queda libre y el partido sigue en el feed de los demás, que pueden ocupar esa plaza.
2. Con el partido cerrado (4/4), abandonar falla.
3. Sin plaza confirmada, o con el partido ya empezado, abandonar falla.
4. Si abandona el organizador, el rol pasa al confirmado más antiguo.
5. Si se va el único votante que faltaba en una solicitud de acceso, esta queda aprobada.
