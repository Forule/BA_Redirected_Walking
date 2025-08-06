using UnityEngine;
using Wave.Essence.Eye;
using TMPro;

public class RedirectedWalkingManager : MonoBehaviour
{
    [Header("General Setup")]
    public Transform environmentRoot;
    public Transform playerRig;
    public BlinkBlackout blackout;
    [HideInInspector] public int blinkDirection = 1;

    [Header("Blink Redirection")]
    public float blinkRotationAngle = 5f;
    public float blinkThreshold = 0.3f;
    public float minBlinkInterval = 0.25f;

    [Header("Movement Redirection")]
    public float walkingRotationAngle = 0f; // Grad pro Meter (Standard)
    public float minMoveDistance = 0.01f;
    private Vector3 lastPlayerPos;

    private bool isWaitingForOpen = false;
    private float lastBlinkTime = 0f;

    void Start()
    {
        lastPlayerPos = playerRig.position;
    }

    void Update()
    {
        // Blink Redirection
        if (EyeManager.Instance != null && EyeManager.Instance.IsEyeTrackingAvailable())
        {
            float leftOpenness, rightOpenness;
            bool leftValid = EyeManager.Instance.GetLeftEyeOpenness(out leftOpenness);
            bool rightValid = EyeManager.Instance.GetRightEyeOpenness(out rightOpenness);

            if (leftValid && rightValid)
            {
                bool isBlink = leftOpenness < blinkThreshold && rightOpenness < blinkThreshold;
                if (!isWaitingForOpen && isBlink && (Time.time - lastBlinkTime) > minBlinkInterval)
                {
                    RotateEnvironment(blinkRotationAngle * blinkDirection);
                    if (blackout != null)
                        blackout.TriggerBlackout();
                    lastBlinkTime = Time.time;
                    isWaitingForOpen = true;
                }
                if (isWaitingForOpen && !isBlink)
                {
                    isWaitingForOpen = false;
                }
            }
        }

        // Walking Redirection: Standard (Rotation pro zurückgelegtem Meter)
        float dist = Vector3.Distance(playerRig.position, lastPlayerPos);

        if (Mathf.Abs(walkingRotationAngle) > 0f && dist > minMoveDistance)
        {
            float rotationThisFrame = walkingRotationAngle * dist; // z.B. 2 Grad pro Meter
            RotateEnvironment(rotationThisFrame);
        }

        lastPlayerPos = playerRig.position;
    }

    // World Rotation
    void RotateEnvironment(float angle)
    {
        Vector3 playerPos = playerRig.position;
        environmentRoot.RotateAround(playerPos, Vector3.up, angle);
    }

    // Set Direction and Gain
    public void SetBlinkDirection(int direction)
    {
        blinkDirection = direction;
    }
    public void SetBlinkRotationAngle(float angle)
    {
        blinkRotationAngle = angle;
    }
}
 