using RoR2;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RoR2.Skills;

namespace CursorHP
{
    public class HealthRingManager
    {
        // UI components
        private Canvas healthRingCanvas;
        private Image healthRingImage;
        private Dictionary<string, Text> textElements = new Dictionary<string, Text>();
        
        // Configuration
        private CursorHPConfiguration config;
        private BepInEx.Logging.ManualLogSource logger;
        private RingTextureGenerator textureGenerator;
        
        // Track previous health to avoid unnecessary texture regeneration
        private float previousHealthPercentage = -1f;
        private float previousShieldPercentage = -1f;
        private float previousBarrierPercentage = -1f;
        private float previousPrimaryCooldownPct = -1f;
        private float previousSecondaryCooldownPct = -1f;
        private float previousUtilityCooldownPct = -1f;
        private float previousSpecialCooldownPct = -1f;
        private float previousEquipmentCooldownPct = -1f;
        private float updateTimer = 0f;
        private const float UPDATE_INTERVAL = 0.1f; // Only update 10 times per second instead of every frame
        
        // Track if the player is dead
        private bool isPlayerDead = false;
        
        // Track which text elements were used in the current update cycle
        private HashSet<string> usedTextElements = new HashSet<string>();
        
        public HealthRingManager(CursorHPConfiguration config, BepInEx.Logging.ManualLogSource logger)
        {
            this.config = config;
            this.logger = logger;
            this.textureGenerator = new RingTextureGenerator(config);
            
            // Subscribe to scene change events
            On.RoR2.Run.OnDestroy += OnRunEnd;
            
            // Subscribe to player death events
            On.RoR2.CharacterMaster.OnBodyDeath += OnPlayerDeath;
        }
        
        // Called when the run/game ends
        private void OnRunEnd(On.RoR2.Run.orig_OnDestroy orig, RoR2.Run self)
        {
            // Call original method first
            orig(self);
            
            // Disable UI when the run ends
            EnableUI(false);
            logger.LogInfo("Run ended, disabling health ring UI");
        }
        
        // Called when a player body dies
        private void OnPlayerDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, RoR2.CharacterMaster self, RoR2.CharacterBody body)
        {
            // Call original method first
            orig(self, body);
            
            // Check if this is the local player's body
            if (body == LocalUserManager.GetFirstLocalUser()?.cachedBody)
            {
                isPlayerDead = true;
                EnableUI(false);
                logger.LogInfo("Player died, disabling health ring UI");
            }
        }
        
        public void Initialize()
        {
            CreateHealthRingUI();
            
            // Initially disable the UI until player spawns
            if (healthRingCanvas != null)
            {
                healthRingCanvas.enabled = false;
                logger.LogInfo("Health ring UI initialized and disabled until player spawns");
            }
        }
        
        public void Cleanup()
        {
            if (healthRingCanvas != null)
            {
                Object.Destroy(healthRingCanvas.gameObject);
            }
            
            textElements.Clear();
            
            // Unsubscribe from events
            On.RoR2.Run.OnDestroy -= OnRunEnd;
            On.RoR2.CharacterMaster.OnBodyDeath -= OnPlayerDeath;
        }
        
        public void EnableUI(bool enable)
        {
            if (healthRingCanvas != null)
            {
                logger.LogInfo($"Setting health ring UI visibility to: {enable}");
                healthRingCanvas.enabled = enable;
                
                // Make sure all child objects are also enabled
                if (healthRingImage != null)
                {
                    healthRingImage.enabled = enable;
                }
                
                // If enabling, force an update immediately
                if (enable)
                {
                    // Reset the previous health tracking to force an update
                    previousHealthPercentage = -1f;
                    previousShieldPercentage = -1f;
                    previousBarrierPercentage = -1f;
                    previousPrimaryCooldownPct = -1f;
                    previousSecondaryCooldownPct = -1f;
                    previousUtilityCooldownPct = -1f;
                    previousSpecialCooldownPct = -1f;
                    previousEquipmentCooldownPct = -1f;
                    updateTimer = UPDATE_INTERVAL; // Set to threshold to trigger immediate update
                }
            }
            else
            {
                logger.LogError("Tried to enable/disable UI but canvas is null!");
            }
        }
        
