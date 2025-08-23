using UnityEngine;
using System.Reflection;
using System.Collections;
using System;

#if UNITY_EDITOR
using NaughtyAttributes.Editor;
using UnityEditor;
#endif

#if UNITY_EDITOR
public class MultiselectAttribute : PropertyAttribute
{
    public string OptionsName { get; private set; }

    public MultiselectAttribute(string _optionsName)
    {
        OptionsName = _optionsName;
    }
}

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