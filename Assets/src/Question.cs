using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a questionnaire question.
/// This class is not attached to any GameObject.
/// </summary>
public class Question : MonoBehaviour
{
    // Stores the answer the subject has given; -1 means unanswered
    private int answer = -1;

    // Total time this question has been displayed (in seconds)
    private int editingTime = 0;

    // Indicates whether the question is currently being displayed
    private bool isOnDisplay = false;

    // Indicates whether the question uses an inverted scale
    private bool isInverted = false;

    // List containing: [0] = question text, [1...] = verbal labels from left to right
    public List<string> verbalLabels = new List<string>();

    // Getter and Setter for inversion
    public bool GetIsInverted() => isInverted;
    public void SetIsInverted(bool value) => isInverted = value;

    // Getter and Setter for display status
    public bool GetIsOnDisplay() => isOnDisplay;
    public void SetIsOnDisplay(bool value) => isOnDisplay = value;

    // Getter and updater for editing time
    public int GetEditingTime() => editingTime;
    public void StoreEditingTime(int time) => editingTime += time;

    // Getter and Setter for answer
    public int GetAnswer() => answer;
    public void SetAnswer(int value) => answer = value;
}