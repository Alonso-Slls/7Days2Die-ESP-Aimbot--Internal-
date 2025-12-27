using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Main mod manager that replaces the monolithic static Hacks class.
    /// Implements singleton pattern with proper instance-based architecture.
    /// Manages all mod systems and provides controlled global access.
    /// </summary>
    public class HacksManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static HacksManager _instance;
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// Gets the singleton instance of the HacksManager.
        /// Thread-safe with lazy initialization.
        /// </summary>
        public static HacksManager Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<HacksManager>();
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("HacksManager");
                            _instance = go.AddComponent<HacksManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return _instance;
                }
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private Camera _mainCamera;
        private Coroutine _updateCoroutine;
        private bool _isLoaded = false;
        private float _timer = 0f;
        
        // Performance tracking
        private int _w2sCount = 0;
        private Stopwatch _scanStopwatch = new Stopwatch();
        private long _lastScanMs = 0;
        
        // Entity scanning state
        private int _scanStep = 0;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets the main camera reference.
        /// </summary>
        public Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                    _mainCamera = Camera.main;
                return _mainCamera;
            }
        }
        
        /// <summary>
        /// Gets whether the game is loaded and mod is active.
        /// </summary>
        public bool IsLoaded => _isLoaded;
        
        /// <summary>
        /// Gets the current timer value for entity scanning.
        /// </summary>
        public float Timer => _timer;
        
        /// <summary>
        /// Gets the current scan step for entity processing.
        /// </summary>
        public int ScanStep => _scanStep;
        
        /// <summary>
        /// Gets the count of WorldToScreenPoint calls this frame.
        /// </summary>
        public int W2SCount => _w2sCount;
        
        /// <summary>
        /// Gets performance statistics.
        /// </summary>
        public string PerformanceStats => $"Scan: {_lastScanMs}ms, W2S: {_w2sCount}, Step: {_scanStep}";
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Initializes the mod manager and all systems.
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Unity Update method - handles per-frame logic.
        /// </summary>
        private void Update()
        {
            // Reset per-frame counters
            _w2sCount = 0;
            
            if (_isLoaded)
            {
                // Handle hotkeys
                Hotkeys.ProcessHotkeys();
                
                // Update timer
                _timer += Time.deltaTime;
                
                // Update entity trackers
                UpdateEntityTrackers();
                
                // Periodic cleanup and scanning
                HandlePeriodicUpdates();
                
                // Handle special keys
                HandleSpecialKeys();
            }
            
            CheckGameState();
        }
        
        /// <summary>
        /// Unity OnGUI method - handles rendering.
        /// </summary>
        private void OnGUI()
        {
            if (!_isLoaded)
            {
                GUI.Box(new Rect(5f, 5f, 250f, 35f), "");
                GUI.Label(new Rect(10f, 5f, 250f, 30f), "Menu will load when in a game");
                return;
            }
            
            // Draw menu
            UI.DrawMenu();
            
            // Draw debug overlay if enabled
            if (Config.Settings.DebugOverlay)
            {
                DrawDebugOverlay();
            }
            
            // Draw FOV circle if aimbot is enabled
            if (Config.Settings.AimbotEnabled && Config.Settings.ShowFOVCircle)
            {
                DrawFOVCircle();
            }
            
            // Render ESP using dedicated renderer
            ESPRenderer.Instance.RenderAllESP();
            
            // Execute batched rendering
            BatchedRenderer.RenderBatches();
        }
        
        /// <summary>
        /// Cleanup when destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the mod and all systems.
        /// </summary>
        private void Start()
        {
            try
            {
                // Initialize detailed logging first
                DetailedLogger.Initialize();
                DetailedLogger.Log(DetailedLogger.LogLevel.INFO, "HacksManager", "Starting mod initialization");
                
                // Initialize configuration
                Config.Initialize();
                DetailedLogger.LogComponentInit("Config", true);
                
                // Initialize error handler
                ErrorHandler.Initialize();
                DetailedLogger.LogComponentInit("ErrorHandler", true);
                
                // Initialize UI
                UI.Initialize();
                DetailedLogger.LogComponentInit("UI", true);
                
                // Initialize entity trackers
                InitializeEntityTrackers();
                
                // Initialize renderers
                InitializeRenderers();
                
                // Start main update coroutine
                StartCoroutine(MainUpdateCoroutine());
                
                _isLoaded = true;
                DetailedLogger.Log(DetailedLogger.LogLevel.INFO, "HacksManager", "Mod initialization completed successfully");
                
                // Log system information
                DetailedLogger.Log(DetailedLogger.LogLevel.DEBUG, "HacksManager", DetailedLogger.GetSystemInfo());
            }
            catch (System.Exception ex)
            {
                DetailedLogger.Log(DetailedLogger.LogLevel.CRITICAL, "HacksManager", $"Initialization failed: {ex.Message}", ex.StackTrace);
                ErrorHandler.LogError("HacksManager", $"Initialization failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Initializes entity trackers with appropriate intervals.
        /// </summary>
        private void InitializeEntityTrackers()
        {
            EntityTracker<EntityEnemy>.Instance.ScanInterval = Config.ENEMY_SCAN_INTERVAL;
            EntityTracker<EntityAnimal>.Instance.ScanInterval = Config.ANIMAL_SCAN_INTERVAL;
            EntityTracker<EntityPlayer>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            EntityTracker<EntityItem>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            EntityTracker<EntityNPC>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            
            DetailedLogger.Log(DetailedLogger.LogLevel.INFO, "HacksManager", "Entity trackers initialized");
        }
        
        /// <summary>
        /// Initializes rendering systems.
        /// </summary>
        private void InitializeRenderers()
        {
            // Initialize ESP renderer
            ESPRenderer.Instance.Initialize();
            DetailedLogger.LogComponentInit("ESPRenderer", true);
            
            // Initialize batched renderer
            BatchedRenderer.Initialize();
            DetailedLogger.LogComponentInit("BatchedRenderer", true);
            
            DetailedLogger.Log(DetailedLogger.LogLevel.INFO, "HacksManager", "Rendering systems initialized");
        }
        
        /// <summary>
        /// Creates the EntityManager component.
        /// </summary>
        private void CreateEntityManager()
        {
            if (EntityManager.Instance == null)
            {
                var entityManagerGO = new GameObject("EntityManager");
                entityManagerGO.AddComponent<EntityManager>();
            }
        }
        
        #endregion
        
        #region Entity Management
        
        /// <summary>
        /// Updates all entity trackers.
        /// </summary>
        private void UpdateEntityTrackers()
        {
            bool espFeaturesActive = Config.Settings.EnemyESP || Config.Settings.PlayerESP || 
                                   Config.Settings.ItemESP || Config.Settings.AnimalESP || 
                                   Config.Settings.NPCESP || Config.Settings.AimbotEnabled;
            
            if (espFeaturesActive)
            {
                EntityTracker<EntityEnemy>.Instance.Update();
                EntityTracker<EntityAnimal>.Instance.Update();
                EntityTracker<EntityPlayer>.Instance.Update();
                EntityTracker<EntityItem>.Instance.Update();
                EntityTracker<EntityNPC>.Instance.Update();
            }
        }
        
        /// <summary>
        /// Handles periodic entity cleanup and scanning.
        /// </summary>
        private void HandlePeriodicUpdates()
        {
            if (_timer >= Config.ENTITY_SCAN_INTERVAL)
            {
                _timer = 0f;
                
                // Cleanup invalid entities
                EntitySubscription.CleanupInvalidEntities();
                EntityTracker<EntityEnemy>.Instance.CleanupInvalidEntities();
                EntityTracker<EntityAnimal>.Instance.CleanupInvalidEntities();
                EntityTracker<EntityPlayer>.Instance.CleanupInvalidEntities();
                EntityTracker<EntityItem>.Instance.CleanupInvalidEntities();
                EntityTracker<EntityNPC>.Instance.CleanupInvalidEntities();
                
                // Occasional rescan for missed entities
                if (UnityEngine.Random.Range(0, 10) == 0)
                {
                    EntitySubscription.RescanAndSubscribeExistingEntities();
                }
            }
        }
        
        /// <summary>
        /// Coroutine for incremental entity updates.
        /// </summary>
        private IEnumerator UpdateObjectsCoroutine()
        {
            while (true)
            {
                _scanStopwatch.Restart();
                
                // Process entities based on current scan step
                ProcessScanStep();
                
                _scanStopwatch.Stop();
                _lastScanMs = _scanStopwatch.ElapsedMilliseconds;
                
                // Update debug info
                UI.dbg = $"scan{_scanStep}:{_lastScanMs}ms W2S:{_w2sCount}";
                
                // Move to next scan step
                _scanStep = (_scanStep + 1) % 7;
                
                yield return new WaitForSeconds(Config.COROUTINE_UPDATE_INTERVAL);
            }
        }
        
        /// <summary>
        /// Processes entities based on current scan step.
        /// </summary>
        private void ProcessScanStep()
        {
            switch (_scanStep)
            {
                case 0: ProcessEnemies(); break;
                case 1: ProcessItems(); break;
                case 2: ProcessLoot(); break;
                case 3: ProcessNPCs(); break;
                case 4: ProcessPlayers(); break;
                case 5: ProcessAnimals(); break;
                case 6: ProcessLocalPlayer(); break;
            }
            
            _scanStep = (_scanStep + 1) % 7;
        }
        
        /// <summary>
        /// Processes enemy entities.
        /// </summary>
        private void ProcessEnemies()
        {
            if (Config.Settings.EnemyESP)
            {
                EntityTracker<EntityEnemy>.Instance.ScanForEntities();
                UI.dbg = EntityTracker<EntityEnemy>.Instance.Count.ToString();
            }
        }
        
        /// <summary>
        /// Processes item entities.
        /// </summary>
        private void ProcessItems()
        {
            if (Config.Settings.ItemESP)
            {
                EntityTracker<EntityItem>.Instance.ScanForEntities();
            }
        }
        
        /// <summary>
        /// Processes loot crates.
        /// </summary>
        private void ProcessLoot()
        {
            if (Config.Settings.ItemESP)
            {
                // Process EntitySupplyCrate if needed
            }
        }
        
        /// <summary>
        /// Processes NPC entities.
        /// </summary>
        private void ProcessNPCs()
        {
            if (Config.Settings.NPCESP)
            {
                EntityTracker<EntityNPC>.Instance.ScanForEntities();
            }
        }
        
        /// <summary>
        /// Processes player entities.
        /// </summary>
        private void ProcessPlayers()
        {
            if (Config.Settings.PlayerESP)
            {
                EntityTracker<EntityPlayer>.Instance.ScanForEntities();
            }
        }
        
        /// <summary>
        /// Processes animal entities.
        /// </summary>
        private void ProcessAnimals()
        {
            if (Config.Settings.AnimalESP)
            {
                EntityTracker<EntityAnimal>.Instance.ScanForEntities();
            }
        }
        
        /// <summary>
        /// Processes local player reference.
        /// </summary>
        private void ProcessLocalPlayer()
        {
            // Handle local player updates if needed
        }
        
        #endregion
        
        #region Rendering
        
        /// <summary>
        /// Draws debug overlay with performance information.
        /// </summary>
        private void DrawDebugOverlay()
        {
            try
            {
                float bx = Screen.width - 260f;
                float by = 5f;
                
                BatchedRenderer.AddBox(bx, by, 250f, 70f, Color.white, 1f);
                
                string dbg = UI.dbg ?? "";
                string winfo = "N/A";
                float wspeed = 0f;
                string ex = ErrorHandler.GetRecentLogs().FirstOrDefault() ?? "N/A";
                
                if (!string.IsNullOrEmpty(ex))
                {
                    int idx = ex.IndexOf('\n');
                    if (idx >= 0)
                    {
                        ex = ex.Substring(0, idx);
                    }
                }
                
                BatchedRenderer.AddText(new Vector2(bx + 8f, by + 6f), $"DBG: {dbg}", Color.white);
                BatchedRenderer.AddText(new Vector2(bx + 8f, by + 24f), $"Weapon: {winfo} speed:{wspeed:0.0}", Color.white);
                BatchedRenderer.AddText(new Vector2(bx + 8f, by + 42f), $"LastErr: {ex}", Color.white);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("HacksManager", $"Debug overlay error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Draws FOV circle for aimbot.
        /// </summary>
        private void DrawFOVCircle()
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            BatchedRenderer.AddCircle(center, Config.DEFAULT_AIM_FOV, Color.green, 64);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Checks and updates game state.
        /// </summary>
        private void CheckGameState()
        {
            bool currentLoaded = GameManager.Instance.gameStateManager.IsGameStarted();
            if (_isLoaded != currentLoaded)
            {
                _isLoaded = currentLoaded;
                ErrorHandler.LogInfo("HacksManager", $"Game state changed: {_isLoaded}");
            }
        }
        
        /// <summary>
        /// Handles special key inputs.
        /// </summary>
        private void HandleSpecialKeys()
        {
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                GameStats.Set(EnumGameStats.IsCreativeMenuEnabled, true);
            }
        }
        
        /// <summary>
        /// Converts world position to screen point with counting.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Screen position</returns>
        public Vector3 WorldToScreenPoint(Vector3 pos)
        {
            _w2sCount++;
            if (_mainCamera == null) _mainCamera = Camera.main;
            Vector3 screenPoint = _mainCamera.WorldToScreenPoint(pos);
            screenPoint.y = Screen.height - screenPoint.y; // Flip Y coordinate
            return screenPoint;
        }
        
        /// <summary>
        /// Safely stops the update coroutine.
        /// </summary>
        public void StopUpdateCoroutine()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }
        
        /// <summary>
        /// Cleanup all resources.
        /// </summary>
        private void Cleanup()
        {
            try
            {
                DetailedLogger.Log(DetailedLogger.LogLevel.INFO, "HacksManager", "Starting cleanup process");
                
                StopUpdateCoroutine();
                
                // Dispose all singleton instances
                ESPRenderer.Instance?.Dispose();
                EntityTracker<EntityEnemy>.Instance?.Dispose();
                EntityTracker<EntityAnimal>.Instance?.Dispose();
                EntityTracker<EntityPlayer>.Instance?.Dispose();
                EntityTracker<EntityItem>.Instance?.Dispose();
                EntityTracker<EntityNPC>.Instance?.Dispose();
                
                // Cleanup detailed logger last
                DetailedLogger.Cleanup();
                
                ErrorHandler.LogInfo("HacksManager", "Cleanup completed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HacksManager] Cleanup failed: {ex.Message}");
            }
        }
        
        #endregion
    }
}
