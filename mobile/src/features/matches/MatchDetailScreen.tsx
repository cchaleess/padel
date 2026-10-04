import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ApiError, api } from '../../api/httpClient';
import type { MatchDetail } from '../../api/types';
import { useAuth } from '../auth/AuthContext';
import { usePendingVotes } from '../activity/PendingVotesContext';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
import AccessRequestCard from './AccessRequestCard';
import { describeShortfall } from './accessLabels';
import { getMatchTypeLabel } from './matchTypeLabel';

type Props = { route: { params: { matchId: string } } };

/** Padel is 2 vs 2: seat positions 0–1 are one pair, 2–3 the other (the organizer starts at 0). */
const PAIRS = [
  { label: 'Pareja A', positions: [0, 1] },
  { label: 'Pareja B', positions: [2, 3] },
];

/** The routes this screen navigates between; both ClubsStack and PartidosStack register them with these names. */
type SeatFlowParamList = {
  MatchDetail: { matchId: string };
  SeatPayment: { matchId: string; heldUntilUtc: string };
};

export default function MatchDetailScreen({ route }: Props) {
  const { matchId } = route.params;
  const navigation = useNavigation<NativeStackNavigationProp<SeatFlowParamList>>();
  const { player } = useAuth();
  const { refresh: refreshPendingVotes } = usePendingVotes();
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [joiningPosition, setJoiningPosition] = useState<number | null>(null);
  const [joinError, setJoinError] = useState<string | null>(null);
  const [requestingPosition, setRequestingPosition] = useState<number | null>(null);
  const [accessError, setAccessError] = useState<string | null>(null);
  const requestSeq = useRef(0);

  // Only the latest load wins, so a reload after voting can't be overwritten by a slower earlier one.
  const load = useCallback(async () => {
    const seq = ++requestSeq.current;
    try {
      const loaded = await api.getMatchDetails(matchId);
      if (seq === requestSeq.current) {
        setMatch(loaded);
        setError(null);
      }
    } catch {
      if (seq === requestSeq.current) {
        setError('No se ha podido cargar el partido.');
      }
    } finally {
      if (seq === requestSeq.current) {
        setIsLoading(false);
      }
    }
  }, [matchId]);

  // Reloaded on every focus, so coming back from SeatPayment shows the seat's new state.
  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const requestAccess = async (position: number) => {
    setRequestingPosition(position);
    setAccessError(null);
    try {
      await api.requestAccess(matchId, position);
      await load();
    } catch (e) {
      setAccessError(e instanceof ApiError ? e.message : 'No se ha podido enviar la solicitud.');
    } finally {
      setRequestingPosition(null);
    }
  };

  const join = async (position: number) => {
    setJoiningPosition(position);
    setJoinError(null);
    try {
      const hold = await api.holdSeat(matchId, position);
      navigation.navigate('SeatPayment', { matchId, heldUntilUtc: hold.heldUntilUtc });
    } catch (e) {
      setJoinError(e instanceof ApiError ? e.message : 'No se ha podido reservar la plaza.');
    } finally {
      setJoiningPosition(null);
    }
  };

  if (isLoading) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  if (error || !match) {
    return <Text style={styles.error}>{error ?? 'Partido no encontrado.'}</Text>;
  }

  // Quality rules (m6-quality-rules): outside a competitive match's criteria, joining needs an approved request.
  const hasAccess = match.myAccess.canJoinDirectly || match.myAccess.requestStatus === 'Approved';
  const canJoin = match.status === 'Open' && match.mySeat === null && hasAccess;
  const range = { minLevel: match.minLevel, maxLevel: match.maxLevel, minMatchesRequired: match.minMatchesRequired };

  const renderAccess = () => {
    const status = match.myAccess.requestStatus;
    if (status === 'Expired' && match.mySeat === null) {
      return (
        <View style={[styles.section, styles.accessBox]}>
          <Text style={[styles.accessStatus, styles.rejected]}>
            El partido se completó antes de que se votara tu solicitud.
          </Text>
        </View>
      );
    }
    if (hasAccess || match.mySeat !== null || match.status !== 'Open') {
      return null;
    }

    // The request isn't a reservation: someone who meets the criteria may take that seat while the vote is open.
    const requestedSeatTaken =
      match.myAccess.requestedPosition != null &&
      match.confirmedPlayers.some((p) => p.position === match.myAccess.requestedPosition);
    return (
      <View style={[styles.section, styles.accessBox]}>
        <Text style={styles.sectionTitle}>Requiere aprobación</Text>
        {match.myAccess.shortfalls.map((s) => (
          <Text key={s} style={typography.note}>
            · {describeShortfall(s, range, 'me')}
            {s === 'LevelBelowRange' || s === 'LevelAboveRange' ? ` (tu nivel: ${player?.level?.toFixed(1)})` : ''}
          </Text>
        ))}
        {status === 'Pending' ? (
          <>
            <Text style={styles.accessStatus}>Solicitud enviada. Esperando a que voten los jugadores confirmados.</Text>
            {requestedSeatTaken ? (
              <Text style={[typography.note, styles.accessHint]}>
                La plaza que pediste ya está ocupada; si te aprueban podrás elegir otra libre.
              </Text>
            ) : null}
          </>
        ) : status === 'Rejected' ? (
          <Text style={[styles.accessStatus, styles.rejected]}>Tu solicitud fue rechazada.</Text>
        ) : (
          <Text style={[typography.note, styles.accessHint]}>
            Toca una plaza vacía para solicitar acceso: entrarás si lo aprueban todos los jugadores confirmados.
          </Text>
        )}
        {accessError ? <Text style={styles.joinError}>{accessError}</Text> : null}
      </View>
    );
  };

  const renderPendingRequests = () =>
    match.pendingRequests.length === 0 ? null : (
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Solicitudes de acceso</Text>
        {match.pendingRequests.map((r) => (
          <AccessRequestCard
            key={r.requester.playerId}
            matchId={matchId}
            requester={r.requester}
            details={[
              ...(r.requestedPosition != null ? [`Quiere jugar en la ${r.requestedPosition < 2 ? 'pareja A' : 'pareja B'}`] : []),
              ...r.shortfalls.map((s) => describeShortfall(s, range, 'them')),
            ]}
            progress={`${r.approvals}/${r.votersNeeded} aprobaciones`}
            myVote={r.myVote}
            onVoted={() => {
              void load();
              void refreshPendingVotes();
            }}
          />
        ))}
      </View>
    );

  const renderSeat = (position: number) => {
    const seatPlayer = match.confirmedPlayers.find((p) => p.position === position);
    if (seatPlayer) {
      const tags = [
        seatPlayer.playerId === player?.id ? 'Tú' : null,
        seatPlayer.playerId === match.organizerId ? 'Organizador' : null,
      ].filter(Boolean);
      return (
        <View key={position} style={styles.seat}>
          <Text style={styles.seatName} numberOfLines={1}>
            {seatPlayer.displayName}
          </Text>
          <Text style={styles.seatLevel}>{seatPlayer.level != null ? seatPlayer.level.toFixed(1) : '—'}</Text>
          {tags.length > 0 ? (
            <Text style={styles.seatTags} numberOfLines={1}>
              {tags.join(' · ')}
            </Text>
          ) : null}
        </View>
      );
    }

    if (match.mySeat?.status === 'Held' && match.mySeat.position === position) {
      return (
        <View key={position} style={[styles.seat, styles.seatMine]}>
          <Text style={styles.seatName} numberOfLines={1}>
            {player?.displayName ?? 'Tú'}
          </Text>
          <Text style={styles.seatLevel}>{player?.level != null ? player.level.toFixed(1) : '—'}</Text>
          <Text style={styles.seatTags} numberOfLines={1}>
            Tú · pendiente de pago
          </Text>
        </View>
      );
    }

    // Outside the criteria (m6-quality-rules), an empty seat is where the player asks for access, and the request
    // shows on that seat only. It isn't a reservation: once approved, they join whichever seat is free.
    const needsAccess = match.status === 'Open' && match.mySeat === null && !hasAccess;
    if (needsAccess && match.myAccess.requestStatus === null) {
      const isRequestingThis = requestingPosition === position;
      return (
        <Pressable
          key={position}
          accessibilityRole="button"
          disabled={requestingPosition !== null}
          style={[styles.seat, styles.seatFree, requestingPosition !== null && !isRequestingThis && styles.buttonDisabled]}
          onPress={() => requestAccess(position)}>
          <Text style={styles.seatJoin}>{isRequestingThis ? 'Enviando...' : 'Solicitar acceso'}</Text>
        </Pressable>
      );
    }
    if (needsAccess && match.myAccess.requestStatus === 'Pending' && match.myAccess.requestedPosition === position) {
      return (
        <View key={position} style={[styles.seat, styles.seatMine]}>
          <Text style={styles.seatName} numberOfLines={1}>
            {player?.displayName ?? 'Tú'}
          </Text>
          <Text style={styles.seatTags}>Solicitud enviada</Text>
        </View>
      );
    }

    // Seats Held by others also show as free (plan §11); trying one returns the server's "not available" message.
    if (!canJoin) {
      return (
        <View key={position} style={[styles.seat, styles.seatFree]}>
          <Text style={styles.seatFreeLabel}>Plaza libre</Text>
        </View>
      );
    }

    const isJoiningThis = joiningPosition === position;
    return (
      <Pressable
        key={position}
        accessibilityRole="button"
        accessibilityLabel={`Unirme en la plaza ${position + 1}`}
        disabled={joiningPosition !== null}
        style={[styles.seat, styles.seatFree, joiningPosition !== null && !isJoiningThis && styles.buttonDisabled]}
        onPress={() => join(position)}>
        <Text style={styles.seatFreeLabel}>Plaza libre</Text>
        <Text style={styles.seatJoin}>{isJoiningThis ? 'Reservando...' : 'Unirme'}</Text>
      </Pressable>
    );
  };

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <Text style={typography.eyebrow}>{getMatchTypeLabel(match.type)}</Text>
      <Text accessibilityRole="header" style={typography.title}>
        {match.clubName}
      </Text>
      <Text style={[typography.note, styles.detail]}>{match.courtName}</Text>
      <Text style={[typography.note, styles.detail]}>{formatSlotSchedule(match.startsAt, match.endsAt)}</Text>
      <Text style={typography.note}>{match.durationMinutes} min</Text>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Jugadores · {match.confirmedSeats}/4</Text>
        {/* Two columns, A | B; same height cards, so each pair's 1st and 2nd seats line up in a row. */}
        <View style={styles.pairs}>
          {PAIRS.map((pair) => (
            <View key={pair.label} style={styles.pair}>
              <Text style={styles.pairLabel}>{pair.label}</Text>
              {pair.positions.map((position) => renderSeat(position))}
            </View>
          ))}
        </View>
        {joinError ? <Text style={styles.joinError}>{joinError}</Text> : null}
      </View>

      <View style={styles.section}>
        {match.mySeat?.status === 'Confirmed' ? (
          <Text style={styles.statusText}>Tienes plaza confirmada</Text>
        ) : match.mySeat?.status === 'Held' && match.mySeat.heldUntilUtc ? (
          <Pressable
            accessibilityRole="button"
            style={styles.button}
            onPress={() =>
              navigation.navigate('SeatPayment', { matchId, heldUntilUtc: match.mySeat!.heldUntilUtc! })
            }>
            <Text style={styles.buttonLabel}>Continuar pago</Text>
          </Pressable>
        ) : match.status === 'Full' ? (
          <Text style={styles.statusText}>Partido completo</Text>
        ) : canJoin ? (
          <Text style={typography.note}>Elige una plaza libre para unirte a una de las dos parejas.</Text>
        ) : null}
      </View>

      {renderAccess()}
      {renderPendingRequests()}

      {match.type === 'Competitive' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Nivel</Text>
          <Text style={typography.note}>
            Rango {match.minLevel} – {match.maxLevel}
          </Text>
          {match.minMatchesRequired != null ? (
            <Text style={typography.note}>Mínimo {match.minMatchesRequired} partidos jugados</Text>
          ) : null}
        </View>
      ) : null}

      {match.note ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Nota</Text>
          <Text style={typography.note}>{match.note}</Text>
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 32 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, margin: 20 },
  detail: { marginTop: 4 },
  section: { marginTop: 20 },
  sectionTitle: { ...typography.note, fontWeight: '700', marginBottom: 6 },
  statusText: { fontSize: 16, fontWeight: '700', color: colors.ink },
  accessBox: { borderWidth: 1, borderColor: '#D8D5C4', borderRadius: 12, padding: 14 },
  accessHint: { marginTop: 8 },
  accessStatus: { marginTop: 8, color: colors.ink, fontSize: 15, fontWeight: '600' },
  rejected: { color: '#B3261E' },
  seat: {
    minHeight: 84,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    marginBottom: 10,
  },
  seatFree: { borderStyle: 'dashed' },
  seatMine: { borderColor: colors.accent, borderWidth: 2 },
  seatJoin: { marginTop: 4, color: colors.brandDark, fontSize: 15, fontWeight: '700' },
  pairs: { flexDirection: 'row', gap: 12, marginTop: 8 },
  pair: { flex: 1 },
  pairLabel: { ...typography.eyebrow, marginBottom: 6, textAlign: 'center' },
  seatFreeLabel: { color: colors.inkMuted, fontSize: 15 },
  seatName: { color: colors.ink, fontSize: 16, fontWeight: '600' },
  seatTags: { marginTop: 2, color: colors.inkMuted, fontSize: 12 },
  seatLevel: { marginTop: 2, color: colors.brandDark, fontSize: 15, fontWeight: '700' },
  button: {
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  buttonDisabled: { opacity: 0.5 },
  buttonLabel: { color: colors.background, fontSize: 16, fontWeight: '700' },
  joinError: { marginTop: 12, color: '#B3261E', fontSize: 14, lineHeight: 20 },
});
