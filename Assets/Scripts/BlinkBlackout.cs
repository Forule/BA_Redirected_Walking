using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkBlackout : MonoBehaviour
{
    public Image blackoutPanel;
    public float blackoutDuration = 0.4f; // z. B. 40ms

    public void TriggerBlackout()
    {
        StartCoroutine(BlackoutRoutine());
    }

    IEnumerator BlackoutRoutine()
    {
        blackoutPanel.enabled = true;
        yield return new WaitForSecondsRealtime(blackoutDuration);
        blackoutPanel.enabled = false;
    }
}
