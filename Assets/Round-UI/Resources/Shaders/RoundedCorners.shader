Shader "Hidden/RoundUI/RoundedCorners"
{
	Properties
	{
		[HideInInspector] _MainTex ("Texture", 2D) = "white" { }
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Int) = 8
		[EightBit] _Stencil("Stencil ID", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Int) = 0
		[EightBit] _StencilWriteMask("Stencil Write Mask", Int) = 255
		[EightBit] _StencilReadMask("Stencil Read Mask", Int) = 255
		[Enum(None, 0, Alpha, 1, Red, 8, Green, 4, Blue, 2, RGB, 14, RGBA, 15)] _ColorMask("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
		[HideInInspector] _MaskTex("MaskTexture", 2D) = "white"{}

		// Gradient
		_GradientEnabled("Gradient Enabled", Float) = 0
		_GradientColorA("Gradient Color A", Color) = (1,1,1,1)
		_GradientColorB("Gradient Color B", Color) = (0,0,0,1)
		_GradientDirection("Gradient Direction", Float) = 0
		_GradientOffset("Gradient Offset", Float) = 0

		// Outline
		_OutlineEnabled("Outline Enabled", Float) = 0
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_OutlineThickness("Outline Thickness", Float) = 0.05

		// Shine
		_ShineEnabled("Shine Enabled", Float) = 0
		_ShineColor("Shine Color", Color) = (1,1,1,0.6)
		_ShineDirection("Shine Direction", Vector) = (0.7071,0.7071,0,0)
		_ShineWidth("Shine Width", Float) = 0.15
		_ShineIntensity("Shine Intensity", Float) = 0.6
		_ShineSpeed("Shine Speed", Float) = 1
		_ShineLoop("Shine Loop", Float) = 1

		// Drop Shadow
		_ShadowEnabled("Shadow Enabled", Float) = 0
		_ShadowColor("Shadow Color", Color) = (0,0,0,0.5)
		_ShadowOffset("Shadow Offset", Vector) = (4,-4,0,0)
		_ShadowBlur("Shadow Blur", Float) = 8
		_ShadowSpread("Shadow Spread", Float) = 0

	}

	SubShader
	{
		Tags
		{
			"RenderType"="Transparent"
			"Queue"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		Cull Off
		Lighting Off
		ZTest [unity_GUIZTestMode]
		ColorMask [_ColorMask]
		// Fragment output is premultiplied alpha.
		Blend One OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#include "ShaderSetup.cginc"
            #include "UnityUI.cginc"

			#pragma vertex vert
			#pragma fragment frag

			// Must live in the shader itself — keyword pragmas inside an include file are not
			// turned into variants, which silently disabled RectMask2D and Mask alpha clipping.
			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			sampler2D _MainTex;

			// Effect uniforms
			float _GradientEnabled;
			fixed4 _GradientColorA;
			fixed4 _GradientColorB;
			float _GradientDirection;
			float _GradientOffset;

			float _OutlineEnabled;
			fixed4 _OutlineColor;
			float _OutlineThickness;

			float _ShineEnabled;
			fixed4 _ShineColor;
			float4 _ShineDirection; // (cos, sin) of the angle, computed on the CPU
			float _ShineWidth;
			float _ShineIntensity;
			float _ShineSpeed;
			float _ShineLoop;

			float _ShadowEnabled;
			fixed4 _ShadowColor;
			float4 _ShadowOffset;
			float _ShadowBlur;
			float _ShadowSpread;

			fixed4 frag(v2f i) : SV_Target {
				float2 uv2x = decode2(i.uv2.x);
				float2 uv2y = decode2(i.uv2.y);

				float2 roundedBoxUV =
#if UNITY_VERSION >= 202020
					i.uv1.zw;
#else
					i.uv;
#endif

				float4 radii = float4(uv2x.x, uv2x.y, uv2y.x, uv2y.y);
				float2 size = i.uv1.xy;
				float falloff = i.uv3.x;
				float border = i.uv3.y;

				// --- Main shape alpha ---
				float mainAlpha = DynamicRoundedBox(roundedBoxUV, size, radii, falloff, border);

				// --- Outline alpha ---
				float outlineAlpha = 0;
				if (_OutlineEnabled > 0.5)
				{
					float2 samplePos = (roundedBoxUV - 0.5) * size;
					float minSize = min(size.x, size.y);
					float dist = sdRoundedBox(samplePos, size * 0.5, radii * 0.5 * minSize);

					float outerAlpha = AntialiasedCutoff(dist - _OutlineThickness * minSize, falloff);
					float solidAlpha = AntialiasedCutoff(dist, falloff);
					outlineAlpha = saturate(outerAlpha - solidAlpha);
				}

				// --- Base color from texture and vertex ---
				fixed4 texColor = tex2D(_MainTex, i.uv);
				fixed4 col = texColor * i.color;

				// --- Apply gradient ---
				if (_GradientEnabled > 0.5)
				{
					float t = 0;
					if (_GradientDirection < 0.5)
						t = roundedBoxUV.y; // Vertical
					else if (_GradientDirection < 1.5)
						t = roundedBoxUV.x; // Horizontal
					else
						t = (roundedBoxUV.x + roundedBoxUV.y) * 0.5; // Diagonal

					t = saturate(t + _GradientOffset);
					fixed4 gradColor = lerp(_GradientColorA, _GradientColorB, t);
					col *= gradColor;
				}

				// --- Compositing (premultiplied alpha) ---
				fixed4 finalColor = fixed4(0, 0, 0, 0);

				// Outer outline (behind main)
				if (_OutlineEnabled > 0.5)
				{
					// Multiplied by vertex alpha so Image color alpha / CanvasGroup fades also fade the outline.
					float oA = outlineAlpha * _OutlineColor.a * i.color.a;
					finalColor = fixed4(_OutlineColor.rgb * oA, oA);
				}

				// Main shape
				float mA = mainAlpha * col.a;
				finalColor.rgb = finalColor.rgb * (1.0 - mA) + col.rgb * mA;
				finalColor.a = finalColor.a * (1.0 - mA) + mA;

				// --- Shine (on top of the shape, not the shadow) ---
				if (_ShineEnabled > 0.5)
				{
					float2 dir = _ShineDirection.xy;

					// Animated on the GPU from _Time so the material never has to be updated
					// per frame (and can be shared between images for batching).
					// Looping shines share the global clock (so equal buttons stay in sync); a one-shot
					// shine counts from the moment its image was enabled (uv3.z, set per image).
					float cycles = _ShineLoop > 0.5 ? _Time.y * _ShineSpeed : (_Time.y - i.uv3.z) * _ShineSpeed;
					float progress = _ShineLoop > 0.5 ? frac(cycles) : saturate(cycles);

					// Work in pixel space so the angle is the same visual angle for any aspect
					// ratio (in normalized UV space a wide button and a square badge would show
					// the same angle value at very different slants).
					float2 p = (roundedBoxUV - 0.5) * size;
					float halfExtent = max(0.5 * (abs(dir.x) * size.x + abs(dir.y) * size.y), 1e-4);
					float t = dot(p, dir) / halfExtent; // -1..1 across the shape

					// Sweep from fully off one side to fully off the other, with a short pause.
					float bandCenter = (progress * 2.0 - 1.0) * 1.5;
					float dist = abs(t - bandCenter);
					float shineValue = smoothstep(_ShineWidth * 1.5, 0.0, dist) * _ShineIntensity * _ShineColor.a;

					finalColor.rgb = lerp(finalColor.rgb, _ShineColor.rgb * finalColor.a, shineValue);
				}

				// Alpha of the shape itself (fill + outline, no shadow). Used for alpha clipping so a
				// RoundedImage acting as a Mask only masks its visible shape, not its drop shadow.
				float contentAlpha = finalColor.a;

				// --- Drop shadow (behind everything) ---
				if (_ShadowEnabled > 0.5)
				{
					float minSize = min(size.x, size.y);
					float2 shadowPos = (roundedBoxUV - 0.5) * size - _ShadowOffset.xy;
					float d = sdRoundedBox(shadowPos, size * 0.5, radii * 0.5 * minSize);

					// Silhouette covers the main shape plus the outer outline band.
					float outer = (_OutlineEnabled > 0.5) ? _OutlineThickness * minSize : 0;
					float normalizedBorder = border / 0.25 / minSize;
					if (normalizedBorder > 0.999)
					{
						d -= outer;
					}
					else
					{
						// Border mode: ring between the inner border edge and the outer edge.
						float inner = -2.0 * border;
						d = abs(d - (outer + inner) * 0.5) - (outer - inner) * 0.5;
					}
					d -= _ShadowSpread;

					// Soft edge centered on the silhouette, half the blur on each side.
					float halfBlur = _ShadowBlur * 0.5;
					// Multiplied by vertex alpha so Image color alpha / CanvasGroup fades also fade the shadow.
					float sA = AntialiasedCutoff(d - halfBlur, falloff + halfBlur) * _ShadowColor.a * i.color.a;

					// Shape over shadow.
					finalColor.rgb = finalColor.rgb + _ShadowColor.rgb * sA * (1.0 - finalColor.a);
					finalColor.a = finalColor.a + sA * (1.0 - finalColor.a);
				}

				// Mask texture and rect clipping (applied to rgb too since output is premultiplied)
				float visibility = tex2D(_MaskTex, i.uv).r;

				#ifdef UNITY_UI_CLIP_RECT
				visibility *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				#endif

				finalColor *= visibility;

				#ifdef UNITY_UI_ALPHACLIP
				clip(contentAlpha * visibility - 0.001);
				#endif

				return finalColor;
			}
			ENDCG
		}
	}
}
