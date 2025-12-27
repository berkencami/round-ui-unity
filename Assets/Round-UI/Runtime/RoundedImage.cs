using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoundUI
{
    /// <summary>
    /// Image that contains additional behaviour for procedurally rounding the corners of the image.
    /// </summary>
    [AddComponentMenu("UI/RoundUI Image")]
    [DisallowMultipleComponent]
    public class RoundedImage : Image
    {
        /// <summary>
        /// The thickness of the border between 0 and 1.
        /// </summary>
        public float BorderThickness
        {
            get
            {
                return _selectedUnit switch
                {
                    RoundingUnit.PERCENTAGE => _borderThickness,
                    RoundingUnit.WORLD => _borderThickness / rectTransform.rect.GetShortLength() * 2,
                    _ => throw new NotSupportedException("This unit is not supported for getting border thickness.")
                };
            }
            private set
            {
                switch (_selectedUnit)
                {
                    case RoundingUnit.PERCENTAGE:
                        if (!Mathf.Approximately(_borderThickness, value))
                            _propertyChanged = true;
                        _borderThickness = value;
                        break;
                    case RoundingUnit.WORLD:
                        if (!Mathf.Approximately(_borderThickness, value))
                            _propertyChanged = true;
                        _borderThickness = value * rectTransform.rect.GetShortLength() / 2;
                        break;
                    default:
                        throw new NotSupportedException("This unit is not supported for setting border thickness.");
                }
            }
        }

        /// <summary>
        /// The mode used for rendering the rounded image.
        /// </summary>
        public RoundingMode Mode
        {
            get => _roundingMode;
            private set
            {
                if (_roundingMode != value)
                    _propertyChanged = true;
                _roundingMode = value;
            }
        }

        /// <summary>
        /// The amount of distance fall off the Rounded Image has.
        /// Value assigned should be positive.
        /// </summary>
        public float DistanceFalloff
        {
            get => _distanceFalloff;
            private set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException($"Distance fall off can't be a value below zero. Error value: {value}");

                if (!Mathf.Approximately(_distanceFalloff, value))
                    _propertyChanged = true;
                _distanceFalloff = value;
            }
        }

        /// <summary>
        /// Whether the outside hitBox is being used.
        /// </summary>
        public bool UseHitBoxOutside
        {
            get => _useHitBoxOutside;
            private set
            {
                _useHitBoxOutside = value;
                if (!_useHitBoxOutside)
                    _useHitBoxInside = false;
            }
        }

        /// <summary>
        /// Whether the inside hitBox is being used.
        /// </summary>
        public bool UseHitBoxInside
        {
            get => _useHitBoxInside;
            private set => _useHitBoxInside = value && _useHitBoxOutside;
        }

        /// <summary>
        /// The current rounding unit mode for this image.
        /// </summary>
        public RoundingUnit RoundingUnit
        {
            get => _selectedUnit;
            set => _selectedUnit = value;
        }

        /// <summary>
        /// The hitBox used for the rounded image graphic.
        /// </summary>
        private RoundedImageHitBox HitBox => _hitBox ??= new RoundedImageHitBox(this);

        /// <summary>
        /// Used to determine the value of the border before it's sent to the shader.
        /// </summary>
        private const float MAX_FACTOR_BORDER = 0.25f;
        
        /// <summary>
        /// The mode used to round the image.
        /// </summary>
        [SerializeField]
        private RoundingMode _roundingMode;

        /// <summary>
        /// The rounding amount for each corner.
        /// </summary>
        [SerializeField]
        private float[] _roundingAmount = new float[4];

        /// <summary>
        /// The border thickness amount.
        /// </summary>
        [SerializeField]
        private float _borderThickness = 0.5f;

        /// <summary>
        /// Whether the outside hitBox is being used.
        /// </summary>
        [SerializeField]
        private bool _useHitBoxOutside = true;

        /// <summary>
        /// Whether the inside hitBox is being used.
        /// </summary>
        [SerializeField]
        private bool _useHitBoxInside = true;

        /// <summary>
        /// The falloff distance amount.
        /// </summary>
        [SerializeField]
        private float _distanceFalloff = 0.5f;

        /// <summary>
        /// The unit that is currently being used.
        /// </summary>
        [SerializeField]
        private RoundingUnit _selectedUnit = RoundingUnit.PERCENTAGE;

        /// <summary>
        /// The hitBox that handles the hit detection.
        /// </summary>
        private RoundedImageHitBox _hitBox;

        /// <summary>
        /// A flag that is used to determine whether a value for the rounded image has changed.
        /// This will then take it into account for the next frame and accordingly update the image.
        /// </summary>
        private bool _propertyChanged;

        /// <summary>
        /// Cached default material for rounded corners shader.
        /// </summary>
        private static Material _defaultMaterial;
        
        /// <summary>
        /// Default material using the rounded corners shader.
        /// </summary>
        public override Material defaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                    _defaultMaterial = new Material(Shader.Find("Hidden/RoundUI/RoundedCorners"));
                return _defaultMaterial;
            }
        }
        
        /// <summary>
        /// Sends data of the image to the shader so we can create the rounded effect.
        /// </summary>
        /// <param name="vh">Contains the vertices of the images.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            var rounding = GetCornerRounding();
            var displaySize = GetImageSize();
            var borderData = (_roundingMode == RoundingMode.BORDER ? BorderThickness : 1) * Mathf.Min(displaySize.x, displaySize.y) * MAX_FACTOR_BORDER;

            // Account for falloff.
            borderData += DistanceFalloff;

            // Encode corner values into single floats. Optimization so we only have to use one UV.
            // Data should be decoded in the shader.
            var rightSideEncoded = Encoding.EncodeFloats(rounding[Corner.TOP_RIGHT], rounding[Corner.BOTTOM_RIGHT]);
            var leftSideEncoded = Encoding.EncodeFloats(rounding[Corner.TOP_LEFT], rounding[Corner.BOTTOM_LEFT]);

            var spriteOuterUV = sprite == null ? new Vector4(0, 0, 1, 1) : UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

            // Populate the vertices with the UV data.
            var vert = new UIVertex();

            float x = 0;
            if (displaySize.x != 0)
                x = DistanceFalloff / displaySize.x;

            float y = 0;
            if (displaySize.y != 0)
                y = DistanceFalloff / displaySize.y;

            var uv1 = displaySize;
            var uv2 = new Vector2(rightSideEncoded, leftSideEncoded);

            var falloff = DistanceFalloff / (sprite == null ? 1 : 2);
            var uv3 = new Vector2(falloff, borderData);

            var positionScalar = Vector3.one + new Vector3(x, y, 0) * 2;
            var uv0Offset = new Vector2((spriteOuterUV.z - spriteOuterUV.x) / 2 + spriteOuterUV.x, (spriteOuterUV.w - spriteOuterUV.y) / 2 + spriteOuterUV.y);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.position.Scale(positionScalar);

