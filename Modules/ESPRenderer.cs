using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Dedicated ESP renderer that separates UI concerns from game logic.
    /// Handles all ESP rendering operations with optimized batching and caching.
    /// Implements singleton pattern with proper disposal.
    /// </summary>
    public class ESPRenderer : IDisposable
    {
        #region Singleton Pattern
        
        private static ESPRenderer _instance;
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// Gets the singleton instance of the ESP renderer.
        /// Thread-safe with lazy initialization.
        /// </summary>
        public static ESPRenderer Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null || _instance._disposed)
                    {
                        _instance = new ESPRenderer();
                    }
                    return _instance;
                }
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private bool _disposed = false;
        private Camera _mainCamera;
        private Vector3 _lastCameraPosition;
        private float _lastCameraUpdateTime;
        
        // Performance tracking
        private int _totalRenderCalls = 0;
        private int _totalEntitiesRendered = 0;
        private float _lastRenderTime = 0f;
        
        // Caching
        private readonly Dictionary<int, Vector2> _screenPositionCache = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, float> _distanceCache = new Dictionary<int, float>();
        private float _cacheUpdateTime = 0f;
        private const float CACHE_UPDATE_INTERVAL = Config.CACHE_UPDATE_INTERVAL; // Update cache every 100ms
        
        // Render settings
        private bool _renderBoxes = true;
        private bool _renderLines = true;
        private bool _renderText = true;
        private bool _renderBones = false;
        private float _maxRenderDistance = 500f;
        
        #endregion
        
        #region Constructor and Cleanup
        
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private ESPRenderer()
        {
            _mainCamera = Camera.main;
            ErrorHandler.LogInfo("ESPRenderer", "Initialized ESP renderer");
        }
        
        /// <summary>
        /// Implements IDisposable pattern for proper cleanup.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Protected dispose method.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _screenPositionCache.Clear();
                _distanceCache.Clear();
                _mainCamera = null;
                _disposed = true;
                
                ErrorHandler.LogInfo("ESPRenderer", "Disposed ESP renderer");
            }
        }
        
        /// <summary>
        /// Destructor to ensure cleanup.
        /// </summary>
        ~ESPRenderer()
        {
            Dispose(false);
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets or sets whether boxes should be rendered.
        /// </summary>
        public bool RenderBoxes
        {
            get => _renderBoxes;
            set => _renderBoxes = value;
        }
        
        /// <summary>
        /// Gets or sets whether lines should be rendered.
        /// </summary>
        public bool RenderLines
        {
            get => _renderLines;
            set => _renderLines = value;
        }
        
        /// <summary>
        /// Gets or sets whether text should be rendered.
        /// </summary>
        public bool RenderText
        {
            get => _renderText;
            set => _renderText = value;
        }
        
        /// <summary>
        /// Gets or sets whether bones should be rendered.
        /// </summary>
        public bool RenderBones
        {
            get => _renderBones;
            set => _renderBones = value;
        }
        
        /// <summary>
        /// Gets or sets the maximum render distance.
        /// </summary>
        public float MaxRenderDistance
        {
            get => _maxRenderDistance;
            set => _maxRenderDistance = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Gets performance statistics.
        /// </summary>
        public string PerformanceStats => $"Calls: {_totalRenderCalls}, Entities: {_totalEntitiesRendered}, Time: {_lastRenderTime:F2}ms";
        
        #endregion
        
        #region Core Rendering Methods
        
        /// <summary>
        /// Renders ESP for all entity types.
        /// This is the main entry point for ESP rendering.
        /// </summary>
        public void RenderAllESP()
        {
            if (_disposed || _mainCamera == null) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Update camera reference if needed
                UpdateCameraReference();
                
                // Update caches periodically
                UpdateCaches();
                
                // Clear render batches
                BatchedRenderer.ClearBatches();
                
                // Render each entity type
                RenderEnemyESP();
                RenderPlayerESP();
                RenderAnimalESP();
                RenderItemESP();
                RenderNPCESP();
                
                // Execute batched rendering
                BatchedRenderer.RenderBatches();
                
                _totalRenderCalls++;
                stopwatch.Stop();
                _lastRenderTime = stopwatch.ElapsedMilliseconds;
                
                // Reset per-frame counters
                ResetFrameCounters();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("ESPRenderer", $"ESP rendering failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Renders ESP for enemy entities.
        /// </summary>
        private void RenderEnemyESP()
        {
            if (!Config.Settings.EnemyESP) return;
            
            var enemies = EntityTracker<EntityEnemy>.Instance.GetEntitiesInRange(_mainCamera.transform.position, _maxRenderDistance);
            
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive() && IsEntityInFOV(enemy.transform.position))
                {
                    RenderEntityESP(enemy, Config.Settings.EnemyColor, "Enemy");
                }
            }
        }
        
        /// <summary>
        /// Renders ESP for player entities.
        /// </summary>
        private void RenderPlayerESP()
        {
            if (!Config.Settings.PlayerESP) return;
            
            var players = EntityTracker<EntityPlayer>.Instance.GetEntitiesInRange(_mainCamera.transform.position, _maxRenderDistance);
            
            foreach (var player in players)
            {
                if (player != null && player.IsAlive() && player.IsSpawned() && IsEntityInFOV(player.transform.position))
                {
                    RenderEntityESP(player, Config.Settings.PlayerColor, "Player");
                }
            }
        }
        
        /// <summary>
        /// Renders ESP for animal entities.
        /// </summary>
        private void RenderAnimalESP()
        {
            if (!Config.Settings.AnimalESP) return;
            
            var animals = EntityTracker<EntityAnimal>.Instance.GetEntitiesInRange(_mainCamera.transform.position, _maxRenderDistance);
            
            foreach (var animal in animals)
            {
                if (animal != null && animal.IsAlive() && IsEntityInFOV(animal.transform.position))
                {
                    RenderEntityESP(animal, Config.Settings.AnimalColor, "Animal");
                }
            }
        }
        
        /// <summary>
        /// Renders ESP for item entities.
        /// </summary>
        private void RenderItemESP()
        {
            if (!Config.Settings.ItemESP) return;
            
            var items = EntityTracker<EntityItem>.Instance.GetEntitiesInRange(_mainCamera.transform.position, _maxRenderDistance);
            
            foreach (var item in items)
            {
                if (item != null && IsEntityInFOV(item.transform.position))
                {
                    RenderEntityESP(item, Config.Settings.ItemColor, "Item");
                }
            }
        }
        
        /// <summary>
        /// Renders ESP for NPC entities.
        /// </summary>
        private void RenderNPCESP()
        {
            if (!Config.Settings.NPCESP) return;
            
            var npcs = EntityTracker<EntityNPC>.Instance.GetEntitiesInRange(_mainCamera.transform.position, _maxRenderDistance);
            
            foreach (var npc in npcs)
            {
                if (npc != null && npc.IsAlive() && npc.IsSpawned() && IsEntityInFOV(npc.transform.position))
                {
                    RenderEntityESP(npc, Config.Settings.NPCColor, "NPC");
                }
            }
        }
        
        #endregion
        
        #region Entity Rendering
        
        /// <summary>
        /// Renders ESP for a single entity.
        /// </summary>
        /// <param name="entity">The entity to render</param>
        /// <param name="color">ESP color</param>
        /// <param name="label">Entity label</param>
        private void RenderEntityESP(Entity entity, Color color, string label)
        {
            if (entity == null) return;
            
            int entityId = entity.GetInstanceID();
            
            // Get cached screen position
            if (!_screenPositionCache.TryGetValue(entityId, out var screenPos))
            {
                screenPos = WorldToScreenPoint(entity.transform.position);
                _screenPositionCache[entityId] = screenPos;
            }
            
            // Skip if behind camera
            if (screenPos.y < 0) return;
            
            // Get cached distance
            if (!_distanceCache.TryGetValue(entityId, out float distance))
            {
                distance = Vector3.Distance(_mainCamera.transform.position, entity.transform.position);
                _distanceCache[entityId] = distance;
            }
            
            // Render ESP elements
            if (_renderBoxes)
            {
                RenderESPBox(entity, color);
            }
            
            if (_renderLines)
            {
                RenderESPLine(screenPos, color);
            }
            
            if (_renderText)
            {
                RenderESPText(screenPos, label, distance, color);
            }
            
            if (_renderBones && entity is EntityEnemy enemy)
            {
                RenderEnemyBones(enemy, color);
            }
            
            _totalEntitiesRendered++;
        }
        
        /// <summary>
        /// Renders an ESP box around an entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="color">Box color</param>
        private void RenderESPBox(Entity entity, Color color)
        {
            try
            {
                // Use the appropriate ESP drawing method based on entity type
                switch (entity)
                {
                    case EntityEnemy enemy:
                        Modules.ESP.esp_drawBox(enemy, color);
                        break;
                    case EntityPlayer player:
                        Modules.ESP.esp_drawBox(player, color);
                        break;
                    case EntityAnimal animal:
                        Modules.ESP.esp_drawBox(animal, color);
                        break;
                    case EntityItem item:
                        Modules.ESP.esp_drawBox(item, color);
                        break;
                    case EntityNPC npc:
                        Modules.ESP.esp_drawBox(npc, color);
                        break;
                    case EntitySupplyCrate crate:
                        Modules.ESP.esp_drawBox(crate, color);
                        break;
                    default:
                        // Fallback for unknown entity types
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("ESPRenderer", $"Failed to render ESP box: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Renders a line from screen center to entity position.
        /// </summary>
        /// <param name="screenPosition">Entity screen position</param>
        /// <param name="color">Line color</param>
        private void RenderESPLine(Vector2 screenPosition, Color color)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            BatchedRenderer.AddLine(screenCenter, screenPosition, color, 1f);
        }
        
        /// <summary>
        /// Renders text label for an entity.
        /// </summary>
        /// <param name="screenPosition">Entity screen position</param>
        /// <param name="label">Entity label</param>
        /// <param name="distance">Distance to entity</param>
        /// <param name="color">Text color</param>
        private void RenderESPText(Vector2 screenPosition, string label, float distance, Color color)
        {
            string text = $"{label}\n{distance:F0}m";
            BatchedRenderer.AddText(screenPosition, text, color, 12);
        }
        
        /// <summary>
        /// Renders skeleton bones for an enemy entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="color">Bone color</param>
        private void RenderEnemyBones(Entity entity, Color color)
        {
            try
            {
                // Use existing bone rendering if available
                if (Config.Settings.EnemyBones && entity is EntityEnemy enemy)
                {
                    // This would integrate with existing bone rendering logic
                    // For now, we'll use the existing ESP system
                    Modules.ESP.esp_drawBox(enemy, color);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("ESPRenderer", $"Failed to render enemy bones: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Updates the camera reference if needed.
        /// </summary>
        private void UpdateCameraReference()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            else if (_mainCamera != Camera.main)
            {
                _mainCamera = Camera.main;
                InvalidateCaches();
            }
        }
        
        /// <summary>
        /// Updates position and distance caches.
        /// </summary>
        private void UpdateCaches()
        {
            _cacheUpdateTime += Time.deltaTime;
            if (_cacheUpdateTime < CACHE_UPDATE_INTERVAL) return;
            
            _cacheUpdateTime = 0f;
            
            // Clear caches if camera moved significantly
            if (_mainCamera != null)
            {
                Vector3 currentCameraPos = _mainCamera.transform.position;
                if (Vector3.Distance(currentCameraPos, _lastCameraPosition) > 10f)
                {
                    InvalidateCaches();
                    _lastCameraPosition = currentCameraPos;
                }
            }
        }
        
        /// <summary>
        /// Invalidates all caches.
        /// </summary>
        private void InvalidateCaches()
        {
            _screenPositionCache.Clear();
            _distanceCache.Clear();
        }
        
        /// <summary>
        /// Converts world position to screen point with caching.
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <returns>Screen position</returns>
        private Vector2 WorldToScreenPoint(Vector3 worldPosition)
        {
            if (_mainCamera == null) return Vector2.zero;
            
            Vector3 screenPoint = _mainCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        }
        
        /// <summary>
        /// Checks if an entity is within the field of view.
        /// </summary>
        /// <param name="entityPosition">Entity world position</param>
        /// <returns>True if entity is in FOV</returns>
        private bool IsEntityInFOV(Vector3 entityPosition)
        {
            if (!Config.Settings.FOVAwareESP || _mainCamera == null) return true;
            
            try
            {
                Vector3 playerForward = _mainCamera.transform.forward;
                Vector3 directionToTarget = (entityPosition - _mainCamera.transform.position).normalized;
                float angle = Vector3.Angle(playerForward, directionToTarget);
                return angle <= Config.Settings.FOVThreshold / 2f;
            }
            catch
            {
                return true; // Default to visible on error
            }
        }
        
        /// <summary>
        /// Resets per-frame counters.
        /// </summary>
        private void ResetFrameCounters()
        {
            _totalEntitiesRendered = 0;
        }
        
        #endregion
    }
}
