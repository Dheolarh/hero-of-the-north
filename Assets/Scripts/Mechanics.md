# Hero of the North — Gameplay Mechanics & Trap Guide

This document is a comprehensive handbook for designing, configuring, and wiring up traps, obstacles, and level triggers in *Hero of the North*. 

All interactive mechanics in the game use the **`CollisionsAndTriggers.cs`** script. By changing its Inspector parameters and wiring it to target GameObjects, you can create a wide variety of puzzle and platforming elements.

---

## Part 1: Core Systems Reference

Before building traps, it is helpful to understand the core scripts attached to objects:

### A. The Master Script: `CollisionsAndTriggers.cs`
This script manipulates one or more target GameObjects (`objectsToTrigger`) when a player enters or exits a 2D Collider configured as a **Trigger** (or hits it as a physics collision).

*   **`TriggerType`**: Controls the primary behavior (Teleport, ContinuousMotion, SingleMotion, RotationTrap, Ally, PhysicsModifier).
*   **`ComponentAction`**: Dynamically alters components on targets (`AddRigidbody2D` to drop objects, `AddBoxCollider2D` to block paths, `RemoveCollider` to open paths).
*   **`setObjectActive`**: Toggles target GameObjects on or off (spawns enemies, opens doors).
*   **`deleteTriggerZone`**: Disables the trigger after use so it only fires once.
*   **Audio Triggers**: Plays a sound effect (optionally looping) on activation.

### B. Camera Shake: `CameraShakeTrigger.cs` & `ShakeStopTrigger.cs`
Used to create rumbling, volcanic, or crumbling effects.
*   **`CameraShakeTrigger`** starts the shake with a specified intensity and frequency.
*   It can dynamically spawn or configure a **`ShakeStopTrigger`** on another GameObject. When that specific object hits the stop trigger, the screen stops shaking.

### C. Level Exit: `LevelGoal.cs`
Attached to the level portal or exit crystal. When touched by the player, it triggers level completion and uploads the score.

---

## Part 2: Movement-Based Traps

Movement traps dynamically change position when triggered, requiring the player to react quickly to avoid damage.

### A. The Falling Spikes / Crumbling Ceiling Trap
*   **How It Works:** A spike or stone sprite floats silently on the ceiling. When the player walks underneath, it suddenly gains physics gravity and crashes down.
*   **Step-by-Step Setup:**
    1. Place your spike/boulder GameObject on the ceiling. Give it a `SpriteRenderer` and a `BoxCollider2D` (configured with `Is Trigger = false`). Do **NOT** add a `Rigidbody2D` component in the editor.
    2. Place an empty GameObject directly underneath it at player ground level. Add a `BoxCollider2D` configured with **`Is Trigger = true`**.
    3. Attach `CollisionsAndTriggers` to the trigger zone.
    4. Set **`Component Action`** to `AddRigidbody2D`.
    5. Drag the ceiling spike/boulder into the **`Objects To Trigger`** array.
    6. Check **`Delete Trigger Zone`** (so it only drops once).

### B. The Crushing Walls / Closing Gate Trap
*   **How It Works:** Walking into a corridor causes solid walls on either side to slide inward, attempting to crush the player.
*   **Step-by-Step Setup:**
    1. Create two stone wall GameObjects on either side of a corridor. Give them `BoxCollider2D` components.
    2. Define your target closed positions (e.g., if Left Wall is at X: 5 and Right Wall is at X: 10, the closed position might be X: 7.5).
    3. Create a trigger zone in the center of the corridor. Add a `BoxCollider2D` set to **`Is Trigger = true`**.
    4. Attach `CollisionsAndTriggers` to the trigger zone.
    5. Set **`Trigger Type`** to `SingleMotion` (move once).
    6. Set **`Target Position`** to the coordinates where you want the walls to meet (or create two separate trigger zones, one for each wall).
    7. Set **`Target Move Speed`** (e.g., `8.0` for a fast crush).
    8. Drag the wall GameObjects into the **`Objects To Trigger`** array.

### C. The Moving Platform (Slowing Down / Stopping on Exit)
*   **How It Works:** A platform moves continuously, but only while the player stands on it.
*   **Step-by-Step Setup:**
    1. Create a platform GameObject with a `BoxCollider2D` (for the player to stand on).
    2. Attach a secondary, slightly taller `BoxCollider2D` configured as a **`Trigger`** on top of the platform.
    3. Attach `CollisionsAndTriggers` to this trigger.
    4. Set **`Trigger Type`** to `ContinousMotion`.
    5. Drag the platform itself into the **`Objects To Trigger`** array.
    6. Configure **`Move Direction`** (e.g., `Right`) and **`Move Speed`** (e.g., `3.0`).
    7. Enable **`Stop Move On Exit`** to `true` (so the platform stops sliding when the player jumps off).

---

## Part 3: Stationary & Rotational Hazards

Stationary hazards spin, rotate, or activate static barriers to impede movement or force precise timing.

### A. The Spinning Saw / Circular Spike Trap
*   **How It Works:** A saw blade spins continuously in place or travels along a static track.
*   **Step-by-Step Setup:**
    1. Place a circular saw sprite in the scene. Add a `CircleCollider2D` (for hurting the player).
    2. Attach `CollisionsAndTriggers` directly to the saw GameObject.
    3. Set **`Trigger Type`** to `RotationTrap`.
    4. Drag the saw GameObject into its own **`Objects To Trigger`** array.
    5. Set **`Rotation Direction`** (Clockwise/CounterClockwise) and **`Rotation Speed`** (e.g., `250` degrees per second).
    6. Enable **`Enable Rotation`** to `true` by default (so it spins immediately when the level starts without needing a trigger).

