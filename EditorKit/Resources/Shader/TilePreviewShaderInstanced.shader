Shader "EditorKit/TilePreviewInstanced"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGBA)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _TopGenTex("Side Texture (RGBA)", 2D) = "white" {}
        _SideGenTex("Side Texture (RGBA)", 2D) = "white" {}

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

        struct meshData
        {
            int instId;
            float3 pos;
            float4 topRect;
            float4 sideRect;
            int viewFlags;
        };

        TEXTURE2D(_TopGenTex);
        SAMPLER(sampler_TopGenTex);

        TEXTURE2D(_SideGenTex);
        SAMPLER(sampler_SideGenTex);

        CBUFFER_START(UnityPerMaterial)

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

            #pragma target 4.5

            #if SHADER_TARGET >= 45
            StructuredBuffer<meshData> instanceBuffer;
            #endif

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
            #pragma multi_compile_instancing

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
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;

                float4 topRect : TEXCOORD4;
                float4 sideRect : TEXCOORD5;
                int viewFlags : TEXCOORD6;

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
					half4 fogFactorAndVertexLight	: TEXCOORD7; // x: fogFactor, yzw: vertex light
                #else
                half fogFactor : TEXCOORD7;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord 				: TEXCOORD8;
                #endif
            };
            
            //  SurfaceData & InputData
            void InitalizeSurfaceData(Varyings IN, out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0; // avoids "not completely initalized" errors

                half4 baseMap = half4(0, 0, 0, 1);

                if (IN.uv.x < 0.5)
                {
                    IN.uv.x *= 2;
                    float2 newUV = IN.topRect.xy + IN.uv * IN.topRect.zw;
                    if ((IN.viewFlags & 1) == 1)
                    {
                        baseMap = SAMPLE_TEXTURE2D(_TopGenTex, sampler_TopGenTex, newUV);
                    }
                    else
                    {
                        baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, newUV);
                    }
                }
                else
                {
                    IN.uv.x = (IN.uv.x - 0.5f) * 2;
                    float2 newUV = IN.sideRect.xy + IN.uv * IN.sideRect.zw;
                    if ((IN.viewFlags & 2) == 2)
                    {
                        baseMap = SAMPLE_TEXTURE2D(_SideGenTex, sampler_SideGenTex, newUV);
                    }
                    else
                    {
                        baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, newUV);
                    }
                }


                // Alpha Clipping
                clip(baseMap.a - _Cutoff);

                half4 diffuse = baseMap * _BaseColor;
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

                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
            }

            // Vertex Shader
            Varyings LitPassVertex(Attributes IN)
            {
                Varyings OUT;

                #if SHADER_TARGET >= 45
                meshData data = instanceBuffer[IN.instanceID];

                if (data.instId == IN.instanceID)
                {
                    if (IN.color.r > 0.2f)
                    {
                        if (IN.color.r < 0.7f &&
                            (data.viewFlags & 8) == 8)
                        {
                            IN.positionOS.z -= 0.3f;
                        }
                        if (IN.color.r > 0.7f &&
                            (data.viewFlags & 4) == 4)
                        {
                            IN.positionOS.z -= 0.3f;
                        }
                    }

                    IN.positionOS.xyz += data.pos;
                    OUT.topRect = data.topRect;
                    OUT.sideRect = data.sideRect;
                    OUT.viewFlags = data.viewFlags;
                }
                else
                {
                    OUT.topRect = 0;
                    OUT.sideRect = 0;
                    OUT.viewFlags = 128;
                }

                #else
                OUT.topRect = 0;
                OUT.sideRect = 0;
                OUT.viewFlags = 128;
                #endif

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS.xyz);
                
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

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
                return OUT;
            }

            // Fragment Shader
            half4 LitPassFragment(Varyings IN) : SV_Target
            {
                if (IN.viewFlags == 128)
                    discard;

                // Setup SurfaceData
                SurfaceData surfaceData;
                InitalizeSurfaceData(IN, surfaceData);

                // Setup InputData
                InputData inputData;
                InitializeInputData(IN, surfaceData.normalTS, inputData);

                // Simple Lighting (Lambert & BlinnPhong)
                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData); // v12 only

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                //color.a = 1;
                return color;
            }
            ENDHLSL
        }

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
            #pragma fragment ShadowPassFragment
            #pragma vertex DisplacedShadowPassVertex

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma target 4.5

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            #if SHADER_TARGET >= 45
            StructuredBuffer<meshData> instanceBuffer;
            #endif

            Varyings DisplacedShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Example Displacement
                #if SHADER_TARGET >= 45
                meshData data = instanceBuffer[input.instanceID];
                #else
                meshData data = (meshData)0;
                #endif

                if (input.color.r > 0.2f)
                {
                    if (input.color.r < 0.7f &&
                        (data.viewFlags & 8) == 8)
                    {
                        input.positionOS.z -= 0.3f;
                    }
                    if (input.color.r > 0.7f &&
                        (data.viewFlags & 4) == 4)
                    {
                        input.positionOS.z -= 0.3f;
                    }
                }

                input.positionOS.xyz += data.pos;

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                return 0;
            }
            ENDHLSL
        }

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

            struct Attributes
            {
                float4 position : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            #if SHADER_TARGET >= 45
                StructuredBuffer<meshData> instanceBuffer;
            #endif

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Example Displacement
                #if SHADER_TARGET >= 45
                meshData data = instanceBuffer[input.instanceID];
                #else
                meshData data = (meshData)0;
                #endif

                if (input.color.r > 0.2f)
                {
                    if (input.color.r < 0.7f &&
                        (data.viewFlags & 8) == 8)
                    {
                        input.position.z -= 0.3f;
                    }
                    if (input.color.r > 0.7f &&
                        (data.viewFlags & 4) == 4)
                    {
                        input.position.z -= 0.3f;
                    }
                }

                input.position.xyz += data.pos;

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                return 0;
            }
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            #if SHADER_TARGET >= 45
                StructuredBuffer<meshData> instanceBuffer;
            #endif

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                 // Example Displacement
                #if SHADER_TARGET >= 45
                meshData data = instanceBuffer[input.instanceID];
                #else
                meshData data = (meshData)0;
                #endif

                if (input.color.r > 0.2f)
                {
                    if (input.color.r < 0.7f &&
                        (data.viewFlags & 8) == 8)
                    {
                        input.positionOS.z -= 0.3f;
                    }
                    if (input.color.r > 0.7f &&
                        (data.viewFlags & 4) == 4)
                    {
                        input.positionOS.z -= 0.3f;
                    }
                }

                input.positionOS.xyz += data.pos;

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_TARGET
            {

                Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

                #if defined(_GBUFFER_NORMALS_OCT)
                    float3 normalWS = normalize(input.normalWS);
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
                    return half4(packedNormalWS, 0.0);
                #else
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}