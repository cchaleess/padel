# Tareas — M6 Quality Rules

## Dominio

- [x] `AccessShortfall` + `MatchCompatibility.GetShortfalls`/`CanJoinDirectly` (sustituye `IsCompatibleWithLevel`) y tests.
- [x] `MatchAccessRequest` (`Pending`/`Approved`/`Rejected`) y `MatchAccessVote`, con tests de transiciones.

## Infraestructura

- [x] Configuraciones EF, índices únicos y migración `AddMatchAccessRequests`.
- [x] `IMatchAccessRepository` / `MatchAccessRepository` (incluido `FOR UPDATE`).

## Aplicación y API

- [x] `HoldSeatAsync` exige solicitud aprobada si faltan criterios (403 con `shortfalls`).
- [x] `MatchAccessService`: crear solicitud y votar (transacción con bloqueo).
- [x] Endpoints `exception-requests` (crear, aprobar, rechazar).
- [x] Detalle con `myAccess` y `pendingRequests`.
- [x] `GET /api/activity`.
- [x] Feed clasificado por `CanJoinDirectly` (+ aprobadas).

## Verificación

- [x] Tests de integración `MatchAccessTests` (criterios 1–12, incluida la concurrencia).
- [x] `dotnet test` completo en verde (51 + 80). El test de concurrencia falla 10/10 sin el `FOR UPDATE` y pasa con él.
- [x] README (sección M6).
