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
    public float moveDirectionX;
    public float moveDirectionY;
    public float moveSpeed = 5f;

    [Header("One-Time Movement Settings")]
    public Vector2 targetPosition;
    public float targetMoveSpeed = 5f;

    [Header("Rotation Settings")]
    public float rotationZ;
    public float rotationX;

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
        }

        // Handle one-time movement to target
        if (isMovingToTarget)
        {
            MoveToTarget();
        }
        ApplyRotation();

        // Apply fall speed multiplier when physics is modified
        if (isPhysicsModified && modifyRigidbody != null)
        {
            ApplyFallSpeedMultiplier();
        }
    }

    // ========== MOVEMENT FUNCTIONS ==========

    void ContinuousMovement()
    {
        if (moveDirectionX != 0 || moveDirectionY != 0)
        {
            objectTransform.Translate(
                moveDirectionX * Time.deltaTime * moveSpeed,
                moveDirectionY * Time.deltaTime * moveSpeed,
                0
            );
        }
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
        if (rotationX != 0 || rotationZ != 0)
        {
            objectTransform.Rotate(
                rotationX * Time.deltaTime * moveSpeed,
                0,
                rotationZ * Time.deltaTime * moveSpeed
            );
        }
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
        if(objectToTrigger.activeSelf == true){
            objectToTrigger.SetActive(true);
        }
        else{objectToTrigger.SetActive(false);}
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
