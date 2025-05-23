#ifndef SPRITE_INSTANCING_INCLUDED
#define SPRITE_INSTANCING_INCLUDED

#if INSTANCING_ENABLED
	#define INSTANCING_ON
#endif

#ifdef DISALLOW_SPRITE_UPSCALING
	#undef TEXTURE_UPSCALING
#endif

#if defined(UNITY_COMMON_INCLUDED)
	// Assume modern SRP
	#define SO_TEXTURE2D TEXTURE2D
	#define SO_TEXTURE2D_PARAM(tex, smp) TEXTURE2D_PARAM(tex, sampler##smp)
	#define SO_TEXTURE2D_ARGS(tex, smp) TEXTURE2D_ARGS(tex, sampler##smp)
	#define SO_SAMPLER(tex) SAMPLER(sampler##tex)
	#define SO_SAMPLE_TEXTURE2D(tex, smp, uv) SAMPLE_TEXTURE2D(tex, sampler##smp, uv)
	#define SO_SAMPLE_TEXTURE2D_LOD(tex, smp, uv, lod) SAMPLE_TEXTURE2D_LOD(tex, sampler##smp, uv, lod)
#else
	// Assume BRP
	#ifndef UNITY_CG_INCLUDED
		Error_UnknownRenderPipeline
	#endif
	#define SO_TEXTURE2D UNITY_DECLARE_TEX2D_NOSAMPLER
	#define SO_SAMPLER(smp) SamplerState sampler##smp;
	#define SO_SAMPLE_TEXTURE2D UNITY_SAMPLE_TEX2D_SAMPLER
	#define SO_SAMPLE_TEXTURE2D_LOD UNITY_SAMPLE_TEX2D_SAMPLER_LOD
	#undef TEXTURE_UPSCALING // Unsupported
#endif

#ifdef TEXTURE_UPSCALING
	#include "Upscaling.hlsl"
#endif

struct InstanceData
{
	float4x4 localToWorld;
#if defined(USE_MOTION_VECTORS)
	float4x4 prevLocalToWorld;
#endif
	float4 rect;
	float2 pivot;
	float4 color;
	float4 emissiveColor;
	float4 flashColor;
	float4 outlineColor;
	float3 gradientIndices;
	float3 transformAnimParams;
	float maskParam;
};

#define MAX_MASK_CHANNELS 32
float4x4 _SpriteObjectMaskArray[MAX_MASK_CHANNELS];

float GetMaskAlpha(float3 positionWS, float maskParam)
{
	if (!maskParam)
	{
		return 1.0;
	}
	int channel = round(abs(maskParam) - 1);
	float4x4 M = _SpriteObjectMaskArray[channel];
	if (!M._44)
	{
		return step(maskParam, 0);
	}
	float2 pos = abs(mul(M, float4(positionWS, 1)).xy * 2);
	if (maskParam > 0)
	{
		return all(pos <= 1);
	}
	else
	{
		return any(pos > 1);
	}
}

#if INSTANCING_ENABLED && defined(UNITY_INSTANCING_ENABLED)
StructuredBuffer<InstanceData> _InstanceData;
#endif

SO_TEXTURE2D(_MainTex);
SO_TEXTURE2D(_EmissiveTex);
SO_TEXTURE2D(_NormalTex);
SO_SAMPLER(_MainTex);
float4 _MainTex_TexelSize;

SO_TEXTURE2D(_SpriteTex);
SO_TEXTURE2D(_SpriteEmissiveTex);
SO_TEXTURE2D(_SpriteNormalTex);
SO_SAMPLER(_SpriteTex);
float4 _SpriteTex_TexelSize;

#if SPRITE_INSTANCING_USE_COMPRESSED_ATLASES
StructuredBuffer<float4> _CompressedColorsBuffer;
uint _CompressedColorsBufferCount;
StructuredBuffer<float4> _CompressedEmissiveColorsBuffer;
uint _CompressedEmissiveColorsBufferCount;
#if !SPRITE_INSTANCING_DISABLE_NORMAL_ATLAS
StructuredBuffer<float4> _CompressedNormalColorsBuffer;
uint _CompressedNormalColorsBufferCount;
#endif
#endif

SO_TEXTURE2D(_TransformAnimationTexture);
float4 _TransformAnimationTexture_TexelSize;
SO_SAMPLER(_transform_linear_clamp);

float4 _Rect;
float2 _Pivot;

float4 _Color;
float4 _EmissiveColor;
float4 _FlashColor;
float4 _OutlineColor;
float3 _GradientIndices;
float3 _TransformAnimParams;
float _MaskParam;

float _TransformAnimationTime;
float _EditorTime;
float _CompressedColorsMaxValue;

