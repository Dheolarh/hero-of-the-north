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
    public static bool suppressNamePromptOnce = false;

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

    // Drag-and-drop selectable state
    private GameObject activeDraggedSelectable = null;
    private Vector3 dragOffset;

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

        // Editor Camera Zoom (Mouse scroll and touch pinch)
        HandleCameraZoom();

        // Focus camera back on Player Spawn point when pressing F
        if (Input.GetKeyDown(KeyCode.F))
        {
            SnapCameraToPlayerStart();
        }

        // Handle Select and Double-click interactions
        HandleClickSelection();

        // Handle dragging selectable objects on screen
        HandleSelectableDragging();

        // Handle Linking Mode wire update
        HandleLinkingWireUpdate();
    }

    // ── Scenery Inspection (Default scene components) ────────────────────────

    private void FindExistingSceneObjects()
    {
        editorObjects.Clear();

        // Scan the entire active scene for Selectable or Player tagged objects
        var selectables = GameObject.FindGameObjectsWithTag("Selectable");
        var players = GameObject.FindGameObjectsWithTag("Player");

        List<GameObject> allTargets = new List<GameObject>();
        allTargets.AddRange(selectables);
        allTargets.AddRange(players);

        foreach (GameObject go in allTargets)
        {
            if (go == null || go.name.Contains("Wire") || go.GetComponent<LineRenderer>() != null)
                continue;

            // Fix degenerate Z-scale (Z scale = 0 breaks Unity 2D physics colliders)
            if (go.transform.localScale.z == 0f)
            {
                Vector3 localScale = go.transform.localScale;
                localScale.z = 1f;
                go.transform.localScale = localScale;
                Debug.Log($"[GridPainter] Fixed degenerate Z-scale on '{go.name}' (forced to 1f for physics support).");
            }

            // Disable physics simulation during editing so rigidbodies don't fall off their parent wrappers
            var rb = go.GetComponentInChildren<Rigidbody2D>(true);
            if (rb != null)
            {
                rb.simulated = false;
            }

            // Make sure they have PlacedEditorObject
            var placedObj = go.GetComponent<PlacedEditorObject>();
            if (placedObj == null)
            {
                placedObj = go.AddComponent<PlacedEditorObject>();
            }

            // Map assetTypeName correctly
            string rawName = go.name;
            if (string.IsNullOrEmpty(placedObj.assetTypeName))
            {
                if (rawName.Contains("Floor") || rawName.Contains("Ground")) placedObj.assetTypeName = "Floor";
                else if (rawName.Contains("Ice")) placedObj.assetTypeName = "PlatformIce";
                else if (rawName.Contains("Platform")) placedObj.assetTypeName = "MovingPlatform";
                else if (rawName.Contains("Spike")) placedObj.assetTypeName = "SpikesMetal";
                else if (rawName.Contains("Spawn") || rawName.Contains("PlayerStart") || rawName.Contains("Player")) placedObj.assetTypeName = "PlayerStart";
                else if (rawName.Contains("Goal") || rawName.Contains("Portal")) placedObj.assetTypeName = "Goal";
                else placedObj.assetTypeName = rawName.Replace("(Clone)", "").Trim();
            }

            // Add collider if missing
            var col = go.GetComponent<Collider2D>();
            if (col == null)
            {
                var newCol = go.AddComponent<BoxCollider2D>();
                newCol.isTrigger = true; // Set as trigger so it does not physically collide with the player
            }

            // Register in the list
            if (!editorObjects.Contains(placedObj))
            {
                editorObjects.Add(placedObj);
            }
        }
        Debug.Log($"[GridPainter] Scanned scene and registered {editorObjects.Count} editor objects.");
    }

    // ── Camera Panning & Zoom ────────────────────────────────────────────────

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
            ClampCameraPosition();
            lastMousePos = Input.mousePosition;
        }
    }

    private void HandleCameraZoom()
    {
        // Ignore zoom if the cursor is hovering over a UI element (e.g., scrolling a panel)
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        float currentSize = editorCamera.orthographicSize;

        // 1. Mouse Scroll Wheel Zoom (Desktop)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.005f)
        {
            currentSize -= scroll * 8f; // Scroll speed scaling factor
        }

        // 2. Touch Screen Pinch-to-Zoom (Mobile WebGL)
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find position in previous frame
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find previous and current touch distance magnitudes
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Difference in distance
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // Adjust size (0.01f touch scaling factor)
            currentSize -= deltaMagnitudeDiff * 0.02f;
        }

        // Clamp editing zoom between orthographic size 2 and 15
        editorCamera.orthographicSize = Mathf.Clamp(currentSize, 2f, 15f);
        
        // Always clamp position after zoom to prevent view exceeding boundaries
        ClampCameraPosition();
    }

    /// <summary>
    /// Clamps the editor camera's position dynamically based on its current orthographic size (zoom),
    /// ensuring that the visible screen boundaries never exceed X [-7, 50] and Y [-25, 25].
    /// </summary>
    private void ClampCameraPosition()
    {
        float halfHeight = editorCamera.orthographicSize;
        float halfWidth = halfHeight * editorCamera.aspect;

        float minXBound = -7f;
        float maxXBound = 50f;
        float minYBound = -25f;
        float maxYBound = 25f;

        Vector3 pos = editorCamera.transform.position;

        // If the viewport fits within bounds, clamp it. Otherwise, center it on that axis.
        if ((maxXBound - minXBound) > (2f * halfWidth))
        {
            pos.x = Mathf.Clamp(pos.x, minXBound + halfWidth, maxXBound - halfWidth);
        }
        else
        {
            pos.x = (minXBound + maxXBound) * 0.5f;
        }

        if ((maxYBound - minYBound) > (2f * halfHeight))
        {
            pos.y = Mathf.Clamp(pos.y, minYBound + halfHeight, maxYBound - halfHeight);
        }
        else
        {
            pos.y = (minYBound + maxYBound) * 0.5f;
        }

        editorCamera.transform.position = pos;
    }

    // ── Selection & Double-Click ─────────────────────────────────────────────

    private GameObject GetSelectableTarget(GameObject hitGo)
    {
        if (hitGo == null) return null;

        PlacedEditorObject peo = hitGo.GetComponentInParent<PlacedEditorObject>();
        if (peo == null) return null;

        Transform curr = peo.transform;
        while (curr != null)
        {
            if (curr.CompareTag("Selectable") || curr.CompareTag("Player"))
            {
                return peo.gameObject;
            }
            curr = curr.parent;
        }
        return null;
    }

    private void HandleClickSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Prevent clicks when clicking UI buttons
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[GridPainter] Selection ignored: Clicked on a UI element.");
                return;
            }

            Vector3 mouseWorldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f; // Force Z coordinates to 0 for 2D calculations
            Debug.Log($"[GridPainter] Click at Screen {Input.mousePosition} -> World {mouseWorldPos}");

            bool oldHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            Collider2D hitCol = Physics2D.OverlapPoint(mouseWorldPos);
            Physics2D.queriesHitTriggers = oldHitTriggers;

            if (hitCol != null)
            {
                Debug.Log($"[GridPainter] Physics overlap HIT: '{hitCol.name}', Tag: '{hitCol.tag}'");
                GameObject selectableGo = GetSelectableTarget(hitCol.gameObject);
                if (selectableGo != null)
                {
                    PlacedEditorObject hitObj = selectableGo.GetComponent<PlacedEditorObject>();
                    if (hitObj != null)
                    {
                        // Check if Eraser tool is active in LevelCreatorUI
                        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsEraserActive)
                        {
                            Debug.Log($"[GridPainter] Eraser active: erasing '{selectableGo.name}'");
                            DeleteObject(hitObj);
                            return; // Return immediately, do not select!
                        }

                        Debug.Log($"[GridPainter] PlacedEditorObject target identified: '{selectableGo.name}' (Parent of '{hitCol.name}')");
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
                else
                {
                    Debug.Log($"[GridPainter] Hit object '{hitCol.name}' does not resolve to a Selectable/Player parent. Ignoring click.");
                }
            }
            else
            {
                Debug.Log("[GridPainter] Click hit NOTHING in 2D space.");
            }

            // Clicked empty space — clear selection (unless in linking mode)
            if (!isLinkingMode)
            {
                ClearSelection();
            }
        }
    }

    private void HandleSelectableDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            bool oldHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            Collider2D hitCol = Physics2D.OverlapPoint(mouseWorldPos);
            Physics2D.queriesHitTriggers = oldHitTriggers;

            if (hitCol != null)
            {
                GameObject selectableGo = GetSelectableTarget(hitCol.gameObject);
                if (selectableGo != null)
                {
                    activeDraggedSelectable = selectableGo;
                    dragOffset = selectableGo.transform.position - mouseWorldPos;
                    dragOffset.z = 0f;
                    Debug.Log($"[GridPainter] Started dragging parent object: '{activeDraggedSelectable.name}'");
                }
            }
        }

        if (Input.GetMouseButton(0) && activeDraggedSelectable != null)
        {
            Vector3 mouseWorldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = mouseWorldPos + dragOffset;
            newPos.z = 0f;

            // Clamp coordinates to stay within level bounds
            newPos.x = Mathf.Clamp(newPos.x, -7f, 50f);
            newPos.y = Mathf.Clamp(newPos.y, -25f, 25f);

            activeDraggedSelectable.transform.position = newPos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            activeDraggedSelectable = null;
        }
    }

    private void SelectObject(PlacedEditorObject obj)
    {
        ClearSelection();
        selectedObject = obj;

        // Highlight sprite renderers in the selected hierarchy
        var sprites = selectedObject.GetComponentsInChildren<SpriteRenderer>(true);
        
        // Detect if there are child sprite renderers under this object
        bool hasChildSprites = false;
        foreach (var sprite in sprites)
        {
            if (sprite.gameObject != selectedObject.gameObject)
            {
                hasChildSprites = true;
                break;
            }
        }

        foreach (var sprite in sprites)
        {
            // Skip highlighting the parent container if child sprites are present
            if (sprite.gameObject == selectedObject.gameObject && hasChildSprites)
            {
                continue;
            }
            sprite.color = new Color(0.7f, 0.9f, 1f, 1f); // light blue tint
        }

        // Notify properties UI
        if (ObjectPropertiesPanel.Instance != null)
        {
            ObjectPropertiesPanel.Instance.ShowProperties(selectedObject);
        }

        // Refresh active tool text display
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.UpdateToolText();
        }
    }

    public PlacedEditorObject GetSelectedObject()
    {
        return selectedObject;
    }

    public List<PlacedEditorObject> GetPlacedObjects()
    {
        return editorObjects;
    }

    private void ClearSelection()
    {
        if (selectedObject != null)
        {
            var sprites = selectedObject.GetComponentsInChildren<SpriteRenderer>(true);
            
            bool hasChildSprites = false;
            foreach (var sprite in sprites)
            {
                if (sprite.gameObject != selectedObject.gameObject)
                {
                    hasChildSprites = true;
                    break;
                }
            }

            foreach (var sprite in sprites)
            {
                if (sprite.gameObject == selectedObject.gameObject && hasChildSprites)
                {
                    continue;
                }
                sprite.color = Color.white;
            }
        }
        selectedObject = null;

        if (ObjectPropertiesPanel.Instance != null)
        {
            ObjectPropertiesPanel.Instance.HideProperties();
        }

        // Refresh active tool text display
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.UpdateToolText();
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

    public void SpawnAssetAtCenter(string typeName)
    {
        CancelLinkingMode();
        ClearSelection();

        // Hero/PlayerStart is a single-instance object — block duplicates
        if (IsPlayerAsset(typeName))
        {
            bool alreadyExists = editorObjects.Exists(o => o != null && IsPlayerAsset(o.assetTypeName));
            if (alreadyExists)
            {
                Debug.LogWarning($"[GridPainter] Tried to spawn '{typeName}' but a Hero already exists in the scene.");
                var panel = ValidatorPanelController.Instance;
                if (panel != null) panel.ShowSingleInstanceWarning("Hero");
                return;
            }
        }

        PaletteItem item = GetPaletteItem(typeName);
        if (item.editorPrefab != null)
        {
            // Calculate center of current camera view snapped to 0.5 units
            Vector3 camCenter = editorCamera.transform.position;
            float snappedX = Mathf.Round(camCenter.x * 2f) / 2f;
            float snappedY = Mathf.Round(camCenter.y * 2f) / 2f;
            Vector3 spawnPos = new Vector3(snappedX, snappedY, 0f);

            // Instantiate
            GameObject spawned = Instantiate(item.editorPrefab, spawnPos, Quaternion.identity, levelContainer);
            spawned.name = item.editorPrefab.name;
            
            // Setup PlacedEditorObject
            var placedObj = spawned.GetComponent<PlacedEditorObject>();
            if (placedObj == null)
            {
                placedObj = spawned.AddComponent<PlacedEditorObject>();
            }
            placedObj.assetTypeName = typeName;

            // Prevent degenerate Z-scale
            if (spawned.transform.localScale.z == 0f)
            {
                Vector3 localScale = spawned.transform.localScale;
                localScale.z = 1f;
                spawned.transform.localScale = localScale;
            }

            // Ensure collider exists for clicking/dragging
            var col = spawned.GetComponent<Collider2D>();
            if (col == null)
            {
                var newCol = spawned.AddComponent<BoxCollider2D>();
                newCol.isTrigger = true;
            }

            // Ensure Rigidbody2D is not simulated during editing
            var rb = spawned.GetComponentInChildren<Rigidbody2D>(true);
            if (rb != null)
            {
                rb.simulated = false;
            }

            // Register
            if (!editorObjects.Contains(placedObj))
            {
                editorObjects.Add(placedObj);
            }

            // Automatically select so they can drag it immediately
            SelectObject(placedObj);
            var controller = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.GetComponentInChildren<MechanicsEditorPanelController>() : null;
            if (controller != null)
            {
                controller.RefreshCandidateList();
                controller.RefreshWiringPanelIfActive(placedObj);
            }

            Debug.Log($"[GridPainter] Instantly spawned '{typeName}' at view center: {spawnPos}");
        }
        else
        {
            Debug.LogWarning($"[GridPainter] Could not spawn '{typeName}': Prefab not found in registry.");
        }
    }

    public void StartDragPlacement(string typeName)
    {
        CancelLinkingMode();
        ClearSelection();

        // Hero/PlayerStart is a single-instance object — block duplicates
        if (IsPlayerAsset(typeName))
        {
            bool alreadyExists = editorObjects.Exists(o => o != null && IsPlayerAsset(o.assetTypeName));
            if (alreadyExists)
            {
                Debug.LogWarning($"[GridPainter] Tried to drag-place '{typeName}' but a Hero already exists in the scene.");
                var panel = ValidatorPanelController.Instance;
                if (panel != null) panel.ShowSingleInstanceWarning("Hero");
                return;
            }
        }

        PaletteItem item = GetPaletteItem(typeName);
        if (item.editorPrefab != null)
        {
            // Spawn the editor visual representation
            activeDragObject = Instantiate(item.editorPrefab, Vector3.zero, Quaternion.identity, levelContainer);
            activeDragObject.name = item.editorPrefab.name;
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

        PlacedEditorObject placedObj = activeDragScript;
        SelectObject(placedObj);
        var controller = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.GetComponentInChildren<MechanicsEditorPanelController>() : null;
        if (controller != null)
        {
            controller.RefreshCandidateList();
            controller.RefreshWiringPanelIfActive(placedObj);
        }

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

    public void DeleteObject(PlacedEditorObject target)
    {
        if (target == null) return;

        // Remove active wires linked to this or from this
        RemoveLink(target);

        // Search for any other triggers that target this object
        foreach (var obj in editorObjects)
        {
            if (obj.targetObject == target)
            {
                RemoveLink(obj);
            }
        }

        editorObjects.Remove(target);
        Destroy(target.gameObject);

        if (selectedObject == target)
        {
            selectedObject = null;
            ClearSelection();
        }
    }

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

            GameObject playtestPrefab = editorObj.customPlaytestPrefab;
            if (playtestPrefab == null)
            {
                PaletteItem item = GetPaletteItem(editorObj.assetTypeName);
                playtestPrefab = item.playtestPrefab;
            }

            if (playtestPrefab != null)
            {
                // Specify levelContainer as parent so clones are correctly organized inside LevelPrefab
                GameObject clone = Instantiate(
                    playtestPrefab, 
                    editorObj.transform.position, 
                    editorObj.transform.rotation,
                    levelContainer
                );

                // Ensure the cloned gameplay object is active, even if the source was disabled/hidden
                clone.SetActive(true);

                // Copy editor scale
                clone.transform.localScale = editorObj.transform.localScale;

                playtestClones.Add(clone);
                playtestPairs[editorObj] = clone;

                // Configure properties
                ConfigureSpawnedTrap(clone, editorObj);
            }
        }

        // 3. Configure player start & Goal Portal (if placed in editor)
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && IsPlayerAsset(o.assetTypeName));
        if (playerStart != null)
        {
            originalPlayerStartPos = playerStart.transform.position;

            // Find the cloned gameplay player object (instantiated from the prefab)
            GameObject playtestPlayerClone = null;
            if (playtestPairs.ContainsKey(playerStart))
            {
                playtestPlayerClone = playtestPairs[playerStart];
            }

            // Search for Rigidbody2D inside the clone or its children
            Rigidbody2D rb = null;
            if (playtestPlayerClone != null)
            {
                rb = playtestPlayerClone.GetComponentInChildren<Rigidbody2D>(true);
            }

            if (rb != null)
            {
                activePlaytestPlayer = rb.gameObject;
                rb.simulated = true;
            }
            else if (playtestPlayerClone != null)
            {
                activePlaytestPlayer = playtestPlayerClone;
            }
            else
            {
                activePlaytestPlayer = playerStart.gameObject;
            }

            if (activePlaytestPlayer != null && LevelCreatorUI.Instance != null)
            {
                var pc = activePlaytestPlayer.GetComponent<PlayerController>() ?? activePlaytestPlayer.GetComponentInChildren<PlayerController>();
                if (pc != null)
                {
                    pc.Speed = LevelCreatorUI.Instance.playerMoveSpeed;
                    pc.JumpForce = LevelCreatorUI.Instance.playerJumpForce;
                    pc.MaxMultiJumps = LevelCreatorUI.Instance.playerMaxJumps;
                    pc.EnableFallDamage = LevelCreatorUI.Instance.playerEnableFallDamage;
                    Debug.Log($"[Playtest] Custom Player settings applied: Speed={pc.Speed}, JumpForce={pc.JumpForce}, MaxJumps={pc.MaxMultiJumps}, FallDamage={pc.EnableFallDamage}");
                }
            }

            var camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(activePlaytestPlayer.transform);
                camFollow.StartFollowing(); // Explicitly start camera tracking
            }
        }
        else
        {
            Debug.LogWarning("[GridPainter] Playtest Player is missing! Please make sure a PlayerStart object is present under LevelPrefab.");
        }

        // 3b. Attach PlaytestGoalValidator to the already-spawned goal clone (main loop already instantiated it)
        PlacedEditorObject goalObj = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "Goal"));
        if (goalObj != null && playtestPairs.ContainsKey(goalObj))
        {
            GameObject portalRoot = playtestPairs[goalObj];

            // The Selectable parent is just a drag container. The actual collidable portal
            // child is what OnTriggerEnter2D fires on. Find the first child with a Collider2D.
            Collider2D[] childColliders = portalRoot.GetComponentsInChildren<Collider2D>(true);
            GameObject validatorTarget = childColliders.Length > 0 ? childColliders[0].gameObject : portalRoot;

            if (validatorTarget.GetComponent<PlaytestGoalValidator>() == null)
            {
                validatorTarget.AddComponent<PlaytestGoalValidator>();
            }
            Debug.Log($"[GridPainter] PlaytestGoalValidator attached to '{validatorTarget.name}' (tag={validatorTarget.tag}) inside LevelPrefab.");
        }
        else
        {
            // Fallback: goal had no playtest prefab in palette, create one now
            PaletteItem goalItem = GetPaletteItem("Goal");
            GameObject goalPrefab = goalItem.playtestPrefab != null ? goalItem.playtestPrefab : goalItem.editorPrefab;
            if (goalPrefab != null && goalObj != null)
            {
                GameObject portalRoot = Instantiate(goalPrefab, goalObj.transform.position, Quaternion.identity, levelContainer);
                playtestClones.Add(portalRoot);
                Collider2D[] childColliders = portalRoot.GetComponentsInChildren<Collider2D>(true);
                GameObject validatorTarget = childColliders.Length > 0 ? childColliders[0].gameObject : portalRoot;
                validatorTarget.AddComponent<PlaytestGoalValidator>();
                Debug.Log($"[GridPainter] Goal fallback — PlaytestGoalValidator on '{validatorTarget.name}' inside LevelPrefab.");
            }
            else
            {
                Debug.LogWarning("[GridPainter] Playtest Goal portal prefab is null! Make sure 'Goal' has a playtestPrefab or editorPrefab assigned in the GridPainter inspector palette.");
            }
        }


        // 4. Wire and configure trigger-to-target links dynamically on the playtest clones
        foreach (var editorObj in editorObjects)
        {
            if (editorObj != null && playtestPairs.ContainsKey(editorObj))
            {
                CopyCollisionsAndTriggers(editorObj, playtestPairs[editorObj], playtestPairs);
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

        if (activePlaytestPlayer != null)
        {
            if (camFollow != null)
            {
                camFollow.SetTarget(activePlaytestPlayer.transform);
                camFollow.StartFollowing();
                Debug.Log("[GridPainter] Snapped camera view to active playtest player.");
            }
        }
        else
        {
            // In Edit Mode, stop active following so it doesn't fight manual panning/zooming,
            // and instantly teleport the camera to focus on the player's current position.
            if (camFollow != null)
            {
                camFollow.StopFollowing();
            }

            PlacedEditorObject playerStart = editorObjects.Find(o => o != null && IsPlayerAsset(o.assetTypeName));
            if (playerStart != null)
            {
                Vector3 playerPos = playerStart.transform.position;
                Vector3 camPos = editorCamera.transform.position;

                var settings = FindFirstObjectByType<LevelCameraSettings>();
                float targetX = playerPos.x;
                float targetY = playerPos.y;

                if (settings != null)
                {
                    targetX += settings.offset.x;
                    targetY = settings.followY ? (playerPos.y + settings.offset.y) : settings.fixedYHeight;
                }

                // Center directly on the player's current coordinates with offset applied
                editorCamera.transform.position = new Vector3(targetX, targetY, camPos.z);
                ClampCameraPosition();

                Debug.Log($"[GridPainter] Snapped editor camera to current player position (offset applied): {editorCamera.transform.position}");
            }
        }
    }

    private void ToggleEditorVisibility(bool visible)
    {
        foreach (var obj in editorObjects)
        {
            if (obj != null)
            {
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

        // Load custom player settings
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerMoveSpeed = data.playerMoveSpeed;
            LevelCreatorUI.Instance.playerJumpForce = data.playerJumpForce;
            LevelCreatorUI.Instance.playerMaxJumps = data.playerMaxJumps;
            LevelCreatorUI.Instance.playerEnableFallDamage = data.playerEnableFallDamage;

            if (ObjectTransformPanelController.Instance != null)
            {
                ObjectTransformPanelController.Instance.UpdatePlayerSettingsUI();
            }
        }

        // Load custom camera settings
        var cameraSettings = FindFirstObjectByType<LevelCameraSettings>();
        if (cameraSettings != null)
        {
            cameraSettings.offset = new Vector3(data.camOffsetX, data.camOffsetY, cameraSettings.offset.z);
            cameraSettings.orthoSize = data.camOrthoSize;
            if (!cameraSettings.followY)
            {
                cameraSettings.fixedYHeight = data.camOffsetY;
            }
            if (LevelCreatorUI.Instance != null)
            {
                LevelCreatorUI.Instance.InitializeCameraSettingsSliders();
            }
        }

        // 1. Load Player Spawn & Goal
        if (data.hasPlayerStart)
        {
            PaletteItem startItem = GetPaletteItem("PlayerStart");
            if (startItem.editorPrefab != null)
            {
                GameObject flag = Instantiate(startItem.editorPrefab, data.playerStartPos.ToVector2(), Quaternion.identity, levelContainer);
                var flagScript = flag.GetComponent<PlacedEditorObject>() ?? flag.AddComponent<PlacedEditorObject>();
                flagScript.assetTypeName = "PlayerStart";
                editorObjects.Add(flagScript);
            }
        }

        if (data.hasGoal)
        {
            PaletteItem goalItem = GetPaletteItem("Goal");
            if (goalItem.editorPrefab != null)
            {
                GameObject portal = Instantiate(goalItem.editorPrefab, data.goalPos.ToVector2(), Quaternion.identity, levelContainer);
                var portalScript = portal.GetComponent<PlacedEditorObject>() ?? portal.AddComponent<PlacedEditorObject>();
                portalScript.assetTypeName = "Goal";
                editorObjects.Add(portalScript);
            }
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

                var ct = clone.GetComponent<CollisionsAndTriggers>();
                if (ct == null && !string.IsNullOrEmpty(trap.triggerTypeStr))
                {
                    ct = clone.AddComponent<CollisionsAndTriggers>();
                }
                
                if (ct != null)
                {
                    ct.activateOnStart = trap.activateOnStart;
                    if (Enum.TryParse(trap.triggerTypeStr, out TriggerType triggerTypeVal)) ct.triggerType = triggerTypeVal;
                    if (Enum.TryParse(trap.componentActionStr, out ComponentAction componentActionVal)) ct.componentAction = componentActionVal;
                    ct.setObjectActive = trap.setObjectActive;
                    if (Enum.TryParse(trap.activationModeStr, out ActivationMode activationModeVal)) ct.activationMode = activationModeVal;
                    ct.enableMove = trap.enableMove;
                    if (Enum.TryParse(trap.moveDirectionStr, out MoveDirection moveDirectionVal)) ct.moveDirection = moveDirectionVal;
                    ct.moveSpeed = trap.moveSpeed;
                    ct.stopMoveOnExit = trap.stopMoveOnExit;
                    ct.isPingPong = trap.isPingPong;
                    ct.pingPongDistance = trap.pingPongDistance;
                    ct.enableRotation = trap.enableRotation;
                    if (Enum.TryParse(trap.rotationDirectionStr, out RotationDirection rotationDirectionVal)) ct.rotationDirection = rotationDirectionVal;
                    ct.rotationSpeed = trap.rotationSpeed;
                    ct.stopRotationOnExit = trap.stopRotationOnExit;
                    ct.useLocalCoordinates = trap.useLocalCoordinates;
                    ct.targetPosition = trap.targetPosition.ToVector2();
                    ct.targetMoveSpeed = trap.targetMoveSpeed;
                    ct.moveStaggerInterval = trap.moveStaggerInterval;
                    ct.moveOnXOnly = trap.moveOnXOnly;
                    ct.moveOnYOnly = trap.moveOnYOnly;
                    ct.preserveRelativeDistance = trap.preserveRelativeDistance;
                    ct.teleportPosition = trap.teleportPosition.ToVector2();
                    ct.useTargetX = trap.useTargetX;
                    ct.useTargetY = trap.useTargetY;
                    ct.newGravityScale = trap.newGravityScale;
                    ct.fallSpeedMultiplier = trap.fallSpeedMultiplier;
                    ct.applyOnEnter = trap.applyOnEnter;
                    ct.resetOnExit = trap.resetOnExit;
                    ct.newMaxJumpsValue = trap.newMaxJumpsValue;
                    ct.triggerDelay = trap.triggerDelay;
                    ct.deleteTriggerZone = trap.deleteTriggerZone;
                    ct.modifyColliderState = trap.modifyColliderState;
                    ct.makeSolid = trap.makeSolid;
                    ct.modifyGravityState = trap.modifyGravityState;
                    ct.makeSubjectToGravity = trap.makeSubjectToGravity;
                    ct.appearOnTrigger = trap.appearOnTrigger;
                    ct.playAudioOnTrigger = trap.playAudioOnTrigger;
                    ct.audioClipName = trap.audioClipName;
                    ct.loopAudio = trap.loopAudio;

                    // Camera Shake settings
                    ct.enableCameraShake = trap.enableCameraShake;
                    ct.playShakeSFX = trap.playShakeSFX;
                    ct.cameraShakeIntensity = trap.cameraShakeIntensity;
                    ct.cameraShakeFrequency = trap.cameraShakeFrequency;
                    ct.stopShakeOnExitBoundary = trap.stopShakeOnExitBoundary;
                }

                editorObjects.Add(script);
                loadedObjects[pos] = script;
            }
        }

        // 4. Reconstruct Trigger references and links
        foreach (var trap in data.traps)
        {
            Vector2 sourcePos = trap.spawnPos.ToVector2();
            if (loadedObjects.ContainsKey(sourcePos))
            {
                var sourceEditorObj = loadedObjects[sourcePos];
                var ct = sourceEditorObj.GetComponent<CollisionsAndTriggers>();
                if (ct != null)
                {
                    // Resolve objectToModify
                    Vector2 modPos = trap.objectToModifyPos.ToVector2();
                    if (modPos != Vector2.zero && loadedObjects.ContainsKey(modPos))
                    {
                        ct.objectToModify = loadedObjects[modPos].gameObject;
                    }

                    // Resolve destinationTargetObject
                    Vector2 destPos = trap.destinationTargetPos.ToVector2();
                    if (destPos != Vector2.zero && loadedObjects.ContainsKey(destPos))
                    {
                        ct.destinationTargetObject = loadedObjects[destPos].gameObject;
                    }

                    // Resolve objectsToTrigger list
                    if (trap.objectsToTriggerPositions != null && trap.objectsToTriggerPositions.Count > 0)
                    {
                        List<GameObject> triggerList = new List<GameObject>();
                        foreach (var targetPosS in trap.objectsToTriggerPositions)
                        {
                            Vector2 targetPos = targetPosS.ToVector2();
                            if (loadedObjects.ContainsKey(targetPos))
                            {
                                triggerList.Add(loadedObjects[targetPos].gameObject);
                            }
                        }
                        ct.objectsToTrigger = triggerList.ToArray();
                    }

                    // Resolve activationObjects list
                    if (trap.activationObjectsPositions != null && trap.activationObjectsPositions.Count > 0)
                    {
                        List<GameObject> activList = new List<GameObject>();
                        foreach (var actPosS in trap.activationObjectsPositions)
                        {
                            Vector2 actPos = actPosS.ToVector2();
                            if (loadedObjects.ContainsKey(actPos))
                            {
                                activList.Add(loadedObjects[actPos].gameObject);
                            }
                        }
                        ct.activationObjects = activList.ToArray();
                    }
                }

                // Keep visual link representation
                if (trap.hasTarget)
                {
                    Vector2 targetPos = trap.targetPos.ToVector2();
                    if (loadedObjects.ContainsKey(targetPos))
                    {
                        sourceEditorObj.hasTarget = true;
                        sourceEditorObj.targetObject = loadedObjects[targetPos];
                        CreateLink(sourceEditorObj, loadedObjects[targetPos]);
                    }
                }
            }
        }

        Debug.Log($"[GridPainter] Loaded free layout successfully. Objects: {editorObjects.Count}");
    }

    // ── Prefab Registry Helpers ──────────────────────────────────────────────

    private PaletteItem GetPaletteItem(string name)
    {
        // 1. Check if LevelCreatorUI defines a custom drag-and-drop prefab for this type
        if (LevelCreatorUI.Instance != null)
        {
            GameObject customPrefab = LevelCreatorUI.Instance.GetPrefabForType(name);
            if (customPrefab != null)
            {
                return new PaletteItem
                {
                    typeName = name,
                    editorPrefab = customPrefab,
                    playtestPrefab = customPrefab // Use the same prefab for gameplay
                };
            }
        }

        // 2. Fallback to old palette registry list
        foreach (var item in palette)
        {
            if (MatchAssetType(item.typeName, name)) return item;
        }
        return new PaletteItem();
    }

    public CustomLevelData ExportLevelData(string levelName, string creatorName)
    {
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        float camX = 0f;
        float camY = 0f;
        float camSize = 5f;
        if (settings != null)
        {
            camX = settings.offset.x;
            camY = settings.offset.y;
            camSize = settings.orthoSize;
        }

        CustomLevelData levelData = new CustomLevelData
        {
            levelName = levelName,
            creator = creatorName,
            playerMoveSpeed = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.playerMoveSpeed : 8f,
            playerJumpForce = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.playerJumpForce : 12f,
            playerMaxJumps = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.playerMaxJumps : 2,
            playerEnableFallDamage = LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.playerEnableFallDamage,
            camOffsetX = camX,
            camOffsetY = camY,
            camOrthoSize = camSize
        };

        // Find PlayerStart and Goal
        PlacedEditorObject playerStart = editorObjects.Find(o => o != null && IsPlayerAsset(o.assetTypeName));
        if (playerStart != null)
        {
            levelData.playerStartPos = new Vector2S(playerStart.transform.position);
            levelData.hasPlayerStart = true;
        }

        PlacedEditorObject goalObj = editorObjects.Find(o => o != null && MatchAssetType(o.assetTypeName, "Goal"));
        if (goalObj != null)
        {
            levelData.goalPos = new Vector2S(goalObj.transform.position);
            levelData.hasGoal = true;
        }

        // Export all other placed tiles and traps
        foreach (var obj in editorObjects)
        {
            if (obj == null || IsPlayerAsset(obj.assetTypeName) || MatchAssetType(obj.assetTypeName, "Goal")) continue;

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

    private bool IsPlayerAsset(string name)
    {
        return MatchAssetType(name, "PlayerStart") || MatchAssetType(name, "Hero") || MatchAssetType(name, "Spawn") || MatchAssetType(name, "Player");
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

    private void CopyCollisionsAndTriggers(PlacedEditorObject editorObj, GameObject spawnedObj, Dictionary<PlacedEditorObject, GameObject> playtestPairs)
    {
        var editorTriggerScript = editorObj.GetComponent<CollisionsAndTriggers>();
        if (editorTriggerScript == null) return;

        // Add or get component on playtest clone
        var playtestTrigger = spawnedObj.GetComponent<CollisionsAndTriggers>() ?? spawnedObj.AddComponent<CollisionsAndTriggers>();

        // Copy basic value fields
        playtestTrigger.activateOnStart = editorTriggerScript.activateOnStart;
        playtestTrigger.triggerType = editorTriggerScript.triggerType;
        playtestTrigger.componentAction = editorTriggerScript.componentAction;
        playtestTrigger.setObjectActive = editorTriggerScript.setObjectActive;
        playtestTrigger.activationMode = editorTriggerScript.activationMode;
        playtestTrigger.enableMove = editorTriggerScript.enableMove;
        playtestTrigger.moveDirection = editorTriggerScript.moveDirection;
        playtestTrigger.moveSpeed = editorTriggerScript.moveSpeed;
        playtestTrigger.stopMoveOnExit = editorTriggerScript.stopMoveOnExit;
        playtestTrigger.isPingPong = editorTriggerScript.isPingPong;
        playtestTrigger.pingPongDistance = editorTriggerScript.pingPongDistance;
        playtestTrigger.enableRotation = editorTriggerScript.enableRotation;
        playtestTrigger.rotationDirection = editorTriggerScript.rotationDirection;
        playtestTrigger.rotationSpeed = editorTriggerScript.rotationSpeed;
        playtestTrigger.stopRotationOnExit = editorTriggerScript.stopRotationOnExit;
        playtestTrigger.useLocalCoordinates = editorTriggerScript.useLocalCoordinates;
        playtestTrigger.targetPosition = editorTriggerScript.targetPosition;
        playtestTrigger.targetMoveSpeed = editorTriggerScript.targetMoveSpeed;
        playtestTrigger.teleportPosition = editorTriggerScript.teleportPosition;
        playtestTrigger.newGravityScale = editorTriggerScript.newGravityScale;
        playtestTrigger.fallSpeedMultiplier = editorTriggerScript.fallSpeedMultiplier;
        playtestTrigger.applyOnEnter = editorTriggerScript.applyOnEnter;
        playtestTrigger.resetOnExit = editorTriggerScript.resetOnExit;
        playtestTrigger.newMaxJumpsValue = editorTriggerScript.newMaxJumpsValue;
        playtestTrigger.triggerDelay = editorTriggerScript.triggerDelay;
        playtestTrigger.deleteTriggerZone = editorTriggerScript.deleteTriggerZone;
        
        // Copy Object Properties values
        playtestTrigger.modifyColliderState = editorTriggerScript.modifyColliderState;
        playtestTrigger.makeSolid = editorTriggerScript.makeSolid;
        playtestTrigger.modifyGravityState = editorTriggerScript.modifyGravityState;
        playtestTrigger.makeSubjectToGravity = editorTriggerScript.makeSubjectToGravity;
        playtestTrigger.appearOnTrigger = editorTriggerScript.appearOnTrigger;
        playtestTrigger.playAudioOnTrigger = editorTriggerScript.playAudioOnTrigger;
        playtestTrigger.audioClipName = editorTriggerScript.audioClipName;
        playtestTrigger.loopAudio = editorTriggerScript.loopAudio;
        playtestTrigger.useTargetX = editorTriggerScript.useTargetX;
        playtestTrigger.useTargetY = editorTriggerScript.useTargetY;
        playtestTrigger.moveOnXOnly = editorTriggerScript.moveOnXOnly;
        playtestTrigger.moveOnYOnly = editorTriggerScript.moveOnYOnly;
        playtestTrigger.moveStaggerInterval = editorTriggerScript.moveStaggerInterval;
        playtestTrigger.preserveRelativeDistance = editorTriggerScript.preserveRelativeDistance;

        // Copy Camera Shake values
        playtestTrigger.enableCameraShake = editorTriggerScript.enableCameraShake;
        playtestTrigger.playShakeSFX = editorTriggerScript.playShakeSFX;
        playtestTrigger.cameraShakeIntensity = editorTriggerScript.cameraShakeIntensity;
        playtestTrigger.cameraShakeFrequency = editorTriggerScript.cameraShakeFrequency;
        playtestTrigger.stopShakeOnExitBoundary = editorTriggerScript.stopShakeOnExitBoundary;

        // Copy reference fields mapped to playtest clones
        if (editorTriggerScript.objectToModify != null)
        {
            var modifyEditorScript = editorTriggerScript.objectToModify.GetComponent<PlacedEditorObject>();
            if (modifyEditorScript != null && playtestPairs.ContainsKey(modifyEditorScript))
            {
                playtestTrigger.objectToModify = playtestPairs[modifyEditorScript];
            }
            else
            {
                playtestTrigger.objectToModify = editorTriggerScript.objectToModify;
            }
        }

        if (editorTriggerScript.destinationTargetObject != null)
        {
            var destEditorScript = editorTriggerScript.destinationTargetObject.GetComponent<PlacedEditorObject>();
            if (destEditorScript != null && playtestPairs.ContainsKey(destEditorScript))
            {
                playtestTrigger.destinationTargetObject = playtestPairs[destEditorScript];
            }
            else
            {
                playtestTrigger.destinationTargetObject = editorTriggerScript.destinationTargetObject;
            }
        }

        List<GameObject> playtestTargets = new List<GameObject>();
        if (editorTriggerScript.objectsToTrigger != null && editorTriggerScript.objectsToTrigger.Length > 0)
        {
            foreach (var targetEditorGo in editorTriggerScript.objectsToTrigger)
            {
                if (targetEditorGo == null) continue;
                var targetEditorScript = targetEditorGo.GetComponent<PlacedEditorObject>();
                if (targetEditorScript != null && playtestPairs.ContainsKey(targetEditorScript))
                {
                    playtestTargets.Add(playtestPairs[targetEditorScript]);
                }
                else
                {
                    playtestTargets.Add(targetEditorGo);
                }
            }
        }
        else if (editorObj.hasTarget && editorObj.targetObject != null && playtestPairs.ContainsKey(editorObj.targetObject))
        {
            playtestTargets.Add(playtestPairs[editorObj.targetObject]);
        }
        playtestTrigger.objectsToTrigger = playtestTargets.ToArray();

        List<GameObject> playtestActivators = new List<GameObject>();
        if (editorTriggerScript.activationObjects != null && editorTriggerScript.activationObjects.Length > 0)
        {
            foreach (var actEditorGo in editorTriggerScript.activationObjects)
            {
                if (actEditorGo == null) continue;
                var actEditorScript = actEditorGo.GetComponent<PlacedEditorObject>();
                if (actEditorScript != null && playtestPairs.ContainsKey(actEditorScript))
                {
                    playtestActivators.Add(playtestPairs[actEditorScript]);
                }
                else
                {
                    playtestActivators.Add(actEditorGo);
                }
            }
        }
        playtestTrigger.activationObjects = playtestActivators.ToArray();

        // Ensure collider is set to trigger if it exists
        var col = spawnedObj.GetComponent<Collider2D>();
        if (col == null)
        {
            var newCol = spawnedObj.AddComponent<BoxCollider2D>();
            newCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        // If this trap uses "Appear on Trigger", hide target objects' renderers right now
        // so they start invisible in playtest. They'll be re-enabled when the trap fires.
        if (playtestTrigger.appearOnTrigger && playtestTrigger.objectsToTrigger != null)
        {
            foreach (var targetObj in playtestTrigger.objectsToTrigger)
            {
                if (targetObj == null) continue;
                var renderers = targetObj.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) r.enabled = false;
            }
        }
    }
}

/// <summary>
/// Helper script added to the spawned playtest Goal Portal.
/// Beats the level and notifies the editor upon contact.
/// </summary>
public class PlaytestGoalValidator : MonoBehaviour
{
    void Start()
    {
        // Force all colliders on this portal to be triggers so OnTriggerEnter2D fires
        var colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length == 0)
        {
            var newCol = gameObject.AddComponent<BoxCollider2D>();
            newCol.isTrigger = true;
            Debug.Log("[PlaytestGoalValidator] No collider found — BoxCollider2D added automatically.");
        }
        else
        {
            foreach (var col in colliders) col.isTrigger = true;
            Debug.Log($"[PlaytestGoalValidator] Ready on '{gameObject.name}'. Colliders={colliders.Length}, all isTrigger=true. Tag={gameObject.tag}");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Walk up the hierarchy — the colliding object might be a child of the player root
        Transform t = other.transform;
        bool isPlayer = false;
        while (t != null)
        {
            if (t.CompareTag("Player"))
            {
                isPlayer = true;
                break;
            }
            t = t.parent;
        }

        Debug.Log($"[PlaytestGoalValidator] OnTriggerEnter2D: hit by '{other.gameObject.name}' tag='{other.tag}' isPlayer={isPlayer}");
        if (isPlayer)
        {
            Debug.Log("[PlaytestGoalValidator] Player reached the Goal portal! Validating playtest success.");
            if (LevelCreatorUI.Instance != null)
                LevelCreatorUI.Instance.ValidatePlaytestSuccess();
        }
    }

    // Fallback: fires if collider is somehow NOT a trigger
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("[PlaytestGoalValidator] Hit player via solid collision — forcing isTrigger and retrying next frame.");
            foreach (var col in GetComponentsInChildren<Collider2D>(true)) col.isTrigger = true;
        }
    }
}
