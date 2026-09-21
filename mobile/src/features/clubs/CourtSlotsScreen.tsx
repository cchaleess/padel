import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { CourtSlot } from '../../api/types';
import { colors, typography } from '../../theme';
import type { ClubsStackParamList } from './ClubsStackNavigator';

type Props = NativeStackScreenProps<ClubsStackParamList, 'CourtSlots'>;

const dateFormatter = new Intl.DateTimeFormat('es-ES', { weekday: 'short', day: 'numeric', month: 'short' });
const timeFormatter = new Intl.DateTimeFormat('es-ES', { hour: '2-digit', minute: '2-digit' });

function formatSlot(slot: CourtSlot): string {
  const startsAt = new Date(slot.startsAt);
  const endsAt = new Date(slot.endsAt);
  return `${dateFormatter.format(startsAt)} · ${timeFormatter.format(startsAt)}–${timeFormatter.format(endsAt)}`;
}

export default function CourtSlotsScreen({ route, navigation }: Props) {
  const { clubId, courtId, courtName } = route.params;
  const [slots, setSlots] = useState<CourtSlot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    navigation.setOptions({ title: courtName });
  }, [navigation, courtName]);

  useEffect(() => {
    (async () => {
      try {
        setSlots(await api.getClubSlots(clubId, courtId));
      } catch {
        setError('No se han podido cargar los huecos disponibles.');
      } finally {
        setIsLoading(false);
      }
    })();
  }, [clubId, courtId]);

  if (isLoading) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  if (error) {
    return <Text style={styles.error}>{error}</Text>;
  }

  return (
    <View style={styles.screen}>
      <FlatList
        data={slots}
        keyExtractor={(slot) => slot.id}
        contentContainerStyle={styles.list}
        renderItem={({ item }) => (
          <View style={styles.row}>
            <Text style={styles.rowTitle}>{formatSlot(item)}</Text>
            <Text style={typography.note}>{item.durationMinutes} min</Text>
          </View>
        )}
        ListEmptyComponent={<Text style={typography.note}>No hay huecos disponibles próximamente.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, margin: 20 },
  list: { paddingBottom: 32 },
  row: {
    borderBottomWidth: 1,
    borderBottomColor: '#E3E0D2',
    paddingVertical: 14,
  },
  rowTitle: { fontSize: 16, fontWeight: '600', color: colors.ink, marginBottom: 2 },
});
