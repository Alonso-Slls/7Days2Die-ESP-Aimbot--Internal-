using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Entity subscription system that hooks into Unity's entity lifecycle.
    /// Replaces expensive FindObjectsOfType scans with O(1) subscription updates.
    /// </summary>
    public static class EntitySubscription
    {
        private static bool _initialized = false;
        private static readonly Dictionary<Type, HashSet<int>> _subscribedEntities = new Dictionary<Type, HashSet<int>>();
        private static readonly Dictionary<int, Entity> _entityCache = new Dictionary<int, Entity>();
        private static readonly object _lockObject = new object();
        
        // Performance tracking
        private static int _totalSubscriptions = 0;
        private static int _subscriptionChanges = 0;
        
        /// <summary>
        /// Initialize the subscription system and hook into entity lifecycle events.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            // Initialize subscription collections for each entity type
            RegisterEntityType<EntityEnemy>();
            RegisterEntityType<EntityPlayer>();
            RegisterEntityType<EntityAnimal>();
            RegisterEntityType<EntityItem>();
            RegisterEntityType<EntityNPC>();
            
            _initialized = true;
            ErrorHandler.LogInfo("EntitySubscription", "Subscription system initialized");
        }
        
        /// <summary>
        /// Register an entity type for subscription tracking.
        /// </summary>
        private static void RegisterEntityType<T>() where T : Entity
        {
            Type type = typeof(T);
            lock (_lockObject)
            {
                _subscribedEntities[type] = new HashSet<int>();
            }
        }
        
        /// <summary>
        /// Subscribe an entity to the tracking system.
        /// Called when entities are created or discovered.
        /// </summary>
        public static void Subscribe(Entity entity)
        {
            if (entity == null || !_initialized) return;
            
            try
            {
                int entityId = entity.GetInstanceID();
                Type entityType = entity.GetType();
                
                lock (_lockObject)
                {
                    // Add to entity cache
                    _entityCache[entityId] = entity;
                    
                    // Add to type-specific subscriptions
                    Type currentType = entityType;
                    while (currentType != null && currentType != typeof(Entity))
                    {
                        if (_subscribedEntities.TryGetValue(currentType, out var entitySet))
                        {
                            entitySet.Add(entityId);
                        }
                        currentType = currentType.BaseType;
                    }
                    
                    _totalSubscriptions++;
                    _subscriptionChanges++;
                }
                
                // Notify entity manager
                if (EntityManager.Instance != null)
                {
                    EntityManager.Instance.TrackEntity(entity);
                }
                
                ErrorHandler.LogDebug("EntitySubscription", $"Subscribed {entityType.Name} (ID: {entityId})");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("EntitySubscription", $"Failed to subscribe entity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unsubscribe an entity from the tracking system.
        /// Called when entities are destroyed.
        /// </summary>
        public static void Unsubscribe(Entity entity)
        {
            if (entity == null || !_initialized) return;
            
            try
            {
                int entityId = entity.GetInstanceID();
                Type entityType = entity.GetType();
                
                lock (_lockObject)
                {
                    // Remove from entity cache
                    _entityCache.Remove(entityId);
                    
                    // Remove from all type subscriptions
                    foreach (var entitySet in _subscribedEntities.Values)
                    {
                        entitySet.Remove(entityId);
                    }
                    
                    _subscriptionChanges++;
                }
                
                // Notify entity manager
                if (EntityManager.Instance != null)
                {
                    EntityManager.Instance.UntrackEntity(entity);
                }
                
                ErrorHandler.LogDebug("EntitySubscription", $"Unsubscribed {entityType.Name} (ID: {entityId})");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("EntitySubscription", $"Failed to unsubscribe entity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get all subscribed entities of a specific type.
        /// </summary>
        public static List<T> GetSubscribedEntities<T>() where T : Entity
        {
            Type type = typeof(T);
            var result = new List<T>();
            
            lock (_lockObject)
            {
                if (_subscribedEntities.TryGetValue(type, out var entityIds))
                {
                    foreach (int entityId in entityIds)
                    {
                        if (_entityCache.TryGetValue(entityId, out var entity) && entity is T typedEntity)
                        {
                            if (entity != null && entity.IsAlive())
                            {
                                result.Add(typedEntity);
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get all subscribed entities within range of a position.
        /// </summary>
        public static List<Entity> GetEntitiesInRange(Vector3 position, float radius)
        {
            var result = new List<Entity>();
            
            lock (_lockObject)
            {
                foreach (var entity in _entityCache.Values)
                {
                    if (entity != null && entity.IsAlive())
                    {
                        float distance = Vector3.Distance(position, entity.transform.position);
                        if (distance <= radius)
                        {
                            result.Add(entity);
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get total count of subscribed entities.
        /// </summary>
        public static int GetTotalSubscribedCount()
        {
            lock (_lockObject)
            {
                return _entityCache.Count;
            }
        }
        
        /// <summary>
        /// Clean up invalid entities (destroyed or null references).
        /// Should be called periodically to maintain accuracy.
        /// </summary>
        public static void CleanupInvalidEntities()
        {
            if (!_initialized) return;
            
            var invalidEntities = new List<int>();
            
            lock (_lockObject)
            {
                // Find invalid entities
                foreach (var kvp in _entityCache)
                {
                    if (kvp.Value == null || !kvp.Value.IsAlive())
                    {
                        invalidEntities.Add(kvp.Key);
                    }
                }
                
                // Remove invalid entities
                foreach (int entityId in invalidEntities)
                {
                    var entity = _entityCache[entityId];
                    _entityCache.Remove(entityId);
                    
                    foreach (var entitySet in _subscribedEntities.Values)
                    {
                        entitySet.Remove(entityId);
                    }
                }
            }
            
            if (invalidEntities.Count > 0)
            {
                ErrorHandler.LogInfo("EntitySubscription", $"Cleaned up {invalidEntities.Count} invalid entities");
            }
        }
        
        /// <summary>
        /// Get performance statistics.
        /// </summary>
        public static string GetPerformanceStats()
        {
            lock (_lockObject)
            {
                return $"Subscriptions: {_totalSubscriptions}, Changes: {_subscriptionChanges}, Active: {_entityCache.Count}";
            }
        }
        
        /// <summary>
        /// Force rescan for existing entities and subscribe them.
        /// Used for initial setup or when entities might have been missed.
        /// </summary>
        public static void RescanAndSubscribeExistingEntities()
        {
            if (!_initialized) return;
            
            try
            {
                // Scan for existing entities and subscribe them
                SubscribeExistingEntities<EntityEnemy>();
                SubscribeExistingEntities<EntityPlayer>();
                SubscribeExistingEntities<EntityAnimal>();
                SubscribeExistingEntities<EntityItem>();
                SubscribeExistingEntities<EntityNPC>();
                
                ErrorHandler.LogInfo("EntitySubscription", $"Rescan completed. Total subscribed: {GetTotalSubscribedCount()}");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("EntitySubscription", $"Rescan failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Subscribe existing entities of a specific type.
        /// </summary>
        private static void SubscribeExistingEntities<T>() where T : Entity
        {
            try
            {
                var entities = UnityEngine.Object.FindObjectsOfType<T>();
                foreach (var entity in entities)
                {
                    if (entity != null && entity.IsAlive())
                    {
                        Subscribe(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("EntitySubscription", $"Failed to subscribe existing {typeof(T).Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Reset the subscription system.
        /// </summary>
        public static void Reset()
        {
            lock (_lockObject)
            {
                _subscribedEntities.Clear();
                _entityCache.Clear();
                _totalSubscriptions = 0;
                _subscriptionChanges = 0;
            }
            
            ErrorHandler.LogInfo("EntitySubscription", "Subscription system reset");
        }
    }
}
