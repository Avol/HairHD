Shader "Avol/GatherShading"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" }
        Cull Off ZWrite On ZTest Always

     
        // NO MULTISAMPLING
        Pass
        {
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog


         
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define AA_OFF

            uniform sampler2D _MainTex;

            uniform sampler2D _Tangent;
            uniform sampler2D _Color;
            uniform sampler2D _ScatterTint;

            uniform sampler2D _FinalDepth;
            uniform sampler2D _PreviousDepth;

            #include "HairShading.cginc"
           // #include "HairShadingGatherBase.cginc"
           
            float4 frag(v2f i) : COLOR
            {
                return compFinal(i, 1);
            }

            ENDHLSL
        }


        // 2X MULTISAMPLING
        Pass
        {
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog



            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            uniform sampler2D _MainTex;

            uniform Texture2DMS<half4, 2> _Tangent;
            uniform Texture2DMS<half4, 2> _Color;
            uniform Texture2DMS<half4, 2> _ScatterTint;

            uniform Texture2DMS<half, 2> _FinalDepth;
            uniform sampler2D _PreviousDepth;

            uniform sampler2D _Test;

            #include "HairShading.cginc"
            //#include "HairShadingGatherBaseAA.cginc"


            float4 frag(v2f i) : SV_Target
            {
                const int AA = 2;
                return compFinal(i, AA);
            }

            ENDHLSL
        }
                  
        // 4X MULTISAMPLING
        Pass
        {
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog



            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            uniform sampler2D _MainTex;

            uniform Texture2DMS<half4, 4> _Tangent;
            uniform Texture2DMS<half4, 4> _Color;
            uniform Texture2DMS<half4, 4> _ScatterTint;

            uniform Texture2DMS<half, 4> _FinalDepth;
            uniform sampler2D _PreviousDepth;

            uniform sampler2D _Test;

            #include "HairShading.cginc"
           // #include "HairShadingGatherBaseAA.cginc"


            float4 frag(v2f i) : COLOR
            {
                const int AA = 4;
                return compFinal(i, AA);
            }

            ENDHLSL
        }

        // 8X MULTISAMPLING
        Pass
        {
            Tags{"LightMode" = "UniversalForward"}
            LOD 300

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog



            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            uniform sampler2D _MainTex;

            uniform Texture2DMS<half4, 8> _Tangent;
            uniform Texture2DMS<half4, 8> _Color;
            uniform Texture2DMS<half4, 8> _ScatterTint;

            uniform Texture2DMS<half, 8> _FinalDepth;
            uniform sampler2D _PreviousDepth;

            uniform sampler2D _Test;

            #include "HairShading.cginc"

            float4 frag(v2f i) : COLOR
            {
                const int AA = 8;
                return compFinal(i, AA);
            }

            ENDHLSL
        }
    }
}
