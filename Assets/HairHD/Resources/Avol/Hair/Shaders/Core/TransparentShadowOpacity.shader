Shader "Avol/Hair"
{
    Properties {
    }
    SubShader
    {    
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        ZWrite Off
        Cull Off
    
        BlendOp Add, Add
        Blend One OneMinusSrcColor, One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex vertShadowDir
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		    #include "HairGeometry.cginc"

            uniform sampler2D   _DepthMap;
            uniform float       _Opacity;
            uniform float4      _LayerDistribution;
            uniform float       _SelfShadowRange;

            uniform float2      _Resolution;

            float4 frag(input i) : SV_Target
            {
                float4x4    worldToShadowMatrix = _MainLightWorldToShadow[_Cascade];

                float   projectionRange     = worldToShadowMatrix._m12;
                float4  cascadeRange        = _LayerDistribution * abs(projectionRange) * _SelfShadowRange;

                float4 screenPos = ComputeScreenPos(float4(i.position.xyz, 1));
                float2 screenUV = screenPos.xy / screenPos.w;

                screenUV.xy         /= _Resolution.xy;
                screenUV.xy         = screenUV.xy * 2.0f;
                screenUV.y          = -screenUV.y;

                float depth         = tex2D(_DepthMap, float2(screenUV.x, screenUV.y)).r;
                float opacityDepth  = i.position.z;

                float4 distribution = 0;

                if (opacityDepth > depth - cascadeRange.x)
                {
                    distribution.x = _Opacity;
                }
                else if (opacityDepth > depth - cascadeRange.y)
                {
                    distribution.y = _Opacity;
                }
                else if (opacityDepth > depth - cascadeRange.z)
                {
                    distribution.z = _Opacity;
                }
                else if (opacityDepth > depth - cascadeRange.w)
                {
                    distribution.w = _Opacity;
                }

                return distribution;
            }

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex vertShadowSpot
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HairGeometry.cginc"

            uniform sampler2D   _DepthMap;
            uniform float       _Opacity;
            uniform float4      _LayerDistribution;
            uniform float       _SelfShadowRange;

            uniform float2      _Resolution;

            uniform float       _NearClip;
            uniform float       _FarClip;

            float LinearDepth(float z)
            {
                float x = (1.0 - _FarClip / _NearClip);
                float y = _FarClip / _NearClip;
                return 1.0 / (x * z + y);
            }

            float4 frag(input i) : SV_Target
            {
                float4  cascadeRange = _LayerDistribution / _FarClip * _SelfShadowRange;

                /*float4 screenPos = ComputeScreenPos(float4(i.position.xyz, 1));
                float2 screenUV = screenPos.xy / screenPos.w;

                screenUV.xy /= _Resolution.xy;
                screenUV.xy = screenUV.xy * 2.0f;
                screenUV.y = -screenUV.y;*/

                float2 screenPos    = i.position.xy / _Resolution.xy;

                float depth             = 1.0f - LinearDepth(1.0f - tex2D(_DepthMap, screenPos).r);
                float opacityDepth      = LinearDepth(1.0f - i.position.z);

                float4 distribution = 0;
                
                if (opacityDepth > depth - cascadeRange.x)
                {
                    distribution.x = _Opacity;
                }
                else if (opacityDepth > depth - cascadeRange.y)
                {
                    distribution.y = _Opacity;
                }
                else if (opacityDepth > depth - cascadeRange.z)
                {
                    distribution.z = _Opacity;
                }
                else
                {
                    distribution.w = _Opacity;
                }
                
                return distribution;
            }
                 
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}