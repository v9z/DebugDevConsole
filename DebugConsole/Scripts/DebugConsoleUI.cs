using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

public class DebugConsoleUI : MonoBehaviour
{
    [SerializeField] GameObject consoleRoot;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TMP_Text outputText;
    [SerializeField] TMP_Text autocompleteText;

    [SerializeField] int maxLines = 100;

    [Header("KeyBinds")]
    [SerializeField] Key toggleKey = Key.Backquote;
    [SerializeField] Key autoKey = Key.Tab;
    [SerializeField] Key PreviousCommandUp = Key.PageUp;
    [SerializeField] Key PreviousCommandDown = Key.PageDown;

    bool isVisible = false;

    private readonly List<string> consoleLines = new();
    private readonly CommandProcessor commandProcessor = new();
    private AutoComplete autoComplete;

    private List<string> currentSuggestions = new();
    private int selectedSuggestionIndex = 0;

    private readonly List<string> commandHistory = new();
    private int historyIndex = -1;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    void Start()
    {
        SetConsoleVisible(false);

        inputField.onSubmit.AddListener(HandleSubmit);
        inputField.onValueChanged.AddListener(HandleInputChanged);

        RefreshOutputText();

        autoComplete = new AutoComplete(consoleRoot.transform);
    }
    private void OnDestroy()
    {
        inputField.onSubmit.RemoveListener(HandleSubmit);
        inputField.onValueChanged.RemoveListener(HandleInputChanged);
    }

    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard[toggleKey].wasPressedThisFrame) 
        { 
            SetConsoleVisible(!isVisible);
        }

        if (isVisible && keyboard.downArrowKey.wasPressedThisFrame)
        {
            MoveSuggestionSelection(1);
        }

        if (isVisible && keyboard.upArrowKey.wasPressedThisFrame)
        {
            MoveSuggestionSelection(-1);
        }

        if (isVisible && keyboard[autoKey].wasPressedThisFrame)
        {
            ApplySelectedSuggestion();
        }

        if (isVisible && keyboard[PreviousCommandUp].wasPressedThisFrame)
        {
            ShowPreviousCommand();
        }

        if (isVisible && keyboard[PreviousCommandDown].wasPressedThisFrame)
        {
            ShowNextCommand();
        }
    }

    private void SetConsoleVisible(bool visible)
    {
        isVisible = visible;

        if (consoleRoot != null)
        {
            consoleRoot.SetActive(visible);
        }

        if (visible && inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    public void CloseButton()
    {
        SetConsoleVisible(false);
    }

    private string ColorText(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    private string CommandColor(string text) => ColorText(text, "#6AD7FF");
    private string WarningColor(string text) => ColorText(text, "#FFD966");
    private string ErrorColor(string text) => ColorText(text, "#FF6666");

    private bool IsErrorMessage(string message)
    {
        return message.StartsWith("Unknown command") ||
               message.StartsWith("Object not found") ||
               message.StartsWith("Component") ||
               message.StartsWith("Field") ||
               message.StartsWith("Member") ||
               message.StartsWith("Property") ||
               message.StartsWith("Could not convert") ||
               message.StartsWith("Type") ||
               message.StartsWith("Usage") ||
               message.StartsWith("Path must");
    }

    private void HandleSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
            return;
        }

        commandHistory.Add(input);
        historyIndex = commandHistory.Count;

        AppendLine(CommandColor($"> {input}"));

        string result = commandProcessor.ProcessCommand(input);

        if (result == "__CLEAR__")
        {
            ClearOutput();
        }
        else if (!string.IsNullOrWhiteSpace(result))
        {
            if (IsErrorMessage(result))
            {
                AppendLine(ErrorColor(result));
            }
            else
            {
                AppendLine(result);
            }
        }

        inputField.text = string.Empty;
        inputField.ActivateInputField();

        currentSuggestions.Clear();

        if (autocompleteText != null)
        {
            autocompleteText.text = "";
        }
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                AppendLine(WarningColor("[Warning] " + logString));
                break;

            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                AppendLine(ErrorColor($"[{type}] " + logString));

                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    AppendLine(ErrorColor(stackTrace));
                }

                break;

            default:
                AppendLine(logString);
                break;
        }
    }

    private void AppendLine(string text)
    {
        consoleLines.Add(text);

        if (consoleLines.Count > maxLines)
        {
            consoleLines.RemoveAt(0);
        }

        RefreshOutputText();
    }

    private void ClearOutput()
    {
        consoleLines.Clear();
        RefreshOutputText();
    }

    private void RefreshOutputText()
    {
        if (outputText == null)
        {
            return;
        }

        outputText.text = string.Join("\n", consoleLines);
    }

    private void RefreshAutocomplete()
    {
        if (autocompleteText == null || inputField == null)
        {
            return;
        }

        currentSuggestions = autoComplete.GetSuggestions(inputField.text);

        if (currentSuggestions.Count == 0)
        {
            selectedSuggestionIndex = 0;
            autocompleteText.text = "";
            return;
        }

        if (selectedSuggestionIndex >= currentSuggestions.Count)
        {
            selectedSuggestionIndex = 0;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < currentSuggestions.Count; i++)
        {
            if (i == selectedSuggestionIndex)
            {
                builder.AppendLine($"> {currentSuggestions[i]}");
            }
            else
            {
                builder.AppendLine($"  {currentSuggestions[i]}");
            }
        }

        autocompleteText.text = builder.ToString().TrimEnd();
    }

    private void MoveSuggestionSelection(int direction)
    {
        if (currentSuggestions.Count == 0)
        {
            return;
        }

        selectedSuggestionIndex += direction;

        if (selectedSuggestionIndex < 0)
        {
            selectedSuggestionIndex = currentSuggestions.Count - 1;
        }
        else if (selectedSuggestionIndex >= currentSuggestions.Count)
        {
            selectedSuggestionIndex = 0;
        }

        RefreshAutocomplete();
    }

    private void HandleInputChanged(string currentInput)
    {
        selectedSuggestionIndex = 0;
        RefreshAutocomplete();
    }

    private void ApplySelectedSuggestion()
    {
        if (inputField == null || currentSuggestions.Count == 0)
        {
            return;
        }

        string selectedSuggestion = currentSuggestions[selectedSuggestionIndex];

        string currentInput = inputField.text.TrimStart();
        string[] split = currentInput.Split(' ', 2);

        if (split.Length == 1 && !currentInput.Contains(" "))
        {
            inputField.text = selectedSuggestion;
        }
        else
        {
            string command = split[0];
            inputField.text = $"{command} {selectedSuggestion}";
        }

        inputField.caretPosition = inputField.text.Length;
        selectedSuggestionIndex = 0;
        RefreshAutocomplete();
    }

    private void ShowPreviousCommand()
    {
        if (commandHistory.Count == 0)
        {
            return;
        }

        historyIndex--;

        if (historyIndex < 0)
        {
            historyIndex = 0;
        }

        SetInputText(commandHistory[historyIndex]);
    }

    private void ShowNextCommand()
    {
        if (commandHistory.Count == 0)
        {
            return;
        }

        historyIndex++;

        if (historyIndex >= commandHistory.Count)
        {
            historyIndex = commandHistory.Count;
            SetInputText(string.Empty);
            return;
        }

        SetInputText(commandHistory[historyIndex]);
    }

    private void SetInputText(string text)
    {
        inputField.text = text;
        inputField.caretPosition = inputField.text.Length;
        inputField.ActivateInputField();
        RefreshAutocomplete();
    }
}
