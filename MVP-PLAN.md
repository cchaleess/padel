# MVP Plan — Plataforma móvil de partidos de pádel

## 1. Visión del producto

Construir una aplicación móvil orientada a jugadores de pádel que permita descubrir partidos disponibles, unirse a ellos, coordinar la partida, registrar el resultado y conservar un histórico útil para mejorar el nivel, la calidad de los partidos y la experiencia social.

El producto debe pensarse desde el inicio para escalar a miles de jugadores y evolucionar posteriormente hacia una plataforma completa de clubes, disponibilidad, reservas y pagos.

Principio central:

> La calidad del partido depende en gran medida del nivel real de los jugadores, pero el nivel de la aplicación es una señal y no una verdad absoluta.

---

## 2. Alcance del MVP

### Incluido

- Aplicación móvil-first.
- Registro y perfil de jugador.
- Geolocalización opcional.
- Encuesta para obtener puntaje inicial.
- Puntaje decimal con precisión de 0.1.
- Partidos competitivos y amistosos.
- Descubrimiento de partidos disponibles.
- Clubes cercanos.
- Selección de un hueco disponible de club/pista.
- Creación de partido sobre un hueco concreto.
- Rango de nivel competitivo.
- Mínimo de partidos jugados como criterio competitivo.
- Solicitudes excepcionales fuera de criterios.
- Aprobación unánime por los jugadores confirmados.
- Lista de espera de máximo 2 jugadores.
- Coordinación de bolas.
- Disponibilidad post-partido.
- Chat textual por partido.
- Confirmación simulada de plaza.
- Histórico de partidos.
- Registro y confirmación de resultados.
- Evaluación obligatoria de nivel post-partido.
- Evaluación social opcional.
- Foto opcional por partido.
- Casos de lesión y no-show.
- Notificaciones push mínimas.

### Fuera del MVP o simplificado

- Pasarela de pago real con tarjeta.
- Panel completo de gestión para clubes.
- Políticas configurables por club.
- Ranking avanzado.
- Algoritmo definitivo de recalculo de nivel.
- Feed social.
- Likes, seguidores, stories.
- Chat global.
- Adjuntos en chat.
- Mapa interactivo de clubes.
- Clubes favoritos.
- Detección automática de clubes duplicados.
- Torneos, ligas y clases.
- App web para clubes.
- Estadísticas avanzadas.
- Cancelación manual por jugadores.

---

## 3. Plataforma y arquitectura

### Cliente

- React Native + Expo.
- Mobile-first.
- Android/iOS.

### Backend

- ASP.NET Core Web API.
- Entity Framework Core.
- PostgreSQL.
- OpenAPI/Swagger.
- Arquitectura modular / Clean Architecture ligera.

Estructura propuesta:

```text
PadelMatch/
├── backend/
│   ├── PadelMatch.Api
│   ├── PadelMatch.Application
│   ├── PadelMatch.Domain
│   └── PadelMatch.Infrastructure
│
└── mobile/
    └── React Native / Expo
```

### Evolución futura

```text
React Native
     ↓
ASP.NET Core API
     ↓
PostgreSQL
     ↑
Web de clubes / administración
```

---

## 4. Navegación principal

```text
[ Partidos ] [ Crear ] [ Actividad ] [ Perfil ]
```

### Partidos

- Partidos compatibles.
- Otros partidos cercanos.
- Clubes.

### Crear

- Crear partido desde un hueco disponible de club/pista.

### Actividad

- Solicitudes pendientes.
- Listas de espera.

### Perfil

- Nivel.
- Confianza del nivel.
- Partidos.
- Histórico.
- Evaluaciones sociales.
- Datos personales.

---

## 5. Registro y onboarding

Datos mínimos:

- Nombre.
- Email.
- Contraseña o login social.
- Fecha de nacimiento.
- Ciudad/zona.
- Foto opcional.

### Ubicación

- Ciudad/zona se guarda en el perfil.
- Geolocalización del dispositivo es opcional.
- Si el usuario acepta, se utiliza para ordenar partidos y clubes cercanos.
- Si la rechaza, se utiliza ciudad/zona como fallback.

### Puntaje inicial

La encuesta de nivel no es obligatoria durante el registro.

