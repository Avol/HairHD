struct HairLightningData
{
    float3 LightColor;
    float3 AmbientColor;
    float3 ReflectionColor;
};

uniform float4 a_unity_SHAr;
uniform float4 a_unity_SHAg;
uniform float4 a_unity_SHAb;
uniform float4 a_unity_SHBr;
uniform float4 a_unity_SHBg;
uniform float4 a_unity_SHBb;
uniform float4 a_unity_SHC;

//RWTexture3D _StohasticCounterBuffer;

uniform             float       _SelfShadowRange;
uniform             float4      _SelfShadowCascades;
uniform             float       _SelfShadowingStrength;
uniform             int         _SelfShadowingSamples;
uniform             float       _SelfShadowJitter;
uniform             bool        _DeepOpacityShadow;

uniform             sampler2D   _DeepDepthMap;
uniform             sampler2D   _DeepOpacityMap;

uniform             sampler2D   _DeepAdditionalDepthMap;
uniform             sampler2D   _DeepAdditionalOpacityMap;
//uniform             float4x4    _DeepOpacityMapMatrix;


uniform				float		_CameraAspect;
uniform				float		_CameraFOV;
uniform             float3      _CameraRight;
uniform             float3      _CameraUp;
uniform             float3      _CameraForward;
uniform             float3      _CameraPosition;
uniform             float2      _CameraPlanes;
uniform             float4x4    _WorldMatrix;


uniform             int         _AmbientMode;
uniform             float       _AmbientIntensity;
uniform             float       _AmbientSkyboxAmount;
uniform             float4      _AmbientColor;


uniform             int                             _MainLightEnabled;
uniform             int                             _VisibleAdditionalLights;

uniform             float4                          _AdditionalLightHairData[MAX_VISIBLE_LIGHTS];

uniform             float4                          _DirectionaLightTextureBounds[4];
//uniform             float                           _DirectionaLightTextureAspect;

uniform             float4                          _AdditionalLightsTextureBounds[256];
//uniform             float                           _AdditionalLightsTextureAspect;

uniform             float2                          _DirectionalShadowTextureTexelSize;
uniform             float2                          _AdditionalShadowTextureTexelSizes[256];


float3 computeCameraRay(float2 screenPos)
{
    // calculate camera ray
    float2 uv = (screenPos.xy - 0.5f) * _CameraFOV;
    uv.x *= _CameraAspect;

    float3 right = _CameraRight;
    float3 up = _CameraUp;
    float3 front = _CameraForward;

    float3 ray = up * uv.y + right * uv.x + front;

    return ray;
}

float3 screenToWorld(float3 screenPos)
{
    float3 ray = computeCameraRay(screenPos.xy);

    float3 farPos = _CameraPosition + ray * _CameraPlanes.y;//_CameraPlanes.x + ray * (_CameraPlanes.y - _CameraPlanes.x);
    float3 nearPos = _CameraPosition + ray * _CameraPlanes.x;
    float3 worldPos = lerp(nearPos, farPos, screenPos.z);

    return worldPos;
}


//
//
float3 worldToScreen(float4x4 screenMatrix, float3 worldPos)
{
    float4 screenPos = mul(screenMatrix, float4(worldPos, 1));
    screenPos.xyz = screenPos.xyz * 0.5f + 0.5f;
    return screenPos.xyz;
}


// Transforms screen coordinate to world space position.
// @ m = inverse projection matrix of the camera.
float3 screenToWorld(float4x4 m, float3 v)
{
    float3 sPoint = v * 2.0 - 1.0;

    float4 wPoint = mul(m, float4(sPoint, 1));
    wPoint /= wPoint.w;

    return wPoint.xyz;
}


float random(float4 seed4)
{
    float product = dot(seed4, float4(12.9898, 78.233, 45.164, 94.673));
    return frac(sin(product) * 43758.5453);
}

half3 SampleSH2(half3 normalWS)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = a_unity_SHAr;
    SHCoefficients[1] = a_unity_SHAg;
    SHCoefficients[2] = a_unity_SHAb;
    SHCoefficients[3] = a_unity_SHBr;
    SHCoefficients[4] = a_unity_SHBg;
    SHCoefficients[5] = a_unity_SHBb;
    SHCoefficients[6] = a_unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}


