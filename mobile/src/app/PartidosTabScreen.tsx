import { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import ClubsStackNavigator from '../features/clubs/ClubsStackNavigator';
import PartidosStackNavigator from '../features/matches/PartidosStackNavigator';
import { colors } from '../theme';

type Segment = 'partidos' | 'clubes';

/** Params another tab can pass to land on a given segment. `requestedAt` makes repeated requests distinct, so
 * the effect below runs even if the same segment is asked for twice. */
export type PartidosTabParams = { segment?: Segment; requestedAt?: number } | undefined;

/// <summary>Two segments, not a navigation library: see design.md "Alternativas y límites".</summary>
export default function PartidosTabScreen({ route }: { route: { params?: PartidosTabParams } }) {
  const [segment, setSegment] = useState<Segment>('partidos');
  const requestedSegment = route.params?.segment;
  const requestedAt = route.params?.requestedAt;

  useEffect(() => {
    if (requestedSegment) {
      setSegment(requestedSegment);
    }
  }, [requestedSegment, requestedAt]);

  return (
    <View style={styles.container}>
      <View style={styles.switcher}>
        <Pressable style={styles.switchButton} onPress={() => setSegment('partidos')}>
          <Text style={[styles.switchLabel, segment === 'partidos' && styles.switchLabelActive]}>Partidos</Text>
        </Pressable>
        <Pressable style={styles.switchButton} onPress={() => setSegment('clubes')}>
          <Text style={[styles.switchLabel, segment === 'clubes' && styles.switchLabelActive]}>Clubes</Text>
        </Pressable>
      </View>

      <View style={styles.content}>
        {segment === 'partidos' ? <PartidosStackNavigator /> : <ClubsStackNavigator />}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  switcher: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    paddingTop: 12,
    paddingBottom: 8,
    gap: 20,
  },
  switchButton: { paddingVertical: 6 },
  switchLabel: { fontSize: 15, fontWeight: '600', color: colors.inkMuted },
  switchLabelActive: { color: colors.brandDark, textDecorationLine: 'underline' },
  content: { flex: 1 },
});
