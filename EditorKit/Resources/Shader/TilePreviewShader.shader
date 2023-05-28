Shader "EditorKit/TilePreview"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGBA)", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        _TopGenTex("Side Texture (RGBA)", 2D) = "white" {}
        _SideGenTex("Side Texture (RGBA)", 2D) = "white" {}
        
        _HasGenTex("Has generated textures", Integer) = 0

        _TopRect ("Top rect", Vector) = (0, 0, 1, 1)
        _SideRect ("Side rect", Vector) = (0, 0, 1, 1)

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Cutoff("Cutoff", Range(0.0, 1.0)) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_TopGenTex);
        SAMPLER(sampler_TopGenTex);

        TEXTURE2D(_SideGenTex);
        SAMPLER(sampler_SideGenTex);
        
        CBUFFER_START(UnityPerMaterial)

        int _HasGenTex;
        
        float4 _TopRect;
        float4 _SideRect;
        
        float4 _BaseMap_ST;
        float4 _BaseColor;

        float _Smoothness;
        float _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // Material Keywords

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            //#pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            #define _SPECULAR_COLOR // always on
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // URP Keywords
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // Note, v11 changes this to :
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING // v10+ only, renamed from "_MIXED_LIGHTING_SUBTRACTIVE"
            #pragma multi_compile _ SHADOWS_SHADOWMASK // v10+ only

            // Unity Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog

            // GPU Instancing (not supported)
            //#pragma multi_compile_instancing

            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
            // Structs
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;

                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float4 color : COLOR;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;

                half3 normalWS : TEXCOORD3;

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
					half4 fogFactorAndVertexLight	: TEXCOORD6; // x: fogFactor, yzw: vertex light
                #else
                half fogFactor : TEXCOORD6;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord 				: TEXCOORD7;
                #endif

                float4 color : COLOR;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
                //UNITY_VERTEX_OUTPUT_STEREO
            };

            // Textures, Samplers & Global Properties
            // (note, BaseMap, BumpMap and EmissionMap is being defined by the SurfaceInput.hlsl include)

            //  SurfaceData & InputData
            void InitalizeSurfaceData(Varyings IN, out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0; // avoids "not completely initalized" errors

                half4 baseMap;

                if (IN.uv.x < 0.5)
                {
                    IN.uv.x *= 2;
                    float2 newUV = _TopRect.xy + IN.uv * _TopRect.zw;
                    if ((_HasGenTex & 1) == 1)
                    {
                        baseMap = SAMPLE_TEXTURE2D(_TopGenTex, sampler_TopGenTex, newUV);
                    }else
                    {
                        baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, newUV);
                    }
                }
                else
                {
                    IN.uv.x = (IN.uv.x - 0.5f) * 2;
                    float2 newUV = _SideRect.xy + IN.uv * _SideRect.zw;
                    if ((_HasGenTex & 2) == 2)
                    {
                        baseMap = SAMPLE_TEXTURE2D(_SideGenTex, sampler_SideGenTex, newUV);
                    }else
                    {
                        baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, newUV);
                    }
                }

                half4 diffuse = baseMap * _BaseColor * IN.color;
                surfaceData.albedo = diffuse.rgb;
                surfaceData.occlusion = 1.0; // unused
                surfaceData.alpha = baseMap.a;

                surfaceData.specular = (half3)0;
                surfaceData.smoothness = _Smoothness;
            }

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0; // avoids "not completely initalized" errors

                inputData.positionWS = input.positionWS;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
                inputData.normalWS = input.normalWS;

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                viewDirWS = SafeNormalize(viewDirWS);
                inputData.viewDirectionWS = viewDirWS;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Fog
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
					inputData.fogCoord = input.fogFactorAndVertexLight.x;
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                #endif

                /* in v11/v12?, could use :
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
                    inputData.vertexLighting = half3(0, 0, 0);
                #endif
                */

                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
            }

            // Vertex Shader
            Varyings LitPassVertex(Attributes IN)
            {
                Varyings OUT;

                //UNITY_SETUP_INSTANCE_ID(IN);
                //UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS.xyz);


                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                half3 viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                OUT.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
					OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                OUT.fogFactor = fogFactor;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					OUT.shadowCoord = GetShadowCoord(positionInputs);
                #endif

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            // Fragment Shader
            half4 LitPassFragment(Varyings IN) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(IN);
                //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Setup SurfaceData
                SurfaceData surfaceData;
                InitalizeSurfaceData(IN, surfaceData);

                // Setup InputData
                InputData inputData;
                InitializeInputData(IN, surfaceData.normalTS, inputData);

                // Simple Lighting (Lambert & BlinnPhong)
                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData); // v12 only
                //half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData.albedo, half4(surfaceData.specular, 1), 
                //surfaceData.smoothness, surfaceData.emission, surfaceData.alpha);
                // See Lighting.hlsl to see how this is implemented.
                // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                //color.a = 1;
                return color;
            }
            ENDHLSL
        }

        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        //UsePass "Universal Render Pipeline/Lit/DepthOnly"

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            // Universal Pipeline Keywords
            // (v11+) This is used during shadow map generation to differentiate between directional and punctual (point/spot) light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }

        // DepthOnly, used for Camera Depth Texture (if cannot copy depth buffer instead, and the DepthNormals below isn't used)
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode"="DepthOnly"
            }

            ColorMask 0
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            
            ENDHLSL
        }

        // DepthNormals, used for SSAO & other custom renderer features that request it
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode"="DepthNormals"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"

            ENDHLSL
        }
    }
}