HairLightningData ComputeLightning(float2 screenUV, float3 worldPosition, float4 tangentShift, float4 albedo, float4 scatter, Light light)
{
    HairLightningData data;

    float3 V = normalize(computeCameraRay(screenUV.xy));
    float3 N = normalize(lerp(-V, tangentShift.xyz, tangentShift.w));

    // Shade with light
    {
        float3 L = normalize(light.direction);

        float3 H = normalize(V + L);
        float3 T = normalize(tangentShift.xyz * 2.0f - 1.0f);

        float3 RColor = scatter.rgb * scatter.a;
        float  RShift = tangentShift.w * PI;

        float3 TTColor = scatter.rgb * scatter.a;
        float  TTShift = tangentShift.w * PI;

        float3 TRTColor = scatter.rgb * scatter.a;
        float  TRTShift = tangentShift.w * PI;

        float clampedSmoothness = clamp(1.0f - albedo.w, 1.0f / 255.0f, 1.0f) *1000.0f;

        float LT = dot(L, T);
        float VT = dot(V, T);
        float VL = dot(V, L);
        float theta_h = asin(LT) + asin(VT);
        float cosPhi_i = dot(normalize(L - LT * T), normalize(V - VT * T));

        // R (reflection) component
        float3 R = saturate(RColor * pow((cos(theta_h - RShift)), clampedSmoothness));

        // TT component
        float3 TT = saturate(TTColor * max(0.0, cosPhi_i) * pow(cos(theta_h - TTShift), clampedSmoothness));

        // TRT component
        float3 TRT = saturate(TRTColor * pow((cos(theta_h - TRTShift)), clampedSmoothness));

        // Kaya
        float3 Diffuse = saturate(albedo.rgb * sqrt(min(1.0, LT * LT)) + albedo.rgb);// *(1.0f - _Metallic));

        // compose
        data.LightColor = (Diffuse + R + TT + TRT) * light.color * light.distanceAttenuation;
    }

    data.AmbientColor = 0;
    data.ReflectionColor = 0;

    return data;
}


