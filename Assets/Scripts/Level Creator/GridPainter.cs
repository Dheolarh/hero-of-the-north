using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the level editor workspace: free-form drag placement, selection,
/// double-click properties, drawing wire links, and instantiating playtest entities.
/// </summary>
public class GridPainter : MonoBehaviour
{
    public static GridPainter Instance { get; private set; }

    [System.Serializable]
    public struct PaletteItem
    {
        public string typeName;
        [Tooltip("Visual representation used in Editor mode.")]
        public GameObject editorPrefab;
        [Tooltip("Active gameplay prefab used in Playtest mode.")]
        public GameObject playtestPrefab;
    }

    [Header("Palette Prefabs")]
    [SerializeField] private List<PaletteItem> palette = new List<PaletteItem>();

    [Header("Editor References")]
    [SerializeField] private Camera editorCamera;
    [SerializeField] private Color wireColor = new Color(0f, 0.8f, 1f, 0.7f);

    // ── Placed Objects ────────────────────────────────────────────────────────
    
    // Root level container to organize placed items
    private Transform levelContainer;

    private List<PlacedEditorObject> editorObjects = new List<PlacedEditorObject>();
    
    // Active selection state
    private PlacedEditorObject selectedObject;

    // Drag-and-drop state
    private GameObject activeDragObject;
    private PlacedEditorObject activeDragScript;

    // Linking mode state (wiring triggers to targets)
    private bool isLinkingMode = false;
    private PlacedEditorObject linkingSource;
    private LineRenderer activeLinkingWire;

    // Double-click detection
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    // Playtest tracking
    private List<GameObject> playtestClones = new List<GameObject>();
    private GameObject activePlaytestPlayer;
    private Vector3 originalPlayerStartPos;

    // Camera panning
    private Vector3 lastMousePos;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (editorCamera == null)
            editorCamera = Camera.main;

        // Find or create root level container (flexible lookup for LevelPrefab or Level)
        GameObject containerObj = GameObject.Find("LevelPrefab");
        if (containerObj == null)
            containerObj = GameObject.Find("Level");

        if (containerObj == null)
            containerObj = new GameObject("Level");

