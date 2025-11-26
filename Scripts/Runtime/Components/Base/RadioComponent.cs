using UnityEngine;

namespace RyleRadio.Components.Base
{

    /// <summary>
    /// A scene component that holds a reference to a \ref RadioData
    /// </summary>
    public abstract class RadioComponent : MonoBehaviour
    {
        /// <summary>
        /// The \ref RadioData (aka just radio) that this component is linked to
        /// </summary>
        [SerializeField] protected RadioData data;

        /// <summary>
        /// Read-only accessor for \ref data
        /// </summary>
        public RadioData Data => data;


        /// <summary>
        /// Initialises this component
        /// </summary>
        public abstract void Init();


        /// <summary>
        /// Link \ref Init() to the radio's init
        /// </summary>
        private void Awake()
        {
            data.OnInit += _ => Init();
        }

        /// <summary>
        /// Unlink \ref Init() from the radio's init
        /// </summary>
        private void OnDestroy()
        {
            data.OnInit -= _ => Init();
        }
    }

}