float3 ComputeDOM(float2 screenUV, float3 worldPosition, float3 shadowCoord)
{
    if (!_DeepOpacityShadow)
        return 1;

    float   shadow = 0;

    static float2 poissonDisk4[4] =
    {
      float2(-0.94201624, -0.39906216),
      float2(0.94558609, -0.76890725),
      float2(-0.094184101, -0.92938870),
      float2(0.34495938, 0.29387760)
    };


    static float2 poissonDisk16[16] =
    {
       float2(-0.94201624, -0.39906216),
       float2(0.94558609, -0.76890725),
       float2(-0.094184101, -0.92938870),
       float2(0.34495938, 0.29387760),
       float2(-0.91588581, 0.45771432),
       float2(-0.81544232, -0.87912464),
       float2(-0.38277543, 0.27676845),
       float2(0.97484398, 0.75648379),
       float2(0.44323325, -0.97511554),
       float2(0.53742981, -0.47373420),
       float2(-0.26496911, -0.41893023),
       float2(0.79197514, 0.19090188),
       float2(-0.24188840, 0.99706507),
       float2(-0.81409955, 0.91437590),
       float2(0.19984126, 0.78641367),
       float2(0.14383161, -0.14100790)
    };


    half        cascadeIndex            = ComputeCascadeIndex(worldPosition);
    float4x4    worldToShadowMatrix     = _MainLightWorldToShadow[cascadeIndex];

    float   projectionRange = worldToShadowMatrix._m12;
    float4  cascadeRange = _SelfShadowCascades * abs(projectionRange) * _SelfShadowRange;


    float   jitterFactor = _SelfShadowJitter * abs(projectionRange) * _SelfShadowRange;

    // TODO: add cascade quadrant to clamp into a cascade texture quad when using poissonDisk.
    // TODO: add jitter intensity based on texture resolution.
    [unroll]
    for (int i = 0; i < _SelfShadowingSamples && i < 16; i++)
    {
        int     index = int(16.0f * random(float4(screenUV.xyy, i))) % 16.0f;

        float2  offset = poissonDisk16[index];
        //offset.x *= _DirectionaLightTextureAspect;
        offset.xy *= _DirectionalShadowTextureTexelSize;

        float2  samplePoint = shadowCoord.xy + offset * jitterFactor;
        samplePoint.x = clamp(samplePoint.x, _DirectionaLightTextureBounds[cascadeIndex].x, _DirectionaLightTextureBounds[cascadeIndex].x + _DirectionaLightTextureBounds[cascadeIndex].z);
        samplePoint.y = clamp(samplePoint.y, _DirectionaLightTextureBounds[cascadeIndex].y, _DirectionaLightTextureBounds[cascadeIndex].y + _DirectionaLightTextureBounds[cascadeIndex].w);

        float   deepDepth       = tex2D(_DeepDepthMap, samplePoint).r;
        float4  deepOpacityMap  = tex2D(_DeepOpacityMap, samplePoint);

        if (shadowCoord.z < deepDepth)
        {
            if (shadowCoord.z > deepDepth - cascadeRange.x)
            { 
                float coeff = ((deepDepth - shadowCoord.z) / cascadeRange.x);
                shadow += deepOpacityMap.r *coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.y)
            {
                float coeff = ((deepDepth - shadowCoord.z - cascadeRange.x) / (cascadeRange.y - cascadeRange.x));
                shadow += deepOpacityMap.r + deepOpacityMap.g *coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.z)
            {
                float coeff = ((deepDepth - shadowCoord.z - cascadeRange.y) / (cascadeRange.z - cascadeRange.y));
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b *coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.w)
            {
                float coeff = (deepDepth - shadowCoord.z - cascadeRange.z) / (cascadeRange.w - cascadeRange.z);
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b + deepOpacityMap.a *coeff;
            }
            else
            {
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b + deepOpacityMap.a;
            }
        }
    }

    shadow /= _SelfShadowingSamples;

    shadow = 1.0f - shadow * _SelfShadowingStrength;

    return saturate(shadow);
}



float LinearDepth(float z, float near, float far)
{
    float x = (1.0 - far / near);
    float y = far / near;
    return 1.0 / (x * z + y);
}


