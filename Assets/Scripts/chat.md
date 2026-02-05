# Uploading Unity WebGL Game to Reddit

---
**Saved:** 02/02/2026, 18:35:03  
**Source:** [Gemini Chat](https://gemini.google.com/app/5a91bcc3c7d249b9)  
---

## You:

So I created A unity game template on developer.reddit.com  I'm building a unity webgl game currently   how do I upload the game I built as the app

---

## Gemini:

To upload your Unity WebGL game to the Reddit Developer Platform (Devvit), you essentially wrap your Unity build within a Reddit app template. Since you’re building for WebGL, you'll need to follow a specific "hybrid" build process to ensure all the files load correctly within the Reddit environment.

### 1. Prepare your Unity Build

Reddit’s platform requires specific files from your WebGL export. You actually need to build **twice** with different settings to get the correct combination of files:


- **Build 1 (Compressed):** 1.  Go to **File > Build Profiles** and switch to **Web**.
2.  Open **Player Settings > Publishing Settings**.
3.  Set **Compression Format** to **Gzip** and ensure **Decompression Fallback** is **checked**.
4.  Build the project. From this build, you only need:
* `[ProjectName].data.unityweb`
* `[ProjectName].wasm.unityweb`


- **Build 2 (Uncompressed):**


1. Go back to **Publishing Settings** and set **Compression Format** to **Disabled**.


1. Build again. From this build, you only need:


1. `[ProjectName].framework.js`


1. `[ProjectName].loader.js`








---

### 2. Set Up the Reddit App Template

If you haven't already, you need to initialize the Reddit Unity template on your machine using terminal/command prompt (requires Node.js 22.2.0+):


1. **Create the project:**

Bash
```
npm create devvit@latest -- --template=unity

```


1. **Navigate and Install:**

Bash
```
cd your-app-name
npm install

```



### 3. Replace the Build Files

Now, you need to "inject" your Unity game into the Reddit app folder structure:


1. Navigate to `src/client/public/Build` in your Reddit project folder.


1. **Delete** the sample files currently in there.


1. **Paste** the 4 files you gathered in Step 1 into this folder.


1. **Important:** If your Unity project name isn't "SampleGame", open `src/client/script.ts` and update the file paths (usually lines 29-34) to match your actual filenames.




---

### 4. Upload and Test

Once your files are in place, use the Devvit CLI to push the game to Reddit:

CommandAction`npm run dev`Creates a **private playtest** on a dev subreddit so you can test the game live.`npm run deploy`Uploads the current version of your app to Reddit's servers.`npm run launch`Submits your app for **review** so it can be installed on public subreddits.Export to Sheets
---

### Key Technical Constraints


- **Size Limit:** Currently, there is a **50MB - 100MB** limit for the bundled data and wasm files. If your game is larger, you may need to optimize textures or strip unused code.


- **Webview Environment:** Your game runs in a webview inside the Reddit app/site. Ensure you use the `DevvitBridge.cs` (included in the template) if you need the game to "talk" to Reddit (e.g., for high scores or usernames).



Since you're a cybersecurity student, you might find it interesting that Reddit reviews all apps before they can be "launched" to larger subreddits to ensure they don't violate safety policies or perform unauthorized actions.

Would you like me to explain how to use the **DevvitBridge** to sync your Unity game's high scores with a Reddit leaderboard?

---

## You:

But now I have to create a leaderboard for my game  which  means   1. I have to be able to get redditor username and profile picture  2. store it which leaderboard stats  3. Different leaderboard per game level  leaderboard shows redditor profile image, username, time to complete level, number of level retries, how many ally saved

---

## Gemini:

Building a leaderboard for a Reddit Unity game involves a "handshake" between your Unity C# code (the **Client**) and the Reddit Devvit TypeScript code (the **Server**).

Since you're a cybersecurity student, you’ll appreciate that this structure keeps sensitive game logic and persistent data (like the database) on the server, while the client only handles the display and gameplay.


---

### 1. Get Username and Profile Picture

You don't need to ask the user for these; the Reddit platform already knows who they are. You just need to pass that info from the Reddit environment into your Unity game using the **Devvit Bridge**.

**In your Reddit App (`src/main.ts` or similar):**
When the game starts, you can fetch the user details from the context and send them to Unity.

TypeScript
```javascript
// Inside your Devvit app component
const user = await ctx.reddit.getCurrentUser();
const username = user?.username ?? "Guest";
const avatarUrl = await user?.getSnoovatarUrl() ?? "";

// Send this data to your Unity game
// 'DevvitBridge' is the name of the GameObject in Unity, 'OnReceiveUserData' is the C# function
webView.postMessage({
  type: 'USER_DATA',
  data: { username, avatarUrl }
});

```

**In your Unity Script (`DevvitBridge.cs`):**
Create a method to catch this data.

C#
```json
public void OnReceiveUserData(string json) {
    var data = JsonUtility.FromJson<UserData>(json);
    Debug.Log("Playing as: " + data.username);
    // Use UnityWebRequest to download the avatarUrl image for the profile picture
}

```


---

### 2. Store Leaderboard Stats (C# to TypeScript)

When a level is completed, you’ll "post" the stats (time, retries, allies saved) back to Reddit.

**In Unity:**

C#
```
public void SubmitScore(int level, float time, int retries, int allies) {
    var stats = new {
        level = level,
        time = time,
        retries = retries,
        allies = allies
    };
    // This sends the data out to the Reddit wrapper
    Application.ExternalCall("window.parent.postMessage", JsonUtility.ToJson(stats), "*");
}

```


---

### 3. Different Leaderboard per Level (Redis)

Reddit provides a **Redis** instance for every app. To keep levels separate, you’ll use level-specific keys (e.g., `leaderboard:level1`).

**In your Reddit App Backend:**
You will use `ctx.redis.zAdd` to store scores. Redis "Sorted Sets" are perfect for leaderboards because they automatically sort by the "score" (in your case, the completion time).

TypeScript
```javascript
// When the server receives the message from Unity
async function saveScore(userId, username, avatarUrl, level, time, retries, allies) {
  const lbKey = `leaderboard:level:${level}`;
  
  // 1. Store the player's detailed stats in a hash
  await ctx.redis.hSet(`stats:${userId}:lvl${level}`, {
    username,
    avatarUrl,
    time: time.toString(),
    retries: retries.toString(),
    allies: allies.toString()
  });

  // 2. Add to the level's sorted set (using 'time' as the sorting score)
  // Note: Redis sorts ascending by default, which is great for "fastest time"
  await ctx.redis.zAdd(lbKey, { member: userId, score: time });
}

```


---

### Summary Table: How the Data Flows

RequirementHow it's handled**User Identity**Fetched via `ctx.reddit.getCurrentUser()` on the Reddit side.**Profile Image**Use `user.getSnoovatarUrl()` and display in Unity via `UnityWebRequest`.**Persistence**Data is saved in **Redis** (Hash for details, Sorted Set for ranking).**Level Separation**Use dynamic keys in Redis like `leaderboard:level:${id}`.**Data Integrity**Validate stats on the server side to prevent cheaters from injecting fake times.Export to Sheets### Security Note for You

As a cybersecurity student, keep in mind that `postMessage` can be intercepted or spoofed if not handled carefully. Always validate the origin of the message on the server side and consider adding a simple "integrity check" or "checksum" to your score payloads to ensure they haven't been tampered with in transit.

Would you like me to show you the **Unity C# code** for downloading and displaying that Reddit Snoovatar (profile image) on a UI Sprite?

---

## You:

So how is my database going to be designed if I want to store this informations in the leaderboard   points  of player   but points is gotten by number of ally saved + time to complete level divided by (number of times level was retried x 0.6)

---

## Gemini:

Since you are building on the Reddit Developer Platform, you will be using **Redis** as your database. Unlike traditional SQL databases with tables and rows, Redis is a key-value store, so we have to be strategic about how we structure your "keys" to handle both the ranking (math) and the metadata (profile info).

Here is the design for your leaderboard database.


---

### 1. The Scoring Formula

Based on your logic, the **Points** calculation happens on the server (the Devvit side) before saving to the database to prevent players from "hacking" their own score in the WebGL client.

Using your formula:


Points=Allies Saved+(Retries×0.6)Time to Complete​*Note: As a developer, you might want to add a small constant to the denominator (e.g., 0.6×Retries+1) to avoid a "division by zero" error if a player has 0 retries.*


---

### 2. The Database Schema (Redis Structure)

To meet your requirements, we will use two different Redis data types for each level:

#### **A. The Leaderboard (Sorted Set)**


- **Key Name:** `lb:level:{level_id}`


- **Purpose:** This automatically sorts players by their calculated "Points."


- **Structure:**


- **Member:** The `redditor_id` (Unique ID).


- **Score:** The calculated `Points`.





#### **B. The Player Profile (Hash)**


- **Key Name:** `player:{level_id}:{redditor_id}`


- **Purpose:** Stores the "extra" info that doesn't affect the ranking but needs to be displayed (username, avatar, etc.).


- **Structure:**



FieldValue Example`username`"CyberStudent_2026"`avatar_url`"[https://www.redditstatic.com/avatar/](https://www.google.com/search?q=https://www.redditstatic.com/avatar/)..."`time`"124.5"`retries`"3"`allies`"12"
---

### 3. How the Data Flow Works

When a player finishes a level, your app should perform these three steps in the backend:


1. **Calculate:** Run the formula to get the final `Points`.


1. **Save Stats:** Use `ctx.redis.hSet` to save the username, avatar, and raw stats into the **Hash**.


1. **Update Rank:** Use `ctx.redis.zAdd` to add the player to the **Sorted Set** using their `Points` as the score.




---

### 4. Fetching the Top 10

When you want to show the leaderboard in Unity, you do the reverse:


1. **Get IDs:** Call `ctx.redis.zRange` on the Sorted Set to get the top 10 `redditor_ids`.


1. **Get Details:** For each ID found, call `ctx.redis.hGetAll` to grab their username, avatar, and specific stats (allies, retries).


1. **Send to Unity:** Package all that into a JSON array and send it to your Unity WebGL game.



### Technical Tip: Atomicity

Since you're studying cybersecurity, you might be familiar with **Race Conditions**. To ensure the Hash and the Sorted Set are updated together without errors, you can wrap these calls in a Redis **Multi/Transaction** block. This ensures that a player's score is never updated without their profile data also being updated.

Would you like the **TypeScript code** for the `saveScore` function that implements this specific formula and Redis logic?

---

## You:

You know that it’s a single leaderboard but per level points accumulate

---

## Gemini:

Since the points accumulate across levels, your database design needs to handle **incremental updates**. Instead of overwriting a score, we will use Redis to "add" the points from a newly completed level to the player's existing total.

Here is how you should structure the database to support a single global leaderboard that tracks cumulative stats and points.


---

### 1. The Cumulative Scoring Logic

When a player finishes a level, you calculate the points for **that specific level** using your formula:

LevelPoints=Allies Saved+(Retries×0.6+0.001)Time to Complete​*(I added 0.001 to prevent a division-by-zero error if a player has 0 retries).*

In Redis, you won't use `zAdd` (which sets a value). You will use **`zIncrBy`**, which takes the existing score and adds the new points to it.


---

### 2. The Redis Schema

You will need one **Sorted Set** for the rankings and one **Hash** per player to track their cumulative "career" stats (total allies saved, total retries, etc.).

#### **A. The Global Leaderboard (Sorted Set)**


- **Key:** `global_leaderboard`


- **Member:** `redditor_id`


- **Score:** `total_accumulated_points`



#### **B. Player Career Stats (Hash)**


- **Key:** `player_stats:{redditor_id}`


- **Fields:**



FieldDescription`username`The player's current Reddit username.`avatar_url`URL to their Reddit Snoovatar.`total_allies`Sum of all allies saved across all levels.`total_retries`Sum of all retries across all levels.`total_time`Sum of all completion times.`levels_completed`A counter of how many levels they have finished.Export to Sheets
---

### 3. The Backend Update Process

When the Unity game sends a "Level Complete" message to the Reddit app, the server performs these three operations:


1. **Update Ranking:** Use `ctx.redis.zIncrBy('global_leaderboard', levelPoints, userId)` to add the new points to their total score.


1. **Update Totals:** Use `ctx.redis.hIncrBy` for integers (allies, retries) and `ctx.redis.hIncrByFloat` for the time. This keeps a running tally of their career performance.


1. **Refresh Profile:** Update the `username` and `avatar_url` in the hash in case the user changed their Reddit profile recently.




---

### 4. Fetching the Data for Unity

When Unity asks for the "Top 10," the server does the following:


1. Fetch the top 10 IDs and their scores from `global_leaderboard`.


1. For each ID, fetch the career stats from `player_stats:{id}`.


1. Send a combined JSON list back to Unity.



### Security Consideration: Validation

Since you are accumulating points, a single "cheated" level submission could ruin a player's total score forever. In your backend TypeScript code, you should add a **sanity check**. For example, if a player claims they saved 1,000 allies in a level where only 10 exist, the server should reject the update. This "Server-Side Validation" is a core principle of secure game design.

Would you like the **TypeScript code** for the `zIncrBy` logic to handle this accumulation on the Devvit server?

---

## You:

So know let me understand how everything will work how player picture and Id and username are gotten and does reddit even provide them   then how are points being calculated and stored per level, how the database is been setup and which information is been added to it and how they are added, and which tables do we need

---

## Gemini:

Since you’re building this on the Reddit Developer Platform (Devvit), the architecture is quite elegant. Reddit provides the user identity and the database (Redis) directly to your app.

Here is exactly how the data moves, how the points are calculated, and how your database "tables" (Redis Keys) look.


---

### 1. How Identity is Gotten

Reddit provides a built-in API that identifies who is currently looking at your game. You don't need a login system.


- **Who provides it:** The `context.reddit` object in your Devvit backend.


- **The Code:**

TypeScript
```javascript
const user = await context.reddit.getCurrentUser();
const userId = user.id; // Unique ID (e.g., "t2_abc123")
const username = user.username; // e.g., "CyberStudent_2026"
const avatarUrl = await user.getSnoovatarUrl(); // Link to their Reddit avatar

```


- **How Unity gets it:** You send this from your TypeScript backend to Unity using the `postMessage` bridge we discussed.




---

### 2. The Database Design (Redis "Tables")

In Redis, we don't use SQL tables. We use **Keys**. Think of these as two distinct spreadsheets:

#### **A. The Ranking "Table" (Sorted Set)**

This key stores only the **UserID** and the **Total Points**. Redis automatically keeps it sorted from highest to lowest.


- **Key Name:** `global_leaderboard`


- **Structure:**


- `t2_abc123` → `542.5` (Points)


- `t2_xyz789` → `410.2` (Points)





#### **B. The Player Detail "Table" (Hash)**

Since the Sorted Set only stores IDs and Scores, we use a **Hash** to store the extra info (username, picture, etc.).


- **Key Name:** `player_info:{userId}`


- **Fields stored inside:**


- `username`: "CyberStudent_2026"


- `avatar_url`: "https://..."


- `total_allies`: 45


- `total_retries`: 12


- `total_time`: 600.5






---

### 3. Point Calculation & Storage Logic

When a player finishes a level, Unity sends the raw data (Time, Retries, Allies) to the backend. **Always calculate points on the server** so players can't easily cheat by sending a fake score.

**The Formula in TypeScript:**

TypeScript
```javascript
const levelPoints = alliesSaved + (completionTime / (retries * 0.6 + 0.1)); 
// Adding 0.1 prevents division by zero if retries are 0

```

**The Storage Process (Atomic Update):**


1. **Add Points:** Use `context.redis.zIncrBy('global_leaderboard', levelPoints, userId)`. This adds the new level points to their existing total score.


1. **Update Stats:** Use `context.redis.hIncrBy` to add the new allies and retries to their career totals in the Hash.


1. **Update Profile:** Overwrite the `username` and `avatar_url` in the Hash to make sure you always have their latest profile info.




---

### 4. Summary of Information Flow

StepComponentAction**1. Play**UnityPlayer finishes level; Unity sends {LevelID, Time, Allies, Retries} to Reddit.**2. Auth**Reddit AppBackend calls `getCurrentUser()` to verify who sent the data.**3. Math**Reddit AppBackend calculates `Points` based on your formula.**4. Save**Redis`zIncrBy` updates the total score; `hIncrBy` updates cumulative stats.**5. Show**UnityUnity asks for Leaderboard; Backend fetches Top 10 from Redis and sends to Unity.Export to Sheets### Cybersecurity Student Perspective: The "Trust No One" Rule

As you're learning about security, remember: **The Client (Unity) is untrusted.** * **Never** let Unity tell the server "I got 500 points."


- **Always** have Unity tell the server "I finished in 30 seconds with 2 retries," and let the server verify if those numbers are even possible for that level before adding them to the database.



Would you like the **exact TypeScript function** to retrieve the Top 10 players and their stats from Redis to send back to Unity?

