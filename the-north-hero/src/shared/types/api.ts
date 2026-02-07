export type InitResponse = {
  type: "init";
  postId: string;
  username: string;
  snoovatarUrl: string;
  previousTime: string; // empty string if no previous time exists
};

export type LevelCompletedRequest = {
  type: "level-completed";
  username: string;
  postId: string;
  time: string;
};

export type LevelCompletedResponse = {
  type: "level-completed";
  success: boolean;
  message?: string;
};

// ===== LEADERBOARD TYPES =====

export type ScoreSubmissionRequest = {
  levelNumber: number;
  alliesSaved: number;
  timeSpent: number;
  retryCount: number;
};

export type ScoreSubmissionResponse = {
  success: boolean;
  heroPoints: number; // Server-calculated points
  totalPoints: number; // Player's new total
  rank: number; // Player's new rank
  message?: string;
};

export type LeaderboardEntry = {
  rank: number;
  username: string;
  avatarUrl: string;
  totalPoints: number;
};

export type LeaderboardResponse = {
  entries: LeaderboardEntry[];
};

export type PlayerStandingResponse = {
  rank: number;
  totalPoints: number;
  levelsCompleted: number;
};
