void SetMainLightDirection_float(float3 Direction, out float Out)
{
	#if defined(UNIVERSAL_LIGHTING_INCLUDED)
	_MainLightDirection = Direction;
	_MainLightDirectionEnabled = 1;
	#endif

	Out = 1.0;
}

void SetMainLightDirection_half(float3 Direction, out float Out)
{
	#if defined(UNIVERSAL_LIGHTING_INCLUDED)
	_MainLightDirection = Direction;
	_MainLightDirectionEnabled = 1;
	#endif

	Out = 1.0;
}

void SetMainLightRadiance_float(float Radiance, out float Out)
{
	#if defined(UNIVERSAL_LIGHTING_INCLUDED)
	_MainLightRadiance = Radiance;
	_MainLightRadianceEnabled = 1;
	#endif

	Out = 1.0;
}

void SetMainLightRadiance_half(half Radiance, out float Out)
{
	#if defined(UNIVERSAL_LIGHTING_INCLUDED)
	_MainLightRadiance = Radiance;
	_MainLightRadianceEnabled = 1;
	#endif

	Out = 1.0;
}

void GetMainLight_float(float3 WorldPos, out float3 Direction, out half3 Color, out float DistanceAttenuation, out float ShadowAttenuation)
{
	#ifdef SHADERGRAPH_PREVIEW
    	Direction = float3(0.5, 0.5, 0);
    	Color = 1;
    	DistanceAttenuation = 1;
    	ShadowAttenuation = 1;
	#elif defined(UNIVERSAL_LIGHTING_INCLUDED)
    	float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    	Light mainLight = GetMainLight(shadowCoord);

    	Direction = mainLight.direction;
    	Color = mainLight.color;
    	DistanceAttenuation = mainLight.distanceAttenuation;
    	#if VERSION_GREATER_EQUAL(10, 1)
			ShadowAttenuation = MainLightShadow(shadowCoord, WorldPos, half4(1,1,1,1), _MainLightOcclusionProbes);
		#else
			ShadowAttenuation = MainLightRealtimeShadow(shadowCoord);
		#endif
	#endif
}

void LuminosityBlend_float(float3 Base, float3 Blend, out float3 Out)
{
	float3 l = float3(0.3, 0.59, 0.11);            
    float dLum = dot(Blend, l);
    float sLum = dot(Base, l);
    float lum = sLum - dLum;
    float3 c = Blend + lum;
    float minC = min(min(c.x, c.y), c.z);
    float maxC = max(max(c.x, c.y), c.z);
    Out = minC < 0.0 ? sLum + ((c - sLum) * sLum) / (sLum - minC) : (maxC > 1.0 ? sLum + ((c - sLum) * (1.0 - sLum)) / (maxC - sLum) : c);
}

void LuminosityBlend_half(half3 Base, half3 Blend, out half3 Out)
{
	half3 l = half3(0.3, 0.59, 0.11);            
    half dLum = dot(Blend, l);
    half sLum = dot(Base, l);
    half lum = sLum - dLum;
    half3 c = Blend + lum;
    half minC = min(min(c.x, c.y), c.z);
    half maxC = max(max(c.x, c.y), c.z);
    Out = minC < 0.0 ? sLum + ((c - sLum) * sLum) / (sLum - minC) : (maxC > 1.0 ? sLum + ((c - sLum) * (1.0 - sLum)) / (maxC - sLum) : c);
}

void NormalizeZeroSafe_float(float3 In, out float3 Out)
{
	if (In.x == 0.0 && In.y == 0.0 && In.z == 0.0)
		Out = float3(0.0, 1.0, 0.0);
	else
		Out = normalize(In.xyz);
}

void NormalizeZeroSafe_half(half3 In, out half3 Out)
{
	if (In.x == 0.0 && In.y == 0.0 && In.z == 0.0)
		Out = half3(0.0, 1.0, 0.0);
	else
		Out = normalize(In.xyz);
}

void DivideZeroSafe_float(float A, float B, out float Out)
{
	if (B == 0.0)
		Out = 0.0;
	else
		Out = A / B;
}

