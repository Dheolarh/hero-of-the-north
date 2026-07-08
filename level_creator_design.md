# Designing a Reddit Level Creator & Sharing System

This document outlines the technical architecture, data serialization, and backend integrations required to build a **User-Generated Level Editor & Sharing System** for *Hero of the North* running on Reddit via **Devvit**.

---

## 1. System Architecture Overview

The system consists of three main layers:

```mermaid
graph TD
    A[Unity Level Editor UI] -->|1. Serialize to JSON| B[Unity WebGL Client]
    B -->|2. HTTP POST Level Data| C[Devvit Backend API]
    C -->|3. Save Data| D[(Devvit Redis Store)]
    C -->|4. Create Post| E[Subreddit Custom Post]
    E -->|5. Deep Link Click| B
```

1. **Unity Level Editor (Frontend):** A grid-based tool inside Unity allowing players to paint blocks, drop hazards, place traps, and test playability.
2. **Devvit Backend (Middle-Tier):** Node.js/TypeScript endpoints built on Hono that authenticate creators, store levels, serve level lists, and handle ratings.
3. **Reddit Ecosystem (Social-Tier):** Automatically generating custom posts or comments in a subreddit for shared levels, allowing players to deep-link directly into the level.

---

## 2. Level Data Representation (Serialization)

To send levels over the network, Unity must convert the level layout into a lightweight JSON string.

### A. Level Schema Example (`LevelDataJSON`)
```json
{
  "levelName": "The Boulder Dash",
  "creator": "u/RedditUser",
  "gridWidth": 30,
  "gridHeight": 15,
  "playerPosition": { "x": -27.0, "y": 5.0 },
  "goalPosition": { "x": 15.0, "y": 2.0 },
  "blocks": [
    { "type": "Floor", "x": -27, "y": 4 },
    { "type": "PlatformGround", "x": -10, "y": 6 }
  ],
  "traps": [
    {
      "type": "ContinuousMotion",
      "spawnPos": { "x": -10, "y": 12 },
      "moveDir": "Down",
      "speed": 5.0,
      "delay": 0.5
    }
  ]
}
```

### B. Unity Implementation
Create a serializable scriptable schema in Unity that mirrors the active code design:
```csharp
[System.Serializable]
public struct Vector2S
{
    public float x;
    public float y;

    public Vector2S(Vector2 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
}

[System.Serializable]
public class CustomTileData
{
    public string type;
    public Vector2S position;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
}

[System.Serializable]
public class CustomTrapData
{
    public string type;
    public Vector2S spawnPos;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
    
    // Physics/Trigger settings
    public string moveDir = "Down";
    public float speed = 3f;
    public float delay = 1f;

    // Trigger wiring (connection target coordinates)
    public bool hasTarget = false;
    public Vector2S targetPos;
}

[System.Serializable]
public class CustomLevelData
{
    public string levelName;
    public string creator;
    
    public int gridWidth = 32;
    public int gridHeight = 18;

    public Vector2S playerStartPos;
    public Vector2S goalPos;

    public List<CustomTileData> tiles = new List<CustomTileData>();
    public List<CustomTrapData> traps = new List<CustomTrapData>();

    // Global Player Settings
    public float playerMoveSpeed = 5f;
    public float playerJumpForce = 7f;
    public int playerMaxJumps = 1;
    public bool playerEnableFallDamage = false;
}
```
*Use `JsonUtility.ToJson(customLevelData)` to serialize and `JsonUtility.FromJson<CustomLevelData>(jsonString)` to deserialize.*

---

## 3. Unity In-Game Level Creator UI

A simple, responsive UI for editing levels on screen:

### Key Features
1. **Grid Snapping:** When objects are selected from the palette, snap their coordinates using:
   ```csharp
   Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
   float snappedX = Mathf.Round(mouseWorldPos.x);
   float snappedY = Mathf.Round(mouseWorldPos.y);
   ```
2. **The Palette:** A scrollable panel containing placeable assets:
   * **Blocks:** Floor, Slippery Ice, Floating Platform.
   * **Hazards:** Spikes, Saws.
   * **Traps:** Pressure Plates, Falling Boulders (wired together).
   * **Essentials:** Player Start, Level Portal.
3. **Playtest Validation (The "Mario Maker" Rule):** 
   To prevent impossible levels from polluting the database, the creator **must beat the level** in playtest mode before the "Publish" button becomes active.

---

## 4. Devvit Backend API & Redis Database

Devvit provides a built-in **Redis Client** (`context.redis`) and a **Key-Value Store** (`context.kvStore`) which are perfect for storing and querying JSON-serialized level strings.

### A. API Endpoints (`src/server/routes/api.ts`)
Add three new routes to support custom levels:

1. **`POST /api/levels/publish`**
   * **Action:** Generates a unique Level ID (e.g. UUID). Saves level details to a Redis JSON entry (`level:{id}`). Adds the ID to a sorted list (`levels:all` or `levels:by_rating`).
   * **Reddit Integration:** Triggers Devvit's post creator to automatically publish a thread in your subreddit, sharing the level.
2. **`GET /api/levels/list`**
   * **Action:** Fetches the top/newest level metadata (ID, Creator, Title, Plays, Upvotes) using pagination.
3. **`GET /api/levels/load?id={levelId}`**
   * **Action:** Retrieves the full JSON payload of `level:{id}` to feed back into Unity.
4. **`POST /api/levels/rate`**
   * **Action:** Updates the upvote/downvote ratio on Redis.

