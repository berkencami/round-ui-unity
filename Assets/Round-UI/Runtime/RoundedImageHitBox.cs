using UnityEngine;

namespace RoundUI
{
    /// <summary>
    /// Handles hitBox detection for rounded images using Signed Distance Field (SDF) calculations
    /// </summary>
    public class RoundedImageHitBox
    {
        private readonly RoundedImage _roundedImage;

        /// <summary>
        /// Gets the bounding rectangle for hitbox calculations
        /// </summary>
        private Rect HitBoxRect => new (_roundedImage.rectTransform.rect.position, _roundedImage.GetImageSize());

        public RoundedImageHitBox(RoundedImage roundedImage)
        {
            _roundedImage = roundedImage;
        }

        /// <summary>
        /// Tests if a screen position is inside the rounded image hitbox
        /// </summary>
        public bool HitTest(Vector2 screenPosition)
        {
            // If hitBox detection is disabled, always return true
            if (!_roundedImage.UseHitBoxOutside && !_roundedImage.UseHitBoxInside)
                return true;

            // Get corner radii and pack them for SDF calculation
            var radii = _roundedImage.GetCornerRounding();
            var rounding = new Vector4(
                radii[Corner.TOP_RIGHT],
                radii[Corner.BOTTOM_RIGHT],
                radii[Corner.TOP_LEFT],
                radii[Corner.BOTTOM_LEFT]
            );

            // Calculate border radius
            var borderRadius = HitBoxRect.GetShortLength() * 0.25f;
            if (_roundedImage.UseHitBoxInside && _roundedImage.Mode == RoundingMode.BORDER)
                borderRadius *= _roundedImage.BorderThickness;

            // Transform screen position to local space
            Vector2 localPosition = _roundedImage.rectTransform.InverseTransformPoint(screenPosition);

            return PerformHitTest(localPosition, HitBoxRect.size, rounding, borderRadius, _roundedImage.DistanceFalloff);
        }

        /// <summary>
        /// Performs SDF-based hit testing
        /// </summary>
        private bool PerformHitTest(Vector2 samplePosition, Vector2 size, Vector4 radii, float borderDistance, float falloff)
        {
            // Transform radii to pixel space
            var minDimension = Mathf.Min(size.x, size.y);
            var transformedRadii = radii * 0.5f * minDimension;

            // Calculate distance from rounded box edge using SDF
            var distance = RoundedBoxSDF(samplePosition, size * 0.5f, transformedRadii);
            var distanceWithBorder = Mathf.Abs(distance + borderDistance) - borderDistance;
            var distanceWithBorderAndFalloff = distanceWithBorder - falloff * 0.5f;

            return distanceWithBorderAndFalloff < 0;
        }

        /// <summary>
        /// Signed Distance Field calculation for a rounded box
        /// Returns the distance from a point to the edge of the rounded box
        /// </summary>
        private float RoundedBoxSDF(Vector2 samplePoint, Vector2 halfSize, Vector4 radii)
        {
            // Select the appropriate corner radius based on quadrant
            var selectedRadius = radii.x; // Default to top-right
            if (samplePoint.x <= 0.0f)
            {
                selectedRadius = samplePoint.y <= 0.0f ? radii.w : radii.z; // Bottom-left or top-left
            }
            else if (samplePoint.y <= 0.0f)
            {
                selectedRadius = radii.y; // Bottom-right
            }

            // Calculate distance from rounded corner
            var absolutePosition = new Vector2(Mathf.Abs(samplePoint.x), Mathf.Abs(samplePoint.y));
            var q = absolutePosition - halfSize;
            q.x += selectedRadius;
            q.y += selectedRadius;

            return Mathf.Min(Mathf.Max(q.x, q.y), 0.0f) + Vector2.Max(q, Vector2.zero).magnitude - selectedRadius;
        }
    }
}
