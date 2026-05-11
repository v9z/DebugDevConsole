using System;
using System.Reflection;
using System.Text;
using UnityEngine;

public class CommandProcessor
{
    private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public string ProcessCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        input = input.Trim();

        if (input == "help")
        {
            return "Available commands: help, echo, clear, find, inspect, get, set";
        }

        if (input == "clear")
        {
            return "__CLEAR__";
        }

        if (input.StartsWith("echo "))
        {
            return input.Substring(5);
        }

        if (input.StartsWith("find "))
        {
            return FindObjectsByName(input.Substring(5).Trim());
        }

        if (input.StartsWith("inspect "))
        {
            return InspectTarget(input.Substring(8).Trim());
        }

        if (input.StartsWith("get "))
        {
            return GetValue(input.Substring(4).Trim());
        }

        if (input.StartsWith("set "))
        {
            return SetValue(input.Substring(4).Trim());
        }

        return "Unknown command";
    }

    private string FindObjectsByName(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return "Usage: find <objectName>";
        }

        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        StringBuilder builder = new StringBuilder();
        int matchCount = 0;
        string lowerSearchTerm = searchTerm.ToLower();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains(lowerSearchTerm))
            {
                builder.AppendLine(obj.name);
                matchCount++;
            }
        }

        if (matchCount == 0)
        {
            return $"No objects found matching: {searchTerm}";
        }

        return $"Found {matchCount} object(s):\n{builder}".TrimEnd();
    }

    private string GetValue(string path)
    {
        if (!TryResolveMemberPath(path, out ResolvedMember member, out string error))
        {
            return error;
        }

        object value = member.GetValue();
        string valueText = value != null ? value.ToString() : "null";

        return $"{member.TargetObject.name}.{member.TargetComponent.GetType().Name}.{member.Name} = {valueText}";
    }

    private string SetValue(string setArguments)
    {
        int firstSpaceIndex = setArguments.IndexOf(' ');

        if (firstSpaceIndex == -1)
        {
            return "Usage: set <Object.Component.member> <value>";
        }

        string path = setArguments.Substring(0, firstSpaceIndex).Trim();
        string valueText = setArguments.Substring(firstSpaceIndex + 1).Trim();

        if (!TryResolveMemberPath(path, out ResolvedMember member, out string error))
        {
            return error;
        }

        if (!TryConvertValue(valueText, member.ValueType, out object convertedValue, out string conversionError))
        {
            return conversionError;
        }

        member.SetValue(convertedValue);

        return $"Set {member.TargetObject.name}.{member.TargetComponent.GetType().Name}.{member.Name} to {convertedValue}";
    }

    private bool TryResolveMemberPath(string path, out ResolvedMember member, out string error)
    {
        member = null;
        error = string.Empty;

        string[] parts = path.Split('.');

        if (parts.Length != 3)
        {
            error = "Path must be in format: Object.Component.member";
            return false;
        }

        string objectName = parts[0];
        string componentName = parts[1];
        string memberName = parts[2];

        GameObject targetObject = FindObjectByExactName(objectName);

        if (targetObject == null)
        {
            error = $"Object not found: {objectName}";
            return false;
        }

        Component targetComponent = FindComponentByName(targetObject, componentName);

        if (targetComponent == null)
        {
            error = $"Component '{componentName}' not found on object '{objectName}'";
            return false;
        }

        Type componentType = targetComponent.GetType();

        FieldInfo field = componentType.GetField(memberName, _bindingFlags);

        if (field != null)
        {
            member = new ResolvedMember
            {
                TargetObject = targetObject,
                TargetComponent = targetComponent,
                Field = field
            };

            return true;
        }

        PropertyInfo property = componentType.GetProperty(memberName, _bindingFlags);

        if (property != null)
        {
            if (!property.CanRead)
            {
                error = $"Property '{memberName}' cannot be read";
                return false;
            }

            member = new ResolvedMember
            {
                TargetObject = targetObject,
                TargetComponent = targetComponent,
                Property = property
            };

            return true;
        }

        error = $"Member '{memberName}' not found on component '{componentName}'";
        return false;
    }

    private bool TryConvertValue(string valueText, Type targetType, out object convertedValue, out string error)
    {
        convertedValue = null;
        error = string.Empty;

        if (targetType == typeof(string))
        {
            convertedValue = valueText;
            return true;
        }

        if (targetType == typeof(int))
        {
            if (int.TryParse(valueText, out int intValue))
            {
                convertedValue = intValue;
                return true;
            }

            error = $"Could not convert '{valueText}' to int";
            return false;
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse(valueText, out float floatValue))
            {
                convertedValue = floatValue;
                return true;
            }

            error = $"Could not convert '{valueText}' to float";
            return false;
        }

        if (targetType == typeof(bool))
        {
            if (bool.TryParse(valueText, out bool boolValue))
            {
                convertedValue = boolValue;
                return true;
            }

            error = $"Could not convert '{valueText}' to bool";
            return false;
        }

        if (targetType == typeof(Vector3))
        {
            string[] parts = valueText.Split(',');

            if (parts.Length != 3)
            {
                error = "Vector3 format must be: x,y,z";
                return false;
            }

            if (!float.TryParse(parts[0], out float x) ||
                !float.TryParse(parts[1], out float y) ||
                !float.TryParse(parts[2], out float z))
            {
                error = $"Could not convert '{valueText}' to Vector3";
                return false;
            }

            convertedValue = new Vector3(x, y, z);
            return true;
        }

        error = $"Type '{targetType.Name}' is not currently supported";
        return false;
    }

    private class ResolvedMember
    {
        public GameObject TargetObject;
        public Component TargetComponent;
        public FieldInfo Field;
        public PropertyInfo Property;

        public string Name => Field != null ? Field.Name : Property.Name;

        public Type ValueType => Field != null ? Field.FieldType : Property.PropertyType;

        public object GetValue()
        {
            if (Field != null)
            {
                return Field.GetValue(TargetComponent);
            }

            return Property.GetValue(TargetComponent);
        }

        public void SetValue(object value)
        {
            if (Field != null)
            {
                Field.SetValue(TargetComponent, value);
                return;
            }

            if (!Property.CanWrite)
            {
                throw new InvalidOperationException($"Property '{Property.Name}' cannot be written to.");
            }

            Property.SetValue(TargetComponent, value);
        }
    }


    #region Inspect
    private string InspectTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return "Usage: inspect <objectName> or inspect <objectName.componentName>";
        }

        string[] parts = target.Split('.');

        if (parts.Length == 1)
        {
            return InspectObject(parts[0]);
        }

        if (parts.Length == 2)
        {
            return InspectComponent(parts[0], parts[1]);
        }

        return "Usage: inspect <objectName> or inspect <objectName.componentName>";
    }

    private string InspectObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return "Usage: inspect <objectName>";
        }

        GameObject targetObject = FindObjectByExactName(objectName);

        if (targetObject == null)
        {
            return $"Object not found: {objectName}";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Object: {targetObject.name}");
        builder.AppendLine();

        Component[] components = targetObject.GetComponents<Component>();

        foreach (Component component in components)
        {
            AppendComponentInspection(builder, component);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private string InspectComponent(string objectName, string componentName)
    {
        GameObject targetObject = FindObjectByExactName(objectName);

        if (targetObject == null)
        {
            return $"Object not found: {objectName}";
        }

        Component targetComponent = FindComponentByName(targetObject, componentName);

        if (targetComponent == null)
        {
            return $"Component '{componentName}' not found on object '{objectName}'";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Object: {targetObject.name}");
        builder.AppendLine();

        AppendComponentInspection(builder, targetComponent);

        return builder.ToString().TrimEnd();
    }

    private void AppendComponentInspection(StringBuilder builder, Component component)
    {
        if (component == null)
        {
            builder.AppendLine("Component: Missing Script");
            return;
        }

        Type componentType = component.GetType();
        builder.AppendLine($"Component: {componentType.Name}");

        FieldInfo[] fields = componentType.GetFields(_bindingFlags);

        if (fields.Length == 0)
        {
            builder.AppendLine("- No fields found");
            return;
        }

        foreach (FieldInfo field in fields)
        {
            string accessModifier = field.IsPublic ? "public" : "private";
            object value = field.GetValue(component);
            string valueText = value != null ? value.ToString() : "null";

            builder.AppendLine($"- {accessModifier} {field.Name} = {valueText}");
        }
    }

    private GameObject FindObjectByExactName(string objectName)
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
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

    #endregion
}