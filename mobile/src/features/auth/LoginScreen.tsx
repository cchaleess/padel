import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { colors, typography } from '../../theme';
import { useAuth } from './AuthContext';
import { useGoogleSignIn } from './useGoogleSignIn';

const DEV_PLAYERS = ['Ana', 'Bruno', 'Carla'];

/** Development builds only: sign in as a fictional player to see the app as someone else
 * (specs/dev-player-simulation). The backend route doesn't exist outside Development either. */
function DevSignIn() {
  const { signInAsDevPlayer } = useAuth();
  const [error, setError] = useState<string | null>(null);

  return (
    <View style={styles.devSection}>
      <Text style={styles.devTitle}>Desarrollo · entrar como jugador de prueba</Text>
      <View style={styles.devButtons}>
        {DEV_PLAYERS.map((name) => (
          <Pressable
            key={name}
            accessibilityRole="button"
            style={styles.devButton}
            onPress={() => {
              setError(null);
              signInAsDevPlayer(name).catch(() =>
                setError('No se pudo entrar. ¿Está la API arrancada en Development?'),
              );
            }}>
            <Text style={styles.devButtonLabel}>{name}</Text>
          </Pressable>
        ))}
      </View>
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

export default function LoginScreen() {
  const { signIn, isSigningIn, error } = useGoogleSignIn();

  return (
    <View style={styles.screen}>
      <StatusBar style="dark" />
      <View style={styles.content}>
        <Text style={typography.eyebrow}>NOS VEMOS EN LA PISTA</Text>
        <Text accessibilityRole="header" style={typography.title}>PadelMatch</Text>
        <View style={styles.accent} />
        <Text style={typography.note}>
          Inicia sesión para ver tu perfil y encontrar tu próximo partido.
        </Text>
        <Pressable
          accessibilityRole="button"
          style={({ pressed }) => [styles.googleButton, pressed && styles.googleButtonPressed]}
          onPress={signIn}
          disabled={isSigningIn}
        >
          {isSigningIn ? (
            <ActivityIndicator color={colors.background} />
          ) : (
            <Text style={styles.googleButtonLabel}>Continuar con Google</Text>
          )}
        </Pressable>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        {__DEV__ ? <DevSignIn /> : null}
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
  googleButton: {
    marginTop: 32,
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  googleButtonPressed: { opacity: 0.85 },
  googleButtonLabel: { color: colors.background, fontSize: 17, fontWeight: '700' },
  error: { marginTop: 16, color: '#B3261E', fontSize: 14, lineHeight: 20 },
  devSection: { marginTop: 40, paddingTop: 20, borderTopWidth: 1, borderTopColor: '#D8D5C4' },
  devTitle: { ...typography.note, fontSize: 13 },
  devButtons: { flexDirection: 'row', gap: 10, marginTop: 10 },
  devButton: {
    flex: 1,
    borderWidth: 1,
    borderColor: colors.brandDark,
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  devButtonLabel: { color: colors.brandDark, fontSize: 15, fontWeight: '600' },
});
