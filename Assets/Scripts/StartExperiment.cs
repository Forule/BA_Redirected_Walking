using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StartExperiment : MonoBehaviour
{
public GameObject overlayPanel;
public TMP_Text overlayText;
public Button startButton;

    // Start is called before the first frame update
    void Start()
    {
    overlayPanel.SetActive(false);
    startButton.gameObject.SetActive(true);
    }

    IEnumerator ShowOverlayWithDelay(int run)
{
    overlayText.text = $"Durchgang {run} startet gleich...";
    overlayPanel.SetActive(true);
    yield return new WaitForSeconds(5f);
    overlayPanel.SetActive(false);

    StartRun(run);
}

void StartRun(int run)
{

}
}