SO_SAMPLER(_gradient_point_clamp_sampler);

#if INSTANCING_ENABLED && defined(UNITY_INSTANCING_ENABLED)
	#define COLOR_TEXTURE _MainTex
	#define COLOR_SAMPLER _MainTex
	#define TEXEL_SIZE _MainTex_TexelSize
	#define EMISSIVE_TEXTURE _EmissiveTex
	#define EMISSIVE_SAMPLER _MainTex
	#define NORMAL_TEXTURE _NormalTex
	#define NORMAL_SAMPLER _MainTex
#else
	#define COLOR_TEXTURE _SpriteTex
	#define COLOR_SAMPLER _SpriteTex
	#define TEXEL_SIZE _SpriteTex_TexelSize
	#define EMISSIVE_TEXTURE _SpriteEmissiveTex
	#define EMISSIVE_SAMPLER _SpriteTex
	#define NORMAL_TEXTURE _SpriteNormalTex
	#define NORMAL_SAMPLER _SpriteTex
#endif

float4 GetUVRange(float4 rect)
{
	return float4(rect.xy, rect.xy + rect.zw) * TEXEL_SIZE.xyxy;
}

float2x2 RotationMatrixRadians(float radians)
{
	float s = sin(radians);
	float c = cos(radians);
	return float2x2( c, -s, s, c );
}
float2x2 RotationMatrixDegrees(float degrees)
{
	return RotationMatrixRadians(degrees * 0.01745329251);
}

void ApplyTransformAnimation(inout float4 vertex, float3 transformAnimParams, float time)
{
	float t = (time - transformAnimParams.y) / transformAnimParams.z;
	if (t >= 0 && t <= 1)
	{
		float y = (transformAnimParams.x + 0.5) * _TransformAnimationTexture_TexelSize.y;
		float4 transformAnim = SO_SAMPLE_TEXTURE2D_LOD(_TransformAnimationTexture, _transform_linear_clamp, float2(t, y), 0);
		vertex.x *= transformAnim.x;
		vertex.y *= transformAnim.y;
		vertex.xy = mul(RotationMatrixDegrees(transformAnim.z), vertex.xy);
	}
}

#if SPRITE_INSTANCING_USE_COMPRESSED_ATLASES
uint GetCompressedBufferIndex(float x, uint count)
{
    return uint(clamp(x * _CompressedColorsMaxValue, 0, float(count - 1)));
}
#endif

float2 GetUV(float2 uv, float4 rect) { return uv = (rect.xy + rect.zw * uv) * TEXEL_SIZE.xy; }
float4 GetColor(float2 uv, float4 uvRange = float4(0, 0, 1, 1))
{
#ifdef TEXTURE_UPSCALING
	float4 color = SAMPLE_TEXTURE2D_UPSCALED(SO_TEXTURE2D_ARGS(COLOR_TEXTURE, COLOR_SAMPLER), uv, TEXEL_SIZE, uvRange);
#else
	float4 color = SO_SAMPLE_TEXTURE2D(COLOR_TEXTURE, COLOR_SAMPLER, uv);
#endif
#if SPRITE_INSTANCING_USE_COMPRESSED_ATLASES
	color = _CompressedColorsBuffer[GetCompressedBufferIndex(color.r, _CompressedColorsBufferCount)];
#endif
	return color;
}
float4 GetEmissive(float2 uv, float4 uvRange = float4(0, 0, 1, 1))
{
#ifdef TEXTURE_UPSCALING
	float4 color = SAMPLE_TEXTURE2D_UPSCALED(SO_TEXTURE2D_ARGS(EMISSIVE_TEXTURE, EMISSIVE_SAMPLER), uv, TEXEL_SIZE, uvRange);
#else
	float4 color = SO_SAMPLE_TEXTURE2D(EMISSIVE_TEXTURE, EMISSIVE_SAMPLER, uv);
#endif
#if SPRITE_INSTANCING_USE_COMPRESSED_ATLASES
	color = _CompressedEmissiveColorsBuffer[GetCompressedBufferIndex(color.r, _CompressedEmissiveColorsBufferCount)];
#endif
	return color;
}
float3 GetNormal(float2 uv, float4 uvRange = float4(0, 0, 1, 1))
{
#if !SPRITE_INSTANCING_DISABLE_NORMAL_ATLAS
	#ifdef TEXTURE_UPSCALING
	float4 color = SAMPLE_TEXTURE2D_UPSCALED(SO_TEXTURE2D_ARGS(NORMAL_TEXTURE, NORMAL_SAMPLER), uv, TEXEL_SIZE, uvRange);
	#else
	float4 color = SO_SAMPLE_TEXTURE2D(NORMAL_TEXTURE, NORMAL_SAMPLER, uv);
	#endif
#if SPRITE_INSTANCING_USE_COMPRESSED_ATLASES
	color = _CompressedNormalColorsBuffer[GetCompressedBufferIndex(color.r, _CompressedNormalColorsBufferCount)];
#endif
	return UnpackNormal(color);
#else
    return float3(0, 0, 1);
#endif
}
float GetAlpha(float2 uv, float4 uvRange = float4(0, 0, 1, 1)) { return GetColor(uv, uvRange).a; }

