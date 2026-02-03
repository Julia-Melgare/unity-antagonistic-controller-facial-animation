Shader "Hidden/OpticalFlowScale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Float) = 10
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Scale;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 flow = tex2D(_MainTex, i.uv);

                // Make motion visible
                flow = abs(flow) * _Scale;

                return saturate(flow);
            }
            ENDCG
        }
    }
}