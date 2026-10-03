import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, SectionList, StyleSheet, Text, View } from 'react-native';
import * as Location from 'expo-location';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../../api/httpClient';
import type { MatchFeedItem } from '../../api/types';
import { colors, typography } from '../../theme';
import { formatSlotSchedule } from '../clubs/slotFormatting';
import { getMatchTypeLabel } from './matchTypeLabel';
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
      if (feed.forYou.length > 0) {
        nextSections.push({ title: 'Partidos para ti', data: feed.forYou });
      }
      if (feed.outOfRange.length > 0) {
        nextSections.push({ title: 'Otros partidos cercanos', data: feed.outOfRange });
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
          <Pressable style={styles.row} onPress={() => navigation.navigate('MatchDetail', { matchId: item.id })}>
            <Text style={styles.rowTitle}>{item.clubName}</Text>
            <Text style={typography.note}>{formatSlotSchedule(item.startsAt, item.endsAt)}</Text>
            <Text style={typography.note}>
              {getMatchTypeLabel(item.type)}
              {item.type === 'Competitive' ? ` · ${item.minLevel}–${item.maxLevel}` : ''}
            </Text>
          </Pressable>
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
  row: {
    borderBottomWidth: 1,
    borderBottomColor: '#E3E0D2',
    paddingVertical: 14,
  },
  rowTitle: { fontSize: 16, fontWeight: '600', color: colors.ink, marginBottom: 2 },
});
