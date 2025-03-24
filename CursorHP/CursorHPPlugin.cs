using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;

namespace CursorHP
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2API.Utils.NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInProcess("Risk of Rain 2.exe")]
    public class CursorHPPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "org.fuzzyexpress.CursorHP";
        public const string PluginName = "CursorHP";
        public const string PluginVersion = "1.0.0";

        // Components
        private CursorHPConfiguration config;
        private HealthRingManager healthRingManager;
        
        // Track if we need to recreate the ring (when configuration changes)
        private bool needsRecreate = false;

        public void Awake()
        {
            // Set property to indicate this is a client-side mod
            RoR2Application.isModded = true;
            
            // Load and initialize configuration
            config = new CursorHPConfiguration(Config);
            
            // Initialize health ring manager
            healthRingManager = new HealthRingManager(config, Logger);
            healthRingManager.Initialize();
            
            // Setup Risk of Options
            SetupRiskOfOptions();
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginGUID} is loaded! (Client-side only mod)");
            
            // Subscribe to the player spawning event
            On.RoR2.PlayerCharacterMasterController.OnBodyStart += PlayerCharacterMasterController_OnBodyStart;
            
            // Subscribe to config change events
            SubscribeToConfigEvents();
        }
        
        private void SetupRiskOfOptions()
        {
            // Create a Risk of Options mod menu
            ModSettingsManager.SetModDescription("Displays a health ring around your cursor");
            
            // Try to load the mod icon
            try
            {
                Sprite modIcon = LoadModIcon();
                if (modIcon != null)
                {
                    ModSettingsManager.SetModIcon(modIcon);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to load mod icon: {ex.Message}");
            }
            
            // Toggle options
            ModSettingsManager.AddOption(new CheckBoxOption(config.EnableHealthRing));
            ModSettingsManager.AddOption(new CheckBoxOption(config.ShowDuringPause));
            ModSettingsManager.AddOption(new KeyBindOption(config.ToggleKey));
            
            // Ring appearance settings
            ModSettingsManager.AddOption(new SliderOption(
                config.RingSize,
                new SliderConfig
                {
                    min = 100f,
                    max = 500f,
                    FormatString = "{0:0}px"
                }
            ));
            
            ModSettingsManager.AddOption(new SliderOption(
                config.RingThickness,
                new SliderConfig
                {
                    min = 5f,
                    max = 50f,
                    FormatString = "{0:0}px"
                }
            ));
            
            ModSettingsManager.AddOption(new SliderOption(
                config.FadeThreshold,
                new SliderConfig
                {
                    min = 0f,
                    max = 1f,
                    FormatString = "{0:0.00}"
                }
            ));
            
            ModSettingsManager.AddOption(new SliderOption(
                config.BorderInset,
                new SliderConfig
                {
                    min = 0f,
                    max = 20f,
                    FormatString = "{0:0.0}"
                }
            ));
            
            // Position settings
            ModSettingsManager.AddOption(new CheckBoxOption(config.CenterPosition));
            
            ModSettingsManager.AddOption(new SliderOption(
                config.OffsetX,
                new SliderConfig
                {
                    min = -960f,
                    max = 960f,
                    FormatString = "{0:0}px"
                }
            ));
            
            ModSettingsManager.AddOption(new SliderOption(
                config.OffsetY,
                new SliderConfig
                {
                    min = -540f,
                    max = 540f,
                    FormatString = "{0:0}px"
                }
            ));
        }
        
        private Sprite LoadModIcon()
        {
            try
            {
                // Create a simple icon using the RingTextureGenerator
                RingTextureGenerator iconGen = new RingTextureGenerator(config);
                iconGen.InitializeTexture(256);
                
                // Draw a health ring icon with red, green, and blue segments
                iconGen.DrawRing(0, 0, 100, 20, 0f, 120f, Color.red, "RedSegment");
                iconGen.DrawRing(0, 0, 100, 20, 120f, 240f, Color.green, "GreenSegment");
                iconGen.DrawRing(0, 0, 100, 20, 240f, 360f, Color.blue, "BlueSegment");
                
                // Draw inner and outer borders
                iconGen.DrawRing(0, 0, 90, 2, 0f, 360f, Color.white, "InnerBorder");
                iconGen.DrawRing(0, 0, 110, 2, 0f, 360f, Color.white, "OuterBorder");
                
                return iconGen.CreateSprite();
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to create mod icon: {ex.Message}");
                return null;
            }
        }
        
        private void SubscribeToConfigEvents()
        {
            // When these properties change, the ring needs to be recreated
            config.RingSize.SettingChanged += (sender, args) => needsRecreate = true;
            config.RingThickness.SettingChanged += (sender, args) => needsRecreate = true;
            config.BorderInset.SettingChanged += (sender, args) => needsRecreate = true;
            config.FadeThreshold.SettingChanged += (sender, args) => needsRecreate = true;
            
            // Position settings
            config.CenterPosition.SettingChanged += (sender, args) => needsRecreate = true;
            config.OffsetX.SettingChanged += (sender, args) => needsRecreate = true;
            config.OffsetY.SettingChanged += (sender, args) => needsRecreate = true;
            
            // Toggle settings
            config.EnableHealthRing.SettingChanged += (sender, args) => 
            {
                if (healthRingManager != null)
                {
                    healthRingManager.EnableUI(config.EnableHealthRing.Value);
                }
            };
            
            config.ShowDuringPause.SettingChanged += (sender, args) => 
            {
                // The Update method will handle this automatically
                Logger.LogInfo($"Show during pause changed to: {config.ShowDuringPause.Value}");
            };
            
            Logger.LogInfo("Subscribed to all config setting change events");
        }

        private void PlayerCharacterMasterController_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, PlayerCharacterMasterController self)
        {
            orig(self);
            
            // When the player body spawns, check if the health ring should be enabled
            if (healthRingManager != null)
            {
                if (config.EnableHealthRing.Value)
                {
                    Logger.LogInfo("Player spawned - enabling health ring UI");
                    healthRingManager.EnableUI(true);
                    
                    // Force an immediate update
                    healthRingManager.Update();
                }
                else
                {
                    Logger.LogInfo("Player spawned but health ring is disabled in settings");
                }
            }
            else
            {
                Logger.LogError("Health ring manager is null when player spawned!");
            }
        }
        
        private void Update()
        {
            // Check for toggle key press
            if (config.ToggleKey.Value.IsDown())
            {
                config.EnableHealthRing.Value = !config.EnableHealthRing.Value;
                Logger.LogInfo($"Health ring toggled to: {config.EnableHealthRing.Value}");
            }
            
            // Check if we need to recreate the ring due to config changes
            if (needsRecreate)
            {
                healthRingManager.Recreate();
                needsRecreate = false;
            }
            
            // Update the health ring and other stats only if enabled
            if (healthRingManager != null && config.EnableHealthRing.Value && LocalUserManager.GetFirstLocalUser()?.cachedBody != null)
            {
                // Only update the health ring, don't recreate texture every frame
                healthRingManager.Update();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up when the plugin is unloaded
            healthRingManager.Cleanup();
            
            // Unsubscribe from events
            On.RoR2.PlayerCharacterMasterController.OnBodyStart -= PlayerCharacterMasterController_OnBodyStart;
        }
    }
}
