# 3: Level Creator Editor Setup Guide

Follow these steps in your Unity Editor to set up the **User-Generated Level Creator**:

---

### Step A: Create the Level Creator Scene
1. Create a new scene in your project named **`LevelCreator`**.
2. Select your **Main Camera** in the hierarchy and ensure it has:
   *   **`CameraFollow`** script attached.
   *   **`CameraShake`** script attached (required for the test compiler to find).

---

### Step B: Set Up the Canvas & `LevelCreatorUI`
1. Right-click in the Hierarchy and create a **UI Canvas**.
2. Select the Canvas and click **Add Component → `LevelCreatorUI`**.
3. In the Inspector, drag your UI panels and text elements to the fields:
   *   **Editor UI Root:** Drag the Canvas itself (or the root Panel holding all editor UI buttons).
   *   **Terrain Palette Panel:** Drag the panel that holds your ground/platform placement buttons.
   *   **Hazards Palette Panel:** Drag the panel that holds spikes, saws, etc.
   *   **Essentials Palette Panel:** Drag the panel that holds Spawn, Goal, and Portal buttons.
   *   **Playtest Button:** Drag your Playtest button here.
   *   **Publish Button:** Drag your Publish button here.
   *   **Selected Tool Text:** (Optional) Drag a TextMeshPro component showing the active tool.
   *   **Level Name Input Field:** Drag your TMP Input Field component here.

#### Hooking up UI Button On Click() Events:
For the buttons on your Canvas, drag the Canvas into the **On Click()** event list and select:
*   **Playtest Button:** `LevelCreatorUI.TogglePlaytest`
*   **Save Button:** `LevelCreatorUI.SaveLevelDraft`
*   **Load Button:** `LevelCreatorUI.LoadLevelDraft`
*   **Clear Button:** `LevelCreatorUI.RequestClearGrid`
*   **Publish Button:** `LevelCreatorUI.PublishLevel`
*   **Eraser Tool Button:** `LevelCreatorUI.ToggleEraser`
*   **Tab Buttons (Terrain, Hazards, Essentials):** `LevelCreatorUI.ShowTerrainPalette` (etc.)

#### Hooking up Palette Item Selection Buttons:
For each block button in your palette (e.g. "Ice Platform" button):
1. In the **On Click()** event, drag the Canvas in.
2. Select **`LevelCreatorUI.SelectAsset`**.
3. In the text parameter field below the function name, type the asset ID (e.g. `Floor`, `SpikesMetal`, `MovingPlatform`). **This name must match the registry name in Step C.**

---

### Step C: Set Up the `GridPainter` Workspace
1. Create an empty GameObject in the hierarchy named **`GridPainter`**.
2. Click **Add Component → `GridPainter`**.
3. In the Inspector:
   *   **Editor Camera:** Drag your **Main Camera** here.
   *   **Grid Line Prefab:** (Optional) A prefab with a LineRenderer used to draw grid bounds.
   *   **Palette:** Click `+` to add entries. For each entry, specify:

| Type Name (Must match Step B name) | Editor Prefab (Lightweight visual placeholder, no physics) | Playtest Prefab (Active gameplay prefab, with colliders/tags) |
|---|---|---|
| **PlayerStart** | A lightweight flag sprite | Your actual **Player** prefab (with controls & rigidbodies) |
| **Goal** | A lightweight portal portal sprite | Your actual **Goal Portal** prefab (with trigger completed script) |
| **Floor** | Floor tile sprite (no collider) | Your actual **Floor** prefab (with solid BoxCollider2D) |
| **SpikesMetal** | Spike tile sprite (no collider) | Your actual **Spike** prefab (with hazard tag/script) |
| **MovingPlatform** | Platform sprite (no collider) | Your actual **PingPong platform** prefab |
| **TriggerZone** | Transparent box outline | Your actual **Trigger Zone** prefab (with `CollisionsAndTriggers`) |

---

### Step D: Testing
1. Press **Play** in Unity.
2. Choose **Floor** or **PlayerStart** and click/drag on the grid workspace to paint.
3. Click **Playtest** — the editor UI will disappear, the player will spawn, and you can play.
4. Reach the portal, and the **Publish** button will light up!
