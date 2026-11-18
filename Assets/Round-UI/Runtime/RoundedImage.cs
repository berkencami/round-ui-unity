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
        /// Outer outline width in local units (0 when the outline is off). Used by the hitbox so the
        /// outline is clickable too.
        /// </summary>
        internal float OutlineWidth(Rect imageRect) =>
            _outlineEnabled ? Mathf.Max(0f, _outlineThickness) * imageRect.GetShortLength() : 0f;

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

        // --- Gradient ---
        [SerializeField]
        private bool _gradientEnabled;

        [SerializeField]
        private Color _gradientColorA = Color.white;

        [SerializeField]
        private Color _gradientColorB = Color.black;

        [SerializeField]
        private GradientDirection _gradientDirection = GradientDirection.VERTICAL;

        [SerializeField]
        private float _gradientOffset = 0f;

        // --- Outline ---
        [SerializeField] private bool _outlineEnabled;
        [SerializeField] private Color _outlineColor = Color.black;
        [SerializeField] private float _outlineThickness = 0.05f;

        // --- Shine ---
        [SerializeField] private bool _shineEnabled;
        [SerializeField] private Color _shineColor = new Color(1, 1, 1, 0.6f);
        [SerializeField] private float _shineAngle = 45f;
        [SerializeField] private float _shineWidth = 0.15f;
        [SerializeField] private float _shineIntensity = 0.6f;
        [SerializeField] private float _shineSpeed = 1f;
        [SerializeField] private bool _shineLoop = true;

        // --- Drop Shadow ---
        [SerializeField] private bool _shadowEnabled;
        [SerializeField] private Color _shadowColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Vector2 _shadowOffset = new Vector2(4f, -4f);
        [SerializeField] private float _shadowBlur = 8f;
        [SerializeField] private float _shadowSpread = 0f;

        /// <summary>
        /// Time (same clock as the shader's <c>_Time.y</c>) at which this image was last enabled.
        /// </summary>
        private float _shineStartTime;

        /// <summary>
        /// Shared effect material from <see cref="RoundedImageMaterialPool"/> (null while no effect is enabled).
        /// </summary>
        private Material _effectMaterial;

        /// <summary>
        /// The settings <see cref="_effectMaterial"/> was acquired with.
        /// </summary>
        private EffectSettings _effectMaterialSettings;

        /// <summary>
        /// Whether any effect that lives on the material is active.
        /// </summary>
        private bool AnyEffectActive => _gradientEnabled || _outlineEnabled || _shineEnabled || _shadowEnabled;

        private EffectSettings CurrentEffectSettings => new EffectSettings(
            _gradientEnabled, _gradientColorA, _gradientColorB, _gradientDirection, _gradientOffset,
            _outlineEnabled, _outlineColor, _outlineThickness,
            _shineEnabled, _shineColor, _shineAngle, _shineWidth, _shineIntensity, _shineSpeed, _shineLoop,
            _shadowEnabled, _shadowColor, _shadowOffset, _shadowBlur, _shadowSpread);

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
        /// The base material. While effects are enabled this is a pooled material shared by every
        /// image with identical effect settings, so those images batch together.
        /// Overriding <c>material</c> (instead of <c>materialForRendering</c>) keeps Unity's
        /// material-modifier chain intact, so <see cref="Mask"/> / stencil clipping keeps working.
        /// </summary>
        public override Material material
        {
            get
            {
                if (!AnyEffectActive)
                {
                    ReleaseEffectMaterial();
                    return base.material;
                }

                var settings = CurrentEffectSettings;
                if (_effectMaterial == null || !settings.Equals(_effectMaterialSettings))
                {
                    // Acquire before releasing so an unchanged entry is never destroyed and recreated.
                    var acquired = RoundedImageMaterialPool.Acquire(settings, defaultMaterial);
                    ReleaseEffectMaterial();
                    _effectMaterial = acquired;
                    _effectMaterialSettings = settings;
                }
                return _effectMaterial;
            }
            set => base.material = value;
        }

        private void ReleaseEffectMaterial()
        {
            if (_effectMaterial == null) return;
            RoundedImageMaterialPool.Release(_effectMaterialSettings);
            _effectMaterial = null;
        }

        /// <summary>
        /// Sends data of the image to the shader so we can create the rounded effect.
        /// </summary>
        /// <param name="vh">Contains the vertices of the images.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            var rounding = GetCornerRoundingPacked();
            var imageRect = GetImageRect();
            var displaySize = imageRect.size;
            var borderData = (_roundingMode == RoundingMode.BORDER ? BorderThickness : 1) * Mathf.Min(displaySize.x, displaySize.y) * MAX_FACTOR_BORDER;

            // Account for falloff.
            borderData += DistanceFalloff;

            // Encode corner values into single floats. Optimization so we only have to use one UV.
            // Data should be decoded in the shader.
            var rightSideEncoded = Encoding.EncodeFloats(rounding.x, rounding.y); // top right, bottom right
            var leftSideEncoded = Encoding.EncodeFloats(rounding.z, rounding.w);  // top left, bottom left

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
            // z: when this image was enabled, so a one-shot (non-looping) shine starts from then.
            // Stored per vertex rather than on the material so the material can stay shared.
            var uv3 = new Vector4(falloff, borderData, _shineStartTime, 0f);

            // Expand mesh so outer effects (outline, drop shadow) have room to be drawn.
            var outlineExpand = _outlineEnabled ? _outlineThickness * Mathf.Min(displaySize.x, displaySize.y) : 0f;
            var expandX = outlineExpand;
            var expandY = outlineExpand;

            if (_shadowEnabled)
            {
                // Shadow silhouette = shape (+ outline) grown by spread, softened by half the blur on each side.
                var shadowExtent = outlineExpand + Mathf.Max(0f, _shadowSpread) + Mathf.Max(0f, _shadowBlur) * 0.5f;
                expandX = Mathf.Max(expandX, shadowExtent + Mathf.Abs(_shadowOffset.x));
                expandY = Mathf.Max(expandY, shadowExtent + Mathf.Abs(_shadowOffset.y));
            }

            var expandsMesh = (expandX > 0 || expandY > 0) && displaySize.x > 0 && displaySize.y > 0;
            if (expandsMesh)
            {
                x += expandX / displaySize.x;
                y += expandY / displaySize.y;
            }

            var positionScalar = Vector3.one + new Vector3(x, y, 0) * 2;
            var uv0Offset = new Vector2((spriteOuterUV.z - spriteOuterUV.x) / 2 + spriteOuterUV.x, (spriteOuterUV.w - spriteOuterUV.y) / 2 + spriteOuterUV.y);

            // Scale around the center of the drawn image (not the pivot) so the expanded mesh
            // stays aligned with the UVs, which are scaled around their own center.
            var meshCenter = imageRect.center;

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.position.x = meshCenter.x + (vert.position.x - meshCenter.x) * positionScalar.x;
                vert.position.y = meshCenter.y + (vert.position.y - meshCenter.y) * positionScalar.y;

