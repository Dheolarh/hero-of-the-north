

**2. The jslib communication approach is not supported**

The `.jslib` + `window.sendToDevvit()` + `SendMessage()` pattern you're using is **not documented or supported** on Devvit Web. The official template uses **`UnityWebRequest`** (plain HTTP) for all Unity↔Server communication:

```csharp
UnityWebRequest request = UnityWebRequest.Get("/api/init");
yield return request.SendWebRequest();
```

Your server routes (`/api/leaderboard/top`, `/api/score/submit`, etc.) are already set up as HTTP endpoints, you just need to call them directly from Unity with `UnityWebRequest` instead of going through the jslib bridge.

**What to change:**
- Delete both `.jslib` files
- Rewrite `DevvitBridge.cs` to use `UnityWebRequest`, see the official version here: <https://github.com/reddit/devvit-unity-project/blob/main/Assets/Scripts/DevvitBridge.cs>
- Remove the `sendToDevvit` message routing in `script.ts` (Unity handles its own HTTP calls now)

**References:**
- Official Unity template: <https://github.com/reddit/devvit-unity-project>
- Code-side fixes I made: <https://github.com/fattenedbricks/bricks-fixed>
