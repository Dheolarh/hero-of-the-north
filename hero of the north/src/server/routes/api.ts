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
    getPlayerStanding,
    clearPlayerScores
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

// ========== COMMUNITY LEVEL SYSTEM ==========

// POST /api/levels/publish
api.post('/levels/publish', async (c) => {
    const userId = context.userId;
    let username = context.username || 'Anonymous';
    let avatarUrl = '';

    if (userId) {
        try {
            const user = await reddit.getUserById(userId);
            if (user) {
                username = user.username ?? username;
                avatarUrl = await user.getSnoovatarUrl() ?? '';
            }
        } catch (e) {
            console.warn('[API] Failed to fetch Reddit user details for publishing:', e);
        }
    }

    const body = await c.req.json();
    const { levelName } = body;

    if (!levelName) {
        return c.json({ success: false, error: 'Missing levelName' }, 400);
    }

    // Generate a unique level ID using get/set counter
    const idStr = await redis.get('community:level:counter');
    const idNum = idStr ? parseInt(idStr) + 1 : 1;
    await redis.set('community:level:counter', idNum.toString());
    const levelId = `lvl_${idNum}`;

    const newLevel = {
        id: levelId,
        levelName,
        creator: username,
        levelData: JSON.stringify(body),
        playCount: 0,
        topPlayer: '',
        avatarUrl
    };

    // Store level payload in a Hash
    await redis.hSet('community:levels', {
        [levelId]: JSON.stringify(newLevel)
    });

    // Append level ID to list stored as a JSON array in a string key
    const listStr = await redis.get('community:levels:list');
    const levelIds: string[] = listStr ? JSON.parse(listStr) : [];
    levelIds.push(levelId);
    await redis.set('community:levels:list', JSON.stringify(levelIds));

    console.log(`[API] Successfully published community level ${levelId} (${levelName}) by ${username}`);
    return c.json({ success: true, levelId });
});

// GET /api/levels/community
api.get('/levels/community', async (c) => {
    try {
        const listStr = await redis.get('community:levels:list');
        const levelIds: string[] = listStr ? JSON.parse(listStr) : [];
        if (levelIds.length === 0) {
            return c.json({ levels: [] });
        }

        const levels: any[] = [];
        for (const levelId of levelIds) {
            const dataStr = await redis.hGet('community:levels', levelId);
            if (dataStr) {
                try {
                    levels.push(JSON.parse(dataStr));
                } catch (err) {
                    console.error('[API] Error parsing community level data:', err);
                }
            }
        }

        return c.json({ levels });
    } catch (e) {
        console.error('[API] Error fetching community levels:', e);
        return c.json({ levels: [] });
    }
});

// POST /api/levels/:id/play
api.post('/levels/:id/play', async (c) => {
    const levelId = c.req.param('id');
    try {
        const dataStr = await redis.hGet('community:levels', levelId);
        if (!dataStr) {
            return c.json({ success: false, error: 'Level not found' }, 404);
        }

        const level = JSON.parse(dataStr);
        level.playCount = (level.playCount || 0) + 1;

        await redis.hSet('community:levels', {
            [levelId]: JSON.stringify(level)
        });
        return c.json({ success: true, playCount: level.playCount });
    } catch (e) {
        console.error('[API] Error incrementing play count:', e);
        return c.json({ success: false }, 500);
    }
});

// POST /api/levels/community/score
api.post('/levels/community/score', async (c) => {
    let username = context.username || 'Anonymous';
    
    // Resolve clean username
    if (username.startsWith('u/')) {
        username = username.substring(2);
    }

    const body = await c.req.json();
    const { levelId, alliesSaved, timeSpent, retryCount } = body;

    if (!levelId) {
        return c.json({ success: false, error: 'Missing levelId' }, 400);
    }

    try {
        const dataStr = await redis.hGet('community:levels', levelId);
        if (!dataStr) {
            return c.json({ success: false, error: 'Level not found' }, 404);
        }

        const level = JSON.parse(dataStr);

        // Score logic: allies saved (100 pts each) + speed bonus (max 1000 pts) - retries penalty (50 pts each)
        const heroPoints = (alliesSaved * 100) + (1000 - Math.min(timeSpent, 900)) - (retryCount * 50);

        const currentTopScore = level.topScore || 0;

        if (heroPoints > currentTopScore || !level.topPlayer) {
            level.topScore = heroPoints;
            level.topPlayer = username;

            await redis.hSet('community:levels', {
                [levelId]: JSON.stringify(level)
            });
            console.log(`[API] New high score for ${levelId}: ${heroPoints} by ${username}`);
            return c.json({ success: true, newHighScore: true, topPlayer: username, topScore: heroPoints });
        }

        return c.json({ success: true, newHighScore: false, topPlayer: level.topPlayer, topScore: currentTopScore });
    } catch (e) {
        console.error('[API] Error submitting community score:', e);
        return c.json({ success: false }, 500);
    }
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

// GET /api/leaderboard/clear-test
// Wipes scores and stats for specific test players to reset the board.
api.get('/leaderboard/clear-test', async (c) => {
    const targets = ['LATTER_PRES', 'AUTOMATIC-FE'];
    const cleared = await clearPlayerScores(redis, targets);
    return c.json({ success: true, cleared });
});
