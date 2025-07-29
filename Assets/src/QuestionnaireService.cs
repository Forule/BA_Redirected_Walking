using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

/// <summary>
/// Handles loading questionnaire data from a CSV file, displaying questions,
/// navigating between pages, and exporting responses.
/// </summary>
public class QuestionnaireService : MonoBehaviour
{
    #region Inspector Fields

    [Header("Information for Data loading and storage")]
    [Tooltip("Please insert the data path of the questionnaire file. E.g. /Users/YourName/Desktop/UEQ.csv")]
    public string questionnaireDataPath;

    [Tooltip("Please insert the location where the output file shall be stored. E.g. /Users/YourName/Desktop/")]
    public string outputDataStoragePath;

    [Tooltip("Please insert the name that you want your output file to have.")]
    public string outputDataName = "Questionnaire_Data";

    [Tooltip("Name of the current subject. This will be stored in the output file.")]
    [SerializeField]
    private string subjectName;

    [Tooltip("Code of the current subject. This will be stored in the output file.")]
    [SerializeField]
    private string subjectCode;

    [Header("Output Options")]
    [Tooltip("If enabled, the output will be format -3...3 instead of 0...6 for a 7-point scale.")]
    [SerializeField]
    private bool storeBipolarAnswerValue = false;

    [Tooltip("If enabled, the complete question text will be stored in the output file.")]
    [SerializeField]
    private bool storeQuestionText = false;

    [Tooltip("If enabled, a fully verbal Likert scale is used. Make sure you include the labels in the CSV file.")]
    [SerializeField]
    private bool useFullyVerbalScale = false;

    [Tooltip("If enabled, the editing time for each question appears in the result CSV file.")]
    [SerializeField]
    private bool outputEditingTime = false;

    [Header("Preferences")]
    [Tooltip("Number of questions on the screen at the same time.")]
    [Range(1, 20)]
    [SerializeField]
    private int noOfQuestionsOnDisplay = 5;

    [Tooltip("Number of answer options created (size of Likert scale).")]
    [Range(1, 15)]
    [SerializeField]
    private int sizeOfLikertScale = 5;

    [Tooltip("If enabled, the buttons will have an extra numeric label.")]
    [SerializeField]
    private bool addNumericLabel = false;

    [Tooltip("If enabled, only one question text will be shown (for generic prompt screens).")]
    [SerializeField]
    private bool oneQuestionTextOnly = false;

    [Tooltip("If enabled, some questions will appear with an inverted scale.")]
    [SerializeField]
    private bool randomisedInvertation = false;

    [Tooltip("Probability that a question gets inverted (1 = every question inverted).")]
    [Range(0f, 1f)]
    [SerializeField]
    private float inversionProbability = 0.5f;

    [Header("Internal GameObject Setup")]
    [SerializeField] private GameObject headerFrame;
    [SerializeField] private GameObject bottomFrame;
    [SerializeField] private GameObject submitButtonForward;
    [SerializeField] private GameObject submitButtonBack;
    [SerializeField] private GameObject powerOnButton;
    [SerializeField] private GameObject questionFramePrefab;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject introductionTextPrefab;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite finishSprite;

    #endregion

    // Tracks whether the initial introduction screen is shown
    private bool initialLoad = true;

    // Stopwatch for measuring editing time per question
    private readonly Stopwatch timer = new Stopwatch();

    private string introductionText;
    private string farewellText;
    private int sizeOfLikertScaleInternal;
    private bool finishSpriteIsOn = false;
    private Color defaultBackButtonColor;

    // Lists holding question data
    private readonly List<Question> questions = new List<Question>();

    #region Unity Methods

    private void Start()
    {
        // Load questions and show the introduction
        LoadQuestionsFromCSV(questionnaireDataPath);
        sizeOfLikertScaleInternal = sizeOfLikertScale;

        var introObj = Instantiate(introductionTextPrefab, headerFrame.transform, false);
        introObj.GetComponent<TextMeshProUGUI>().SetText(introductionText);
    }

