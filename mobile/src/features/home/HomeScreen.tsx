import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../theme';

export default function HomeScreen() {
  return (
    <View style={styles.screen}>
      <StatusBar style="dark" />
      <View style={styles.content}>
        <Text style={typography.eyebrow}>NOS VEMOS EN LA PISTA</Text>
        <Text accessibilityRole="header" style={typography.title}>PadelMatch</Text>
        <View style={styles.accent} />
        <Text style={typography.body}>
          Tu próximo partido de pádel empieza aquí.
        </Text>
        <Text style={typography.note}>
          Estamos preparando un lugar para encontrar partidos y compartir pista.
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.background,
    paddingHorizontal: 28,
    paddingVertical: 48,
  },
  content: { width: '100%', maxWidth: 460 },
  accent: { height: 5, width: 52, backgroundColor: colors.accent, borderRadius: 3, marginVertical: 24 },
});
