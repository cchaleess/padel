import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import ComingSoonScreen from '../components/ui/ComingSoonScreen';
import ClubsStackNavigator from '../features/clubs/ClubsStackNavigator';
import { colors } from '../theme';

type Segment = 'partidos' | 'clubes';

/// <summary>Two segments, not a navigation library: see design.md "Alternativas y límites".
/// Defaults to "clubes" because "partidos" has no real content until M4.</summary>
export default function PartidosTabScreen() {
  const [segment, setSegment] = useState<Segment>('clubes');

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
        {segment === 'partidos' ? <ComingSoonScreen title="Partidos" /> : <ClubsStackNavigator />}
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