#if INSTANCING_ENABLED && defined(UNITY_INSTANCING_ENABLED) || defined(SINGLE_RENDERER)
SO_TEXTURE2D(_GradientMapAtlas);
float4 _GradientMapAtlas_TexelSize;
#else
SO_TEXTURE2D(_GradientMap1);
SO_TEXTURE2D(_GradientMap2);
SO_TEXTURE2D(_GradientMap3);
#endif

#define GRADIENT_MAP_WIDTH 256

float s_tallMask;

float3 LinearToGamma(float3 x)
{
    return max(1.055 * pow(max(x, 0.0), 0.416666667) - 0.055, 0.0);
}

float3 SampleGradients(float3 color, float3 indices)
{
	float3 key = (LinearToGamma(color) * (GRADIENT_MAP_WIDTH - 1.0) + 0.5) / GRADIENT_MAP_WIDTH;
	float3 hasGradient = step(1e-5, color);
	if (any(indices >= 0))
	{
		color = 0.0;
	}
#if INSTANCING_ENABLED && defined(UNITY_INSTANCING_ENABLED) || defined(SINGLE_RENDERER)
	if (indices.x >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMapAtlas, _gradient_point_clamp_sampler, float2(key.r, (indices.x + 0.5) * _GradientMapAtlas_TexelSize.y), 0).rgb * hasGradient.x;
	}
	if (indices.y >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMapAtlas, _gradient_point_clamp_sampler, float2(key.g, (indices.y + 0.5) * _GradientMapAtlas_TexelSize.y), 0).rgb * hasGradient.y;
	}
	if (indices.z >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMapAtlas, _gradient_point_clamp_sampler, float2(key.b, (indices.z + 0.5) * _GradientMapAtlas_TexelSize.y), 0).rgb * hasGradient.z;
	}
#else
	if (indices.x >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMap1, _gradient_point_clamp_sampler, float2(key.r, 0), 0).rgb * hasGradient.x;
	}
	if (indices.y >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMap2, _gradient_point_clamp_sampler, float2(key.g, 0), 0).rgb * hasGradient.y;
	}
	if (indices.z >= 0)
	{
		color += SO_SAMPLE_TEXTURE2D_LOD(_GradientMap3, _gradient_point_clamp_sampler, float2(key.b, 0), 0).rgb * hasGradient.z;
	}
#endif
	return color;
}

float GetOutline(float2 uv, float4 rect, out float outlineSum)
{
	float result = 0.0;
	float2 pixel = uv * rect.zw;
	outlineSum = 0.0;
	float4 uvRange = GetUVRange(rect);
#if THICK_OUTLINE
	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			float2 localPixel = pixel + float2(x, y);
			float2 localUV = (rect.xy + localPixel) * TEXEL_SIZE.xy;
			float localAlpha = GetAlpha(localUV, uvRange) * all(localPixel > 0 && localPixel < rect.zw);
			outlineSum += localAlpha;
			result = max(result, localAlpha);
		}
	}
	outlineSum /= 9;
#else
	for (int x = -1; x <= 1; x += 2)
	{
		float2 localPixel = pixel + float2(x, 0);
		float2 localUV = (rect.xy + localPixel) * TEXEL_SIZE.xy;
		float localAlpha = GetAlpha(localUV, uvRange) * all(localPixel > 0 && localPixel < rect.zw);
		outlineSum += localAlpha;
		result = max(result, localAlpha);
	}
	for (int y = -1; y <= 1; y += 2)
	{
		float2 localPixel = pixel + float2(0, y);
		float2 localUV = (rect.xy + localPixel) * TEXEL_SIZE.xy;
		float localAlpha = GetAlpha(localUV, uvRange) * all(localPixel > 0 && localPixel < rect.zw);
		outlineSum += localAlpha;
		result = max(result, localAlpha);
	}
	outlineSum /= 4;
#endif
	return result;
}

float GetOutline(float2 uv, float4 rect)
{
	float outlineSum;
	return GetOutline(uv, rect, outlineSum);
}

#endif // SPRITE_INSTANCING_INCLUDED