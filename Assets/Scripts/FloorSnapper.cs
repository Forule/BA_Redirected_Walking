using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FloorSnapper : MonoBehaviour
{
    [Header("Refs")]
    public Transform xrRig;        // Root der Rig (wird in Y verschoben)
    public Transform mainCamera;   // HMD/Camera
    public LayerMask groundLayer = ~0;
    public float raycastDistance = 5f;
    public float cameraYOffset = 0.2f; // kleiner Offset über der Kamera für den Ray

    [Header("Betrieb")]
    public bool continuousSnap = true;          // jeden Frame aktiv
    public bool respectTrackingOrigin = true;   // bei Floor-Origin nix tun (bei Problemen auf false stellen)

    [Header("Smoothing (sichtbar)")]
    public bool smoothWhenVisible = true;
    public float smoothTime = 0.08f;            // ~80 ms – weich
    public float maxSpeed = 2f;                 // m/s
    public float deadzone = 0.002f;             // 2 mm – verhindert Mikrojitter

    // intern
    private float _velY = 0f;
    private float _invisibleSnapUntil = -1f;
    private CharacterController _cc;

    void Reset()
    {
        if (xrRig == null) xrRig = transform;
        if (mainCamera == null && Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Awake()
    {
        if (xrRig == null) xrRig = transform;
        _cc = xrRig.GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (!continuousSnap) return;
        if (respectTrackingOrigin && IsFloorOrigin()) return;
        if (!TryGetGroundY(out float targetY)) return;

        // Während des „unsichtbaren Fensters“: harter, sofortiger Snap ohne sichtbares Gleiten
        if (Time.time < _invisibleSnapUntil)
        {
            ApplyImmediateY(targetY);
            return;
        }

        if (smoothWhenVisible) ApplySmoothY(targetY);
        else ApplyImmediateY(targetY);
    }

    /// <summary> Direkt & sofort snappen (z.B. vom Experiment-Manager am Reset-Ende). </summary>
    public void SnapOnceImmediate()
    {
        if (respectTrackingOrigin && IsFloorOrigin()) return;
        if (TryGetGroundY(out float targetY)) ApplyImmediateY(targetY);
    }

    /// <summary> Für X Sekunden werden alle Snaps sofort und „unsichtbar“ ausgeführt. </summary>
    public void BeginInvisibleSnapWindow(float seconds = 0.25f)
    {
        _invisibleSnapUntil = Time.time + Mathf.Max(0f, seconds);
        // Beim Start des Fensters direkt einmal anwenden
        SnapOnceImmediate();
    }

    /// <summary> Optional: Referenzen zur Laufzeit setzen. </summary>
    public void SetRigAndHead(Transform rig, Transform head)
    {
        xrRig = rig;
        mainCamera = head;
        _cc = xrRig != null ? xrRig.GetComponent<CharacterController>() : null;
    }

    // ---------------- intern ----------------

    bool TryGetGroundY(out float targetY)
    {
        targetY = 0f;
        if (xrRig == null || mainCamera == null) return false;

        Vector3 origin = mainCamera.position + Vector3.up * cameraYOffset;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            // HMD lokale Höhe relativ zur Rig
            float camLocalY = mainCamera.localPosition.y;
            targetY = hit.point.y - camLocalY;
            return true;
        }
        return false;
    }

    void ApplyImmediateY(float targetY)
    {
        if (xrRig == null) return;

        if (_cc != null)
        {
            bool was = _cc.enabled;
            _cc.enabled = false;
            Vector3 p = xrRig.position; p.y = targetY; xrRig.position = p;
            _cc.enabled = was;
            // Ground-Status sofort aktualisieren (falls CC verwendet wird)
            _cc.Move(Vector3.zero);
        }
        else
        {
            Vector3 p = xrRig.position; p.y = targetY; xrRig.position = p;
        }
    }

    void ApplySmoothY(float targetY)
    {
        float currentY = xrRig.position.y;
        float newY = Mathf.SmoothDamp(currentY, targetY, ref _velY, smoothTime, maxSpeed, Time.deltaTime);
        float deltaY = newY - currentY;

        if (Mathf.Abs(deltaY) < deadzone) return;

        if (_cc != null)
        {
            _cc.Move(new Vector3(0f, deltaY, 0f));
        }
        else
        {
            Vector3 p = xrRig.position; p.y = newY; xrRig.position = p;
        }
    }

    // --- XR-Helpers ---
    bool IsFloorOrigin()
    {
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        for (int i = 0; i < subsystems.Count; i++)
        {
            try
            {
                var mode = subsystems[i].GetTrackingOriginMode();
                if ((mode & TrackingOriginModeFlags.Floor) != 0)
                    return true;
            }
            catch { /* Provider ohne Implementierung */ }
        }
        return false;
    }
}
