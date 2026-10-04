import { Pressable, StyleSheet, Text } from 'react-native';
import type { MatchFeedItem } from '../../api/types';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
import { getMatchTypeLabel } from './matchTypeLabel';

/** One match as a list row: shared by the feed and the profile's "Próximos". */
export default function MatchSummaryRow({ match, onPress }: { match: MatchFeedItem; onPress: () => void }) {
  return (
    <Pressable style={styles.row} onPress={onPress}>
      <Text style={styles.rowTitle}>{match.clubName}</Text>
      <Text style={typography.note}>{formatSlotSchedule(match.startsAt, match.endsAt)}</Text>
      <Text style={typography.note}>
        {getMatchTypeLabel(match.type)}
        {match.type === 'Competitive' ? ` · ${match.minLevel}–${match.maxLevel}` : ''}
        {` · ${match.confirmedSeats}/4`}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    borderBottomWidth: 1,
    borderBottomColor: '#E3E0D2',
    paddingVertical: 14,
  },
  rowTitle: { fontSize: 16, fontWeight: '600', color: colors.ink, marginBottom: 2 },
});
