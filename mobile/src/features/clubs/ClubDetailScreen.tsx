import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { ClubDetail } from '../../api/types';
import { colors, typography } from '../../theme';
import type { ClubsStackParamList } from './ClubsStackNavigator';

type Props = NativeStackScreenProps<ClubsStackParamList, 'ClubDetail'>;

export default function ClubDetailScreen({ route, navigation }: Props) {
  const { clubId } = route.params;
  const [club, setClub] = useState<ClubDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        setClub(await api.getClubDetails(clubId));
      } catch {
        setError('No se ha podido cargar el club.');
      } finally {
        setIsLoading(false);
      }
    })();
  }, [clubId]);

  if (isLoading) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  if (error || !club) {
    return <Text style={styles.error}>{error ?? 'Club no encontrado.'}</Text>;
  }

  return (
    <View style={styles.screen}>
      <Text accessibilityRole="header" style={typography.title}>
        {club.name}
      </Text>
      {club.status === 'UserSubmitted' ? <Text style={styles.badge}>No verificado</Text> : null}
      <Text style={[typography.note, styles.address]}>{club.address}</Text>

      <Text style={styles.sectionTitle}>Pistas</Text>
      <FlatList
        data={club.courts}
        keyExtractor={(court) => court.id}
        renderItem={({ item }) => (
          <Pressable
            style={styles.row}
            onPress={() =>
              navigation.navigate('CourtSlots', { clubId: club.id, courtId: item.id, courtName: item.name })
            }
          >
            <Text style={styles.rowTitle}>{item.name}</Text>
          </Pressable>
        )}
        ListEmptyComponent={<Text style={typography.note}>Este club todavía no tiene pistas.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, margin: 20 },
  badge: {
    marginTop: 8,
    alignSelf: 'flex-start',
    color: colors.brandDark,
    fontWeight: '700',
    fontSize: 12,
    textTransform: 'uppercase',
  },
  address: { marginTop: 12, marginBottom: 8 },
  sectionTitle: { ...typography.note, fontWeight: '700', marginTop: 20, marginBottom: 8 },
  row: {
    borderBottomWidth: 1,
    borderBottomColor: '#E3E0D2',
    paddingVertical: 14,
  },
  rowTitle: { fontSize: 16, fontWeight: '600', color: colors.ink },
});
