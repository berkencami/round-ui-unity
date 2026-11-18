using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoundUI
{
    /// <summary>
    /// The effect parameters that live on the material. Images with equal settings share one
    /// material (and therefore batch together). Values of disabled effects are zeroed so they
    /// don't split otherwise identical images into separate materials.
    /// </summary>
    internal readonly struct EffectSettings : IEquatable<EffectSettings>
    {
        public readonly bool GradientEnabled;
        public readonly Color GradientColorA;
        public readonly Color GradientColorB;
        public readonly GradientDirection GradientDirection;
        public readonly float GradientOffset;

        public readonly bool OutlineEnabled;
        public readonly Color OutlineColor;
        public readonly float OutlineThickness;

        public readonly bool ShineEnabled;
        public readonly Color ShineColor;
        public readonly float ShineAngle;
        public readonly float ShineWidth;
        public readonly float ShineIntensity;
        public readonly float ShineSpeed;
        public readonly bool ShineLoop;

        public readonly bool ShadowEnabled;
        public readonly Color ShadowColor;
        public readonly Vector2 ShadowOffset;
        public readonly float ShadowBlur;
        public readonly float ShadowSpread;

        public EffectSettings(
            bool gradientEnabled, Color gradientColorA, Color gradientColorB, GradientDirection gradientDirection, float gradientOffset,
            bool outlineEnabled, Color outlineColor, float outlineThickness,
            bool shineEnabled, Color shineColor, float shineAngle, float shineWidth, float shineIntensity, float shineSpeed, bool shineLoop,
            bool shadowEnabled, Color shadowColor, Vector2 shadowOffset, float shadowBlur, float shadowSpread)
        {
            GradientEnabled = gradientEnabled;
            GradientColorA = gradientEnabled ? gradientColorA : default;
            GradientColorB = gradientEnabled ? gradientColorB : default;
            GradientDirection = gradientEnabled ? gradientDirection : default;
            GradientOffset = gradientEnabled ? gradientOffset : 0f;

            OutlineEnabled = outlineEnabled;
            OutlineColor = outlineEnabled ? outlineColor : default;
            OutlineThickness = outlineEnabled ? outlineThickness : 0f;

            ShineEnabled = shineEnabled;
            ShineColor = shineEnabled ? shineColor : default;
            ShineAngle = shineEnabled ? shineAngle : 0f;
            ShineWidth = shineEnabled ? shineWidth : 0f;
            ShineIntensity = shineEnabled ? shineIntensity : 0f;
            ShineSpeed = shineEnabled ? shineSpeed : 0f;
            ShineLoop = shineEnabled && shineLoop;

            ShadowEnabled = shadowEnabled;
            ShadowColor = shadowEnabled ? shadowColor : default;
            ShadowOffset = shadowEnabled ? shadowOffset : default;
            ShadowBlur = shadowEnabled ? Mathf.Max(0f, shadowBlur) : 0f;
            ShadowSpread = shadowEnabled ? shadowSpread : 0f;
        }

        public bool AnyEnabled => GradientEnabled || OutlineEnabled || ShineEnabled || ShadowEnabled;

        // Exact comparisons on purpose: Color/Vector2 '==' are approximate, which would be
        // inconsistent with GetHashCode.
        public bool Equals(EffectSettings o) =>
            GradientEnabled == o.GradientEnabled && GradientColorA.Equals(o.GradientColorA) &&
            GradientColorB.Equals(o.GradientColorB) && GradientDirection == o.GradientDirection &&
            GradientOffset.Equals(o.GradientOffset) &&
            OutlineEnabled == o.OutlineEnabled && OutlineColor.Equals(o.OutlineColor) &&
            OutlineThickness.Equals(o.OutlineThickness) &&
            ShineEnabled == o.ShineEnabled && ShineColor.Equals(o.ShineColor) && ShineAngle.Equals(o.ShineAngle) &&
            ShineWidth.Equals(o.ShineWidth) && ShineIntensity.Equals(o.ShineIntensity) &&
            ShineSpeed.Equals(o.ShineSpeed) && ShineLoop == o.ShineLoop &&
            ShadowEnabled == o.ShadowEnabled && ShadowColor.Equals(o.ShadowColor) &&
            ShadowOffset.Equals(o.ShadowOffset) && ShadowBlur.Equals(o.ShadowBlur) &&
            ShadowSpread.Equals(o.ShadowSpread);

        public override bool Equals(object obj) => obj is EffectSettings other && Equals(other);

        public override int GetHashCode()
        {
            var h = HashCode.Combine(GradientEnabled, GradientColorA, GradientColorB, (int)GradientDirection, GradientOffset);
            h = HashCode.Combine(h, OutlineEnabled, OutlineColor, OutlineThickness);
            h = HashCode.Combine(h, ShineEnabled, ShineColor, ShineAngle, ShineWidth, ShineIntensity, ShineSpeed, ShineLoop);
            return HashCode.Combine(h, ShadowEnabled, ShadowColor, ShadowOffset, ShadowBlur, ShadowSpread);
        }
    }

    /// <summary>
    /// Reference-counted pool of effect materials, keyed by <see cref="EffectSettings"/>.
    /// A pooled material is never modified after creation, so modifier copies made from it
    /// (e.g. the stencil materials created by <see cref="UnityEngine.UI.Mask"/>) always stay in sync.
    /// </summary>
    internal static class RoundedImageMaterialPool
    {
        private sealed class Entry
        {
            public Material Material;
            public int RefCount;
        }

        private static readonly Dictionary<EffectSettings, Entry> Entries = new Dictionary<EffectSettings, Entry>();

        private static readonly int PropGradientEnabled = Shader.PropertyToID("_GradientEnabled");
        private static readonly int PropGradientColorA = Shader.PropertyToID("_GradientColorA");
        private static readonly int PropGradientColorB = Shader.PropertyToID("_GradientColorB");
        private static readonly int PropGradientDirection = Shader.PropertyToID("_GradientDirection");
        private static readonly int PropGradientOffset = Shader.PropertyToID("_GradientOffset");

        private static readonly int PropOutlineEnabled = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int PropOutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropOutlineThickness = Shader.PropertyToID("_OutlineThickness");

        private static readonly int PropShineEnabled = Shader.PropertyToID("_ShineEnabled");
        private static readonly int PropShineColor = Shader.PropertyToID("_ShineColor");
        private static readonly int PropShineDirection = Shader.PropertyToID("_ShineDirection");
        private static readonly int PropShineWidth = Shader.PropertyToID("_ShineWidth");
        private static readonly int PropShineIntensity = Shader.PropertyToID("_ShineIntensity");
        private static readonly int PropShineSpeed = Shader.PropertyToID("_ShineSpeed");
        private static readonly int PropShineLoop = Shader.PropertyToID("_ShineLoop");

        private static readonly int PropShadowEnabled = Shader.PropertyToID("_ShadowEnabled");
        private static readonly int PropShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int PropShadowOffset = Shader.PropertyToID("_ShadowOffset");
        private static readonly int PropShadowBlur = Shader.PropertyToID("_ShadowBlur");
        private static readonly int PropShadowSpread = Shader.PropertyToID("_ShadowSpread");

        /// <summary>Number of distinct effect materials currently alive (for diagnostics).</summary>
        internal static int Count => Entries.Count;

        public static Material Acquire(in EffectSettings settings, Material template)
        {
            if (Entries.TryGetValue(settings, out var entry) && entry.Material != null)
            {
                entry.RefCount++;
                return entry.Material;
            }

            var material = new Material(template)
            {
                name = "RoundUI Image (Effects)",
                hideFlags = HideFlags.HideAndDontSave
            };
            Apply(material, settings);
            Entries[settings] = new Entry { Material = material, RefCount = 1 };
            return material;
        }

        public static void Release(in EffectSettings settings)
        {
            if (!Entries.TryGetValue(settings, out var entry)) return;
            if (--entry.RefCount > 0) return;

            Entries.Remove(settings);
            DestroyMaterial(entry.Material);
        }

        private static void Apply(Material mat, in EffectSettings s)
        {
            mat.SetFloat(PropGradientEnabled, s.GradientEnabled ? 1 : 0);
            mat.SetColor(PropGradientColorA, s.GradientColorA);
            mat.SetColor(PropGradientColorB, s.GradientColorB);
            mat.SetFloat(PropGradientDirection, (float)s.GradientDirection);
            mat.SetFloat(PropGradientOffset, s.GradientOffset);

            mat.SetFloat(PropOutlineEnabled, s.OutlineEnabled ? 1 : 0);
            mat.SetColor(PropOutlineColor, s.OutlineColor);
            mat.SetFloat(PropOutlineThickness, s.OutlineThickness);

            // Direction is computed once here instead of cos/sin per pixel in the shader.
            var angle = s.ShineAngle * Mathf.Deg2Rad;
            mat.SetFloat(PropShineEnabled, s.ShineEnabled ? 1 : 0);
            mat.SetColor(PropShineColor, s.ShineColor);
            mat.SetVector(PropShineDirection, new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), 0, 0));
            mat.SetFloat(PropShineWidth, s.ShineWidth);
            mat.SetFloat(PropShineIntensity, s.ShineIntensity);
            mat.SetFloat(PropShineSpeed, s.ShineSpeed);
            mat.SetFloat(PropShineLoop, s.ShineLoop ? 1 : 0);

            mat.SetFloat(PropShadowEnabled, s.ShadowEnabled ? 1 : 0);
            mat.SetColor(PropShadowColor, s.ShadowColor);
            mat.SetVector(PropShadowOffset, s.ShadowOffset);
            mat.SetFloat(PropShadowBlur, s.ShadowBlur);
            mat.SetFloat(PropShadowSpread, s.ShadowSpread);
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(material);
            else
                UnityEngine.Object.DestroyImmediate(material);
        }

#if UNITY_EDITOR
        // Pooled materials are HideAndDontSave, so they would survive a domain reload while the
        // (static) pool forgets them. Destroy them before the reload to avoid leaking.
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterReloadCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                foreach (var entry in Entries.Values)
                    DestroyMaterial(entry.Material);
                Entries.Clear();
            };
        }
#endif
    }
}
