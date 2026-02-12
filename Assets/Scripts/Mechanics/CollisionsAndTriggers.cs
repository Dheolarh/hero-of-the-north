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
    PhysicsModifier
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

public class CollisionsAndTriggers : MonoBehaviour
{
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

    [Header("One-Time Movement Settings")]
    public Vector2 targetPosition;
    public float targetMoveSpeed;

    [Header("Teleport Settings")]
    public Vector2 teleportPosition;

    [Header("Physics Modification Settings")]
    public float newGravityScale;
    public float fallSpeedMultiplier;
    public bool applyOnEnter = true;
    public bool resetOnExit = false;

    private bool isMovingToTarget = false;
    private Rigidbody2D modifyRigidbody;
    private float originalGravityScale;
    private bool isPhysicsModified = false;

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

    void MoveToTarget()
    {
        if (objectsToTrigger == null || objectsToTrigger.Length == 0) return;

        foreach (GameObject obj in objectsToTrigger)
        {
            if (obj == null) continue;

            Vector2 currentPos = obj.transform.position;
            Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, targetMoveSpeed * Time.deltaTime);
            obj.transform.position = newPos;

            // Check if reached target (using first object as reference)
            if (obj == objectsToTrigger[0] && Vector2.Distance(currentPos, targetPosition) < 0.01f)
            {
                obj.transform.position = targetPosition;
                isMovingToTarget = false;
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
                    obj.transform.position = teleportPosition;
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
                    obj.SetActive(!obj.activeSelf);
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
        if (modifyRigidbody.velocity.y < 0)
        {
            modifyRigidbody.velocity += Vector2.up * Physics2D.gravity.y * (fallSpeedMultiplier - 1) * Time.deltaTime;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[CollisionsAndTriggers] OnTriggerEnter2D called! Other: {other.gameObject.name}, Tag: {other.tag}, This GameObject: {gameObject.name}");        
        if (other.CompareTag("Player"))
        {
            switch (triggerType)
            {
                case TriggerType.ContinousMotion:
                    enableMove = true;
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

            // Handle audio playback
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


        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerType == TriggerType.ContinousMotion && stopMoveOnExit)
            {
                StopMove();
            }

            if (triggerType == TriggerType.RotationTrap && stopRotationOnExit)
            {
                StopRotation();
            }

            if (triggerType == TriggerType.PhysicsModifier && resetOnExit)
            {
                ResetPhysics();
            }
        }

        if (deleteTriggerZone)
        {
            var collider = GetComponent<Collider2D>();
            collider.enabled = false;
        }
    }
}
