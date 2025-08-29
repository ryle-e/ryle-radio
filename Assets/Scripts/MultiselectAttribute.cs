using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using NaughtyAttributes.Editor;
using UnityEditor;

using System.Reflection;
using System.Collections;
using System;
#endif

public class MultiselectAttribute : PropertyAttribute
{
    public string OptionsName { get; private set; }

    public MultiselectAttribute(string _optionsName)
    {
        OptionsName = _optionsName;
    }


    public static int[] ZeroTo31 => new int[32] {
        0, 1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31
    };

    public static int[] OneTo32 => new int[32] {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31,
        32
    };

    public static T[] To<T>(int _flags, T[] _options)
    {
        int[] outIndexes = new int[32];

        for (int i = 0; i < 32; i++)
        {
            if ((_flags & (1 << i)) != 0)
                outIndexes[i] = i;
            else
                outIndexes[i] = -1;
        }

        T[] o = outIndexes
            .Where(b => b >= 0)
            .Select(i => _options[i])
            .ToArray();

        return o;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MultiselectAttribute))]
public class MultiselectDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int mask = property.intValue;
        string dataName = ((MultiselectAttribute)attribute).OptionsName;

        object options = GetValues(property, dataName);
        string[] optionNames;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        if (options != null)
        {
            if (options is IList list)
            {
                if (list.Count > 0)
                {
                    optionNames = new string[list.Count];

                    for (int i = 0; i < list.Count; i++)
                        optionNames[i] = list[i].ToString();

                    mask = EditorGUILayout.MaskField(mask, optionNames);
                }
                else
                {
                    EditorGUILayout.LabelField($"{dataName} has size 0!");
                }
            }
            else if (options is Array array)
            {
                if (array.Length > 0)
                { 
                    optionNames = new string[array.Length];

                    for (int i = 0; i < array.Length; i++)
                        optionNames[i] = array.GetValue(i).ToString();

                    mask = EditorGUILayout.MaskField(mask, optionNames);
                }
                else
                {
                    EditorGUILayout.LabelField($"{dataName} has size 0!");
                }
            }
            else
            {
                EditorGUILayout.LabelField($"{dataName} is not a List or an Array!");
            }
        }
        else
            EditorGUILayout.LabelField($"Invalid collection at {dataName}! Cannot display multiselect!");

        EditorGUILayout.EndHorizontal();

        property.intValue = mask;
    }

    // taken from DropdownPropertyDrawer.cs in NaughtyAttributes.Editor
    private object GetValues(SerializedProperty property, string valuesName)
    {
        object target = PropertyUtility.GetTargetObjectWithProperty(property);

        FieldInfo valuesFieldInfo = ReflectionUtility.GetField(target, valuesName);
        if (valuesFieldInfo != null)
        {
            return valuesFieldInfo.GetValue(target);
        }

        PropertyInfo valuesPropertyInfo = ReflectionUtility.GetProperty(target, valuesName);
        if (valuesPropertyInfo != null)
        {
            return valuesPropertyInfo.GetValue(target);
        }

        MethodInfo methodValuesInfo = ReflectionUtility.GetMethod(target, valuesName);
        if (methodValuesInfo != null &&
            methodValuesInfo.ReturnType != typeof(void) &&
            methodValuesInfo.GetParameters().Length == 0)
        {
            return methodValuesInfo.Invoke(target, null);
        }

        return null;
    }
}
#endif