import { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { api } from '../../api/httpClient';
import type { MatchDetail } from '../../api/types';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
import { getMatchTypeLabel } from './matchTypeLabel';

type Props = { route: { params: { matchId: string } } };

export default function MatchDetailScreen({ route }: Props) {
  const { matchId } = route.params;
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        setMatch(await api.getMatchDetails(matchId));
      } catch {
        setError('No se ha podido cargar el partido.');
      } finally {
        setIsLoading(false);
      }
    })();
  }, [matchId]);

  if (isLoading) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  if (error || !match) {
    return <Text style={styles.error}>{error ?? 'Partido no encontrado.'}</Text>;
  }

  return (
    <View style={styles.screen}>
      <Text style={typography.eyebrow}>{getMatchTypeLabel(match.type)}</Text>
      <Text accessibilityRole="header" style={typography.title}>
        {match.clubName}
      </Text>
      <Text style={[typography.note, styles.detail]}>{match.courtName}</Text>
      <Text style={[typography.note, styles.detail]}>{formatSlotSchedule(match.startsAt, match.endsAt)}</Text>
      <Text style={typography.note}>{match.durationMinutes} min</Text>

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
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, margin: 20 },
  detail: { marginTop: 4 },
  section: { marginTop: 20 },
  sectionTitle: { ...typography.note, fontWeight: '700', marginBottom: 6 },
});