### B. Redis Key Design
*   `level:{id}` (Hash or JSON): Stores `levelName`, `creator`, `playsCount`, `upvotes`, and the raw `levelJson` data.
*   `levels:newest` (Sorted Set): Score is the Unix timestamp of creation. Used to fetch new levels.
*   `levels:top` (Sorted Set): Score is the upvote count. Used to fetch popular levels.

---

## 5. Subreddit Integration & Deep-Linking

Sharing and playing custom levels natively inside Reddit is where Devvit shines.

### A. Deep-Linking via URL Parameters
When Devvit renders the game WebGL container, it can pass the **Reddit Post ID** or a **Query Parameter** (e.g., `?levelId=1234`) to the WebGL container via the `webView` config:

1. A Redditor clicks a shared post: `reddit.com/r/north_hero/comments/xyz/play_my_custom_level`.
2. The Devvit WebView launches and extracts `xyz` (the Post ID).
3. The Devvit WebView does an API call: `GET /api/levels/load?postId=xyz`.
4. Devvit sends the level JSON directly to Unity via a message:
   ```javascript
   unityInstance.SendMessage("LevelManager", "LoadCustomLevel", levelJson);
   ```
5. Unity boots directly into custom-play mode for that specific level!

### B. Social Feedback (Ratings & Comments)
*   **Auto-Post Thread:** The thread automatically created on Reddit acts as the level's comment section, allowing players to discuss strategies, leave feedback, and write reviews natively.
*   **Upvote Syncing:** Upvoting the Reddit post can automatically increment the level's score/rating in your database using Devvit's trigger events (`onPostUpvoted`).

## 6. Play Mode Custom Level Loader Integration

When a user launches a shared level, the WebGL container sends the JSON string to Unity via `LevelManager.LoadCustomLevel(string json)`. Instead of loading a static prefab, Unity dynamically reconstructs the level in the Game scene.

### A. Level Loading Flow (Reconstructing from JSON)
1. **Load Gameplay Scene:** `LevelManager` transitions the game to the dedicated `Game` scene.
2. **Deserialize JSON:** We parse the string back into `CustomLevelData`.
3. **Instantiate Terrain & Tiles:** Loops through `data.tiles` and instantiates the matching gameplay prefab from the palette, applying position, scale, and rotation.
4. **Instantiate Traps & Physics Elements:** Loops through `data.traps` and instantiates the trap gameplay prefab. It attaches/configures the `CollisionsAndTriggers` component with:
   - Speed (`speed`), delay (`delay`), target direction (`moveDir`), and target position (`targetPos`).
   - Copy the trigger references and wire connection targets dynamically.
   - Respect custom modifiers like `preserveRelativeDistance` for staggered movement triggers.
5. **Spawn Player & Setup Controller:** Instantiates the player prefab at `data.playerStartPos`. Overrides the `PlayerController` fields with the custom properties defined in the level:
   ```csharp
   var pc = playerInstance.GetComponent<PlayerController>();
   pc.Speed = data.playerMoveSpeed;
   pc.JumpForce = data.playerJumpForce;
   pc.MaxMultiJumps = data.playerMaxJumps;
   pc.EnableFallDamage = data.playerEnableFallDamage;
   ```
6. **Instantiate Goal Portal:** Instantiates the Goal prefab at `data.goalPos`. Touch completion connects to:
   ```csharp
   LevelManager.Instance.CompleteLevel(); // Integrates directly into GameManager/ScoreManager flow
   ```
7. **Configure Custom Camera Settings:** Retrieves or adds a `LevelCameraSettings` component on the spawned level container to override the default follow offsets and orthographic size:
   ```csharp
   var settings = playerInstance.GetComponentInParent<LevelCameraSettings>() ?? playerInstance.gameObject.AddComponent<LevelCameraSettings>();
   settings.offset = new Vector3(data.camOffsetX, data.camOffsetY, settings.offset.z);
   settings.orthoSize = data.camOrthoSize;
   if (!settings.followY) settings.fixedYHeight = data.camOffsetY;
   ```
8. **Refocus Camera:** Wires `CameraFollow` to the player clone:
   ```csharp
   CameraFollow.Instance.SetTarget(playerInstance.transform);
   ```

### B. Core Managers Integration
*   **GameManager:** The global game states (`isGameOver`, `isLevelCompleted`, `isPaused`) work natively as they track the spawned player clone's life cycle. On player death/fall, the retry panel triggers `LevelManager.Instance.RestartLevel()` which clears custom objects and rebuilds them.
*   **AudioManager:** Dynamically spawned objects containing audio triggers (e.g. traps playing SFX on collision) call `AudioManager.Instance.PlaySfx(clipName)` when triggered.
*   **Control System:** The player movement controls and mechanics (double jumping, fall damage calculations) dynamically scale based on the properties read from `CustomLevelData`.

---

## 7. Phase-by-Phase Implementation Plan

> [!TIP]
> Break the implementation into four small milestones to test systems independently.

*   **Phase 1: Local Editor & Testing (Unity Only):** Build the grid Snapping tool, block painting brush, and local JSON export/import tool.
*   **Phase 2: Redis Storage (Devvit Backend):** Implement the `publish` and `load` Hono endpoints to read/write JSON files to Devvit's Redis store.
*   **Phase 3: Level Browser (Unity Game Menu):** Add a scrollable "Redditor Levels" menu in Unity that fetches and displays the paginated list of shared levels.
*   **Phase 4: Reddit Deep-Linking (Devvit):** Set up post-launch handlers so loading a level's Reddit post directly launches that level in Unity.