        public void Update()
        {
            // If the canvas doesn't exist, we can't do anything
            if (healthRingCanvas == null) return;

            // Check if we're in a valid game state
            bool inGame = RoR2.Run.instance != null;
            if (!inGame)
            {
                if (healthRingCanvas.enabled)
                {
                    EnableUI(false);
                    logger.LogInfo("Not in game, disabling health ring UI");
                }
                return;
            }

            // Check for player existence
            bool hasPlayerBody = LocalUserManager.GetFirstLocalUser()?.cachedBody != null;
            
            // Handle pause menu differently - don't disable/enable rapidly as it causes flickering
            bool isPaused = RoR2.PauseManager.isPaused;
            
            // Handle cases where we should definitely disable the UI
            if (!hasPlayerBody || isPlayerDead)
            {
                if (healthRingCanvas.enabled)
                {
                    EnableUI(false);
                    logger.LogInfo($"Disabling UI - hasBody: {hasPlayerBody}, isDead: {isPlayerDead}");
                }
                return;
            }
            
            // Handle the case where we should enable the UI and we haven't yet
            if (!healthRingCanvas.enabled && hasPlayerBody && !isPlayerDead && (!isPaused || config.ShowDuringPause.Value))
            {
                EnableUI(true);
                // Force an immediate update
                updateTimer = UPDATE_INTERVAL;
                logger.LogInfo("Enabling health ring UI");
            }
            
            // If paused and we shouldn't show during pause, disable
            if (isPaused && !config.ShowDuringPause.Value && healthRingCanvas.enabled)
            {
                healthRingCanvas.enabled = false;
                logger.LogInfo("Game paused, hiding health ring UI");
                return;
            }
            
            // If the UI isn't enabled or we're paused, don't update
            if (!healthRingCanvas.enabled || (isPaused && !config.ShowDuringPause.Value)) return;

            // Only update the ring a few times per second, not every frame
            updateTimer += Time.deltaTime;
            if (updateTimer < UPDATE_INTERVAL) return;
            updateTimer = 0f;

            UpdateHealthRing();
        }
        
        public void Recreate()
        {
            // Save the current visibility state
            bool wasEnabled = healthRingCanvas != null ? healthRingCanvas.enabled : false;
            
            // Recreate the UI
            CreateHealthRingUI();
            
            // Restore the visibility state if it was previously enabled
            if (wasEnabled)
            {
                logger.LogInfo("Restoring health ring visibility after recreation");
                EnableUI(true);
                
                // Force an immediate update of the ring content
                if (LocalUserManager.GetFirstLocalUser()?.cachedBody != null)
                {
                    // Reset the previous health tracking to force an update
                    previousHealthPercentage = -1f;
                    previousShieldPercentage = -1f;
                    previousBarrierPercentage = -1f;
                    previousPrimaryCooldownPct = -1f;
                    previousSecondaryCooldownPct = -1f;
                    previousUtilityCooldownPct = -1f;
                    previousSpecialCooldownPct = -1f;
                    previousEquipmentCooldownPct = -1f;
                    
                    // Update immediately
                    UpdateHealthRing();
                }
            }
        }
        
        // Get the texture generator for direct access
        public RingTextureGenerator GetTextureGenerator()
        {
            return textureGenerator;
        }
        
        // Apply the current texture to the ring image
        public void ApplyTexture()
        {
            healthRingImage.sprite = textureGenerator.CreateSprite();
        }
        
