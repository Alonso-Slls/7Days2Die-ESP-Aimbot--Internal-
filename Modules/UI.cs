using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Modernized UI system using Unity's IMGUI with improved styling and layout.
    /// Features responsive design, better visual hierarchy, and enhanced user experience.
    /// </summary>
    class UI
    {
        //******* ESP Toggle Variables ********
        public static bool t_ESP = false;
        public static bool t_EnemyESP = false;
        public static bool t_EnemyBones = false;
        public static bool t_ItemESP = false;
        public static bool t_NPCESP = false;
        public static bool t_PlayerESP = false;
        public static bool t_AnimalESP = false;
        public static bool t_ESPLines = false;
        public static bool t_ESPBoxes = true;

        //******* Aimbot Toggle Variables ********
        public static bool t_AIM = false;
        public static bool t_AAIM = false;

        public static bool t_TEnemies = false;
        public static bool t_TAnimals = false;
        public static bool t_TPlayers = false;
        public static bool t_TFOV = false;
        
        // Aim tuning
        public static int t_AimFOV = 150;
        public static float t_AimSmooth = 5f;
        public static bool t_DebugOverlay = true;

        // FOV-Aware ESP Settings
        public static bool t_FOVAwareESP = true;
        public static float t_FOVThreshold = 120f;
        
        // Legacy compatibility
        public static string dbg = "debug";

        // UI Style and Layout Configuration
        private static GUIStyle windowStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle labelStyle;
        private static GUIStyle toggleStyle;
        private static GUIStyle sliderStyle;
        private static GUIStyle boxStyle;
        
        // Layout Constants
        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 400f;
        private const float PADDING = 10f;
        private const float BUTTON_HEIGHT = 25f;
        private const float SPACING = 5f;
        
        // Colors
        private static Color primaryColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        private static Color accentColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
        private static Color textColor = Color.white;
        private static Color disabledColor = Color.gray;
        
        // Animation and Interpolation
        private static float menuAlpha = 0f;
        private static bool menuVisible = false;
        private const float FADE_SPEED = 5f;

        /// <summary>
        /// Initialize UI styles and colors for modern appearance.
        /// </summary>
        private static void InitializeStyles()
        {
            if (windowStyle != null) return; // Already initialized
            
            // Window Style
            windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = CreateColorTexture(primaryColor) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            
            // Button Style
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = textColor },
                hover = { textColor = Color.white },
                active = { textColor = accentColor },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            // Label Style
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = textColor },
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft
            };
            
            // Toggle Style
            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = textColor },
                fontSize = 10,
                fontStyle = FontStyle.Normal
            };
            
            // Box Style
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateColorTexture(new Color(0.05f, 0.05f, 0.05f, 0.8f)) }
            };
        }
        
        /// <summary>
        /// Create a simple colored texture for UI elements.
        /// </summary>
        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Main menu drawing method with modern IMGUI implementation.
        /// </summary>
        public static void DrawMenu()
        {
            if (!Hacks.Menu || !Hacks.isLoaded) return;
            
            InitializeStyles();
            
            // Handle menu fade animation
            if (Hacks.Menu && !menuVisible)
            {
                menuVisible = true;
            }
            else if (!Hacks.Menu && menuVisible)
            {
                menuVisible = false;
            }
            
            // Smooth fade in/out
            if (menuVisible && menuAlpha < 1f)
                menuAlpha = Mathf.Min(1f, menuAlpha + Time.deltaTime * FADE_SPEED);
            else if (!menuVisible && menuAlpha > 0f)
                menuAlpha = Mathf.Max(0f, menuAlpha - Time.deltaTime * FADE_SPEED);
            
            if (menuAlpha <= 0f) return;
            
            GUI.color = new Color(1f, 1f, 1f, menuAlpha);
            
            // Main Menu Window
            GUILayout.BeginArea(new Rect(PADDING, PADDING, WINDOW_WIDTH, WINDOW_HEIGHT), windowStyle);
            DrawMainMenu();
            GUILayout.EndArea();
            
            // ESP Submenu
            if (t_ESP)
            {
                GUILayout.BeginArea(new Rect(WINDOW_WIDTH + PADDING * 2, PADDING, 250f, 300f), windowStyle);
                DrawESPMenu();
                GUILayout.EndArea();
            }
            
            // Aimbot Submenu
            if (t_AIM)
            {
                GUILayout.BeginArea(new Rect(WINDOW_WIDTH + PADDING * 2, PADDING, 250f, 350f), windowStyle);
                DrawAimbotMenu();
                GUILayout.EndArea();
            }
            
            // Debug Overlay
            if (t_DebugOverlay)
            {
                DrawDebugOverlay();
            }
            
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// Draw the main menu with toggle options.
        /// </summary>
        private static void DrawMainMenu()
        {
            GUILayout.Label("7Days2Die - Gh0st Mod Menu", labelStyle);
            GUILayout.Space(SPACING);
            
            // ESP Toggle
            GUI.color = t_ESP ? accentColor : Color.white;
            if (GUILayout.Button("ESP System [ON/OFF]", buttonStyle, GUILayout.Height(BUTTON_HEIGHT)))
            {
                t_ESP = !t_ESP;
                if (t_ESP) t_AIM = false; // Exclusive menus
            }
            
            // Aimbot Toggle
            GUI.color = t_AIM ? accentColor : Color.white;
            if (GUILayout.Button("Aimbot System [ON/OFF]", buttonStyle, GUILayout.Height(BUTTON_HEIGHT)))
            {
                t_AIM = !t_AIM;
                if (t_AIM) t_ESP = false; // Exclusive menus
            }
            
            GUI.color = Color.white;
            GUILayout.Space(SPACING * 2);
            
            // Quick Status
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Status:", labelStyle);
            GUILayout.Label($"ESP: {(t_ESP ? "Active" : "Inactive")}", labelStyle);
            GUILayout.Label($"Aimbot: {(t_AIM ? "Active" : "Inactive")}", labelStyle);
            GUILayout.Label($"FOV-Aware: {(t_FOVAwareESP ? "Enabled" : "Disabled")}", labelStyle);
            GUILayout.EndVertical();
            
            GUILayout.Space(SPACING);
            
            // Debug info
            if (t_DebugOverlay)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label("Debug Info:", labelStyle);
                GUILayout.Label($"FPS: {(int)(1f / Time.deltaTime)}", labelStyle);
                GUILayout.Label($"Entities: {GetTotalEntityCount()}", labelStyle);
                GUILayout.Label(ESPPool.GetStats(), labelStyle);
                GUILayout.Label(SpatialGrid.GetStats(), labelStyle);
                GUILayout.EndVertical();
            }
        }
        
        /// <summary>
        /// Draw the ESP configuration menu.
        /// </summary>
        private static void DrawESPMenu()
        {
            GUILayout.Label("ESP Configuration", labelStyle);
            GUILayout.Space(SPACING);
            
            // ESP Options - Two columns
            GUILayout.BeginHorizontal();
            
            // Left Column
            GUILayout.BeginVertical();
            t_EnemyESP = GUILayout.Toggle(t_EnemyESP, "Enemy ESP", toggleStyle);
            t_ItemESP = GUILayout.Toggle(t_ItemESP, "Item ESP", toggleStyle);
            t_NPCESP = GUILayout.Toggle(t_NPCESP, "NPC ESP", toggleStyle);
            t_PlayerESP = GUILayout.Toggle(t_PlayerESP, "Player ESP", toggleStyle);
            t_AnimalESP = GUILayout.Toggle(t_AnimalESP, "Animal ESP", toggleStyle);
            GUILayout.EndVertical();
            
            GUILayout.Space(SPACING);
            
            // Right Column
            GUILayout.BeginVertical();
            t_EnemyBones = GUILayout.Toggle(t_EnemyBones, "Enemy Bones", toggleStyle);
            t_ESPLines = GUILayout.Toggle(t_ESPLines, "Draw Lines", toggleStyle);
            t_ESPBoxes = GUILayout.Toggle(t_ESPBoxes, "Draw Boxes", toggleStyle);
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(SPACING * 2);
            
            // FOV-Aware Settings
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("FOV-Aware Settings:", labelStyle);
            t_FOVAwareESP = GUILayout.Toggle(t_FOVAwareESP, "Enable FOV-Aware ESP", toggleStyle);
            
            if (t_FOVAwareESP)
            {
                GUILayout.Label($"FOV Threshold: {t_FOVThreshold:F0}°", labelStyle);
                t_FOVThreshold = GUILayout.HorizontalSlider(t_FOVThreshold, 60f, 180f);
            }
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the Aimbot configuration menu.
        /// </summary>
        private static void DrawAimbotMenu()
        {
            GUILayout.Label("Aimbot Configuration", labelStyle);
            GUILayout.Space(SPACING);
            
            // Aimbot Controls
            t_AAIM = GUILayout.Toggle(t_AAIM, "Activate Aimbot", toggleStyle);
            GUILayout.Space(SPACING);
            
            // Target Selection
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Target Selection:", labelStyle);
            t_TEnemies = GUILayout.Toggle(t_TEnemies, "Target Enemies", toggleStyle);
            t_TAnimals = GUILayout.Toggle(t_TAnimals, "Target Animals", toggleStyle);
            t_TPlayers = GUILayout.Toggle(t_TPlayers, "Target Players", toggleStyle);
            t_TFOV = GUILayout.Toggle(t_TFOV, "Show FOV Circle", toggleStyle);
            GUILayout.EndVertical();
            
            GUILayout.Space(SPACING);
            
            // Aim Settings
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Aim Settings:", labelStyle);
            
            GUILayout.Label($"Aim FOV: {t_AimFOV}", labelStyle);
            t_AimFOV = (int)GUILayout.HorizontalSlider(t_AimFOV, 50f, 400f);
            
            GUILayout.Label($"Aim Smooth: {t_AimSmooth:F1}", labelStyle);
            t_AimSmooth = GUILayout.HorizontalSlider(t_AimSmooth, 1f, 20f);
            GUILayout.EndVertical();
            
            GUILayout.Space(SPACING);
            
            // Debug Options
            t_DebugOverlay = GUILayout.Toggle(t_DebugOverlay, "Debug Overlay", toggleStyle);
        }
        
        /// <summary>
        /// Draw debug overlay information.
        /// </summary>
        private static void DrawDebugOverlay()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 200f, PADDING, 190f, 100f), boxStyle);
            GUILayout.Label("Debug Overlay", labelStyle);
            GUILayout.Label($"FPS: {(int)(1f / Time.deltaTime)}", labelStyle);
            GUILayout.Label($"Menu Alpha: {menuAlpha:F2}", labelStyle);
            GUILayout.Label($"Time: {DateTime.Now:HH:mm:ss}", labelStyle);
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Get total entity count for debug display.
        /// </summary>
        private static int GetTotalEntityCount()
        {
            int count = 0;
            try
            {
                if (Hacks.eEnemy != null) count += Hacks.eEnemy.Count;
                if (Hacks.eAnimal != null) count += Hacks.eAnimal.Count;
                if (Hacks.ePlayers != null) count += Hacks.ePlayers.Count;
            }
            catch
            {
                count = -1; // Error indicator
            }
            return count;
        }
        
        /// <summary>
        /// Cleanup method to release resources.
        /// </summary>
        public static void Cleanup()
        {
            if (windowStyle?.normal?.background != null)
                UnityEngine.Object.Destroy(windowStyle.normal.background);
            if (boxStyle?.normal?.background != null)
                UnityEngine.Object.Destroy(boxStyle.normal.background);
        }
    }
}
