using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ResetNewInstancesAttribute : PropertyAttribute
{
    public ResetNewInstancesAttribute() { }
}

[CustomPropertyDrawer(typeof(ResetNewInstancesAttribute))]
public class ResetNewInstancesDrawer : PropertyDrawer
{
    private int collectionSize;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (GUI.changed)
        {
            if (property.arraySize > collectionSize)
            {

            }
        }
    }
}
