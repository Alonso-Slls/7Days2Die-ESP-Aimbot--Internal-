using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Centralized entity management system replacing static lists.
    /// Provides thread-safe entity tracking with lifecycle management.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        private static EntityManager _instance;
        public static EntityManager Instance => _instance ??= FindObjectOfType<EntityManager>();
        
        // Entity collections with thread-safe access
        private readonly Dictionary<Type, List<Entity>> _entityCollections = new Dictionary<Type, List<Entity>>();
        private readonly Dictionary<int, Entity> _trackedEntities = new Dictionary<int, Entity>();
        private readonly object _lockObject = new object();
        
        // Performance tracking
        private int _totalEntities;
        private int _lastScanCount;
        private float _lastScanTime;
        
        // Events for entity lifecycle
        public static event Action<Entity> OnEntityAdded;
        public static event Action<Entity> OnEntityRemoved;
        public static event Action<Entity> OnEntityUpdated;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCollections();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initialize entity collections for different types.
        /// </summary>
        private void InitializeCollections()
        {
            _entityCollections[typeof(EntityEnemy)] = new List<Entity>();
            _entityCollections[typeof(EntityPlayer)] = new List<Entity>();
            _entityCollections[typeof(EntityAnimal)] = new List<Entity>();
            _entityCollections[typeof(EntityItem)] = new List<Entity>();
            _entityCollections[typeof(EntityNPC)] = new List<Entity>();
        }
        
        /// <summary>
        /// Get all entities of a specific type.
        /// </summary>
        public List<T> GetEntities<T>() where T : Entity
        {
            lock (_lockObject)
            {
                Type type = typeof(T);
                if (_entityCollections.TryGetValue(type, out var entities))
                {
                    return entities.OfType<T>().ToList();
                }
                return new List<T>();
            }
        }
        
        /// <summary>
        /// Get all entities within range of a position.
        /// </summary>
        public List<Entity> GetEntitiesInRange(Vector3 position, float radius)
        {
            lock (_lockObject)
            {
                var nearbyEntities = new List<Entity>();
                foreach (var entity in _trackedEntities.Values)
                {
                    if (entity != null && entity.IsAlive())
                    {
                        float distance = Vector3.Distance(position, entity.transform.position);
                        if (distance <= radius)
                        {
                            nearbyEntities.Add(entity);
                        }
                    }
                }
                return nearbyEntities;
            }
        }
        
        /// <summary>
        /// Get all entities (for performance monitoring).
        /// </summary>
        public int GetTotalEntityCount()
        {
            lock (_lockObject)
            {
                return _trackedEntities.Count;
            }
        }
        
        /// <summary>
        /// Refresh all entity collections using optimized scanning.
        /// </summary>
        public void RefreshAllEntities()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Clear existing collections
                lock (_lockObject)
                {
                    foreach (var collection in _entityCollections.Values)
                    {
                        collection.Clear();
                    }
                    _trackedEntities.Clear();
                }
                
                // Scan for each entity type
                RefreshEntityType<EntityEnemy>();
                RefreshEntityType<EntityPlayer>();
                RefreshEntityType<EntityAnimal>();
                RefreshEntityType<EntityItem>();
                RefreshEntityType<EntityNPC>();
                
                stopwatch.Stop();
                _lastScanTime = stopwatch.ElapsedMilliseconds;
                _lastScanCount = _trackedEntities.Count;
                
                Debug.Log($"[EntityManager] Refreshed {_lastScanCount} entities in {_lastScanTime}ms");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EntityManager] Failed to refresh entities: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh entities of a specific type.
        /// </summary>
        private void RefreshEntityType<T>() where T : Entity
        {
            try
            {
                var entities = FindObjectsOfType<T>();
                Type type = typeof(T);
                
                lock (_lockObject)
                {
                    if (_entityCollections.TryGetValue(type, out var collection))
                    {
                        foreach (var entity in entities)
                        {
                            if (entity != null && entity.IsAlive())
                            {
                                collection.Add(entity);
                                _trackedEntities[entity.GetInstanceID()] = entity;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EntityManager] Failed to refresh {typeof(T).Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add an entity to tracking (for subscription model).
        /// </summary>
        public void TrackEntity(Entity entity)
        {
            if (entity == null) return;
            
            lock (_lockObject)
            {
                int id = entity.GetInstanceID();
                if (!_trackedEntities.ContainsKey(id))
                {
                    _trackedEntities[id] = entity;
                    
                    Type type = entity.GetType();
                    while (type != typeof(Entity) && type != null)
                    {
                        if (_entityCollections.TryGetValue(type, out var collection))
                        {
                            if (!collection.Contains(entity))
                            {
                                collection.Add(entity);
                            }
                        }
                        type = type.BaseType;
                    }
                    
                    OnEntityAdded?.Invoke(entity);
                }
            }
        }
        
        /// <summary>
        /// Remove an entity from tracking.
        /// </summary>
        public void UntrackEntity(Entity entity)
        {
            if (entity == null) return;
            
            lock (_lockObject)
            {
                int id = entity.GetInstanceID();
                if (_trackedEntities.Remove(id))
                {
                    // Remove from all collections
                    foreach (var collection in _entityCollections.Values)
                    {
                        collection.Remove(entity);
                    }
                    
                    OnEntityRemoved?.Invoke(entity);
                }
            }
        }
        
        /// <summary>
        /// Clean up invalid entities (called periodically).
        /// </summary>
        public void CleanupInvalidEntities()
        {
            lock (_lockObject)
            {
                var invalidEntities = new List<int>();
                
                foreach (var kvp in _trackedEntities)
                {
                    if (kvp.Value == null || !kvp.Value.IsAlive())
                    {
                        invalidEntities.Add(kvp.Key);
                    }
                }
                
                foreach (int id in invalidEntities)
                {
                    var entity = _trackedEntities[id];
                    _trackedEntities.Remove(id);
                    
                    foreach (var collection in _entityCollections.Values)
                    {
                        collection.Remove(entity);
                    }
                    
                    OnEntityRemoved?.Invoke(entity);
                }
                
                if (invalidEntities.Count > 0)
                {
                    Debug.Log($"[EntityManager] Cleaned up {invalidEntities.Count} invalid entities");
                }
            }
        }
        
        /// <summary>
        /// Get performance statistics.
        /// </summary>
        public string GetPerformanceStats()
        {
            lock (_lockObject)
            {
                return $"Entities: {_trackedEntities.Count}, Last Scan: {_lastScanCount} in {_lastScanTime}ms";
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
