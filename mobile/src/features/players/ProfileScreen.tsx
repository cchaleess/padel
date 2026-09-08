import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { StatusBar } from 'expo-status-bar';
import { api, ApiError } from '../../api/httpClient';
import { colors, typography } from '../../theme';
import { useAuth } from '../auth/AuthContext';
import { formatDateOnly, parseDateOnly } from './date';
import { levelConfidenceLabels } from './levelLabels';

export default function ProfileScreen({ onStartLevelSurvey }: { onStartLevelSurvey: () => void }) {
  const { player, signOut, refreshProfile } = useAuth();
  const [cityOrZone, setCityOrZone] = useState(player?.cityOrZone ?? '');
  const [photoUrl, setPhotoUrl] = useState(player?.photoUrl ?? '');
  const [dateOfBirth, setDateOfBirth] = useState(player?.dateOfBirth);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  if (!player) {
    return null;
  }

  const hasLevel = player.levelConfidence !== 'None';

  const handleSave = async () => {
    setError(null);
    setSaved(false);
    setIsSaving(true);
    try {
      await api.updateOwnProfile({
        cityOrZone: cityOrZone.trim().length > 0 ? cityOrZone.trim() : null,
        dateOfBirth: dateOfBirth ?? null,
        photoUrl: photoUrl.trim().length > 0 ? photoUrl.trim() : null,
      });
      await refreshProfile();
      setSaved(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se ha podido guardar el perfil.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <StatusBar style="dark" />
      <Text style={typography.eyebrow}>TU PERFIL</Text>
      <Text accessibilityRole="header" style={typography.title}>{player.displayName}</Text>
      <View style={styles.accent} />

      <View style={styles.levelCard}>
        <Text style={styles.levelCardTitle}>{levelConfidenceLabels[player.levelConfidence]}</Text>
        {hasLevel ? (
          <Text style={styles.levelCardValue}>Nivel {player.level?.toFixed(1)}</Text>
        ) : (
          <Pressable style={styles.surveyButton} onPress={onStartLevelSurvey}>
            <Text style={styles.surveyButtonLabel}>Completar encuesta de nivel</Text>
          </Pressable>
        )}
      </View>

      <Text style={styles.label}>Ciudad o zona</Text>
      <TextInput
        style={styles.input}
        value={cityOrZone}
        onChangeText={setCityOrZone}
        placeholder="Ej. Madrid centro"
        placeholderTextColor={colors.inkMuted}
      />

      <Text style={styles.label}>Fecha de nacimiento</Text>
      <Pressable style={styles.input} onPress={() => setShowDatePicker(true)}>
        <Text style={dateOfBirth ? styles.inputText : styles.inputPlaceholder}>
          {dateOfBirth ?? 'Sin definir'}
        </Text>
      </Pressable>
      {showDatePicker ? (
        <DateTimePicker
          value={dateOfBirth ? parseDateOnly(dateOfBirth) : new Date(2000, 0, 1)}
          mode="date"
          display="default"
          maximumDate={new Date()}
          onChange={(event, selectedDate) => {
            setShowDatePicker(false);
            if (event.type === 'set' && selectedDate) {
              setDateOfBirth(formatDateOnly(selectedDate));
            }
          }}
        />
      ) : null}

      <Text style={styles.label}>URL de foto de perfil</Text>
      <TextInput
        style={styles.input}
        value={photoUrl}
        onChangeText={setPhotoUrl}
        placeholder="https://..."
        placeholderTextColor={colors.inkMuted}
        autoCapitalize="none"
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}
      {saved ? <Text style={styles.saved}>Perfil actualizado.</Text> : null}

      <Pressable style={styles.saveButton} onPress={handleSave} disabled={isSaving}>
        <Text style={styles.saveButtonLabel}>{isSaving ? 'Guardando...' : 'Guardar cambios'}</Text>
      </Pressable>

      <Pressable style={styles.signOutButton} onPress={signOut}>
        <Text style={styles.signOutLabel}>Cerrar sesión</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: 28, paddingVertical: 48, paddingBottom: 64 },
  accent: { height: 5, width: 52, backgroundColor: colors.accent, borderRadius: 3, marginVertical: 24 },
  levelCard: {
    backgroundColor: '#EDEBDD',
    borderRadius: 14,
    padding: 18,
    marginBottom: 28,
  },
  levelCardTitle: { fontSize: 15, fontWeight: '700', color: colors.brandDark },
  levelCardValue: { marginTop: 6, fontSize: 20, fontWeight: '800', color: colors.ink },
  surveyButton: { marginTop: 12, alignSelf: 'flex-start' },
  surveyButtonLabel: { color: colors.brandDark, fontWeight: '700', textDecorationLine: 'underline' },
  label: { ...typography.note, fontWeight: '700', marginTop: 16, marginBottom: 6 },
  input: {
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    justifyContent: 'center',
    minHeight: 48,
    color: colors.ink,
  },
  inputText: { color: colors.ink, fontSize: 16 },
  inputPlaceholder: { color: colors.inkMuted, fontSize: 16 },
  error: { marginTop: 16, color: '#B3261E', fontSize: 14, lineHeight: 20 },
  saved: { marginTop: 16, color: colors.brandDark, fontSize: 14, lineHeight: 20 },
  saveButton: {
    marginTop: 24,
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  saveButtonLabel: { color: colors.background, fontSize: 16, fontWeight: '700' },
  signOutButton: { marginTop: 20, alignItems: 'center' },
  signOutLabel: { color: colors.inkMuted, fontSize: 14, textDecorationLine: 'underline' },
});