### B. The Hidden Ground Spikes (Toggle Active State)
*   **How It Works:** Spikes hidden in the ground pop up when the player steps on a pressure plate.
*   **Step-by-Step Setup:**
    1. Create a spikes GameObject. Add a collider and set the GameObject to **Inactive** (uncheck active checkbox in Inspector).
    2. Create a pressure plate sprite on the floor. Place a `BoxCollider2D` set to **`Is Trigger = true`** over it.
    3. Attach `CollisionsAndTriggers` to the pressure plate.
    4. Check the **`Set Object Active`** checkbox to `true`.
    5. Drag the hidden spikes GameObject into the **`Objects To Trigger`** array.
    6. *(Optional)* Add a sound effect like `"SpikeSfx"` in **`Audio Clip Name`**.

---

## Part 4: Teleport & Displacement Traps

These traps manipulate coordinates, disorienting the player or forcing them back to the start of a section.

### A. The Portal Maze Trap
*   **How It Works:** Stepping into a glowing portal instantly warps the player to a corresponding landing pad in another room.
*   **Step-by-Step Setup:**
    1. Place a Portal sprite. Add a `BoxCollider2D` set to **`Is Trigger = true`**.
    2. Attach `CollisionsAndTriggers` to the portal.
    3. Set **`Trigger Type`** to `Teleport`.
    4. Drag your **Player** GameObject into the **`Objects To Trigger`** array.
    5. Set **`Teleport Position`** to the destination coordinates in the scene (e.g., `X: -20, Y: 10`).
    6. Set **`Play Audio On Trigger`** to `true` and set **`Audio Clip Name`** to `"Teleport"`.

### B. The Pitfall Redirection (Safe Fall Zone)
*   **How It Works:** Instead of dying when falling into a pit, the player is silently teleported to a checkpoint above.
*   **Step-by-Step Setup:**
    1. Create a wide trigger zone at the bottom of the pit.
    2. Attach `CollisionsAndTriggers` to it.
    3. Set **`Trigger Type`** to `Teleport`.
    4. Drag your **Player** GameObject into **`Objects To Trigger`**.
    5. Set **`Teleport Position`** to a safe platform above the pit.

---

## Part 5: Physics & Gravity Traps

These zones modify physical properties of objects or players, changing acceleration, velocity, or direction of fall.

### A. The Low-Gravity Float Chamber
*   **How It Works:** When the player enters a glowing blue energy chamber, they float upwards or jump much higher.
*   **Step-by-Step Setup:**
    1. Create a chamber GameObject. Add a `BoxCollider2D` set to **`Is Trigger = true`**.
    2. Attach `CollisionsAndTriggers` to the chamber.
    3. Set **`Trigger Type`** to `PhysicsModifier`.
    4. Drag your **Player** GameObject into the **`Object To Modify`** field.
    5. Set **`New Gravity Scale`** to `0.2` (for floating) or `-0.5` (to fall upwards to the ceiling).
    6. Enable **`Reset On Exit`** to `true` (so normal gravity restores immediately when they leave the chamber).

### B. The Slippery Slide Zone (Acceleration Modifier)
*   **How It Works:** Entering an icy slide causes the player to fall and slide much faster.
*   **Step-by-Step Setup:**
    1. Create a trigger zone over an icy slope.
    2. Attach `CollisionsAndTriggers` to the slope.
    3. Set **`Trigger Type`** to `PhysicsModifier`.
    4. Drag your **Player** GameObject into the **`Object To Modify`** field.
    5. Set **`Fall Speed Multiplier`** to `2.5` (accelerates downward movement quickly).
    6. Set **`Reset On Exit`** to `true`.

---

## Part 6: Multi-Object & Sequence Puzzles

Advanced puzzles that require combining multiple triggers, physical objects, and camera effects.

### A. The Earthquake Cave-In Puzzle
*   **How It Works:** Walking into a cavern starts a screen-shaking earthquake. To stop it, the player must push a heavy boulder into a fissure slot.
*   **Step-by-Step Setup:**
    1. **Start Trigger:** Place a trigger zone at the cavern entrance. Add the `CameraShakeTrigger` script. Set `Shake Intensity = 0.4` and `Shake Audio Clip Name = "Earthquake"`.
    2. **The Boulder:** Place a boulder sprite. Add a `CircleCollider2D` and a `Rigidbody2D` so the player can push it.
    3. **Stop Trigger:** Place a trigger zone in the fissure slot on the floor.
    4. **Wire the Start Trigger:**
       * Drag the Fissure (Stop Trigger) GameObject into the **`Stop Shake Trigger`** slot of the `CameraShakeTrigger`.
       * Drag the Boulder GameObject into the **`Object That Stops Shake`** slot.
    5. **Result:** When the player enters the cave, the screen rumbles. Pushing the boulder into the slot stops the shake instantly.

### B. The Double-Lock Door (Switch Sequence)
*   **How It Works:** Two pressure plates must both be pressed to open a locked gateway.
*   **Step-by-Step Setup:**
    1. Create a Gate GameObject. Give it a `BoxCollider2D` (non-trigger) to block the player.
    2. Create Switch A and Switch B GameObjects on the floor. Add trigger colliders to both.
    3. Attach `CollisionsAndTriggers` to Switch A.
       * Set **`Component Action`** to `RemoveCollider`.
       * Drag the Gate GameObject into the **`Objects To Trigger`** array.
       * Do **not** set delete trigger zone (let it run).
    4. Attach `CollisionsAndTriggers` to Switch B.
       * Set **`Component Action`** to `RemoveCollider`.
       * Drag the Gate GameObject into the **`Objects To Trigger`** array.
    5. When the player stands on both switches, the gate colliders are disabled, allowing the player to pass!
