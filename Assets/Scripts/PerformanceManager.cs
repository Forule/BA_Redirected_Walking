using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [Tooltip("Set to -1 for unlimited FPS, or z.B. 90 für 90 Hz")]
    public int targetFrameRate = -1;

    void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0; // VSync aus, Framerate nicht an Monitor gebunden
        Debug.Log("PerformanceManager: targetFrameRate = " + targetFrameRate + ", VSync aus.");
    }
}
