Shader "Avol/Hair"
{
    Properties{
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        ZWrite On
        Cull Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vertInternal
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HairGeometry.cginc"

            float frag(input i) : COLOR
            {
                return i.position.z;
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}