using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class CSVLogger : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("Ordnername im öffentlichen Download-Verzeichnis (/sdcard/Download/<Subfolder>).")]
    public string publicDownloadSubfolder = "RDW";
    [Tooltip("Auf Android direkt in den öffentlichen Download-Ordner schreiben.")]
    public bool writeToPublicDownloadOnAndroid = true;
    [Tooltip("CSV-Trennzeichen")]
    public char separator = ';';

    private string _filePath;
    private List<string> _headerKeys = new List<string>();

    public string GetFilePath() => _filePath;

    /// <summary>
    /// Initialisiert die CSV. Wenn fileName leer ist, wird ein Zeitstempel verwendet.
    /// </summary>
    public void InitCSV(List<string> headerKeys, string fileName = null, char? sepOverride = null, bool? forcePublicDownload = null)
    {
        if (headerKeys == null || headerKeys.Count == 0)
            throw new ArgumentException("headerKeys must not be empty");

        _headerKeys = new List<string>(headerKeys);

        if (sepOverride.HasValue) separator = sepOverride.Value;
        if (forcePublicDownload.HasValue) writeToPublicDownloadOnAndroid = forcePublicDownload.Value;

        if (string.IsNullOrEmpty(fileName))
            fileName = $"RDW_ExperimentLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        string dir = GetBaseDirectory();
        Directory.CreateDirectory(dir);

        _filePath = Path.Combine(dir, fileName);

        if (!File.Exists(_filePath))
        {
            WriteLine(_headerKeys); // Header
        }

        Debug.Log($"[CSVLogger] Writing to: {_filePath}");
    }

    /// <summary>
    /// Loggt eine Zeile. Keys werden in Header-Reihenfolge geschrieben.
    /// Fehlende Keys => leere Felder.
    /// </summary>
    public void LogTrial(Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            Debug.LogWarning("[CSVLogger] Not initialized. Call InitCSV() first.");
            return;
        }

        IEnumerable<string> cols = _headerKeys.Select(k =>
        {
            object v;
            if (data != null && data.TryGetValue(k, out v) && v != null)
                return v.ToString();
            return "";
        });

        WriteLine(cols);
    }

    // ---------- intern ----------

    string GetBaseDirectory()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (writeToPublicDownloadOnAndroid)
        {
            // öffentlicher Download-Ordner (sichtbar per USB/MTP)
            string d = $"/sdcard/Download/{publicDownloadSubfolder}";
            return d;
        }
        // fallback: App-spezifischer Bereich
        return Application.persistentDataPath;
#else
        // Editor/Windows: lege neben persistentDataPath einen RDW-Ordner an
        return Path.Combine(Application.persistentDataPath, publicDownloadSubfolder);
#endif
    }

    void WriteLine(IEnumerable<string> cols)
    {
        // robust gegen Sharing Violations
        using (var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        using (var sw = new StreamWriter(fs, new UTF8Encoding(true)))
        {
            bool first = true;
            foreach (var c in cols)
            {
                if (!first) sw.Write(separator);
                first = false;
                sw.Write(Escape(c));
            }
            sw.WriteLine();
        }
    }

    string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // Newlines entfernen
        s = s.Replace("\r", " ").Replace("\n", " ");
        // wenn Trennzeichen oder Quotes enthalten -> CSV-quote
        if (s.IndexOf(separator) >= 0 || s.Contains("\""))
        {
            s = "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        return s;
    }
}
