using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Spatial partitioning system using grid-based hash map for efficient entity queries.
    /// Reduces O(n) entity searches to near O(1) for local area queries.
    /// </summary>
    public static class SpatialGrid
    {
        // Grid configuration
        private static float cellSize = Config.DEFAULT_CELL_SIZE; // 50 meter grid cells
        private static Dictionary<Vector2Int, List<Entity>> grid = new Dictionary<Vector2Int, List<Entity>>();
        private static List<Entity> nearbyEntities = new List<Entity>();
        
        // Performance tracking
        private static int totalEntities = 0;
        private static int gridUpdates = 0;
        
        /// <summary>
        /// Convert world position to grid coordinates.
        /// </summary>
        private static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / cellSize),
                Mathf.FloorToInt(worldPos.z / cellSize)
            );
        }
        
        /// <summary>
        /// Get the grid key for a specific world position.
        /// </summary>
        private static Vector2Int GetGridKey(Vector3 position)
        {
            return WorldToGrid(position);
        }
        
        /// <summary>
        /// Add an entity to the spatial grid.
        /// </summary>
        public static void AddEntity(Entity entity)
        {
            if (entity == null) return;
            
            Vector2Int gridKey = GetGridKey(entity.transform.position);
            
            if (!grid.ContainsKey(gridKey))
            {
                grid[gridKey] = new List<Entity>();
            }
            
            grid[gridKey].Add(entity);
            totalEntities++;
        }
        
        /// <summary>
        /// Remove an entity from the spatial grid.
        /// </summary>
        public static void RemoveEntity(Entity entity)
        {
            if (entity == null) return;
            
            Vector2Int gridKey = GetGridKey(entity.transform.position);
            
            if (grid.ContainsKey(gridKey))
            {
                grid[gridKey].Remove(entity);
                if (grid[gridKey].Count == 0)
                {
                    grid.Remove(gridKey);
                }
                totalEntities--;
            }
        }
        
        /// <summary>
        /// Update an entity's position in the spatial grid.
        /// </summary>
        public static void UpdateEntity(Entity entity)
        {
            if (entity == null) return;
            
            // Remove from old position (we don't track old position, so we'll rebuild)
            // For performance, we'll do bulk updates instead of individual updates
        }
        
        /// <summary>
        /// Get all entities within a certain radius of a position.
        /// </summary>
        public static List<Entity> GetEntitiesInRange(Vector3 center, float radius)
        {
            nearbyEntities.Clear();
            
            // Calculate grid bounds for the search area
            Vector2Int minGrid = WorldToGrid(center - new Vector3(radius, 0, radius));
            Vector2Int maxGrid = WorldToGrid(center + new Vector3(radius, 0, radius));
            
            // Check all grid cells within the search area
            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int z = minGrid.y; z <= maxGrid.y; z++)
                {
                    Vector2Int gridKey = new Vector2Int(x, z);
                    if (grid.ContainsKey(gridKey))
                    {
                        foreach (Entity entity in grid[gridKey])
                        {
                            if (entity != null)
                            {
                                float distance = Vector3.Distance(center, entity.transform.position);
                                if (distance <= radius)
                                {
                                    nearbyEntities.Add(entity);
                                }
                            }
                        }
                    }
                }
            }
            
            return nearbyEntities;
        }
        
        /// <summary>
        /// Get all entities within the player's vicinity (optimized for ESP).
        /// </summary>
        public static List<Entity> GetNearbyEntities(Vector3 playerPosition, float maxDistance = Config.DEFAULT_MAX_DISTANCE)
        {
            return GetEntitiesInRange(playerPosition, maxDistance);
        }
        
        /// <summary>
        /// Clear and rebuild the entire spatial grid.
        /// Call this periodically to maintain accuracy.
        /// </summary>
        public static void RebuildGrid(List<Entity> allEntities)
        {
            ClearGrid();
            
            foreach (Entity entity in allEntities)
            {
                if (entity != null && entity.IsAlive())
                {
                    AddEntity(entity);
                }
            }
            
            gridUpdates++;
        }
        
        /// <summary>
        /// Clear the entire spatial grid.
        /// </summary>
        public static void ClearGrid()
        {
            grid.Clear();
            totalEntities = 0;
        }
        
        /// <summary>
        /// Get performance statistics for the spatial grid.
        /// </summary>
        public static string GetStats()
        {
            return $"Spatial Grid - Cells: {grid.Count}, Entities: {totalEntities}, Updates: {gridUpdates}";
        }
        
        /// <summary>
        /// Set the cell size for the spatial grid.
        /// </summary>
        public static void SetCellSize(float size)
        {
            if (size > 0f && size != cellSize)
            {
                cellSize = size;
                ClearGrid(); // Clear grid to force rebuild with new cell size
            }
        }
        
        /// <summary>
        /// Get the current cell size.
        /// </summary>
        public static float GetCellSize()
        {
            return cellSize;
        }
    }
    
    /// <summary>
    /// Extension methods for Entity to work with spatial grid.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Check if an entity is within range of a position.
        /// </summary>
        public static bool IsInRange(this Entity entity, Vector3 position, float range)
        {
            if (entity == null) return false;
            return Vector3.Distance(entity.transform.position, position) <= range;
        }
        
        /// <summary>
        /// Check if an entity is visible from a position (simple line of sight check).
        /// </summary>
        public static bool IsVisibleFrom(this Entity entity, Vector3 observerPosition, float maxDistance = Config.DEFAULT_VISIBILITY_DISTANCE)
        {
            if (entity == null) return false;
            
            float distance = Vector3.Distance(observerPosition, entity.transform.position);
            if (distance > maxDistance) return false;
            
            // Simple visibility check - could be enhanced with raycasting
            Vector3 direction = (entity.transform.position - observerPosition).normalized;
            return Vector3.Angle(observerPosition, direction) < 90f; // Basic FOV check
        }
    }
}
