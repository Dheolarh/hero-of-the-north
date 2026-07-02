using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerType
{
    None,
    Teleport,
    ContinousMotion,
    SingleMotion,
    RotationTrap,
    Ally,
    PhysicsModifier,
    JumpModifier
}

public enum ComponentAction
{
    None,
    AddRigidbody2D,
    AddBoxCollider2D,
    RemoveCollider
}

public enum MoveDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum RotationDirection
{
    Clockwise,
    CounterClockwise
}

public enum ActivationMode
{
    Toggle,
    ForceActive,
    ForceInactive
}

public class CollisionsAndTriggers : MonoBehaviour
{
    [Header("Activation Conditions")]
    [Tooltip("If true, the trap will activate immediately when the level starts.")]
    public bool activateOnStart = false;

    [Tooltip("List of objects that can trigger this trap when they collide with each other.")]
    public GameObject[] activationObjects;

    [Header("Objects to Manipulate")]
    public GameObject[] objectsToTrigger;

    [Header("This Object only uses physics settings")]
    public GameObject objectToModify;

    [Header("Trigger Type")]
    public TriggerType triggerType = TriggerType.None;

    [Header("Component Action")]
    public ComponentAction componentAction = ComponentAction.None;

    [Header("Object Active Toggle")]
    public bool setObjectActive; // Toggle objectsToTrigger active state
    [Tooltip("Choose whether the target object(s) should toggle their active state, force-activate (SET active = true), or force-deactivate (SET active = false).")]
    public ActivationMode activationMode = ActivationMode.Toggle;

    [Header("Movement Settings")]
    public bool enableMove;
    public MoveDirection moveDirection = MoveDirection.Right;
    public float moveSpeed;
    public bool stopMoveOnExit;

    [Header("Rotation Settings")]
    public bool enableRotation;
    public RotationDirection rotationDirection = RotationDirection.Clockwise;
    public float rotationSpeed;
    public bool stopRotationOnExit;

    [Header("Coordinate Mode")]
    [Tooltip("If true, movement and teleportation will use local coordinates relative to the parent object instead of global world coordinates.")]
    public bool useLocalCoordinates = false;

    [Header("One-Time Movement Settings")]
    public Vector2 targetPosition;
    public float targetMoveSpeed;

    [Header("Teleport Settings")]
    public Vector2 teleportPosition;

    [Header("Destination Target Settings")]
    [Tooltip("The object in the scene to use as the movement/teleportation destination.")]
    public GameObject destinationTargetObject;
    [Tooltip("Should it match the target's X position?")]
    public bool useTargetX = true;
    [Tooltip("Should it match the target's Y position?")]
    public bool useTargetY = true;

    [Header("Physics Modification Settings")]
    public float newGravityScale;
    public float fallSpeedMultiplier;
    public bool applyOnEnter = true;
    public bool resetOnExit = false;

    [Header("Jump Modification Settings")]
    [Tooltip("The overridden MaxMultiJumps value applied to the player (0 = no jump, 1 = normal jump, 2 = 1 multijump).")]
    public int newMaxJumpsValue = 2;

    private int originalMaxJumpsValue;
    private bool isJumpModified = false;
    
    [Header("Delay Settings")]
    [Tooltip("Delay in seconds before the trap activates after being triggered.")]
    public float triggerDelay = 0f;
    
    private bool isMovingToTarget = false;
    private Rigidbody2D modifyRigidbody;
    private float originalGravityScale;
    private bool isPhysicsModified = false;
    private float lastTriggerTime = -999f;

    [Header("Delete Trigger Zone")]
    public bool deleteTriggerZone;

    [Header("Audio Settings")]
    public bool playAudioOnTrigger;
    public string audioClipName;
    public bool loopAudio;



    void Start()
    {
        // Cache the Rigidbody2D component if objectToModify is set
        if (objectToModify != null)
        {
            modifyRigidbody = objectToModify.GetComponent<Rigidbody2D>();
            if (modifyRigidbody != null)
            {
                originalGravityScale = modifyRigidbody.gravityScale;
            }
        }

        // Setup activation conditions
        if (activateOnStart)
        {
            ExecuteTriggerActions();
        }
        else
        {
            // Always add listener to ourselves so we can also detect collisions
            var selfListener = gameObject.GetComponent<ActivationCollisionListener>() ?? gameObject.AddComponent<ActivationCollisionListener>();
            selfListener.parentTrigger = this;

            if (activationObjects != null && activationObjects.Length > 0)
            {
                foreach (var obj in activationObjects)
                {
                    if (obj != null)
                    {
                        // Add listeners to detect mutual collisions
                        var listener = obj.GetComponent<ActivationCollisionListener>() ?? obj.AddComponent<ActivationCollisionListener>();
                        listener.parentTrigger = this;
                    }
                }
            }
        }
    }

