 using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using ResearchSweet.Transport;
using ResearchSweet.Transport.Helpers;
using ResearchSweet;
using System.Collections.Generic;
using ResearchSweet.Transport.Server;
using System.Linq;



public class RDWExperimentManager : MonoBehaviour
{
    //Player World
    public GameObject xrRig;
    public Transform cameraOffset;
    public Transform mainCamera;
    public GameObject environmentRoot;
    public Transform startPoint;
    private List<GameObject> allCoins;
    private Vector3 startRigPosition, startWorldPosition;
    private Quaternion startRigRotation, startWorldRotation;

    //UI
    public GameObject startButtonObj;
    public GameObject overlayPanel;
    public TMP_Text overlayText;
    public GameObject afcPanel;
    public TMP_Text questionText;
    public TMP_Text feedbackText;

    //Likert
    public Slider zusatzSlider1;
    public Slider zusatzSlider2;
    public TMP_Text zusatzSlider1ValueText;
    public TMP_Text zusatzSlider2ValueText;
    public GameObject finishButton;

    //Redirection
    public RedirectedWalkingManager redirectionManager;
    public GameObject endCollider;
    public float minAngle = 0.5f;
    public float maxAngle = 5.0f;
    public float currentAngle = 0.5f;
    public float initialGain = 1.0f;
    public float gainStep = 0.10f;
    public float minGain = 1.0f;
    public float maxGain = 2.0f;
    public ResearchSweetInitComponent researchSweetInit;

    private int blinkDirection = 1;
    private int currentTrial = 0;
    private int manipulatedRun;
    private bool experimentStarted = false;
    private int? user2AFCChoice = null;

    //Staircase & Reverse Staircase
    public enum ExperimentPhase { Staircase, ReverseStaircase }
    private ExperimentPhase currentPhase = ExperimentPhase.Staircase;
    private float staircaseStep = 0.5f;  // Start-Gain
    private float reverseStep = 2.0f;    // Reverse Staircase gain
    private int umkehrpunktCount = 0;
    private int lastAnswer = -1;         // 1 = richtig, 0 = falsch
    private List<float> jndList = new List<float>();
    private float userJND = 0f;
    private int trialInPhase = 0;
    private int minUmkehrpunkte = 6;

    void Start()
    {
        // Start Position
        startRigPosition = xrRig.transform.position;
        startRigRotation = xrRig.transform.rotation;
        startWorldPosition = environmentRoot.transform.position;
        startWorldRotation = environmentRoot.transform.rotation;

        allCoins = new List<GameObject>(GameObject.FindGameObjectsWithTag("Coin"));
        startButtonObj.SetActive(true);
        overlayPanel.SetActive(false);
        afcPanel.SetActive(false);
        feedbackText.text = "";
        experimentStarted = false;

        if (endCollider != null) endCollider.SetActive(false);

        //Likert Skalen
        zusatzSlider1.gameObject.SetActive(false);
        zusatzSlider2.gameObject.SetActive(false);
        zusatzSlider1ValueText.gameObject.SetActive(false);
        zusatzSlider2ValueText.gameObject.SetActive(false);
        finishButton.SetActive(false);
        zusatzSlider1.value = 1;
        zusatzSlider2.value = 1;
    }

    public void OnReachedEndOfRun1()
    {
        if (endCollider != null) endCollider.SetActive(false);
        ResetWorldRelativeToRig();
        StartCoroutine(Run2AfterOverlay());
    }

    IEnumerator Run2AfterOverlay()
    {
        yield return ShowOverlayAndStartRun(2);
        StartRun2();
    }

    public void OnReachedEndOfRun2()
    {
        ResetWorldRelativeToRig();
        StartCoroutine(Show2AFCPanel());
    }

    public void ResetWorldRelativeToRig()
    {
        environmentRoot.transform.position = startWorldPosition;
        environmentRoot.transform.rotation = startWorldRotation;
        xrRig.transform.position = startRigPosition;
        xrRig.transform.rotation = startRigRotation;
        var cam = xrRig.GetComponentInChildren<Camera>().transform;
        Vector3 headOffset = startRigPosition - cam.position;
        xrRig.transform.position += headOffset;
        foreach (var coin in allCoins) coin.SetActive(true);
    }

