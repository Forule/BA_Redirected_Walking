using UnityEngine;
using Wave.Essence.Eye;

public class RedirectedWalkingManager : MonoBehaviour
{
    [Header("General Setup")]
    public Transform environmentRoot;   // Muss NICHT Parent des XR-Rigs sein
    public Transform xrRig;
    public Transform hmd;               // HMD/MainCamera Transform
    public BlinkBlackout blackout;

    [Header("Blink Redirection (° pro Blink)")]
    [Tooltip("Signierter Winkel in Grad pro Blink (vom ExperimentManager gesetzt).")]
    public float blinkRotationAngle = 0f;
    [Range(0f, 1f)] public float blinkThreshold = 0.3f; // Openness-Schwelle (0=zu,1=offen)
    public float minBlinkInterval = 0.25f;              // Debounce (Sek.)

    [Header("Blackout-Optionen")]
    [Tooltip("Auch bei blinkRotationAngle = 0 Blackout nach dem Blink zeigen.")]
    public bool blackoutOnBlinkEvenIfNoRotation = true;
    [Tooltip("Dauer Blackout NACH dem Blink (ms).")]
    public int postBlinkBlackoutMs = 200;

    [Header("Walking Redirection (° pro Meter)")]
    [Tooltip("Signierter Gain in Grad pro realem Meter (vom ExperimentManager gesetzt).")]
    public float walkingRotationAngle = 0f;     // °/m
    public float minMoveDistance = 0.01f;       // Bewegungsrauschen filtern
    public float teleportThresholdMeters = 0.3f;// Resets/Sprünge ignorieren

    // intern
    private Vector3 lastHmdPosXZ;
    private bool prevIsBlink = false;
    private bool isWaitingForOpen = false;
    private float lastBlinkTime = 0f;

    private bool runActive = false;             // wird in BeginRunTracking gesetzt

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

    void Start() { SyncLastHmdPos(); }

    void Update()
    {
        // Erst laufen lassen, wenn ExperimentManager den Run wirklich gestartet hat
        if (!redirectionEnabled || !runActive)
        {
            SyncLastHmdPos();
            return;
        }

        HandleBlinkRedirection();
        HandleWalkingRedirection();
    }

    // --- Blink: Rotation im Blink, Blackout NACH dem Blink ---
    void HandleBlinkRedirection()
    {
        var em = EyeManager.Instance;
        if (em == null || !em.IsEyeTrackingAvailable())
        {
            prevIsBlink = false;
            return;
        }

        float leftOpen, rightOpen;
        bool leftValid = em.GetLeftEyeOpenness(out leftOpen);
        bool rightValid = em.GetRightEyeOpenness(out rightOpen);
        if (!leftValid || !rightValid)
        {
            prevIsBlink = false;
            return;
        }

        bool isBlink = (leftOpen < blinkThreshold) && (rightOpen < blinkThreshold);
        float now = Time.time;

        // Rising edge (Blink beginnt)
        if (!prevIsBlink && isBlink)
        {
            BlinkDetectedCount++;
            lastBlinkTime = now;
            // Rotation EINMAL zu Beginn des Blinks -> komplett verdeckt
            if (!Mathf.Approximately(blinkRotationAngle, 0f))
            {
                ApplyRigYaw(blinkRotationAngle); // ° pro Blink (signed)
                BlinkAppliedCount++;
            }
        }

        // Falling edge (Blink endet) -> kurzer Blackout danach (Debounce beachtet)
        if (prevIsBlink && !isBlink && (now - lastBlinkTime) >= minBlinkInterval)
        {
            if (blackout != null && (blackoutOnBlinkEvenIfNoRotation || !Mathf.Approximately(blinkRotationAngle, 0f)))
            {
                // Unterstütze beide möglichen APIs
                // Prefer: ShowFor(ms); Fallback: TriggerBlackout()
                try { blackout.SendMessage("ShowFor", postBlinkBlackoutMs, SendMessageOptions.DontRequireReceiver); }
                catch { /* ignored */ }
                try { blackout.SendMessage("TriggerBlackout", SendMessageOptions.DontRequireReceiver); }
                catch { /* ignored */ }
            }
        }

        prevIsBlink = isBlink;
    }

