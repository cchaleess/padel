import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ApiError, api } from '../../api/httpClient';
import type { AccessRequester } from '../../api/types';
import { colors, typography } from '../../theme';

type Props = {
  matchId: string;
  requester: AccessRequester;
  /** Extra lines under the requester, e.g. the unmet criteria or the match's club and time. */
  details: string[];
  /** e.g. "1/2 aprobaciones"; omitted where it isn't known (Actividad). */
  progress?: string;
  myVote: boolean | null;
  onVoted: () => void;
  onOpenMatch?: () => void;
};

/** One exception request for a confirmed player to vote on (m6-quality-rules): shared by the match detail and
 * the Actividad tab. */
export default function AccessRequestCard({ matchId, requester, details, progress, myVote, onVoted, onOpenMatch }: Props) {
  const [isVoting, setIsVoting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const vote = async (approve: boolean) => {
    setIsVoting(true);
    setError(null);
    try {
      await api.voteAccess(matchId, requester.playerId, approve);
      onVoted();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se ha podido registrar el voto.');
    } finally {
      setIsVoting(false);
    }
  };

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <Text style={styles.name}>{requester.displayName}</Text>
        <Text style={styles.level}>{requester.level != null ? requester.level.toFixed(1) : '—'}</Text>
      </View>
      {details.map((line) => (
        <Text key={line} style={typography.note}>
          {line}
        </Text>
      ))}
      {progress ? <Text style={styles.progress}>{progress}</Text> : null}

      {myVote === null ? (
        <View style={styles.actions}>
          <Pressable
            accessibilityRole="button"
            disabled={isVoting}
            style={[styles.button, styles.approve, isVoting && styles.disabled]}
            onPress={() => vote(true)}>
            <Text style={styles.approveLabel}>Aprobar</Text>
          </Pressable>
          <Pressable
            accessibilityRole="button"
            disabled={isVoting}
            style={[styles.button, styles.reject, isVoting && styles.disabled]}
            onPress={() => vote(false)}>
            <Text style={styles.rejectLabel}>Rechazar</Text>
          </Pressable>
        </View>
      ) : (
        <Text style={styles.voted}>{myVote ? 'Has aprobado' : 'Has rechazado'}</Text>
      )}
      {onOpenMatch ? (
        <Pressable accessibilityRole="link" onPress={onOpenMatch} style={styles.openMatch}>
          <Text style={styles.openMatchLabel}>Ver partido</Text>
        </Pressable>
      ) : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: { borderWidth: 1, borderColor: '#D8D5C4', borderRadius: 10, padding: 14, marginBottom: 10 },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  name: { color: colors.ink, fontSize: 16, fontWeight: '600' },
  level: { color: colors.brandDark, fontSize: 16, fontWeight: '700' },
  progress: { ...typography.note, fontSize: 13, marginTop: 2 },
  actions: { flexDirection: 'row', gap: 10, marginTop: 10 },
  button: { flex: 1, borderRadius: 10, paddingVertical: 10, alignItems: 'center' },
  approve: { backgroundColor: colors.brandDark },
  approveLabel: { color: colors.background, fontWeight: '700', fontSize: 15 },
  reject: { borderWidth: 1, borderColor: '#B3261E' },
  rejectLabel: { color: '#B3261E', fontWeight: '700', fontSize: 15 },
  disabled: { opacity: 0.5 },
  voted: { marginTop: 8, color: colors.brandDark, fontWeight: '600' },
  openMatch: { marginTop: 8 },
  openMatchLabel: { color: colors.brandDark, fontWeight: '600', textDecorationLine: 'underline' },
  error: { marginTop: 8, color: '#B3261E', fontSize: 14, lineHeight: 20 },
});
