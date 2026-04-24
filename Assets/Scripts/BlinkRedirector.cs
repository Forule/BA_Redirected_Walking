using UnityEngine;
using Wave.Essence.Eye;

public class RedirectedWalkingManager : MonoBehaviour
{
    #region Variablen Deklaration
    [Header("General Setup")]
    public Transform environmentRoot;
    public Transform xrRig;
    public Transform hmd;
    public BlinkBlackout blackout;

    [Header("Blink Redirection (° pro Blink)")]
    public float blinkRotationAngle = 0f;
    [Range(0f, 1f)] public float blinkThreshold = 0.3f;
    public float minBlinkInterval = 0.25f;

    [Header("Blackout-Optionen")]
    public bool blackoutOnBlinkEvenIfNoRotation = true;

    [Header("Walking Redirection (° pro Meter)")]
    public float walkingRotationAngle = 0f;
    public float minMoveDistance = 0.01f;
    public float teleportThresholdMeters = 0.3f;

    // --- GEÄNDERT: Variablen für deine Blink-Logik ---
    private bool isWaitingForOpen = false;
    private float lastBlinkTime = -999f;
    private bool prevIsBlink = false; // Wird für die Zählung der Blinks benötigt

    private Vector3 lastHmdPosXZ;
    private bool runActive = false;
    public bool redirectionEnabled { get; private set; } = true;

    public int BlinkDetectedCount { get; private set; }
    public int BlinkAppliedCount { get; private set; }
    private float runBlinkStartTime = 0f;
    #endregion

    void Awake()
    {
        if (!xrRig) xrRig = transform;
        if (!hmd && Camera.main) hmd = Camera.main.transform;
    }

    void Start() { SyncLastHmdPos(); }

    void Update()
    {
        if (!redirectionEnabled || !runActive)
        {
            SyncLastHmdPos();
            return;
        }
        HandleBlinkRedirection();
        HandleWalkingRedirection();
    }

    // --- GEÄNDERT: Deine bevorzugte Blink-Erkennungslogik ---
    void HandleBlinkRedirection()
    {
        var em = EyeManager.Instance;
        if (em == null || !em.IsEyeTrackingAvailable()) return;

        bool isBlinkingNow = em.GetLeftEyeOpenness(out float l) && em.GetRightEyeOpenness(out float r) && l < blinkThreshold && r < blinkThreshold;

        // Rising Edge für die Zählung der erkannten Blinks
        if (!prevIsBlink && isBlinkingNow)
        {
            BlinkDetectedCount++;
        }

        // Aktions-Logik basierend auf deinem Code
        if (!isWaitingForOpen && isBlinkingNow && (Time.time - lastBlinkTime > minBlinkInterval))
        {
            // Rotation anwenden, falls ein Winkel gesetzt ist
            if (!Mathf.Approximately(blinkRotationAngle, 0f))
            {
                ApplyWorldYaw(blinkRotationAngle);
                BlinkAppliedCount++;
            }

            // Blackout auslösen, falls gewünscht
            if (blackout != null && (blackoutOnBlinkEvenIfNoRotation || !Mathf.Approximately(blinkRotationAngle, 0f)))
            {
                blackout.TriggerBlackout();
            }

            lastBlinkTime = Time.time;
            isWaitingForOpen = true;
        }

        // Zustand zurücksetzen, wenn Augen wieder offen sind
        if (isWaitingForOpen && !isBlinkingNow)
        {
            isWaitingForOpen = false;
        }

        prevIsBlink = isBlinkingNow;
    }

    void HandleWalkingRedirection()
    {
        if (!hmd) return;

        Vector3 nowXZ = new Vector3(hmd.position.x, 0f, hmd.position.z);
        float dist = Vector3.Distance(nowXZ, lastHmdPosXZ);

        if (dist > teleportThresholdMeters)
        {
            lastHmdPosXZ = nowXZ;
            return;
        }

        if (dist > minMoveDistance && !Mathf.Approximately(walkingRotationAngle, 0f))
        {
            float deltaYaw = walkingRotationAngle * dist;
            ApplyWorldYaw(deltaYaw);
        }

        // Wichtig: Position nach jeder Prüfung aktualisieren, um korrekte Distanz zu gewährleisten
        lastHmdPosXZ = nowXZ;
    }

    void ApplyWorldYaw(float deltaYawDeg)
    {
        if (!environmentRoot)
        {
            Debug.LogError("RedirectedWalkingManager: EnvironmentRoot ist nicht zugewiesen!");
            return;
        }
        Vector3 pivot = hmd ? hmd.position : xrRig.position;
        environmentRoot.RotateAround(pivot, Vector3.up, deltaYawDeg);
    }

    public void SetBlinkRotationAngle(float angleDegSigned) { blinkRotationAngle = angleDegSigned; }
    public void SetWalkingRotationAngle(float degPerMeterSigned) { walkingRotationAngle = degPerMeterSigned; }

    public void PauseRedirection()
    {
        redirectionEnabled = false;
        runActive = false;
    }

    public void ResumeRedirection()
    {
        redirectionEnabled = true;
        SyncLastHmdPos();
    }

    // --- GEÄNDERT: Setzt jetzt die korrekten Blink-Variablen zurück ---
    public void BeginRunTracking()
    {
        BlinkDetectedCount = 0;
        BlinkAppliedCount = 0;
        runBlinkStartTime = Time.time;

        isWaitingForOpen = false;
        prevIsBlink = false;
        lastBlinkTime = -999f;

        SyncLastHmdPos();
        runActive = true;
    }

    public void EndRunTracking(out int detected, out int applied, out float seconds)
    {
        detected = BlinkDetectedCount;
        applied = BlinkAppliedCount;
        seconds = Mathf.Max(0f, Time.time - runBlinkStartTime);
        runActive = false;
    }

    // --- GEÄNDERT: Setzt jetzt die korrekten Blink-Variablen zurück ---
    public void NotifyWorldReset()
    {
        SyncLastHmdPos();
        isWaitingForOpen = false;
        prevIsBlink = false;
        lastBlinkTime = -999f;
    }

    void SyncLastHmdPos()
    {
        if (hmd)
            lastHmdPosXZ = new Vector3(hmd.position.x, 0f, hmd.position.z);
    }
}