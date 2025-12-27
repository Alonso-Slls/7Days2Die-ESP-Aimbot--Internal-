using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Game_7D2D.Modules; 

namespace Game_7D2D
{
    class Hacks : MonoBehaviour
    {
        public static Camera MainCamera = null;
        public static float Timer = 0f;

        //Entities
        public static List<EntityZombie> eZombie = new List<EntityZombie>();
        public static List<EntityEnemy> eEnemy = new List<EntityEnemy>();
        public static List<EntityItem> eItem = new List<EntityItem>();
        public static List<EntityNPC> eNPC = new List<EntityNPC>();
        public static List<EntityPlayer> ePlayers = new List<EntityPlayer>();
        public static List<EntityAnimal> eAnimal = new List<EntityAnimal>();
        public static List<EntitySupplyCrate> eLoot = new List<EntitySupplyCrate>();
        public static LocalPlayer localP;
        public static EntityPlayerLocal eLocalPlayer;
        public static Coroutine coro;
        private static int scanStep = 0; 
        public static int W2SCount = 0;
        public static Stopwatch scanStopwatch = new Stopwatch();
        public static long lastScanMs = 0; 
        // Reduce frequency of expensive LocalPlayer lookups to avoid repeated hitches
        private static float lastLocalSearchTime = 0f;
        private const float localSearchInterval = 5f; // seconds between local player lookup attempts
        // Enemy scan throttling to avoid periodic hitches (full FindObjectsOfType can be expensive)
        private static float lastEnemyScanTime = 0f;
        private const float enemyScanInterval = 2.0f; // seconds between full enemy scans (tuned)
        // Incremental pruning budget to spread list maintenance across frames (lower default)
        private static int pruneBudgetPerTick = 2; // remove up to N invalid entries per cycle
        private static int enemyPruneIndex = 0;
        // Buffer & incremental full-scan fields for enemies
        private static EntityEnemy[] enemyRefreshBuffer = null;
        private static int enemyRefreshPos = 0;
        private static List<EntityEnemy> enemyRefreshTemp = null;
        private static bool enemyRefreshActive = false;
        // Animal scan throttling and incremental pruning
        private static float lastAnimalScanTime = 0f;
        private const float animalScanInterval = 2.0f; // seconds between full animal scans (tuned)
        private static int animalPruneIndex = 0;
        // Buffer & incremental full-scan fields for animals
        private static EntityAnimal[] animalRefreshBuffer = null;
        private static int animalRefreshPos = 0;
        private static List<EntityAnimal> animalRefreshTemp = null;
        private static bool animalRefreshActive = false;
        // Chunk size when processing refresh buffers (how many entries to process per tick)
        private const int refreshChunkSize = 20;

        //Menu Variables
        public static bool Menu = true;

        public static bool isLoaded = GameManager.Instance.gameStateManager.IsGameStarted();

        public void Start()
        {
            // Initialize all systems
            ErrorHandler.Initialize();
            Config.Initialize();
            EntitySubscription.Initialize();
            BatchedRenderer.Initialize();
            
            // Initialize entity trackers
            EntityTracker<EntityEnemy>.Instance.ScanInterval = Config.ENEMY_SCAN_INTERVAL;
            EntityTracker<EntityAnimal>.Instance.ScanInterval = Config.ANIMAL_SCAN_INTERVAL;
            EntityTracker<EntityPlayer>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            EntityTracker<EntityItem>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            EntityTracker<EntityNPC>.Instance.ScanInterval = Config.ENTITY_SCAN_INTERVAL;
            
            // Create EntityManager component
            if (EntityManager.Instance == null)
            {
                var entityManagerGO = new GameObject("EntityManager");
                entityManagerGO.AddComponent<EntityManager>();
            }
            
            // Start entity tracking
            EntitySubscription.RescanAndSubscribeExistingEntities();
            
            coro = StartCoroutine(updateObjects());
            MainCamera = Camera.main;
            Modules.UI.dbg = "loaded";
            
            ErrorHandler.LogInfo("Hacks", "All systems initialized successfully");
        }
        
