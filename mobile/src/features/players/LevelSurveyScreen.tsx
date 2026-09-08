import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { api, ApiError } from '../../api/httpClient';
import type { SelfPerceivedLevel, WeeklyFrequency, YearsPlayingPadel } from '../../api/types';
import { colors, typography } from '../../theme';
import { useAuth } from '../auth/AuthContext';
import { selfPerceivedLevelOptions, weeklyFrequencyOptions, yearsPlayingOptions } from './levelLabels';

interface OptionGroupProps<T extends string> {
  label: string;
  options: { value: T; label: string }[];
  value: T | null;
  onSelect: (value: T) => void;
}

function OptionGroup<T extends string>({ label, options, value, onSelect }: OptionGroupProps<T>) {
  return (
    <View style={styles.group}>
      <Text style={styles.groupLabel}>{label}</Text>
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <Pressable
            key={option.value}
            style={[styles.option, selected && styles.optionSelected]}
            onPress={() => onSelect(option.value)}
          >
            <Text style={[styles.optionLabel, selected && styles.optionLabelSelected]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

export default function LevelSurveyScreen({ onDone, onCancel }: { onDone: () => void; onCancel: () => void }) {
  const { refreshProfile } = useAuth();
  const [yearsPlaying, setYearsPlaying] = useState<YearsPlayingPadel | null>(null);
  const [weeklyFrequency, setWeeklyFrequency] = useState<WeeklyFrequency | null>(null);
  const [selfPerceivedLevel, setSelfPerceivedLevel] = useState<SelfPerceivedLevel | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = yearsPlaying && weeklyFrequency && selfPerceivedLevel;

  const handleSubmit = async () => {
    if (!yearsPlaying || !weeklyFrequency || !selfPerceivedLevel) {
      return;
    }
    setError(null);
    setIsSubmitting(true);
    try {
      await api.completeLevelSurvey({ yearsPlaying, weeklyFrequency, selfPerceivedLevel });
      await refreshProfile();
      onDone();
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setError('Antes de completar la encuesta, define tu fecha de nacimiento en el perfil.');
      } else {
        setError('No se ha podido completar la encuesta.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <StatusBar style="dark" />
      <Text style={typography.eyebrow}>ENCUESTA DE NIVEL</Text>
      <Text accessibilityRole="header" style={typography.title}>¿Cómo juegas?</Text>
      <View style={styles.accent} />

      <OptionGroup label="Años jugando pádel" options={yearsPlayingOptions} value={yearsPlaying} onSelect={setYearsPlaying} />
      <OptionGroup label="Frecuencia semanal" options={weeklyFrequencyOptions} value={weeklyFrequency} onSelect={setWeeklyFrequency} />
      <OptionGroup label="Autopercepción de nivel" options={selfPerceivedLevelOptions} value={selfPerceivedLevel} onSelect={setSelfPerceivedLevel} />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.submitButton, !canSubmit && styles.submitButtonDisabled]}
        onPress={handleSubmit}
        disabled={!canSubmit || isSubmitting}
      >
        <Text style={styles.submitButtonLabel}>{isSubmitting ? 'Enviando...' : 'Guardar encuesta'}</Text>
      </Pressable>

      <Pressable style={styles.cancelButton} onPress={onCancel}>
        <Text style={styles.cancelLabel}>Volver al perfil</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: 28, paddingVertical: 48, paddingBottom: 64 },
  accent: { height: 5, width: 52, backgroundColor: colors.accent, borderRadius: 3, marginVertical: 24 },
  group: { marginBottom: 24 },
  groupLabel: { ...typography.note, fontWeight: '700', marginBottom: 10 },
  option: {
    borderWidth: 1,
    borderColor: '#D8D5C4',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    marginBottom: 8,
  },
  optionSelected: { borderColor: colors.brandDark, backgroundColor: '#EDEBDD' },
  optionLabel: { color: colors.ink, fontSize: 15 },
  optionLabelSelected: { fontWeight: '700', color: colors.brandDark },
  error: { marginBottom: 16, color: '#B3261E', fontSize: 14, lineHeight: 20 },
  submitButton: {
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  submitButtonDisabled: { opacity: 0.5 },
  submitButtonLabel: { color: colors.background, fontSize: 16, fontWeight: '700' },
  cancelButton: { marginTop: 20, alignItems: 'center' },
  cancelLabel: { color: colors.inkMuted, fontSize: 14, textDecorationLine: 'underline' },
});