        levelContainer = containerObj.transform;
    }

    void Start()
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.OnPlaytestToggled += HandlePlaytestToggle;
            LevelCreatorUI.Instance.OnClearGridRequest += ClearGrid;
            LevelCreatorUI.Instance.OnLoadLevelRequest  += LoadLevel;
        }

        FindExistingSceneObjects();

        // Initially freeze the player's physics simulation while editing
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "PlayerStart"));
        if (playerStart != null)
        {
            var rb = playerStart.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
            }
        }

        // Snap camera to the PlayerStart settings immediately on startup
        SnapCameraToPlayerStart();
    }

    void Update()
    {
        if (LevelCreatorUI.Instance == null) return;

        if (LevelCreatorUI.Instance.IsPlaytesting) return;

        // Camera Panning (Right Mouse Button drag)
        HandleCameraPanning();

        // Focus camera back on Player Spawn point when pressing F
        if (Input.GetKeyDown(KeyCode.F))
        {
            SnapCameraToPlayerStart();
        }

        // Handle Select and Double-click interactions
        HandleClickSelection();

        // Handle Linking Mode wire update
        HandleLinkingWireUpdate();
    }

    // ── Scenery Inspection (Default scene components) ────────────────────────

    private void FindExistingSceneObjects()
    {
        // Scan the scene for any pre-placed objects under the Level container (including inactive ones)
        if (levelContainer != null)
        {
            var components = levelContainer.GetComponentsInChildren<PlacedEditorObject>(true);
            foreach (var comp in components)
            {
                if (!editorObjects.Contains(comp))
                    editorObjects.Add(comp);
            }
            Debug.Log($"[GridPainter] Scanned scene and registered {components.Length} existing components under '{levelContainer.name}'.");
        }
    }

    // ── Camera Panning ───────────────────────────────────────────────────────

    private void HandleCameraPanning()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePos = Input.mousePosition;

            // Stop camera follow so user can pan manual workspace freely
            var camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.StopFollowing();
            }
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = editorCamera.ScreenToViewportPoint(lastMousePos - Input.mousePosition);
            Vector3 move = new Vector3(delta.x * editorCamera.orthographicSize * 2f * editorCamera.aspect, delta.y * editorCamera.orthographicSize * 2f, 0f);
            editorCamera.transform.position += move;
            lastMousePos = Input.mousePosition;
        }
    }

    // ── Selection & Double-Click ─────────────────────────────────────────────

    private void HandleClickSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Prevent clicks when clicking UI buttons
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                PlacedEditorObject hitObj = hit.collider.GetComponentInParent<PlacedEditorObject>();
                if (hitObj != null)
                {
                    // Detect double click
                    float timeSinceLastClick = Time.time - lastClickTime;
                    if (timeSinceLastClick <= doubleClickThreshold && selectedObject == hitObj)
                    {
                        OpenProperties(hitObj);
                    }
                    else
                    {
                        SelectObject(hitObj);
                    }
                    lastClickTime = Time.time;
                    return;
                }
            }

            // Clicked empty space — clear selection (unless in linking mode)
            if (!isLinkingMode)
            {
                ClearSelection();
            }
        }
    }

    private void SelectObject(PlacedEditorObject obj)
    {
        ClearSelection();
        selectedObject = obj;

        // Highlight selected object in scene (simple color swap or wireframe shader placeholder)
        var sprite = selectedObject.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = new Color(0.7f, 0.9f, 1f, 1f); // light blue tint
        }

        // Notify properties UI
        if (ObjectPropertiesPanel.Instance != null)
        {
            ObjectPropertiesPanel.Instance.ShowProperties(selectedObject);
        }
    }

    private void ClearSelection()
    {
        if (selectedObject != null)
        {
            var sprite = selectedObject.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = Color.white;
            }
        }
        selectedObject = null;

        if (ObjectPropertiesPanel.Instance != null)
        {
            ObjectPropertiesPanel.Instance.HideProperties();
        }
    }

    private void OpenProperties(PlacedEditorObject obj)
    {
        SelectObject(obj);
        if (ObjectPropertiesPanel.Instance != null)
        {
            ObjectPropertiesPanel.Instance.OpenPanel();
        }
    }

    // ── Drag-and-Drop Placement API ──────────────────────────────────────────

    public void StartDragPlacement(string typeName)
    {
        CancelLinkingMode();
        ClearSelection();

        PaletteItem item = GetPaletteItem(typeName);
        if (item.editorPrefab != null)
        {
            // Spawn the editor visual representation
            activeDragObject = Instantiate(item.editorPrefab, Vector3.zero, Quaternion.identity, levelContainer);
            activeDragScript = activeDragObject.GetComponent<PlacedEditorObject>();
            if (activeDragScript == null)
            {
                activeDragScript = activeDragObject.AddComponent<PlacedEditorObject>();
            }

            activeDragScript.assetTypeName = typeName;

            // Set semi-transparent color for visual dragging feedback
            var sprite = activeDragObject.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = new Color(1f, 1f, 1f, 0.6f);
            }

            // Disable colliders so it doesn't block mouse raycasts while dragging
            var colliders = activeDragObject.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders) col.enabled = false;
        }
    }

    public void UpdateDragPlacement(Vector3 worldPos)
    {
        if (activeDragObject == null) return;

        // Optional Snapping (e.g. snaps to 0.5 units for cleaner placement grids)
        float snappedX = Mathf.Round(worldPos.x * 2f) / 2f;
        float snappedY = Mathf.Round(worldPos.y * 2f) / 2f;

        activeDragObject.transform.position = new Vector3(snappedX, snappedY, 0f);
    }

    public void EndDragPlacement(Vector3 worldPos)
    {
        if (activeDragObject == null) return;

        // Final snap placement
        UpdateDragPlacement(worldPos);

        // Restore full opacity
        var sprite = activeDragObject.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = Color.white;
        }

        // Re-enable colliders
        var colliders = activeDragObject.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = true;

        // Add to active placed list
        editorObjects.Add(activeDragScript);

        SelectObject(activeDragScript);

        activeDragObject = null;
        activeDragScript = null;
    }

    // ── Trigger Wiring / Linking Mode ────────────────────────────────────────

    private void HandleLinkingWireUpdate()
    {
        if (isLinkingMode && activeLinkingWire != null)
        {
            Vector3 mouseWorldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            activeLinkingWire.SetPosition(1, mouseWorldPos);

            // Cancel wiring on Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelLinkingMode();
            }
        }
    }

    public void StartLinkingMode(PlacedEditorObject source)
    {
        if (source == null) return;

        isLinkingMode = true;
        linkingSource = source;

        activeLinkingWire = CreateWireRenderer();
        activeLinkingWire.SetPosition(0, source.transform.position);
    }

    private void CreateLink(PlacedEditorObject source, PlacedEditorObject target)
    {
        RemoveLink(source);

        source.hasTarget = true;
        source.targetObject = target;

        // Draw visual connecting line
        source.wireLine = CreateWireRenderer();
        source.wireLine.SetPosition(0, source.transform.position);
        source.wireLine.SetPosition(1, target.transform.position);

        Debug.Log($"[GridPainter] Wired {source.name} to target {target.name}");
    }

    public void RemoveLink(PlacedEditorObject source)
    {
        source.hasTarget = false;
        source.targetObject = null;
        if (source.wireLine != null)
        {
            Destroy(source.wireLine.gameObject);
            source.wireLine = null;
        }
    }

    public void CancelLinkingMode()
    {
        isLinkingMode = false;
        linkingSource = null;
        if (activeLinkingWire != null)
        {
            Destroy(activeLinkingWire.gameObject);
            activeLinkingWire = null;
        }
    }

    private void HandleLinkingSelection(PlacedEditorObject target)
    {
        if (isLinkingMode && linkingSource != null && target != linkingSource)
        {
            CreateLink(linkingSource, target);
        }
        CancelLinkingMode();
    }

    // Called when clicking or editing fields in wiring
    public void RequestTriggerWiringLink()
    {
        if (selectedObject != null)
        {
            StartLinkingMode(selectedObject);
        }
    }

    // ── Delete Placed Object ─────────────────────────────────────────────────

    public void DeleteSelectedObject()
    {
        if (selectedObject == null) return;

        // Remove active wires linked to this or from this
        RemoveLink(selectedObject);

        // Search for any other triggers that target this object
        foreach (var obj in editorObjects)
        {
            if (obj.targetObject == selectedObject)
            {
                RemoveLink(obj);
            }
        }

        editorObjects.Remove(selectedObject);
        Destroy(selectedObject.gameObject);
        selectedObject = null;

        ClearSelection();
    }

    // ── Edit vs Playtest Transition (Level Building) ───────────────────────

    private void HandlePlaytestToggle(bool isPlaytesting)
    {
        if (isPlaytesting)
        {
            BuildPlaytestLevel();
        }
        else
        {
            ClearPlaytestLevel();
        }
    }

    private void BuildPlaytestLevel()
    {
        ClearSelection();

        // 1. Hide all editor placeholders
        ToggleEditorVisibility(false);

        playtestClones.Clear();
        Dictionary<PlacedEditorObject, GameObject> playtestPairs = new Dictionary<PlacedEditorObject, GameObject>();

        // 2. Instantiate and transform all active elements
        foreach (var editorObj in editorObjects)
        {
            if (editorObj == null) continue;

            PaletteItem item = GetPaletteItem(editorObj.assetTypeName);
            if (item.playtestPrefab != null)
            {
                GameObject clone = Instantiate(
                    item.playtestPrefab, 
                    editorObj.transform.position, 
                    editorObj.transform.rotation
                );

                // Copy editor scale
                clone.transform.localScale = editorObj.transform.localScale;

                playtestClones.Add(clone);
                playtestPairs[editorObj] = clone;

                // Configure properties
                ConfigureSpawnedTrap(clone, editorObj);
            }
        }

        // 3. Configure player start & Goal Portal (if placed in editor)
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "PlayerStart"));
        if (playerStart != null)
        {
            originalPlayerStartPos = playerStart.transform.position;
            activePlaytestPlayer = playerStart.gameObject;

            // Enable physics and simulation during playtest
            var rb = activePlaytestPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
            }

            var camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.SetTarget(activePlaytestPlayer.transform);
        }
        else
        {
            Debug.LogWarning("[GridPainter] Playtest Player is missing! Please make sure a PlayerStart object is present under LevelPrefab.");
        }

        PlacedEditorObject goalObj = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "Goal"));
        Vector3 goalPos = goalObj != null ? goalObj.transform.position : Vector3.zero;
        PaletteItem goalItem = GetPaletteItem("Goal");
        if (goalItem.playtestPrefab != null)
        {
            GameObject portal = Instantiate(goalItem.playtestPrefab, goalPos, Quaternion.identity);
            playtestClones.Add(portal);

            // goal completes test
            portal.AddComponent<PlaytestGoalValidator>();
        }
        else
        {
            Debug.LogWarning("[GridPainter] Playtest Goal portal prefab is null! Make sure 'Goal' has a playtestPrefab assigned in the GridPainter inspector palette.");
        }

        // 4. Wire trigger-to-target links dynamically
        foreach (var editorObj in editorObjects)
        {
            if (editorObj.hasTarget && editorObj.targetObject != null)
            {
                if (playtestPairs.ContainsKey(editorObj) && playtestPairs.ContainsKey(editorObj.targetObject))
                {
                    GameObject triggerClone = playtestPairs[editorObj];
                    GameObject targetClone = playtestPairs[editorObj.targetObject];

                    CollisionsAndTriggers triggerScript = triggerClone.GetComponent<CollisionsAndTriggers>();
                    if (triggerScript != null)
                    {
                        triggerScript.objectsToTrigger = new GameObject[] { targetClone };
                        Debug.Log($"[GridPainter] Wired playtest triggers: {triggerClone.name} -> {targetClone.name}");
                    }
                }
            }
        }
    }

    private void ClearPlaytestLevel()
    {
        foreach (var clone in playtestClones)
        {
            if (clone != null) Destroy(clone);
        }
        playtestClones.Clear();

        // Disable physics simulation and reset position of the scene player
        if (activePlaytestPlayer != null)
        {
            var rb = activePlaytestPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            activePlaytestPlayer.transform.position = originalPlayerStartPos;
            activePlaytestPlayer.transform.rotation = Quaternion.identity;
        }

        activePlaytestPlayer = null;

        // Snap camera back to editor player spawn and follow settings
        SnapCameraToPlayerStart();

        ToggleEditorVisibility(true);
    }

    /// <summary>
    /// Snaps the Main Camera to focus on the Player Start marker position,
    /// and configures CameraFollow to apply its LevelCameraSettings bounds and size.
    /// </summary>
    public void SnapCameraToPlayerStart()
    {
        var camFollow = Camera.main.GetComponent<CameraFollow>();
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "PlayerStart"));

        if (camFollow != null && playerStart != null)
        {
            camFollow.SetTarget(playerStart.transform);
            camFollow.StartFollowing();
            Debug.Log("[GridPainter] Snapped camera view to PlayerStart marker.");
        }
    }

    private void ToggleEditorVisibility(bool visible)
    {
        foreach (var obj in editorObjects)
        {
            if (obj != null)
            {
                // NEVER hide the player start GameObject during playtest!
                if (!visible && MatchAssetType(obj.assetTypeName, "PlayerStart"))
                {
                    continue;
                }

                obj.gameObject.SetActive(visible);
                if (obj.wireLine != null) obj.wireLine.gameObject.SetActive(visible);
            }
        }
    }

    private void ConfigureSpawnedTrap(GameObject spawnedObj, PlacedEditorObject editorObj)
    {
        PingPongMovement pingPong = spawnedObj.GetComponent<PingPongMovement>();
        if (pingPong != null)
        {
            pingPong.speed = editorObj.speed;
            pingPong.movementDirection = editorObj.moveDir == "Up" || editorObj.moveDir == "Down"
                ? PingPongDirection.Vertical
                : PingPongDirection.Horizontal;

            // Compute dynamic offsets based on editor delay property
            pingPong.maxLeftOffset = editorObj.delay * 3f;
            pingPong.maxRightOffset = editorObj.delay * 3f;
        }

        ProjectileSpawner spawner = spawnedObj.GetComponent<ProjectileSpawner>();
        if (spawner != null)
        {
            spawner.moveSpeed = editorObj.speed;
            spawner.spawnInterval = editorObj.delay;
        }
    }

    // ── Save & Load Layout Logic ─────────────────────────────────────────────

    private void ClearGrid()
    {
        ClearSelection();
        CancelLinkingMode();

        foreach (var obj in editorObjects)
        {
            if (obj != null)
            {
                if (obj.wireLine != null) Destroy(obj.wireLine.gameObject);
                Destroy(obj.gameObject);
            }
        }
        editorObjects.Clear();
    }

    private void LoadLevel(CustomLevelData data)
    {
        ClearGrid();

        // 1. Load Player Spawn & Goal
        PaletteItem startItem = GetPaletteItem("PlayerStart");
        if (startItem.editorPrefab != null)
        {
            GameObject flag = Instantiate(startItem.editorPrefab, data.playerStartPos.ToVector2(), Quaternion.identity, levelContainer);
            var flagScript = flag.GetComponent<PlacedEditorObject>() ?? flag.AddComponent<PlacedEditorObject>();
            flagScript.assetTypeName = "PlayerStart";
            editorObjects.Add(flagScript);
        }

        PaletteItem goalItem = GetPaletteItem("Goal");
        if (goalItem.editorPrefab != null)
        {
            GameObject portal = Instantiate(goalItem.editorPrefab, data.goalPos.ToVector2(), Quaternion.identity, levelContainer);
            var portalScript = portal.GetComponent<PlacedEditorObject>() ?? portal.AddComponent<PlacedEditorObject>();
            portalScript.assetTypeName = "Goal";
            editorObjects.Add(portalScript);
        }

        Dictionary<Vector2, PlacedEditorObject> loadedObjects = new Dictionary<Vector2, PlacedEditorObject>();

        // 2. Load static tiles
        foreach (var tile in data.tiles)
        {
            PaletteItem item = GetPaletteItem(tile.type);
            if (item.editorPrefab != null)
            {
                Vector2 pos = tile.position.ToVector2();
                GameObject clone = Instantiate(item.editorPrefab, pos, Quaternion.Euler(0f, 0f, tile.rotation), levelContainer);
                clone.transform.localScale = tile.scale.ToVector2();

                var script = clone.GetComponent<PlacedEditorObject>() ?? clone.AddComponent<PlacedEditorObject>();
                script.assetTypeName = tile.type;
                editorObjects.Add(script);
                loadedObjects[pos] = script;
            }
        }

        // 3. Load traps
        foreach (var trap in data.traps)
        {
            PaletteItem item = GetPaletteItem(trap.type);
            if (item.editorPrefab != null)
            {
                Vector2 pos = trap.spawnPos.ToVector2();
                GameObject clone = Instantiate(item.editorPrefab, pos, Quaternion.Euler(0f, 0f, trap.rotation), levelContainer);
                clone.transform.localScale = trap.scale.ToVector2();

                var script = clone.GetComponent<PlacedEditorObject>() ?? clone.AddComponent<PlacedEditorObject>();
                script.assetTypeName = trap.type;
                script.moveDir = trap.moveDir;
                script.speed = trap.speed;
                script.delay = trap.delay;
                script.hasTarget = trap.hasTarget;

                editorObjects.Add(script);
                loadedObjects[pos] = script;
            }
        }

        // 4. Restore wires
        foreach (var trap in data.traps)
        {
            if (trap.hasTarget)
            {
                Vector2 sourcePos = trap.spawnPos.ToVector2();
                Vector2 targetPos = trap.targetPos.ToVector2();

                if (loadedObjects.ContainsKey(sourcePos) && loadedObjects.ContainsKey(targetPos))
                {
                    CreateLink(loadedObjects[sourcePos], loadedObjects[targetPos]);
                }
            }
        }

        Debug.Log($"[GridPainter] Loaded free layout successfully. Objects: {editorObjects.Count}");
    }

    // ── Prefab Registry Helpers ──────────────────────────────────────────────

    private PaletteItem GetPaletteItem(string name)
    {
        foreach (var item in palette)
        {
            if (MatchAssetType(item.typeName, name)) return item;
        }
        return new PaletteItem();
    }

    public CustomLevelData ExportLevelData(string levelName, string creatorName)
    {
        CustomLevelData levelData = new CustomLevelData
        {
            levelName = levelName,
            creator = creatorName
        };

        // Find PlayerStart and Goal
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "PlayerStart"));
        if (playerStart != null) levelData.playerStartPos = new Vector2S(playerStart.transform.position);

        PlacedEditorObject goalObj = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "Goal"));
        if (goalObj != null) levelData.goalPos = new Vector2S(goalObj.transform.position);

        // Export all other placed tiles and traps
        foreach (var obj in editorObjects)
        {
            if (obj == null || MatchAssetType(obj.assetTypeName, "PlayerStart") || MatchAssetType(obj.assetTypeName, "Goal")) continue;

            if (IsTrapAsset(obj.assetTypeName))
            {
                levelData.traps.Add(obj.ToTrapData());
            }
            else
            {
                levelData.tiles.Add(obj.ToTileData());
            }
        }

        return levelData;
    }

    private bool IsTrapAsset(string name)
    {
        return name == "MovingPlatform" || name == "ProjectileSpawner" || name == "TriggerZone" || name == "CameraShake";
    }

    private bool MatchAssetType(string assetName, string targetType)
    {
        if (string.IsNullOrEmpty(assetName)) return false;
        string cleanAsset = assetName.Replace(" ", "").ToLower();
        string cleanTarget = targetType.Replace(" ", "").ToLower();
        return cleanAsset == cleanTarget;
    }

    private LineRenderer CreateWireRenderer()
    {
        GameObject go = new GameObject("Wire");
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = wireColor;
        lr.endColor = wireColor;
        lr.sortingOrder = 10;
        return lr;
    }
}

/// <summary>
/// Helper script added to the spawned playtest Goal Portal.
/// Beats the level and notifies the editor upon contact.
/// </summary>
public class PlaytestGoalValidator : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (LevelCreatorUI.Instance != null)
            {
                LevelCreatorUI.Instance.ValidatePlaytestSuccess();
            }
        }
    }
}