#if UNITY_2020_2_OR_NEWER
                if (sprite != null)
                {
                    vert.uv0 -= (Vector4)uv0Offset;
                    vert.uv0.Scale(positionScalar);
                    vert.uv0 += (Vector4)uv0Offset;
                }
#else
                vert.uv0 -= uv0Offset;
                vert.uv0.Scale(positionScalar);
                vert.uv0 += uv0Offset;
#endif

#if UNITY_2020_2_OR_NEWER
                var z = (vert.uv0.x - spriteOuterUV.x) / (spriteOuterUV.z - spriteOuterUV.x);
                var w = (vert.uv0.y - spriteOuterUV.y) / (spriteOuterUV.w - spriteOuterUV.y);
                vert.uv1 = new Vector4(uv1.x, uv1.y, float.IsNaN(z) ? 0 : z, float.IsNaN(w) ? 0 : w);
#else
                vert.uv1 = uv1;
#endif
                vert.uv2 = uv2;
                vert.uv3 = uv3;

                vh.SetUIVertex(vert, i);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resets the component values.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            BorderThickness = 0.5f;
            Mode = default;
            DistanceFalloff = default;
            UseHitBoxOutside = true;
            UseHitBoxInside = true;
            RoundingUnit = default;
            SetCornerRounding(0f, 0f, 0f, 0f);
        }
