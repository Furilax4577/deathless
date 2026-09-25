// Eau du donjon : surface low poly à facettes (normales calculées par dérivées, donc plates par triangle),
// ondulation légère par sommets, semi-opaque (on devine le fond, sans flou ni réfraction), teinte bleu profond ou
// bleu-gris, jamais verte (le vert est réservé à Nyxessa). Non éclairée : lisible même dans le noir du donjon.
Shader "Deathless/Donjon/EauLowPoly"
{
    Properties
    {
        _Couleur ("Couleur (alpha = opacité)", Color) = (0.11, 0.22, 0.38, 0.74)
        _Reflet ("Reflet des facettes", Color) = (0.42, 0.56, 0.74, 1)
        _Amplitude ("Amplitude des vagues (m)", Float) = 0.06
        _Vitesse ("Vitesse", Float) = 0.9
        _Echelle ("Fréquence spatiale", Float) = 1.1
        _Facettes ("Contraste des facettes", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Eau"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Couleur;
                half4 _Reflet;
                float _Amplitude;
                float _Vitesse;
                float _Echelle;
                float _Facettes;
            CBUFFER_END

            struct Attributs { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float brouillard : TEXCOORD1;
            };

            Varyings Vert(Attributs i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float t = _Time.y * _Vitesse;
                p.y += (sin(p.x * _Echelle + t) + cos(p.z * _Echelle * 1.31 + t * 1.27) + sin((p.x + p.z) * _Echelle * 0.7 - t * 0.8)) * 0.33 * _Amplitude;
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.brouillard = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(cross(ddy(i.positionWS), ddx(i.positionWS)));
                if (n.y < 0) n = -n;
                // Chaque facette penche un peu : sa pente décide de sa teinte, entre le fond et le reflet.
                float pente = saturate(0.5 + (n.x * 0.6 + n.z * 0.45) * _Facettes);
                half3 c = lerp(_Couleur.rgb, _Reflet.rgb, pente * pente * 0.55);
                c = MixFog(c, i.brouillard);
                return half4(c, _Couleur.a);
            }
            ENDHLSL
        }
    }
}
