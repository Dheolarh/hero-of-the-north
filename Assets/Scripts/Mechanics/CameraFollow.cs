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

    private Vector3 logicalPosition;
    private CameraShake cameraShake;

    void Start()
    {
        cameraShake = GetComponent<CameraShake>();
        logicalPosition = transform.position;
    }

    void LateUpdate()
    {
        if (player == null || !isFollowing) return;

        Vector3 desiredPosition = player.position + offset;

        Vector3 targetPosition = new Vector3(
            followX ? desiredPosition.x : logicalPosition.x,
            followY ? desiredPosition.y : logicalPosition.y,
            followZ ? desiredPosition.z : logicalPosition.z
        );

        if (useSmoothing)
        {
            logicalPosition = Vector3.Lerp(logicalPosition, targetPosition, smoothSpeed * Time.deltaTime);
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
}
