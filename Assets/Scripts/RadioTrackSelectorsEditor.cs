using UnityEngine;
using System.Reflection;
using System.Collections;
using System;

#if UNITY_EDITOR
using NaughtyAttributes.Editor;
using UnityEditor;
#endif

public class RadioTrackSelectorAttribute : PropertyAttribute
{
    public string DataName { get; private set; }

    public RadioTrackSelectorAttribute(string _optionsName)
    {
        DataName = _optionsName;
    }
}

public class MultiRadioTrackSelectorAttribute : PropertyAttribute
{
    public string DataName { get; private set; }

    public MultiRadioTrackSelectorAttribute(string _optionsName)
    {
        DataName = _optionsName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RadioTrackSelectorAttribute))]
public class RadioTrackSelectorDrawer : PropertyDrawer
{
    protected virtual void DrawDropdown()
    {

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string dataName = ((RadioTrackSelectorAttribute)attribute).DataName;

        var dataProperty = property.FindPropertyRelative(dataName);
        var namesProperty = dataProperty.FindPropertyRelative("TrackNames");

        var name = property.FindPropertyRelative("id");

        RadioData data = GetValues(property, dataName) as RadioData;
        string[] options = data.TrackNames.ToArray();

        //var thing = property.Find

        int index = 0;

        try
        {
            //index = namesProperty.arr
            //index = Mathf.Max(0, data.Tracks.IndexOf((RadioTrack) property.boxedValue));
        }
        catch (InvalidCastException e)
        {
            Debug.LogError(property.name + " is not a RadioTrack, but has the RadioTrackSelectorAttribute!");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        if (data != null)
        {
            int i = EditorGUILayout.Popup(index, options);
            RadioTrackWrapper track = data.TrackWrappers[i];

            if (track != null)
                property.boxedValue = track; 

            property.serializedObject.ApplyModifiedProperties();
        }
        else
            EditorGUILayout.TextArea($"Assign RadioData to {dataName} to select tracks!");

        EditorGUILayout.EndHorizontal();
    }

    // taken from DropdownPropertyDrawer.cs in NaughtyAttributes.Editor
    protected object GetValues(SerializedProperty property, string valuesName)
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

[CustomPropertyDrawer(typeof(MultiRadioTrackSelectorAttribute))]
public class MultiRadioTrackSelectorDrawer : RadioTrackSelectorDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int mask = property.intValue;
        string dataName = ((MultiRadioTrackSelectorAttribute)attribute).DataName;

        object data = GetValues(property, dataName);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        if (data != null)
            mask = EditorGUILayout.MaskField(mask, ((RadioData)data).TrackNames.ToArray());
        else
            EditorGUILayout.TextArea($"Assign RadioData to {dataName} to select tracks!");

        EditorGUILayout.EndHorizontal();

        property.intValue = mask;
    }
}
#endif