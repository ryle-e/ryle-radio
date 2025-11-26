using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Components;

#if UNITY_EDITOR
    using UnityEditor;

    /// <summary>
    /// Custom inspector for a \ref RadioBroadcaster
    /// <br><br>This is mainly so that we can use `Handles` (draggable gizmos).
    /// 
    /// <b>See: </b> \ref RadioBroadcaster
    /// </summary>
    [CustomEditor(typeof(RadioBroadcaster))]
    public class RadioBroadcasterEditor : Editor
    {
        /// <summary>
        /// The broadcaster this is linked to
        /// </summary>
        private RadioBroadcaster broadcaster;

        /// The colour of the gizmo for the x/inner value of \ref RadioBroadcaster.broadcastRadius
        private Color innerColor;
        /// The colour of the gizmo for the y/outer value of \ref RadioBroadcaster.broadcastRadius
        private Color outerColor;


        /// <summary>
        /// Initializes the broadcaster's editor
        /// </summary>
        private void OnEnable()
        {
            // if the broadcaster's not set, set it
            if (broadcaster == null)
                broadcaster = (RadioBroadcaster) target;

            // if the broadcaster has a RadioData assigned, get gizmo colours from it
            if (broadcaster.Data != null)
            {
                innerColor = broadcaster.Data.GizmoColorSecondary;
                outerColor = broadcaster.Data.GizmoColor;
            }
            // otherwise make them white
            else
            {
                innerColor = Color.white;
                outerColor = Color.white;
            }
        }

        /// <summary>
        /// Displays the handles for \ref RadioBroadcaster.broadcastRadius in the scene
        /// </summary>
        private void OnSceneGUI()
        {
            // get the radii
            Vector2 o = broadcaster.broadcastRadius;

            // display the inner radius
            Handles.color = innerColor;
            o.x = Mathf.Min(o.y, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.x));

            // display the outer radius
            Handles.color = outerColor;
            o.y = Mathf.Max(o.x, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.y));

            // set the radii
            broadcaster.broadcastRadius = o;
        }

        /// <summary>
        /// Draw the actual inspector as default
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
#endif

}