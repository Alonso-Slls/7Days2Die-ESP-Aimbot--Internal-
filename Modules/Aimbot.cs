using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;

namespace Game_7D2D.Modules
{

    class Aimbot
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        public static bool hasTarget = false;
        private static Camera Camera;
        private static Vector2 lastAimTarget = Vector2.zero;
        private static Dictionary<int, Transform> headCache = new Dictionary<int, Transform>();
        private static Dictionary<int, Transform> torsoCache = new Dictionary<int, Transform>();
        private static float lastFireTime = 0f;
        private static float fireCooldown = Config.FIRE_COOLDOWN; // seconds between auto-fires
        private struct PosSample { public Vector3 pos; public float time; }
        private static Dictionary<int, PosSample> posSamples = new Dictionary<int, PosSample>();

        // Per-weapon projectile speed detection (cached to avoid heavy reflection calls)
        private static float cachedWeaponSpeed = 0f;
        private static int cachedWeaponId = 0;
        private static float lastWeaponCheckTime = 0f;
        private const float weaponCheckInterval = Config.WEAPON_CHECK_INTERVAL; // check at most 4x/sec
        private static string cachedWeaponInfo = ""; // e.g. "CompoundBow:180"
        private static string lastException = ""; // last exception text (first line for overlay)

        private struct Candidate
        {
            public Vector3 worldPos;
            public Vector3 screenPos;
            public float screenDist;
            public Entity entity;
            public float distance;
            public Vector3 velocity;
        }
        
                
        public static void AimAssist()
        {
            //Aimbot is semi copy and pasted
            float minDist = 9999f;

            Vector2 target = Vector2.zero;

            if (UI.t_TAnimals)
            {
                foreach (EntityAnimal animal in Hacks.eAnimal)
                {
                    if (animal && animal.IsAlive())
                    {
                        try
                        {
                            Vector3 lookAt = animal.emodel.GetHeadTransform().position;
                            if (lookAt == Vector3.zero) continue;
                            
                            Vector3 w2s = Camera.main.WorldToScreenPoint(lookAt);
                            if (float.IsNaN(w2s.x) || float.IsNaN(w2s.y)) continue;

                            // If they're outside of our FOV.
                            if (Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(w2s.x, w2s.y)) > 150f)
                                continue;

                            if (IsOnScreen(w2s))
                            {
                                float distance = Math.Abs(Vector2.Distance(new Vector2(w2s.x, Screen.height - w2s.y), new Vector2(Screen.width / 2, Screen.height / 2)));

                                if (distance < minDist)
                                {
                                    minDist = distance;
                                    target = new Vector2(w2s.x, Screen.height - w2s.y);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.Log($"[Aimbot] Animal targeting error: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            if (UI.t_TPlayers)
            {
                foreach (EntityPlayer player in Hacks.ePlayers)
                {
                    if (player && player.IsAlive())
                    {
                        try
                        {
                            Vector3 lookAt = player.emodel.GetHeadTransform().position;
                            if (lookAt == Vector3.zero) continue;
                            
                            Vector3 w2s = Camera.main.WorldToScreenPoint(lookAt);
                            if (float.IsNaN(w2s.x) || float.IsNaN(w2s.y)) continue;

                            // If they're outside of our FOV.
                            if (Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(w2s.x, w2s.y)) > 150f)
                                continue;

                            if (IsOnScreen(w2s))
                            {
                                float distance = Math.Abs(Vector2.Distance(new Vector2(w2s.x, Screen.height - w2s.y), new Vector2(Screen.width / 2, Screen.height / 2)));

                                if (distance < minDist)
                                {
                                    minDist = distance;
                                    target = new Vector2(w2s.x, Screen.height - w2s.y);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.Log($"[Aimbot] Player targeting error: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            if (UI.t_TEnemies) {
                foreach (EntityEnemy enemy in Hacks.eEnemy)
                {
                    if (enemy && enemy.IsAlive())
                    {
                        try
                        {
                            Vector3 lookAt = enemy.emodel.GetHeadTransform().position;
                            if (lookAt == Vector3.zero) continue;
                            
                            Vector3 w2s = Camera.main.WorldToScreenPoint(lookAt);
                            if (float.IsNaN(w2s.x) || float.IsNaN(w2s.y)) continue;

                            // If they're outside of our FOV.
                            if (Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(w2s.x, w2s.y)) > 150f)
                                continue;

                            if (IsOnScreen(w2s))
                            {
                                float distance = Math.Abs(Vector2.Distance(new Vector2(w2s.x, Screen.height - w2s.y), new Vector2(Screen.width / 2, Screen.height / 2)));

                                if (distance<minDist)
                                {
                                    minDist = distance;
                                    target = new Vector2(w2s.x, Screen.height - w2s.y);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.Log($"[Aimbot] Enemy targeting error: {ex.Message}");
                            continue;
                        }
                    }
                }
            }




            if (target != Vector2.zero)
            {
                // Enhanced mouse event logic from backup
                try
                {
                    double distX = target.x - Screen.width / 2f;
                    double distY = target.y - Screen.height / 2f;

                    // Apply aim smoothing if enabled
                    if (UI.t_AIM && UI.t_AAIM)
                    {
                        // Smooth aiming based on configured smoothness
                        float smoothFactor = UI.t_AimSmooth;
                        
                        // Different smoothing for different weapon types
                        if (IsBowWeapon())
                        {
                            // Less smoothing for bows (need precision)
                            distX /= smoothFactor * 0.5f;
                            distY /= smoothFactor * 0.5f;
                        }
                        else if (IsRangedWeapon())
                        {
                            // Standard smoothing for rifles/pistols
                            distX /= smoothFactor;
                            distY /= smoothFactor;
                        }
                        else
                        {
                            // More smoothing for other weapons
                            distX /= smoothFactor * 1.5f;
                            distY /= smoothFactor * 1.5f;
                        }
                    }
                    else
                    {
                        // No smoothing - instant aim
                        distX /= 1f;
                        distY /= 1f;
                    }

                    // Apply mouse movement with clamping to prevent excessive movement
                    int maxMove = 100; // Maximum pixels to move per frame
                    int moveX = (int)Math.Max(-maxMove, Math.Min(maxMove, distX));
                    int moveY = (int)Math.Max(-maxMove, Math.Min(maxMove, distY));

                    mouse_event(0x0001, moveX, moveY, 0, 0);
                    
                    // Update last target for tracking
                    lastAimTarget = target;
                    hasTarget = true;
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"[Aimbot] Mouse event error: {ex.Message}");
                    hasTarget = false;
                }
            }
            else
            {
                hasTarget = false;
            }

        }

        public static bool IsOnScreen(Vector3 position)
        {
            return position.y > Config.MIN_SCREEN_POSITION && position.y < Screen.height - Config.SCREEN_EDGE_MARGIN && position.z > Config.MIN_SCREEN_POSITION;
        }

        /// <summary>
        /// Check if current weapon is a bow type
        /// </summary>
        private static bool IsBowWeapon()
        {
            try
            {
                // Check current weapon type using reflection
                var player = GameManager.Instance.World.GetPrimaryPlayer();
                if (player != null && player.inventory != null)
                {
                    var item = player.inventory.GetItem();
                    if (item != null)
                    {
                        string itemName = item.ItemClass.Name.ToLower();
                        return itemName.Contains("bow") || itemName.Contains("crossbow");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log($"[Aimbot] Weapon detection error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Check if current weapon is a ranged weapon (rifle, pistol, etc.)
        /// </summary>
        private static bool IsRangedWeapon()
        {
            try
            {
                var player = GameManager.Instance.World.GetPrimaryPlayer();
                if (player != null && player.inventory != null)
                {
                    var item = player.inventory.GetItem();
                    if (item != null)
                    {
                        string itemName = item.ItemClass.Name.ToLower();
                        return itemName.Contains("rifle") || itemName.Contains("pistol") || 
                               itemName.Contains("smg") || itemName.Contains("shotgun") ||
                               itemName.Contains("gun") || itemName.Contains("sniper");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log($"[Aimbot] Weapon detection error: {ex.Message}");
            }
            return false;
        }

    }
}
