using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EndCollider : MonoBehaviour
{
    public RDWExperimentManager experimentManager;

    [Tooltip("1 = Run 1, 2 = Run 2")]
    [SerializeField] private int runNumber = 1;

    [Header("Trigger-Filter")]
    [Tooltip("Tag des Spielers (am XR-Rig-Root oder am Kollisions-Body).")]
    public string playerTag = "Player";

    [Tooltip("Nach dem Treffer diesen Collider automatisch deaktivieren.")]
    public bool autoDisableAfterHit = true;

    private bool triggered = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        triggered = false; // bei Re-Aktivierung neu „scharf“
    }

    public void SetRunNumber(int value)
    {
        runNumber = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || experimentManager == null) return;

        // Tag am getroffenen Collider ODER am Root prüfen
        if (!other.CompareTag(playerTag) && !other.transform.root.CompareTag(playerTag))
            return;

        triggered = true;
        Debug.Log($"EndCollider Hit (Run {runNumber}) by {other.name}");

        if (runNumber == 1)
            experimentManager.OnReachedEndOfRun1();
        else
            experimentManager.OnReachedEndOfRun2();

        if (autoDisableAfterHit)
            gameObject.SetActive(false);
    }
}
