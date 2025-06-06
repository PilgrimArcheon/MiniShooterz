using System.Collections.Generic;
using UnityEngine;

public static class TextTool
{
    static string textToCopy;
    static bool showCopiedMessage;

    static void OnGUI()
    {
        // Display "Copied!" message
        if (showCopiedMessage)
        {
            GUI.Label(new Rect(10, 60, 200, 40), textToCopy + "\n Copied!");
        }
    }

    public static string ToSentenceCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.Trim();
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    public static IDictionary<string, object> ConvertJsonToDictionary(string json)
    {
        Dictionary<string, object> dictionary = new();
        string cleanJson = json.TrimStart('{').TrimEnd('}');
        string[] pairs = cleanJson.Split(',');

        foreach (string pair in pairs)
        {
            string[] kvp = pair.Split(':');
            string key = kvp[0].Trim(' ', '"');
            string value = kvp[1].Trim(' ', '"');

            if (int.TryParse(value, out int intValue))
                dictionary[key] = intValue;
            else if (float.TryParse(value, out float floatValue))
                dictionary[key] = floatValue;
            else if (bool.TryParse(value, out bool boolValue))
                dictionary[key] = boolValue;
            else
                dictionary[key] = value;
        }

        return dictionary;
    }
    public static void CopyTextToClipboard(string text)
    {
        textToCopy = text;
        GUIUtility.systemCopyBuffer = text;
        showCopiedMessage = true;

        DelayedCall(1500);
    }

    public static async void DelayedCall(int delayMilliseconds)
    {
        await System.Threading.Tasks.Task.Delay(delayMilliseconds);
        showCopiedMessage = true;
    }
}