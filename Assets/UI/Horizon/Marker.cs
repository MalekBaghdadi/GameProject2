using UnityEngine;

namespace Horizon
{
    /// <summary>
    /// Attach this component to any GameObject to make it trackable on the compass.
    /// It automatically handles its registration with the MarkerManager.
    /// A Collider component is required on this object or its children for raycast-based interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class Marker : MonoBehaviour, IHighlightable
    {
        [Tooltip("The MarkerData asset that defines this marker's default appearance and behavior.")]
        [SerializeField] private MarkerData markerData;

        /// <summary>The data asset currently defining this marker.</summary>
        public MarkerData CurrentMarkerData => markerData;

        /// <summary>Is the marker currently registered and active in the MarkerManager?</summary>
        public bool IsRegistered { get; private set; }

        /// <summary>The world position of this marker's transform.</summary>
        public Vector3 WorldPosition => transform.position;

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        /// <summary>
        /// Dynamically changes the MarkerData for this marker at runtime.
        /// </summary>
        /// <param name="newData">The new MarkerData to apply.</param>
        public void SetMarkerData(MarkerData newData)
        {
            if (IsRegistered)
            {
                TryUnregister();
            }

            markerData = newData;
            TryRegister();
        }

        private void TryRegister()
        {
            if (markerData == null)
            {
                return;
            }

            // Note the fix here: We call the static method directly on the class name.
            if (!IsRegistered && MarkerManager.InstanceExists)
            {
                MarkerManager.RegisterMarker(this); // CORRECTED
                IsRegistered = true;
            }
        }

        private void TryUnregister()
        {
            // Note the fix here: We call the static method directly on the class name.
            if (IsRegistered && MarkerManager.InstanceExists)
            {
                MarkerManager.UnregisterMarker(this); // CORRECTED
                IsRegistered = false;
            }
        }

        #region IHighlightable Implementation

        public void SetHighlight(bool isHighlighted)
        {
            // TODO: Implement your highlight logic here.
        }

        #endregion
    }
}