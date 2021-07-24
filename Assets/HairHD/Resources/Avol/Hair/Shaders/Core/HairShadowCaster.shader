Shader "Avol/Hair"
{
    Properties{
    }
    SubShader
    {
        Cull Off ZWrite On ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HairGeometry.cginc"

            #pragma vertex vertShadowDir
            #pragma geometry geom
            #pragma fragment frag

            float4 frag(input i, out float depth : SV_Depth) : SV_Target
            {
                depth = i.position.z;
                return i.position.z;
            }

            ENDHLSL
        }

        // Spot / Point light
        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HairGeometry.cginc"

            #pragma vertex vertShadowSpot
            #pragma geometry geom
            #pragma fragment frag

            float4 frag(input i, out float depth : SV_Depth) : SV_Target
            {
                depth = i.position.z;
                return i.position.z;
            }

            ENDHLSL
        }
        
    }

    FallBack "Diffuse"
}
