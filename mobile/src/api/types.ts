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
  confirmedSeats: number;
}

export interface SeatHold {
  heldUntilUtc: string;
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

export type MatchStatus = 'Open' | 'Full';

/** Only an active seat reaches mobile: an expired Held comes back as `mySeat: null`. */
export type SeatStatus = 'Held' | 'Confirmed';

export interface MySeat {
  /** 0–3: 0–1 pair A, 2–3 pair B. */
  position: number;
  status: SeatStatus;
  heldUntilUtc: string | null;
}

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
  confirmedSeats: number;
  mySeat: MySeat | null;
  /** Ordered by position. Held seats aren't included (plan §11). */
  confirmedPlayers: ConfirmedPlayer[];
}

export interface ConfirmedPlayer {
  /** 0–3: 0–1 pair A, 2–3 pair B. */
  position: number;
  playerId: string;
  displayName: string;
  level: number | null;
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
  confirmedSeats: number;
}

export interface SeatHold {
  heldUntilUtc: string;
}

export interface MatchFeed {
  /** My upcoming full matches (4/4 confirmed). */
  confirmed: MatchFeedItem[];
  /** My upcoming matches where I'm confirmed but the match isn't full yet. */
  pendingConfirmation: MatchFeedItem[];
  forYou: MatchFeedItem[];
  outOfRange: MatchFeedItem[];
}
