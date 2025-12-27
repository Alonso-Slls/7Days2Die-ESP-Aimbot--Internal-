using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Centralized configuration system with JSON persistence.
    /// Replaces scattered UI settings and magic numbers with structured configuration.
    /// </summary>
    public static class Config
    {
        private static string ConfigPath => Path.Combine(Application.dataPath, "..", "mod_config.json");
        private static ModSettings _settings;
        
        // Performance constants
        public const float ENTITY_SCAN_INTERVAL = 5f;
        public const float ENEMY_SCAN_INTERVAL = 2.0f;
        public const float ANIMAL_SCAN_INTERVAL = 2.0f;
        public const float LOCAL_SEARCH_INTERVAL = 5f;
        public const int PRUNE_BUDGET_PER_TICK = 2;
        public const int REFRESH_CHUNK_SIZE = 20;
        
        // ESP constants
        public const float MAX_ESP_DISTANCE = 100f;
        public const int DEFAULT_AIM_FOV = 150;
        public const float DEFAULT_AIM_SMOOTH = 5f;
        public const float DEFAULT_FOV_THRESHOLD = 120f;
        
        // UI constants
        public const float WINDOW_WIDTH = 300f;
        public const float WINDOW_HEIGHT = 400f;
        public const float PADDING = 10f;
        public const float BUTTON_HEIGHT = 25f;
        public const float SPACING = 5f;
        
        // Rendering constants
        public const float FADE_SPEED = 5f;
        public const float CACHE_UPDATE_INTERVAL = 0.1f;
        public const float THICKNESS_TOLERANCE = 0.01f;
        public const float MIN_SCREEN_POSITION = 0.01f;
        public const int SCREEN_EDGE_MARGIN = 5;
        public const float DEFAULT_CELL_SIZE = 50f;
        public const float DEFAULT_MAX_DISTANCE = 200f;
        public const float DEFAULT_VISIBILITY_DISTANCE = 100f;
        public const float COROUTINE_UPDATE_INTERVAL = 0.25f;
        public const float FIRE_COOLDOWN = 0.15f;
        public const float WEAPON_CHECK_INTERVAL = 0.25f;
        
        // Color constants
        public static readonly Color PrimaryColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        public static readonly Color AccentColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
        public static readonly Color BoxBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        public static readonly Color TextColor = Color.white;
        public static readonly Color DisabledColor = Color.gray;
        
        // Font constants
        public const int WINDOW_FONT_SIZE = 12;
        public const int BUTTON_FONT_SIZE = 11;
        public const int LABEL_FONT_SIZE = 11;
        public const int TOGGLE_FONT_SIZE = 10;
        
        // Slider ranges
        public const int MIN_AIM_FOV = 50;
        public const int MAX_AIM_FOV = 400;
        public const float MIN_AIM_SMOOTH = 1f;
        public const float MAX_AIM_SMOOTH = 20f;
        public const float MIN_FOV_THRESHOLD = 60f;
        public const float MAX_FOV_THRESHOLD = 180f;
        
        // Debug overlay constants
        public const float DEBUG_OVERLAY_WIDTH = 200f;
        public const float DEBUG_OVERLAY_HEIGHT = 100f;
        public const float SUBMENU_WIDTH = 250f;
        public const float ESP_SUBMENU_HEIGHT = 300f;
        public const float AIMBOT_SUBMENU_HEIGHT = 350f;
        
        // Texture constants
        public const int TEXTURE_SIZE = 1;
        
        /// <summary>
        /// Initialize configuration system and load settings.
        /// </summary>
        public static void Initialize()
        {
            LoadSettings();
        }
        
        /// <summary>
        /// Get current settings instance.
        /// </summary>
        public static ModSettings Settings => _settings ?? (_settings = new ModSettings());
        
        /// <summary>
        /// Load settings from JSON file or create defaults.
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    _settings = JsonUtility.FromJson<ModSettings>(json);
                    Debug.Log($"[Config] Loaded settings from {ConfigPath}");
                }
                else
                {
                    _settings = new ModSettings();
                    SaveSettings();
                    Debug.Log($"[Config] Created default settings at {ConfigPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Config] Failed to load settings: {ex.Message}");
                _settings = new ModSettings();
            }
        }
        
        /// <summary>
        /// Save current settings to JSON file.
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(_settings, true);
                File.WriteAllText(ConfigPath, json);
                Debug.Log($"[Config] Saved settings to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Config] Failed to save settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Reset settings to defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            _settings = new ModSettings();
            SaveSettings();
        }
        
        /// <summary>
        /// Validate settings and fix invalid values.
        /// </summary>
        public static void ValidateSettings()
        {
            if (_settings.AimFOV < 50 || _settings.AimFOV > 400)
                _settings.AimFOV = DEFAULT_AIM_FOV;
                
            if (_settings.AimSmooth < 1f || _settings.AimSmooth > 20f)
                _settings.AimSmooth = DEFAULT_AIM_SMOOTH;
                
            if (_settings.FOVThreshold < 60f || _settings.FOVThreshold > 180f)
                _settings.FOVThreshold = DEFAULT_FOV_THRESHOLD;
        }
    }
    
    /// <summary>
    /// Serializable settings structure for the mod.
    /// </summary>
    [Serializable]
    public class ModSettings
    {
        // ESP Settings
        public bool ESPEnabled = false;
        public bool EnemyESP = false;
        public bool EnemyBones = false;
        public bool ItemESP = false;
        public bool NPCESP = false;
        public bool PlayerESP = false;
        public bool AnimalESP = false;
        public bool ESPLines = false;
        public bool ESPBoxes = true;
        public bool FOVAwareESP = true;
        public float FOVThreshold = Config.DEFAULT_FOV_THRESHOLD;
        
        // Aimbot Settings
        public bool AimbotEnabled = false;
        public bool AutoAim = false;
        public bool TargetEnemies = false;
        public bool TargetAnimals = false;
        public bool TargetPlayers = false;
        public bool ShowFOVCircle = false;
        public int AimFOV = Config.DEFAULT_AIM_FOV;
        public float AimSmooth = Config.DEFAULT_AIM_SMOOTH;
        
        // UI Settings
        public bool MenuVisible = true;
        public bool DebugOverlay = true;
        public bool ShowPerformanceStats = true;
        
        // Performance Settings
        public bool EnableObjectPooling = true;
        public bool EnableSpatialGrid = true;
        public float SpatialGridCellSize = 50f;
        
        // Hotkey Settings
        public KeyCode MenuToggleKey = KeyCode.Insert;
        public KeyCode UnloadKey = KeyCode.End;
        public KeyCode QuickESPKey = KeyCode.Keypad1;
        
        // Color Settings
        public Color EnemyColor = new Color(1f, 0f, 0f, 1f); // Red
        public Color PlayerColor = new Color(0f, 1f, 0f, 1f); // Green
        public Color AnimalColor = new Color(1f, 1f, 0f, 1f); // Yellow
        public Color ItemColor = new Color(0f, 1f, 1f, 1f); // Cyan
        public Color NPCColor = new Color(1f, 0f, 1f, 1f); // Magenta
    }
}
