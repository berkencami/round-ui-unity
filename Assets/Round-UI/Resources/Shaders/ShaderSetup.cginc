#include "UnityCG.cginc"

struct appdata 
{
	float4 vertex  : POSITION;
	float2 uv      : TEXCOORD0;
#if UNITY_VERSION >= 202020
	float4 uv1     : TEXCOORD1;
#else
	float2 uv1     : TEXCOORD1;
#endif
	float2 uv2     : TEXCOORD2; // corner radii, two per float (see Encoding.EncodeFloats)
	float4 uv3     : TEXCOORD3; // x: falloff, y: border, z: shine start time
	float4 color   : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f 
{
	float2 uv            : TEXCOORD0;
#if UNITY_VERSION >= 202020
	float4 uv1           : TEXCOORD1;
#else
	float2 uv1           : TEXCOORD1;
#endif
	// Packed integers: must not be interpolated, or the decode could round to a neighbour value.
	nointerpolation float2 uv2 : TEXCOORD2;
	float4 uv3           : TEXCOORD3;
	float4 worldPosition : TEXCOORD4;
	float4 vertex        : SV_POSITION;
	float4 color         : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

// Keyword pragmas (UNITY_UI_CLIP_RECT / UNITY_UI_ALPHACLIP) are declared in RoundedCorners.shader.

#include "UnityUI.cginc"

float4 _ClipRect;
sampler2D _MaskTex;

v2f vert(appdata v) 
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.worldPosition = v.vertex;
	o.uv = v.uv;
	o.uv1 = v.uv1;
	o.uv2 = v.uv2;
	o.uv3 = v.uv3;
	o.color = v.color;
	return o;
}

// Decodes two 0..1 values packed as integers: value = round(b * 4095) * 4096 + round(a * 4095).
// Every packed value is an integer below 2^24, so it is exact in a float — no bit reinterpretation,
// no NaN/denormal patterns that some GPUs or drivers would flush or canonicalize.
float2 decode2(float value)
{
	float v = round(value);
	float hi = floor(v / 4096.0);
	float lo = v - hi * 4096.0;
	return float2(lo, hi) / 4095.0;
}

float sdRoundedBox(in float2 p, in float2 s, in float4 r)
{
	r.xy = (p.x > 0.0) ? r.xy : r.zw;
	r.x = (p.y > 0.0) ? r.x : r.y;
	float2 q = abs(p) - s + r.x;
	return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

float AntialiasedCutoff(float distance, float amount) 
{
	return smoothstep(amount, -amount, distance + amount);
}

float RoundedBox(float2 samplePosition, float2 size, float4 radius, float falloff) {
	float2 samplePositionTranslated = (samplePosition - .5) * size;
	float distToRect = sdRoundedBox(samplePositionTranslated, size * .5, radius * .5 * min(size.x, size.y));
	return AntialiasedCutoff(distToRect, falloff);
}

float RoundedBoxBorder(float2 samplePosition, float2 size, float4 radius, float falloff, float distance)
{
	float2 samplePositionTranslated = (samplePosition - .5) * size;
	float distToRect = abs(sdRoundedBox(samplePositionTranslated, size * .5, radius * .5 * min(size.x, size.y)) + distance) - distance;
	return AntialiasedCutoff(distToRect, falloff);
}

float DynamicRoundedBox(float2 samplePosition, float2 size, float4 radius, float falloff, float distance)
{
	const float normalizedBorder = distance / 0.25 / min(size.x, size.y);
	return normalizedBorder > 0.999 ? RoundedBox(samplePosition, size, radius, falloff) : RoundedBoxBorder(samplePosition, size, radius, falloff, distance);
}