    public void OnStartButtonClicked()
    {
        startButtonObj.SetActive(false);
        experimentStarted = true;
        currentPhase = ExperimentPhase.Staircase; // Reset
        umkehrpunktCount = 0;
        lastAnswer = -1;
        jndList.Clear();
        currentTrial = 0;
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    IEnumerator ShowOverlayAndStartRun(int runNumber)
    {
        overlayPanel.SetActive(true);
        overlayText.text = $"Durchgang {runNumber} startet gleich...";
        yield return new WaitForSeconds(5f);
        overlayPanel.SetActive(false);
        if (runNumber == 1) StartRun1();
    }

    void StartRun1()
    {
        if (endCollider != null) endCollider.SetActive(true);
        manipulatedRun = Random.Range(1, 3);
        if (manipulatedRun == 1)
        {
            blinkDirection = (Random.value > 0.5f) ? 1 : -1;
            redirectionManager.blinkRotationAngle = currentAngle * blinkDirection;
        }
        else
        {
            redirectionManager.blinkRotationAngle = 0f;
        }
    }

    void StartRun2()
    {
        if (manipulatedRun == 2)
        {
            blinkDirection = (Random.value > 0.5f) ? 1 : -1;
            redirectionManager.blinkRotationAngle = currentAngle * blinkDirection;
        }
        else
        {
            redirectionManager.blinkRotationAngle = 0f;
        }
        StartCoroutine(Show2AFCPanel());
    }

    IEnumerator Show2AFCPanel()
    {
        afcPanel.SetActive(true);
        questionText.text = "In welchem Durchgang war die Manipulation?";
        zusatzSlider1.gameObject.SetActive(false);
        zusatzSlider2.gameObject.SetActive(false);
        zusatzSlider1ValueText.gameObject.SetActive(false);
        zusatzSlider2ValueText.gameObject.SetActive(false);
        finishButton.SetActive(false);
        user2AFCChoice = null;
        yield break;
    }

    // AFC-Button Methoden
    public void OnChoose1()
    {
        user2AFCChoice = 1;
        ZeigeZusatzfragen();
    }
    public void OnChoose2()
    {
        user2AFCChoice = 2;
        ZeigeZusatzfragen();
    }

    void ZeigeZusatzfragen()
    {
        zusatzSlider1.gameObject.SetActive(true);
        zusatzSlider2.gameObject.SetActive(true);
        zusatzSlider1ValueText.gameObject.SetActive(true);
        zusatzSlider2ValueText.gameObject.SetActive(true);
        finishButton.SetActive(true);
        UpdateSliderValueText1();
        UpdateSliderValueText2();
    }

    // Slider
    public void UpdateSliderValueText1()
    {
        zusatzSlider1ValueText.text = zusatzSlider1.value.ToString("0");
    }
    public void UpdateSliderValueText2()
    {
        zusatzSlider2ValueText.text = zusatzSlider2.value.ToString("0");
    }

    public void OnFinishQuestionnaire()
    {
        if (user2AFCChoice == null)
        {
            Debug.LogWarning("Bitte zuerst eine AFC-Auswahl treffen!");
            return;
        }
        int likert1 = (int)zusatzSlider1.value;
        int likert2 = (int)zusatzSlider2.value;
        afcPanel.SetActive(false);
        EvaluateAnswer(user2AFCChoice.Value, likert1, likert2);
        user2AFCChoice = null;
        zusatzSlider1.value = 1;
        zusatzSlider2.value = 1;
    }

    void EvaluateAnswer(int choice, int zusatz1, int zusatz2)
    {
        bool correct = (choice == manipulatedRun);
        int currentAnswer = correct ? 1 : 0;

        // STAIRCASE
        if (currentPhase == ExperimentPhase.Staircase)
        {
            if (lastAnswer != -1 && currentAnswer != lastAnswer)
            {
                umkehrpunktCount++;
                jndList.Add(currentAngle);
                Debug.Log("Umkehrpunkt #" + umkehrpunktCount + ", Schwelle: " + currentAngle);
            }

            if (correct)
                currentAngle = Mathf.Max(minAngle, currentAngle - staircaseStep);
            else
                currentAngle = Mathf.Min(maxAngle, currentAngle + staircaseStep);

            if (umkehrpunktCount >= minUmkehrpunkte)
            {
                userJND = jndList.Count > 0 ? jndList.Average() : currentAngle;
                StarteReverseStaircase();
                return;
            }
        }
        // REVERSE STAIRCASE
        else if (currentPhase == ExperimentPhase.ReverseStaircase)
        {
            trialInPhase++;
            // gain halbieren

            if (trialInPhase % 3 == 0)
                reverseStep = Mathf.Max(0.1f, reverseStep * 0.5f);

            if (correct)
                currentAngle = Mathf.Max(minAngle, currentAngle - reverseStep);
            else
                currentAngle = Mathf.Min(maxAngle, currentAngle + reverseStep);
        }

        lastAnswer = currentAnswer;

        // ResearchSweet
        var rsClient = ResearchSweetHelpers.Client;

        var eventData = new Dictionary<string, object>();
        eventData.Add("trialIndex", currentTrial);
        eventData.Add("manipulatedRun", manipulatedRun);
        eventData.Add("userChoice", choice);
        eventData.Add("isCorrect", correct);
        eventData.Add("currentAngle", currentAngle);
        eventData.Add("likert1", zusatz1);
        eventData.Add("likert2", zusatz2);
        eventData.Add("timestamp", System.DateTime.Now.ToString("o"));

        rsClient.SendAsync<IEventControl>(x => x.SendEventAsync("TrialEvent", eventData));


        currentTrial++;
        StartCoroutine(NextTrialAfterDelay());
    }

    void StarteReverseStaircase()
    {
        currentPhase = ExperimentPhase.ReverseStaircase;
        currentAngle = userJND;
        reverseStep = 2.0f;
        trialInPhase = 0;
        overlayPanel.SetActive(true);
        overlayText.text = $"Zweite Phase (Reverse Staircase) startet!\nDeine Schwelle: {userJND:F2}°";
        StartCoroutine(CloseOverlayAndNextTrial());
    }

    IEnumerator CloseOverlayAndNextTrial()
    {
        yield return new WaitForSeconds(3f);
        overlayPanel.SetActive(false);
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    IEnumerator NextTrialAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (currentTrial >= 30)
        {
            float meanJND = jndList.Count > 0 ? jndList.Average() : currentAngle;
            overlayPanel.SetActive(true);
            overlayText.text = $"Experiment beendet.\nJND: {meanJND:F2}°";
            yield break;
        }
        ResetWorldRelativeToRig();
        StartCoroutine(ShowOverlayAndStartRun(1));
    }
}
