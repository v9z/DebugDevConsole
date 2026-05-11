using UnityEngine;

public static class ConsoleStartScript
{
    private const string ConsolePrefabPath = "ConsoleCanvas";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (Object.FindFirstObjectByType<DebugConsoleUI>() != null)
        {
            return;
        }

        GameObject consolePrefab = Resources.Load<GameObject>(ConsolePrefabPath);

        if (consolePrefab == null)
        {
            Debug.LogError($"DebugConsole: Could not find prefab at Resources/{ConsolePrefabPath}");
            return;
        }

        GameObject consoleInstance = Object.Instantiate(consolePrefab);
        Object.DontDestroyOnLoad(consoleInstance);

        #endif
    }
}