```text
Registro
↓
Home
↓
Usuario puede navegar sin puntaje
```

Cuando quiera crear o participar en un competitivo:

```text
Sin puntaje
↓
Encuesta de nivel
↓
Puntaje inicial
```

La encuesta exacta se definirá posteriormente.

---

## 6. Sistema de nivel

### Escala

```text
1.0 – 5.0 → jugador amateur
6.0 – 7.0 → federado / monitor / nivel especial
```

Granularidad:

```text
1.0
1.1
1.2
...
4.9
5.0
```

### Conceptos separados

```text
Level
= puntaje actual

MatchesPlayed
= experiencia acumulada

LevelConfidence
= fiabilidad del puntaje

MatchHistory
= evidencia disponible
```

Ejemplo:

```text
Jugador A
Nivel: 4.0
Partidos: 100
Confianza: Alta

Jugador B
Nivel: 5.0
Partidos: 2
Confianza: Baja
```

El algoritmo definitivo de nivel queda fuera del MVP, pero el modelo de datos debe recoger desde el principio la información necesaria.

---

## 7. Tipos de partido

```text
Competitive
Friendly
```

### Competitive

- Requiere puntaje para crear.
- Requiere puntaje para participar.
- Tiene rango de nivel.
- Puede exigir mínimo de partidos.
- Resultado puede afectar al puntaje.
- Evaluación de nivel post-partido obligatoria.

### Friendly

- No requiere puntaje.
- No restringe por nivel.
- No exige mínimo de partidos.
- Resultado informativo.
- No afecta al puntaje.
- Evaluación social post-partido opcional.

---

## 8. Clubes y geolocalización

### Descubrimiento

La Home prioriza partidos.

Dentro de `Partidos`:

```text
[ Partidos ] [ Clubes ]
```

Los clubes se muestran en lista, no en mapa.

Orden:

1. Clubes cercanos.
2. Clubes usados recientemente.
3. Clubes aportados por usuarios.

También existe búsqueda por nombre.

### Clubes aportados por usuarios

Pueden existir clubes:

```text
Official
UserSubmitted
```

Los `UserSubmitted`:

- se guardan;
- pueden reutilizarse;
- aparecen en búsquedas;
- se muestran como no verificados;
- no tienen detección de duplicados en el MVP.

### Detalle mínimo del club

- Nombre.
- Dirección.
- Estado: oficial / aportado por usuarios.
- Partidos abiertos asociados.

La capa completa de gestión del club queda fuera de esta etapa.

---

## 9. Disponibilidad de pista

Una partida solo puede crearse a partir de un hueco disponible asociado a:

```text
Club
Pista
Fecha
Hora de inicio
Duración
```

Cada hueco:

- pertenece a una pista concreta;
- puede asociarse a una sola partida;
- queda bloqueado al crear una partida;
- no puede ser utilizado simultáneamente por otra partida.

### Duraciones

Lista cerrada:

```text
60 min
90 min
120 min
```

Valor por defecto:

```text
90 min
```

`EndsAt` se calcula automáticamente:

```text
EndsAt = StartsAt + Duration
```

---

## 10. Reserva de pista

La reserva real se hará en el futuro mediante pago con tarjeta.

En el MVP:

- la confirmación de plaza se simula;
- la arquitectura debe mantener el mismo flujo que tendrá el pago real;
- no se implementa una pasarela real.

### Regla de liberación de pista

Si faltan 3 horas para el inicio y la reserva aún no está confirmada:

```text
Partida sin reserva efectiva
↓
faltan 3 horas
↓
hueco se libera
↓
partida se cierra
↓
jugadores reciben notificación
```

La partida cancelada:

- no aparece en el histórico de partidos jugados;
- queda registrada internamente;
- el hueco vuelve a estar disponible;
- una nueva partida se crea desde cero sobre ese hueco.

Esta regla será en el futuro configurable por los clubes.

---

## 11. Confirmación de plaza / pago simulado

Estados de plaza:

```text
Available
Held
Confirmed
```

### Available

Plaza libre.

### Held

- Estado interno.
- Se utiliza durante la confirmación/pago.
- Duración: 5 minutos.
- No se muestra visualmente como estado al resto.
- Impide que otro usuario confirme esa misma plaza.

