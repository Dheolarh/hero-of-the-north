import express from "express";
import {
  InitResponse,
  LevelCompletedRequest,
  LevelCompletedResponse,
  ScoreSubmissionRequest,
  ScoreSubmissionResponse,
  LeaderboardResponse,
  PlayerStandingResponse,
} from "../shared/types/api";
import {
  createServer,
  context,
  getServerPort,
  reddit,
  redis,
} from "@devvit/web/server";
import { createPost } from "./core/post";
import { validateLevelStats, calculateHeroPoints } from "./services/scoreService";
import {
  getTop50,
  getPlayerStanding,
  savePlayerStats,
  saveLevelScore,
  updateLeaderboard,
  getCompletedLevels,
  addCompletedLevel,
} from "./services/leaderboardService";

const app = express();

// Middleware for JSON body parsing
app.use(express.json());
// Middleware for URL-encoded body parsing
app.use(express.urlencoded({ extended: true }));
// Middleware for plain text body parsing
app.use(express.text());

const router = express.Router();

// Example to show how to send initial data to the Unity Game
router.get<
  { postId: string },
  InitResponse | { status: string; message: string }
>("/api/init", async (_req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("API Init Error: postId not found in devvit context");
    res.status(400).json({
      status: "error",
      message: "postId is required but missing from context",
    });
    return;
  }

  try {
    const username = await reddit.getCurrentUsername();
    const currentUsername = username ?? "anonymous";

    // Fetch user info for snoovatar
    let snoovatarUrl = "";
    if (username && context.userId) {
      const user = await reddit.getUserById(context.userId);
      if (user) {
        snoovatarUrl = (await user.getSnoovatarUrl()) ?? "";
      }
    }

    // Fetch previous time from Redis using postId:username as key
    const redisKey = `${postId}:${currentUsername}`;
    const previousTime = await redis.get(redisKey);

    res.json({
      type: "init",
      postId: postId,
      username: currentUsername,
      snoovatarUrl: snoovatarUrl,
      previousTime: previousTime ?? "",
    });
  } catch (error) {
    console.error(`API Init Error for post ${postId}:`, error);
    let errorMessage = "Unknown error during initialization";
    if (error instanceof Error) {
      errorMessage = `Initialization failed: ${error.message}`;
    }
    res.status(400).json({ status: "error", message: errorMessage });
  }
});

router.post<
  unknown,
  LevelCompletedResponse | { status: string; message: string },
  LevelCompletedRequest
>("/api/level-completed", async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("No postId in context");
    res.status(400).json({
      status: "error",
      message: "postId is required",
    });
    return;
  }

  try {
    const { username, time } = req.body;

    if (!username || !time) {
      console.error("Missing username or time in request");
      res.status(400).json({
        status: "error",
        message: "username and time are required",
      });
      return;
    }

    // Store the completion time in Redis with key format: postId:username
    const redisKey = `${postId}:${username}`;
    await redis.set(redisKey, time);

    res.json({
      type: "level-completed",
      success: true,
      message: "Time saved successfully",
    });
  } catch (error) {
    console.error(`API Level Completed Error for post ${postId}:`, error);
    let errorMessage = "Unknown error saving completion time";
    if (error instanceof Error) {
      errorMessage = `Failed to save time: ${error.message}`;
    }
    res.status(500).json({
      type: "level-completed",
      success: false,
      message: errorMessage,
    });
  }
});

// ===== LEADERBOARD ENDPOINTS =====

// Submit score for a completed level
router.post<
  unknown,
  ScoreSubmissionResponse | { status: string; message: string },
  ScoreSubmissionRequest
