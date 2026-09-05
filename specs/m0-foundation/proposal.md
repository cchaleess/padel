# M0 — Foundation

## Intención y problema

Establecer una base técnica ejecutable y reproducible para desarrollar la plataforma móvil de partidos de pádel. El repositorio contiene el plan del MVP, pero todavía no dispone de la solución backend, la persistencia ni el cliente móvil sobre los que construir los siguientes milestones.

M0 debe permitir compilar y arrancar el backend, trabajar con PostgreSQL mediante EF Core y migraciones, consultar el contrato OpenAPI y arrancar la estructura inicial de React Native/Expo. El resultado habilita M1 y los siguientes incrementos sin anticipar sus funcionalidades.

## Referencias y restricciones

- [MVP-PLAN.md](../../MVP-PLAN.md), §38, M0: alcance de Foundation.
- [Constitución del proyecto](../CONSTITUTION.md): stack, organización modular, integridad y principios transversales derivados del plan, especialmente §§3, 35 y 41.
- Las entidades de §34 y las rutas orientativas de §37 describen el conjunto del MVP; no son una lista de entregables de M0.

El proposal respeta la separación `backend/` y `mobile/` y los cuatro proyectos backend propuestos en el plan. No selecciona todavía versiones, librerías adicionales ni mecanismos técnicos concretos.

## Alcance incluido

| Área del M0 original | Resultado esperado |
| --- | --- |
| Solución .NET | Una solución que pueda restaurarse y compilarse desde el repositorio. |
| Proyectos | La estructura `PadelMatch.Api`, `PadelMatch.Application`, `PadelMatch.Domain` y `PadelMatch.Infrastructure`, coherente con la arquitectura modular / Clean Architecture ligera. |
| PostgreSQL | Una base de datos de desarrollo accesible mediante una configuración local documentada. |
| Entity Framework Core | La integración de persistencia necesaria para comprobar que el backend accede a PostgreSQL. |
| Migraciones | Un punto de partida versionado y un procedimiento reproducible para inicializar y evolucionar la base de datos mediante EF Core. |
| OpenAPI | Un documento OpenAPI accesible y una forma documentada de consultarlo durante el desarrollo. |
| React Native / Expo | La estructura móvil inicial, capaz de arrancar y mostrar una pantalla mínima, con soporte previsto para Android e iOS. |

La documentación de arranque y la verificación básica acompañan estos entregables para que otra persona pueda reproducirlos. La forma concreta de demostrar la persistencia y las migraciones se definirá en el diseño sin añadir funcionalidades de negocio ajenas a M0.

## Fuera de alcance

- Registro, autenticación funcional, perfiles, encuesta y cálculo de nivel (M1 y evolución posterior).
- Clubes, pistas y disponibilidad como funcionalidades de producto (M2).
- Definir la capa de gestión del club y sus políticas de pista/reserva: cancelaciones automáticas, excepciones operativas, devoluciones y cambios de pista. Su definición corresponde a una spec posterior de clubes.
- Creación y descubrimiento de partidos, participación, confirmación simulada, solicitudes excepcionales, lista de espera y ciclo de reserva (M3–M9).
- Chat, coordinación, notificaciones, resultados, incidencias, evaluaciones e histórico (M8–M13).
- Modelar por adelantado todas las entidades, estados o rutas del MVP.
- Implementar en Foundation las transacciones de plazas, las restricciones de capacidad y los procesos de expiración de futuros milestones. Las garantías de la constitución siguen siendo obligatorias cuando esos flujos se incorporen.
- Pagos reales, gestión completa de clubes, algoritmo definitivo de nivel y design system final.
- Despliegue productivo, publicación en tiendas, observabilidad y QA completo del MVP (M14). M0 sí incluye comprobar que su propia base técnica funciona en desarrollo.

## Criterios de aceptación

Estos criterios describen cómo reconocer que M0 está terminado cuando se implemente; no son resultados de verificaciones ya realizadas.

