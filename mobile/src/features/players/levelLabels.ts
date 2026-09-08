import type { LevelConfidence, SelfPerceivedLevel, WeeklyFrequency, YearsPlayingPadel } from '../../api/types';

export const levelConfidenceLabels: Record<LevelConfidence, string> = {
  None: 'Sin nivel todavía',
  Low: 'Nivel orientativo',
  Medium: 'Nivel con confianza media',
  High: 'Nivel con confianza alta',
};

export const yearsPlayingOptions: { value: YearsPlayingPadel; label: string }[] = [
  { value: 'LessThanOne', label: 'Menos de 1 año' },
  { value: 'OneToTwo', label: 'De 1 a 2 años' },
  { value: 'ThreeToFive', label: 'De 3 a 5 años' },
  { value: 'MoreThanFive', label: 'Más de 5 años' },
];

export const weeklyFrequencyOptions: { value: WeeklyFrequency; label: string }[] = [
  { value: 'Rarely', label: 'Rara vez' },
  { value: 'OnceAWeek', label: 'Una vez por semana' },
  { value: 'TwoOrThreeTimesAWeek', label: '2-3 veces por semana' },
  { value: 'FourOrMoreTimesAWeek', label: '4 o más veces por semana' },
];

export const selfPerceivedLevelOptions: { value: SelfPerceivedLevel; label: string }[] = [
  { value: 'Beginner', label: 'Principiante' },
  { value: 'Intermediate', label: 'Intermedio' },
  { value: 'Advanced', label: 'Avanzado' },
  { value: 'Competitive', label: 'Competitivo' },
];
