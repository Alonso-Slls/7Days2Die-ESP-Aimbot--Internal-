using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Generic entity tracker that provides centralized scanning, filtering, and lifecycle management.
    /// Implements singleton pattern with proper disposal and thread safety.
    /// </summary>
    /// <typeparam name="T">The entity type to track</typeparam>
    public class EntityTracker<T> : IDisposable where T : Entity
    {
        #region Singleton Pattern
        
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// Gets the singleton instance for the specified entity type.
        /// Thread-safe with lazy initialization.
        /// </summary>
        public static EntityTracker<T> Instance
        {
            get
            {
                lock (_lockObject)
                {
                    Type type = typeof(T);
                    if (!_instances.TryGetValue(type, out var instance))
                    {
                        instance = new EntityTracker<T>();
                        _instances[type] = instance;
                    }
                    return (EntityTracker<T>)instance;
                }
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private readonly HashSet<T> _trackedEntities = new HashSet<T>();
        private readonly Dictionary<int, T> _entityById = new Dictionary<int, T>();
        private readonly object _entityLock = new object();
        private bool _disposed = false;
        
        // Performance tracking
        private int _totalScans = 0;
        private int _totalAdditions = 0;
        private int _totalRemovals = 0;
        private float _lastScanDuration = 0f;
        
        // Configuration
        private float _scanInterval = 5f;
        private float _lastScanTime = 0f;
        private bool _autoScanEnabled = true;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an entity is added to the tracker.
        /// </summary>
        public event Action<T> OnEntityAdded;
        
        /// <summary>
        /// Fired when an entity is removed from the tracker.
        /// </summary>
        public event Action<T> OnEntityRemoved;
        
        /// <summary>
        /// Fired when the tracker completes a scan.
        /// </summary>
        public event Action<int> OnScanCompleted;
        
        #endregion
        
        #region Constructor and Cleanup
        
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private EntityTracker()
        {
            ErrorHandler.LogInfo($"EntityTracker<{typeof(T).Name}>", "Initialized entity tracker");
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
                lock (_entityLock)
                {
                    _trackedEntities.Clear();
                    _entityById.Clear();
                }
                
                // Clear event handlers
                OnEntityAdded = null;
                OnEntityRemoved = null;
                OnScanCompleted = null;
                
                _disposed = true;
                ErrorHandler.LogInfo($"EntityTracker<{typeof(T).Name}>", "Disposed entity tracker");
            }
        }
        
        /// <summary>
        /// Destructor to ensure cleanup.
        /// </summary>
        ~EntityTracker()
        {
            Dispose(false);
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets the current count of tracked entities.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_entityLock)
                {
                    return _trackedEntities.Count;
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the scan interval in seconds.
        /// </summary>
        public float ScanInterval
        {
            get => _scanInterval;
            set => _scanInterval = Mathf.Max(1f, value);
        }
        
        /// <summary>
        /// Gets or sets whether automatic scanning is enabled.
        /// </summary>
        public bool AutoScanEnabled
        {
            get => _autoScanEnabled;
            set => _autoScanEnabled = value;
        }
        
        /// <summary>
        /// Gets performance statistics.
        /// </summary>
        public string PerformanceStats => $"Scans: {_totalScans}, Added: {_totalAdditions}, Removed: {_totalRemovals}, Last Scan: {_lastScanDuration:F2}ms";
        
        #endregion
        
        #region Entity Management
        
        /// <summary>
        /// Adds an entity to the tracker if it's valid and not already tracked.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <returns>True if the entity was added, false if it was already tracked or invalid</returns>
        public bool AddEntity(T entity)
        {
            if (entity == null || _disposed) return false;
            
            try
            {
                int entityId = entity.GetInstanceID();
                
                lock (_entityLock)
                {
                    if (_entityById.ContainsKey(entityId)) return false;
                    
                    if (entity.IsAlive())
                    {
                        _trackedEntities.Add(entity);
                        _entityById[entityId] = entity;
                        _totalAdditions++;
                        
                        OnEntityAdded?.Invoke(entity);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"EntityTracker<{typeof(T).Name}>", $"Failed to add entity: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes an entity from the tracker.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <returns>True if the entity was removed, false if it wasn't tracked</returns>
        public bool RemoveEntity(T entity)
        {
            if (entity == null || _disposed) return false;
            
            try
            {
                int entityId = entity.GetInstanceID();
                
                lock (_entityLock)
                {
                    if (_entityById.Remove(entityId))
                    {
                        _trackedEntities.Remove(entity);
                        _totalRemovals++;
                        
                        OnEntityRemoved?.Invoke(entity);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"EntityTracker<{typeof(T).Name}>", $"Failed to remove entity: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all currently tracked entities.
        /// </summary>
        /// <returns>Array of tracked entities</returns>
        public T[] GetAllEntities()
        {
            lock (_entityLock)
            {
                return _trackedEntities.Where(e => e != null && e.IsAlive()).ToArray();
            }
        }
        
        /// <summary>
        /// Gets entities within the specified distance of a position.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="radius">Maximum distance</param>
        /// <returns>Entities within range</returns>
        public T[] GetEntitiesInRange(Vector3 position, float radius)
        {
            lock (_entityLock)
            {
                return _trackedEntities
                    .Where(e => e != null && e.IsAlive())
                    .Where(e => Vector3.Distance(position, e.transform.position) <= radius)
                    .ToArray();
            }
        }
        
        /// <summary>
        /// Gets entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">Filter function</param>
        /// <returns>Matching entities</returns>
        public T[] GetEntitiesWhere(Func<T, bool> predicate)
        {
            if (predicate == null) return new T[0];
            
            lock (_entityLock)
            {
                return _trackedEntities
                    .Where(e => e != null && e.IsAlive())
                    .Where(predicate)
                    .ToArray();
            }
        }
        
        #endregion
        
        #region Scanning
        
        /// <summary>
        /// Performs a full scan for entities of type T using FindObjectsOfType.
        /// Automatically updates the tracker with any new or removed entities.
        /// </summary>
        public void ScanForEntities()
        {
            if (_disposed) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                T[] allEntities = UnityEngine.Object.FindObjectsOfType<T>();
                var currentEntities = new HashSet<int>();
                
                // Process found entities
                foreach (T entity in allEntities)
                {
                    if (entity != null && entity.IsAlive())
                    {
                        int entityId = entity.GetInstanceID();
                        currentEntities.Add(entityId);
                        
                        // Add if not already tracked
                        if (!_entityById.ContainsKey(entityId))
                        {
                            AddEntity(entity);
                        }
                    }
                }
                
                // Remove entities that no longer exist
                var entitiesToRemove = new List<int>();
                lock (_entityLock)
                {
                    foreach (var kvp in _entityById)
                    {
                        if (!currentEntities.Contains(kvp.Key))
                        {
                            entitiesToRemove.Add(kvp.Key);
                        }
                    }
                }
                
                foreach (int entityId in entitiesToRemove)
                {
                    if (_entityById.TryGetValue(entityId, out var entity))
                    {
                        RemoveEntity(entity);
                    }
                }
                
                _totalScans++;
                stopwatch.Stop();
                _lastScanDuration = stopwatch.ElapsedMilliseconds;
                
                OnScanCompleted?.Invoke(_trackedEntities.Count);
                
                ErrorHandler.LogDebug($"EntityTracker<{typeof(T).Name}>", 
                    $"Scan completed: {_trackedEntities.Count} entities in {_lastScanDuration}ms");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"EntityTracker<{typeof(T).Name}>", $"Scan failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the tracker, performing automatic scans if enabled.
        /// Should be called from Update() or similar.
        /// </summary>
        public void Update()
        {
            if (_disposed || !_autoScanEnabled) return;
            
            _lastScanTime += Time.deltaTime;
            if (_lastScanTime >= _scanInterval)
            {
                _lastScanTime = 0f;
                ScanForEntities();
            }
        }
        
        /// <summary>
        /// Cleans up invalid entities (destroyed or null references).
        /// </summary>
        public void CleanupInvalidEntities()
        {
            if (_disposed) return;
            
            var invalidEntities = new List<T>();
            
            lock (_entityLock)
            {
                foreach (var entity in _trackedEntities)
                {
                    if (entity == null || !entity.IsAlive())
                    {
                        invalidEntities.Add(entity);
                    }
                }
            }
            
            foreach (var entity in invalidEntities)
            {
                RemoveEntity(entity);
            }
            
            if (invalidEntities.Count > 0)
            {
                ErrorHandler.LogInfo($"EntityTracker<{typeof(T).Name}>", 
                    $"Cleaned up {invalidEntities.Count} invalid entities");
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Clears all tracked entities.
        /// </summary>
        public void Clear()
        {
            lock (_entityLock)
            {
                var entities = _trackedEntities.ToArray();
                _trackedEntities.Clear();
                _entityById.Clear();
                
                foreach (var entity in entities)
                {
                    OnEntityRemoved?.Invoke(entity);
                }
            }
            
            ErrorHandler.LogInfo($"EntityTracker<{typeof(T).Name}>", "Cleared all tracked entities");
        }
        
        /// <summary>
        /// Gets the nearest entity to the specified position.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="maxDistance">Maximum search distance</param>
        /// <returns>Nearest entity or null if none found</returns>
        public T GetNearestEntity(Vector3 position, float maxDistance = float.MaxValue)
        {
            T nearest = null;
            float nearestDistance = maxDistance;
            
            lock (_entityLock)
            {
                foreach (var entity in _trackedEntities)
                {
                    if (entity != null && entity.IsAlive())
                    {
                        float distance = Vector3.Distance(position, entity.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearest = entity;
                        }
                    }
                }
            }
            
            return nearest;
        }
        
        #endregion
    }
}