    void Update()
    {
        // Handle continuous motion (independent)
        if (enableMove)
        {
            ContinuousMovement();
        }

        // Handle continuous rotation (independent)
        if (enableRotation)
        {
            ApplyRotation();
        }

        // Handle one-time movement to target
        if (isMovingToTarget)
        {
            MoveToTarget();
        }

        // Apply fall speed multiplier when physics is modified
        if (isPhysicsModified && modifyRigidbody != null)
        {
            ApplyFallSpeedMultiplier();
        }
    }

    // ========== MOVEMENT FUNCTIONS ==========

    private Vector2 GetTargetDestination(GameObject movingObj, Vector2 defaultStaticPos, bool isTeleport)
    {
        if (destinationTargetObject == null)
        {
            return isTeleport ? teleportPosition : targetPosition;
        }

        Vector2 targetCoords = useLocalCoordinates ? (Vector2)destinationTargetObject.transform.localPosition : (Vector2)destinationTargetObject.transform.position;
        Vector2 currentCoords = useLocalCoordinates ? (Vector2)movingObj.transform.localPosition : (Vector2)movingObj.transform.position;

        float x = useTargetX ? targetCoords.x : currentCoords.x;
        float y = useTargetY ? targetCoords.y : currentCoords.y;

        return new Vector2(x, y);
    }

    void MoveToTarget()
    {
        if (objectsToTrigger == null || objectsToTrigger.Length == 0) return;

        foreach (GameObject obj in objectsToTrigger)
        {
            if (obj == null) continue;

            Vector2 dest = GetTargetDestination(obj, targetPosition, false);

            if (useLocalCoordinates)
            {
                Vector2 currentPos = obj.transform.localPosition;
                Vector2 newPos = Vector2.MoveTowards(currentPos, dest, targetMoveSpeed * Time.deltaTime);
                obj.transform.localPosition = new Vector3(newPos.x, newPos.y, obj.transform.localPosition.z);

                // Check if reached target (using first object as reference)
                if (obj == objectsToTrigger[0] && Vector2.Distance(currentPos, dest) < 0.01f)
                {
                    obj.transform.localPosition = new Vector3(dest.x, dest.y, obj.transform.localPosition.z);
                    isMovingToTarget = false;
                }
            }
            else
            {
                Vector2 currentPos = obj.transform.position;
                Vector2 newPos = Vector2.MoveTowards(currentPos, dest, targetMoveSpeed * Time.deltaTime);
                obj.transform.position = new Vector3(newPos.x, newPos.y, obj.transform.position.z);

                // Check if reached target (using first object as reference)
                if (obj == objectsToTrigger[0] && Vector2.Distance(currentPos, dest) < 0.01f)
                {
                    obj.transform.position = new Vector3(dest.x, dest.y, obj.transform.position.z);
                    isMovingToTarget = false;
                }
            }
        }
    }

    void ContinuousMovement()
    {
        if (objectsToTrigger == null) return;

        float xDirection = 0f;
        float yDirection = 0f;

        switch (moveDirection)
        {
            case MoveDirection.Right: xDirection = 1f; break;
            case MoveDirection.Left: xDirection = -1f; break;
            case MoveDirection.Up: yDirection = 1f; break;
            case MoveDirection.Down: yDirection = -1f; break;
        }

        // Apply movement to all objects in array
        foreach (GameObject obj in objectsToTrigger)
        {
            if (obj != null)
            {
                // Skip translation if the object has its own PingPongMovement script to prevent double movement
                if (obj.GetComponent<PingPongMovement>() != null) continue;

                obj.transform.Translate(
                    xDirection * Time.deltaTime * moveSpeed,
                    yDirection * Time.deltaTime * moveSpeed,
                    0,
                    Space.World
                );
            }
        }
    }

