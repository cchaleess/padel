import { useCallback, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ApiError, api } from '../../api/httpClient';
import type { MatchDetail } from '../../api/types';
import { useAuth } from '../auth/AuthContext';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
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
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [joiningPosition, setJoiningPosition] = useState<number | null>(null);
  const [joinError, setJoinError] = useState<string | null>(null);

  // Reloaded on every focus, so coming back from SeatPayment shows the seat's new state.
  useFocusEffect(
    useCallback(() => {
      let isActive = true;
      (async () => {
        try {
          const loaded = await api.getMatchDetails(matchId);
          if (isActive) {
            setMatch(loaded);
            setError(null);
          }
        } catch {
          if (isActive) {
            setError('No se ha podido cargar el partido.');
          }
        } finally {
          if (isActive) {
            setIsLoading(false);
          }
        }
      })();
      return () => {
        isActive = false;
      };
    }, [matchId]),
  );

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

  const canJoin = match.status === 'Open' && match.mySeat === null;

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
        ) : (
          <Text style={typography.note}>Elige una plaza libre para unirte a una de las dos parejas.</Text>
        )}
      </View>

      {match.type === 'Competitive' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Nivel</Text>
          <Text style={typography.note}>
            Rango {match.minLevel} – {match.maxLevel} (tu nivel al crear: {match.organizerLevelAtCreation})
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
