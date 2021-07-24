Shader "Avol/HairGeometryGather"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True"}

        ZWrite On
        Cull Back
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HairGeometry.cginc"

            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            uniform float _Transparency;
            uniform float _Roughness;
            uniform float _Shift;
            uniform float4 _ScatterTint;

            struct FragOutput
            {
                half4    tangent        : SV_Target1;
                half4    color          : SV_Target2;
                half4    scatterTint    : SV_Target3;
                float    depth          : SV_Target4;
            };

            // adapted from https://www.shadertoy.com/view/4djSRW
            float hash13(float3 p3)
            {
                p3 = frac(p3 * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            FragOutput frag(input i, out uint coverage : SV_Coverage) : SV_Target
            {
                //float4 color        = float4( ComputeShading( screenPos, i.tanget.xyz, i.color.rgb), 1);
                // screenUV.xy = screenUV.xy / screenUV.w;
                // screenUV.xy /= _ScreenParams.xy;
                // screenUV.xy = screenUV.xy * 2.0f;
                // screenUV.y = -screenUV.y;


               /* float noise = hash13(screenPos.xyz - frac(_Time.y * float3(0.5, 1.0, 2.0)));

                int MSAA = 8;
                float4 col = 1;
                col.a = saturate(col.a * ((float)(MSAA + 1) / (float)(MSAA)) - (noise / (float)(MSAA)));

                int mask = (240 >> int(col.a * float(MSAA) + 0.5)) & 15;
                int shift = (int(noise * float(MSAA - 1))) & (MSAA - 1);*/

                // for (int i = 0; i < MSAA; i++)
                //     coverage = coverage | 1;// (((mask << MSAA) | mask) >> shift) & 15;

                int stoshatic   = ceil(_Transparency * 7) + 1;
                int offset      = (i.position.z) % 7;

                coverage = 1;
                for (int c = 1; c < stoshatic + 1; c++)
                {
                    int index = c + offset;
                    if (index > 8)
                    {
                        index -= 8;
                        index += 1;
                    }

                    int bit = pow(2, index);
                    coverage = coverage | bit;
                }
                
                FragOutput output;
    
                output.tangent      = float4(i.tanget.xyz * 0.5f + 0.5f, _Shift);
                output.color        = float4(i.color.rgb, _Roughness);
                output.scatterTint  = float4(_ScatterTint);
                output.depth        = i.position.z;
    
                return output;
            }

            ENDHLSL
        }
    }
}