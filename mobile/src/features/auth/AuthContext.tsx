import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api } from '../../api/httpClient';
import { setSessionToken, setUnauthorizedHandler } from '../../api/httpClient';
import type { PlayerProfile } from '../../api/types';
import { clearSession, loadSession, saveSession } from './session';

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

interface AuthContextValue {
  status: AuthStatus;
  player: PlayerProfile | null;
  signInWithGoogleIdToken: (idToken: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [player, setPlayer] = useState<PlayerProfile | null>(null);

  const becomeUnauthenticated = async () => {
    await clearSession();
    setSessionToken(null);
    setPlayer(null);
    setStatus('unauthenticated');
  };

  useEffect(() => {
    setUnauthorizedHandler(() => {
      void becomeUnauthenticated();
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  useEffect(() => {
    (async () => {
      const token = await loadSession();
      if (!token) {
        setStatus('unauthenticated');
        return;
      }
      setSessionToken(token);
      try {
        const profile = await api.getOwnProfile();
        setPlayer(profile);
        setStatus('authenticated');
      } catch {
        await becomeUnauthenticated();
      }
    })();
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      player,
      signInWithGoogleIdToken: async (idToken: string) => {
        const { sessionToken, player: authenticatedPlayer } = await api.authenticateWithGoogle(idToken);
        await saveSession(sessionToken);
        setSessionToken(sessionToken);
        setPlayer(authenticatedPlayer);
        setStatus('authenticated');
      },
      signOut: becomeUnauthenticated,
      refreshProfile: async () => {
        const profile = await api.getOwnProfile();
        setPlayer(profile);
      },
    }),
    [status, player],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider.');
  }
  return context;
}
