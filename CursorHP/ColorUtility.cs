using UnityEngine;

namespace CursorHP
{
    public static class ColorUtility
    {
        /// <summary>
        /// Creates a color for the health ring based on a health percentage
        /// </summary>
        /// <param name="healthPercentage">Health percentage between 0 and 1</param>
        /// <param name="alpha">Alpha value for the color (0-1)</param>
        /// <returns>A color ranging from red (low health) to green (full health)</returns>
        public static Color GetHealthColor(float healthPercentage, float alpha = 1.0f)
        {
            // Ensure health percentage is in valid range
            healthPercentage = Mathf.Clamp01(healthPercentage);
            
            // Calculate hue based on health percentage (0.333333 for full health, 0 for no health)
            float hue = healthPercentage * 0.333333f;
            
            // Create color with HSV
            Color color = Color.HSVToRGB(hue, 1f, 1f);
            
            // Ensure alpha is in valid range
            alpha = Mathf.Clamp01(alpha);
            color.a = alpha;
            
            return color;
        }

        
        public static float Map(float value, float sourceMin, float sourceMax, float targetMin, float targetMax)
        {
            // Ensure that the value is within the source range
            value = Mathf.Clamp(value, sourceMin, sourceMax);

            // Calculate the normalized value within the source range
            float normalizedValue = (value - sourceMin) / (sourceMax - sourceMin);

            // Map the normalized value to the target range
            float result = targetMin + normalizedValue * (targetMax - targetMin);

            return result;
        }
        
        /// <summary>
        /// Blend two colors with proper alpha blending
        /// </summary>
        public static Color BlendColors(Color background, Color foreground)
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
    }
} 