Si otro usuario intenta ocuparla:

> Esta plaza no está disponible temporalmente.

### Confirmed

La plaza queda ocupada únicamente cuando la confirmación finaliza correctamente.

### Reglas de Held

```text
Usuario inicia confirmación
↓
Held durante 5 minutos
↓
confirma
↓
Confirmed
```

Si:

- pasan 5 minutos;
- el usuario abandona;
- ocurre un error;

la plaza se libera inmediatamente.

El usuario puede volver a intentarlo si continúa disponible.

En el futuro, la confirmación simulada se sustituirá por el pago real con tarjeta.

---

## 12. Creación de partido competitivo

El primer jugador que crea la partida es el organizador.

Su nivel actual se guarda como snapshot:

```text
OrganizerLevelAtCreation
```

Ejemplo:

```text
Pedro
Nivel: 4.0
```

La partida tiene nivel base `4.0`.

El organizador decide cuánto abre el rango hacia abajo y hacia arriba:

```text
MinLevel = 3.7
MaxLevel = 4.5
```

No necesita ser simétrico.

También define:

```text
MinMatchesRequired
Note
```

Todos estos datos quedan congelados al publicar.

```text
Draft
→ editable

Open
→ inmutable
```

No pueden modificarse después:

- rango;
- mínimo de partidos;
- nota.

---

## 13. Descubrimiento de partidos

La Home de un jugador registrado debe responder:

> ¿A qué partidos puedo unirme?

### Feed principal

Prioridad:

1. Competitivos compatibles.
2. Amistosos disponibles.
3. Competitivos fuera de rango que permiten solicitar acceso.

Los partidos completos no aparecen.

Se excluyen:

- Full.
- Finalizados.
- Cancelados internamente.
- Expirados.

Cuando un partido pasa de `3/4` a `4/4`:

```text
desaparece del feed público
```

Permanece visible para sus participantes.

### Competitivos fuera de rango

Se muestran en una sección separada:

```text
Partidos para ti
...

Otros partidos cercanos
...
Requiere aprobación
```

---

## 14. Card de partido

Información mínima:

- Club.
- Fecha/hora.
- Tipo.
- Rango de nivel si es competitivo.
- Jugadores confirmados `x/4`.
- Calidad estimada.
- Estado relevante de pista/reserva cuando corresponda.

No se muestra distancia en la card.

La ubicación se consulta desde el detalle/club.

Tampoco se muestran nombres de jugadores en la card.

---

## 15. Detalle del partido

Debe mostrar de forma visual:

- Club.
- Pista.
- Fecha/hora.
- Tipo.
- Rango de nivel.
- Calidad estimada.
- Jugadores confirmados.
- Nivel individual.
- Plazas disponibles.
- Estado de reserva.
- Tiempo restante antes de liberar la pista.
- Nota del organizador.
- Coordinación de bolas.
- Disponibilidad post-partido.

### Iconografía

Se utilizarán iconos de aplicación para señales rápidas.

Ejemplo conceptual:

```text
Carlos  3.8  [bolas] [post-partido]
Juan    3.9  [post-partido]
Pedro   3.7
```

El design system definitivo se decidirá posteriormente.

---

## 16. Calidad del partido

El nivel no debe bloquear de forma absoluta.

En competitivo:

```text
Nivel
↓
compatibilidad
↓
calidad estimada
```

Si el jugador cumple los criterios:

```text
entra directamente
```

Si no cumple:

- rango de nivel;
- mínimo de partidos;

puede solicitar acceso.

### Aprobación excepcional

Todas las decisiones que pueden afectar a la calidad deben ser aprobadas por unanimidad por los jugadores confirmados.

```text
Jugador fuera de criterios
↓
solicitud pendiente
↓
todos los jugadores confirmados votan
↓
¿todos aprueban?
├── Sí → entra
└── No → no entra
```

---

## 17. Lista de espera

Máximo:

```text
2 jugadores
```

Si el partido está `4/4`, puede existir lista de espera.

Si se libera una plaza:

- si hay jugadores en espera, el partido no vuelve al feed;
- el organizador elige quién entra;
- el elegido pasa directamente a `Confirmed`;
- recibe notificación;
- el otro permanece en espera.

