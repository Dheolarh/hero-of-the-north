using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerType
{
    None,
    Teleport,
    Trap,
    MotionTrap,
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
    [Header("Object to Manipulate")]
    public GameObject objectToTrigger;

    [Header("This Object only uses physics settings")]
    public GameObject objectToModify;

    [Header("Trigger Type")]
    public TriggerType triggerType = TriggerType.None;

    [Header("Component Action")]
    public ComponentAction componentAction = ComponentAction.None;
    
    [Header("Object Active Toggle")]
    public bool setObjectActive; // Toggle objectToTrigger active state

    [Header("Movement Settings")]
    public bool enableMove;
    public MoveDirection moveDirection = MoveDirection.Right;
    public float moveSpeed = 5f;
    public bool stopMoveOnExit;

    [Header("Rotation Settings")]
    public bool enableRotation;
    public RotationDirection rotationDirection = RotationDirection.Clockwise;
    public float rotationSpeed = 100f;
    public bool stopRotationOnExit;

    [Header("One-Time Movement Settings")]
    public Vector2 targetPosition;
    public float targetMoveSpeed = 5f;

    [Header("Teleport Settings")]
    public Vector2 teleportPosition;

    [Header("Physics Modification Settings")]
    public float newGravityScale = 1f;
    public float fallSpeedMultiplier = 2.5f;
    public bool applyOnEnter = true; 
    public bool resetOnExit = false;
    private Transform objectTransform;
    private bool isMovingToTarget = false;
    private Rigidbody2D modifyRigidbody;
    private float originalGravityScale;
    private bool isPhysicsModified = false;

    [Header("Delete Trigger Zone")]
    public bool deleteTriggerZone;

    void Start()
    {
        if (objectToTrigger != null)
        {
            objectTransform = objectToTrigger.transform;
        }

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
        if (objectTransform == null) return;

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
        Vector2 currentPos = objectTransform.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, targetMoveSpeed * Time.deltaTime);
        objectTransform.position = newPos;
        if (Vector2.Distance(currentPos, targetPosition) < 0.01f)
        {
            objectTransform.position = targetPosition;
            isMovingToTarget = false;
        }
    }

    void ContinuousMovement()
    {
        float xDirection = 0f;
        float yDirection = 0f;
        
        switch (moveDirection)
        {
            case MoveDirection.Right: xDirection = 1f; break;
            case MoveDirection.Left: xDirection = -1f; break;
            case MoveDirection.Up: yDirection = 1f; break;
            case MoveDirection.Down: yDirection = -1f; break;
        }
        
        objectTransform.Translate(
            xDirection * Time.deltaTime * moveSpeed,
            yDirection * Time.deltaTime * moveSpeed,
            0,
            Space.World
        );
    }

    void ApplyRotation()
    {
        float rotationDir = (rotationDirection == RotationDirection.Clockwise) ? -1f : 1f;
        objectTransform.Rotate(
            0,
            0,
            rotationDir * rotationSpeed * Time.deltaTime
        );
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
        if (objectTransform == null) return;
        objectTransform.position = teleportPosition;
    }





    // ========== OBJECT MANIPULATION FUNCTIONS ========== 

    void SetObjectActiveState()
    {
        objectToTrigger.SetActive(!objectToTrigger.activeSelf);
    }

    void AddComponentToObject()
    {
        switch (componentAction)
        {
            case ComponentAction.AddRigidbody2D:
                objectToTrigger.AddComponent<Rigidbody2D>();
                break;
            case ComponentAction.AddBoxCollider2D:
                objectToTrigger.AddComponent<BoxCollider2D>();
                break;
            case ComponentAction.RemoveCollider:
                var collider = objectToTrigger.GetComponent<BoxCollider2D>();
                if (collider != null) collider.enabled = false;
                break;
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
                Destroy(gameObject);
            }
        }
    }

    // ========== TRIGGER EVENTS ==========

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            switch (triggerType)
            {
                case TriggerType.MotionTrap:
                    enableMove = true;
                    break;
                    
                case TriggerType.RotationTrap:
                    enableRotation = true;
                    break;
                    
                case TriggerType.Trap:
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
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerType == TriggerType.MotionTrap && stopMoveOnExit)
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
            Destroy(gameObject);
        }
    }
}
