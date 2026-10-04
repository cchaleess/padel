import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import OptionGroup from '../../components/ui/OptionGroup';
import { api, ApiError } from '../../api/httpClient';
import type { MatchType } from '../../api/types';
import { colors, typography } from '../../theme';
import { useAuth } from '../auth/AuthContext';
import type { ClubsStackParamList } from '../clubs/ClubsStackNavigator';
import { formatSlotSchedule } from '../clubs/slotFormatting';

type Props = NativeStackScreenProps<ClubsStackParamList, 'CreateMatch'>;

function parseDecimal(value: string): number | undefined {
  const trimmed = value.trim().replace(',', '.');
  if (trimmed.length === 0) {
    return undefined;
  }
  const parsed = Number(trimmed);
  return Number.isNaN(parsed) ? undefined : parsed;
}

function parseInteger(value: string): number | undefined {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return undefined;
  }
  const parsed = Number.parseInt(trimmed, 10);
  return Number.isNaN(parsed) ? undefined : parsed;
}

export default function CreateMatchScreen({ route, navigation }: Props) {
  const { courtSlotId, courtName, startsAt, endsAt, durationMinutes } = route.params;
  const { player } = useAuth();
  const hasLevel = player?.level != null;

  const [type, setType] = useState<MatchType | null>(null);
  const [minLevel, setMinLevel] = useState('');
  const [maxLevel, setMaxLevel] = useState('');
  const [minMatchesRequired, setMinMatchesRequired] = useState('');
  const [note, setNote] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const parsedMinLevel = parseDecimal(minLevel);
  const parsedMaxLevel = parseDecimal(maxLevel);
  const canSubmit =
    type === 'Friendly' || (type === 'Competitive' && parsedMinLevel !== undefined && parsedMaxLevel !== undefined);

  const handleSubmit = async () => {
    if (type === null) {
      return;
    }
    setError(null);
    setIsSubmitting(true);
    try {
      const created = await api.createMatch({
        courtSlotId,
        type,
        minLevel: type === 'Competitive' ? parsedMinLevel : undefined,
        maxLevel: type === 'Competitive' ? parsedMaxLevel : undefined,
        minMatchesRequired: type === 'Competitive' ? parseInteger(minMatchesRequired) : undefined,
        note: note.trim().length > 0 ? note.trim() : null,
      });
      navigation.replace('MatchDetail', { matchId: created.id });
      // The organizer's seat is born Held: go straight to paying it, with the detail underneath so leaving the
      // payment screen lands there (m5-mobile-confirmation design.md).
      if (created.mySeat?.status === 'Held' && created.mySeat.heldUntilUtc) {
        navigation.navigate('SeatPayment', { matchId: created.id, heldUntilUtc: created.mySeat.heldUntilUtc });
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se ha podido crear el partido.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <Text style={typography.eyebrow}>{courtName}</Text>
      <Text accessibilityRole="header" style={typography.title}>
        {formatSlotSchedule(startsAt, endsAt)}
      </Text>
      <Text style={typography.note}>{durationMinutes} min</Text>

      <OptionGroup
        label="Tipo de partido"
        options={[
          { value: 'Friendly', label: 'Amistoso' },
          { value: 'Competitive', label: 'Competitivo', disabled: !hasLevel },
        ]}
        value={type}
        onSelect={setType}
      />
      {!hasLevel ? (
        <Text style={styles.hint}>Completa la encuesta de nivel para crear partidos competitivos.</Text>
      ) : null}

      {type === 'Competitive' ? (
        <>
          <Text style={styles.label}>Nivel mínimo</Text>
          <TextInput
            style={styles.input}
            value={minLevel}
            onChangeText={setMinLevel}
            placeholder="Ej. 2.5"
            placeholderTextColor={colors.inkMuted}
            keyboardType="decimal-pad"
          />

          <Text style={styles.label}>Nivel máximo</Text>
          <TextInput
            style={styles.input}
            value={maxLevel}
            onChangeText={setMaxLevel}
            placeholder="Ej. 3.5"
            placeholderTextColor={colors.inkMuted}
            keyboardType="decimal-pad"
          />

          <Text style={styles.label}>Mínimo de partidos jugados (opcional)</Text>
          <TextInput
            style={styles.input}
            value={minMatchesRequired}
            onChangeText={setMinMatchesRequired}
            placeholder="Ej. 5"
            placeholderTextColor={colors.inkMuted}
            keyboardType="number-pad"
          />
        </>
      ) : null}

      <Text style={styles.label}>Nota (opcional)</Text>
      <TextInput
        style={[styles.input, styles.noteInput]}
        value={note}
        onChangeText={setNote}
        placeholder="Ej. Buscamos un cuarto jugador"
        placeholderTextColor={colors.inkMuted}
        multiline
        maxLength={500}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.submitButton, !canSubmit && styles.submitButtonDisabled]}
        onPress={handleSubmit}
        disabled={!canSubmit || isSubmitting}
      >
        <Text style={styles.submitButtonLabel}>{isSubmitting ? 'Creando...' : 'Crear partido'}</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: 20, paddingTop: 20, paddingBottom: 48 },
  hint: { ...typography.note, marginTop: -12, marginBottom: 16 },
  label: { ...typography.note, fontWeight: '700', marginBottom: 6, marginTop: 16 },
  input: {
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    color: colors.ink,
  },
  noteInput: { minHeight: 80, textAlignVertical: 'top' },
  error: { marginTop: 16, color: '#B3261E', fontSize: 14, lineHeight: 20 },
  submitButton: {
    marginTop: 28,
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  submitButtonDisabled: { opacity: 0.5 },
  submitButtonLabel: { color: colors.background, fontSize: 16, fontWeight: '700' },
});
