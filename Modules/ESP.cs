using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game_7D2D.Modules
{
    class ESP
    {
        private static Camera Camera;
        private static Vector3 eb_head, eb_neck, eb_spine, eb_leftshoulder, eb_leftarm, eb_leftforearm, eb_lefthand, eb_rightshoulder, eb_rightarm, eb_rightforearm;
        private static Vector3 eb_righthand, eb_hips, eb_leftupleg, eb_leftleg, eb_leftfoot, eb_rightupleg, eb_rightleg, eb_rightfoot;
        
        // FOV-Aware ESP Configuration
        public static float fovThreshold = 120f; // 120 degrees FOV threshold
        public static bool fovAwareEnabled = true; // Enable/disable FOV-aware rendering
        
        /// <summary>
        /// Checks if an entity is within the player's field of view.
        /// Uses Vector3.Angle to calculate the angle between player's forward vector and direction to target.
        /// </summary>
        /// <param name="targetPosition">World position of the target entity</param>
        /// <returns>True if target is within FOV threshold, false otherwise</returns>
        public static bool IsEntityInFOV(Vector3 targetPosition)
        {
            if (!fovAwareEnabled || Hacks.eLocalPlayer == null || Camera.main == null)
                return true; // Default to visible if FOV-aware is disabled or components missing
            
            try
            {
                // Get player's forward direction
                Vector3 playerForward = Hacks.eLocalPlayer.transform.forward;
                
                // Calculate direction from player to target
                Vector3 directionToTarget = (targetPosition - Hacks.eLocalPlayer.transform.position).normalized;
                
                // Calculate angle between player's forward vector and direction to target
                float angle = Vector3.Angle(playerForward, directionToTarget);
                
                // Return true if angle is within threshold (half of FOV threshold since we check from center)
                return angle <= fovThreshold / 2f;
            }
            catch (System.Exception)
            {
                return true; // Default to visible on error
            }
        }
        public static void esp_drawBox(EntityEnemy entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            if (Hacks.eLocalPlayer == null || Hacks.eLocalPlayer.transform == null) return;
            const float maxDist = 100f;
            float distSqr = (entity.transform.position - Hacks.eLocalPlayer.transform.position).sqrMagnitude;
            if (distSqr > maxDist * maxDist) return;

            // FOV-Aware ESP: Check if entity is within player's field of view
            if (!IsEntityInFOV(entity_head))
                return; // Skip rendering if outside FOV

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);

            float Distance = Mathf.Sqrt(distSqr);
            Vector3 w2s_test = Hacks.W2S(entity.emodel.GetHeadTransform().position);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0 && Distance <= maxDist)
            {
                if (UI.t_ESPBoxes)
                {
                    DrawESPBox(w2s_feet, w2s_head, color, entity.EntityName);
                    DrawESPBox(w2s_test, new Vector3(w2s_test.x - 1f, w2s_test.y - 1f, w2s_test.z), Color.green, "");
                }


                if (UI.t_ESPLines)
                {
                    Render.DrawLine(new Vector2(w2s_head.x, (float)Screen.height - w2s_head.y), new Vector2((float)Screen.width / 2, (float)Screen.height - 100), color, 1f);
                }

                if (UI.t_EnemyBones)
                {
                    // Only compute bones if head is on-screen (saves many W2S calls when off-screen)
                    if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0)
                    {
                        try
                        {
                            var smr = entity.GetComponentInChildren<SkinnedMeshRenderer>();
                            if (smr == null || smr.bones == null) return;
                            
                            Transform[] entityBones = smr.bones;
                            int canBone = 0;

                            // Reset bone positions
                            eb_head = eb_neck = eb_spine = Vector3.zero;
                            eb_leftshoulder = eb_leftarm = eb_leftforearm = eb_lefthand = Vector3.zero;
                            eb_rightshoulder = eb_rightarm = eb_rightforearm = eb_righthand = Vector3.zero;
                            eb_hips = eb_leftupleg = eb_leftleg = eb_leftfoot = Vector3.zero;
                            eb_rightupleg = eb_rightleg = eb_rightfoot = Vector3.zero;

                            for (int j = 0; j < entityBones.Length; j++)
                            {
                                if (entityBones[j] == null) continue;
                                var bonePos = entityBones[j].position;
                                var boneName = entityBones[j].name.ToLower();
                                
                                // Use more flexible bone name matching
                                if (boneName.Contains("head")) { eb_head = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("neck")) { eb_neck = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("spine") || boneName.Contains("chest")) { eb_spine = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftshoulder") || boneName.Contains("l_shoulder")) { eb_leftshoulder = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftarm") || boneName.Contains("l_arm")) { eb_leftarm = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftforearm") || boneName.Contains("l_forearm")) { eb_leftforearm = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("lefthand") || boneName.Contains("l_hand")) { eb_lefthand = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightshoulder") || boneName.Contains("r_shoulder")) { eb_rightshoulder = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightarm") || boneName.Contains("r_arm")) { eb_rightarm = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightforearm") || boneName.Contains("r_forearm")) { eb_rightforearm = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("righthand") || boneName.Contains("r_hand")) { eb_righthand = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("hips") || boneName.Contains("pelvis")) { eb_hips = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftupleg") || boneName.Contains("l_thigh")) { eb_leftupleg = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftleg") || boneName.Contains("l_leg")) { eb_leftleg = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("leftfoot") || boneName.Contains("l_foot")) { eb_leftfoot = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightupleg") || boneName.Contains("r_thigh")) { eb_rightupleg = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightleg") || boneName.Contains("r_leg")) { eb_rightleg = Hacks.W2S(bonePos); canBone++; }
                                else if (boneName.Contains("rightfoot") || boneName.Contains("r_foot")) { eb_rightfoot = Hacks.W2S(bonePos); canBone++; }
                            }

                            // Draw skeleton with minimum required bones (more flexible threshold)
                            if (canBone >= 8) // Reduced from 18 to 8 for better compatibility
                            {
                                // Spine
                                if (eb_head != Vector3.zero && eb_neck != Vector3.zero) DrawESPLine(eb_head, eb_neck, Color.green);
                                if (eb_neck != Vector3.zero && eb_spine != Vector3.zero) DrawESPLine(eb_neck, eb_spine, Color.green);
                                if (eb_spine != Vector3.zero && eb_hips != Vector3.zero) DrawESPLine(eb_spine, eb_hips, Color.green);

                                // Left arm
                                if (eb_neck != Vector3.zero && eb_leftshoulder != Vector3.zero) DrawESPLine(eb_neck, eb_leftshoulder, Color.green);
                                if (eb_leftshoulder != Vector3.zero && eb_leftarm != Vector3.zero) DrawESPLine(eb_leftshoulder, eb_leftarm, Color.green);
                                if (eb_leftarm != Vector3.zero && eb_leftforearm != Vector3.zero) DrawESPLine(eb_leftarm, eb_leftforearm, Color.green);
                                if (eb_leftforearm != Vector3.zero && eb_lefthand != Vector3.zero) DrawESPLine(eb_leftforearm, eb_lefthand, Color.green);

                                // Right arm
                                if (eb_neck != Vector3.zero && eb_rightshoulder != Vector3.zero) DrawESPLine(eb_neck, eb_rightshoulder, Color.green);
                                if (eb_rightshoulder != Vector3.zero && eb_rightarm != Vector3.zero) DrawESPLine(eb_rightshoulder, eb_rightarm, Color.green);
                                if (eb_rightarm != Vector3.zero && eb_rightforearm != Vector3.zero) DrawESPLine(eb_rightarm, eb_rightforearm, Color.green);
                                if (eb_rightforearm != Vector3.zero && eb_righthand != Vector3.zero) DrawESPLine(eb_rightforearm, eb_righthand, Color.green);

                                // Left leg
                                if (eb_hips != Vector3.zero && eb_leftupleg != Vector3.zero) DrawESPLine(eb_hips, eb_leftupleg, Color.green);
                                if (eb_leftupleg != Vector3.zero && eb_leftleg != Vector3.zero) DrawESPLine(eb_leftupleg, eb_leftleg, Color.green);
                                if (eb_leftleg != Vector3.zero && eb_leftfoot != Vector3.zero) DrawESPLine(eb_leftleg, eb_leftfoot, Color.green);

                                // Right leg
                                if (eb_hips != Vector3.zero && eb_rightupleg != Vector3.zero) DrawESPLine(eb_hips, eb_rightupleg, Color.green);
                                if (eb_rightupleg != Vector3.zero && eb_rightleg != Vector3.zero) DrawESPLine(eb_rightupleg, eb_rightleg, Color.green);
                                if (eb_rightleg != Vector3.zero && eb_rightfoot != Vector3.zero) DrawESPLine(eb_rightleg, eb_rightfoot, Color.green);
                            }
                        }
                        catch (System.Exception)
                        {
                            // Silently handle bone ESP errors to avoid crashes
                        }
                    }
                }
                
            }
            
        }
        public static void esp_drawBox(EntityItem entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            if (Hacks.eLocalPlayer == null || Hacks.eLocalPlayer.transform == null) return;
            const float maxDist = 100f;
            float distSqr = (entity.transform.position - Hacks.eLocalPlayer.transform.position).sqrMagnitude;
            if (distSqr > maxDist * maxDist) return;

            // FOV-Aware ESP: Check if item is within player's field of view
            if (!IsEntityInFOV(entity_head))
                return; // Skip rendering if outside FOV

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);

            float Distance = Mathf.Sqrt(distSqr);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0 && Distance <= maxDist)
            {
                DrawESPBox(w2s_feet, w2s_head, color, entity.name);
            }
        }

        public static void esp_drawBox(EntitySupplyCrate entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            if (Hacks.eLocalPlayer == null || Hacks.eLocalPlayer.transform == null) return;
            const float maxDist = 100f;
            float distSqr = (entity.transform.position - Hacks.eLocalPlayer.transform.position).sqrMagnitude;
            if (distSqr > maxDist * maxDist) return;

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);

            float Distance = Mathf.Sqrt(distSqr);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0 && Distance <= maxDist)
            {
                DrawESPText(w2s_feet, w2s_head, color, entity.name);
            }
        }
        public static void esp_drawBox(EntityNPC entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);

            float Distance = Vector3.Distance(entity.transform.position, Hacks.eLocalPlayer.transform.position);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0)
            {
                if (UI.t_ESPBoxes)
                {
                    DrawESPBox(w2s_feet, w2s_head, color, entity.EntityName);
                }

                if (UI.t_ESPLines)
                {
                    Render.DrawLine(new Vector2(w2s_head.x, (float)Screen.height - w2s_head.y), new Vector2((float)Screen.width / 2, (float)Screen.height - 100), color, 1f);
                }
            }
        }
        public static void esp_drawBox(EntityPlayer entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            if (Hacks.eLocalPlayer == null || Hacks.eLocalPlayer.transform == null) return;
            const float maxDist = 100f;
            float distSqr = (entity.transform.position - Hacks.eLocalPlayer.transform.position).sqrMagnitude;
            if (distSqr > maxDist * maxDist) return;

            // FOV-Aware ESP: Check if player is within player's field of view
            if (!IsEntityInFOV(entity_head))
                return; // Skip rendering if outside FOV

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);
            Vector3 w2s_test = Hacks.W2S(entity.emodel.GetHeadTransform().position);

            float Distance = Mathf.Sqrt(distSqr);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0)
            {
                if (UI.t_ESPBoxes)
                {
                    if (entity != Hacks.eLocalPlayer)
                    {
                        if (entity.IsGodMode == true)
                        {
                            DrawESPBox(w2s_feet, w2s_head, color, $"{entity.EntityName} [GOD]");
                        }
                        else
                        {
                            DrawESPBox(w2s_feet, w2s_head, color, entity.EntityName);
                        }
                    }

                    DrawESPBox(w2s_test, new Vector3(w2s_test.x - 1f, w2s_test.y - 1f, w2s_test.z), Color.green, "");
                }

                if (UI.t_ESPLines)
                {
                    if (entity != Hacks.eLocalPlayer)
                    {
                        Render.DrawLine(new Vector2(w2s_head.x, (float)Screen.height - w2s_head.y), new Vector2((float)Screen.width / 2, (float)Screen.height - 100), color, 1f);
                    }
                }

            }
        }
        public static void esp_drawBox(EntityAnimal entity, Color color)
        {
            Vector3 entity_head = entity.transform.position;
            Vector3 entity_feet = new Vector3(entity_head.x, entity_head.y + entity.height, entity_head.z);

            if (Hacks.eLocalPlayer == null || Hacks.eLocalPlayer.transform == null) return;
            const float maxDist = 100f;
            float distSqr = (entity.transform.position - Hacks.eLocalPlayer.transform.position).sqrMagnitude;
            if (distSqr > maxDist * maxDist) return;

            // FOV-Aware ESP: Check if animal is within player's field of view
            if (!IsEntityInFOV(entity_head))
                return; // Skip rendering if outside FOV

            Camera = Hacks.MainCamera;
            Vector3 w2s_head = Hacks.W2S(entity_head);
            Vector3 w2s_feet = Hacks.W2S(entity_feet);
            Vector3 w2s_test = Hacks.W2S(entity.emodel.GetHeadTransform().position);

            float Distance = Mathf.Sqrt(distSqr);

            if (w2s_head.z > 0f && w2s_head.x > 0 && w2s_head.x < (float)Screen.width && w2s_head.y > 0 && Distance <= maxDist)
            {
                if (UI.t_ESPBoxes)
                {
                    DrawESPBox(w2s_feet, w2s_head, color, entity.EntityName.Replace("animal", ""));
                    DrawESPBox(w2s_test, new Vector3(w2s_test.x - 1f, w2s_test.y - 1f, w2s_test.z), Color.green, "");
                }
                if (UI.t_ESPLines)
                {
                    Render.DrawLine(new Vector2(w2s_head.x, (float)Screen.height - w2s_head.y), new Vector2((float)Screen.width / 2, (float)Screen.height - 100), color, 1f);
                }
            }
        }


        /// <summary>
        /// Draw an ESP line using object pooling for performance.
        /// </summary>
        private static void DrawESPLine(Vector3 start, Vector3 end, Color color)
        {
            var lineData = ESPPool.GetLine();
            lineData.Set(
                new Vector2(start.x, Screen.height - start.y),
                new Vector2(end.x, Screen.height - end.y),
                color,
                1f
            );
            
            // Render the line using the pooled data
            Render.DrawLine(lineData.start, lineData.end, lineData.color, lineData.thickness);
            
            // Return to pool for reuse
            ESPPool.ReturnLine(lineData);
            ESPPool.IncrementLineCount();
        }
        
        /// <summary>
        /// Draw an ESP box using object pooling for performance.
        /// </summary>
        private static void DrawESPBox(Vector3 objfootPos, Vector3 objheadPos, Color objColor, String name)
        {
            var boxData = ESPPool.GetBox();
            
            //Draw Basic ESP Method from Vector3 W2S input
            float height = objheadPos.y - objfootPos.y;
            float widthOffset = 2f;
            float width = height / widthOffset;

            boxData.Set(
                objfootPos.x - (width / 2),
                (float)Screen.height - objfootPos.y - height,
                width,
                height,
                objColor,
                2f,
                name
            );

            // Render the box using the pooled data
            Render.DrawBox(boxData.x, boxData.y, boxData.width, boxData.height, boxData.color, boxData.thickness);
            
            if (!string.IsNullOrEmpty(boxData.text))
            {
                var textData = ESPPool.GetText();
                textData.Set(
                    new Vector2(boxData.x, boxData.y),
                    boxData.text,
                    Color.white
                );
                Render.DrawString(textData.position, textData.text);
                ESPPool.ReturnText(textData);
                ESPPool.IncrementTextCount();
            }
            
            // Return to pool for reuse
            ESPPool.ReturnBox(boxData);
            ESPPool.IncrementBoxCount();
        }
        
        /// <summary>
        /// Draw ESP text using object pooling for performance.
        /// </summary>
        private static void DrawESPText(Vector3 objfootPos, Vector3 objheadPos, Color objColor, String name)
        {
            var textData = ESPPool.GetText();
            textData.Set(
                new Vector2(objfootPos.x, (float)Screen.height - objfootPos.y),
                name,
                objColor
            );
            
            Render.DrawString(textData.position, textData.text);
            
            ESPPool.ReturnText(textData);
            ESPPool.IncrementTextCount();
        }

        /// <summary>
        /// Reset performance counters at the start of each frame.
        /// </summary>
        public static void ResetFrameCounters()
        {
            ESPPool.ResetFrameCounters();
        }

    }
}
