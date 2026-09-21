import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api, ApiError } from '../../api/httpClient';
import { colors, typography } from '../../theme';
import type { ClubsStackParamList } from './ClubsStackNavigator';

type Props = NativeStackScreenProps<ClubsStackParamList, 'SubmitClub'>;

export default function SubmitClubScreen({ navigation }: Props) {
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [cityOrZone, setCityOrZone] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    setError(null);
    setIsSubmitting(true);
    try {
      await api.submitClub({
        name: name.trim(),
        address: address.trim(),
        cityOrZone: cityOrZone.trim().length > 0 ? cityOrZone.trim() : null,
      });
      navigation.goBack();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se ha podido aportar el club.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const canSubmit = name.trim().length > 0 && address.trim().length > 0;

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <Text style={styles.label}>Nombre del club</Text>
      <TextInput
        style={styles.input}
        value={name}
        onChangeText={setName}
        placeholder="Ej. Padel Indoor Norte"
        placeholderTextColor={colors.inkMuted}
      />

      <Text style={styles.label}>Dirección</Text>
      <TextInput
        style={styles.input}
        value={address}
        onChangeText={setAddress}
        placeholder="Ej. Calle Mayor 10"
        placeholderTextColor={colors.inkMuted}
      />

      <Text style={styles.label}>Ciudad o zona (opcional)</Text>
      <TextInput
        style={styles.input}
        value={cityOrZone}
        onChangeText={setCityOrZone}
        placeholder="Ej. Madrid centro"
        placeholderTextColor={colors.inkMuted}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.submitButton, !canSubmit && styles.submitButtonDisabled]}
        onPress={handleSubmit}
        disabled={!canSubmit || isSubmitting}
      >
        <Text style={styles.submitButtonLabel}>{isSubmitting ? 'Enviando...' : 'Aportar club'}</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: 20, paddingTop: 20, paddingBottom: 48 },
  label: { ...typography.note, fontWeight: '700', marginBottom: 6, marginTop: 16 },
  input: {
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    color: colors.ink,
  },
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
