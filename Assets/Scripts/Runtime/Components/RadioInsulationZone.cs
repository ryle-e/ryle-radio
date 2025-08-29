using NaughtyAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

public class RadioInsulationZone : MonoBehaviour
{
    public static Action InitInsulators { get; private set; }

    [SerializeField] private RadioData data;

    [Space(12)]
    public Bounds outerBox = new(Vector3.zero, Vector3.one);
    public Bounds innerBox = new(Vector3.zero, Vector3.one * 0.8f);

    [Space(12)]
    [SerializeField, MinMaxSlider(0, 1)] 
    private Vector2 insulation = new(0, 0.5f);

    [SerializeField, CurveRange(0, 0, 1, 1)]
    private AnimationCurve insulationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Space(12)]
    [SerializeField, Multiselect("Tracks")]
    private int affectedTracks;
    private int lastAffectedTracks;

    public Bounds OuterBoxAdjusted { get; private set; }
    public Bounds InnerBoxAdjusted { get; private set; }

    private List<string> Tracks => data.TrackNames;

    public RadioData Data => data;


    private void Awake()
    {
        InitInsulators += AssignToTrack;
    }

    private void OnDestroy()
    {
        InitInsulators -= AssignToTrack;
    }

    private void Update()
    {
        OuterBoxAdjusted = new(outerBox.center + transform.position, outerBox.size);
        InnerBoxAdjusted = new(innerBox.center + transform.position, innerBox.size);
    }

    public void AssignToTrack()
    {
        if (lastAffectedTracks != affectedTracks)
        {
            int removedTrack = lastAffectedTracks ^ affectedTracks;
            int[] index = MultiselectAttribute.To<int>(removedTrack, MultiselectAttribute.ZeroTo31);
            //Debug.Log("removing from track " + data.TrackNames[index[0]]);
                 
            data.TrackWrappers[index[0]].insulators.Remove(this);
        }

        int[] affectedIndexes = MultiselectAttribute.To<int>(affectedTracks, MultiselectAttribute.ZeroTo31);
        lastAffectedTracks = 0;

        for (int i = 0; i < affectedIndexes.Length; i++)
        {
            RadioTrackWrapper track = data.TrackWrappers[i];
            //Debug.Log("adding to track " + track.name);

            track.insulators.Add(this);
            lastAffectedTracks |= affectedIndexes[i];
        }
    }

    public float GetPower(Vector3 _position)
    {
        float t = 0;

        if (InnerBoxAdjusted.Contains(_position))
            t = 1;

        else if (!OuterBoxAdjusted.Contains(_position))
            t = 0;

        else
        {
            Vector3 dir = (_position - OuterBoxAdjusted.center).normalized;
            Vector3 scaledDir = new Vector3(dir.x * OuterBoxAdjusted.size.x, dir.y * OuterBoxAdjusted.size.y, dir.z * OuterBoxAdjusted.size.z);

            Ray ray = new Ray(OuterBoxAdjusted.center, scaledDir);

            OuterBoxAdjusted.IntersectRay(ray, out float distance);
            InnerBoxAdjusted.IntersectRay(ray, out float distance2);

            Vector3 closestOnOuter = OuterBoxAdjusted.center + ray.direction.normalized * -distance;
            Vector3 closestOnInner = OuterBoxAdjusted.center + ray.direction.normalized * -distance2;

            float outerInnerDistance = Vector3.Distance(closestOnOuter, closestOnInner);
            float d = Vector3.Distance(_position, closestOnInner);

            t = 1 - d / outerInnerDistance;
        }

        float adjustedT = insulationCurve.Evaluate(t);
        return Mathf.Lerp(insulation.x, insulation.y, adjustedT);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(RadioInsulationZone))]
public class RadioInsulationZoneEditor : Editor
{
    private BoxBoundsHandle outerBoundsHandle = new();
    private BoxBoundsHandle innerBoundsHandle = new();

    private RadioInsulationZone zone = null;

    private void OnEnable()
    {
        if (zone == null)
                zone = (RadioInsulationZone)target;

        outerBoundsHandle.SetColor(zone.Data.GizmoColor);
        innerBoundsHandle.SetColor(zone.Data.GizmoColorSecondary);
    }

    private void OnSceneGUI()
    {
        if (zone == null)
            zone = (RadioInsulationZone)target;

        Bounds outerBounds = zone.outerBox;
        Bounds innerBounds = zone.innerBox;

        outerBoundsHandle.center = outerBounds.center + zone.transform.position;
        outerBoundsHandle.size = outerBounds.size;

        innerBoundsHandle.center = innerBounds.center + zone.transform.position;
        innerBoundsHandle.size = innerBounds.size;

        EditorGUI.BeginChangeCheck();
        outerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, "Change Outer Bounds");
            zone.outerBox = new (outerBoundsHandle.center - zone.transform.position, outerBoundsHandle.size);
        }

        EditorGUI.BeginChangeCheck();
        innerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, "Change Inner Bounds");
            zone.innerBox = new (innerBoundsHandle.center - zone.transform.position, innerBoundsHandle.size);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif