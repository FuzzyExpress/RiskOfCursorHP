using BepInEx.Configuration;
using UnityEngine;
using RiskOfOptions;

namespace CursorHP
{
    public class CursorHPConfiguration
    {
        
        // Ring appearance
        public ConfigEntry<float> RingSize { get; private set; }
        public ConfigEntry<float> RingThickness { get; private set; }
        public ConfigEntry<float> FadeThreshold { get; private set; }
        
        // Position
        public ConfigEntry<bool> CenterPosition { get; private set; }
        public ConfigEntry<float> OffsetX { get; private set; }
        public ConfigEntry<float> OffsetY { get; private set; }
        
        // Border settings
        public ConfigEntry<float> BorderInset { get; private set; }
        
        // Toggle options
        public ConfigEntry<bool> EnableHealthRing { get; private set; }
        public ConfigEntry<bool> ShowDuringPause { get; private set; }
        public ConfigEntry<KeyboardShortcut> ToggleKey { get; private set; }
        
        public CursorHPConfiguration(ConfigFile config)
        {
            // Ring appearance
            RingSize = config.Bind("Ring Appearance", "RingSize", 300f, 
                "Size of the health ring in pixels");
            
            RingThickness = config.Bind("Ring Appearance", "RingThickness", 20f, 
                "Thickness of the health ring in pixels");
            
            FadeThreshold = config.Bind("Ring Appearance", "FadeThreshold", 0.75f, 
                "Health percentage above which the ring starts to fade out (0.0 - 1.0)");
            
            // Border settings
            BorderInset = config.Bind("Ring Appearance", "BorderInset", 8f,
                "How much the inner ring is inset from the border (0.0 - 1.0)");
            
            // Position
            CenterPosition = config.Bind("Position", "CenterPosition", true, 
                "Whether to position the ring at the center of the screen");
            
            OffsetX = config.Bind("Position", "OffsetX", 0f, 
                "Horizontal offset from center (only used if CenterPosition is false)");
            
            OffsetY = config.Bind("Position", "OffsetY", 0f, 
                "Vertical offset from center (only used if CenterPosition is false)");
                
            // Toggle options
            EnableHealthRing = config.Bind("Toggle Options", "EnableHealthRing", true,
                "Whether the health ring is enabled");
                
            ShowDuringPause = config.Bind("Toggle Options", "ShowDuringPause", false,
                "Whether to show the health ring during pause menu");
                
            ToggleKey = config.Bind("Toggle Options", "ToggleKey", new KeyboardShortcut(KeyCode.F4),
                "Key to toggle the health ring on/off");
        }
    }
} 