    private void Update()
    {
        // Placeholder for potential per-frame logic
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Display the next page of questions or finish the questionnaire.
    /// </summary>
    public void DisplayNextQuestion()
    {
        int maxIndex = GetHighestDisplayedQuestionIndex();
        int minIndex = GetLowestDisplayedQuestionIndex();

        if (initialLoad)
        {
            defaultBackButtonColor = powerOnButton.GetComponent<Image>().color;
            powerOnButton.SetActive(false);
            submitButtonForward.SetActive(true);
            submitButtonBack.SetActive(true);
            submitButtonBack.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.66f);

            initialLoad = false;
            maxIndex = -1;

            // Remove introduction text
            Destroy(headerFrame.transform.GetChild(0).gameObject);
            headerFrame.GetComponent<Image>().enabled = false;
        }
        else
        {
            timer.Stop();
            StoreQuestionDataAndDestroy();
        }

        // If reaching the last page, change forward icon
        if (maxIndex + noOfQuestionsOnDisplay >= questions.Count)
        {
            var forwardImage = bottomFrame.transform
                .Find("ForwardButtonFrame/ForwardButton/Image").GetComponent<Image>();
            forwardImage.sprite = finishSprite;
            finishSpriteIsOn = true;
        }

        // Re-enable the back button on second page
        if (maxIndex == noOfQuestionsOnDisplay - 1)
        {
            submitButtonBack.GetComponent<Image>().color = defaultBackButtonColor;
        }

        // If no more questions, finalize and show farewell
        if (questions.Count - (maxIndex + 1) <= 0)
        {
            CreateAnswerCSV();
            Debug.Log("User data stored in CSV.");

            var farewellObj = Instantiate(introductionTextPrefab, headerFrame.transform, false);
            farewellObj.GetComponent<TextMeshProUGUI>().SetText(farewellText);

            bottomFrame.SetActive(false);
            headerFrame.GetComponent<Image>().enabled = true;
        }
        else
        {
            // Load next set of questions
            LoadNewQuestionsAndButtons(maxIndex + 1);
        }
    }

    /// <summary>
    /// Display the previous page of questions.
    /// </summary>
    public void DisplayPreviousQuestion()
    {
        int maxIndex = GetHighestDisplayedQuestionIndex();
        int minIndex = GetLowestDisplayedQuestionIndex();

        timer.Stop();
        StoreQuestionDataAndDestroy();

        int startIndex = Mathf.Max(minIndex - noOfQuestionsOnDisplay, 0);
        LoadNewQuestionsAndButtons(startIndex);

        // Hide back button if on first page
        if (questions[0].GetIsOnDisplay())
        {
            submitButtonBack.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.66f);
        }

