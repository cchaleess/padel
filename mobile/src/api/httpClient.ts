import type {
  AccessRequestStatus,
  Activity,
  AuthResponse,
  ClubDetail,
  ClubSummary,
  CourtSlot,
  CreateMatchRequest,
  LevelSurveyRequest,
  MatchDetail,
  MatchFeed,
  PlayerProfile,
  SeatHold,
  SubmitClubRequest,
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

  // Some endpoints answer 200 with no body (e.g. seat confirm/release), so json() would throw on them.
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  authenticateWithGoogle: (idToken: string) =>
    request<AuthResponse>('/api/auth/google', {
      method: 'POST',
      body: JSON.stringify({ idToken }),
    }),

  /** Development only: the backend maps this route only in Development (specs/dev-player-simulation). */
  createDevSession: (name: string) =>
    request<AuthResponse>('/api/dev/session', {
      method: 'POST',
      body: JSON.stringify({ name }),
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

  getNearbyClubs: (params?: { lat?: number; lng?: number }) => {
    const query = new URLSearchParams();
    if (params?.lat !== undefined && params?.lng !== undefined) {
      query.set('lat', String(params.lat));
      query.set('lng', String(params.lng));
    }
    const suffix = query.toString();
    return request<ClubSummary[]>(`/api/clubs/nearby${suffix ? `?${suffix}` : ''}`);
  },

  searchClubs: (query: string) =>
    request<ClubSummary[]>(`/api/clubs/search?q=${encodeURIComponent(query)}`),

  getClubDetails: (id: string) => request<ClubDetail>(`/api/clubs/${id}`),

  getClubSlots: (clubId: string, courtId?: string) =>
    request<CourtSlot[]>(`/api/clubs/${clubId}/slots${courtId ? `?courtId=${courtId}` : ''}`),

  submitClub: (body: SubmitClubRequest) =>
    request<ClubDetail>('/api/clubs', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  createMatch: (body: CreateMatchRequest) =>
    request<MatchDetail>('/api/matches', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  getMatchDetails: (id: string) => request<MatchDetail>(`/api/matches/${id}`),

  getMatchFeed: (params?: { lat?: number; lng?: number }) => {
    const query = new URLSearchParams();
    if (params?.lat !== undefined && params?.lng !== undefined) {
      query.set('lat', String(params.lat));
      query.set('lng', String(params.lng));
    }
    const suffix = query.toString();
    return request<MatchFeed>(`/api/matches/feed${suffix ? `?${suffix}` : ''}`);
  },

  /** `position` 0–3 picks the seat (0–1 pair A, 2–3 pair B); omitted, the backend picks any free one. */
  holdSeat: (matchId: string, position?: number) =>
    request<SeatHold>(`/api/matches/${matchId}/hold`, {
      method: 'POST',
      body: JSON.stringify(position === undefined ? {} : { position }),
    }),

  /** Exceptional access to a competitive match outside my criteria (m6-quality-rules). */
  requestAccess: (matchId: string, position: number) =>
    request<void>(`/api/matches/${matchId}/exception-requests`, {
      method: 'POST',
      body: JSON.stringify({ position }),
    }),

  voteAccess: (matchId: string, playerId: string, approve: boolean) =>
    request<{ status: AccessRequestStatus }>(
      `/api/matches/${matchId}/exception-requests/${playerId}/${approve ? 'approve' : 'reject'}`,
      { method: 'POST' },
    ),

  getActivity: () => request<Activity>('/api/activity'),

  confirmSeat: (matchId: string) => request<void>(`/api/matches/${matchId}/confirm`, { method: 'POST' }),

  releaseSeat: (matchId: string) => request<void>(`/api/matches/${matchId}/release`, { method: 'POST' }),
};
