using UnityEngine;
using TMPro;

public class ConsoleToText : MonoBehaviour
{
    [SerializeField] private TMP_Text consoleOutput;
    [SerializeField] private int maxTotalLines = 100;
    [SerializeField] private int maxLogEntryLength = 500;

    private int lastLogCount = 1;
    private string lastLogMessage = string.Empty;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message = $"[{type}] {logString}";

        // Limit the length of a single log entry
        if (message.Length > maxLogEntryLength)
        {
            message = message.Substring(0, maxLogEntryLength) + "...";
        }

        // Check if the new log entry matches the last one
        if (message == lastLogMessage)
        {
            lastLogCount++;
            string[] lines = consoleOutput.text.Split(new[] { '\n' }, System.StringSplitOptions.None);
            lines[lines.Length - 1] = $"{lastLogMessage} (x{lastLogCount})";
            consoleOutput.text = string.Join("\n", lines);
        }
        else
        {
            lastLogMessage = message;
            lastLogCount = 1;
            consoleOutput.text += "\n" + message;
        }

        // Optional: limit text length to avoid performance issues by restricting the number of lines
        string[] allLines = consoleOutput.text.Split(new[] { '\n' }, System.StringSplitOptions.None);
        if (allLines.Length > maxTotalLines)
        {
            consoleOutput.text = string.Join("\n", allLines, allLines.Length - maxTotalLines, maxTotalLines);
        }
    }
}