        private void CreateHealthRingUI()
        {
            // Remove previous ring if it exists
            if (healthRingCanvas != null)
            {
                Object.Destroy(healthRingCanvas.gameObject);
            }
            
            textElements.Clear();
            
            // Create a canvas for our UI
            GameObject canvasObj = new GameObject("HealthRingCanvas");
            healthRingCanvas = canvasObj.AddComponent<Canvas>();
            healthRingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            healthRingCanvas.sortingOrder = 5; // Lower value to ensure it's drawn below menus
            
            // Add a CanvasScaler to properly handle different screen sizes
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height matching
            
            // Create a parent object for all UI elements with pivot at center
            GameObject uiContainer = new GameObject("UIContainer");
            uiContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Set the size based on the texture size to ensure proper scaling
            int textureSize = textureGenerator.GetTextureSize();
            containerRect.sizeDelta = new Vector2(textureSize, textureSize);
            containerRect.anchoredPosition = Vector2.zero;
            
            // Create the health ring GameObject
            GameObject healthRingObj = new GameObject("HealthRing");
            healthRingObj.transform.SetParent(uiContainer.transform, false);
            
            // Add and configure the Image component
            healthRingImage = healthRingObj.AddComponent<Image>();
            healthRingImage.preserveAspect = true;
            healthRingImage.raycastTarget = false; // Don't block mouse input
            
            // Initialize the texture generator
            textureGenerator.InitializeTexture();
            healthRingImage.sprite = textureGenerator.CreateSprite();
            
            // Position and size the ring to match the container
            RectTransform ringRect = healthRingObj.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(textureSize, textureSize);
            ringRect.anchoredPosition = Vector2.zero;
            
            // Make the UI persist between scenes
            Object.DontDestroyOnLoad(canvasObj);
            
            // Reset the previous health tracking
            previousHealthPercentage = -1f;
            previousShieldPercentage = -1f;
            previousBarrierPercentage = -1f;
            previousPrimaryCooldownPct = -1f;
            previousSecondaryCooldownPct = -1f;
            previousUtilityCooldownPct = -1f;
            previousSpecialCooldownPct = -1f;
            previousEquipmentCooldownPct = -1f;
            
            // Log that we created the UI
            logger.LogInfo($"Health Ring UI created successfully with texture size {textureSize}x{textureSize}");
            
            // Force an immediate draw with a simple test ring
            textureGenerator.ClearTexture();
            DrawRing(0, 0, 100, 10, 0, 360, Color.red, "DEBUG_RING");
            ApplyTexture();
        }
        
        // Create a text UI element
        private Text CreateTextElement(string name, Vector2 position, int fontSize, Color color)
        {
            if (healthRingCanvas == null) return null;
            
            // Get the parent container
            Transform parent = healthRingCanvas.transform.Find("UIContainer");
            if (parent == null)
            {
                logger.LogError("UIContainer not found when creating text element");
                return null;
            }
            
            // Create a new GameObject for the text with its own rect transform
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            // Add text component
            Text textComponent = textObj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            // Configure the RectTransform for accurate positioning
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot point
            rectTransform.sizeDelta = new Vector2(40, 30); // Small enough to fit in ring
            
            // Set position directly - no scaling needed
            rectTransform.anchoredPosition = position;
            
            // Store for later reference
            textElements[name] = textComponent;
            return textComponent;
        }
        
        // Draw left-aligned text
        public void DrawTextL(float x, float y, string text, float fontSize, Color color)
        {
            // Use only position for element key, not the text content
            string elementName = $"Text_L_{x}_{y}";
            Text textElement;
            
            if (!textElements.TryGetValue(elementName, out textElement))
            {
                textElement = CreateTextElement(elementName, new Vector2(x, y), (int)fontSize, color);
                textElement.alignment = TextAnchor.MiddleLeft;
            }
            
            if (textElement != null)
            {
                textElement.text = text;
                textElement.color = color;
                textElement.fontSize = (int)fontSize;
                textElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
                
                // Mark this element as used in this update cycle
                MarkTextElementUsed(elementName);
            }
        }
        
        // Draw center-aligned text
        public void DrawTextC(float x, float y, string text, float fontSize, Color color)
        {
            // Use only position for element key, not the text content
            string elementName = $"Text_C_{x}_{y}";
            Text textElement;
            
            if (!textElements.TryGetValue(elementName, out textElement))
            {
                textElement = CreateTextElement(elementName, new Vector2(x, y), (int)fontSize, color);
                textElement.alignment = TextAnchor.MiddleCenter;
            }
            
            if (textElement != null)
            {
                textElement.text = text;
                textElement.color = color;
                textElement.fontSize = (int)fontSize;
                
                // Set position directly - no need for scaling calculations
                RectTransform rt = textElement.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
                
                // Mark this element as used in this update cycle
                MarkTextElementUsed(elementName);
                
                // Log the position for debugging
                logger.LogInfo($"Text '{text}' positioned at ({x}, {y})");
            }
        }
        