Información mínima para el organizador:

```text
Nombre
Nivel
Partidos jugados
```

Si no hay lista de espera:

```text
4/4 → 3/4
↓
el partido vuelve al feed
```

---

## 18. Organizador

El organizador es el primer jugador que crea la partida.

Si abandona antes del inicio:

- el partido sigue activo;
- el rol pasa automáticamente al jugador confirmado que lleve más tiempo inscrito.

El organizador gestiona:

- lista de espera;
- decisiones operativas no relacionadas con condiciones ya congeladas.

Las excepciones relacionadas con calidad requieren unanimidad del grupo.

---

## 19. Coordinación previa

### Bolas

Separar:

```text
BallPreference
- NewBallsPreferred
- NoPreference

BallContribution
- BringingBalls
- NotBringingBalls
- Unknown
```

La UI puede resumir:

- quién lleva bolas;
- cuántos prefieren bolas nuevas.

### Disponibilidad post-partido

Opciones:

```text
Unknown
LeavingImmediately
AvailableForAWhile
Available
```

Se representa visualmente mediante iconografía de app.

---

## 20. Chat de partido

Cada partida crea automáticamente un chat privado.

### Participantes del MVP

- Jugadores confirmados.
- Mensajes automáticos del sistema.

Preparado para que en el futuro participe también el club.

### Características

- Solo texto.
- Sin imágenes.
- Sin archivos.
- Sin audio.
- Sin ubicación compartida.

### Mensajes del sistema

Ejemplos:

- jugador abandona;
- jugador entra desde lista de espera;
- partido completo;
- resultado pendiente.

### Vida útil

El chat existe para resolver incidencias previas y durante el partido.

```text
Match creado → chat activo
↓
Match.EndsAt → chat cerrado
```

No se conserva como parte visible del histórico.

---

## 21. Notificaciones mínimas

Solo eventos que requieren atención o acción:

- solicitud pendiente de aprobación;
- solicitud aprobada/rechazada;
- jugador abandona y libera plaza;
- entrada desde lista de espera;
- nuevo mensaje de chat;
- recordatorio antes del inicio;
- partido terminado y resultado disponible;
- resultado pendiente de confirmación;
- cancelación automática por falta de confirmación de reserva.

No generar push por eventos secundarios.

---

## 22. Estados del partido

```text
Draft
Open
Full
InProgress
AwaitingResult
ResultPendingConfirmation
Completed
UnresolvedResult
EndedByInjury
NoShow
```

La cancelación por políticas de reserva existe internamente, aunque no forma parte del histórico normal del jugador.

### Transiciones automáticas

```text
Open → Full
al llegar a 4/4

Open / Full → InProgress
al llegar StartsAt

InProgress → AwaitingResult
al llegar EndsAt

AwaitingResult → ResultPendingConfirmation
cuando se introduce resultado

ResultPendingConfirmation → Completed
cuando existe confirmación cruzada

ResultPendingConfirmation → UnresolvedResult
tras 24 h sin acuerdo
```

---

## 23. Salida de un jugador

### Antes del inicio

Puede abandonar.

Su plaza:

- pasa a lista de espera si existen candidatos;
- o vuelve al feed si no existen candidatos.

### Después del inicio

Un jugador confirmado no puede abandonar normalmente desde la app.

Solo se contemplan incidencias específicas:

- lesión;
- no-show.

---

## 24. Lesión

En un competitivo ya iniciado:

```text
Jugador declara lesión
↓
otros 3 jugadores deben validarla
↓
3/3 aprueban
↓
EndedByInjury
```

Consecuencias:

- partido finalizado;
- queda en histórico;
- no computa victoria/derrota;
- no afecta al puntaje;
- no alimenta el algoritmo de nivel.

---

## 25. No-show

Si un jugador confirmado no se presenta:

```text
otros 3 jugadores reportan no-show
↓
3/3 confirman
↓
NoShow
```

Consecuencias:

- partido queda registrado;
- no afecta al puntaje;
- no computa como victoria/derrota normal;
- se registra como incidencia interna;
- no se muestra todavía como penalización pública.

---

## 26. Resultado competitivo

Solo puede introducirse cuando:

```text
CurrentTime >= EndsAt
```

