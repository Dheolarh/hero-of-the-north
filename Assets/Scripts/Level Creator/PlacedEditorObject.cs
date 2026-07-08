using UnityEngine;

/// <summary>
/// Attached to every object placed in the editor scene.
/// Holds configuration properties (scale, rotation, trigger target link)
/// which are dynamically edited in UI and serialized to JSON.
/// </summary>
public class PlacedEditorObject : MonoBehaviour
{
    [Header("Asset Info")]
    public string assetTypeName;
    [Tooltip("Optional custom name to display in the editor UI when this object is selected (e.g. 'Player' instead of 'PlayerStart').")]
    public string customToolDisplayName;

    [Header("Properties (Adjusted via UI Properties Panel)")]
    [Tooltip("Optionally drag in a specific project prefab to spawn during playtests instead of the global registry default.")]
    public GameObject customPlaytestPrefab;

    public string moveDir = "Down"; // For spawner/pingpong directions: Up, Down, Left, Right
    public float speed = 3f;
    public float delay = 1f;

    [Header("Trigger Wiring Links")]
    public bool hasTarget = false;
    public PlacedEditorObject targetObject;

    // Reference to visual wire connection in Edit Mode
    [HideInInspector] public LineRenderer wireLine;

    // Helper to get relative transformation data for saving
    public CustomTileData ToTileData()
    {
        return new CustomTileData
        {
            type = assetTypeName,
            position = new Vector2S(transform.position),
            scale = new Vector2S(transform.localScale),
            rotation = transform.eulerAngles.z
        };
    }

    public CustomTrapData ToTrapData()
    {
        CustomTrapData data = new CustomTrapData
        {
            type = assetTypeName,
            spawnPos = new Vector2S(transform.position),
            scale = new Vector2S(transform.localScale),
            rotation = transform.eulerAngles.z,
            moveDir = moveDir,
            speed = speed,
            delay = delay,
            hasTarget = hasTarget
        };

        if (hasTarget && targetObject != null)
        {
            data.targetPos = new Vector2S(targetObject.transform.position);
        }

        var ct = GetComponent<CollisionsAndTriggers>();
        if (ct != null)
        {
            data.activateOnStart = ct.activateOnStart;
            data.triggerTypeStr = ct.triggerType.ToString();
            data.componentActionStr = ct.componentAction.ToString();
            data.setObjectActive = ct.setObjectActive;
            data.activationModeStr = ct.activationMode.ToString();
            data.enableMove = ct.enableMove;
            data.moveDirectionStr = ct.moveDirection.ToString();
            data.moveSpeed = ct.moveSpeed;
            data.stopMoveOnExit = ct.stopMoveOnExit;
            data.enableRotation = ct.enableRotation;
            data.rotationDirectionStr = ct.rotationDirection.ToString();
            data.rotationSpeed = ct.rotationSpeed;
            data.stopRotationOnExit = ct.stopRotationOnExit;
            data.useLocalCoordinates = ct.useLocalCoordinates;
            data.targetPosition = new Vector2S(ct.targetPosition);
            data.targetMoveSpeed = ct.targetMoveSpeed;
            data.moveStaggerInterval = ct.moveStaggerInterval;
            data.moveOnXOnly = ct.moveOnXOnly;
            data.moveOnYOnly = ct.moveOnYOnly;
            data.preserveRelativeDistance = ct.preserveRelativeDistance;
            data.teleportPosition = new Vector2S(ct.teleportPosition);
            data.useTargetX = ct.useTargetX;
            data.useTargetY = ct.useTargetY;
            data.newGravityScale = ct.newGravityScale;
            data.fallSpeedMultiplier = ct.fallSpeedMultiplier;
            data.applyOnEnter = ct.applyOnEnter;
            data.resetOnExit = ct.resetOnExit;
            data.newMaxJumpsValue = ct.newMaxJumpsValue;
            data.triggerDelay = ct.triggerDelay;
            data.deleteTriggerZone = ct.deleteTriggerZone;
            data.modifyColliderState = ct.modifyColliderState;
            data.makeSolid = ct.makeSolid;
            data.modifyGravityState = ct.modifyGravityState;
            data.makeSubjectToGravity = ct.makeSubjectToGravity;
            data.appearOnTrigger = ct.appearOnTrigger;
            data.playAudioOnTrigger = ct.playAudioOnTrigger;
            data.audioClipName = ct.audioClipName;
            data.loopAudio = ct.loopAudio;

            // Camera Shake settings
            data.enableCameraShake = ct.enableCameraShake;
            data.playShakeSFX = ct.playShakeSFX;
            data.cameraShakeIntensity = ct.cameraShakeIntensity;
            data.cameraShakeFrequency = ct.cameraShakeFrequency;
            data.stopShakeOnExitBoundary = ct.stopShakeOnExitBoundary;

            // Serialize references as spawn positions
            if (ct.objectToModify != null)
            {
                data.objectToModifyPos = new Vector2S(ct.objectToModify.transform.position);
            }
            if (ct.destinationTargetObject != null)
            {
                data.destinationTargetPos = new Vector2S(ct.destinationTargetObject.transform.position);
            }
            if (ct.objectsToTrigger != null)
            {
                foreach (var obj in ct.objectsToTrigger)
                {
                    if (obj != null)
                    {
                        data.objectsToTriggerPositions.Add(new Vector2S(obj.transform.position));
                    }
                }
            }
            if (ct.activationObjects != null)
            {
                foreach (var obj in ct.activationObjects)
                {
                    if (obj != null)
                    {
                        data.activationObjectsPositions.Add(new Vector2S(obj.transform.position));
                    }
                }
            }
        }

        return data;
    }
}
