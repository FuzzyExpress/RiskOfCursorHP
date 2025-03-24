using UnityEngine;
using System.Collections.Generic;

namespace CursorHP
{
    public class RingTextureGenerator
    {
        private CursorHPConfiguration config;
        private const int TextureSize = (int)(512 * 2.25);
        private Texture2D baseTexture;
        
        // Cache for previously drawn rings
        private Dictionary<string, Rect> ringCache = new Dictionary<string, Rect>();
        private bool isDirty = false;
        
        // Simple ring definition structure
        public struct RingDefinition
        {
            public float outerRadius;      // Outer radius in pixels
            public float innerRadius;      // Inner radius in pixels
            public Color color;            // Color of the ring
            public string statName;        // Name of the stat this ring represents
        }
        
        public RingTextureGenerator(CursorHPConfiguration config)
        {
            this.config = config;
            InitializeTexture();
        }
        
        // Initialize a new transparent texture
        public void InitializeTexture()
        {
            baseTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            baseTexture.filterMode = FilterMode.Bilinear;
            ClearTexture();
        }
        
        // Initialize a new transparent texture with a custom size
        public void InitializeTexture(int size)
        {
            baseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            baseTexture.filterMode = FilterMode.Bilinear;
            ClearTexture();
        }
        
        // Clear the texture to transparent
        public void ClearTexture()
        {
            Color transparent = new Color(0, 0, 0, 0);
            Color[] clearColors = new Color[TextureSize * TextureSize];
            for (int i = 0; i < clearColors.Length; i++)
            {
                clearColors[i] = transparent;
            }
            baseTexture.SetPixels(clearColors);
            baseTexture.Apply();
            isDirty = true;
            ringCache.Clear();
        }
        
        // Main draw ring function, similar to Terraria's DrawCircleRadial
        // centerX, centerY: center position of the ring
        // radius: distance from center to middle of the ring
        // width: thickness of the ring
        // degreeStart: starting angle in degrees (0 is top, goes counterclockwise)
        // degreeEnd: ending angle in degrees
        // color: color of the ring
        // statName: optional name for the stat this ring represents
        public void DrawRing(float centerX, float centerY, float radius, float width, float degreeStart, float degreeEnd, Color color, string statName = "")
        {
            // Force minimum settings for visibility during debugging
            // if (width < 4) width = 4;
            // Don't force alpha to 1.0, respect the original alpha value
            
            // Clear the cache for now to ensure redraw
            ringCache.Clear();
            
            // Convert center coordinates to texture space
            float texCenterX = (TextureSize / 2f) + centerX;
            float texCenterY = (TextureSize / 2f) + centerY;
            
            // Calculate inner and outer radii
            float outerRadius = radius + (width / 2f);
            float innerRadius = radius - (width / 2f);
            if (innerRadius < 1) innerRadius = 1;
            
            float innerRadiusSq = innerRadius * innerRadius;
            float outerRadiusSq = outerRadius * outerRadius;
            
            // Calculate the bounding box for this ring (optimization)
            float boundRadius = outerRadius + 2; // +2 for safety
            int minX = Mathf.Max(0, Mathf.FloorToInt(texCenterX - boundRadius));
            int maxX = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(texCenterX + boundRadius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(texCenterY - boundRadius));
            int maxY = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(texCenterY + boundRadius));
            
            // Special case for full circle to avoid precision issues
            bool isFullCircle = Mathf.Approximately(Mathf.Abs(degreeEnd - degreeStart), 360f) || 
                               degreeEnd - degreeStart >= 359f;
            
            // Very basic and reliable approach - no optimizations, just make sure it works
            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - texCenterY;
                float dy2 = dy * dy;
                
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - texCenterX;
                    float distanceSq = dx * dx + dy2;
                    
