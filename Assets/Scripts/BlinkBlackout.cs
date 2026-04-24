using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkBlackout : MonoBehaviour
{
    [Tooltip("UI-Image, das den Bildschirm abdeckt (vollflächiges schwarzes Panel).")]
    public Image blackoutPanel;

    [Tooltip("Standard-Dauer in Sekunden, wenn TriggerBlackout() genutzt wird. 0.4 = 400 ms.")]
    public float blackoutDuration = 0.4f;

    void Awake()
    {
        if (blackoutPanel != null) blackoutPanel.enabled = false; // Start: aus
    }

    // Alte API – kompatibel zu deinem bisherigen Code
    public void TriggerBlackout()
    {
        if (!isActiveAndEnabled || blackoutPanel == null) return;
        StopAllCoroutines();
        StartCoroutine(BlackoutSeconds(blackoutDuration));
    }

    // Neue API – kompatibel zu RedirectedWalkingManager (ShowFor(ms))
    public void ShowFor(int ms)
    {
        if (!isActiveAndEnabled || blackoutPanel == null) return;
        StopAllCoroutines();
        StartCoroutine(BlackoutSeconds(ms / 1000f));
    }

    IEnumerator BlackoutSeconds(float seconds)
    {
        blackoutPanel.enabled = true;
        yield return new WaitForSecondsRealtime(seconds); // unabhängig von Time.timeScale
        blackoutPanel.enabled = false;
    }
}