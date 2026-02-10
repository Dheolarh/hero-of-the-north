import { Hono } from 'hono';
import { redis, context, reddit } from '@devvit/web/server';
import {
    getUnlockStatus,
    getAllLevelsUnlockInfo,
    isLevelUnlocked
} from '../core/levelUnlock';
import {
    validateAndCalculateScore,
    LevelCompleteData
} from '../core/scoreCalculator';
import {
    submitScore,
    getTopPlayers,
    getPlayerStanding,
    getPlayerRank
} from '../core/leaderboard';

export const api = new Hono();

// User Identity
api.get('/user/me', async (c) => {
    // Check if context has userId (it usually does in Devvit environment)
    const userId = (context as any).userId;

    if (!userId) {
        // Fallback for unauthenticated/testing
        return c.json({
            userId: 'anonymous',
            username: 'Anonymous',
            avatarUrl: ''
        });
    }

    let avatarUrl = '';
    try {
        const user = await reddit.getUserById(userId);
        avatarUrl = await user?.getSnoovatarUrl() || '';
    } catch (e) {
        console.warn('Failed to fetch snoovatar:', e);
    }

    return c.json({
        userId: userId,
        username: context.username || 'Anonymous',
        avatarUrl: avatarUrl
    });
});

// Unlock System
api.get('/levels/unlock-status', async (c) => {
    // Use redis directly, not context.redis
    const status = await getUnlockStatus(redis);
    return c.json(status);
});

api.get('/levels/all-info', async (c) => {
    const levels = await getAllLevelsUnlockInfo(redis);
    return c.json({ levels });
});

api.get('/levels/:number/unlocked', async (c) => {
    const levelNumber = parseInt(c.req.param('number'));

    if (isNaN(levelNumber)) {
        return c.json({ error: 'Invalid level number' }, 400);
    }

    const isUnlocked = await isLevelUnlocked(redis, levelNumber);
    return c.json({ isUnlocked });
});

// Score Submission
api.post('/score/submit', async (c) => {
    const body = await c.req.json();

    // Validate request body
    const {
        userId,
        username,
        avatarUrl, // New: Avatar URL from client (fetched from /user/me)
        levelNumber,
        alliesSaved,
        timeSpent,
        retryCount
    } = body;

    if (!userId || !username || levelNumber === undefined) {
        return c.json({ error: 'Missing required fields' }, 400);
    }

    // Validate game data & calculate secure score
    const validation = validateAndCalculateScore({
        levelNumber,
        alliesSaved,
        timeSpent,
        retryCount
    });

    if (!validation.isValid) {
        console.warn(`[Score] Rejected submission for user ${userId}:`, validation.errors);
        return c.json({
            success: false,
            errors: validation.errors
        }, 400);
    }

    // Check if level is actually unlocked for this user?
    const isUnlocked = await isLevelUnlocked(redis, levelNumber);
    if (!isUnlocked) {
        return c.json({
            success: false,
            error: 'Level is currently locked'
        }, 403);
    }

    try {
        const result = await submitScore(
            redis,
            userId,
            username,
            avatarUrl || '', // Pass to submitScore
            levelNumber,
            validation.heroPoints,
            alliesSaved,
            timeSpent,
            retryCount
        );

        return c.json({
            success: true,
            heroPoints: validation.heroPoints,
            totalPoints: result.totalPoints,
            rank: result.rank,
        });
    } catch (e) {
        console.error('[Score] Error submitting score:', e);
        return c.json({ success: false, error: 'Internal server error' }, 500);
    }
});

// Leaderboard
api.get('/leaderboard/top', async (c) => {
    const limit = parseInt(c.req.query('limit') || '50');

    const entries = await getTopPlayers(redis, limit);
    return c.json({ entries });
});

api.get('/leaderboard/standing/:userId', async (c) => {
    const userId = c.req.param('userId');

    const standing = await getPlayerStanding(redis, userId);
    if (!standing) {
        return c.json({ found: false });
    }

    return c.json({ found: true, standing });
});
