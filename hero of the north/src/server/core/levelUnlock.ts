/**
 * Daily level unlock system - Global for all players
 * Tutorial + Level 1 unlocked initially, then +1 level per day
 */
// import { RedisClient } from '@devvit/public-api'; // Removed to avoid type conflicts

const TOTAL_LEVELS = 32; // Tutorial (0) + 31 regular levels
const INITIALLY_UNLOCKED = 2; // Tutorial + Level 1
const MS_PER_DAY = 24 * 60 * 60 * 1000;
const LAUNCH_TIME_KEY = 'game:launchTime';

export interface UnlockStatus {
    unlockedLevels: number; // Number of levels currently unlocked
    nextUnlockTime: number | null; // Timestamp of next unlock (null if all unlocked)
    totalLevels: number;
    daysElapsed: number;
}

export interface LevelUnlockInfo {
    levelNumber: number;
    isUnlocked: boolean;
    unlockTime: number | null; // Timestamp when this level unlocks (null if already unlocked)
    timeUntilUnlock: number | null; // Milliseconds until unlock (null if already unlocked)
}

/**
 * Initialize the game launch time (call once when app is first installed)
 */
export async function initializeLaunchTime(redis: any): Promise<number> {
    const existing = await redis.get(LAUNCH_TIME_KEY);

    // Check if existing is null/undefined or empty string
    if (existing) {
        console.log(`[LevelUnlock] Launch time already set: ${new Date(parseInt(existing)).toISOString()}`);
        return parseInt(existing);
    }

    const now = Date.now();
    await redis.set(LAUNCH_TIME_KEY, now.toString());
    console.log(`[LevelUnlock] Game launch time initialized: ${new Date(now).toISOString()}`);
    return now;
}

/**
 * Get the current unlock status
 */
export async function getUnlockStatus(redis: any): Promise<UnlockStatus> {
    const launchTimeStr = await redis.get(LAUNCH_TIME_KEY);

    if (!launchTimeStr) {
        // If not initialized, initialize now
        await initializeLaunchTime(redis);
        return {
            unlockedLevels: INITIALLY_UNLOCKED,
            nextUnlockTime: Date.now() + MS_PER_DAY,
            totalLevels: TOTAL_LEVELS,
            daysElapsed: 0,
        };
    }

    const launchTime = parseInt(launchTimeStr);
    const now = Date.now();
    const daysElapsed = Math.floor((now - launchTime) / MS_PER_DAY);

    // Calculate unlocked levels: initial + days elapsed, capped at total
    const unlockedLevels = Math.min(INITIALLY_UNLOCKED + daysElapsed, TOTAL_LEVELS);

    // Calculate next unlock time
    let nextUnlockTime: number | null = null;
    if (unlockedLevels < TOTAL_LEVELS) {
        const nextUnlockDay = daysElapsed + 1;
        nextUnlockTime = launchTime + (nextUnlockDay * MS_PER_DAY);
    }

    return {
        unlockedLevels,
        nextUnlockTime,
        totalLevels: TOTAL_LEVELS,
        daysElapsed,
    };
}

/**
 * Get detailed unlock info for all levels
 */
export async function getAllLevelsUnlockInfo(redis: any): Promise<LevelUnlockInfo[]> {
    const status = await getUnlockStatus(redis);
    const now = Date.now();

    // We get launchTime from redis again or infer it
    const launchTimeStr = await redis.get(LAUNCH_TIME_KEY);
    const launchTime = launchTimeStr ? parseInt(launchTimeStr) : now;

    const levels: LevelUnlockInfo[] = [];

    for (let i = 0; i < TOTAL_LEVELS; i++) {
        const isUnlocked = i < status.unlockedLevels;

        let unlockTime: number | null = null;
        let timeUntilUnlock: number | null = null;

        if (!isUnlocked) {
            // Calculate when this level will unlock
            const daysUntilUnlock = i - INITIALLY_UNLOCKED + 1;
            unlockTime = launchTime + (daysUntilUnlock * MS_PER_DAY);
            timeUntilUnlock = Math.max(0, unlockTime - now);
        }

        levels.push({
            levelNumber: i,
            isUnlocked,
            unlockTime,
            timeUntilUnlock,
        });
    }

    return levels;
}

/**
 * Check if a specific level is unlocked
 */
export async function isLevelUnlocked(redis: any, levelNumber: number): Promise<boolean> {
    if (levelNumber < 0 || levelNumber >= TOTAL_LEVELS) {
        return false;
    }

    const status = await getUnlockStatus(redis);
    return levelNumber < status.unlockedLevels;
}
