import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, SectionList, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { Activity } from '../../api/types';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
import AccessRequestCard from '../matches/AccessRequestCard';
import { requestStatusLabels } from '../matches/accessLabels';
import { getMatchTypeLabel } from '../matches/matchTypeLabel';
import type { ActivityStackParamList } from './ActivityStackNavigator';
import { usePendingVotes } from './PendingVotesContext';

type Props = NativeStackScreenProps<ActivityStackParamList, 'Activity'>;

type Row =
  | { kind: 'toVote'; item: Activity['toVote'][number] }
  | { kind: 'mine'; item: Activity['myRequests'][number] };

/** Plan §4 "Actividad": exception requests waiting for my vote and my own requests (m6-mobile-quality-rules).
 * Waitlists join here in M7. */
export default function ActivityScreen({ navigation }: Props) {
  const [activity, setActivity] = useState<Activity | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSeq = useRef(0);
  const { report } = usePendingVotes();

  const load = useCallback(async () => {
    const seq = ++requestSeq.current;
    try {
      const loaded = await api.getActivity();
      if (seq === requestSeq.current) {
        setActivity(loaded);
        setError(null);
        report(loaded.toVote.length);
      }
    } catch {
      if (seq === requestSeq.current) {
        setError('No se ha podido cargar tu actividad.');
      }
    } finally {
      if (seq === requestSeq.current) {
        setIsRefreshing(false);
      }
    }
  }, [report]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  if (!activity && !error) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  const sections: { title: string; data: Row[] }[] = [];
  if (activity && activity.toVote.length > 0) {
    sections.push({
      title: 'Solicitudes que tienes que votar',
      data: activity.toVote.map((item) => ({ kind: 'toVote', item })),
    });
  }
  if (activity && activity.myRequests.length > 0) {
    sections.push({ title: 'Tus solicitudes', data: activity.myRequests.map((item) => ({ kind: 'mine', item })) });
  }

  const openMatch = (matchId: string) => navigation.navigate('MatchDetail', { matchId });

  return (
    <View style={styles.screen}>
      {error ? <Text style={styles.error}>{error}</Text> : null}
      <SectionList
        sections={sections}
        keyExtractor={(row) =>
          row.kind === 'toVote' ? `vote-${row.item.match.matchId}-${row.item.requester.playerId}` : `mine-${row.item.match.matchId}`
        }
        contentContainerStyle={styles.list}
        refreshControl={
          <RefreshControl
            refreshing={isRefreshing}
            onRefresh={() => {
              setIsRefreshing(true);
              void load();
            }}
          />
        }
        renderSectionHeader={({ section }) => <Text style={styles.sectionTitle}>{section.title}</Text>}
        renderItem={({ item: row }) => {
          const match = row.item.match;
          const matchLine = `${match.clubName} · ${formatSlotSchedule(match.startsAt, match.endsAt)}`;
          if (row.kind === 'toVote') {
            return (
              <AccessRequestCard
                matchId={match.matchId}
                requester={row.item.requester}
                details={[`Quiere unirse a: ${matchLine}`]}
                myVote={null}
                onVoted={() => void load()}
                onOpenMatch={() => openMatch(match.matchId)}
              />
            );
          }
          return (
            <Pressable style={styles.row} onPress={() => openMatch(match.matchId)}>
              <Text style={styles.rowTitle}>{match.clubName}</Text>
              <Text style={typography.note}>
                {formatSlotSchedule(match.startsAt, match.endsAt)} · {getMatchTypeLabel(match.type)}
              </Text>
              <Text style={[styles.status, (row.item.status === 'Rejected' || row.item.status === 'Expired') && styles.rejected]}>
                {requestStatusLabels[row.item.status]}
              </Text>
            </Pressable>
          );
        }}
        ListEmptyComponent={<Text style={typography.note}>No tienes solicitudes pendientes.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, marginBottom: 8 },
  list: { paddingBottom: 32 },
  sectionTitle: { ...typography.note, fontWeight: '700', marginTop: 20, marginBottom: 8 },
  row: { borderBottomWidth: 1, borderBottomColor: '#E3E0D2', paddingVertical: 14 },
  rowTitle: { fontSize: 16, fontWeight: '600', color: colors.ink, marginBottom: 2 },
  status: { marginTop: 2, color: colors.brandDark, fontWeight: '600' },
  rejected: { color: '#B3261E' },
});
