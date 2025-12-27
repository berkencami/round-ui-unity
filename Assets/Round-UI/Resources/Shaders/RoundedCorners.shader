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

			fixed4 frag(v2f i) : SV_Target {
				float2 uv2x = decode2(i.uv2.x);
				float2 uv2y = decode2(i.uv2.y);

				float2 roundedBoxUV =
#if UNITY_VERSION >= 202020
					i.uv1.zw;
#else
					i.uv;
#endif

				float alpha = DynamicRoundedBox(roundedBoxUV, i.uv1, float4(uv2x.x, uv2x.y, uv2y.x, uv2y.y), i.uv3.x, i.uv3.y);

				#ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (alpha - 0.001);
                #endif
				
				return mixAlpha(tex2D(_MainTex, i.uv), i.uv, i.color, i.worldPosition, alpha);
			}
			ENDCG
		}
	}
}
