/**
 * Leaderboard management using Redis sorted sets
 */
// import { RedisClient } from '@devvit/public-api'; // Removed to avoid type conflicts

function getWeeklyId(): string {
    // Reset weekly on Monday 00:00 UTC
    // Unix epoch was Thursday, Jan 1, 1970. Monday was Jan 5 (4 days offset).
    const fourDaysInMs = 4 * 24 * 60 * 60 * 1000;
    const weekInMs = 7 * 24 * 60 * 60 * 1000;
    const weekTimestamp = Math.floor((Date.now() - fourDaysInMs) / weekInMs);
    return `W_${weekTimestamp}`;
}

function getLeaderboardKey(): string {
    return `leaderboard:${getWeeklyId()}`;
}

function getPlayerStatsPrefix(): string {
    return `player:stats:${getWeeklyId()}:`;
}

function getPlayerLevelPrefix(): string {
    return `player:level:${getWeeklyId()}:`;
}

export interface PlayerStats {
    userId: string;
    username: string;
    avatarUrl?: string; // Added avatar URL
    totalPoints: number;
    levelsCompleted: number;
    lastPlayed: number;
}

export interface PlayerStanding {
    rank: number; // 1-based rank
    totalPoints: number;
    levelsCompleted: number;
}

export interface LeaderboardEntry {
    rank: number;
    username: string;
    userId: string;
    avatarUrl?: string; // Added avatar URL
    totalPoints: number;
}

/**
 * Submit score and update leaderboard
 */
export async function submitScore(
    redis: any, // Using any to avoid type mismatch
    userId: string,
    username: string,
    avatarUrl: string, // Accept avatar URL
    levelNumber: number,
    heroPoints: number,
    alliesSaved: number,
    timeSpent: number,
    retryCount: number
): Promise<{ totalPoints: number; rank: number }> {
    const now = Date.now();

    // ── Atomic duplicate-submission guard ────────────────────────────────────
    // Use a short-lived Redis lock (SET NX + EX) so that two in-flight requests
    // for the same user+level can't both slip through the "already played" check
    // before either has written to Redis (classic read-then-write race condition).
    const lockKey = `lock:score:${userId}:${levelNumber}`;
    // SET NX returns true only when the key did NOT previously exist (we got the lock).
    const gotLock = await redis.set(lockKey, '1', { nx: true, ex: 10 });
    if (!gotLock) {
        // Another request is already processing this user+level — reject early.
        const statsKey = getPlayerStatsPrefix() + userId;
        const currentStats = await redis.hGetAll(statsKey);
        const totalPoints = parseInt(currentStats?.totalPoints || '0');
        const rank = await getPlayerRank(redis, userId);
        return { totalPoints, rank };
    }

    try {
        // Get player stats
        const statsKey = getPlayerStatsPrefix() + userId;
        const currentStats = await redis.hGetAll(statsKey);

        // Check if level already completed
        const levelKey = getPlayerLevelPrefix() + userId + ":" + levelNumber;
        const existingLevel = await redis.hGetAll(levelKey);

        let totalPoints = parseInt(currentStats?.totalPoints || '0');
        let levelsCompleted = parseInt(currentStats?.levelsCompleted || '0');

        const existingPoints = parseInt(existingLevel?.heroPoints || '0');
        const isNewLevel = !existingLevel?.heroPoints;
        const isImprovedScore = heroPoints > existingPoints;

        if (isNewLevel) {
            levelsCompleted++;
            totalPoints += heroPoints;
        } else if (isImprovedScore) {
            totalPoints = totalPoints - existingPoints + heroPoints;
        } else {
            // Score not improved
            const rank = await getPlayerRank(redis, userId);
            return { totalPoints, rank };
        }

        // Save level stats
        await redis.hSet(levelKey, {
            levelNumber: levelNumber.toString(),
            heroPoints: heroPoints.toString(),
            alliesSaved: alliesSaved.toString(),
            timeSpent: timeSpent.toString(),
            retryCount: retryCount.toString(),
            completedAt: now.toString(),
        });

        // Update player stats
        // Only update avatarUrl if provided (it might not be available in some contexts, but should be from api)
        const statsUpdate: any = {
            userId,
            username,
            totalPoints: totalPoints.toString(),
            levelsCompleted: levelsCompleted.toString(),
            lastPlayed: now.toString(),
        };

        if (avatarUrl) {
            statsUpdate.avatarUrl = avatarUrl;
        }

        await redis.hSet(statsKey, statsUpdate);

        // Update leaderboard
        await redis.zAdd(getLeaderboardKey(), { member: userId, score: totalPoints });

        const rank = await getPlayerRank(redis, userId);
        return { totalPoints, rank };
    } finally {
        // Always release the lock so a legitimate future attempt (e.g. improved score
        // on a retry a few seconds later) isn't permanently blocked.
        await redis.del(lockKey);
    }
}


