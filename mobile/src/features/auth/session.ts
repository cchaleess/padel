import * as SecureStore from 'expo-secure-store';

const SESSION_TOKEN_KEY = 'padelmatch.sessionToken';

export const saveSession = (sessionToken: string): Promise<void> =>
  SecureStore.setItemAsync(SESSION_TOKEN_KEY, sessionToken);

export const loadSession = (): Promise<string | null> =>
  SecureStore.getItemAsync(SESSION_TOKEN_KEY);

export const clearSession = (): Promise<void> =>
  SecureStore.deleteItemAsync(SESSION_TOKEN_KEY);
