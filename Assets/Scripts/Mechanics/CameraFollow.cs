using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] Transform player;

    [Header("Follow Settings")]
    [SerializeField] bool followX = true;
    [SerializeField] bool followY = true;
    [SerializeField] bool followZ = false;

    [Header("Offset")]
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10);

    [Header("Smoothing")]
    [SerializeField] bool useSmoothing = true;
    [SerializeField] float smoothSpeed = 5f;

    private bool isFollowing = true;

    private LevelCameraSettings activeSettings;
    private Vector3 logicalPosition;
    private CameraShake cameraShake;
    private Camera cam;

    void Start()
    {
        cameraShake = GetComponent<CameraShake>();
        logicalPosition = transform.position;
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null || !isFollowing) return;

        // Read settings dynamically from activeSettings if available, falling back to local defaults
        bool currentFollowX = activeSettings != null ? activeSettings.followX : followX;
        bool currentFollowY = activeSettings != null ? activeSettings.followY : followY;
        Vector3 currentOffset = activeSettings != null ? activeSettings.offset : offset;
        bool currentUseSmoothing = activeSettings != null ? activeSettings.useSmoothing : useSmoothing;
        float currentSmoothSpeed = activeSettings != null ? activeSettings.smoothSpeed : smoothSpeed;
        float currentFixedY = activeSettings != null ? activeSettings.fixedYHeight : (player.position.y + currentOffset.y);

        // Dynamically apply zoom (Orthographic Size) from level settings
        if (cam != null && activeSettings != null)
        {
            cam.orthographicSize = activeSettings.orthoSize;
        }

        Vector3 desiredPosition = player.position + currentOffset;

        Vector3 targetPosition = new Vector3(
            currentFollowX ? desiredPosition.x : logicalPosition.x,
            currentFollowY ? desiredPosition.y : currentFixedY,
            followZ ? desiredPosition.z : logicalPosition.z
        );

        if (currentUseSmoothing)
        {
            logicalPosition = Vector3.Lerp(logicalPosition, targetPosition, currentSmoothSpeed * Time.deltaTime);
        }
        else
        {
            logicalPosition = targetPosition;
        }

        Vector3 finalPosition = logicalPosition;
        if (cameraShake != null)
        {
            finalPosition += cameraShake.CurrentShakeOffset;
        }

        transform.position = finalPosition;
    }

    public void StopFollowing()
    {
        isFollowing = false;
    }

    public void StartFollowing()
    {
        isFollowing = true;
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
        activeSettings = null;

        if (newTarget != null)
        {
            // Find level-specific settings in the spawned prefab hierarchy
            activeSettings = newTarget.GetComponentInParent<LevelCameraSettings>();
            if (activeSettings == null)
            {
                // Fallback: search the active scene
                activeSettings = FindFirstObjectByType<LevelCameraSettings>();
            }

            if (activeSettings != null)
            {
                bool currentFollowX = activeSettings.followX;
                bool currentFollowY = activeSettings.followY;
                Vector3 currentOffset = activeSettings.offset;
                float currentFixedY = activeSettings.fixedYHeight;

                float startX = currentFollowX ? (newTarget.position.x + currentOffset.x) : activeSettings.transform.position.x + currentOffset.x;
                float startY = currentFollowY ? (newTarget.position.y + currentOffset.y) : currentFixedY;
                logicalPosition = new Vector3(startX, startY, newTarget.position.z + currentOffset.z);
            }
            else
            {
                logicalPosition = newTarget.position + offset;
            }

            isFollowing = true;
        }
    }
}