1. Desde una copia limpia del repositorio y con los prerrequisitos documentados, una persona puede restaurar dependencias, compilar la solución y arrancar la API siguiendo las instrucciones del proyecto.
2. Existen los cuatro proyectos backend y la estructura móvil prevista, con una separación coherente con la constitución.
3. El backend establece una conexión real con PostgreSQL mediante EF Core usando la configuración de desarrollo documentada.
4. El procedimiento de migraciones inicializa una base de datos vacía y deja registrado su estado; ejecutarlo de nuevo sin migraciones pendientes no produce errores ni cambios adicionales. La estrategia inicial no exige implementar el dominio de milestones posteriores.
5. Con la API en marcha, se puede obtener y consultar un documento OpenAPI válido y correspondiente a la API disponible. No se exige exponer las rutas de negocio orientativas del plan.
6. El proyecto Expo arranca y muestra su pantalla mínima. Las instrucciones cubren Android e iOS; la comprobación registra las plataformas realmente verificadas y cualquier limitación del entorno.
7. Las instrucciones de desarrollo explican prerrequisitos, configuración, arranque de PostgreSQL, migraciones, ejecución de API y cliente, y consulta de OpenAPI con suficiente detalle para reproducir la verificación.

## Aclaraciones y ambigüedades del plan

Las aclaraciones A1–A3 quedan resueltas para esta revisión mediante las respuestas recogidas a continuación. Las políticas de gestión de pista pertenecen a una capa de clubes todavía por definir; su detalle se difiere expresamente a esa spec. Resolver estas aclaraciones no sustituye la revisión final de constitution y proposal solicitada por el usuario: no se avanza todavía a `design.md` ni a `tasks.md`.

### A1. ¿El organizador ocupa una plaza automáticamente al crear el partido?

Las secciones 12 y 18 identifican al creador como organizador, pero no aclaran cómo obtiene su plaza. La sección 11 indica que una plaza solo queda ocupada al finalizar correctamente la confirmación.

**Decisión del usuario:** ningún jugador ocupa definitivamente una plaza hasta que se haya confirmado el pago. Esto incluye al organizador. En el MVP el pago se simula; crear/publicar el partido no basta para pasar a `Confirmed`.

La aclaración fija la condición de confirmación, pero no prescribe en qué paso se inicia una retención `Held`; ese detalle se concretará en el flujo correspondiente. Afecta principalmente a M3 y M5. También deberá respetarse al diseñar la entrada desde lista de espera (M7): el paso directo a `Confirmed` descrito en §17 no puede omitir la condición de pago confirmado.

### A2. ¿Qué pasa si abandona el único jugador confirmado?

La sección 18 mantiene activo el partido cuando sale el organizador y transfiere su rol al confirmado con mayor antigüedad. La sección 23 permite abandonar antes del inicio, pero ninguna define qué hacer cuando ya no queda un confirmado al que transferir el rol.

**Decisión del usuario:** si el único jugador confirmado abandona la partida antes del inicio, no hay reemplazo y el hueco de pista queda disponible para reservarse de nuevo.

Como consecuencia, la partida deja de estar activa sobre ese hueco y no se transfiere el rol de organizador ni se selecciona un sustituto. La liberación se produce por esa salida, sin esperar al plazo de cancelación previo al inicio definido en A3. Es una consecuencia automática del abandono permitido en §23; no añade una acción de cancelación manual del partido al MVP.

El diseño del ciclo de reserva deberá preservar la exclusividad del hueco frente a confirmaciones en curso y mantener el registro interno previsto por la constitución. La gestión de esa liberación y cualquier devolución asociada corresponden a la capa de clubes pendiente de definición; esta respuesta no determina una política de reembolso. Afecta al ciclo del partido, la sucesión del organizador y la reserva.

### A3. ¿Qué política temporal rige `StartsAt`, `EndsAt` y las expiraciones?

La sección 9 establece `EndsAt = StartsAt + Duration`, pero no define la zona horaria de esas fechas ni su representación en API y persistencia. Las secciones 10, 11, 22 y 26 fijan plazos sin una política temporal común.

**Decisión del usuario sobre la zona horaria:** clubes y jugadores comparten una única zona horaria de España. `StartsAt` y `EndsAt` se introducen y muestran en esa zona común; el MVP no necesita presentar una hora distinta para cada jugador.

**Decisión del usuario sobre el reloj y el cómputo:** el servidor es la autoridad temporal. Los instantes se almacenan e intercambian en UTC y los plazos se calculan como tiempo real transcurrido, manteniendo la zona española compartida para introducir y mostrar horarios. Un reloj móvil desajustado no modifica los vencimientos.

El backend calcula y guarda los vencimientos y comprueba el plazo al confirmar. La cuenta atrás de la app se basa en el tiempo comunicado por el servidor y se actualiza al reconectar. El identificador técnico de la zona y los tipos concretos de API y persistencia se definirán en el diseño, respetando esta política y los cambios de horario.

