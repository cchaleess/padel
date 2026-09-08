import { useCallback, useState } from 'react';
import { GoogleSignin } from '@react-native-google-signin/google-signin';
import { useAuth } from './AuthContext';

let configured = false;

function ensureConfigured(): void {
  if (configured) {
    return;
  }
  const webClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID;
  if (!webClientId) {
    throw new Error(
      'EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID no está configurada (ver mobile/.env.example).',
    );
  }
  GoogleSignin.configure({ webClientId });
  configured = true;
}

export function useGoogleSignIn() {
  const { signInWithGoogleIdToken } = useAuth();
  const [isSigningIn, setIsSigningIn] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const signIn = useCallback(async () => {
    setError(null);
    setIsSigningIn(true);
    try {
      ensureConfigured();
      await GoogleSignin.hasPlayServices({ showPlayServicesUpdateDialog: true });
      const response = await GoogleSignin.signIn();
      if (response.type === 'cancelled') {
        return;
      }
      const idToken = response.data.idToken;
      if (!idToken) {
        setError('Google no ha devuelto un token de identidad válido.');
        return;
      }
      await signInWithGoogleIdToken(idToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se ha podido iniciar sesión con Google.');
    } finally {
      setIsSigningIn(false);
    }
  }, [signInWithGoogleIdToken]);

  return { signIn, isSigningIn, error };
}
