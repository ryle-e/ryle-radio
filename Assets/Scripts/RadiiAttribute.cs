using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RadiiAttribute : PropertyAttribute
{
    public float max;

    public RadiiAttribute(float _max = -1)
    {
        max = _max;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RadiiAttribute))]
public class RadiiDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Vector2 o = property.vector2Value;

        property.vector2Value = EditorGUILayout.Vector2Field(label, property.vector2Value);

        Vector3 worldPos = (property.serializedObject.targetObject as MonoBehaviour).transform.position;

        Debug.Log("is this working");

        o.x = Handles.RadiusHandle(Quaternion.identity, worldPos, o.x);
        o.y = Handles.RadiusHandle(Quaternion.identity, worldPos, o.y);

        property.vector2Value = o;
        //EditorAction. += () => OnSceneGUI(property);
    }
}
#endif