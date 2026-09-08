import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { colors, typography } from '../../theme';
import { useGoogleSignIn } from './useGoogleSignIn';

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
});
