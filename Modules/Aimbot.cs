using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
                // DIRECT CAMERA ROTATION - Bypass DirectInput limitations
                // Convert screen coordinates to world direction for perfect precision
                
                try
                {
                    // Convert screen target to world ray
                    Ray cameraRay = Camera.main.ScreenPointToRay(new Vector3(target.x, target.y, 0));
                    
                    // Calculate direction to target
                    Vector3 targetDirection = cameraRay.direction.normalized;
                    
                    // Create rotation to look at target
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    
                    // Apply rotation directly to camera (instant, perfect precision)
                    Camera.main.transform.rotation = targetRotation;
                    
                    Debug.Log($"[Aimbot] Direct camera rotation applied - Perfect precision achieved!");
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"[Aimbot] Camera rotation error: {ex.Message}");
                    
                    // Fallback to mouse_event if camera rotation fails
                    double distX = target.x - Screen.width / 2f;
                    double distY = target.y - Screen.height / 2f;
                    mouse_event(0x0001, (int)distX, (int)distY, 0, 0);
                }
            }

        }

        public static bool IsOnScreen(Vector3 position)
        {
            return position.y > Config.MIN_SCREEN_POSITION && position.y < Screen.height - Config.SCREEN_EDGE_MARGIN && position.z > Config.MIN_SCREEN_POSITION;
        }

    }
}
