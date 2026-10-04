import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, RefreshControl, SectionList, StyleSheet, Text, View } from 'react-native';
import * as Location from 'expo-location';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { MatchFeedItem } from '../../api/types';
import { colors, typography } from '../../theme';
import MatchSummaryRow from './MatchSummaryRow';
import type { PartidosStackParamList } from './PartidosStackNavigator';

type Props = NativeStackScreenProps<PartidosStackParamList, 'Feed'>;

interface Section {
  title: string;
  data: MatchFeedItem[];
}

export default function FeedScreen({ navigation }: Props) {
  const [sections, setSections] = useState<Section[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSeq = useRef(0);

  const loadFeed = useCallback(async () => {
    const seq = ++requestSeq.current;
    try {
      const permission = await Location.requestForegroundPermissionsAsync();
      let feed;
      if (permission.status === 'granted') {
        const position = await Location.getCurrentPositionAsync();
        feed = await api.getMatchFeed({ lat: position.coords.latitude, lng: position.coords.longitude });
      } else {
        feed = await api.getMatchFeed();
      }
      if (seq !== requestSeq.current) {
        return;
      }
      const nextSections: Section[] = [];
      if (feed.confirmed.length > 0) {
        nextSections.push({ title: 'Tienes estas partidas confirmadas', data: feed.confirmed });
      }
      if (feed.pendingConfirmation.length > 0) {
        nextSections.push({ title: 'Partidas pendientes de confirmación', data: feed.pendingConfirmation });
      }
      if (feed.forYou.length > 0) {
        nextSections.push({ title: 'Partidos para ti', data: feed.forYou });
      }
      if (feed.outOfRange.length > 0) {
        // Plan §13: the ones the player can't join directly need the confirmed players' approval.
        nextSections.push({ title: 'Requieren aprobación', data: feed.outOfRange });
      }
      setSections(nextSections);
      setError(null);
    } catch {
      if (seq === requestSeq.current) {
        setError('No se ha podido cargar el feed de partidos.');
      }
    } finally {
      if (seq === requestSeq.current) {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      setIsLoading(true);
      void loadFeed();
    }, [loadFeed]),
  );

  const onRefresh = useCallback(() => {
    setIsRefreshing(true);
    void loadFeed();
  }, [loadFeed]);

  if (isLoading) {
    return <ActivityIndicator style={styles.loading} color={colors.brandDark} />;
  }

  return (
    <View style={styles.screen}>
      {error ? <Text style={styles.error}>{error}</Text> : null}
      <SectionList
        sections={sections}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={isRefreshing} onRefresh={onRefresh} />}
        renderSectionHeader={({ section }) => <Text style={styles.sectionTitle}>{section.title}</Text>}
        renderItem={({ item }) => (
          <MatchSummaryRow match={item} onPress={() => navigation.navigate('MatchDetail', { matchId: item.id })} />
        )}
        ListEmptyComponent={<Text style={typography.note}>No hay partidos disponibles por ahora.</Text>}
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
});
