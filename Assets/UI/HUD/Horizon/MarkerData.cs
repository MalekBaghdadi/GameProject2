using UnityEngine;

namespace Horizon
{
    /// <summary>
    /// A ScriptableObject that defines the visual properties and behavior of a specific type of compass marker.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMarkerData", menuName = "Horizon/Marker Data", order = 0)]
    public class MarkerData : ScriptableObject, IKeyedData
    {
        [Header("Identity")]
        [Tooltip("The unique identifier for this marker type. Used for lookups and logic.")]
        [SerializeField] private string markerID;

        /// <summary>The unique ID for this marker, implementing the IKeyedData interface.</summary>
        public string ID => markerID;

        [Header("Display Properties")]
        [Tooltip("The display name for the marker, which could be shown on a map or in UI tooltips.")]
        public string markerName;

        [Tooltip("The icon that will be displayed on the compass UI for this marker.")]
        public Sprite markerIcon;

        [Tooltip("A color tint that will be applied to the marker's icon.")]
        public Color tintColor = Color.white;

        [Header("Behavior")]
        [Tooltip("If true, this marker will always appear on the compass regardless of distance.")]
        public bool alwaysShowOnCompass = false;

        [Tooltip("If 'Always Show' is false, this marker will only appear if the player is within this distance. Set to 0 to disable.")]
        [Min(0)]
        public float showWithinDistance = 100f;

        [Tooltip("A flag the UI can use to add a pulsing or highlighting effect, typically for quest objectives.")]
        public bool pulseWhenObjective = false;

        /// <summary>
        /// Called in the Unity Editor to validate data.
        /// </summary>
        private void OnValidate()
        {
            // Auto-corrects the ID to prevent common errors.
            if (string.IsNullOrWhiteSpace(markerID))
            {
                Debug.LogWarning($"MarkerData asset '{this.name}' has an empty ID. This is required.", this);
            }
            else if (markerID.Contains(" "))
            {
                markerID = markerID.Replace(" ", "");
                Debug.LogWarning($"MarkerData ID on '{this.name}' contained spaces and was auto-corrected to '{markerID}'.", this);
            }
        }
    }
}