#if UNITY_2020_2_OR_NEWER
                if (sprite != null || expandsMesh)
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

            _gradientEnabled = false;
            _gradientColorA = Color.white;
            _gradientColorB = Color.black;
            _gradientDirection = GradientDirection.VERTICAL;
            _gradientOffset = 0f;

            _outlineEnabled = false;
            _outlineColor = Color.black;
            _outlineThickness = 0.05f;

            _shineEnabled = false;
            _shineColor = new Color(1, 1, 1, 0.6f);
            _shineAngle = 45f;
            _shineWidth = 0.15f;
            _shineIntensity = 0.6f;
            _shineSpeed = 1f;
            _shineLoop = true;

            _shadowEnabled = false;
            _shadowColor = new Color(0, 0, 0, 0.5f);
            _shadowOffset = new Vector2(4f, -4f);
            _shadowBlur = 8f;
            _shadowSpread = 0f;
        }
#endif
        
        /// <summary>
        /// Validates raycast location against rounded corners hitBox.
        /// </summary>
        /// <param name="screenPoint">The point on the screen where the graphic was sampled.</param>
        /// <param name="eventCamera">The camera used for creating the event.</param>
        /// <returns>Whether the screen point hit the graphic.</returns>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            => HitBox.HitTest(screenPoint, eventCamera) && base.IsRaycastLocationValid(screenPoint, eventCamera);

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
        /// <summary>
        /// Allocation-free version of <see cref="GetCornerRounding()"/> used on hot paths
        /// (mesh generation, raycasts). Packed as (top right, bottom right, top left, bottom left),
        /// the order the shader and hitbox SDF expect.
        /// </summary>
        internal Vector4 GetCornerRoundingPacked() => new Vector4(
            GetCornerRounding(Corner.TOP_RIGHT),
            GetCornerRounding(Corner.BOTTOM_RIGHT),
            GetCornerRounding(Corner.TOP_LEFT),
            GetCornerRounding(Corner.BOTTOM_LEFT));

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
        public Vector2 GetImageSize() => GetImageRect().size;

        /// <summary>
        /// The visual rect of the image inside the Rect Transform, in local space.
        /// </summary>
        internal Rect GetImageRect()
        {
            var imageRect = GetPixelAdjustedRect();
            if (preserveAspect && sprite != null)
                PreserveSpriteAspectRatio(ref imageRect, sprite.rect.size);

            return imageRect;
        }
        
        /// <summary>
        /// Checks whether to update the image. Effect materials need no per-frame work:
        /// the shine animates in the shader from <c>_Time</c>.
        /// </summary>
        private void Update()
        {
            if (_propertyChanged)
            {
                SetVerticesDirty();
                _propertyChanged = false;
            }
        }

        protected override void OnEnable()
        {
            // Matches the shader's _Time.y in Play Mode. In Edit Mode a one-shot shine is not replayed.
            _shineStartTime = Application.isPlaying ? Time.timeSinceLevelLoad : 0f;
            base.OnEnable(); // marks the mesh dirty, so the new start time reaches the vertices
            EnsureCanvasShaderChannels();
            ApplyOutlineRaycastPadding();
        }

        protected override void OnDisable()
        {
            RestoreRaycastPadding();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            ApplyOutlineRaycastPadding(); // outline width scales with the image size
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyOutlineRaycastPadding(); // outline toggled/resized in the Inspector during Play Mode
        }
