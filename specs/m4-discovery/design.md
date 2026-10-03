# Diseño — M4 Discovery

## Objetivo y decisiones

Implementar el alcance del [proposal](proposal.md): un endpoint de feed que devuelve los partidos `Open` y no expirados cercanos al jugador, agrupados en "para ti" (`Friendly` + `Competitive` compatible con su `Level`) y "fuera de rango" (`Competitive` incompatible o sin `Level`), cada grupo ordenado por proximidad y luego por horario. Reutiliza el patrón de proximidad ya existente en `ClubDiscoveryService` (`lat`/`lng` del dispositivo, o `cityOrZone` del jugador como alternativa) en vez de construir uno nuevo.

## `IMatchRepository`: nueva consulta y datos de club

`MatchWithSlotDetails` (ya usado por `GetMatchDetails`) se extiende con las coordenadas del club, necesarias para ordenar por distancia:

```csharp
public sealed record MatchWithSlotDetails(
    Match Match, Guid ClubId, string ClubName, double? ClubLatitude, double? ClubLongitude, string? ClubCityOrZone,
    Guid CourtId, string CourtName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
```

(`ClubCityOrZone` también se añade al registro, junto a las coordenadas: la rama de ordenación por `cityOrZone` la necesita igual que la rama por coordenadas necesita `ClubLatitude`/`ClubLongitude`.)

Nuevo método en `IMatchRepository`:

```csharp
Task<IReadOnlyList<MatchWithSlotDetails>> FindOpenUpcomingAsync(DateTimeOffset now, CancellationToken cancellationToken);
```

`MatchRepository` lo implementa con el mismo join (`Matches`→`CourtSlots`→`Courts`→`Clubs`) que ya usa `FindDetailsByIdAsync`, filtrando `match.Status == MatchStatus.Open && slot.StartsAt > now`. `FindDetailsByIdAsync` se actualiza para poblar también `ClubLatitude`/`ClubLongitude` en el `Select`, aunque `GetMatchDetails` no los use — mismo registro para ambos casos de uso, sin duplicar el join.

## Compatibilidad de nivel: helper reutilizable

Nueva clase estática en `PadelMatch.Domain.Matches` (no en `Application`, porque es una regla del dominio sobre `Match`, no un detalle de infraestructura ni de caso de uso):

```csharp
public static class MatchCompatibility
{
    public static bool IsCompatibleWithLevel(Match match, decimal? playerLevel) =>
        match.Type == MatchType.Friendly
        || (playerLevel is { } level && match.MinLevel <= level && level <= match.MaxLevel);
}
```

Un `Friendly` siempre es compatible. Un `Competitive` exige `playerLevel` no nulo y dentro de `[MinLevel, MaxLevel]`. Se escribe ahora pensando en reutilizarse cuando M5 valide si un jugador puede unirse — mismo criterio, un solo sitio.

## `IMatchFeedService` (nuevo, `PadelMatch.Application.Matches`)

```csharp
public interface IMatchFeedService
{
    Task<MatchFeed> GetFeedAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken);
}

public sealed record MatchFeed(IReadOnlyList<MatchWithDistance> ForYou, IReadOnlyList<MatchWithDistance> OutOfRange);
public sealed record MatchWithDistance(MatchWithSlotDetails Details, double? DistanceKm);
```

`MatchFeedService(IMatchRepository, IPlayerRepository, TimeProvider)`:

1. `matches = matchRepository.FindOpenUpcomingAsync(clock.GetUtcNow(), ct)`.
2. `player = playerRepository.FindByIdAsync(playerId, ct)` → `playerLevel = player?.Level`.
3. Particiona `matches` con `MatchCompatibility.IsCompatibleWithLevel(m.Match, playerLevel)` en `forYou`/`outOfRange`.
4. Ordena cada partición con la misma estrategia de tres ramas que `ClubDiscoveryService.GetNearbyClubsAsync` (deliberadamente copiada, no extraída a un helper compartido — son ~15 líneas sobre formas de dato distintas, `Club` vs `MatchWithSlotDetails`; ver "Alternativas"):
   - Si hay `lat`/`lng`: por `HaversineDistanceCalculator.DistanceKm` ascendente (los partidos cuyo club no tiene coordenadas van al final, por `StartsAt`); después de la distancia, empate por `StartsAt` ascendente.
   - Si no, con `cityOrZone` (override o `player.CityOrZone`): primero los partidos cuyo club coincide en zona, luego el resto; dentro de cada bloque, por `StartsAt` ascendente.
   - Si no hay ninguno de los dos: todos por `StartsAt` ascendente.

## Endpoint

```text
GET /api/matches/feed?lat=&lng=&cityOrZone=   [autenticado]
```

Mismos parámetros opcionales que `GET /api/clubs/nearby`, mismo significado. Respuesta:

```csharp
public sealed record MatchFeedResponse(IReadOnlyList<MatchFeedItemResponse> ForYou, IReadOnlyList<MatchFeedItemResponse> OutOfRange);

public sealed record MatchFeedItemResponse(
    Guid Id, Guid ClubId, string ClubName, string CourtName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes,
    MatchType Type, decimal? MinLevel, decimal? MaxLevel, double? DistanceKm)
{
    public static MatchFeedItemResponse From(MatchWithDistance item) => new(
        item.Details.Match.Id, item.Details.ClubId, item.Details.ClubName, item.Details.CourtName,
        item.Details.StartsAt, item.Details.EndsAt, (int)(item.Details.EndsAt - item.Details.StartsAt).TotalMinutes,
        item.Details.Match.Type, item.Details.Match.MinLevel, item.Details.Match.MaxLevel, item.DistanceKm);
}
```

Deliberadamente sin `OrganizerId`, `OrganizerLevelAtCreation`, `MinMatchesRequired` ni `Note` — el plan (§14) pide "información mínima" en la card; el detalle completo sigue siendo `GET /api/matches/{id}` (ya existente, sin cambios), al que se navega al tocar una card. Sin calidad estimada ni contador de confirmados (proposal.md, "sin calidad estimada ni contador de confirmados").

## Alternativas y límites

- **No se extrae un helper de ordenación por proximidad compartido entre `ClubDiscoveryService` y `MatchFeedService`.** Ambos ordenan por las mismas tres ramas (coordenadas → `cityOrZone` → nombre), pero sobre formas de dato distintas (`Club` vs `MatchWithSlotDetails`, que además necesita una clave secundaria `StartsAt` que `Club` no tiene). Extraer una abstracción genérica ahora, para dos usos con formas distintas, es la premura que la constitución evita; se reconsidera si aparece un tercer uso real.
- **Sin paginación**: el feed devuelve todos los partidos que superan el filtro de vigencia. El volumen esperado en el MVP (pocos clubes, pocos partidos simultáneos) no la justifica todavía; se añade si el catálogo crece.
- **Sin caché ni invalidación**: cada llamada recalcula el feed contra la base en el momento. Consistente con "las reglas críticas deben protegerse en backend" (constitución) y con que M3 ya puede cambiar el estado de un hueco en cualquier momento.
- **El feed no distingue "mis propios partidos" de los de otros organizadores**: un jugador puede ver en el feed un partido que él mismo organizó (aparecerá igual que cualquier otro). No es un error — "mis partidos" es un listado personal fuera de alcance de esta spec (proposal.md).
