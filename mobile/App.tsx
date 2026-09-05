import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View } from 'react-native';

export default function App() {
  return (
    <View style={styles.screen}>
      <StatusBar style="dark" />
      <View style={styles.content}>
        <Text style={styles.eyebrow}>NOS VEMOS EN LA PISTA</Text>
        <Text accessibilityRole="header" style={styles.title}>PadelMatch</Text>
        <View style={styles.accent} />
        <Text style={styles.description}>
          Tu próximo partido de pádel empieza aquí.
        </Text>
        <Text style={styles.note}>
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
    backgroundColor: '#F5F4EE',
    paddingHorizontal: 28,
    paddingVertical: 48,
  },
  content: { width: '100%', maxWidth: 460 },
  eyebrow: { fontSize: 11, letterSpacing: 2, fontWeight: '700', color: '#36584A', marginBottom: 18 },
  title: { fontSize: 44, fontWeight: '800', letterSpacing: -2, color: '#163D2F' },
  accent: { height: 5, width: 52, backgroundColor: '#B4CB45', borderRadius: 3, marginVertical: 24 },
  description: { fontSize: 26, lineHeight: 34, fontWeight: '600', color: '#163D2F' },
  note: { fontSize: 16, lineHeight: 25, color: '#53635B', marginTop: 18 },
});