>("/api/submit-score", async (req, res): Promise<void> => {
  const { userId } = context;

  if (!userId) {
    res.status(401).json({
      status: "error",
      message: "User not authenticated",
    });
    return;
  }

  try {
    const { levelNumber, alliesSaved, timeSpent, retryCount } = req.body;

    // Validate input
    const validation = validateLevelStats(levelNumber, {
      alliesSaved,
      timeSpent,
      retryCount,
    });

    if (!validation.isValid) {
      res.status(400).json({
        status: "error",
        message: validation.error || "Invalid stats",
      });
      return;
    }

    // Calculate hero points SERVER-SIDE (never trust client)
    const heroPoints = calculateHeroPoints({
      alliesSaved,
      timeSpent,
      retryCount,
    });

    // Get user info
    const username = (await reddit.getCurrentUsername()) ?? "anonymous";
    let avatarUrl = "";
    if (userId) {
      const user = await reddit.getUserById(userId);
      if (user) {
        avatarUrl = (await user.getSnoovatarUrl()) ?? "";
      }
    }

    // Check if level was already completed (prevent score overwriting on replay)
    const existingLevelData = await redis.hGetAll(`player:${userId}:level:${levelNumber}`);

    if (existingLevelData && existingLevelData.heroPoints) {
      // Level already completed - don't overwrite, just return existing data
      const existingPoints = parseInt(existingLevelData.heroPoints);

      // Get current standing
      const standing = await getPlayerStanding(redis, userId);

      res.json({
        success: true,
        heroPoints: existingPoints,
        totalPoints: standing.totalPoints,
        rank: standing.rank,
        message: "Level already completed - score not updated",
      });
      return;
    }

    // Save level score (first completion only)
    await saveLevelScore(
      redis,
      userId,
      levelNumber,
      alliesSaved,
      timeSpent,
      retryCount,
      heroPoints
    );

    // Add to completed levels list
    await addCompletedLevel(redis, userId, levelNumber);

    // Get all completed levels and calculate total points
    const completedLevels = await getCompletedLevels(redis, userId);
    let totalPoints = 0;

    for (const level of completedLevels) {
      const levelData = await redis.hGetAll(`player:${userId}:level:${level}`);
      if (levelData?.heroPoints) {
        totalPoints += parseInt(levelData.heroPoints);
      }
    }

    // Update player stats
    await savePlayerStats(
      redis,
      userId,
      username,
      avatarUrl,
      totalPoints,
      completedLevels.length
    );

    // Update leaderboard
    await updateLeaderboard(redis, userId, totalPoints);

    // Get new rank
    const standing = await getPlayerStanding(redis, userId);

    res.json({
      success: true,
      heroPoints,
      totalPoints,
      rank: standing.rank,
      message: "Score submitted successfully",
    });
  } catch (error) {
    console.error("Error submitting score:", error);
    res.status(500).json({
      status: "error",
      message: "Failed to submit score",
    });
  }
});

// Get top 50 leaderboard
router.get<unknown, LeaderboardResponse | { status: string; message: string }>(
  "/api/leaderboard",
  async (_req, res): Promise<void> => {
    try {
      const entries = await getTop50(redis);

      res.json({
        entries,
      });
    } catch (error) {
      console.error("Error fetching leaderboard:", error);
      res.status(500).json({
        status: "error",
        message: "Failed to fetch leaderboard",
      });
    }
  }
);

// Get player's standing
router.get<
  unknown,
  PlayerStandingResponse | { status: string; message: string }
>("/api/player-standing", async (_req, res): Promise<void> => {
  const { userId } = context;

  if (!userId) {
    res.status(401).json({
      status: "error",
      message: "User not authenticated",
    });
    return;
  }

  try {
    const standing = await getPlayerStanding(redis, userId);

    res.json(standing);
  } catch (error) {
    console.error("Error fetching player standing:", error);
    res.status(500).json({
      status: "error",
      message: "Failed to fetch player standing",
    });
  }
});

router.post('/internal/on-app-install', async (_req, res): Promise<void> => {
  try {
    const post = await createPost();

    res.json({
      status: 'success',
      message: `Post created in subreddit ${context.subredditName} with id ${post.id}`,
    });
  } catch (error) {
    console.error(`Error creating post: ${error}`);
    res.status(400).json({
      status: 'error',
      message: 'Failed to create post',
    });
  }
});

router.post('/internal/menu/post-create', async (_req, res): Promise<void> => {
  try {
    const post = await createPost();
    post

    res.json({
      navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
    });
  } catch (error) {
    console.error(`Error creating post: ${error}`);
    res.status(400).json({
      status: 'error',
      message: 'Failed to create post',
    });
  }
});

app.use(router);

const server = createServer(app);
server.on("error", (err) => console.error(`server error; ${err.stack}`));
server.listen(getServerPort());
