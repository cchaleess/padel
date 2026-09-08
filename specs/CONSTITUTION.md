# Constitución del proyecto

## Propósito y alcance

Esta constitución recoge los principios transversales de [MVP-PLAN.md](../MVP-PLAN.md), especialmente sus secciones 3, 35 y 41. Toda spec debe respetarlos. Los flujos, pantallas y reglas particulares se desarrollan en las specs de sus milestones; su presencia aquí no implica implementarlos en M0.

El producto permite descubrir, organizar y jugar partidos de pádel, y conservar evidencia útil para mejorar la calidad de los encuentros y la experiencia social. Debe poder evolucionar hacia una plataforma con clubes, disponibilidad, reservas y pagos, preparada para miles de jugadores (§§1, 3, 35).

## Stack y organización

- Cliente móvil: React Native con Expo, orientado a Android e iOS y con enfoque mobile-first.
- Backend: ASP.NET Core Web API.
- Persistencia: PostgreSQL mediante Entity Framework Core.
- Contrato y documentación de API: OpenAPI/Swagger.
- Arquitectura modular con Clean Architecture ligera.
- La organización de referencia del plan separa `backend/` y `mobile/`; el backend distingue los proyectos `PadelMatch.Api`, `PadelMatch.Application`, `PadelMatch.Domain` y `PadelMatch.Infrastructure`.
- Organización de `mobile/`: estructura por feature. `src/features/<dominio>/` agrupa las pantallas, componentes y hooks propios de cada dominio del plan (`auth`, `players`, `matches`, `clubs`, ...); `src/components/ui/` reúne primitivas compartidas entre features; `src/theme/` reúne los tokens de diseño compartidos. Decisión del usuario (2026-09-08): resuelve la asimetría de que la constitución ya fijaba arquitectura para el backend pero no para mobile, detectada al revisar M0. Se adopta antes de que M1 añada la primera pantalla real más allá del placeholder de M0.
- Identidad visual / design system: mínimo viable por ahora. Los tokens de color y tipografía ya usados en la pantalla de M0 (`mobile/App.tsx`) se extraen a `src/theme/`, sin inventar una identidad nueva. Escala tipográfica completa, iconografía, catálogo de estados de componentes y accesibilidad quedan fuera de esta decisión: se resuelven pantalla a pantalla hasta que el volumen de pantallas justifique un sistema formal (§40 del plan lo trata como fase posterior). Se evaluó adoptar la herramienta externa Impeccable como mecanismo formal; no se adopta por ahora.

Fuente: §3. El plan no fija versiones, convenciones detalladas de código ni herramientas de pruebas; deberán concretarse en el diseño correspondiente.

## Principios de arquitectura y evolución

- El núcleo debe admitir la incorporación de gestión de clubes y pagos sin rehacerse. La futura web de clubes/administración se integra con la arquitectura de API y persistencia compartida (§§3, 41).
- La gestión de pistas y reservas pertenece a la capa de clubes, todavía por definir: cancelaciones automáticas, excepciones operativas, devoluciones y cambios de pista. Sus políticas y plazos concretos se definirán en las specs de esa capa; no son invariantes universales del núcleo (aclaración del usuario durante la revisión). Esto no traslada al club las decisiones de calidad del partido que requieren unanimidad de los jugadores.
- La confirmación simulada del MVP debe mantener el flujo previsto para el pago real. La pasarela y la operativa real de pagos pertenecen a una fase posterior (§§10, 11, 40).
- La UI del MVP debe ser simple, con un modelo de dominio capaz de conservar la información necesaria para la evolución del producto (§§6, 34, 41).
- Las reglas críticas se protegen en backend y base de datos; las comprobaciones de UI no constituyen una garantía de integridad (§§35, 41).

## Integridad y concurrencia: no negociables

Estas garantías deben mantenerse también ante operaciones simultáneas y ejecuciones repetidas (§35, con las aclaraciones del usuario durante la revisión):

