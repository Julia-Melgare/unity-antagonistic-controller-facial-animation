Shader "su_Double_Clip"
{
    Properties
    {
        _TexColor("Tint Color", Color) = (1,1,1,1)
        _Texture("Texture", 2D) = "gray" {}
        _ClipValue("Alpha Cutoff", Range(0,1)) = 0.5

        // LitInput.hlsl / pass includes require these to exist
        [HideInInspector] _BaseMap("Base Map", 2D) = "white" {}
        [HideInInspector] _BaseColor("Base Color", Color) = (1,1,1,1)
        [HideInInspector] _Cutoff("Cutoff", Range(0,1)) = 0.5
        [HideInInspector] _Smoothness("Smoothness", Range(0,1)) = 0.5
        [HideInInspector] _Metallic("Metallic", Range(0,1)) = 0.0
        [HideInInspector] _BumpMap("Bump Map", 2D) = "bump" {}
        [HideInInspector] _BumpScale("Bump Scale", Float) = 1.0
        [HideInInspector] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        [HideInInspector] _EmissionMap("Emission Map", 2D) = "white" {}
        [HideInInspector] _OcclusionMap("Occlusion", 2D) = "white" {}
        [HideInInspector] _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0
        [HideInInspector] _MetallicGlossMap("Metallic Gloss", 2D) = "white" {}
        [HideInInspector] _SpecColor("Spec Color", Color) = (1,1,1,1)
        [HideInInspector] _SpecGlossMap("Spec Gloss Map", 2D) = "white" {}
        [HideInInspector] _GlossMapScale("Gloss Map Scale", Range(0,1)) = 1.0
        [HideInInspector] _SmoothnessTextureChannel("Smoothness Channel", Float) = 0
        [HideInInspector] _DetailMask("Detail Mask", 2D) = "white" {}
        [HideInInspector] _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        [HideInInspector] _DetailNormalMap("Detail Normal", 2D) = "bump" {}
        [HideInInspector] _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        [HideInInspector] _ParallaxMap("Parallax", 2D) = "black" {}
        [HideInInspector] _Parallax("Parallax Scale", Range(0.005,0.08)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "AlphaTest"
        }

        Cull Off
        ZWrite On
        ZTest LEqual

        // ------------------------------------------------------------------
        // Forward Lit Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Texture); SAMPLER(sampler_Texture);

            // _Texture_ST and _ClipValue live outside LitInput's CBUFFER
            // so we extend it here — note LitInput.hlsl already opened
            // UnityPerMaterial; we cannot reopen it, so we use separate uniforms
            float4 _Texture_ST;
            half4  _TexColor;
            half   _ClipValue;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInput.positionCS;
                output.positionWS = posInput.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.uv         = TRANSFORM_TEX(input.uv, _Texture);
                output.fogFactor  = ComputeFogFactor(posInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 texSample = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                clip(texSample.a - _ClipValue);

                half3 albedo = texSample.rgb * _TexColor.rgb;

                half3 normalWS = facing >= 0.0
                    ? normalize(input.normalWS)
                    : normalize(-input.normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half  NdotL     = saturate(dot(normalWS, mainLight.direction));
                half3 color     = albedo * (SampleSH(normalWS)
                                + mainLight.color * NdotL * mainLight.shadowAttenuation);

                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; ++i)
                {
                    Light light  = GetAdditionalLight(i, input.positionWS);
                    half  NdotLi = saturate(dot(normalWS, light.direction));
                    color += albedo * light.color * NdotLi
                           * light.distanceAttenuation * light.shadowAttenuation;
                }

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow Caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth Only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
