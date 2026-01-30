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
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#include "ShaderSetup.cginc"
            #include "UnityUI.cginc"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;

			// Effect uniforms
			float _GradientEnabled;
			fixed4 _GradientColorA;
			fixed4 _GradientColorB;
			float _GradientDirection;
			float _GradientOffset;

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

				// --- Compositing ---
				fixed4 finalColor = fixed4(0, 0, 0, 0);
				float mA = mainAlpha * col.a;
				finalColor.rgb = col.rgb * mA;
				finalColor.a = mA;

				// Mask texture
				finalColor.a *= tex2D(_MaskTex, i.uv).r;

				#ifdef UNITY_UI_CLIP_RECT
				finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(finalColor.a - 0.001);
				#endif

				return finalColor;
			}
			ENDCG
		}
	}
}
