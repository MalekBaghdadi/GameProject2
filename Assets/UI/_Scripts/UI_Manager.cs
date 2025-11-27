using UnityEngine;
using System;

namespace Game.UI
{
    public class UIManager : MonoBehaviour
    {
        // Single point to open/close menus and relay events to HUD.
        public static UIManager Instance { get; private set; }

        [Header("Containers")]
        public GameObject hudContainer;      // assign HUD_Container prefab instance
        public GameObject menusContainer;    // assign Menus_Container prefab instance

        public event Action OnPauseToggled;  // subscribe by Pause menu, player, etc.

        private bool isPaused = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject); // optional: keep UI manager alive across scenes
        }

        void Start()
        {
            // sanity checks
            if (hudContainer == null) Debug.LogWarning("HUD container not assigned in UIManager.");
            if (menusContainer == null) Debug.LogWarning("Menus container not assigned in UIManager.");
        }

        // Public API
        public void TogglePause()
        {
            isPaused = !isPaused;
            SetMenuActive("Pause", isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
            OnPauseToggled?.Invoke();
        }

        public void SetMenuActive(string menuName, bool active)
        {
            var target = menusContainer.transform.Find(menuName);
            if (target != null) target.gameObject.SetActive(active);
            else Debug.LogWarning($"Menu '{menuName}' not found under Menus_Container.");
        }

        public void ShowHUDWidget(string widgetPath, bool active)
        {
            // widgetPath example: "HUD_Container/HealthBar"
            var t = hudContainer.transform.Find(widgetPath.Replace("HUD_Container/", ""));
            if (t != null) t.gameObject.SetActive(active);
        }
    }
}