#endif

        /// <summary>
        /// How far the raycast padding is currently grown for the outline. Only this delta is ever
        /// added/removed, so the user's own padding (and any change they make to it) is preserved.
        /// </summary>
        private float _outlinePaddingApplied;

        /// <summary>
        /// <see cref="GraphicRaycaster"/> rejects points outside the RectTransform (+ raycast padding)
        /// before the rounded hitbox runs, but the outline is drawn outside the rect. Grow the padding by
        /// the outline width so the outline is clickable. Play Mode only, so the serialized padding in
        /// the scene is never changed; the delta is removed again when the image is disabled.
        /// </summary>
        private void ApplyOutlineRaycastPadding()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;

            var width = OutlineWidth(GetImageRect());
            if (Mathf.Approximately(width, _outlinePaddingApplied)) return;

            // Negative padding enlarges the raycast area.
            var delta = width - _outlinePaddingApplied;
            raycastPadding -= new Vector4(delta, delta, delta, delta);
            _outlinePaddingApplied = width;
        }

        private void RestoreRaycastPadding()
        {
            if (_outlinePaddingApplied == 0f) return;
            var w = _outlinePaddingApplied;
            raycastPadding += new Vector4(w, w, w, w);
            _outlinePaddingApplied = 0f;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            EnsureCanvasShaderChannels();
        }

        /// <summary>
        /// The shader reads size, corner radii and falloff/border data from UV1–UV3.
        /// A Canvas only forwards those channels when they are enabled in
        /// <see cref="Canvas.additionalShaderChannels"/>, so make sure they are
        /// (otherwise the image renders garbage on a freshly created Canvas).
        /// </summary>
        private void EnsureCanvasShaderChannels()
        {
            const AdditionalCanvasShaderChannels required =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.TexCoord3;

            var c = canvas;
            if (c == null) return;
            if ((c.additionalShaderChannels & required) != required)
                c.additionalShaderChannels |= required;
        }

        /// <summary>
        /// Returns the pooled effect material.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseEffectMaterial();
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