void DivideZeroSafe_half(float A, float B, out float Out)
{
	if (B == 0.0)
		Out = 0.0;
	else
		Out = A / B;
}

void ElevationFromRGBTerrain_float(float3 RGB, float minElevation, out float Out)
{
	Out = minElevation + ((RGB.x * 255 * 65536.0 + RGB.y * 255 * 256 + RGB.z * 255) * 0.1);
}

void ElevationFromRGBTerrain_half(half3 RGB, float minElevation, out half Out)
{
	Out = minElevation + ((RGB.x * 255 * 65536.0 + RGB.y * 255 * 256 + RGB.z * 255) * 0.1);
}

void SharpenSampleTexture2D_float(UnityTexture2D Texture, float2 UV, SamplerState Sampler, float SharpenFactor, out float4 RGBA)
{
	float2 texelSize = float2(Texture.texelSize.z, Texture.texelSize.w);

    float4 up = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(0, 1) / texelSize);
    float4 left = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(-1, 0) / texelSize);
    float4 center = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
    float4 right = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(1, 0) / texelSize);
    float4 down = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(0, -1) / texelSize);

    // Return edge detection
    RGBA = (1.0 + 4.0 * SharpenFactor) * center - SharpenFactor * (up + left + right + down);
}

void SharpenSampleTexture2D_half(UnityTexture2D Texture, float2 UV, SamplerState Sampler, float SharpenFactor, out float4 RGBA)
{
	float2 texelSize = float2(Texture.texelSize.z, Texture.texelSize.w);

    float4 up = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(0, 1) / texelSize);
    float4 left = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(-1, 0) / texelSize);
    float4 center = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
    float4 right = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(1, 0) / texelSize);
    float4 down = SAMPLE_TEXTURE2D(Texture, Sampler, UV + float2(0, -1) / texelSize);

    // Return edge detection
    RGBA = (1.0 + 4.0 * SharpenFactor) * center - SharpenFactor * (up + left + right + down);
}

float GetRectangleVolumeMask(StructuredBuffer<float> customEffectBuffer, float4 worldPosition, int index)
{
	float4x4 worldToLocalMat = float4x4(
			float4(customEffectBuffer[index], customEffectBuffer[index + 1], customEffectBuffer[index + 2], customEffectBuffer[index + 3]),
			float4(customEffectBuffer[index + 4], customEffectBuffer[index + 5], customEffectBuffer[index + 6], customEffectBuffer[index + 7]),
			float4(customEffectBuffer[index + 8], customEffectBuffer[index + 9], customEffectBuffer[index + 10], customEffectBuffer[index + 11]),
			float4(0.0, 0.0, 0.0, 1.0)
			);
	float4 localPosition = mul(worldToLocalMat, worldPosition);
	return abs(localPosition.x) >= 1.0 || abs(localPosition.y) >= 1.0 || abs(localPosition.z) >= 1.0 ? 1.0 : 0.0;
}

StructuredBuffer<float> _CustomEffectsBuffer;
uint _CustomEffectsBufferDimensions;
void CustomEffects_float(float4 worldPosition, out float4 color, out float alpha)
{
	color = float4(1.0, 1.0, 1.0, 1.0);
	alpha = 1.0;

	for (uint index = 0 ; index < _CustomEffectsBufferDimensions ;)
	{
		int customEffectType = _CustomEffectsBuffer[index];
		index++;

		alpha = min(alpha, customEffectType == 0 ? GetRectangleVolumeMask(_CustomEffectsBuffer, worldPosition, index) : alpha);

		index += customEffectType == 0 ? 12 : 0;
	}
}

void CustomEffects_half(float4 worldPosition, out float4 color, out float alpha)
{
	color = float4(1.0, 1.0, 1.0, 1.0);
	alpha = 1.0;

	for (uint index = 0 ; index < _CustomEffectsBufferDimensions ;)
	{
		int customEffectType = _CustomEffectsBuffer[index];
		index++;

		alpha = min(alpha, customEffectType == 0 ? GetRectangleVolumeMask(_CustomEffectsBuffer, worldPosition, index) : alpha);

		index += customEffectType == 0 ? 12 : 0;
	}
}