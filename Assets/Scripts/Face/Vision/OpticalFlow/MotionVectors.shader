Shader "Unlit/MotionVectors"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"

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

            sampler2D _MotionVectorTexture;
            float4 _MotionVectorTexture_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MotionVectorTexture);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                /*float2 motion = tex2D(_MotionVectorTexture, i.uv).rg;
                float3 rgb = MotionVectorsToOpticalFlow(motion);
                return float4(rgb, 1);*/
                fixed4 col = tex2D(_MotionVectorTexture, i.uv);
                col.xy *= 100;
                return col;
            }
            ENDCG
        }
    }
}
