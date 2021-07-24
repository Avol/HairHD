Shader "Hidden/Test"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // No culling or depth
        //Cull Off ZWrite Off ZTest Always
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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


            uniform sampler2D _MainTex;

            fixed4 frag(v2f i, out float depth : SV_Depth) : SV_Target
            {
                depth = 1;
                return 1;
            }
            ENDCG
        }
    }
}
