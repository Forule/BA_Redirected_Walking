using UnityEngine;
using Wave.Essence.Eye;

public class RedirectedWalkingManager : MonoBehaviour
{
    [Header("General Setup")]
    // Der Kommentar wurde angepasst, um die Wichtigkeit dieser Variable zu betonen.
    public Transform environmentRoot;       // Entscheidend für die Redirection!
    public Transform xrRig;                 // dein XR Rig (Root des Players)
    public Transform hmd;                   // HMD/MainCamera Transform (Pivot)
    public BlinkBlackout blackout;

    // ... (der Rest der Variablendeklaration bleibt unverändert)

    [Header("Blink Redirection (° pro Blink)")]
    [Tooltip("Bereits signierter Winkel in Grad pro Blink (vom ExperimentManager gesetzt).")]
    public float blinkRotationAngle = 0f;
    [Range(0f, 1f)] public float blinkThreshold = 0.3f;
    public float minBlinkInterval = 0.25f;

    [Header("Blackout-Option")]
    [Tooltip("Auch im neutralen/Waking-Run Blackout beim Blink zeigen, selbst wenn blinkRotationAngle = 0.")]
    public bool blackoutOnBlinkEvenIfNoRotation = true;

    [Header("Walking Redirection (° pro Meter)")]
    [Tooltip("Bereits signierter Gain in Grad pro realem Meter (vom ExperimentManager gesetzt).")]
    public float walkingRotationAngle = 0f;      // °/m
    public float minMoveDistance = 0.01f;        // Rauschen filtern
    public float teleportThresholdMeters = 0.3f; // Sprünge (Reset) ignorieren

    // intern
    private Vector3 lastHmdPosXZ;
    private bool isWaitingForOpen = false;
    private float lastBlinkTime = 0f;
    private bool prevIsBlink = false;

    public bool redirectionEnabled { get; private set; } = true;

    // Blink-Tracking (pro Run)
    public int BlinkDetectedCount { get; private set; }
    public int BlinkAppliedCount { get; private set; }
    private float runBlinkStartTime = 0f;

    void Awake()
    {
        if (!xrRig) xrRig = transform;
        if (!hmd && Camera.main) hmd = Camera.main.transform;
    }

    void Start()
    {
        SyncLastHmdPos();
    }

    void Update()
    {
        if (!redirectionEnabled)
        {
            SyncLastHmdPos();
            return;
        }

        HandleBlinkRedirection();
        HandleWalkingRedirection();
    }

    void HandleBlinkRedirection()
    {
        // ... (Dieser Abschnitt bleibt komplett unverändert)
        if (EyeManager.Instance == null || !EyeManager.Instance.IsEyeTrackingAvailable()) { /*...*/ return; }
        // ...
        if (!Mathf.Approximately(blinkRotationAngle, 0f))
        {
            // NAME ZURÜCKGEÄNDERT: Ruft wieder die Funktion mit dem alten Namen auf.
            ApplyRigYaw(blinkRotationAngle);
            BlinkAppliedCount++;
        }
        // ...
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

        if (!Mathf.Approximately(walkingRotationAngle, 0f) && dist > minMoveDistance)
        {
            float deltaYaw = walkingRotationAngle * dist;

            // NAME ZURÜCKGEÄNDERT: Ruft wieder die Funktion mit dem alten Namen auf.
            ApplyRigYaw(deltaYaw);

            lastHmdPosXZ = nowXZ;
        }
    }

    // -------- NAME ZURÜCKGEÄNDERT, ABER LOGIK BLEIBT KORREKT --------
    // Die Funktion heißt wieder "ApplyRigYaw", rotiert aber weiterhin die Welt.
    void ApplyRigYaw(float deltaYawDeg)
    {
        if (!environmentRoot)
        {
            Debug.LogError("RedirectedWalkingManager: EnvironmentRoot ist nicht zugewiesen! Redirection kann nicht angewendet werden.");
            return;
        }

        Vector3 pivot = hmd.position;

        // Die entscheidende Logik bleibt: Rotiere die Welt UM den Spieler.
        environmentRoot.RotateAround(pivot, Vector3.up, deltaYawDeg);
    }

    // -------- API für ExperimentManager (unverändert) --------
    public void SetBlinkRotationAngle(float angleDegSigned) { blinkRotationAngle = angleDegSigned; }
    public void SetWalkingRotationAngle(float degPerMeterSigned) { walkingRotationAngle = degPerMeterSigned; }

    public void PauseRedirection()
    {
        redirectionEnabled = false;
        // ... (Rest der Funktion unverändert)
    }

    public void ResumeRedirection()
    {
        redirectionEnabled = true;
        SyncLastHmdPos();
    }

    public void BeginRunTracking()
    {
        BlinkDetectedCount = 0;
        BlinkAppliedCount = 0;
        runBlinkStartTime = Time.time;
        isWaitingForOpen = false;
        prevIsBlink = false;
        lastBlinkTime = Time.time;
        SyncLastHmdPos();
    }

    public void EndRunTracking(out int detected, out int applied, out float seconds)
    {
        detected = BlinkDetectedCount;
        applied = BlinkAppliedCount;
        seconds = Mathf.Max(0f, Time.time - runBlinkStartTime);
    }

    public void NotifyWorldReset()
    {
        SyncLastHmdPos();
        isWaitingForOpen = false;
        prevIsBlink = false;
        lastBlinkTime = Time.time;
    }

    void SyncLastHmdPos()
    {
        if (hmd)
            lastHmdPosXZ = new Vector3(hmd.position.x, 0f, hmd.position.z);
        else
            lastHmdPosXZ = Vector3.zero;
    }
}