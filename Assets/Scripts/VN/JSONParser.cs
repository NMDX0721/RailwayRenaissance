using UnityEngine;

public class JSONParser
{
    public ScriptData LoadScript(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName))
        {
            Debug.LogError("Script name is null or empty.");
            return null;
        }

        string resourcePath = "Scripts/" + scriptName;
        TextAsset jsonAsset = LoadJsonAsset(resourcePath);

        if (jsonAsset == null)
        {
            Debug.LogError("Failed to load JSON asset at path: " + resourcePath);
            return null;
        }

        string jsonText = jsonAsset.text;

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("Loaded JSON text is null or empty for script: " + scriptName);
            return null;
        }

        try
        {
            ScriptData scriptData = JsonUtility.FromJson<ScriptData>(jsonText);
            return scriptData;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse JSON for script " + scriptName + ": " + e.Message);
            return null;
        }
    }

    private TextAsset LoadJsonAsset(string resourcePath)
    {
        return Resources.Load<TextAsset>(resourcePath);
    }
}