- Un hueco de pista no puede asignarse simultáneamente a dos partidos. Su reutilización solo procede tras la liberación prevista por el ciclo de reserva; una cancelación queda registrada internamente (§§9, 10, 35).
- Una plaza retenida (`Held`) no puede ser confirmada simultáneamente por dos jugadores.
- La confirmación final de plaza debe ser atómica.
- Ningún jugador, incluido el organizador, ocupa definitivamente una plaza hasta que se haya confirmado su pago. En el MVP esa confirmación es simulada; crear el partido no concede por sí solo una plaza confirmada (aclaración del usuario durante la revisión).
- Un partido admite como máximo cuatro jugadores confirmados y dos en lista de espera.
- Las transiciones de estado deben ser idempotentes.
- Las retenciones de plaza deben liberarse automáticamente al expirar. El plazo del MVP es de cinco minutos; abandono o error también liberan la retención (§11).
- Cuando una política del club determine una cancelación o liberación automática, el backend y la base de datos deben aplicarla preservando la exclusividad del hueco, la idempotencia y el registro interno. El plazo y las condiciones de esa política pertenecen a la capa de clubes (§§10, 35 y aclaración del usuario).

Las specs responsables concretarán los mecanismos y su verificación. Estos invariantes no prescriben todavía una estrategia de bloqueos, transacciones o ejecución de expiraciones. El plan incluye pruebas de concurrencia y de extremo a extremo en M14 (§38).

## Principios de producto

Los doce principios de la sección 41 rigen el conjunto del producto:

1. El feed debe mostrar oportunidades reales de jugar y evitar el ruido.
2. La calidad del partido es una prioridad.
3. El nivel orienta; es una señal, no una verdad absoluta, y admite excepciones humanas.
4. Las excepciones que afectan a la calidad requieren unanimidad de los jugadores confirmados.
5. El histórico nunca debe perderse.
6. El nivel debe evolucionar de forma estable y explicable.
7. La experiencia móvil debe ser rápida y visual.
8. Las notificaciones deben ser mínimas y accionables.
9. El chat es operativo, no una red social.
10. La arquitectura debe permitir incorporar clubes y pagos sin rehacer el núcleo.
11. Las reglas críticas deben protegerse en backend y base de datos.
12. El MVP debe ser simple en UI, pero ambicioso en el modelo de dominio.

## Política temporal

- El reloj del servidor es la autoridad para las transiciones y los vencimientos; la hora del dispositivo no determina si una operación está dentro de plazo.
- Los instantes se almacenan e intercambian en UTC. Los plazos se calculan como tiempo real transcurrido entre instantes, también durante cambios de horario.
- En el MVP, clubes y jugadores comparten una única zona horaria de España. Las horas de los partidos se introducen y muestran en esa zona común, convirtiéndolas a UTC para su tratamiento interno.
- El backend calcula y guarda los vencimientos y los comprueba al confirmar operaciones. La cuenta atrás móvil se basa en el tiempo comunicado por el servidor y se actualiza al reconectar.

Estas decisiones fueron aceptadas por el usuario durante la revisión. El identificador de la zona y los tipos concretos de API y persistencia se precisarán en el diseño.

## Datos, evidencia y privacidad

- Nivel actual, experiencia acumulada, confianza del nivel e histórico son conceptos separados. El algoritmo definitivo de nivel queda fuera del MVP, pero debe recogerse la evidencia necesaria desde el inicio (§6).
- La información histórica conserva el nivel de su momento mediante snapshots; un cambio del nivel actual no debe reescribir la representación del pasado (§29).
- Conservar el histórico no significa mostrar toda la información operativa: las cancelaciones de reserva se registran internamente y no cuentan como partidos jugados; el chat no forma parte visible del histórico (§§10, 20, 29).
- La geolocalización del dispositivo es opcional y la ciudad/zona del perfil sirve como alternativa (§5).
- Las evaluaciones son anónimas para el jugador evaluado; las valoraciones sociales se muestran agregadas. Los perfiles de otros jugadores solo son visibles en contextos relacionados, sin buscador público global en el MVP (§§28, 32).

## Límites de lo definido

La revisión del usuario ha aclarado la condición de pago para todos los jugadores y la política temporal común. El [proposal de M0](m0-foundation/proposal.md) conserva las demás respuestas, incluida la salida del único confirmado y las referencias de dos horas para crear y 90 minutos para cancelar por falta de jugadores. La definición completa de las políticas de pista y reserva corresponde a la futura capa de clubes; esos valores no se fijan aquí como invariantes del proyecto. Las aclaraciones A1–A3 quedan resueltas para esta revisión, con los detalles de clubes expresamente diferidos a su propia spec.

El algoritmo definitivo de nivel, la gestión completa de clubes, los pagos reales y el design system siguen pendientes para fases posteriores (§40).
