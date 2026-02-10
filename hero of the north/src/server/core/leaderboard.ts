/**
 * Leaderboard management using Redis sorted sets
 */
// import { RedisClient } from '@devvit/public-api'; // Removed to avoid type conflicts

const LEADERBOARD_KEY = 'leaderboard:global';
const PLAYER_STATS_PREFIX = 'player:stats:';      // Hash: userId -> stats
const PLAYER_LEVEL_PREFIX = 'player:level:';      // Hash: userId:levelNum -> level stats

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

    // Get player stats
    const statsKey = `${PLAYER_STATS_PREFIX}${userId}`;
    const currentStats = await redis.hGetAll(statsKey);

    // Check if level already completed
    const levelKey = `${PLAYER_LEVEL_PREFIX}${userId}:${levelNumber}`;
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
        let rank = await getPlayerRank(redis, userId);
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
    await redis.zAdd(LEADERBOARD_KEY, { member: userId, score: totalPoints });

    const rank = await getPlayerRank(redis, userId);
    return { totalPoints, rank };
}

/**
 * Get top N players
 */
export async function getTopPlayers(
    redis: any,
    limit: number = 50
): Promise<LeaderboardEntry[]> {
    try {
        const topMembers = await redis.zRange(LEADERBOARD_KEY, 0, limit - 1, {
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
            const stats = await redis.hGetAll(`${PLAYER_STATS_PREFIX}${userId}`);
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
        const rank = await redis.zRank(LEADERBOARD_KEY, userId);

        if (rank === undefined || rank === null) return 0;

        const count = await redis.zCard(LEADERBOARD_KEY);
        return count - rank;
    } catch (e) {
        console.error('Error getting rank:', e);
        return 0;
    }
}

/**
 * Get single player standing
 */
export async function getPlayerStanding(redis: any, userId: string): Promise<PlayerStanding | null> {
    const stats = await redis.hGetAll(`${PLAYER_STATS_PREFIX}${userId}`);
    if (!stats || !stats.totalPoints) return null;

    const rank = await getPlayerRank(redis, userId);

    return {
        rank,
        totalPoints: parseInt(stats.totalPoints),
        levelsCompleted: parseInt(stats.levelsCompleted),
    };
}
