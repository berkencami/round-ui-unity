using UnityEngine;

namespace RoundUI
{
    /// <summary>
    /// Handles hitBox detection for rounded images using Signed Distance Field (SDF) calculations
    /// </summary>
    public class RoundedImageHitBox
    {
        private readonly RoundedImage _roundedImage;

        public RoundedImageHitBox(RoundedImage roundedImage)
        {
            _roundedImage = roundedImage;
        }

        /// <summary>
        /// Tests if a screen position is inside the rounded image hitbox
        /// </summary>
        /// <param name="screenPosition">Screen point of the pointer event.</param>
        /// <param name="eventCamera">The event camera (null for Screen Space - Overlay canvases).</param>
        public bool HitTest(Vector2 screenPosition, Camera eventCamera)
        {
            // If hitBox detection is disabled, always return true
            if (!_roundedImage.UseHitBoxOutside)
                return true;

            // Transform the screen point into the rect's local space. Works for every canvas render
            // mode because the event camera is taken into account.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _roundedImage.rectTransform, screenPosition, eventCamera, out var localPosition))
                return false;

            // The SDF is centered on the drawn image, which is not the pivot unless the pivot is (0.5, 0.5).
            var imageRect = _roundedImage.GetImageRect();
            localPosition -= imageRect.center;

            // Corner radii packed for the SDF (no allocation — this runs on every pointer move).
            var rounding = _roundedImage.GetCornerRoundingPacked();

            // Only a border with the inside hitbox enabled is a ring (its hole is not clickable).
            // Everything else is tested as a solid shape — modelling a filled shape as a ring of
            // half the short side put its inner edge exactly on the center, so center clicks were
            // rejected depending on float rounding.
            float? borderRadius = null;
            if (_roundedImage.UseHitBoxInside && _roundedImage.Mode == RoundingMode.BORDER)
                borderRadius = imageRect.GetShortLength() * 0.25f * _roundedImage.BorderThickness;

            // The outline is drawn outside the shape, so it extends the clickable area outward.
            var outlineWidth = _roundedImage.OutlineWidth(imageRect);

            return PerformHitTest(localPosition, imageRect.size, rounding, borderRadius, outlineWidth,
                _roundedImage.DistanceFalloff);
        }

        /// <summary>
        /// Performs SDF-based hit testing. The clickable region is every point whose signed
        /// distance to the shape edge lies in [inner, outer]: outer = outline width (0 without one),
        /// inner = -(ring width) for a border with the inside hitbox, otherwise unbounded (solid).
        /// </summary>
        private bool PerformHitTest(Vector2 samplePosition, Vector2 size, Vector4 radii, float? borderDistance,
            float outlineWidth, float falloff)
        {
            // Transform radii to pixel space
            var minDimension = Mathf.Min(size.x, size.y);
            var transformedRadii = radii * 0.5f * minDimension;

            // Calculate distance from rounded box edge using SDF
            var distance = RoundedBoxSDF(samplePosition, size * 0.5f, transformedRadii);

            var outer = outlineWidth + falloff * 0.5f;
            if (distance >= outer)
                return false;

            return !borderDistance.HasValue || distance > -2f * borderDistance.Value;
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
