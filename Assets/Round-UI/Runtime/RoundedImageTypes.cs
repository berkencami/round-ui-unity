using System;
using System.Runtime.InteropServices;
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
        [StructLayout(LayoutKind.Explicit)]
        private struct UIntFloat
        {
            [FieldOffset(0)]
            public uint uintValue;
            [FieldOffset(0)]
            public float floatValue;
        }

        /// <summary>
        /// Encodes two float values (0-1 range) into a single float by packing them as 16-bit values
        /// </summary>
        public static float EncodeFloats(float a, float b)
        {
            a = Mathf.Clamp01(a);
            b = Mathf.Clamp01(b);
            a *= UInt16.MaxValue;
            b *= UInt16.MaxValue;
            var aInt = (UInt32)Mathf.FloorToInt(a);
            var bInt = ((UInt32)Mathf.FloorToInt(b)) << 16;

            var union = new UIntFloat { uintValue = aInt | bInt };
            return union.floatValue;
        }
    }
}