                    // If pixel is within the ring thickness
                    if (distanceSq <= outerRadiusSq && distanceSq >= innerRadiusSq)
                    {
                        // For full circles, we don't need to check the angle
                        if (isFullCircle)
                        {
                            baseTexture.SetPixel(x, y, color);
                            continue;
                        }
                        
                        // Calculate the angle in degrees, properly aligned with 0 at top
                        float pixelDegrees = GetAngleInDegrees(dx, dy);
                        
                        // Check if the pixel is within the arc
                        if (IsAngleInArc(pixelDegrees, degreeStart, degreeEnd))
                        {
                            baseTexture.SetPixel(x, y, color);
                        }
                    }
                }
            }
            
            // Mark as dirty
            isDirty = true;
        }
        
        // Calculate angle in degrees where 0 = top (12 o'clock) and increases counterclockwise
        private float GetAngleInDegrees(float dx, float dy)
        {
            // Convert from cartesian to polar coordinates, with adjustment for our coordinate system
            // atan2(y, x) gives angle from positive x-axis, counterclockwise
            // We need to adjust it so 0 is at the top (12 o'clock)
            
            // Get standard atan2 angle (in radians, -π to π)
            float standardAngle = Mathf.Atan2(dy, dx);
            
            // Convert to degrees
            float degrees = standardAngle * Mathf.Rad2Deg;
            
            // Adjust so 0 is at top and goes counterclockwise
            // Standard atan2: 0 = right, 90 = up, 180/-180 = left, -90 = down
            // We want: 0 = up, 90 = left, 180 = down, 270 = right
            
            // Subtract 90 degrees to make 0 at the top
            float adjustedDegrees = degrees - 90;
            
            // Ensure the result is in the range 0-360
            if (adjustedDegrees < 0)
                adjustedDegrees += 360;
            if (adjustedDegrees >= 360)
                adjustedDegrees -= 360;
            
            return adjustedDegrees;
        }
        
        // Check if a given angle is within the specified arc
        private bool IsAngleInArc(float angle, float startAngle, float endAngle)
        {
            // Handle the case where the arc wraps around 360 degrees
            if (startAngle <= endAngle)
            {
                // Simple case: check if angle is between start and end
                return angle >= startAngle && angle <= endAngle;
            }
            else
            {
                // Arc wraps around the 0/360 boundary
                // Check if angle is either >= start or <= end
                return angle >= startAngle || angle <= endAngle;
            }
        }
        
        // Blend two colors with proper alpha
        private Color BlendColors(Color background, Color foreground)
        {
            // If foreground is fully opaque or background is fully transparent, return foreground
            if (foreground.a >= 1f || background.a <= 0f)
                return foreground;
                
            // If foreground is fully transparent, return background
            if (foreground.a <= 0f)
                return background;
                
            // Calculate resultant alpha
            float resultAlpha = foreground.a + background.a * (1 - foreground.a);
            
            // If resulting alpha is approximately zero, return transparent color
            if (resultAlpha <= 0.01f)
                return new Color(0, 0, 0, 0);
                
            // Calculate color components with alpha blending
            float resultRed = (foreground.r * foreground.a + background.r * background.a * (1 - foreground.a)) / resultAlpha;
            float resultGreen = (foreground.g * foreground.a + background.g * background.a * (1 - foreground.a)) / resultAlpha;
            float resultBlue = (foreground.b * foreground.a + background.b * background.a * (1 - foreground.a)) / resultAlpha;
            
            return new Color(resultRed, resultGreen, resultBlue, resultAlpha);
        }
        
    
        
        // Create a sprite from the current texture state
        public Sprite CreateSprite()
        {
            // Always apply changes to ensure texture is up to date
            baseTexture.Apply();
            isDirty = false;
            
            // Create a new sprite with proper pivot at center
            return Sprite.Create(
                baseTexture, 
                new Rect(0, 0, TextureSize, TextureSize), 
                new Vector2(0.5f, 0.5f),  // Pivot at center
                100f,                     // Pixels per unit
                0,                        // Extrude edges
                SpriteMeshType.FullRect   // Full rect mesh for UI
            );
        }
        
        // Helper method to get the texture size
        public int GetTextureSize()
        {
            return TextureSize;
        }
    }
} 