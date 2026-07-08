using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct Vector2S
{
    public float x;
    public float y;

    public Vector2S(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2S(Vector2 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
}

/// <summary>
/// Represents placed terrain block element in custom levels with position, scale, and rotation.
/// </summary>
[Serializable]
public class CustomTileData
{
    public string type;
    public Vector2S position;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
}

/// <summary>
/// Represents placed hazard/trap elements with position, scale, rotation, and mechanics properties.
/// </summary>
[Serializable]
public class CustomTrapData
{
    public string type;
    public Vector2S spawnPos;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
    
    // Physics/Trigger settings
    public string moveDir = "Down";
    public float speed = 3f;
    public float delay = 1f;

    // Trigger wiring (connection target coordinates)
    public bool hasTarget = false;
    public Vector2S targetPos;

    // Advanced Trigger settings (CollisionsAndTriggers fields)
    public bool activateOnStart;
    public string triggerTypeStr; // TriggerType enum
    public string componentActionStr; // ComponentAction enum
    public bool setObjectActive;
    public string activationModeStr; // ActivationMode enum
    public bool enableMove;
    public string moveDirectionStr; // MoveDirection enum
    public float moveSpeed;
    public bool stopMoveOnExit;
    public bool enableRotation;
    public string rotationDirectionStr; // RotationDirection enum
    public float rotationSpeed;
    public bool stopRotationOnExit;
    public bool useLocalCoordinates;
    public Vector2S targetPosition;
    public float targetMoveSpeed;
    public float moveStaggerInterval;
    public bool moveOnXOnly;
    public bool moveOnYOnly;
    public bool preserveRelativeDistance;
    public Vector2S teleportPosition;
    public bool useTargetX;
    public bool useTargetY;
    public float newGravityScale;
    public float fallSpeedMultiplier;
    public bool applyOnEnter;
    public bool resetOnExit;
    public int newMaxJumpsValue;
    public float triggerDelay;
    public bool deleteTriggerZone;
    public bool modifyColliderState;
    public bool makeSolid;
    public bool modifyGravityState;
    public bool makeSubjectToGravity;
    public bool appearOnTrigger;
    public bool playAudioOnTrigger;
    public string audioClipName;
    public bool loopAudio;

    // Camera Shake settings
    public bool enableCameraShake;
    public bool playShakeSFX;
    public float cameraShakeIntensity;
    public float cameraShakeFrequency;
    public bool stopShakeOnExitBoundary;

    // Serialized Object References (represented as spawn coordinates)
    public Vector2S objectToModifyPos;
    public Vector2S destinationTargetPos;
    public List<Vector2S> objectsToTriggerPositions = new List<Vector2S>();
    public List<Vector2S> activationObjectsPositions = new List<Vector2S>();
}

/// <summary>
/// Root data structure representing a complete custom level configuration.
/// Can be fully serialized to a JSON string via JsonUtility.ToJson.
/// </summary>
[Serializable]
public class CustomLevelData
{
    public string levelName;
    public string creator;
    
    public int gridWidth = 32;
    public int gridHeight = 18;

    public Vector2S playerStartPos;
    public Vector2S goalPos;

    public List<CustomTileData> tiles = new List<CustomTileData>();
    public List<CustomTrapData> traps = new List<CustomTrapData>();

    // Global Player Settings
    public float playerMoveSpeed = 5f;
    public float playerJumpForce = 7f;
    public int playerMaxJumps = 1;
    public bool playerEnableFallDamage = false;

    // Camera Settings
    public float camOffsetX = 0f;
    public float camOffsetY = 0f;
    public float camOrthoSize = 5f;
}
