using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class RDWExperimentManager : MonoBehaviour
{
    // Player World
    public GameObject xrRig;
    public Transform cameraOffset;
    public Transform mainCamera;
    public GameObject environmentRoot;
    public Transform startPoint;
    private List<GameObject> allCoins;
    private Vector3 startRigPosition, startWorldPosition;
    private Quaternion startRigRotation, startWorldRotation;
    public TMP_InputField probandInputField;

    // Experiment Modes & Phases
    public enum RedirectionMode { Blink, Walking }
    public enum ExperimentPhase { Staircase_Blink, Reverse_Blink, Staircase_Walking, Reverse_Walking, Finished }
    private RedirectionMode currentMode = RedirectionMode.Blink;
    private ExperimentPhase currentPhase = ExperimentPhase.Staircase_Blink;

    // UI
    public GameObject startButtonObj;
    public GameObject overlayPanel;
    public TMP_Text overlayText;
    public GameObject afcPanel;
    public TMP_Text questionText;
    public TMP_Text feedbackText;

    // Likert
    public Slider zusatzSlider1;
    public Slider zusatzSlider2;
    public TMP_Text zusatzSlider1ValueText;
    public TMP_Text zusatzSlider2ValueText;
    public GameObject finishButton;
    public string[] likertLabels1 = { "gar nicht sicher", "wenig sicher", "neutral", "eher sicher", "sehr sicher" };
    public string[] likertLabels2 = { "gar nicht", "wenig", "neutral", "eher", "sehr" };

    // Redirection
    public RedirectedWalkingManager redirectionManager;
    public GameObject endCollider;
    public float minAngle = 0.5f;
    public float maxAngle = 5.0f;
    public float initialAngleBlink = 0.5f;
    public float initialAngleWalking = 0.5f;
    public float gainStep = 0.1f;
    public float minGain = 1.0f;
    public float maxGain = 2.0f;
    public float walkingRotationAngle = 0f;

    private int blinkDirection = 1;
    private int currentTrial = 0;
    private int manipulatedRun;
    private bool experimentStarted = false;
    private int? user2AFCChoice = null; 

    // Staircase & Reverse Staircase
    private float staircaseStep = 0.5f;
    private float reverseStep = 2.0f;
    private int umkehrpunktCount = 0;
    private int lastAnswer = -1;         // 1 = richtig, 0 = falsch
    private List<float> jndList = new List<float>();
    private float userJND = 0f;
    private int trialInPhase = 0;
    private int minUmkehrpunkte = 6;
    private int maxTrialsProPhase = 20;  // z.B. maximal 20 Trials pro Phase
    private float currentAngle = 0.5f;

    // Logging
    public CSVLogger csvLogger;  // Zieh deinen Logger hier im Inspector rein!
    public int probandID = 1;    // Setze diese ID dynamisch per UI oder Inspector

    void Start()
    {
        Debug.Log("RDWexpoeriment Manager Start()");
        if (csvLogger != null)
        {
            Debug.Log("CSV Logger gestartet");
            csvLogger.InitCSV(new List<string>{
                "timestamp", "probandID", "phase", "mode", "trialIndex", "manipulatedRun", "userChoice", "isCorrect", "currentAngle", "likert1", "likert2"
            });
        }
        else
        {
            Debug.LogWarning("CSVLogger nicht zugewiesen!");
        }

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

        // Likert Skalen
        zusatzSlider1.gameObject.SetActive(false);
        zusatzSlider2.gameObject.SetActive(false);
        zusatzSlider1ValueText.gameObject.SetActive(false);
        zusatzSlider2ValueText.gameObject.SetActive(false);
        finishButton.SetActive(false);
        zusatzSlider1.value = 1;
        zusatzSlider2.value = 1;

        // CSV-Header festlegen
        
    }
    
    public void SetProbandID(string input)
    {
        if (int.TryParse(input, out int value))
            probandID = value;
        else
            probandID = 1; // oder Standardwert/falls ungültig
    }
    
    public void OnStartButtonClicked()
    {
        SetProbandID(probandInputField.text);
        startButtonObj.SetActive(false);
        experimentStarted = true;
        // Counterbalancing durch Teilnehmernummer (hier Dummy: gerade/ungerade)
        // In der Praxis von UI oder extern setzen!
        if (probandID % 2 == 0)
        {
            currentMode = RedirectionMode.Walking;
            currentPhase = ExperimentPhase.Staircase_Walking;
            currentAngle = initialAngleWalking;
        }
        else
        {
            currentMode = RedirectionMode.Blink;
            currentPhase = ExperimentPhase.Staircase_Blink;
            currentAngle = initialAngleBlink;
        }
        umkehrpunktCount = 0;
        lastAnswer = -1;
        jndList.Clear();
        currentTrial = 0;
        trialInPhase = 0;
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    IEnumerator ShowOverlayAndStartRun(int runNumber)
    {
        overlayPanel.SetActive(true);
        overlayText.text = $"Durchgang {runNumber} startet gleich...";
        yield return new WaitForSeconds(2.5f);
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
            SetCurrentRedirection();
        }
        else
        {
            SetCurrentRedirection(0f);
        }
    }

    void StartRun2()
    {
        if (manipulatedRun == 2)
        {
            blinkDirection = (Random.value > 0.5f) ? 1 : -1;
            SetCurrentRedirection();
        }
        else
        {
            SetCurrentRedirection(0f);
        }
        StartCoroutine(Show2AFCPanel());
    }

    void SetCurrentRedirection(float overrideAngle = float.NaN)
    {
        if (currentMode == RedirectionMode.Blink)
        {
            redirectionManager.blinkRotationAngle = float.IsNaN(overrideAngle) ? currentAngle * blinkDirection : overrideAngle;
        }
        else if (currentMode == RedirectionMode.Walking)
        {
            redirectionManager.walkingRotationAngle = float.IsNaN(overrideAngle) ? currentAngle * blinkDirection : overrideAngle;
        }
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

    public void UpdateSliderValueText1()
    {
        int idx = Mathf.Clamp(Mathf.RoundToInt(zusatzSlider1.value), 0, likertLabels1.Length - 1);
        zusatzSlider1ValueText.text = likertLabels1[idx];
    }
    public void UpdateSliderValueText2()
    {
        int idx = Mathf.Clamp(Mathf.RoundToInt(zusatzSlider2.value), 0, likertLabels2.Length - 1);
        zusatzSlider2ValueText.text = likertLabels2[idx];
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

        if (currentPhase == ExperimentPhase.Staircase_Blink || currentPhase == ExperimentPhase.Staircase_Walking)
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
        else if (currentPhase == ExperimentPhase.Reverse_Blink || currentPhase == ExperimentPhase.Reverse_Walking)
        {
            trialInPhase++;
            if (trialInPhase % 3 == 0)
                reverseStep = Mathf.Max(0.1f, reverseStep * 0.5f);

            if (correct)
                currentAngle = Mathf.Max(minAngle, currentAngle - reverseStep);
            else
                currentAngle = Mathf.Min(maxAngle, currentAngle + reverseStep);
        }

        lastAnswer = currentAnswer;

        // CSV-Logging
        if (csvLogger != null)
        {
            var data = new Dictionary<string, object>
            {
                {"timestamp", System.DateTime.Now.ToString("o")},
                {"probandID", probandID},
                {"phase", currentPhase.ToString()},
                {"mode", currentMode.ToString()},
                {"trialIndex", currentTrial},
                {"manipulatedRun", manipulatedRun},
                {"userChoice", choice},
                {"isCorrect", correct ? 1 : 0},
                {"currentAngle", currentAngle},
                {"likert1", zusatz1},
                {"likert2", zusatz2}
            };
            csvLogger.LogTrial(data);
        }

        currentTrial++;
        StartCoroutine(NextTrialAfterDelay());
    }

    void StarteReverseStaircase()
    {
        if (currentPhase == ExperimentPhase.Staircase_Blink)
        {
            currentPhase = ExperimentPhase.Reverse_Blink;
            currentAngle = userJND;
        }
        else if (currentPhase == ExperimentPhase.Staircase_Walking)
        {
            currentPhase = ExperimentPhase.Reverse_Walking;
            currentAngle = userJND;
        }
        reverseStep = 2.0f;
        trialInPhase = 0;
        overlayPanel.SetActive(true);
        overlayText.text = $"Zweite Phase (Reverse Staircase) startet!\nDeine Schwelle: {userJND:F2}°";
        StartCoroutine(CloseOverlayAndNextTrial());
    }

    IEnumerator CloseOverlayAndNextTrial()
    {
        yield return new WaitForSeconds(2.0f);
        overlayPanel.SetActive(false);
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    IEnumerator NextTrialAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        if (currentTrial >= maxTrialsProPhase)
        {
            if (currentPhase == ExperimentPhase.Reverse_Blink)
            {
                currentPhase = ExperimentPhase.Staircase_Walking;
                currentMode = RedirectionMode.Walking;
                currentTrial = 0;
                umkehrpunktCount = 0;
                lastAnswer = -1;
                jndList.Clear();
                currentAngle = initialAngleWalking;
                overlayPanel.SetActive(true);
                overlayText.text = "Walking-Redirection Test startet!";
                yield return new WaitForSeconds(2f);
                overlayPanel.SetActive(false);
                StartCoroutine(ShowOverlayAndStartRun(1));
                yield break;
            }
            if (currentPhase == ExperimentPhase.Reverse_Walking)
            {
                currentPhase = ExperimentPhase.Finished;
                overlayPanel.SetActive(true);
                overlayText.text = "Experiment beendet!";
                yield break;
            }
        }
        ResetWorldRelativeToRig();
        StartCoroutine(ShowOverlayAndStartRun(1));
    }
}
