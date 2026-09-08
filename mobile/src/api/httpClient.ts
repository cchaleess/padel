import type {
  AuthResponse,
  LevelSurveyRequest,
  PlayerProfile,
  UpdateProfileRequest,
} from './types';

const baseUrl = process.env.EXPO_PUBLIC_API_BASE_URL;

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}

let sessionToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setSessionToken(token: string | null): void {
  sessionToken = token;
}

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  onUnauthorized = handler;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  if (!baseUrl) {
    throw new Error(
      'EXPO_PUBLIC_API_BASE_URL no está configurada (ver mobile/.env.example).',
    );
  }

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> | undefined),
  };
  if (sessionToken) {
    headers.Authorization = `Bearer ${sessionToken}`;
  }

  const response = await fetch(`${baseUrl}${path}`, { ...options, headers });

  if (response.status === 401) {
    onUnauthorized?.();
    throw new ApiError(401, 'La sesión ha caducado.');
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string } | null;
    throw new ApiError(response.status, problem?.title ?? `Error ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  authenticateWithGoogle: (idToken: string) =>
    request<AuthResponse>('/api/auth/google', {
      method: 'POST',
      body: JSON.stringify({ idToken }),
    }),

  getOwnProfile: () => request<PlayerProfile>('/api/players/me'),

  updateOwnProfile: (body: UpdateProfileRequest) =>
    request<PlayerProfile>('/api/players/me', {
      method: 'PUT',
      body: JSON.stringify(body),
    }),

  completeLevelSurvey: (body: LevelSurveyRequest) =>
    request<PlayerProfile>('/api/players/me/level-survey', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
};
