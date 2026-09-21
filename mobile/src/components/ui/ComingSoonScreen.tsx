import { StyleSheet, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { colors, typography } from '../../theme';

export default function ComingSoonScreen({ title }: { title: string }) {
  return (
    <View style={styles.screen}>
      <StatusBar style="dark" />
      <Text style={typography.eyebrow}>{title.toUpperCase()}</Text>
      <Text accessibilityRole="header" style={typography.title}>
        Próximamente
      </Text>
      <View style={styles.accent} />
      <Text style={typography.note}>Esta sección todavía no está disponible.</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.background,
    paddingHorizontal: 28,
    paddingTop: 64,
  },
  accent: { height: 5, width: 52, backgroundColor: colors.accent, borderRadius: 3, marginVertical: 24 },
});