float3 ComputeDOMPerspective(int lightIndex, float3 lightDirection, float2 screenUV, float3 worldPosition, float4 additionalLightHairData)
{
    // get shadow slice index.
    half4 shadowParams = GetAdditionalLightShadowParams(lightIndex);

    int shadowSliceIndex = shadowParams.w;

    if (shadowSliceIndex < 0)
        return 1.0;

    half isPointLight = shadowParams.z;

    if (isPointLight)
    {
        float cubemapFaceId = CubeMapFaceID(-lightDirection);
        shadowSliceIndex += cubemapFaceId;
    }


    float4x4 mat = _AdditionalLightsWorldToShadow[shadowSliceIndex];
    float4 shadowCoord = mul(mat, float4(worldPosition, 1.0f));

    shadowCoord.xyz /= shadowCoord.w;
    shadowCoord.z = 1.0f - LinearDepth(1.0f - shadowCoord.z, additionalLightHairData.x, additionalLightHairData.y);


    if (!_DeepOpacityShadow)
        return 1;

    float   shadow = 0;

    static float2 poissonDisk4[4] =
    {
      float2(-0.94201624, -0.39906216),
      float2(0.94558609, -0.76890725),
      float2(-0.094184101, -0.92938870),
      float2(0.34495938, 0.29387760)
    };


    static float2 poissonDisk16[16] =
    {
       float2(-0.94201624, -0.39906216),
       float2(0.94558609, -0.76890725),
       float2(-0.094184101, -0.92938870),
       float2(0.34495938, 0.29387760),
       float2(-0.91588581, 0.45771432),
       float2(-0.81544232, -0.87912464),
       float2(-0.38277543, 0.27676845),
       float2(0.97484398, 0.75648379),
       float2(0.44323325, -0.97511554),
       float2(0.53742981, -0.47373420),
       float2(-0.26496911, -0.41893023),
       float2(0.79197514, 0.19090188),
       float2(-0.24188840, 0.99706507),
       float2(-0.81409955, 0.91437590),
       float2(0.19984126, 0.78641367),
       float2(0.14383161, -0.14100790)
    };


    float   projectionRange = additionalLightHairData.y;
    float4  cascadeRange = _SelfShadowCascades / abs(projectionRange) * _SelfShadowRange;


    // TODO: fix the jitter thing - incorrect alson in directional light map.
    float   jitterFactor = _SelfShadowJitter;// *abs(projectionRange)* _SelfShadowRange;


    // TODO: add individual light texture clamp into a texture when using poissonDisk.

    int samples = 0;
    for (int i = 0; /*i < _SelfShadowingSamples &&*/ i < 16; i++)
    {
        int     index = int(16.0f * random(float4(screenUV.xyy, i))) % 16.0f;
         

        float2  offset = poissonDisk16[index];

        offset.xy *= _AdditionalShadowTextureTexelSizes[shadowSliceIndex];
        //offset.x *= _DirectionaLightTextureAspect;

        float2  samplePoint     = shadowCoord.xy + offset * jitterFactor;
        float4  textureBounds   = _AdditionalLightsTextureBounds[shadowSliceIndex];
        samplePoint.x = clamp(samplePoint.x, textureBounds.x, textureBounds.x + textureBounds.z);
        samplePoint.y = clamp(samplePoint.y, textureBounds.y, textureBounds.y + textureBounds.w);

        float   deepDepth       = 1.0f - LinearDepth(1.0f - tex2D(_DeepAdditionalDepthMap, samplePoint).r, additionalLightHairData.x, additionalLightHairData.y);
        float4  deepOpacityMap  = tex2D(_DeepAdditionalOpacityMap, samplePoint);

        if (shadowCoord.z < deepDepth)
        {
            if (shadowCoord.z > deepDepth - cascadeRange.x)
            {
                float coeff = ((deepDepth - shadowCoord.z) / cascadeRange.x);
                shadow += deepOpacityMap.r * coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.y)
            {
                float coeff = ((deepDepth - shadowCoord.z - cascadeRange.x) / (cascadeRange.y - cascadeRange.x));
                shadow += deepOpacityMap.r + deepOpacityMap.g * coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.z)
            {
                float coeff = ((deepDepth - shadowCoord.z - cascadeRange.y) / (cascadeRange.z - cascadeRange.y));
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b * coeff;
            }
            else if (shadowCoord.z > deepDepth - cascadeRange.w)
            {
                float coeff = (deepDepth - shadowCoord.z - cascadeRange.z) / (cascadeRange.w - cascadeRange.z);
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b + deepOpacityMap.a * coeff;
            }
            else
            {
                shadow += deepOpacityMap.r + deepOpacityMap.g + deepOpacityMap.b + deepOpacityMap.a;
            }
        }
    }

    shadow /= _SelfShadowingSamples;

    shadow = 1.0f - shadow * _SelfShadowingStrength;

    return saturate(shadow);
}


float3 ComputeAmbient(float2 screenUV, float4 tangentShift, float4 albedo)
{
    float3 V = normalize(computeCameraRay(screenUV.xy));
    float3 N = normalize(lerp(-V, tangentShift.xyz, tangentShift.w));

    // flat color
    if (_AmbientMode == 3)
        return albedo.rgb * _AmbientColor;// *_AmbientTint.rgb* _AmbientTint.a;

    // skybox
    else if (_AmbientMode == 0)
        return albedo.rgb * SampleSH2(N);

    // equator
    else if (_AmbientMode == 1)
        return albedo.rgb * half3(a_unity_SHAr.w, a_unity_SHAg.w, a_unity_SHAb.w);

    return 0;
}

float3 ComputeShading(float2 screenUV, float3 worldPosition, float4 tangentShift, float4 albedo, float4 scatter, Light light)
{
    HairLightningData lightningData = ComputeLightning(screenUV, worldPosition, tangentShift, albedo, scatter, light);

    lightningData.LightColor *= light.shadowAttenuation;

    return lightningData.LightColor;
}


struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv             : TEXCOORD0;
    float4 vertex         : SV_POSITION;
    float4 shadowCoord    : TEXCOORD6;
};

