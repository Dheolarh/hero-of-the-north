/**
 * Leaderboard service for managing player rankings
 */

import type { RedisClient } from "@devvit/web/server";
import { LeaderboardEntry } from "../../shared/types/api";

/**
 * Get top 50 players from leaderboard
 */
export async function getTop50(redis: RedisClient): Promise<LeaderboardEntry[]> {
    try {
        // Get top 50 from sorted set (sorted by totalPoints descending)
        // Using 'rank' to get members by their position
        const topPlayers = await redis.zRange("leaderboard:global", 0, 49, {
            by: "rank",
            reverse: true,
        });

        const entries: LeaderboardEntry[] = [];

        for (let i = 0; i < topPlayers.length; i++) {
            const userId = topPlayers[i]?.member;

            if (!userId) continue;

            // Get player data (includes totalPoints)
            const playerData = await redis.hGetAll(`player:${userId}:stats`);

            if (playerData) {
                entries.push({
                    rank: i + 1,
                    username: playerData.username || "Unknown",
                    avatarUrl: playerData.avatarUrl || "",
                    totalPoints: parseInt(playerData.totalPoints || "0"),
                });
            }
        }

        return entries;
    } catch (error) {
        console.error("Error fetching top 50:", error);
        return [];
    }
}

/**
 * Get player's current rank
 */
export async function getPlayerRank(
    redis: RedisClient,
    userId: string
): Promise<number> {
    try {
        // Get all members with their ranks
        const allPlayers = await redis.zRange("leaderboard:global", 0, -1, {
            by: "rank",
            reverse: true,
        });

        // Find the player's position
        const rank = allPlayers.findIndex((player) => player.member === userId);
        return rank !== -1 ? rank + 1 : 0; // +1 because rank is 0-indexed
    } catch (error) {
        console.error("Error getting player rank:", error);
        return 0;
    }
}

/**
 * Get player's standing (rank, points, levels completed)
 */
export async function getPlayerStanding(
    redis: RedisClient,
    userId: string
): Promise<{ rank: number; totalPoints: number; levelsCompleted: number }> {
    try {
        const rank = await getPlayerRank(redis, userId);
        const playerData = await redis.hGetAll(`player:${userId}:stats`);

        return {
            rank: rank,
            totalPoints: parseInt(playerData?.totalPoints || "0"),
            levelsCompleted: parseInt(playerData?.levelsCompleted || "0"),
        };
    } catch (error) {
        console.error("Error getting player standing:", error);
        return { rank: 0, totalPoints: 0, levelsCompleted: 0 };
    }
}

/**
 * Update player's total score in leaderboard
 */
export async function updateLeaderboard(
    redis: RedisClient,
    userId: string,
    totalPoints: number
): Promise<void> {
    try {
        await redis.zAdd("leaderboard:global", {
            member: userId,
            score: totalPoints,
        });
    } catch (error) {
        console.error("Error updating leaderboard:", error);
        throw error;
    }
}

/**
 * Save player stats
 */
export async function savePlayerStats(
    redis: RedisClient,
    userId: string,
    username: string,
    avatarUrl: string,
    totalPoints: number,
    levelsCompleted: number
): Promise<void> {
    try {
        await redis.hSet(`player:${userId}:stats`, {
            username,
            avatarUrl,
            totalPoints: totalPoints.toString(),
            levelsCompleted: levelsCompleted.toString(),
            lastUpdated: Date.now().toString(),
        });
    } catch (error) {
        console.error("Error saving player stats:", error);
        throw error;
    }
}

/**
 * Save level score for a player
 */
export async function saveLevelScore(
    redis: RedisClient,
    userId: string,
    levelNumber: number,
    alliesSaved: number,
    timeSpent: number,
    retryCount: number,
    heroPoints: number
): Promise<void> {
    try {
        await redis.hSet(`player:${userId}:level:${levelNumber}`, {
            alliesSaved: alliesSaved.toString(),
            timeSpent: timeSpent.toString(),
            retryCount: retryCount.toString(),
            heroPoints: heroPoints.toString(),
            completedAt: Date.now().toString(),
        });
    } catch (error) {
        console.error("Error saving level score:", error);
        throw error;
    }
}

/**
 * Get all completed levels for a player
 * Since Redis keys() is not available, we track completed levels in a separate set
 */
export async function getCompletedLevels(
    redis: RedisClient,
    userId: string
): Promise<number[]> {
    try {
        // We'll store completed levels in a Redis set
        // This is more efficient than scanning keys
        const completedLevelsStr = await redis.get(`player:${userId}:completed_levels`);

        if (!completedLevelsStr) {
            return [];
        }

        // Parse JSON array of level numbers
        return JSON.parse(completedLevelsStr);
    } catch (error) {
        console.error("Error getting completed levels:", error);
        return [];
    }
}

/**
 * Add a level to the player's completed levels list
 */
export async function addCompletedLevel(
    redis: RedisClient,
    userId: string,
    levelNumber: number
): Promise<void> {
    try {
        const completedLevels = await getCompletedLevels(redis, userId);

        if (!completedLevels.includes(levelNumber)) {
            completedLevels.push(levelNumber);
            await redis.set(
                `player:${userId}:completed_levels`,
                JSON.stringify(completedLevels)
            );
        }
    } catch (error) {
        console.error("Error adding completed level:", error);
        throw error;
    }
}
