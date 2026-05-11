using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AutoComplete
{
    private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private Transform ignoredRoot;

    private static readonly string[] BaseCommands =
    {
        "help",
        "echo",
        "clear",
        "find",
        "inspect",
        "get",
        "set"
    };

    public AutoComplete(Transform ignoredRoot)
    {
        this.ignoredRoot = ignoredRoot;
    }

    private bool ShouldIgnoreObject(GameObject obj)
    {
        if (ignoredRoot == null)
        {
            return false;
        }

        return obj.transform == ignoredRoot || obj.transform.IsChildOf(ignoredRoot);
    }

    public List<string> GetSuggestions(string input)
    {
        List<string> suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
        {
            suggestions.AddRange(BaseCommands);
            return suggestions;
        }

        input = input.TrimStart();

        string[] split = input.Split(' ', 2);

        if (split.Length == 1 && !input.Contains(" "))
        {
            AddMatches(suggestions, BaseCommands, input);
            return suggestions;
        }

        string command = split[0];
        string argumentText = split.Length > 1 ? split[1] : "";

        switch (command)
        {
            case "find":
            case "inspect":
                AddObjectSuggestions(suggestions, argumentText);
                break;

            case "get":
                AddPathSuggestions(suggestions, argumentText);
                break;

            case "set":
                AddPathSuggestions(suggestions, argumentText);
                break;
        }

        return suggestions;
    }

    private void AddMatches(List<string> suggestions, IEnumerable<string> source, string partial)
    {
        string lowerPartial = partial.ToLower();

        foreach (string item in source)
        {
            if (item.ToLower().StartsWith(lowerPartial))
            {
                suggestions.Add(item);
            }
        }
    }

    private void AddObjectSuggestions(List<string> suggestions, string partialObjectName)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        string lowerPartial = partialObjectName.ToLower();

        foreach (GameObject obj in allObjects)
        {
            if (ShouldIgnoreObject(obj))
            {
                continue;
            }

            if (obj.name.ToLower().StartsWith(lowerPartial) && !suggestions.Contains(obj.name))
            {
                suggestions.Add(obj.name);
            }
        }
    }

    private void AddPathSuggestions(List<string> suggestions, string partialPath)
    {
        string[] parts = partialPath.Split('.');

        if (parts.Length == 1)
        {
            AddObjectSuggestions(suggestions, parts[0]);
            return;
        }

        if (parts.Length == 2)
        {
            GameObject targetObject = FindObjectByExactName(parts[0]);

            if (targetObject == null)
            {
                return;
            }

            string componentPartial = parts[1].ToLower();
            Component[] components = targetObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                string componentName = component.GetType().Name;

                if (componentName.ToLower().StartsWith(componentPartial))
                {
                    suggestions.Add($"{targetObject.name}.{componentName}");
                }
            }

            return;
        }

        if (parts.Length == 3)
        {
            GameObject targetObject = FindObjectByExactName(parts[0]);

            if (targetObject == null)
            {
                return;
            }

            Component targetComponent = FindComponentByName(targetObject, parts[1]);

            if (targetComponent == null)
            {
                return;
            }

            string fieldPartial = parts[2].ToLower();
            FieldInfo[] fields = targetComponent.GetType().GetFields(_bindingFlags);

            foreach (FieldInfo field in fields)
            {
                if (field.Name.ToLower().StartsWith(fieldPartial))
                {
                    suggestions.Add($"{targetObject.name}.{targetComponent.GetType().Name}.{field.Name}");
                }
            }

            PropertyInfo[] properties = targetComponent.GetType().GetProperties(_bindingFlags);

            foreach (PropertyInfo property in properties)
            {
                if (property.Name.ToLower().StartsWith(fieldPartial))
                {
                    suggestions.Add($"{targetObject.name}.{targetComponent.GetType().Name}.{property.Name}");
                }
            }
        }
    }

    private GameObject FindObjectByExactName(string objectName)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (ShouldIgnoreObject(obj))
            {
                continue;
            }

            if (obj.name == objectName)
            {
                return obj;
            }
        }

        return null;
    }

    private Component FindComponentByName(GameObject targetObject, string componentName)
    {
        Component[] components = targetObject.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            if (component.GetType().Name == componentName)
            {
                return component;
            }
        }

        return null;
    }
}