    void ApplyRotation()
    {
        if (objectsToTrigger == null) return;

        float rotationDir = (rotationDirection == RotationDirection.Clockwise) ? -1f : 1f;

        // Apply rotation to all objects in array
        foreach (GameObject obj in objectsToTrigger)
        {
            if (obj != null)
            {
                obj.transform.Rotate(
                    0,
                    0,
                    rotationDir * rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    void StartMoveToTarget()
    {
        isMovingToTarget = true;
    }

    void StartMove()
    {
        enableMove = true;
    }

    void StopMove()
    {
        enableMove = false;
    }

    void StartRotation()
    {
        enableRotation = true;
    }

    void StopRotation()
    {
        enableRotation = false;
    }

    void Teleport()
    {
        if (objectsToTrigger != null && objectsToTrigger.Length > 0)
        {
            foreach (GameObject obj in objectsToTrigger)
            {
                if (obj != null)
                {
                    Vector2 dest = GetTargetDestination(obj, teleportPosition, true);

                    if (useLocalCoordinates)
                    {
                        obj.transform.localPosition = new Vector3(dest.x, dest.y, obj.transform.localPosition.z);
                    }
                    else
                    {
                        obj.transform.position = new Vector3(dest.x, dest.y, obj.transform.position.z);
                    }
                }
            }
        }
    }





    // ========== OBJECT MANIPULATION FUNCTIONS ========== 

    void SetObjectActiveState()
    {
        if (objectsToTrigger != null)
        {
            foreach (GameObject obj in objectsToTrigger)
            {
                if (obj != null)
                {
                    switch (activationMode)
                    {
                        case ActivationMode.Toggle:
                            obj.SetActive(!obj.activeSelf);
                            break;
                        case ActivationMode.ForceActive:
                            obj.SetActive(true);
                            break;
                        case ActivationMode.ForceInactive:
                            obj.SetActive(false);
                            break;
                    }
                }
            }
        }
    }

    void AddComponentToObject()
    {
        if (objectsToTrigger == null) return;

        foreach (GameObject obj in objectsToTrigger)
        {
            if (obj == null) continue;

            switch (componentAction)
            {
                case ComponentAction.AddRigidbody2D:
                    obj.AddComponent<Rigidbody2D>();
                    break;
                case ComponentAction.AddBoxCollider2D:
                    obj.AddComponent<BoxCollider2D>();
                    break;
                case ComponentAction.RemoveCollider:
                    var collider = obj.GetComponent<BoxCollider2D>();
                    if (collider != null) collider.enabled = false;
                    break;
            }
        }
    }


    // ========== PHYSICS MODIFICATION FUNCTIONS ==========

    void ModifyPhysics()
    {
        if (modifyRigidbody == null)
        {
            Debug.LogWarning("No Rigidbody2D found on objectToModify!");
            return;
        }

        if (!isPhysicsModified)
        {
            originalGravityScale = modifyRigidbody.gravityScale;
        }

        modifyRigidbody.gravityScale = newGravityScale;
        isPhysicsModified = true;

        Debug.Log($"Physics modified: Gravity Scale = {newGravityScale}, Fall Multiplier = {fallSpeedMultiplier}");
    }

    void ResetPhysics()
    {
        if (modifyRigidbody == null || !isPhysicsModified) return;

        modifyRigidbody.gravityScale = originalGravityScale;
        isPhysicsModified = false;

        Debug.Log("Physics reset to original values");
    }

    void ApplyFallSpeedMultiplier()
    {
        if (modifyRigidbody.linearVelocity.y < 0)
        {
            modifyRigidbody.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallSpeedMultiplier - 1) * Time.deltaTime;
        }
    }

    // ========== JUMP MODIFICATION FUNCTIONS ==========

    void ModifyJumpSettings()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (!isJumpModified)
                {
                    originalMaxJumpsValue = pc.MaxMultiJumps;
                    isJumpModified = true;
                }
                pc.MaxMultiJumps = newMaxJumpsValue;
                Debug.Log($"[JumpModifier] Jump settings overridden to: {newMaxJumpsValue}");
            }
        }
    }