        public void stopCoro()
        {
            if (coro != null)
            {
                StopCoroutine(coro);
                coro = null;
            }
        }

        public void Update()
        {
            // reset per-frame counters
            W2SCount = 0;

            if (isLoaded)
            {
                Modules.Hotkeys.hotkeys();
                Timer += Time.deltaTime; 

                // Performance Optimization: Only update entities when ESP features are active
                bool espFeaturesActive = Config.Settings.EnemyESP || Config.Settings.PlayerESP || 
                                       Config.Settings.ItemESP || Config.Settings.AnimalESP || 
                                       Config.Settings.NPCESP || Config.Settings.AimbotEnabled;
                
                // Update entity trackers
                if (espFeaturesActive)
                {
                    EntityTracker<EntityEnemy>.Instance.Update();
                    EntityTracker<EntityAnimal>.Instance.Update();
                    EntityTracker<EntityPlayer>.Instance.Update();
                    EntityTracker<EntityItem>.Instance.Update();
                    EntityTracker<EntityNPC>.Instance.Update();
                }
                
                // Use subscription model instead of periodic scans
                if (Timer >= Config.ENTITY_SCAN_INTERVAL && espFeaturesActive)
                {
                    Timer = 0f;
                    // Cleanup invalid entities periodically
                    EntitySubscription.CleanupInvalidEntities();
                    
                    // Cleanup entity trackers
                    EntityTracker<EntityEnemy>.Instance.CleanupInvalidEntities();
                    EntityTracker<EntityAnimal>.Instance.CleanupInvalidEntities();
                    EntityTracker<EntityPlayer>.Instance.CleanupInvalidEntities();
                    EntityTracker<EntityItem>.Instance.CleanupInvalidEntities();
                    EntityTracker<EntityNPC>.Instance.CleanupInvalidEntities();
                    
                    // Rescan for any missed entities (fallback)
                    if (UnityEngine.Random.Range(0, 10) == 0) // 10% chance per interval
                    {
                        EntitySubscription.RescanAndSubscribeExistingEntities();
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    GameStats.Set(EnumGameStats.IsCreativeMenuEnabled, true);
                }

                
            }
            
            checkState();
        }

        /// <summary>
        /// Render ESP using the subscription model for better performance.
        /// </summary>
        private void RenderESPWithSubscription()
        {
            try
            {
                // Render Enemy ESP
                if (Config.Settings.EnemyESP)
                {
                    var enemies = EntitySubscription.GetSubscribedEntities<EntityEnemy>();
                    foreach (var enemy in enemies)
                    {
                        if (enemy != null && enemy.IsAlive())
                        {
                            Modules.ESP.esp_drawBox(enemy, Config.Settings.EnemyColor);
                        }
                    }
                }

                // Render Item ESP
                if (Config.Settings.ItemESP)
                {
                    var items = EntitySubscription.GetSubscribedEntities<EntityItem>();
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            Modules.ESP.esp_drawBox(item, Config.Settings.ItemColor);
                        }
                    }
                }

                // Render NPC ESP
                if (Config.Settings.NPCESP)
                {
                    var npcs = EntitySubscription.GetSubscribedEntities<EntityNPC>();
                    foreach (var npc in npcs)
                    {
                        if (npc != null && npc.IsAlive() && npc.IsSpawned())
                        {
                            Modules.ESP.esp_drawBox(npc, Config.Settings.NPCColor);
                        }
                    }
                }

                // Render Player ESP
                if (Config.Settings.PlayerESP)
                {
                    var players = EntitySubscription.GetSubscribedEntities<EntityPlayer>();
                    foreach (var player in players)
                    {
                        if (player != null && player.IsAlive() && player.IsSpawned())
                        {
                            Modules.ESP.esp_drawBox(player, Config.Settings.PlayerColor);
                        }
                    }
                }

                // Render Animal ESP
                if (Config.Settings.AnimalESP)
                {
                    var animals = EntitySubscription.GetSubscribedEntities<EntityAnimal>();
                    foreach (var animal in animals)
                    {
                        if (animal != null && animal.IsAlive())
                        {
                            Modules.ESP.esp_drawBox(animal, Config.Settings.AnimalColor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Hacks.RenderESPWithSubscription", $"ESP rendering error: {ex.Message}");
            }
        }

        public void OnGUI()
        {
            if (!isLoaded)
            {
                GUI.Box(new Rect(5f, 5f, 250f, 35f), "");
                GUI.Label(new Rect(10f, 5f, 250f, 30f), "Menu will load when in a game");
                return;
            }

            Modules.UI.DrawMenu();

            // Draw persistent debug overlay when enabled (shows auto-checks, weapon speed, last exception)
            try
            {
                if (Config.Settings.DebugOverlay && Hacks.isLoaded)
                {
                    float bx = Screen.width - 260f;
                    float by = 5f;
                    BatchedRenderer.AddBox(bx, by, 250f, 70f, Color.white, 1f);
                    
                    string dbg = Modules.UI.dbg ?? "";
                    string winfo = "N/A";
                    float wspeed = 0f;
                    string ex = "N/A";
                    if (!string.IsNullOrEmpty(ex))
                    {
                        int idx = ex.IndexOf('\n');
                        if (idx >= 0) ex = ex.Substring(0, idx);
                    }
                    
                    BatchedRenderer.AddText(new Vector2(bx + 8f, by + 6f), $"DBG: {dbg}", Color.white);
                    BatchedRenderer.AddText(new Vector2(bx + 8f, by + 24f), $"Weapon: {winfo} speed:{wspeed:0.0}", Color.white);
                    BatchedRenderer.AddText(new Vector2(bx + 8f, by + 42f), $"LastErr: {ex}", Color.white);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Hacks.OnGUI", $"Debug overlay error: {ex.Message}");
            }

            // Draw FOV circle using batched rendering
            if (Config.Settings.AimbotEnabled && Config.Settings.ShowFOVCircle)
            {
                Vector2 center = new Vector2((float)Screen.width / 2, (float)Screen.height / 2);
                BatchedRenderer.AddCircle(center, Config.Settings.AimFOV, Color.green, 64);
            }

            // Render ESP using the dedicated ESP renderer
            ESPRenderer.Instance.RenderAllESP();

            // Execute all batched render operations
            BatchedRenderer.RenderBatches();
        }

        private void checkState()
        {
            if (isLoaded != GameManager.Instance.gameStateManager.IsGameStarted())
            {
                isLoaded = !isLoaded;
            }
        }

        
        public IEnumerator updateObjects()
        {
            while (true)
            {
                scanStopwatch.Restart();

                // helper to count W2S calls if needed from other modules (keeps instrumentation centralized)
                // Usage: Hacks.W2S(position)
                // Implemented here to avoid duplicate counting boilerplate in ESP/Aimbot
                // (Will be JIT inlined by the compiler)
                
                // track whether we performed an expensive local-player search during this step
                bool didLocalSearch = false;
                bool caseHeavy = false;
                long caseMs = 0;
                var caseStopwatch = System.Diagnostics.Stopwatch.StartNew();

                switch (scanStep)
                {
                    case 0:
                        if (Modules.UI.t_EnemyESP)
                        {
                            // If we need a full refresh and we're not already processing one, capture the buffer and process it progressively.
                            if (!enemyRefreshActive && (UnityEngine.Time.realtimeSinceStartup - lastEnemyScanTime) > enemyScanInterval)
                            {
                                enemyRefreshBuffer = UnityEngine.GameObject.FindObjectsOfType<EntityEnemy>();
                                enemyRefreshPos = 0;
                                enemyRefreshTemp = new List<EntityEnemy>(enemyRefreshBuffer.Length);
                                enemyRefreshActive = true;
                                lastEnemyScanTime = UnityEngine.Time.realtimeSinceStartup;
                            }

                            if (enemyRefreshActive)
                            {
                                // process a small chunk from the buffer each tick to avoid spikes
                                int toProcess = Math.Min(refreshChunkSize, enemyRefreshBuffer.Length - enemyRefreshPos);
                                for (int q = 0; q < toProcess; q++)
                                {
                                    var e = enemyRefreshBuffer[enemyRefreshPos++];
                                    if (e != null && e.IsAlive()) enemyRefreshTemp.Add(e);
                                }

                                if (enemyRefreshPos >= enemyRefreshBuffer.Length)
                                {
                                    // finished refreshing; swap lists atomically
                                    Hacks.eEnemy = enemyRefreshTemp;
                                    enemyRefreshBuffer = null;
                                    enemyRefreshTemp = null;
                                    enemyRefreshActive = false;
                                    enemyPruneIndex = 0;
                                }
                            }
                            else
                            {
                                // incremental prune up to pruneBudgetPerTick invalid entries starting from enemyPruneIndex
                                int removed = 0;
                                int scanned = 0;
                                int count = Hacks.eEnemy.Count;
                                while (removed < pruneBudgetPerTick && count > 0 && scanned < count)
                                {
                                    int idx = (enemyPruneIndex + scanned) % Hacks.eEnemy.Count;
                                    var e = Hacks.eEnemy[idx];
                                    if (e == null || !e.IsAlive())
                                    {
                                        Hacks.eEnemy.RemoveAt(idx);
                                        removed++;
                                        count--;
                                    }
                                    else
                                    {
                                        scanned++;
                                    }
                                }
                                if (Hacks.eEnemy.Count > 0)
                                    enemyPruneIndex = (enemyPruneIndex + scanned) % Hacks.eEnemy.Count;
                            }

                            Modules.UI.dbg = $"{Hacks.eEnemy.Count}";
                        }
                        break;
                    case 1:
                        // Items only (split from previous combined items+loot to reduce per-scan work)
                        if (Modules.UI.t_ItemESP)
                        {
                            Hacks.eItem.Clear();
                            var foundItems = UnityEngine.GameObject.FindObjectsOfType<EntityItem>();
                            for (int i = 0; i < foundItems.Length; i++) if (foundItems[i] != null) Hacks.eItem.Add(foundItems[i]);
                        }
                        break;
                    case 2:
                        // Loot (supply crates) only
                        if (Modules.UI.t_ItemESP)
                        {
                            Hacks.eLoot.Clear();
                            var foundLoot = UnityEngine.GameObject.FindObjectsOfType<EntitySupplyCrate>();
                            for (int i = 0; i < foundLoot.Length; i++) if (foundLoot[i] != null) Hacks.eLoot.Add(foundLoot[i]);
                        }
                        break;
                    case 3:
                        if (Modules.UI.t_NPCESP)
                        {
                            Hacks.eNPC.Clear();
                            var foundNPCs = UnityEngine.GameObject.FindObjectsOfType<EntityNPC>();
                            for (int i = 0; i < foundNPCs.Length; i++) if (foundNPCs[i] != null) Hacks.eNPC.Add(foundNPCs[i]);
                        }
                        break;
                    case 4:
                        if (Modules.UI.t_PlayerESP)
                        {
                            Hacks.ePlayers.Clear();
                            var foundPlayers = UnityEngine.GameObject.FindObjectsOfType<EntityPlayer>();
                            for (int i = 0; i < foundPlayers.Length; i++) if (foundPlayers[i] != null) Hacks.ePlayers.Add(foundPlayers[i]);
                        }
                        break;
                    case 5:
                        if (Modules.UI.t_AnimalESP)
                        {
                            // If we need a full refresh and not already processing one, capture buffer and process progressively.
                            if (!animalRefreshActive && (UnityEngine.Time.realtimeSinceStartup - lastAnimalScanTime) > animalScanInterval)
                            {
                                animalRefreshBuffer = UnityEngine.GameObject.FindObjectsOfType<EntityAnimal>();
                                animalRefreshPos = 0;
                                animalRefreshTemp = new List<EntityAnimal>(animalRefreshBuffer.Length);
                                animalRefreshActive = true;
                                lastAnimalScanTime = UnityEngine.Time.realtimeSinceStartup;
                            }

                            if (animalRefreshActive)
                            {
                                int toProcess = Math.Min(refreshChunkSize, animalRefreshBuffer.Length - animalRefreshPos);
                                for (int q = 0; q < toProcess; q++)
                                {
                                    var a = animalRefreshBuffer[animalRefreshPos++];
                                    if (a != null && a.IsAlive()) animalRefreshTemp.Add(a);
                                }
                                if (animalRefreshPos >= animalRefreshBuffer.Length)
                                {
                                    Hacks.eAnimal = animalRefreshTemp;
                                    animalRefreshBuffer = null;
                                    animalRefreshTemp = null;
                                    animalRefreshActive = false;
                                    animalPruneIndex = 0;
                                }
                            }
                            else
                            {
                                int removed = 0;
                                int scanned = 0;
                                int count = Hacks.eAnimal.Count;
                                while (removed < pruneBudgetPerTick && count > 0 && scanned < count)
                                {
                                    int idx = (animalPruneIndex + scanned) % Hacks.eAnimal.Count;
                                    var a = Hacks.eAnimal[idx];
                                    if (a == null || !a.IsAlive())
                                    {
                                        Hacks.eAnimal.RemoveAt(idx);
                                        removed++;
                                        count--;
                                    }
                                    else
                                    {
                                        scanned++;
                                    }
                                }
                                if (Hacks.eAnimal.Count > 0)
                                    animalPruneIndex = (animalPruneIndex + scanned) % Hacks.eAnimal.Count;
                            }
                        }
                        break;
                    case 6:
                        // Only poll for LocalPlayer / EntityPlayerLocal when we don't have a cached reference.
                        // Avoid repeated FindObjectOfType calls each cycle which can cause frame hitches.
                        if ((Hacks.localP == null || Hacks.eLocalPlayer == null) && (UnityEngine.Time.realtimeSinceStartup - lastLocalSearchTime) > localSearchInterval)
                        {
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            Hacks.localP = UnityEngine.GameObject.FindObjectOfType<LocalPlayer>();
                            Hacks.eLocalPlayer = UnityEngine.GameObject.FindObjectOfType<EntityPlayerLocal>();
                            sw.Stop();
                            if (sw.ElapsedMilliseconds > 10) // mark as heavy if lookup itself was slow
                                caseHeavy = true;
                            lastLocalSearchTime = UnityEngine.Time.realtimeSinceStartup;
                            didLocalSearch = true;
                        }
                        break;
                }

                scanStopwatch.Stop();
                lastScanMs = scanStopwatch.ElapsedMilliseconds;
                scanStopwatch.Stop();
                lastScanMs = scanStopwatch.ElapsedMilliseconds;
                caseStopwatch.Stop();
                caseMs = caseStopwatch.ElapsedMilliseconds;
                if (caseMs > 20) caseHeavy = true; // threshold for heavy case execution
                // report the step we just executed (makes it easier to correlate spikes)
                Modules.UI.dbg = $"scan{scanStep}:{lastScanMs}ms case:{caseMs}ms w2s:{W2SCount} s:{(didLocalSearch?1:0)} h:{(caseHeavy?1:0)}";
                scanStep = (scanStep + 1) % 7;
                yield return new WaitForSeconds(0.25f);
            }
        }

        public static Vector3 W2S(Vector3 pos)
        {
            W2SCount++;
            if (MainCamera == null) MainCamera = Camera.main;
            return MainCamera.WorldToScreenPoint(pos);
        }

    }

}

