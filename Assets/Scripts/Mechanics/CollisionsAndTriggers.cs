using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionsAndTriggers : MonoBehaviour
{
    [Header("Object to Manipulate")]
    public GameObject objectToTrigger;
    public GameObject objectToModify;

    [Header("Trigger Type - Check ONE")]
    public bool isTeleportTrigger;
    public bool isTrapTrigger;
    public bool isTrapMovementTrigger;
    public bool isAllyTrigger;
    public bool setObjectActive;
    public bool addComponentToObject;
    public bool isRigidbody2D;
    public bool isBoxCollider2D;
    public bool removeCollider;
    public bool isPhysicsModifier;

    [Header("Movement Type")]
    public bool allowConstantMotion; // TRUE = continuous, FALSE = move to target once

    [Header("Continuous Motion Settings")]
    public bool moveRight; // Move right if true, left if false
    public bool moveUp; // Move up if true, down if false
    public float moveSpeed = 5f;

    [Header("One-Time Movement Settings")]
    public Vector2 targetPosition;
    public float targetMoveSpeed = 5f;

    [Header("Rotation Settings")]
    public bool enableRotation; // Enable rotation while moving
    public bool rotateClockwise; // Rotate clockwise if true, counter-clockwise if false
    public float rotationSpeed = 100f; // Degrees per second

    [Header("Physics Modification Settings")]
    public float newGravityScale = 1f;
    public float fallSpeedMultiplier = 2.5f;
    public bool applyOnEnter = true; // Apply physics on trigger enter
    public bool resetOnExit = false; // Reset physics on trigger exit

    private Transform objectTransform;
    private bool isMovingToTarget = false;
    private Rigidbody2D modifyRigidbody;
    private float originalGravityScale;
    private bool isPhysicsModified = false;

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

        // Handle continuous motion
        if (allowConstantMotion)
        {
            ContinuousMovement();
            
            // Apply rotation independently if enabled
            if (enableRotation)
            {
                ApplyRotation();
            }
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

    void ContinuousMovement()
    {
        float xDirection = moveRight ? 1f : -1f;
        float yDirection = moveUp ? 1f : -1f;
        
        // Move in world space to avoid rotation affecting movement direction
        objectTransform.Translate(
            xDirection * Time.deltaTime * moveSpeed,
            yDirection * Time.deltaTime * moveSpeed,
            0,
            Space.World
        );
    }

    void MoveToTarget()
    {
        Vector2 currentPos = objectTransform.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, targetMoveSpeed * Time.deltaTime);
        objectTransform.position = newPos;

        // Stop when reached
        if (Vector2.Distance(currentPos, targetPosition) < 0.01f)
        {
            objectTransform.position = targetPosition;
            isMovingToTarget = false;
        }
    }

    void ApplyRotation()
    {
        float rotationDirection = rotateClockwise ? -1f : 1f;
        objectTransform.Rotate(
            0,
            0,
            rotationDirection * rotationSpeed * Time.deltaTime
        );
    }

    void StartContinuousMotion()
    {
        allowConstantMotion = true;
    }

    void StopContinuousMotion()
    {
        allowConstantMotion = false;
    }

    void StartMoveToTarget()
    {
        isMovingToTarget = true;
    }

    void SetObjectActiveState()
    {
        objectToTrigger.SetActive(!objectToTrigger.activeSelf);
    }

    void AddComponentToObject()
    {
        if(isRigidbody2D){objectToTrigger.AddComponent<Rigidbody2D>();}
        if(isBoxCollider2D){objectToTrigger.AddComponent<BoxCollider2D>();}
    }

    void RemoveCollider()
    {
        if(removeCollider){objectToTrigger.GetComponent<BoxCollider2D>().enabled = false;}
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
            if (isAllyTrigger)
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

            if (isTrapMovementTrigger)
            {
                if (allowConstantMotion)
                {
                    StartContinuousMotion();
                }
            }

            if (isTrapTrigger)
            {
                StartMoveToTarget();
                Debug.Log("Trap triggered!");
            }

            if (isTeleportTrigger)
            {
                Debug.Log("Teleport triggered!");
            }
            
            if (setObjectActive)
            {
                SetObjectActiveState();
            }
            
            if (addComponentToObject)
            {
                AddComponentToObject();
            }
            
            if (removeCollider)
            {
                RemoveCollider();
            }

            if (isPhysicsModifier && applyOnEnter)
            {
                ModifyPhysics();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isTrapMovementTrigger && allowConstantMotion)
            {
                StopContinuousMotion();
            }

            if (isPhysicsModifier && resetOnExit)
            {
                ResetPhysics();
            }
        }
    }
}