y únicamente por un participante.

Cualquiera de los 4 puede introducirlo.

### Confirmación

El resultado requiere confirmación cruzada:

```text
2 vs 2
```

Si un jugador del equipo A lo introduce, debe confirmarlo al menos un jugador del equipo B.

```text
Equipo A introduce
↓
Equipo B confirma
↓
Completed
```

### Discrepancia

Los propios jugadores corrigen el resultado hasta coincidir.

Plazo:

```text
24 horas
```

Si no existe acuerdo:

```text
UnresolvedResult
```

Consecuencias:

- queda en histórico;
- no afecta al puntaje;
- no tiene resultado competitivo válido;
- no activa la encuesta post-partido.

### Sin resultado

Si pasan 24 h desde el final y nadie introduce resultado:

```text
UnresolvedResult
```

Sin impacto en puntaje.

---

## 27. Resultado amistoso

- Es informativo.
- No afecta al puntaje.
- No requiere alimentar el algoritmo competitivo.
- Puede conservarse en histórico.
- Puede habilitar encuesta social opcional.

---

## 28. Encuesta post-partido

### Competitivo

Orden:

```text
Resultado confirmado
↓
Evaluación obligatoria de nivel
↓
Evaluación social opcional
↓
cierre completo
```

Cada jugador evalúa a los otros 3.

### Evaluación de nivel

Obligatoria y anónima.

Escala:

```text
-2 Mucho más bajo
-1 Algo más bajo
 0 Correcto
+1 Algo más alto
+2 Mucho más alto
```

Esta señal queda disponible para el futuro algoritmo de nivel.

### Evaluación social

Opcional y anónima.

Aspectos positivos:

- Buen compañero.
- Juego limpio.
- Buena energía.
- Repetiría partido.

Aspectos de mejora:

- Puntualidad.
- Comunicación en pista.
- Actitud competitiva.
- Respeto por el ritmo de juego.

Sin comentarios libres.

El jugador evaluado puede ver resultados agregados en su perfil, pero nunca quién realizó cada valoración.

---

## 29. Histórico

El histórico se conserva desde el MVP porque alimentará el sistema de nivel y futuras estadísticas.

Cada partido histórico muestra como mínimo:

- fecha;
- club;
- tipo;
- compañero;
- rivales;
- resultado;
- nivel del jugador en ese momento;
- foto opcional.

Se debe guardar un snapshot del nivel:

```text
LevelAtMatch
```

El nivel actual no debe alterar la representación histórica.

### Histórico compartido

Al abrir el perfil de otro jugador relacionado:

```text
Has jugado 7 veces con Juan
3 como compañero
4 como rival
```

---

## 30. Partidos dentro del perfil

Sección:

```text
Próximos
Pendientes de cerrar
Finalizados
```

### Próximos

Solo partidos con plaza confirmada.

### Pendientes de cerrar

Partidos terminados con:

- resultado pendiente;
- confirmación pendiente;
- encuesta pendiente.

### Finalizados

Partidos completamente cerrados e incorporados al histórico.

Solicitudes y listas de espera permanecen en `Actividad`, no en `Próximos`.

---

## 31. Perfil propio

Información mínima:

- Nivel actual.
- Confianza del nivel.
- Partidos competitivos.
- Partidos amistosos.
- Total jugados.
- Victorias.
- Derrotas.
- Valoraciones sociales agregadas.

Separar:

```text
Partidos competitivos
Partidos amistosos
Total
```

Los amistosos cuentan como experiencia general, pero no afectan al nivel competitivo.

---

## 32. Perfil de otros jugadores

Visible únicamente en contextos relacionados.

Ejemplos:

- comparte partido contigo;
- ha jugado contigo;
- aparece en lista de espera relacionada;
- aparece en un partido que puedes consultar.

Información mínima:

- nombre;
- nivel;
- partidos jugados;
- confianza del nivel;
- valoraciones sociales agregadas;
- histórico compartido.

No existe buscador global público de jugadores en el MVP.

---

## 33. Foto del partido

- Una única foto opcional por partido.
- Asociada al histórico.
- Sin galería.
- Sin múltiples imágenes.
- Sin foto específica por pareja en el MVP.

---

