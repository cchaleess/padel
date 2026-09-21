import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import * as Location from 'expo-location';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { ClubSummary } from '../../api/types';
import { colors, typography } from '../../theme';
import type { ClubsStackParamList } from './ClubsStackNavigator';

type Props = NativeStackScreenProps<ClubsStackParamList, 'ClubsList'>;

export default function ClubsListScreen({ navigation }: Props) {
  const [clubs, setClubs] = useState<ClubSummary[]>([]);
  const [query, setQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSeq = useRef(0);

  const loadNearby = useCallback(async (seq: number) => {
    try {
      const permission = await Location.requestForegroundPermissionsAsync();
      let nearby: ClubSummary[];
      if (permission.status === 'granted') {
        const position = await Location.getCurrentPositionAsync();
        nearby = await api.getNearbyClubs({
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        });
      } else {
        nearby = await api.getNearbyClubs();
      }
      if (seq !== requestSeq.current) {
        return;
      }
      setClubs(nearby);
      setError(null);
    } catch {
      try {
        const nearby = await api.getNearbyClubs();
        if (seq !== requestSeq.current) {
          return;
        }
        setClubs(nearby);
        setError(null);
      } catch {
        if (seq === requestSeq.current) {
          setError('No se han podido cargar los clubes.');
        }
      }
    } finally {
      if (seq === requestSeq.current) {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    }
  }, []);

  const search = useCallback(async (trimmedQuery: string, seq: number) => {
    try {
      const results = await api.searchClubs(trimmedQuery);
      if (seq !== requestSeq.current) {
        return;
      }
      setClubs(results);
      setError(null);
    } catch {
      if (seq === requestSeq.current) {
        setError('No se ha podido completar la búsqueda.');
      }
    } finally {
      if (seq === requestSeq.current) {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    }
  }, []);

  useEffect(() => {
    const seq = ++requestSeq.current;
    setIsRefreshing(true);
    const trimmedQuery = query.trim();
    if (trimmedQuery.length === 0) {
      void loadNearby(seq);
      return;
    }
    const timeout = setTimeout(() => void search(trimmedQuery, seq), 350);
    return () => clearTimeout(timeout);
  }, [query, loadNearby, search]);

  useEffect(() => {
    navigation.setOptions({
      headerRight: () => (
        <Pressable onPress={() => navigation.navigate('SubmitClub')}>
          <Text style={styles.headerAction}>Aportar</Text>
        </Pressable>
      ),
    });
  }, [navigation]);

  return (
    <View style={styles.screen}>
      <View style={styles.searchRow}>
        <TextInput
          style={styles.search}
          value={query}
          onChangeText={setQuery}
          placeholder="Buscar club por nombre"
          placeholderTextColor={colors.inkMuted}
          returnKeyType="search"
        />
        {isRefreshing && !isLoading ? (
          <ActivityIndicator style={styles.searchSpinner} color={colors.brandDark} />
        ) : null}
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {isLoading ? (
        <ActivityIndicator style={styles.loading} color={colors.brandDark} />
      ) : (
        <FlatList
          data={clubs}
          keyExtractor={(club) => club.id}
          contentContainerStyle={styles.list}
          renderItem={({ item }) => (
            <Pressable
              style={styles.row}
              onPress={() => navigation.navigate('ClubDetail', { clubId: item.id })}
            >
              <Text style={styles.rowTitle}>{item.name}</Text>
              <Text style={typography.note}>
                {item.cityOrZone ?? 'Sin ciudad/zona'}
                {item.distanceKm !== null ? ` · ${item.distanceKm.toFixed(1)} km` : ''}
              </Text>
            </Pressable>
          )}
          ListEmptyComponent={<Text style={typography.note}>No se han encontrado clubes.</Text>}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  searchRow: { position: 'relative', marginBottom: 16 },
  search: {
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    paddingRight: 40,
    color: colors.ink,
  },
  searchSpinner: { position: 'absolute', right: 12, top: 0, bottom: 0 },
  loading: { marginTop: 32 },
  error: { color: '#B3261E', fontSize: 14, lineHeight: 20, marginTop: 16, marginBottom: 8 },
  list: { paddingBottom: 32 },
  row: {
    borderBottomWidth: 1,
    borderBottomColor: '#E3E0D2',
    paddingVertical: 14,
  },
  rowTitle: { fontSize: 17, fontWeight: '700', color: colors.ink, marginBottom: 4 },
  headerAction: { color: colors.brandDark, fontWeight: '700', fontSize: 15, marginRight: 4 },
});
