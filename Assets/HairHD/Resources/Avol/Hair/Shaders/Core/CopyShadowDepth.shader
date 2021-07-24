Shader "Hidden/Avol/CopyShadowDepth"
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

            fixed frag(v2f i, inout float depth : SV_Depth) : SV_Target
            {
                float hairDepth = tex2D(_MainTex, i.uv);
                depth = hairDepth;
                return  hairDepth;
            }

            ENDHLSL
        }
    }
}