#endif
        
        /// <summary>
        /// Validates raycast location against rounded corners hitBox.
        /// </summary>
        /// <param name="screenPoint">The point on the screen where the graphic was sampled.</param>
        /// <param name="eventCamera">The camera used for creating the event.</param>
        /// <returns>Whether the screen point hit the graphic.</returns>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            => HitBox.HitTest(screenPoint) && base.IsRaycastLocationValid(screenPoint, eventCamera);

        /// <summary>
        /// Returns the percentage rounding of the given corner.
        /// </summary>
        /// <param name="corner">The corner to get the rounding from.</param>
        /// <returns>The rounding of the given corner in percentages.</returns>
        private float GetCornerRounding(Corner corner)
        {
            return _selectedUnit switch
            {
                RoundingUnit.PERCENTAGE => _roundingAmount[(int)corner],
                RoundingUnit.WORLD => Mathf.Clamp01(_roundingAmount[(int)corner] / rectTransform.rect.GetShortLength() *
                                                    2),
                _ => throw new NotSupportedException("This unit is not supported for getting corner rounding.")
            };
        }

        /// <summary>
        /// Returns a dictionary with the rounding in percentages from every corner mapped by the corner.
        /// </summary>
        /// <returns>A dictionary with the rounding in percentages from every corner mapped by the corner.</returns>
        public Dictionary<Corner, float> GetCornerRounding()
        {
            var output = new Dictionary<Corner, float>(4);
            for (var i = 0; i < _roundingAmount.Length; i++)
            {
                var c = (Corner)i;
                output.Add(c, GetCornerRounding(c));
            }
            return output;
        }
        
        /// <summary>
        /// Sets the corner amount of every corner.
        /// Amounts should be within the bounds of zero to one.
        /// </summary>
        private void SetCornerRounding(float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            var rounding = new [] { topLeft, topRight, bottomLeft, bottomRight };
            for (var i = 0; i < 4; i++)
                ApplyCornerRounding((Corner)i, rounding[i]);
        }

        /// <summary>
        /// The visual size of the image inside the Rect Transform.
        /// </summary>
        /// <returns>The visual size of the image inside the Rect Transform.</returns>
        public Vector2 GetImageSize()
        {
            var imageRect = GetPixelAdjustedRect();
            if (preserveAspect && sprite != null)
                PreserveSpriteAspectRatio(ref imageRect, sprite.rect.size);

            return imageRect.size;
        }
        
        /// <summary>
        /// Checks whether to update the image.
        /// </summary>
        private void Update()
        {
            if (!_propertyChanged) return;
            SetVerticesDirty();
            _propertyChanged = false;
        }

        /// <summary>
        /// Applies rounding amount to given corner.
        /// </summary>
        /// <param name="corner">Corner to round.</param>
        /// <param name="amount">By how much to round.</param>
        private void ApplyCornerRounding(Corner corner, float amount)
        {
            if (amount is < 0 or > 1)
                throw new ArgumentOutOfRangeException(
                    $"Given amount should be within a range of zero to one. Error value: {amount}, corner: {corner}");

            switch (_selectedUnit)
            {
                case RoundingUnit.PERCENTAGE:
                    if (!Mathf.Approximately(amount, _roundingAmount[(int)corner]))
                        _propertyChanged = true;
                    _roundingAmount[(int)corner] = amount;
                    break;
                case RoundingUnit.WORLD:
                    var newValue = amount * rectTransform.rect.GetShortLength() / 2;
                    if (!Mathf.Approximately(newValue, _roundingAmount[(int)corner]))
                        _propertyChanged = true;
                    _roundingAmount[(int)corner] = newValue;
                    break;
                default:
                    throw new NotSupportedException("This unit is not supported for setting corner rounding.");
            }
        }

        /// <summary>
        /// Updates the given Rect with the values where the sprite maintains aspect ratio.
        /// </summary>
        /// <param name="rect">The rect with the old values that will be updated with new.</param>
        /// <param name="spriteSize">The size of the sprite.</param>
        private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
            }
        }
    }
}