    void ResetJumpSettings()
    {
        if (!isJumpModified) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.MaxMultiJumps = originalMaxJumpsValue;
                isJumpModified = false;
                Debug.Log($"[JumpModifier] Jump settings reset to original: {originalMaxJumpsValue}");
            }
        }
    }

    // ========== COLLISION EVENTS ==========

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (triggerType == TriggerType.Ally)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.alliesSaved++;
                    Debug.Log($"[CollisionsAndTriggers] Ally saved! Total: {ScoreManager.Instance.alliesSaved}");
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfx("Ally");
                }
                Destroy(gameObject);
            }
        }
    }

    // ========== TRIGGER EVENTS ==========

    public void ReportActivationCollision(GameObject objA, GameObject objB)
    {
        if (Time.time - lastTriggerTime < 0.1f) return;
        lastTriggerTime = Time.time;

        Debug.Log($"[CollisionsAndTriggers] Activation collision detected between {objA.name} and {objB.name}!");
        if (triggerDelay > 0f)
        {
            StartCoroutine(TriggerSequenceWithDelay());
        }
        else
        {
            ExecuteTriggerActions();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Fallback to default Player collision behavior if activationObjects list is empty and activateOnStart is false
        if ((activationObjects == null || activationObjects.Length == 0) && !activateOnStart)
        {
            Debug.Log($"[CollisionsAndTriggers] OnTriggerEnter2D called! Other: {other.gameObject.name}, Tag: {other.tag}, This GameObject: {gameObject.name}");        
            if (other.CompareTag("Player"))
            {
                if (triggerDelay > 0f)
                {
                    StartCoroutine(TriggerSequenceWithDelay());
                }
                else
                {
                    ExecuteTriggerActions();
                }
            }
        }
    }

    private IEnumerator TriggerSequenceWithDelay()
    {
        yield return new WaitForSeconds(triggerDelay);
        ExecuteTriggerActions();
    }

    private void ExecuteTriggerActions()
    {
        // Handle audio playback first (before teleporting/moving the player)
        if (playAudioOnTrigger && !string.IsNullOrEmpty(audioClipName))
        {
            if (AudioManager.Instance != null)
            {
                if (loopAudio)
                {
                    AudioManager.Instance.PlayLoopingSound(audioClipName);
                }
                else
                {
                    AudioManager.Instance.PlaySfx(audioClipName);
                }
            }
        }

        switch (triggerType)
        {
            case TriggerType.ContinousMotion:
                enableMove = true;
                if (objectsToTrigger != null)
                {
                    foreach (GameObject obj in objectsToTrigger)
                    {
                        if (obj != null)
                        {
                            PingPongMovement ppm = obj.GetComponent<PingPongMovement>();
                            if (ppm != null) ppm.Activate();
                        }
                    }
                }
                break;

            case TriggerType.RotationTrap:
                enableRotation = true;
                break;

            case TriggerType.SingleMotion:
                StartMoveToTarget();
                Debug.Log("Trap triggered!");
                break;

            case TriggerType.Teleport:
                Teleport();
                Debug.Log("Teleport triggered!");
                break;

            case TriggerType.PhysicsModifier:
                if (applyOnEnter)
                {
                    ModifyPhysics();
                }
                break;

            case TriggerType.JumpModifier:
                if (applyOnEnter)
                {
                    ModifyJumpSettings();
                }
                break;

            case TriggerType.Ally:
                break;
        }

        // Handle object active toggle
        if (setObjectActive)
        {
            SetObjectActiveState();
        }

        // Handle component actions
        if (componentAction != ComponentAction.None)
        {
            AddComponentToObject();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerType == TriggerType.ContinousMotion && stopMoveOnExit)
            {
                StopMove();
                if (objectsToTrigger != null)
                {
                    foreach (GameObject obj in objectsToTrigger)
                    {
                        if (obj != null)
                        {
                            PingPongMovement ppm = obj.GetComponent<PingPongMovement>();
                            if (ppm != null) ppm.Deactivate();
                        }
                    }
                }
            }

            if (triggerType == TriggerType.RotationTrap && stopRotationOnExit)
            {
                StopRotation();
            }

            if (triggerType == TriggerType.PhysicsModifier && resetOnExit)
            {
                ResetPhysics();
            }

            if (triggerType == TriggerType.JumpModifier && resetOnExit)
            {
                ResetJumpSettings();
            }
        }

        if (deleteTriggerZone)
        {
            var collider = GetComponent<Collider2D>();
            collider.enabled = false;
        }
    }
}

/// <summary>
/// Helper listener added at runtime to objects that trigger the trap.
/// Reports a trigger collision if any two items in the activation list touch.
/// </summary>
public class ActivationCollisionListener : MonoBehaviour
{
    public CollisionsAndTriggers parentTrigger;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject otherGo)
    {
        if (parentTrigger == null || parentTrigger.activationObjects == null) return;

        bool thisInList = false;
        bool otherInList = false;

        foreach (var obj in parentTrigger.activationObjects)
        {
            if (obj == gameObject) thisInList = true;
            if (obj == otherGo) otherInList = true;
        }

        // Case 1: Two objects in the activation list touch
        if (thisInList && otherInList)
        {
            parentTrigger.ReportActivationCollision(gameObject, otherGo);
            return;
        }

        // Case 2: Only 1 activation object is selected, check if it touches the trigger zone itself
        if (parentTrigger.activationObjects.Length == 1)
        {
            bool thisIsTriggerZone = (gameObject == parentTrigger.gameObject);
            bool otherIsTriggerZone = (otherGo == parentTrigger.gameObject);

            if ((thisInList && otherIsTriggerZone) || (otherInList && thisIsTriggerZone))
            {
                parentTrigger.ReportActivationCollision(gameObject, otherGo);
            }
        }
    }
}