v2f vert(appdata v)
{
    v2f o;

    float4 vert = v.vertex;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex);

    o.vertex = vertexInput.positionCS;
    o.uv = v.uv;
    o.shadowCoord = GetShadowCoord(vertexInput);

    return o;
}


/*real ComputeFogFactorZ0ToFar(float z)
{
#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
    return real(fogFactor);
#elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return real(unity_FogParams.x * z);
#else
    return real(0.0);
#endif
}*/

float4 compFinal(v2f i, int AA)
{
    uint2   UV = i.uv * _ScreenParams.xy;

    float3  finalColor      = 0;
    int     sampleCoverage  = 0;
    float   cameraDepth     = tex2D(_PreviousDepth, i.uv).r;



    [fastopt]
    for (int k = 0; k < AA && k < 8; k++)
    {
        // TANGENT + SHIFT
        float4  tangentShift = 0; 
#ifdef AA_OFF
        tangentShift = tex2D(_Tangent, i.uv);
#else
        tangentShift = _Tangent.Load(UV, k);
#endif

        if (tangentShift.x != 0 || tangentShift.y != 0 || tangentShift.z != 0)
        {

            // DEPTH
            float   depth = 0;
#ifdef AA_OFF
            depth = tex2D(_FinalDepth, i.uv).r;
#else
            depth = _FinalDepth.Load(UV, k);
#endif
            


            // COLOR + ROUGHNESS
            float4  albedo = 0;
#ifdef AA_OFF
            albedo = tex2D(_Color, i.uv);
#else
            albedo = _Color.Load(UV, k);
#endif

            // Scatter Tint
            float4  scatter = 0;
#ifdef AA_OFF
            scatter = tex2D(_ScatterTint, i.uv);
#else
            scatter = _ScatterTint.Load(UV, k);
#endif


            float3  wPos    = screenToWorld(_WorldMatrix, float3(i.uv, 1.0f - depth));

            float3 sampleColor = 0;

            // main light
            if (_MainLightEnabled)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(wPos);

                Light mainLight = GetMainLight(shadowCoord);
                mainLight.distanceAttenuation = 1;
                mainLight.shadowAttenuation = lerp(1.0f, mainLight.shadowAttenuation, GetMainLightShadowStrength());

                float3 DOM = ComputeDOM(UV, wPos, shadowCoord);
                sampleColor += ComputeShading(UV, wPos, tangentShift, albedo, scatter, mainLight) * DOM;
            }

            // additional lights.
            {
                for (int i = 0; i < _VisibleAdditionalLights && i < 16; i++)
                {
                    Light light = GetAdditionalPerObjectLight(i, wPos);
 
                    if (light.distanceAttenuation != 0)
                    {
                        float4 shadowMask = 0;

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                        half4 occlusionProbeChannels = _AdditionalLightsBuffer[i].occlusionProbeChannels;
#else
                        half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[i];
#endif
                        light.shadowAttenuation = AdditionalLightShadow(i, wPos, light.direction, shadowMask, occlusionProbeChannels);

                        float3 DOM = ComputeDOMPerspective(i, light.direction, UV, wPos, _AdditionalLightHairData[i]);
                        finalColor += ComputeShading(UV, wPos, tangentShift, albedo, scatter, light) * DOM;
                    }
                }
            }

            // compute ambient.
            sampleColor += ComputeAmbient(UV, tangentShift, albedo);


            // compute fog.
            float   viewZ         = length(_WorldSpaceCameraPos - wPos);
            float   nearToFarZ    = max(viewZ - _ProjectionParams.y, 0);
            half    fogFactor     = ComputeFogFactorZ0ToFar(nearToFarZ);
            sampleColor = MixFog(sampleColor, fogFactor);


            // add to final color.
            finalColor += sampleColor;

            sampleCoverage++;
            
            // TODO: make this work for multi hair rendering.
            // if (depth > cameraDepth)
            // {
            // }
        }
    }

    finalColor /= AA;

    float4 prev = tex2D(_MainTex, i.uv);
    float4 current = float4(finalColor, sampleCoverage / (float)AA);
    prev *= (1.0f - current.a);

    return prev + current;
}