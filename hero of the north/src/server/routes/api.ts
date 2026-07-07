import { Hono } from 'hono';
import { redis, context, reddit } from '@devvit/web/server';
import {
    getUnlockStatus,
    getAllLevelsUnlockInfo,
    isLevelUnlocked
} from '../core/levelUnlock';
import {
    validateAndCalculateScore
} from '../core/scoreCalculator';
import {
    submitScore,
    getTopPlayers,
    getPlayerStanding
} from '../core/leaderboard';

export const api = new Hono();

// ========== USER IDENTITY ==========

// GET /api/user/me
// Returns the authenticated Reddit user's identity.
// userId is resolved from the Reddit session server-side — cannot be spoofed.
api.get('/user/me', async (c) => {
    const userId = context.userId;

    if (!userId) {
        return c.json({
            userId: null,
            username: 'Anonymous',
            avatarUrl: ''
        });
    }

    let username = context.username || 'Anonymous';
    let avatarUrl = '';

    try {
        const user = await reddit.getUserById(userId);
        if (user) {
            username  = user.username ?? username;
            avatarUrl = await user.getSnoovatarUrl() ?? '';
        }
    } catch (e) {
        console.warn('[API] Failed to fetch Reddit user details:', e);
    }

    return c.json({ userId, username, avatarUrl });
});

// ========== LEVEL UNLOCK SYSTEM ==========

// GET /api/levels/unlock-status
api.get('/levels/unlock-status', async (c) => {
    const status = await getUnlockStatus(redis);
    return c.json(status);
});

// GET /api/levels/all-info
// Returns unlock status + countdown timers for all 32 levels.
api.get('/levels/all-info', async (c) => {
    const levels = await getAllLevelsUnlockInfo(redis);
    return c.json({ levels });
});

// GET /api/levels/:number/unlocked
api.get('/levels/:number/unlocked', async (c) => {
    const levelNumber = parseInt(c.req.param('number'));

    if (isNaN(levelNumber)) {
        return c.json({ error: 'Invalid level number' }, 400);
    }

    const unlocked = await isLevelUnlocked(redis, levelNumber);
    return c.json({ isUnlocked: unlocked });
});

// ========== SCORE SUBMISSION ==========

// POST /api/score/submit
// Game data is sent from Unity. userId is resolved from the Reddit session — never trusted from body.
api.post('/score/submit', async (c) => {
    // Security: always use server-side userId from the authenticated Reddit session
    const userId = context.userId;

    if (!userId) {
        return c.json({ success: false, error: 'Not authenticated' }, 401);
    }

    const body = await c.req.json();
    const { levelNumber, alliesSaved, timeSpent, retryCount } = body;

    // Tutorial (level 0) doesn't count for the leaderboard
    if (levelNumber === 0) {
        return c.json({ success: true, message: 'Tutorial complete — no score recorded' });
    }

    if (levelNumber === undefined || levelNumber === null) {
        return c.json({ success: false, error: 'Missing levelNumber' }, 400);
    }

    // Server-side score validation and calculation
    const validation = validateAndCalculateScore({ levelNumber, alliesSaved, timeSpent, retryCount });

    if (!validation.isValid) {
        console.warn(`[Score] Rejected submission from ${userId}:`, validation.errors);
        return c.json({ success: false, errors: validation.errors }, 400);
    }

    // Verify the level is actually unlocked globally
    const levelUnlocked = await isLevelUnlocked(redis, levelNumber);
    if (!levelUnlocked) {
        return c.json({ success: false, error: 'Level is currently locked' }, 403);
    }

    // Fetch username and avatar from Reddit (authoritative source)
    let username  = context.username || 'Anonymous';
    let avatarUrl = '';
    try {
        const user = await reddit.getUserById(userId);
        if (user) {
            username  = user.username ?? username;
            avatarUrl = await user.getSnoovatarUrl() ?? '';
        }
    } catch (e) {
        console.warn('[Score] Could not fetch Reddit profile, using defaults:', e);
    }

    try {
        const result = await submitScore(
            redis,
            userId,
            username,
            avatarUrl,
            levelNumber,
            validation.heroPoints,
            alliesSaved,
            timeSpent,
            retryCount
        );

        return c.json({
            success:     true,
            heroPoints:  validation.heroPoints,
            totalPoints: result.totalPoints,
            rank:        result.rank,
        });
    } catch (e) {
        console.error('[Score] Error submitting score:', e);
        return c.json({ success: false, error: 'Internal server error' }, 500);
    }
});

// ========== LEADERBOARD ==========

// GET /api/leaderboard/top?limit=50
api.get('/leaderboard/top', async (c) => {
    const limit = Math.min(parseInt(c.req.query('limit') || '50'), 100);
    const entries = await getTopPlayers(redis, limit);
    return c.json({ entries });
});

// GET /api/leaderboard/standing/me
// Returns the currently authenticated player's rank and points.
api.get('/leaderboard/standing/me', async (c) => {
    const userId = context.userId;

    if (!userId) {
        return c.json({ found: false });
    }

    const standing = await getPlayerStanding(redis, userId);
    if (!standing) {
        return c.json({ found: false });
    }

    return c.json({ found: true, standing });
});

// GET /api/leaderboard/standing/:userId
// Returns a specific player's standing (kept for compatibility / admin use).
api.get('/leaderboard/standing/:userId', async (c) => {
    const targetUserId = c.req.param('userId');
    const standing = await getPlayerStanding(redis, targetUserId);

    if (!standing) {
        return c.json({ found: false });
    }

    return c.json({ found: true, standing });
});

// GET /api/post/info
// Returns current post author (summoner) and active viewer (player) names.
api.get('/post/info', async (c) => {
    const postId = context.postId;
    let summoner = 'Someone';
    
    if (postId) {
        try {
            const post = await reddit.getPostById(postId);
            if (post && post.authorName) {
                summoner = post.authorName;
            }
        } catch (e) {
            console.warn('[API] Could not fetch post details:', e);
        }
    }

    const player = context.username || 'Redditor';
    return c.json({ summoner, player });
});
