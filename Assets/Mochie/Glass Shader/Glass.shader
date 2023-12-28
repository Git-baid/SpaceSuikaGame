﻿Shader "Mochie/Glass" {
    Properties {

        _GrabpassTint("Grabpass Tint", Color) = (1,1,1,1)
        _SpecularityTint("Specularity Tint", Color) = (1,1,1,1)
		_BaseColorTint("Base Color Tint", Color) = (1,1,1,1)

        _BaseColor("Base Color", 2D) = "black" {}
		_RoughnessMap("Roughness Map", 2D) = "white" {}
        _MetallicMap("Metallic Map", 2D) = "white" {}
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Roughness("Roughness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
		_Occlusion("Occlusion", Range(0,1)) = 1
        _NormalStrength("Normal Strength", Float) = 1
        [KeywordEnum(ULTRA, HIGH, MED, LOW)]BlurQuality("Blur Quality", Int) = 1
		_Blur("Blur Strength", Float) = 1
        _Refraction("Refraction Strength", Float) = 5
        [ToggleUI]_RefractMeshNormals("Refract Mesh Normals", Int) = 0

        [Toggle(_RAIN_ON)]_RainToggle("Enable", Int) = 0
		[HideInInspector]_RainSheet("Texture Sheet", 2D) = "black" {}
		[HideInInspector]_Rows("Rows", Float) = 8
		[HideInInspector]_Columns("Columns", Float) = 8
		_Speed("Speed", Float) = 60
		_XScale("X Scale", Float) = 1.5
        _YScale("Y Scale", Float) = 1.5
		_Strength("Normal Strength", Float) = 0.3
		_RippleScale("Ripple Scale", float) = 10
		_RippleSpeed("Ripple Speed", float) = 10
		_RippleStrength("Ripple Strength", float) = 1
        _RippleSize("Ripple Size", Range(2,10)) = 6
        _RippleDensity("Ripple Density", Float) = 1.57079632679
        [Enum(Droplets,0, Ripples,1)]_RainMode("Mode", Int) = 0
        _RainMask("Mask", 2D) = "white" {}
        [Enum(Red,0, Green,1, Blue,2, Alpha,0)]_RainMaskChannel("Channel", Int) = 0
        _DropletMask("Rain Droplet Mask", 2D) = "white" {}
        _DynamicDroplets("Droplet Strength", Range(0,1)) = 0.5
        _RainBias("Rain Bias", Float) = -1
        _Test("Test", Float) = 1

        [Toggle(_REFLECTIONS_ON)]_ReflectionsToggle("Reflections", Int) = 1
        [Toggle(_SPECULAR_HIGHLIGHTS_ON)]_SpecularToggle("Specular Highlights", Int) = 1
        [Toggle(_LIT_BASECOLOR_ON)]_LitBaseColor("Lit Base Color", Int) = 1
        [Enum(Default,0, Stochastic,1)]_SamplingMode("Sampling Mode", Int) = 0
		[Enum(UnityEngine.Rendering.CullMode)]_Culling("Culling", Int) = 2
        [Enum(Grabpass,0, Premultiplied,1, Off,2)]_BlendMode("Transparency", Int) = 0
        [HideInInspector]_SrcBlend("Src Blend", Int) = 1
        [HideInInspector]_DstBlend("Dst Blend", Int) = 0
        [HideInInspector]_ZWrite("Z Write", Int) = 0
        [HideInInspector]_MaterialResetCheck("Reset", Int) = 0
        _QueueOffset("Queue Offset", Int) = 0
    }
    SubShader {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "ForceNoShadowCaster"="True"
            "IgnoreProjector"="True"
        }
        GrabPass {
            Tags {"LightMode"="Always"}
            "_GlassGrab"
        }
        Cull [_Culling]
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass {
            Name "ForwardBase"
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma shader_feature_local _RAIN_ON
            #pragma shader_feature_local _ BLURQUALITY_ULTRA BLURQUALITY_HIGH BLURQUALITY_MED BLURQUALITY_LOW
            #pragma shader_feature_local _REFLECTIONS_ON
            #pragma shader_feature_local _SPECULAR_HIGHLIGHTS_ON
            #pragma shader_feature_local _GRABPASS_ON
            #pragma shader_feature_local _LIT_BASECOLOR_ON
            #pragma shader_feature_local _STOCHASTIC_SAMPLING_ON
            #pragma shader_feature_local _NORMALMAP_ON
            #pragma shader_feature_local _RAINMODE_RIPPLE
            #pragma target 5.0

            #include "GlassDefines.cginc"

            v2f vert (appdata v){
                v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvGrab = ComputeGrabScreenPos(o.pos);

                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent.xyz = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0)).xyz);
                o.tangent.w = v.tangent.w;
                o.binormal = normalize(cross(o.normal, o.tangent) * v.tangent.w);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.cameraPos = GetCameraPos();
                o.localPos = v.vertex;
                
                UNITY_TRANSFER_SHADOW(o, o.pos)
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            float4 frag (v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target {

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 specCol = 0;
		        float3 reflCol = 0;
                float flipbookBase = 0;

                float3 normalDir = normalize(i.normal);
                float3 normalMap = 0;
                #if defined(_NORMALMAP_ON)
                    normalMap = UnpackScaleNormal(SampleTexture(_NormalMap, TRANSFORM_TEX(i.uv, _NormalMap)), _NormalStrength);
                #endif
                #if defined(_RAIN_ON)
                    float rainMask = tex2D(_RainMask, TRANSFORM_TEX(i.uv, _RainMask));
                    #if defined(_RAINMODE_RIPPLE)
                        float3 rainNormal = GetRipplesNormal(i.uv, _RippleScale, _RippleStrength*rainMask, _RippleSpeed, _RippleSize, _RippleDensity);
                    #else
                        float3 rainNormal = GetFlipbookNormals(i, flipbookBase, rainMask);
                        ApplyExtraDroplets(i, rainNormal, flipbookBase, rainMask);
                    #endif
                    #if defined(_NORMALMAP_ON)
                        normalMap = BlendNormals(rainNormal, normalMap);
                    #else
                        normalMap = rainNormal;
                    #endif
                #endif
                #if defined(_NORMALMAP_ON) || defined(_RAIN_ON)
                    float3 binormal = cross(i.normal, i.tangent.xyz) * (i.tangent.w * unity_WorldTransformParams.w);
                    normalDir = normalize(normalMap.x * i.tangent + normalMap.y * binormal + normalMap.z * i.normal);
                #endif
                normalDir = lerp(-normalDir, normalDir, isFrontFace);
                normalMap = lerp(-normalMap, normalMap, isFrontFace);
                
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float3 reflDir = reflect(-viewDir, normalDir);

                float roughnessMap = SampleTexture(_RoughnessMap, TRANSFORM_TEX(i.uv, _RoughnessMap)) * _Roughness;
                float roughness = saturate(roughnessMap-flipbookBase);

                #if defined(_SPECULAR_HIGHLIGHTS_ON) || defined(_REFLECTIONS_ON)
                    float roughSq = roughness * roughness;
                    float roughBRDF = max(roughSq, 0.003);
                    float metallic = SampleTexture(_MetallicMap, TRANSFORM_TEX(i.uv, _MetallicMap)) * _Metallic;
                    float omr = unity_ColorSpaceDielectricSpec.a - metallic * unity_ColorSpaceDielectricSpec.a;
                    float3 specularTint = lerp(unity_ColorSpaceDielectricSpec.rgb, 1, metallic);

                    float3 halfVector = normalize(lightDir + viewDir);
                    float NdotL = dot(normalDir, lightDir);
                    float NdotH = Safe_DotClamped(normalDir, halfVector);
                    float LdotH = Safe_DotClamped(lightDir, halfVector);
                    float NdotV = abs(dot(normalDir, viewDir));

                    #if defined(_SPECULAR_HIGHLIGHTS_ON)
                        float3 fresnelTerm = FresnelTerm(specularTint, LdotH);
                        float specularTerm = SpecularTerm(NdotL, NdotV, NdotH, roughBRDF);
                        specCol = _LightColor0 * fresnelTerm * specularTerm * atten;
                    #endif

                    #if defined(_REFLECTIONS_ON)
                        float surfaceReduction = 1.0 / (roughBRDF*roughBRDF + 1.0);
                        float grazingTerm = saturate((1-_Roughness) + (1-omr));
                        float fresnel = FresnelLerp(specularTint, grazingTerm, NdotV);
                        reflCol = GetWorldReflections(reflDir, i.worldPos, roughness) * fresnel * surfaceReduction;
                    #endif
                #endif

                // float2 offset = lerp(normalMap, normalDir, _RefractMeshNormals) * _Refraction * 0.01;
                float2 offset = normalMap * _Refraction * 0.01;
                float2 screenUV = (i.uvGrab.xy / max(EPSILON, i.uvGrab.w)) + offset;
                // float3 wPos = GetWorldSpacePixelPos(i.localPos, screenUV);
                // float dist = distance(wPos, i.cameraPos);
                // _Blur *= 1-min(dist/10, 1);

                float3 grabCol = 0;

                #ifdef _GRABPASS_ON
                    float blurStr = _Blur * 0.0125;
                    #if UNITY_SINGLE_PASS_STEREO || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                        blurStr *= 0.25;
                    #endif
                    if (_Roughness > 0 && _Blur > 0)
                        grabCol = BlurredGrabpassSample(screenUV, (roughness * blurStr));
                    else
                        grabCol = MOCHIE_SAMPLE_TEX2D_SCREENSPACE(_GlassGrab, screenUV);
                    grabCol *= _GrabpassTint;
                    
                #endif

                float4 baseColorTex = SampleTexture(_BaseColor, TRANSFORM_TEX(i.uv, _BaseColor)) * _BaseColorTint;
                float3 baseColor = baseColorTex.rgb * baseColorTex.a;
                #ifdef _LIT_BASECOLOR_ON
                    float3 lightCol = ShadeSH9(normalDir) + _LightColor0;
                    baseColor *= saturate(lightCol);
                #endif
                float occlusion = lerp(1, SampleTexture(_OcclusionMap, TRANSFORM_TEX(i.uv, _OcclusionMap)), _Occlusion);
                float3 specularity = (specCol + reflCol) * _SpecularityTint;

                float3 col = (specularity + grabCol + baseColor) * occlusion;
                #ifdef _GRABPASS_ON
                    float4 finalCol = float4(col, 1);
                #else
                    float4 finalCol = float4(col, 0);
                #endif

                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol; // float4(flipbookBase.xxx, 1);
            }
            ENDCG
        }
    }
    CustomEditor "GlassEditor"
}