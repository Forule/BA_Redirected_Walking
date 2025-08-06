using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// CSV-Logger für VR-Experimente. Speichert jede Trial-Zeile als CSV im persistentDataPath.
/// </summary>
public class CSVLogger : MonoBehaviour
{
    private string filePath;
    private bool fileInitialized = false;
    private List<string> headers = null;

    /// <summary>
    /// Muss EINMALIG vor dem ersten Loggen aufgerufen werden.
    /// </summary>
    /// <param name="headerFields">Reihenfolge der Variablennamen (Spaltenüberschriften)</param>
    public void InitCSV(List<string> headerFields)
    {
        Debug.Log("CSV intiialized");
        headers = new List<string>(headerFields);
        filePath = Path.Combine(Application.persistentDataPath, "RDW_ExperimentLog.csv");
        fileInitialized = true;
        Debug.Log("CSV-Logger-Pfad: " + Application.persistentDataPath);

        if (!File.Exists(filePath))
        {
            var headerLine = string.Join(",", headers);
            File.WriteAllText(filePath, headerLine + "\n", Encoding.UTF8);
        }
    }

    /// <summary>
    /// Fügt eine Zeile mit Daten an die CSV an. Die Keys im Dictionary müssen exakt zu den Headern passen!
    /// </summary>
    public void LogTrial(Dictionary<string, object> data)
    {
        if (!fileInitialized || headers == null) return;

        var line = new StringBuilder();
        foreach (var key in headers)
        {
            object value = data.ContainsKey(key) ? data[key] : "";
            string cell = value != null ? value.ToString().Replace("\"", "\"\"") : "";
            if (cell.Contains(",")) cell = $"\"{cell}\"";
            line.Append(cell + ",");
        }
        if (line.Length > 0) line.Length--;

        File.AppendAllText(filePath, line + "\n", Encoding.UTF8);
    }

    /// <summary>
    /// Gibt den Dateipfad der aktuellen CSV-Datei zurück (Debugging).
    /// </summary>
    public string GetFilePath() => filePath;
}
