using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Horizon
{
    /// <summary>
    /// A high-performance, event-driven singleton that tracks all active Markers in the scene.
    /// It provides a static API for registration and a read-only collection for UI systems.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensures this manager is ready before other scripts run their OnEnable.
    public class MarkerManager : MonoBehaviour
    {
        private static MarkerManager _instance;

        /// <summary>Singleton instance of the MarkerManager.</summary>
        public static MarkerManager Instance
        {
            get
            {
                // The getter provides a helpful error if the manager is accessed without being in the scene.
                if (_instance == null)
                    Debug.LogError("MarkerManager.Instance was accessed, but no instance exists in the scene. Please add one.");
                return _instance;
            }
        }

        /// <summary>Returns true if a MarkerManager instance currently exists, preventing errors on shutdown.</summary>
        public static bool InstanceExists => _instance != null;

        // --- Events ---
        /// <summary>Fired when a new marker is registered with the manager.</summary>
        public static event Action<Marker> OnMarkerRegistered;
        /// <summary>Fired when a marker is unregistered from the manager.</summary>
        public static event Action<Marker> OnMarkerUnregistered;

        /// <summary>Provides safe, read-only access to the collection of active markers.</summary>
        public static IEnumerable<Marker> ActiveMarkers => _instance?._activeMarkers ?? Enumerable.Empty<Marker>();

        [Tooltip("Enable to print marker registration and unregistration events to the Console.")]
        [SerializeField] private bool debugLogging = false;

        /// <summary>Using a HashSet for an O(1) performance on Add, Remove, and Contains operations.</summary>
        private readonly HashSet<Marker> _activeMarkers = new HashSet<Marker>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MarkerManager] Duplicate instance detected. Destroying the new one.", gameObject);
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                // Clear the instance reference when this object is destroyed.
                _instance = null;
            }
        }

        /// <summary>
        /// Registers a marker to be tracked. Called automatically by the Marker component's OnEnable.
        /// </summary>
        public static void RegisterMarker(Marker marker)
        {
            if (!InstanceExists || marker == null) return;

            // HashSet.Add returns true if the item was successfully added (i.e., not already in the set).
            if (_instance._activeMarkers.Add(marker))
            {
                OnMarkerRegistered?.Invoke(marker);
                if (_instance.debugLogging) Debug.Log($"[Horizon] Registered marker: {marker.name}", marker);
            }
        }

        /// <summary>
        /// Unregisters a marker. Called automatically by the Marker component's OnDisable.
        /// </summary>
        public static void UnregisterMarker(Marker marker)
        {
            if (!InstanceExists || marker == null) return;

            // HashSet.Remove returns true if the item was found and successfully removed.
            if (_instance._activeMarkers.Remove(marker))
            {
                OnMarkerUnregistered?.Invoke(marker);
                if (_instance.debugLogging) Debug.Log($"[Horizon] Unregistered marker: {marker.name}", marker);
            }
        }
    }
}