        // Restore forward arrow if it was changed
        if (finishSpriteIsOn)
        {
            var forwardImage = bottomFrame.transform
                .Find("ForwardButtonFrame/ForwardButton/Image").GetComponent<Image>();
            forwardImage.sprite = arrowSprite;
            finishSpriteIsOn = false;
        }
    }

    #endregion

    #region Data Loading and Storage

    /// <summary>
    /// Reads questionnaire data from a CSV file and initializes question list.
    /// </summary>
    private void LoadQuestionsFromCSV(string path)
    {
        Debug.Log("Loading questions from CSV...");
        using var reader = new StreamReader(File.OpenRead(path));
        int lineIndex = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;

            var values = line.Split(';');
            switch (lineIndex)
            {
                case 0:
                    introductionText = line.Trim(';');
                    break;
                case 1:
                    farewellText = line.Trim(';');
                    break;
                default:
                    var question = new Question();
                    foreach (var value in values)
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                            question.verbalLabels.Add(value.Trim());
                    }
                    questions.Add(question);
                    break;
            }

            if (randomisedInvertation && questions.Count > 0)
            {
                UnityEngine.Random.InitState(Environment.TickCount);
                for (int i = 0; i < questions.Count; i++)
                {
                    if (UnityEngine.Random.value < inversionProbability)
                        questions[i].SetIsInverted(true);
                }
            }

            lineIndex++;
        }
    }

    private void LoadNewQuestionsAndButtons(int startIndex)
    {
        // Instantiate question frames and answer buttons
        for (int i = 0; i < noOfQuestionsOnDisplay && startIndex + i < questions.Count; i++)
        {
            var questionObj = Instantiate(questionFramePrefab, headerFrame.transform, false);
            var q = questions[startIndex + i];

            // Set question text and labels
            var headerText = questionObj.transform
                .Find("Q_HeaderFrame").GetComponentInChildren<TextMeshProUGUI>();
            headerText.SetText(q.verbalLabels[0]);

            var leftLabel = questionObj.transform
                .Find("Q_BottomFrame/LeftLabelFrame").GetComponentInChildren<TextMeshProUGUI>();
            var rightLabel = questionObj.transform
                .Find("Q_BottomFrame/RightLabelFrame").GetComponentInChildren<TextMeshProUGUI>();

            if (!useFullyVerbalScale)
            {
                if (!q.GetIsInverted())
                {
                    leftLabel.SetText(q.verbalLabels[1]);
                    rightLabel.SetText(q.verbalLabels[2]);
                }
                else
                {
                    leftLabel.SetText(q.verbalLabels[2]);
                    rightLabel.SetText(q.verbalLabels[1]);
                }
            }
            else
            {
                leftLabel.transform.parent.gameObject.SetActive(false);
                rightLabel.transform.parent.gameObject.SetActive(false);
            }

            if (oneQuestionTextOnly && i > 0)
                questionObj.transform.Find("Q_HeaderFrame").gameObject.SetActive(false);

            q.SetIsOnDisplay(true);

            // Create toggles for answers
            var buttonFrame = questionObj.transform
                .Find("Q_BottomFrame/ButtonFrame").GetComponent<ToggleGroup>();

            for (int j = 0; j < sizeOfLikertScaleInternal; j++)
            {
                var btn = Instantiate(buttonPrefab, buttonFrame.transform, false);
                var toggle = btn.GetComponent<Toggle>();
                toggle.group = buttonFrame;

                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (addNumericLabel && !useFullyVerbalScale)
                    label.SetText((j + 1).ToString());
                else if (useFullyVerbalScale && j + 1 < q.verbalLabels.Count)
                    label.SetText(q.verbalLabels[1 + j]);
                else
                    label.SetText(string.Empty);
            }

            // Restore previous answer if exists
            int prevAnswer = q.GetAnswer();
            if (prevAnswer >= 0)
            {
                if (q.GetIsInverted())
                    prevAnswer = (sizeOfLikertScaleInternal - 1) - prevAnswer;

                var toggles = buttonFrame.GetComponentsInChildren<Toggle>();
                toggles[prevAnswer].isOn = true;
            }
        }

        // Start timing editing
        timer.Reset();
        timer.Start();
    }

    private void StoreQuestionDataAndDestroy()
    {
        int minIndex = GetLowestDisplayedQuestionIndex();

        int childCount = headerFrame.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            timer.Stop();
            int elapsedSec = (int)timer.Elapsed.TotalSeconds;
            questions[minIndex + i].StoreEditingTime(elapsedSec);
            questions[minIndex + i].SetIsOnDisplay(false);

            var btnFrame = headerFrame.transform.GetChild(i)
                .Find("Q_BottomFrame/ButtonFrame").gameObject;

            for (int j = 0; j < btnFrame.transform.childCount; j++)
            {
                var toggle = btnFrame.transform.GetChild(j)
                    .GetComponent<Toggle>();
                if (toggle.isOn)
                {
                    int ans = questions[minIndex + i].GetIsInverted()
                        ? (sizeOfLikertScaleInternal - 1) - j
                        : j;
                    questions[minIndex + i].SetAnswer(ans);
                    break;
                }
            }

            Destroy(headerFrame.transform.GetChild(i).gameObject);
        }
    }

    private void CreateAnswerCSV()
    {
        bool isWindows = Application.platform == RuntimePlatform.WindowsEditor;
        string delimiter = "; ";
        string extension = ".csv";
        string path = Path.Combine(outputDataStoragePath, outputDataName + extension);

        // Write header if file doesn't exist
        if (!File.Exists(path))
        {
            using var writer = new StreamWriter(path);
            writer.Write("Subject Name" + delimiter + "Subject Code" + delimiter);
            for (int i = 0; i < questions.Count; i++)
            {
                writer.Write(storeQuestionText
                    ? questions[i].verbalLabels[0].Replace(";", " ")
                    : $"Question {i}");

                if (outputEditingTime) writer.Write(" (t in s)");
                writer.Write(delimiter);
            }
            writer.WriteLine();
        }

        int offset = 0;
        if (storeBipolarAnswerValue && sizeOfLikertScaleInternal % 2 == 1)
            offset = Mathf.FloorToInt(sizeOfLikertScaleInternal / 2f);

        using var appender = File.AppendText(path);
        string result = string.Empty;

        result += string.IsNullOrEmpty(subjectName)
            ? "Subject" + delimiter
            : subjectName + delimiter;
        result += string.IsNullOrEmpty(subjectCode)
            ? "no subject code given" + delimiter
            : subjectCode + delimiter;

        foreach (var q in questions)
        {
            result += q.GetAnswer() >= 0
                ? (q.GetAnswer() - offset).ToString()
                : "no answer given";

            if (outputEditingTime)
                result += " (" + q.GetEditingTime() + ")";

            result += delimiter;
        }

        appender.WriteLine(result);
    }

    private int GetHighestDisplayedQuestionIndex()
    {
        int index = 0;
        for (int i = 0; i < questions.Count; i++)
            if (questions[i].GetIsOnDisplay()) index = i;
        return index;
    }

    private int GetLowestDisplayedQuestionIndex()
    {
        for (int i = 0; i < questions.Count; i++)
            if (questions[i].GetIsOnDisplay()) return i;
        return 0;
    }

    #endregion
}
