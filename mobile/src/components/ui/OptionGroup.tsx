import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../theme';

interface Option<T extends string> {
  value: T;
  label: string;
  disabled?: boolean;
}

interface OptionGroupProps<T extends string> {
  label: string;
  options: Option<T>[];
  value: T | null;
  onSelect: (value: T) => void;
}

export default function OptionGroup<T extends string>({ label, options, value, onSelect }: OptionGroupProps<T>) {
  return (
    <View style={styles.group}>
      <Text style={styles.groupLabel}>{label}</Text>
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <Pressable
            key={option.value}
            style={[styles.option, selected && styles.optionSelected, option.disabled && styles.optionDisabled]}
            onPress={() => onSelect(option.value)}
            disabled={option.disabled}
          >
            <Text style={[styles.optionLabel, selected && styles.optionLabelSelected]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
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
  optionDisabled: { opacity: 0.5 },
  optionLabel: { color: colors.ink, fontSize: 15 },
  optionLabelSelected: { fontWeight: '700', color: colors.brandDark },
});
