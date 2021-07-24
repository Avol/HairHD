Shader "Hidden/Avol/HairCompose"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite On ZTest On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile   MSAA_0 MSAA_1 MSAA_2 MSAA_3
            #pragma multi_compile   COMPARE_DEPTH

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            uniform sampler2D   _MainTex;
        
            uniform sampler2D   _HairColorRT;
            uniform sampler2D   _CameraDepthAttachment;

#ifdef MSAA_0
            uniform sampler2D   _HairDepth;
#endif

#ifdef MSAA_1
            uniform Texture2DMS<float4, 2>   _HairDepth;
#endif

#ifdef MSAA_2
            uniform Texture2DMS<float4, 4>   _HairDepth;
#endif

#ifdef MSAA_3
            uniform Texture2DMS<float4, 8>   _HairDepth;
#endif

            uniform sampler2D               _CameraDepthTexture;

            fixed4 frag(v2f i, inout float depth : SV_Depth) : SV_Target
            {
                float hairDepth = 0;
            
                #ifdef MSAA_0
                    hairDepth = tex2D(_HairDepth, i.uv);
                #endif

                #ifdef MSAA_1
                    hairDepth = 0;
                    for (int c = 0; c < 2; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                #ifdef MSAA_2
                    hairDepth = 0;
                    for (int c = 0; c < 4; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                #ifdef MSAA_3
                    hairDepth = 0;
                    for (int c = 0; c < 8; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                float4  source      = tex2D(_MainTex, i.uv);
                float   cameraDepth = tex2D(_CameraDepthAttachment, i.uv).r;

                // only needed in deferred and when writing to separate depth texture.
                #ifdef COMPARE_DEPTH
                    if (cameraDepth > hairDepth)
                    {
                        depth = cameraDepth;
                        return source;
                    }
                #endif
                
                            
                float4  color       = tex2D(_HairColorRT, i.uv);

                if (hairDepth > depth)
                    depth = hairDepth;

                return float4(source.rgb * (1.0f - color.a) + color.rgb, 1.0f);
            }

            ENDHLSL
        }


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile   MSAA_0 MSAA_1 MSAA_2 MSAA_3
            #pragma shader_feature  DEFERRED

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
           // #include "HairShading.cginc"


#ifdef MSAA_0
            uniform sampler2D   _HairDepth;
#endif

#ifdef MSAA_1
            uniform Texture2DMS<float4, 2>   _HairDepth;
#endif

#ifdef MSAA_2
            uniform Texture2DMS<float4, 4>   _HairDepth;
#endif

#ifdef MSAA_3
            uniform Texture2DMS<float4, 8>   _HairDepth;
#endif

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed frag(v2f i, inout float depth : SV_Depth) : SV_Target
            {
                float hairDepth = 0;

                #ifdef MSAA_0
                    hairDepth = tex2D(_HairDepth, i.uv);
                #endif

                #ifdef MSAA_1
                    hairDepth = 0;
                    for (int c = 0; c < 2; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                #ifdef MSAA_2
                    hairDepth = 0;
                    for (int c = 0; c < 4; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                #ifdef MSAA_3
                    hairDepth = 0;
                    for (int c = 0; c < 8; c++)
                    {
                        float depth = _HairDepth.Load(i.uv * _ScreenParams.xy, c);
                        if (hairDepth < depth)
                            hairDepth = depth;
                    }
                #endif

                 /*
                float4  source = tex2D(_MainTex, i.uv);
                float   cameraDepth = tex2D(_CameraDepthTexture, i.uv).r;*/

                 if (hairDepth > depth)
                    depth = hairDepth;

                 return 1;
            }

            ENDHLSL
        }
    }
}
