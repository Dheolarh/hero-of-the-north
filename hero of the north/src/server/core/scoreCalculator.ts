/**
 * Server-side hero point calculation to prevent cheating
 */

export interface LevelCompleteData {
    levelNumber: number;
    alliesSaved: number;
    timeSpent: number;
    retryCount: number;
}

export interface ScoreValidationResult {
    isValid: boolean;
    heroPoints: number;
    errors: string[];
}

// Level configuration - defines max allies per level
// Currently used for validation logic
const LEVEL_CONFIG: Record<number, { maxAllies: number; minTime: number }> = {
    0: { maxAllies: 0, minTime: 5 }, // Tutorial
    1: { maxAllies: 3, minTime: 10 },
    2: { maxAllies: 3, minTime: 10 },
    3: { maxAllies: 4, minTime: 15 },
    4: { maxAllies: 4, minTime: 15 },
    5: { maxAllies: 5, minTime: 20 },
    // Default fallback will be used for others
};

const DEFAULT_MAX_ALLIES = 50;
const DEFAULT_MIN_TIME = 10; // Relaxed min time
const MAX_REASONABLE_TIME = 3600; // 1 hour max per level

/**
 * Calculate hero points (Server Authoritative)
 * Formula: (allies × 100) + max(0, 300 - timeSpent) - (retries × 5)
 */
export function calculateHeroPoints(data: LevelCompleteData): number {
    const alliesPoints = data.alliesSaved * 100;
    const timePoints = Math.max(0, 300 - data.timeSpent);
    const retryPenalty = data.retryCount * 5;

    return Math.round(alliesPoints + timePoints - retryPenalty);
}

/**
 * Validate level completion data and calculate score
 */
export function validateAndCalculateScore(data: LevelCompleteData): ScoreValidationResult {
    const errors: string[] = [];

    // Basic validation
    if (data.levelNumber < 0 || data.levelNumber > 31) {
        errors.push(`Invalid level number: ${data.levelNumber}`);
    }

    // Get config
    const config = LEVEL_CONFIG[data.levelNumber] || {
        maxAllies: DEFAULT_MAX_ALLIES,
        minTime: DEFAULT_MIN_TIME,
    };

    // Validate metrics
    if (data.alliesSaved < 0 || data.alliesSaved > config.maxAllies) {
        errors.push(`Invalid allies count: ${data.alliesSaved} (Max: ${config.maxAllies})`);
    }

    if (data.timeSpent < 0) {
        errors.push(`Invalid time: cannot be negative`);
    } else if (data.timeSpent < config.minTime) {
        errors.push(`Completion too fast: ${data.timeSpent}s (Min: ${config.minTime}s)`);
    }

    if (data.retryCount < 0) {
        errors.push(`Invalid retry count`);
    }

    if (errors.length > 0) {
        return { isValid: false, heroPoints: 0, errors };
    }

    return {
        isValid: true,
        heroPoints: calculateHeroPoints(data),
        errors: [],
    };
}