        private void UpdateHealthRing()
        {
            // Get the local player's body
            if (LocalUserManager.GetFirstLocalUser()?.cachedBody == null) return;
            
            CharacterBody playerBody = LocalUserManager.GetFirstLocalUser().cachedBody;
            HealthComponent healthComponent = playerBody.healthComponent;

            // Get the player's skill locator component
            SkillLocator skillLocator = playerBody.skillLocator;

            
            
            // Get equipment cooldown information
            float equipmentCooldownPct = 0f;
            
            if (playerBody.inventory != null && playerBody.equipmentSlot != null)
            {
                // Get the player's current equipment index
                EquipmentIndex equipIndex = playerBody.inventory.GetEquipmentIndex();
                if (equipIndex != EquipmentIndex.None)
                {
                    // Get the equipment definition for name/info
                    EquipmentDef equipDef = EquipmentCatalog.GetEquipmentDef(equipIndex);
                    
                    // Calculate cooldown percentage
                    float cooldownRemaining = playerBody.equipmentSlot.cooldownTimer;
                    float cooldownTotal = equipDef?.cooldown ?? 1f;
                    
                    if (cooldownRemaining > 0f)
                    {
                        equipmentCooldownPct = cooldownRemaining / cooldownTotal;
                    }
                }
            }
            
            if (healthComponent != null)
            {
                // Calculate health percentage
                float healthPct = ColorUtility.Map(healthComponent.health, 0f, healthComponent.fullHealth, 0f, 1f);
                float shieldPct = healthComponent.shield > 0 ? ColorUtility.Map(healthComponent.shield, 0f, healthComponent.fullShield, 0f, 1f) : 0f;
                float barrierPct = healthComponent.barrier > 0 ? ColorUtility.Map(healthComponent.barrier, 0f, healthComponent.fullBarrier, 0f, 1f) : 0f;

                // Get cooldown info for each skill
                float primaryCooldown = skillLocator?.primary?.cooldownRemaining ?? 0f;
                float secondaryCooldown = skillLocator?.secondary?.cooldownRemaining ?? 0f;
                float utilityCooldown = skillLocator?.utility?.cooldownRemaining ?? 0f;
                float specialCooldown = skillLocator?.special?.cooldownRemaining ?? 0f;

                // Get total cooldown for each skill
                float primaryTotalCd = skillLocator?.primary?.baseRechargeInterval ?? 0f;
                float secondaryTotalCd = skillLocator?.secondary?.baseRechargeInterval ?? 0f;
                float utilityTotalCd = skillLocator?.utility?.baseRechargeInterval ?? 0f;
                float specialTotalCd = skillLocator?.special?.baseRechargeInterval ?? 0f;

                float primaryCooldownPct = primaryCooldown / primaryTotalCd;
                float secondaryCooldownPct = secondaryCooldown / secondaryTotalCd;
                float utilityCooldownPct = utilityCooldown / utilityTotalCd;
                float specialCooldownPct = specialCooldown / specialTotalCd;

                // Get stock information for each skill
                int primaryStocks = skillLocator?.primary?.stock ?? 0;
                int primaryMaxStocks = skillLocator?.primary?.maxStock ?? 0;

                int secondaryStocks = skillLocator?.secondary?.stock ?? 0;
                int secondaryMaxStocks = skillLocator?.secondary?.maxStock ?? 0;

                int utilityStocks = skillLocator?.utility?.stock ?? 0;
                int utilityMaxStocks = skillLocator?.utility?.maxStock ?? 0;

                int specialStocks = skillLocator?.special?.stock ?? 0;
                int specialMaxStocks = skillLocator?.special?.maxStock ?? 0;

                int equipmentStocks = playerBody.equipmentSlot?.stock ?? 0;
                int equipmentMaxStocks = playerBody.equipmentSlot?.maxStock ?? 0;
                
                // Only recreate the texture if something changed
                if (healthPct != previousHealthPercentage || 
                    shieldPct != previousShieldPercentage || 
                    barrierPct != previousBarrierPercentage ||
                    primaryCooldownPct != previousPrimaryCooldownPct ||
                    secondaryCooldownPct != previousSecondaryCooldownPct ||
                    utilityCooldownPct != previousUtilityCooldownPct ||
                    specialCooldownPct != previousSpecialCooldownPct ||
                    equipmentCooldownPct != previousEquipmentCooldownPct)
                {
                    // Update tracked values
                    previousHealthPercentage = healthPct;
                    previousShieldPercentage = shieldPct;
                    previousBarrierPercentage = barrierPct;
                    previousPrimaryCooldownPct = primaryCooldownPct;
                    previousSecondaryCooldownPct = secondaryCooldownPct;
                    previousUtilityCooldownPct = utilityCooldownPct;
                    previousSpecialCooldownPct = specialCooldownPct;
                    previousEquipmentCooldownPct = equipmentCooldownPct;
                    

                    // Clear the texture and redraw all rings
                    textureGenerator.ClearTexture();
                    
                    // Get base health color with full alpha
                    Color healthColor = ColorUtility.GetHealthColor(healthPct, 1.0f);

                    float ringRadius = config.RingSize.Value / 2f;
                    float ringThickness = config.RingThickness.Value;
                    float dimmer = 0.7f;

                    Color HealthColorBack = new Color(healthColor.r * dimmer, healthColor.g * dimmer, healthColor.b * dimmer, 0.2f);
                    DrawRing(0, 0, ringRadius, ringThickness, 0f, 360f, HealthColorBack, "HealthBG");
                    
                    // Draw health ring based on health percentage
                    float healthDegrees = healthPct * 360f;
                    Color healthColorMain = new Color(healthColor.r * dimmer, healthColor.g * dimmer, healthColor.b * dimmer, 0.8f);
                    DrawRing(0, 0, ringRadius, ringThickness, 0f, healthDegrees, healthColorMain, "Health");

                    DrawRing(0, 0, ringRadius - ringThickness/2, config.BorderInset.Value, 0f, 360f, healthColor, "HealthBorder");
                    DrawRing(0, 0, ringRadius + ringThickness/2, config.BorderInset.Value, 0f, 360f, healthColor, "HealthBorder");

                    
                    // Draw shield ring if player has shield
                    if (shieldPct > 0)
                    {
                        Color shieldColor = new Color(0.2f, 0.5f, 1f, 1.0f);
                        float shieldDegrees = shieldPct * 360f;
                        DrawRing(0, 0, ringRadius, 8, 0f, shieldDegrees, shieldColor, "Shield");
                    }
                    
                    // Draw barrier ring if player has barrier
                    if (barrierPct > 0)
                    {
                        Color barrierColor = new Color(1f, 0.7f, 0.4f, 1.0f);
                        float barrierDegrees = barrierPct * 360f;
                        DrawRing(0, 0, ringRadius, 6, 0f, barrierDegrees, barrierColor, "Barrier");
                    }

                    float abilityRadius = 30;
                    
                    float pos = ringRadius + ringThickness/2 + 6 + abilityRadius;
                    float posY = pos;
                    
                    // AbilityDraw( pos, -posY, "L",  primaryCooldownPct,    primaryStocks,   primaryMaxStocks,    Color.cyan );   pos += abilityRadius*2 + 5;
                    AbilityDraw( pos, -posY, "R",  secondaryCooldownPct,  secondaryStocks,   secondaryMaxStocks, Color.cyan );   pos += abilityRadius*2 + 10;
                    AbilityDraw( pos, -posY, "Sh", utilityCooldownPct,    utilityStocks,     utilityMaxStocks,   Color.cyan );   pos += abilityRadius*2 + 10;
                    AbilityDraw( pos, -posY, "F",  specialCooldownPct,    specialStocks,     specialMaxStocks,   Color.cyan );   pos += abilityRadius*2 + 10;
                    AbilityDraw( pos, -posY, "F8", equipmentCooldownPct,  equipmentStocks,   equipmentMaxStocks, new Color(1, 0.4f, 0f) );
                    
                    // Apply the texture to the image
                    ApplyTexture();
                }
            }
            
            // Clean up any text elements that weren't used in this update cycle
            CleanupUnusedTextElements();
        }

