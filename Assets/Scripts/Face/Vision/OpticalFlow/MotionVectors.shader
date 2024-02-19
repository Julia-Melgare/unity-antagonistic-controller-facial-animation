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

            float3 Hue(float H)
			{
			    float R = abs(H * 6 - 3) - 1;
			    float G = 2 - abs(H * 6 - 2);
			    float B = 2 - abs(H * 6 - 4);
			    return saturate(float3(R,G,B));
			}

            float3 HSVtoRGB(float3 HSV)
			{
			    return float3(((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z);
			}

            float _Sensitivity = 10;
			float3 MotionVectorsToOpticalFlow(float2 motion)
			{
				// Currently is based on HSV encoding from:
				//			"Optical Flow in a Smart Sensor Based on Hybrid Analog-Digital Architecture" by P. Guzman et al
				//			http://www.mdpi.com/1424-8220/10/4/2975

				// Analogous to http://docs.opencv.org/trunk/d7/d8b/tutorial_py_lucas_kanade.html
				// but might need to swap or rotate axis!

				float angle = atan2(-motion.y, -motion.x);
				float hue = angle / (UNITY_PI * 2.0) + 0.5;		// convert motion angle to Hue
				float value = length(motion) * _Sensitivity;  	// convert motion strength to Value
    			return HSVtoRGB(float3(hue, 1, value));		// HSV -> RGB
			}

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                /*float2 motion = tex2D(_MotionVectorTexture, i.uv).rg;
                float3 rgb = MotionVectorsToOpticalFlow(motion);
                return float4(rgb, 1);*/
                fixed4 col = tex2D(_MotionVectorTexture, i.uv);
                col.xy *= 10;
                return col;
            }
            ENDCG
        }
    }
}