## 34. Modelo de dominio inicial

Entidades principales:

```text
Player
Club
Court
CourtSlot
Match
MatchParticipant
MatchSlot
WaitlistEntry
MatchResult
MatchResultConfirmation
PlayerLevelAssessment
PlayerSocialAssessment
MatchChat
ChatMessage
MatchPhoto
Notification
```

Conceptos/value objects/enums:

```text
MatchType
MatchStatus
ParticipationStatus
SlotStatus
ClubStatus
AfterMatchAvailability
BallPreference
BallContribution
LevelConfidence
```

---

## 35. Reglas críticas de concurrencia

La aplicación está pensada para miles de jugadores.

Debe garantizarse:

- una pista/hueco no puede generar dos partidos;
- una plaza `Held` no puede confirmarse simultáneamente por dos jugadores;
- la confirmación final debe ser atómica;
- máximo 4 jugadores confirmados;
- máximo 2 jugadores en espera;
- transiciones de estado idempotentes;
- liberación automática de `Held`;
- liberación automática del hueco de pista 3 h antes si no se confirma la reserva.

Estas reglas deben protegerse en backend/base de datos, no solo en UI.

---

## 36. Casos de uso principales

### Player

```text
RegisterPlayer
UpdateProfile
CompleteLevelSurvey
GetPlayerProfile
GetRelatedPlayerProfile
```

### Discovery

```text
DiscoverMatches
GetMatchDetails
GetNearbyClubs
SearchClubs
GetClubDetails
```

### Match

```text
CreateMatchFromCourtSlot
JoinMatch
RequestExceptionalJoin
ApproveExceptionalJoin
LeaveMatch
JoinWaitlist
SelectWaitlistedPlayer
ReportInjury
ConfirmInjury
ReportNoShow
ConfirmNoShow
```

### Reservation

```text
HoldMatchSlot
ConfirmMatchSlot
ReleaseMatchSlot
ExpireHeldSlot
ExpireUnconfirmedCourtReservation
```

### Result

```text
SubmitResult
ConfirmResult
CorrectResult
ExpireUnresolvedResult
```

### Post-match

```text
SubmitLevelAssessments
SubmitSocialAssessments
UploadMatchPhoto
```

### Chat

```text
SendMatchMessage
GetMatchMessages
CloseMatchChat
```

---

## 37. API inicial orientativa

```http
POST /api/auth/register

GET  /api/matches/discover
GET  /api/matches/{id}

POST /api/matches
POST /api/matches/{id}/join
POST /api/matches/{id}/leave

POST /api/matches/{id}/waitlist
POST /api/matches/{id}/waitlist/{playerId}/select

POST /api/matches/{id}/exception-requests/{playerId}/approve

POST /api/matches/{id}/slots/hold
POST /api/matches/{id}/slots/confirm

POST /api/matches/{id}/result
POST /api/matches/{id}/result/confirm

POST /api/matches/{id}/injury
POST /api/matches/{id}/injury/confirm

POST /api/matches/{id}/no-show
POST /api/matches/{id}/no-show/confirm

POST /api/matches/{id}/level-assessments
POST /api/matches/{id}/social-assessments

GET  /api/clubs/nearby
GET  /api/clubs/search
GET  /api/clubs/{id}
GET  /api/clubs/{id}/slots

GET  /api/players/me
GET  /api/players/me/matches
GET  /api/players/{id}

GET  /api/matches/{id}/chat
POST /api/matches/{id}/chat/messages
```

La API definitiva se refinará durante el diseño técnico.

---

## 38. Milestones propuestas

### M0 — Foundation

- solución .NET;
- proyectos;
- PostgreSQL;
- EF Core;
- migraciones;
- OpenAPI;
- estructura React Native/Expo.

### M1 — Players

- registro;
- perfil;
- ciudad/zona;
- nivel opcional;
- encuesta inicial.

### M2 — Clubs & Courts read-only

- clubes;
- pistas;
- huecos disponibles;
- búsqueda;
- geolocalización;
- clubes aportados por usuarios.

No incluye todavía panel de administración del club.

### M3 — Match creation

- crear desde `CourtSlot`;
- competitivo/amistoso;
- rango;
- mínimo de partidos;
- nota;
- duración;
- snapshots;
- estados.