        private void AbilityDraw(float x, float y, string text, float fac, int stocks, int maxStocks, Color stockColor)
        {
            float radius = 30;
            
            // if (cooldownPct <= 0) {return;}
            
            Color c = fac > 0 ? Color.gray : stockColor;
            int fontSize = text.Length > 1 ? 20 : 24; 
            bool hasOne = false;

            if (maxStocks == 0) {return;}
            else if (maxStocks == 1)
            {
                hasOne = stocks == 1;
                DrawRing(x, y, radius, hasOne ? 5f : 3f, 0f, (1 - fac) * 360f, c, "Ability_CD_" + text);
            }
            else
            {
                float degrees = 360f / maxStocks;
                float start;
                float end;
                
                for (int i = 0; i < maxStocks; i++)
                {
                    
                    if (i == stocks)
                    {
                        c = Color.gray;
                        start = degrees * i + 10;
                        end   = ColorUtility.Map( fac, 0, 1, degrees * (i + 1) - 10, degrees * i );
                    }
                    else if (i > stocks) {continue;}
                    else
                    {
                        hasOne = true;
                        c = stockColor;
                        start = degrees * (i + 0) + 10;
                        end   = degrees * (i + 1) - 10;
                    }

                    if ( end <= start ) {continue;}
                    
                    DrawRing( x, y, radius, c == stockColor ? 5f : 3f, start, end, c, "Ability_CD_" + text);
                        // Debuging: 
                    // DrawRing( x, y + 25*(i+1)+25, 10, 2, start, end, c, "Ability_CD_" + text);
                    // DrawTextC(x, y - 40 - i * 90, $"{i}:{start}", 16, c);
                    // DrawTextC(x, y - 60 - i * 90, $"{i}:{end}",   16, c);
                    // DrawTextC(x, y - 80 - i * 90, $"{i}:{fac}",   16, c);
                }
            }
            
            
            DrawTextC(x, y - 80, $"{text}\n{fac}\n{stocks}\n{maxStocks}\n{stockColor}", 12, c);
            DrawTextC(x, y, text, fontSize, hasOne ? stockColor : Color.gray);
            
            
            // Log the positions for debugging
            logger.LogInfo($"Drew ability '{text}' with ring and text at position ({x}, {y})");
        }

