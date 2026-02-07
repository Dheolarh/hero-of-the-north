/**
 * Score calculation and validation service
 * Recalculates hero points server-side to prevent cheating
 */

export interface LevelStats {
    alliesSaved: number;
    timeSpent: number;
    retryCount: number;
}

export interface ValidationResult {
    isValid: boolean;
    error?: string;
}

/**
 * Validate level completion stats
 */
export function validateLevelStats(
    levelNumber: number,
    stats: LevelStats
): ValidationResult {
    // Validate level number
    if (levelNumber < 1 || levelNumber > 50) {
        return { isValid: false, error: "Invalid level number" };
    }

    // Validate allies saved (0-10 is reasonable range)
    if (stats.alliesSaved < 0 || stats.alliesSaved > 10) {
        return { isValid: false, error: "Invalid allies count" };
    }

    // Validate time spent (must be positive, max 1 hour = 3600 seconds)
    if (stats.timeSpent <= 0 || stats.timeSpent > 3600) {
        return { isValid: false, error: "Invalid time spent" };
    }

    // Validate retry count (must be non-negative, max 100 retries)
    if (stats.retryCount < 0 || stats.retryCount > 100) {
        return { isValid: false, error: "Invalid retry count" };
    }

    return { isValid: true };
}

/**
 * Calculate hero points based on performance
 * IMPORTANT: This is the server-side calculation - client values are ignored
 */
export function calculateHeroPoints(stats: LevelStats): number {
    const { alliesSaved, timeSpent, retryCount } = stats;

    // Base points for allies (100 points per ally)
    const allyPoints = alliesSaved * 100;
    // 120+ seconds = 0 bonus
    const timeBonus = Math.max(0, Math.min(300, 300 - (timeSpent - 30) * 2));


    const retryPenalty = retryCount * 50;

    // Calculate total (minimum 0)
    const total = Math.max(0, allyPoints + timeBonus - retryPenalty);

    return Math.floor(total);
}
