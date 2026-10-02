// Deathless/VertexColorLit avec la découpe circulaire autour du héros du donjon (DonjonDecoupeCommun.hlsl, la même que
// Deathless/DonjonDecoupe) : matériau de pierre du donjon en terrasses (Assets/Jeu/Resources/DonjonTerrasses/
// DonjonTerrasses_Pierre.mat). Ce qui se trouve entre la caméra et le héros, dans un disque centré sur lui à l'écran,
// est découpé en damier (sans alpha) ; piloté par les globales de CameraEpaule.PoserDecoupe (rayon 0 hors du donjon :
// rendu identique à Deathless/VertexColorLit). Les passes de profondeur et de normales portent la même découpe
// (occlusion ambiante, texture de profondeur) ; l'ombre portée reste pleine (passe d'URP Lit).
Shader "Deathless/VertexColorLitDecoupe"
{
    Properties
    {
        _BaseColor("Teinte", Color) = (1, 1, 1, 1)
        _Smoothness("Brillance", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Art/Shaders/DonjonDecoupeCommun.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DonjonDecouper(i.positionCS);
                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.positionCS = i.positionCS;
                d.normalWS = normalize(i.normalWS);
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogFactor;
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);

                half3 couleur = i.color.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                couleur = SRGBToLinear(couleur);
                #endif
                SurfaceData s = (SurfaceData)0;
                s.albedo = couleur * _BaseColor.rgb;
                s.alpha = 1;
                s.smoothness = _Smoothness;
                s.specular = half3(0.05, 0.05, 0.05);
                s.occlusion = 1;

                half4 c = UniversalFragmentBlinnPhong(d, s);
                c.rgb = MixFog(c.rgb, i.fogFactor);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertProfondeur
            #pragma fragment fragProfondeur
            #pragma multi_compile_instancing
            #include "Assets/Art/Shaders/DonjonDecoupeCommun.hlsl"

            struct AttributesP { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct VaryingsP { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            VaryingsP vertProfondeur(AttributesP i)
            {
                VaryingsP o = (VaryingsP)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }

            half fragProfondeur(VaryingsP i) : SV_Target
            {
                DonjonDecouper(i.positionCS);
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertNormales
            #pragma fragment fragNormales
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Assets/Art/Shaders/DonjonDecoupeCommun.hlsl"

            struct AttributesN { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct VaryingsN { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            VaryingsN vertNormales(AttributesN i)
            {
                VaryingsN o = (VaryingsN)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            half4 fragNormales(VaryingsN i) : SV_Target
            {
                DonjonDecouper(i.positionCS);
                float3 n = normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                float2 remap = saturate(oct * 0.5 + 0.5);
                return half4(PackFloat2To888(remap), 0.0);
                #else
                return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