        // Public method to match the Terraria signature:
        // DrawRing(centerX, centerY, radius, width, degreeStart, degreeEnd, color, label)
        public void DrawRing(float centerX, float centerY, float radius, float width, float degreeStart, float degreeEnd, Color color, string name = "")
        {
            // Draw the ring but don't apply texture yet (will be applied once at the end)
            textureGenerator.DrawRing(centerX, centerY, radius, width, degreeStart, degreeEnd, color, name);
        }
 
        
        // Public method to clear the texture
        public void ClearTexture()
        {
            if (textureGenerator != null)
            {
                textureGenerator.ClearTexture();
            }
            
            // Properly destroy all text elements
            foreach (var textPair in textElements)
            {
                if (textPair.Value != null)
                {
                    // Destroy the GameObject to prevent visual artifacts
                    Object.Destroy(textPair.Value.gameObject);
                }
            }
            
            // Clear the dictionary
            textElements.Clear();
            
            ApplyTexture();
        }

        // Method to mark a text element as used in this update cycle
        private void MarkTextElementUsed(string elementName)
        {
            usedTextElements.Add(elementName);
        }
        
        // Clean up unused text elements after an update cycle
        private void CleanupUnusedTextElements()
        {
            // Create a list of keys to remove
            List<string> keysToRemove = new List<string>();
            
            // Find text elements that weren't used in this update cycle
            foreach (var pair in textElements)
            {
                if (!usedTextElements.Contains(pair.Key))
                {
                    // Destroy the GameObject
                    if (pair.Value != null)
                    {
                        Object.Destroy(pair.Value.gameObject);
                    }
                    keysToRemove.Add(pair.Key);
                }
            }
            
            // Remove unused elements from dictionary
            foreach (var key in keysToRemove)
            {
                textElements.Remove(key);
            }
            
            // Clear the used elements set for next cycle
            usedTextElements.Clear();
        }
    }
} 