/**
 * Get top N players
 */
export async function getTopPlayers(
    redis: any,
    limit: number = 50
): Promise<LeaderboardEntry[]> {
    try {
        const topMembers = await redis.zRange(getLeaderboardKey(), 0, limit - 1, {
            by: 'rank',
            reverse: true
        });

        const entries: LeaderboardEntry[] = [];
        for (let i = 0; i < topMembers.length; i++) {
            const member = topMembers[i];
            const userId = member.member;
            const score = member.score;

            if (!userId) continue;

            // Get username and avatar
            const stats = await redis.hGetAll(getPlayerStatsPrefix() + userId);
            entries.push({
                rank: i + 1,
                userId,
                username: stats?.username || 'Unknown',
                avatarUrl: stats?.avatarUrl || '', // Return avatar URL
                totalPoints: score,
            });
        }
        return entries;
    } catch (e) {
        console.error('Error fetching leaderboard:', e);
        return [];
    }
}

/**
 * Get rank for specific user (1-based, highest score is #1)
 */
export async function getPlayerRank(redis: any, userId: string): Promise<number> {
    try {
        const rank = await redis.zRevRank(getLeaderboardKey(), userId);

        if (rank === undefined || rank === null) return 0;

        return rank + 1;
    } catch (e) {
        console.error('Error getting rank:', e);
        return 0;
    }
}

/**
 * Get single player standing
 */
export async function getPlayerStanding(redis: any, userId: string): Promise<PlayerStanding | null> {
    const stats = await redis.hGetAll(getPlayerStatsPrefix() + userId);
    if (!stats || !stats.totalPoints) return null;

    const rank = await getPlayerRank(redis, userId);

    return {
        rank,
        totalPoints: parseInt(stats.totalPoints),
        levelsCompleted: parseInt(stats.levelsCompleted),
    };
}

/**
 * Clear scores and stats for specific usernames
 */
export async function clearPlayerScores(redis: any, usernames: string[]): Promise<string[]> {
    const leaderboardKey = getLeaderboardKey();
    const statsPrefix = getPlayerStatsPrefix();
    const levelPrefix = getPlayerLevelPrefix();

    const upperUsernames = usernames.map(u => u.toUpperCase());
    const topMembers = await redis.zRange(leaderboardKey, 0, -1, { by: 'rank' });
    const cleared: string[] = [];

    for (const member of topMembers) {
        const userId = member.member;
        if (!userId) continue;

        const stats = await redis.hGetAll(statsPrefix + userId);
        const username = stats?.username;

        if (username && upperUsernames.includes(username.toUpperCase())) {
            // Delete from leaderboard
            await redis.zRem(leaderboardKey, userId);
            // Delete stats
            await redis.del(statsPrefix + userId);
            // Delete levels (0 to 31)
            for (let lvl = 0; lvl <= 31; lvl++) {
                await redis.del(`${levelPrefix}${userId}:${lvl}`);
            }
            cleared.push(username);
        }
    }
    return cleared;
}