### M4 — Discovery

- feed personalizado;
- filtros;
- competitivos compatibles;
- fuera de rango;
- amistosos;
- exclusión de Full.

### M5 — Confirmation flow

- `Available`;
- `Held`;
- `Confirmed`;
- hold de 5 minutos;
- confirmación simulada;
- concurrencia.

### M6 — Quality rules

- acceso directo;
- solicitud excepcional;
- unanimidad;
- mínimo de partidos.

### M7 — Waitlist

- máximo 2;
- selección por organizador;
- reincorporación al feed si corresponde.

### M8 — Match coordination

- bolas;
- disponibilidad post-partido;
- chat;
- mensajes del sistema;
- notificaciones mínimas.

### M9 — Reservation lifecycle

- bloqueo de `CourtSlot`;
- liberación automática a T-3h;
- cancelación interna;
- reutilización del hueco.

### M10 — Results

- resultado competitivo;
- confirmación cruzada;
- corrección;
- expiración 24 h;
- `UnresolvedResult`.

### M11 — Edge cases

- lesión;
- validación 3/3;
- no-show;
- validación 3/3.

### M12 — Post-match

- evaluación de nivel;
- evaluación social;
- histórico;
- foto opcional.

### M13 — Profile & history

- próximos;
- pendientes;
- finalizados;
- estadísticas mínimas;
- histórico compartido.

### M14 — Deploy / QA

- backend;
- base de datos;
- aplicación móvil;
- observabilidad mínima;
- pruebas de concurrencia;
- pruebas end-to-end.

---

## 39. Criterio de MVP terminado

Un usuario debe poder completar este recorrido:

```text
Registro
↓
entra sin nivel
↓
consulta partidos y clubes
↓
completa encuesta de nivel
↓
elige hueco disponible en club/pista
↓
crea competitivo
↓
define rango y mínimo de partidos
↓
otros jugadores descubren el partido
↓
confirman sus plazas
↓
se completa 4/4
↓
coordinan bolas/chat/post-partido
↓
se juega
↓
se registra resultado
↓
un jugador de cada equipo confirma
↓
cada jugador evalúa el nivel de los otros 3
↓
encuesta social opcional
↓
partido pasa al histórico
```

También deben funcionar:

- jugador fuera de rango;
- mínimo de partidos incumplido;
- aprobación unánime;
- lista de espera;
- liberación de plaza;
- hold concurrente;
- liberación de pista a T-3h;
- lesión;
- no-show;
- resultado no resuelto en 24 h.

---

## 40. Decisiones pendientes para fases posteriores

### RatingEngine

Definir algoritmo que combine:

- victorias/derrotas;
- nivel de rivales;
- nivel de compañero;
- cantidad de partidos;
- estabilidad histórica;
- evaluaciones de nivel recibidas;
- confianza del nivel;
- ponderación de evaluadores.

### Club Management

- gestión de pistas;
- disponibilidad;
- políticas;
- precios;
- cancelaciones;
- reglas por club;
- panel web;
- incidencias operativas.

### Payments

- pasarela con tarjeta;
- payment intents;
- webhooks;
- reembolsos;
- split payments;
- confirmación final de reserva.

### Design System

- identidad visual;
- tipografía;
- colores;
- iconografía;
- componentes;
- estados;
- accesibilidad.

### Social

- tarjetas compartibles;
- perfiles más ricos;
- grupos;
- feed;
- recomendaciones sociales.

---

## 41. Principios de producto

1. El feed debe mostrar oportunidades reales de jugar, no ruido.
2. La calidad del partido es una prioridad.
3. El nivel orienta, pero admite excepciones humanas.
4. Las excepciones de calidad requieren unanimidad.
5. El histórico nunca debe perderse.
6. El nivel debe evolucionar de forma estable y explicable.
7. La experiencia móvil debe ser rápida y visual.
8. Las notificaciones deben ser mínimas y accionables.
9. El chat es operativo, no una red social.
10. La arquitectura debe permitir incorporar clubes y pagos sin rehacer el núcleo.
11. Las reglas críticas deben protegerse en backend y base de datos.
12. El MVP debe ser simple en UI, pero ambicioso en el modelo de dominio.
