export type LevelConfidence = 'None' | 'Low' | 'Medium' | 'High';

export type YearsPlayingPadel = 'LessThanOne' | 'OneToTwo' | 'ThreeToFive' | 'MoreThanFive';

export type WeeklyFrequency = 'Rarely' | 'OnceAWeek' | 'TwoOrThreeTimesAWeek' | 'FourOrMoreTimesAWeek';

export type SelfPerceivedLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Competitive';

export interface PlayerProfile {
  id: string;
  displayName: string;
  cityOrZone: string | null;
  dateOfBirth: string | null;
  photoUrl: string | null;
  level: number | null;
  levelConfidence: LevelConfidence;
  matchesPlayed: number;
}

export interface AuthResponse {
  sessionToken: string;
  player: PlayerProfile;
}

export interface UpdateProfileRequest {
  cityOrZone?: string | null;
  dateOfBirth?: string | null;
  photoUrl?: string | null;
}

export interface LevelSurveyRequest {
  yearsPlaying: YearsPlayingPadel;
  weeklyFrequency: WeeklyFrequency;
  selfPerceivedLevel: SelfPerceivedLevel;
}

export type ClubStatus = 'Official' | 'UserSubmitted';

export interface ClubSummary {
  id: string;
  name: string;
  cityOrZone: string | null;
  status: ClubStatus;
  distanceKm: number | null;
}

export interface Court {
  id: string;
  name: string;
}

export interface ClubDetail {
  id: string;
  name: string;
  address: string;
  cityOrZone: string | null;
  status: ClubStatus;
  courts: Court[];
}

export interface CourtSlot {
  id: string;
  courtId: string;
  courtName: string;
  startsAt: string;
  endsAt: string;
  durationMinutes: number;
}

export interface SubmitClubRequest {
  name: string;
  address: string;
  cityOrZone?: string | null;
}

export type MatchType = 'Competitive' | 'Friendly';

export type MatchStatus = 'Open';

export interface MatchDetail {
  id: string;
  clubId: string;
  clubName: string;
  courtId: string;
  courtName: string;
  startsAt: string;
  endsAt: string;
  durationMinutes: number;
  type: MatchType;
  status: MatchStatus;
  organizerId: string;
  organizerLevelAtCreation: number | null;
  minLevel: number | null;
  maxLevel: number | null;
  minMatchesRequired: number | null;
  note: string | null;
}

export interface CreateMatchRequest {
  courtSlotId: string;
  type: MatchType;
  minLevel?: number | null;
  maxLevel?: number | null;
  minMatchesRequired?: number | null;
  note?: string | null;
}

export interface MatchFeedItem {
  id: string;
  clubId: string;
  clubName: string;
  courtName: string;
  startsAt: string;
  endsAt: string;
  durationMinutes: number;
  type: MatchType;
  minLevel: number | null;
  maxLevel: number | null;
  distanceKm: number | null;
}

export interface MatchFeed {
  forYou: MatchFeedItem[];
  outOfRange: MatchFeedItem[];
}