**Decisión del usuario sobre el reintento de pago:** el reintento reinicia el plazo de cinco minutos. En el caso consultado, un pago iniciado a las 12:00 que falla y se reintenta a las 12:03 tiene un nuevo vencimiento a las 12:08.

Se mantiene la regla de §11: un error libera la retención y se puede volver a intentar si la plaza continúa disponible. El nuevo plazo no permite desplazar a otro jugador que haya obtenido la plaza entretanto.

**Decisión del usuario sobre el límite de la retención:** se acepta la confirmación del pago que llega exactamente en el instante de vencimiento. Si la retención vence a las 12:05:00, una confirmación recibida a las 12:05:00 está dentro de plazo. El límite es inclusivo; esta decisión no concede tiempo adicional después del vencimiento. La liberación automática debe respetar ese límite y la confirmación atómica de la plaza.

**Decisión del usuario sobre la antelación de creación:** se permite crear partidas hasta dos horas antes del inicio, incluido ese límite. Por ejemplo, a las 16:00 se puede crear un partido para las 18:00; con menos de dos horas de antelación ya no se permite crearlo.

**Decisión del usuario sobre la cancelación automática:** si no se consiguen los cuatro jugadores confirmados dentro del plazo, la partida y su reserva se cancelan automáticamente y el hueco queda libre. El nuevo límite es de 90 minutos antes del inicio y sustituye al de tres horas del plan original (§§10, 35 y M9 de §38). Esta regla también se aplica a las partidas creadas con dos horas de antelación. Los jugadores cuentan como confirmados únicamente tras confirmar el pago, simulado en el MVP, según A1.

**Alcance aclarado por el usuario:** la gestión de la pista, incluidas cancelaciones automáticas, excepciones operativas, devoluciones y cambios de pista, pertenece a la capa de clubes todavía por definir. Las respuestas de dos horas y 90 minutos quedan recogidas como referencias acordadas para esa definición; no son invariantes globales ni implican definir o implementar ahora esa capa en M0.

El corte exacto del ejemplo de las 20:00 (90 minutos antes son las 18:30), sus excepciones y su relación con pagos y devoluciones se resolverán en la spec de clubes. No se seguirá afinando esa política dentro del proposal de Foundation.

**Decisión del usuario sobre el resultado:** al presentar el resultado comienza un nuevo plazo de 24 horas para su confirmación. Por ejemplo, si el partido termina el lunes a las 20:00 y el resultado se presenta el martes a las 18:00, el plazo de confirmación vence el miércoles a las 18:00.

Existen dos plazos distintos: se mantiene la regla de §26 que lleva a `UnresolvedResult` si nadie presenta resultado en las 24 horas posteriores a `EndsAt`; si se presenta dentro de ese plazo, comienza el de 24 horas para alcanzar el acuerdo desde su presentación. Si vence sin acuerdo, pasa a `UnresolvedResult`.

**Decisión del usuario sobre las correcciones:** corregir un resultado pendiente reinicia el plazo de confirmación, concediendo 24 horas desde la corrección. Por ejemplo, un resultado presentado el martes a las 18:00 y corregido el miércoles a las 10:00 pasa a vencer el jueves a las 10:00. El vencimiento se calcula desde la última corrección del resultado pendiente.

Las decisiones temporales de esta revisión quedan resueltas. Afectan a los futuros modelos, contratos y procesos automáticos de M5, M8, M9 y M10, sin añadir esos flujos a M0. La política completa de clubes y el tratamiento de confirmaciones tardías o repetidas del proveedor de pagos se definirán en sus specs correspondientes.

## Decisiones técnicas reservadas para el diseño

Tras la revisión final de este proposal y la constitución por el usuario, el diseño concretará las versiones del stack, las dependencias entre proyectos, la configuración local de PostgreSQL, el punto de partida de EF Core y migraciones, los tipos de fecha acordes con la política temporal, la exposición de OpenAPI y la comprobación mínima de la aplicación móvil. Las políticas de clubes expresamente diferidas se definirán en su propia spec, fuera de M0.

Este documento define intención, alcance y resultados verificables. La siguiente etapa queda pendiente de la revisión del usuario; no se incluyen todavía diseño técnico, checklist de implementación ni código.
