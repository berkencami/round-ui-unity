using System;
using UnityEngine;

namespace RoundUI
{
    /// <summary>
    /// Mappings for the corners used by RoundedImage.
    /// </summary>
    public enum Corner
    {
        /// <summary>
        /// The top left corner.
        /// </summary>
        [InspectorName("Top Left")]
        TOP_LEFT = 0,

        /// <summary>
        /// The top right corner.
        /// </summary>
        [InspectorName("Top Right")]
        TOP_RIGHT = 1,

        /// <summary>
        /// The bottom left corner.
        /// </summary>
        [InspectorName("Bottom Left")]
        BOTTOM_LEFT = 2,

        /// <summary>
        /// The bottom right corner.
        /// </summary>
        [InspectorName("Bottom Right")]
        BOTTOM_RIGHT = 3,
    }

    /// <summary>
    /// The fill mode used for rounding the image.
    /// </summary>
    public enum RoundingMode
    {
        /// <summary>
        /// Fills the entire rounded image with a solid colour.
        /// </summary>
        [InspectorName("Fill")]
        FILL = 0,

        /// <summary>
        /// Creates a parameterized border the user can tweak.
        /// </summary>
        [InspectorName("Border")]
        BORDER = 1,
    }

    /// <summary>
    /// The different units used as representation in the inspector.
    /// </summary>
    public enum RoundingUnit
    {
        /// <summary>
        /// Everything in a range between 0 and 1.
        /// </summary>
        [InspectorName("Percentage")]
        PERCENTAGE = 0,

        /// <summary>
        /// Uses Unity's world units.
        /// </summary>
        [InspectorName("World Units")]
        WORLD = 1,
    }

    /// <summary>
    /// The direction used for gradient color blending.
    /// </summary>
    public enum GradientDirection
    {
        /// <summary>
        /// Blends colors from top to bottom.
        /// </summary>
        [InspectorName("Vertical")]
        VERTICAL = 0,

        /// <summary>
        /// Blends colors from left to right.
        /// </summary>
        [InspectorName("Horizontal")]
        HORIZONTAL = 1,

        /// <summary>
        /// Blends colors diagonally.
        /// </summary>
        [InspectorName("Diagonal")]
        DIAGONAL = 2,
    }

    /// <summary>
    /// Utility extension methods for Rect operations
    /// </summary>
    public static class RectUtilities
    {
        /// <summary>
        /// Returns the shortest dimension (width or height) of the rect
        /// </summary>
        public static float GetShortLength(this Rect rect) => Mathf.Min(rect.width, rect.height);
    }

    /// <summary>
    /// Utility class for encoding multiple float values into a single float for shader communication
    /// </summary>
    public static class Encoding
    {
        /// <summary>Steps per value (12 bits). 4096 * 4096 = 2^24, the largest integer range a float stores exactly.</summary>
        private const int Steps = 4095;

        /// <summary>
        /// Encodes two float values (0-1 range) into a single float as the integer
        /// <c>round(b * 4095) * 4096 + round(a * 4095)</c>. The result is always an exact integer
        /// below 2^24, so — unlike reinterpreting packed bits as a float — it can never form a NaN or
        /// denormal bit pattern that a GPU or driver might flush or canonicalize.
        /// Decoded by <c>decode2</c> in ShaderSetup.cginc.
        /// </summary>
        public static float EncodeFloats(float a, float b)
        {
            int aInt = Mathf.RoundToInt(Mathf.Clamp01(a) * Steps);
            int bInt = Mathf.RoundToInt(Mathf.Clamp01(b) * Steps);
            return bInt * (Steps + 1) + aInt;
        }
    }
}
