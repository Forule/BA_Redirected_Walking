using UnityEngine;
using UnityEngine.XR;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RDWExperimentManager : MonoBehaviour
{
    #region Variablen Deklaration
    // Player World
    public GameObject xrRig;
    public Transform cameraOffset;
    public Transform mainCamera;
    public Transform environmentRoot;
    public Transform startPoint;
    private List<GameObject> allCoins;
    private Vector3 startRigPosition, startWorldPosition;
    private Quaternion startRigRotation, startWorldRotation;

    // Probanden ID
    [Header("Probanden ID Auswahl")]
    public TMP_Text probandIdText;
    public int maxProbandID = 50;
    public int probandID = 1;
    public Transform headAnchor;
    public bool zeroCameraOffsetOnReset = false;

    // Experiment Logik
    public enum RedirectionMode { Blink, Walking }
    public enum ExperimentPhase { Coarse, Fine, Finished }
    private RedirectionMode currentMode = RedirectionMode.Blink;
    private ExperimentPhase currentPhase = ExperimentPhase.Coarse;
    private bool blinkBlockDone = false;
    private bool walkingBlockDone = false;

    // UI
    public GameObject startButtonObj;
    public GameObject overlayPanel;
    public TMP_Text overlayText;
    public GameObject afcPanel;
    public TMP_Text questionText;

    // Reset Logik
    public bool rotatePlayer180OnReset = true;

    // Likert Scales
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

    [Header("Blink (° pro Blink)")]
    public float minBlinkAngleDeg = 0.0f;
    public float maxBlinkAngleDeg = 12.0f;
    public float initialBlinkAngleDeg = 8.0f;
    public float coarseStepBlinkDeg = 2.0f;
    public float minFineStepBlinkDeg = 0.1f;

    [Header("Walking (° pro Meter)")]
    public float minWalkingGainDegPerMeter = 0.0f;
    public float maxWalkingGainDegPerMeter = 12.0f;
    public float initialWalkingGainDegPerMeter = 8.0f;
    public float coarseStepWalkingDegPerM = 2.0f;
    public float minFineStepWalkingDegPerMeter = 0.1f;

    [Header("Staircase Phasen & Stopp-Kriterien")]
    [Tooltip("Relative Start-Schrittweite für die Fein-Phase, als Faktor des SEED-Werts aus der Grob-Phase.")]
    [Range(0.01f, 0.5f)]
    public float fineStartStepFactor = 0.25f;
    public int coarseMinReversals = 2;
    public int fineMaxReversals = 6;
    public int maxTotalTrialsPerBlock = 30;
    public bool useConvergenceStop = true;
    public int convWindow = 4;
    public float convRangeBlinkDeg = 0.5f;
    public float convRangeWalkDegPerM = 0.5f;

    [Header("Catch Trials")]
    [Range(0f, 1f)]
    public float catchTrialProbability = 0.15f;

    [Header("Timing & Floor Snap")]
    public float overlayDelaySec = 1.0f;
    public FloorSnapper floorSnapper;
    public bool snapToFloorOnReset = true;

    // Private Zustandsvariablen
    private bool isCatchTrial = false;
    private float currentBlinkAngleDeg;
    private float currentWalkingGainDegPerMeter;
    private float currentFineStep;
    private int blinkDirection = 1;
    private int totalTrialCount = 0;
    private int manipulatedRun;
    private int? user2AFCChoice = null;
    private int coarseReversalCount = 0;
    private int fineReversalCount = 0;
    private int lastStepDir = 0;
    private int trialsInCurrentBlock = 0;
    private List<float> coarseReversals = new List<float>();
    private List<float> fineReversals = new List<float>();
    private bool finePhaseConverged = false;
    public CSVLogger csvLogger;
    private bool legendRowWritten = false;
    #endregion

    void Start()
    {
        if (mainCamera == null && Camera.main != null) mainCamera = Camera.main.transform;
        if (floorSnapper != null)
        {
            floorSnapper.SetRigAndHead(xrRig.transform, mainCamera);
            floorSnapper.continuousSnap = true;
        }

        UpdateProbandIdDisplay();

        startRigPosition = xrRig.transform.position;
        startRigRotation = xrRig.transform.rotation;
        startWorldPosition = environmentRoot.position;
        startWorldRotation = environmentRoot.rotation;

        if (csvLogger != null)
        {
            var fileName = $"RDW_P{probandID}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            csvLogger.InitCSV(new List<string>{
                "timestamp_iso8601", "participant_id", "method_mode", "phase", "block_trial_index", "total_trial_index",
                "is_catch_trial", "manipulated_run", "participant_choice", "is_correct", "false_alarm",
                "gain_before_step", "step_size_applied", "gain_after_step", "gain_unit",
                "total_reversals", "step_direction", "is_reversal",
                "likert_confidence", "likert_discomfort"
            }, fileName, ';', true);
            WriteCsvLegendRow();
        }

        allCoins = new List<GameObject>(GameObject.FindGameObjectsWithTag("Coin"));
        startButtonObj.SetActive(true);
        overlayPanel.SetActive(false);
        afcPanel.SetActive(false);
    }

    void WriteCsvLegendRow()
    {
        if (legendRowWritten || csvLogger == null) return;
        var legend = new Dictionary<string, object> {
            { "is_catch_trial", "1 if catch trial, 0 otherwise" },
            { "manipulated_run", "1 or 2, 0 for catch trial" },
            { "false_alarm", "1 if participant chose 1 or 2 on a catch trial" },
            { "total_reversals", "Sum of coarse and fine reversals in this block" }
        };
        csvLogger.LogTrial(legend);
        legendRowWritten = true;
    }

    public void OnStartButtonClicked()
    {
        startButtonObj.SetActive(false);
        blinkBlockDone = false;
        walkingBlockDone = false;
        totalTrialCount = 0;

        if (probandID % 2 == 0) BeginWalkingBlockFresh();
        else BeginBlinkBlockFresh();
    }

    void EndCurrentBlock()
    {
        if (currentMode == RedirectionMode.Blink)
        {
            blinkBlockDone = true;
            if (!walkingBlockDone) BeginWalkingBlockFresh(); else FinishExperiment();
        }
        else
        {
            walkingBlockDone = true;
            if (!blinkBlockDone) BeginBlinkBlockFresh(); else FinishExperiment();
        }
    }
    IEnumerator ShowOverlayAndStartRun(int runNumber)
    {
        overlayPanel.SetActive(true);
        overlayText.text = $"Durchgang {runNumber} startet gleich...";

        yield return new WaitForSeconds(overlayDelaySec);
        if (redirectionManager != null) redirectionManager.ResumeRedirection();
        overlayPanel.SetActive(false);

        if (runNumber == 1) StartRun1();
        else StartRun2();
    }

    void StartRun1()
    {
        if (endCollider != null) endCollider.SetActive(true);

        endCollider.GetComponent<EndCollider>()?.SetRunNumber(1);

        isCatchTrial = Random.value < catchTrialProbability;

        if (isCatchTrial)
        {
            manipulatedRun = 0;
            SetCurrentRedirection(0f);
        }
        else
        {
            manipulatedRun = Random.Range(1, 3);
            blinkDirection = (Random.value > 0.5f) ? 1 : -1;
            if (manipulatedRun == 1) SetCurrentRedirection(); else SetCurrentRedirection(0f);
        }
        if (redirectionManager != null) redirectionManager.BeginRunTracking();
    }

    void StartRun2()
    {
        if (endCollider != null) endCollider.SetActive(true);

        endCollider.GetComponent<EndCollider>()?.SetRunNumber(2);
        if (isCatchTrial)
        {
            SetCurrentRedirection(0f);
        }
        else
        {
            blinkDirection = (Random.value > 0.5f) ? 1 : -1;
            if (manipulatedRun == 2) SetCurrentRedirection(); else SetCurrentRedirection(0f);
        }
        if (redirectionManager != null) redirectionManager.BeginRunTracking();
    }

    void SetCurrentRedirection(float overrideValue = float.NaN)
    {
        if (currentMode == RedirectionMode.Blink)
        {
            float angleDeg = float.IsNaN(overrideValue) ? currentBlinkAngleDeg * blinkDirection : overrideValue;
            if (redirectionManager != null) redirectionManager.SetBlinkRotationAngle(angleDeg);
        }
        else
        {
            float degPerMeter = float.IsNaN(overrideValue) ? currentWalkingGainDegPerMeter * blinkDirection : overrideValue;
            if (redirectionManager != null) redirectionManager.SetWalkingRotationAngle(degPerMeter);
        }
    }

    public void OnReachedEndOfRun1()
    {
        if (endCollider != null) endCollider.SetActive(false);
        if (redirectionManager != null)
        {
            redirectionManager.PauseRedirection();
        }
        ResetWorldRelativeToRig();
        StartCoroutine(ShowOverlayAndStartRun(2));
    }

    public void OnReachedEndOfRun2()
    {
        if (endCollider != null) endCollider.SetActive(false);
        if (redirectionManager != null) redirectionManager.PauseRedirection();
        StartCoroutine(Show2AFCPanel());
    }

    public void ResetWorldRelativeToRig()
    {
        var cc = xrRig.GetComponent<CharacterController>();
        bool ccWasEnabled = false;
        if (cc) { ccWasEnabled = cc.enabled; cc.enabled = false; }

        RecenterHeadXZImmediate();

        if (rotatePlayer180OnReset)
        {
            xrRig.transform.Rotate(0f, 180f, 0f);
        }

        StartCoroutine(RecenterHeadXZNextFrame());
        if (redirectionManager != null) redirectionManager.NotifyWorldReset();

        if (cc)
        {
            cc.enabled = ccWasEnabled;
            cc.Move(Vector3.zero);
        }
        if (snapToFloorOnReset && floorSnapper != null)
            floorSnapper.BeginInvisibleSnapWindow(0.25f);
        foreach (var c in allCoins) c.SetActive(true);
    }

    void EvaluateAnswer(int choice, int likert1, int likert2)
    {
        trialsInCurrentBlock++;
        totalTrialCount++;

        bool isBlink = (currentMode == RedirectionMode.Blink);
        float beforeParam = isBlink ? currentBlinkAngleDeg : currentWalkingGainDegPerMeter;

        if (isCatchTrial)
        {
            bool falseAlarm = (choice == 1 || choice == 2);
            LogCsvTrialRow(false, choice, likert1, likert2, beforeParam, false, true, falseAlarm);
            StartCoroutine(NextTrialOrBlock());
            return;
        }

        bool correct = (choice == manipulatedRun);
        bool isCoarse = (currentPhase == ExperimentPhase.Coarse);

        float pMin = isBlink ? minBlinkAngleDeg : minWalkingGainDegPerMeter;
        float pMax = isBlink ? maxBlinkAngleDeg : maxWalkingGainDegPerMeter;
        float step = isCoarse ? (isBlink ? coarseStepBlinkDeg : coarseStepWalkingDegPerM) : currentFineStep;

        int stepDirThisTrial = correct ? -1 : 1;

        float newParam = beforeParam + (step * stepDirThisTrial);
        if (isBlink) currentBlinkAngleDeg = Mathf.Clamp(newParam, pMin, pMax);
        else currentWalkingGainDegPerMeter = Mathf.Clamp(newParam, pMin, pMax);

        bool reversalThisTrial = (lastStepDir != 0 && stepDirThisTrial != lastStepDir);
        lastStepDir = stepDirThisTrial;

        if (reversalThisTrial)
        {
            if (isCoarse)
            {
                coarseReversalCount++;
                coarseReversals.Add(beforeParam);
            }
            else
            {
                fineReversalCount++;
                fineReversals.Add(beforeParam);

                float minStep = isBlink ? minFineStepBlinkDeg : minFineStepWalkingDegPerMeter;
                currentFineStep = Mathf.Max(minStep, currentFineStep * 0.5f);
            }
        }

        LogCsvTrialRow(correct, choice, likert1, likert2, beforeParam, reversalThisTrial, false, false);
        StartCoroutine(NextTrialOrBlock());
    }

    IEnumerator NextTrialOrBlock()
    {
        yield return new WaitForSeconds(1.0f);

        // * Harte Obergrenze gilt für Coarse+Fine zusammen *
        if (trialsInCurrentBlock >= maxTotalTrialsPerBlock)
        {
            EndCurrentBlock();
            yield break;
        }

        bool isCoarse = (currentPhase == ExperimentPhase.Coarse);

        // --- Übergang in Fine, sobald Coarse-Kriterium erfüllt ---
        if (isCoarse && coarseReversalCount >= coarseMinReversals)
        {
            StartFinePhase();
            yield break;
        }

        // --- Feinphase: reguläre Stop-Kriterien ---
        if (!isCoarse)
        {
            if (useConvergenceStop) CheckConvergence();
            bool blockFinished =
                fineReversalCount >= fineMaxReversals ||
                finePhaseConverged;

            if (blockFinished)
            {
                EndCurrentBlock();
                yield break;
            }
        }

        // --- weiter mit nächstem Trial ---
        ResetWorldRelativeToRig();
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    void StartFinePhase()
    {
        // * Falls Cap schon erreicht, Block sofort beenden *
        if (trialsInCurrentBlock >= maxTotalTrialsPerBlock)
        {
            EndCurrentBlock();
            return;
        }

        currentPhase = ExperimentPhase.Fine;
        float seedValue;

        if (currentMode == RedirectionMode.Blink)
        {
            seedValue = (coarseReversals.Count > 0) ? coarseReversals.Average() : initialBlinkAngleDeg;
            currentBlinkAngleDeg = Mathf.Clamp(seedValue, minBlinkAngleDeg, maxBlinkAngleDeg);
            currentFineStep = seedValue * fineStartStepFactor;
        }
        else
        {
            seedValue = (coarseReversals.Count > 0) ? coarseReversals.Average() : initialWalkingGainDegPerMeter;
            currentWalkingGainDegPerMeter = Mathf.Clamp(seedValue, minWalkingGainDegPerMeter, maxWalkingGainDegPerMeter);
            currentFineStep = seedValue * fineStartStepFactor;
        }

        fineReversalCount = 0;
        fineReversals.Clear();
        lastStepDir = 0;
        finePhaseConverged = false;

        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    void BeginBlinkBlockFresh()
    {
        currentMode = RedirectionMode.Blink;
        currentPhase = ExperimentPhase.Coarse;
        currentBlinkAngleDeg = initialBlinkAngleDeg;
        ResetBlockCounters();
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    void BeginWalkingBlockFresh()
    {
        currentMode = RedirectionMode.Walking;
        currentPhase = ExperimentPhase.Coarse;
        currentWalkingGainDegPerMeter = initialWalkingGainDegPerMeter;
        ResetBlockCounters();
        StartCoroutine(ShowOverlayAndStartRun(1));
    }

    void ResetBlockCounters()
    {
        trialsInCurrentBlock = 0;
        coarseReversalCount = 0;
        coarseReversals.Clear();
        fineReversalCount = 0;
        fineReversals.Clear();
        lastStepDir = 0;
        finePhaseConverged = false;
    }

    void FinishExperiment()
    {
        currentPhase = ExperimentPhase.Finished;
        overlayPanel.SetActive(true);
        overlayText.text = "Experiment beendet – vielen Dank!";
    }

    void LogCsvTrialRow(bool correct, int choice, int likert1, int likert2, float gainBefore, bool isReversal, bool isCatch, bool falseAlarm)
    {
        if (csvLogger == null) return;

        bool isBlink = (currentMode == RedirectionMode.Blink);
        float afterParam = isBlink ? currentBlinkAngleDeg : currentWalkingGainDegPerMeter;
        float stepSize = (currentPhase == ExperimentPhase.Coarse) ? (isBlink ? coarseStepBlinkDeg : coarseStepWalkingDegPerM) : currentFineStep;

        if (isCatch) { afterParam = 0; stepSize = 0; }

        var row = new Dictionary<string, object>
        {
            { "timestamp_iso8601", System.DateTime.Now.ToString("o") },
            { "participant_id", probandID },
            { "method_mode", currentMode.ToString() },
            { "phase", currentPhase.ToString() },
            { "block_trial_index", trialsInCurrentBlock },
            { "total_trial_index", totalTrialCount },
            { "is_catch_trial", isCatch ? 1 : 0 },
            { "manipulated_run", manipulatedRun },
            { "participant_choice", choice },
            { "is_correct", correct ? 1 : 0 },
            { "false_alarm", falseAlarm ? 1 : 0 },
            { "gain_before_step", gainBefore },
            { "step_size_applied", isCatch ? 0 : stepSize * lastStepDir },
            { "gain_after_step", afterParam },
            { "gain_unit", isBlink ? "deg" : "deg/m" },
            { "total_reversals", coarseReversalCount + fineReversalCount },
            { "step_direction", lastStepDir },
            { "is_reversal", isReversal ? 1 : 0 },
            { "likert_confidence", likert1 },
            { "likert_discomfort", likert2 }
        };
        csvLogger.LogTrial(row);
    }

    void CheckConvergence()
    {
        if (fineReversals.Count < convWindow) return;

        var relevantReversals = fineReversals.Skip(fineReversals.Count - convWindow);
        float range = relevantReversals.Max() - relevantReversals.Min();
        float thr = (currentMode == RedirectionMode.Blink) ? convRangeBlinkDeg : convRangeWalkDegPerM;

        if (range <= thr)
        {
            finePhaseConverged = true;
        }
    }

    #region Hilfsfunktionen
    IEnumerator EnsureGroundedDeferred() { Physics.SyncTransforms(); yield return null; var cc = xrRig.GetComponent<CharacterController>(); if (cc != null) cc.Move(Vector3.zero); }
    IEnumerator Show2AFCPanel()
    {
        afcPanel.SetActive(true);
        questionText.text = "In welchem Durchgang war die Manipulation?";
        user2AFCChoice = null;

        bool isFine = (currentPhase == ExperimentPhase.Fine);
        zusatzSlider1.gameObject.SetActive(isFine);
        zusatzSlider2.gameObject.SetActive(isFine);
        zusatzSlider1ValueText.gameObject.SetActive(isFine);
        zusatzSlider2ValueText.gameObject.SetActive(isFine);
        finishButton.SetActive(isFine);
        if (isFine) { UpdateSliderValueText1(); UpdateSliderValueText2(); }
        yield break;
    }
    public void OnChoose1() { user2AFCChoice = 1; HandleAFCChoiceUI(); }
    public void OnChoose2() { user2AFCChoice = 2; HandleAFCChoiceUI(); }
    void HandleAFCChoiceUI()
    {
        if (user2AFCChoice == null) return;
        bool isFine = (currentPhase == ExperimentPhase.Fine);
        if (isFine) return;
        afcPanel.SetActive(false);
        EvaluateAnswer(user2AFCChoice.Value, 0, 0);
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
        if (user2AFCChoice == null) return;
        int likert1 = (int)zusatzSlider1.value;
        int likert2 = (int)zusatzSlider2.value;
        afcPanel.SetActive(false);
        EvaluateAnswer(user2AFCChoice.Value, likert1, likert2);
    }
    void RecenterHeadXZImmediate()
    {
        if (mainCamera == null) return;
        if (zeroCameraOffsetOnReset && cameraOffset != null)
            cameraOffset.localPosition = new Vector3(0f, cameraOffset.localPosition.y, 0f);
        Vector3 desired = (headAnchor != null ? headAnchor : startPoint) != null ? (headAnchor != null ? headAnchor : startPoint).position : startRigPosition;
        Vector3 delta = desired - mainCamera.position;
        delta.y = 0f;
        var cc = xrRig.GetComponent<CharacterController>();
        if (cc != null)
        {
            bool was = cc.enabled; cc.enabled = false;
            xrRig.transform.position += delta;
            cc.enabled = was;
            cc.Move(Vector3.zero);
        }
        else { xrRig.transform.position += delta; }
        Physics.SyncTransforms();
    }
    IEnumerator RecenterHeadXZNextFrame() { yield return new WaitForEndOfFrame(); RecenterHeadXZImmediate(); }

    public void IncrementProbandID()
    {
        probandID++;
        if (probandID > maxProbandID) probandID = 1;
        UpdateProbandIdDisplay();
    }

    public void DecrementProbandID()
    {
        probandID--;
        if (probandID < 1) probandID = maxProbandID;
        UpdateProbandIdDisplay();
    }
    // --- NEU: Die fehlende Funktion für die Probanden-ID-Buttons ---
    public void UpdateProbandIdDisplay()
    {
        if (probandIdText != null)
        {
            probandIdText.text = $"ID: {probandID}";
        }
    }
    #endregion
}