    // --- Walking: kontinuierliche Rotation proportional zur Wegstrecke ---
    void HandleWalkingRedirection()
    {
        if (!hmd) return;

        Vector3 nowXZ = new Vector3(hmd.position.x, 0f, hmd.position.z);
        float dist = Vector3.Distance(nowXZ, lastHmdPosXZ);

        // Teleports/Resets nicht einrechnen
        if (dist > teleportThresholdMeters)
        {
            lastHmdPosXZ = nowXZ;
            return;
        }

        if (dist > minMoveDistance)
        {
            if (!Mathf.Approximately(walkingRotationAngle, 0f))
            {
                float deltaYaw = walkingRotationAngle * dist; // (deg/m) * m
                ApplyRigYaw(deltaYaw);
            }
            // IMMER aktualisieren (auch wenn kein Gain anliegt)
            lastHmdPosXZ = nowXZ;
        }
    }

    // Welt um den HMD-Pivot drehen (Rig bleibt tracking-geführt)
    void ApplyRigYaw(float deltaYawDeg)
    {
        if (!environmentRoot)
        {
            Debug.LogError("RedirectedWalkingManager: EnvironmentRoot ist nicht zugewiesen! Redirection kann nicht angewendet werden.");
            return;
        }
        Vector3 pivot = hmd ? hmd.position : Vector3.zero;
        environmentRoot.RotateAround(pivot, Vector3.up, deltaYawDeg);
    }

    // -------- API für ExperimentManager --------
    public void SetBlinkRotationAngle(float angleDegSigned)
    {
        blinkRotationAngle = angleDegSigned;
        // Mischbetrieb verhindern
        if (!Mathf.Approximately(angleDegSigned, 0f))
            walkingRotationAngle = 0f;
    }

    public void SetWalkingRotationAngle(float degPerMeterSigned)
    {
        walkingRotationAngle = degPerMeterSigned;
        // Mischbetrieb verhindern
        if (!Mathf.Approximately(degPerMeterSigned, 0f))
            blinkRotationAngle = 0f;
    }

    public void PauseRedirection()
    {
        redirectionEnabled = false;
        runActive = false; // Run pausiert
    }

    public void ResumeRedirection()
    {
        redirectionEnabled = true;
        // Noch NICHT runActive setzen – das macht BeginRunTracking(),
        // damit ein vorgezogenes Resume (dein aktueller Flow) keine Effekte hat.
        SyncLastHmdPos();
    }

    public void BeginRunTracking()
    {
        BlinkDetectedCount = 0;
        BlinkAppliedCount = 0;
        runBlinkStartTime = Time.time;

        prevIsBlink = false;
        isWaitingForOpen = false;
        lastBlinkTime = Time.time;

        SyncLastHmdPos();
        runActive = true; // erst jetzt darf Update() rotieren
    }

    public void EndRunTracking(out int detected, out int applied, out float seconds)
    {
        detected = BlinkDetectedCount;
        applied = BlinkAppliedCount;
        seconds = Mathf.Max(0f, Time.time - runBlinkStartTime);
        runActive = false;
    }

    public void NotifyWorldReset()
    {
        SyncLastHmdPos();
        prevIsBlink = false;
        isWaitingForOpen = false;
        lastBlinkTime = Time.time;
        // runActive bleibt unverändert
    }

    void SyncLastHmdPos()
    {
        if (hmd)
            lastHmdPosXZ = new Vector3(hmd.position.x, 0f, hmd.position.z);
        else
            lastHmdPosXZ = Vector3.zero;
    }
}
