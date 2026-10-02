// Eau de la rivière du village (carte v5, 02/10/2026) : la surface low poly de l'eau du donjon (facettes plates par
// dérivées, ondulation par sommets, semi-opaque, non éclairée), plus le COURANT : traits d'écume qui glissent dans le
// sens de l'eau. Le maillage porte UV0 = (travers 0..1, distance le long de la rivière en m, croissante vers l'aval)
// et UV1.x = facteur de vitesse (plus vif au pied de la cascade et aux gués). Dans le bassin, UV0.y est le rayon : les
// traits deviennent des anneaux qui s'élargissent. Teintes posées depuis la palette Eau (VillageBuilder), jamais vert.
// La nuit (_DeathlessNuit, global posé par CascadeVillage depuis DayCycle.Night), l'eau et l'écume s'assombrissent.
// Anneaux du bassin (retour du 02/10/2026 : « le centre de l'animation n'est pas aligné avec la cascade ») : le disque du
// maillage est centré sur le bassin, 1 m à l'ouest du pied de la nappe ; quand CascadeVillage pose _DeathlessCascadePied
// (x, z monde du pied mesuré sur la nappe, w = 1), le rayon et l'angle des anneaux sont calculés dans le fragment à partir
// de ce point (cercles parfaitement centrés sur le pied), au lieu des UV du disque.
Shader "Deathless/Village/EauRiviere"
{
    Properties
    {
        _Couleur ("Couleur (alpha = opacité)", Color) = (0.11, 0.24, 0.4, 0.78)
        _Reflet ("Reflet des facettes", Color) = (0.45, 0.72, 0.9, 1)
        _Ecume ("Écume", Color) = (0.95, 0.98, 1, 1)
        _Amplitude ("Amplitude des vagues (m)", Float) = 0.05
        _Vitesse ("Vitesse des vagues", Float) = 0.9
        _Echelle ("Fréquence spatiale", Float) = 1.1
        _Facettes ("Contraste des facettes", Float) = 5
        _Courant ("Vitesse du courant (m/s)", Float) = 0.9
        _Voies ("Voies de traits en travers", Float) = 9
        _Trait ("Longueur d'un cycle de trait (m)", Float) = 2.2
        _Densite ("Part de traits éteints (0..1)", Float) = 0.68
        _EcumeForce ("Force de l'écume", Float) = 0.55
        _LargeurMin ("Largeur minimale d'un trait (m)", Float) = 0.1
        _LargeurRuban ("Largeur du ruban de rivière (m)", Float) = 6.3
        _NuitFacteur ("Teinte à la pleine nuit", Float) = 0.45
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
                half4 _Ecume;
                float _Amplitude, _Vitesse, _Echelle, _Facettes;
                float _Courant, _Voies, _Trait, _Densite, _EcumeForce, _NuitFacteur, _LargeurMin, _LargeurRuban;
            CBUFFER_END
            float _DeathlessNuit;
            float4 _DeathlessCascadePied;   // xy = pied de la cascade (monde, x et z), w = 1 si posé

            struct Attributs { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float vit : TEXCOORD2;
                float brouillard : TEXCOORD3;
                float disque : TEXCOORD4;
            };

            float Hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

            Varyings Vert(Attributs i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float t = _Time.y * _Vitesse;
                p.y += (sin(p.x * _Echelle + t) + cos(p.z * _Echelle * 1.31 + t * 1.27) + sin((p.x + p.z) * _Echelle * 0.7 - t * 0.8)) * 0.33 * _Amplitude;
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.uv = i.uv;
                o.vit = max(0.2, i.uv2.x);
                o.disque = i.uv2.y;   // 1 : disque du bassin (UV0.y = rayon), 0 : ruban de la rivière
                o.brouillard = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(cross(ddy(i.positionWS), ddx(i.positionWS)));
                if (n.y < 0) n = -n;
                float pente = saturate(0.5 + (n.x * 0.6 + n.z * 0.45) * _Facettes);
                half3 c = lerp(_Couleur.rgb, _Reflet.rgb, pente * pente * 0.55);
                // courant : traits d'écume par voies, qui avancent vers l'aval. Retour de Quentin du 02/10/2026 (« les micro-traits dans
                // l'eau, c'est pas GG ») : le défaut venait de la vitesse locale (UV1.x) multipliée par le temps dans la phase du motif :
                // elle variait d'un point à l'autre, donc la fréquence des traits croissait avec le temps de jeu (traits de 1 à 2 pixels
                // en peignes, moiré). Maintenant : vitesse de défilement UNIQUE (_Courant), phase = distance - temps seulement ; la
                // vitesse locale ne règle plus que la densité et la largeur. Traits adoucis par dérivées (pas de scintillement) et
                // éteints quand ils tombent sous quelques pixels ; largeur physique minimale de _LargeurMin m (10 cm par défaut).
                // disque du bassin : rayon et angle autour du pied de la nappe (si posé), sinon UV du maillage
                float2 dp = i.positionWS.xz - _DeathlessCascadePied.xy;
                float2 uvPolaire = float2(atan2(dp.y, dp.x) * 0.15915494 + 0.5, length(dp));
                float2 uv = (i.disque > 0.5 && _DeathlessCascadePied.w > 0.5) ? uvPolaire : i.uv;
                float x = uv.x * _Voies;
                float xDecale = frac(uv.x + 0.5) * _Voies;   // dérivée sans la couture de l'angle (atan2 saute de 1 à 0)
                float voie = floor(x);
                float dans = abs(frac(x) - 0.5);
                float aaX = max(min(fwidth(x), fwidth(xDecale)), 1e-4);
                float s = (uv.y - _Time.y * _Courant) / _Trait + Hash(voie + 3.7) * 7.0;
                float aaS = max(fwidth(s), 1e-4);
                float cellule = floor(s), f = frac(s);
                float densite = saturate(_Densite - (i.vit - 1.0) * 0.18);
                float allume = step(densite, Hash(cellule * 1.618 + voie * 5.31));
                float longueur = 0.35 + 0.3 * Hash(cellule + voie * 2.1);
                // largeur (fraction de voie) : jamais sous _LargeurMin m ; dans le disque du bassin la voie est un arc (2 pi r / voies)
                float voieM = lerp(_LargeurRuban, 6.2832 * uv.y, i.disque) / _Voies;
                float demiL = max(0.09 + 0.05 * i.vit, 0.5 * _LargeurMin / max(voieM, 0.05));
                demiL = min(demiL, 0.46);
                float bord = smoothstep(demiL + aaX, demiL - aaX, dans);
                float bout = smoothstep(longueur + aaS, longueur - aaS, f);
                float visible = saturate((2.0 * demiL / aaX - 1.5) / 1.5) * saturate((longueur / aaS - 1.5) / 1.5);
                float place = saturate((voieM - 2.0 * _LargeurMin) / (2.0 * _LargeurMin));   // voie trop étroite (centre du bassin) : pas de trait
                float trait = allume * bord * bout * visible * place * sin(3.14159 * saturate(f / longueur));
                float ecume = saturate(trait * _EcumeForce * saturate(0.6 + 0.4 * i.vit));
                c = lerp(c, _Ecume.rgb, ecume);
                c *= lerp(1.0, _NuitFacteur, saturate(_DeathlessNuit));
                c = MixFog(c, i.brouillard);
                return half4(c, saturate(_Couleur.a + ecume * 0.2));
            }
            ENDHLSL
        }
    }
}
