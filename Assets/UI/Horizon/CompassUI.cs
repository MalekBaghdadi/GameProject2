using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Horizon
{
    /// <summary>
    /// Handles all compass rendering logic, including object pooling for icons,
    /// edge clamping for off-screen markers, and real-time placement.
    /// </summary>
    [DefaultExecutionOrder(100)] // Ensures it runs after the MarkerManager has initialized.
    public class CompassUI : MonoBehaviour
    {
        // A helper class to hold cached components for each UI icon, preventing GetComponent calls in Update.
        private class CompassMarkerUI
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }
            public Image IconImage { get; }
            public CanvasGroup CanvasGroup { get; }

            public CompassMarkerUI(GameObject instance)
            {
                GameObject = instance;
                RectTransform = instance.GetComponent<RectTransform>();
                IconImage = instance.GetComponent<Image>();
                CanvasGroup = instance.GetComponent<CanvasGroup>() ?? instance.AddComponent<CanvasGroup>();
            }
        }

        [Header("References")]
        [Tooltip("The player's camera or transform used to determine facing direction.")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("The UI RawImage representing the compass background. The material's texture will be scrolled.")]
        [SerializeField] private RawImage compassBackground;
        [Tooltip("The parent RectTransform under which marker icons will be instantiated.")]
        [SerializeField] private RectTransform markerContainer;
        [Tooltip("The prefab for the marker icon. Must have an Image component.")]
        [SerializeField] private GameObject markerIconPrefab;

        [Header("Configuration")]
        [Tooltip("The horizontal field of view in degrees that the compass displays.")]
        [SerializeField, Range(60f, 360f)] private float compassFOV = 120f;

        private readonly Dictionary<Marker, CompassMarkerUI> activeIcons = new Dictionary<Marker, CompassMarkerUI>();
        private readonly Queue<CompassMarkerUI> iconPool = new Queue<CompassMarkerUI>();

        private void OnEnable()
        {
            MarkerManager.OnMarkerRegistered += AddMarker;
            MarkerManager.OnMarkerUnregistered += RemoveMarker;

            // Immediately populate with any markers that were registered before this UI was enabled.
            if (MarkerManager.InstanceExists)
            {
                foreach (var marker in MarkerManager.ActiveMarkers)
                {
                    AddMarker(marker);
                }
            }
        }

        private void OnDisable()
        {
            MarkerManager.OnMarkerRegistered -= AddMarker;
            MarkerManager.OnMarkerUnregistered -= RemoveMarker;

            // Clean up all icons and clear the collections.
            foreach (var iconUI in activeIcons.Values) Destroy(iconUI.GameObject);
            activeIcons.Clear();
            foreach (var pooledIcon in iconPool) Destroy(pooledIcon.GameObject);
            iconPool.Clear();
        }

        private void Update()
        {
            if (playerTransform == null || compassBackground == null) return;

            // Scroll the compass background texture.
            compassBackground.uvRect = new Rect(playerTransform.eulerAngles.y / 360f, 0f, 1f, 1f);

            // Update the position and appearance of each active marker icon.
            foreach (var pair in activeIcons)
            {
                UpdateMarkerIcon(pair.Key, pair.Value);
            }
        }

        private void UpdateMarkerIcon(Marker marker, CompassMarkerUI iconUI)
        {
            if (marker == null || !marker.gameObject.activeInHierarchy)
            {
                iconUI.GameObject.SetActive(false);
                return;
            }

            // Check distance visibility
            Vector3 toMarker = marker.WorldPosition - playerTransform.position;
            if (!marker.CurrentMarkerData.alwaysShowOnCompass && marker.CurrentMarkerData.showWithinDistance > 0)
            {
                if (toMarker.sqrMagnitude > marker.CurrentMarkerData.showWithinDistance * marker.CurrentMarkerData.showWithinDistance)
                {
                    iconUI.GameObject.SetActive(false);
                    return;
                }
            }

            iconUI.GameObject.SetActive(true);

            // Calculate the angle between the player's forward direction and the marker.
            float angle = Vector3.SignedAngle(playerTransform.forward, toMarker.normalized, Vector3.up);

            float halfFOV = compassFOV / 2f;
            float compassWidth = markerContainer.rect.width;

            // If the marker is within the compass FOV, place it normally.
            if (Mathf.Abs(angle) <= halfFOV)
            {
                float xPos = (angle / compassFOV) * compassWidth;
                iconUI.RectTransform.anchoredPosition = new Vector2(xPos, 0);
                iconUI.CanvasGroup.alpha = 1f;
            }
            else // Otherwise, clamp it to the edge of the compass.
            {
                float clampedXPos = Mathf.Clamp(angle, -halfFOV, halfFOV) / compassFOV * compassWidth;
                iconUI.RectTransform.anchoredPosition = new Vector2(clampedXPos, 0);
                iconUI.CanvasGroup.alpha = 0.5f; // Fade out edge icons slightly.
            }

            // Handle pulsing for objective markers.
            if (marker.CurrentMarkerData.pulseWhenObjective)
            {
                float pulse = 0.75f + Mathf.Sin(Time.time * 5f) * 0.25f; // Simple sine wave pulse from 0.75 to 1.0
                iconUI.RectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                iconUI.RectTransform.localScale = Vector3.one;
            }
        }

        private void AddMarker(Marker marker)
        {
            if (activeIcons.ContainsKey(marker)) return;

            CompassMarkerUI newIconUI;
            if (iconPool.Count > 0)
            {
                newIconUI = iconPool.Dequeue();
            }
            else
            {
                GameObject iconInstance = Instantiate(markerIconPrefab, markerContainer);
                newIconUI = new CompassMarkerUI(iconInstance);
            }

            newIconUI.GameObject.name = $"Marker_{marker.CurrentMarkerData.markerName}";
            newIconUI.IconImage.sprite = marker.CurrentMarkerData.markerIcon;
            newIconUI.IconImage.color = marker.CurrentMarkerData.tintColor;
            newIconUI.GameObject.SetActive(true);

            activeIcons.Add(marker, newIconUI);
        }

        private void RemoveMarker(Marker marker)
        {
            if (!activeIcons.TryGetValue(marker, out CompassMarkerUI iconUI)) return;

            iconUI.GameObject.SetActive(false);
            iconPool.Enqueue(iconUI);
            activeIcons.Remove(marker);
        }
    }
}