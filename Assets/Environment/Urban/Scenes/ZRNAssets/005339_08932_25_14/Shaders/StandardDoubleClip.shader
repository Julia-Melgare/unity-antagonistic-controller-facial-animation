Shader "StandardDoubleClip"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _BumpScale("Normal Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _ParallaxScale("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}

        // Smoothness source required by LitInput.hlsl
        [HideInInspector] _SmoothnessTextureChannel("Smoothness Channel", Float) = 0
        [HideInInspector] _SpecColor("Spec Color", Color) = (1,1,1,1)
        [HideInInspector] _SpecGlossMap("Spec Gloss Map", 2D) = "white" {}
        [HideInInspector] _GlossMapScale("Gloss Map Scale", Range(0,1)) = 1.0
        [HideInInspector] _Glossiness("Glossiness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 300

        // ------------------------------------------------------------------
        // Forward Lit Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles

            #pragma vertex   LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _DETAIL_MULX2 _DETAIL_SCALED

            // LitInput.hlsl must be included before Lighting.hlsl in this pass
            // so that SurfaceData helpers are available
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 texcoord   : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float4 tangentWS  : TEXCOORD4; // xyz = tangent, w = sign
                float3 viewDirWS  : TEXCOORD5;
                half4  fogFactorAndVertexLight : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInput  = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                half  fogFactor   = ComputeFogFactor(vertexInput.positionCS.z);

                output.uv         = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;
                // Store tangent + bitangent sign in w (input.tangentOS.w holds the sign)
                output.tangentWS  = float4(normalInput.tangentWS, input.tangentOS.w);
                output.viewDirWS  = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                output.positionCS = vertexInput.positionCS;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            half4 LitPassFragment(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;

                #if defined(_PARALLAXMAP)
                    half3 viewDirTS = GetViewDirectionTangentSpace(
                        input.tangentWS, input.normalWS, input.viewDirWS);
                    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap),
                        viewDirTS, _ParallaxScale, uv);
                #endif

                // Use LitInput helpers — defined in LitInput.hlsl
                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(uv, surfaceData);

                #if defined(_ALPHATEST_ON)
                    clip(surfaceData.alpha - _Cutoff);
                #endif

                // Reconstruct normal WS with back-face flip for double-sided
                // tangentWS.w holds the bitangent sign stored in the vertex shader
                float  sgn       = facing >= 0.0 ? input.tangentWS.w : -input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3  normalWS  = TransformTangentToWorld(surfaceData.normalTS,
                    half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
                normalWS = NormalizeNormalPerPixel(normalWS);
                surfaceData.normalTS = normalWS; // not used further but keeps struct consistent

                InputData inputData = (InputData)0;
                inputData.positionWS              = input.positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = SafeNormalize(input.viewDirWS);
                inputData.fogCoord                = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting           = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI                 = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask              = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb   = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow Caster — LitInput.hlsl before ShadowCasterPass.hlsl
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
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
        // Depth Only — LitInput.hlsl before DepthOnlyPass.hlsl
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
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

        // ------------------------------------------------------------------
        // Meta — LitInput.hlsl before LitMetaPass.hlsl
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex   UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
