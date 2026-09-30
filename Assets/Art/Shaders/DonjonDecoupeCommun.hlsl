// Découpe circulaire du donjon (voir DonjonDecoupe.shader) : fonction partagée par les passes couleur, profondeur et
// normales de profondeur (sans la découpe des pré-passes, l'occlusion ambiante et la texture de profondeur garderaient
// le mur). Globales posées par CameraEpaule.PoserDecoupe.
#ifndef DEATHLESS_DONJON_DECOUPE_INCLUDED
#define DEATHLESS_DONJON_DECOUPE_INCLUDED

// _DecoupeCentre : xy = position du héros à l'écran (0..1), z = sa profondeur de vue (m), w = hauteur (monde) de ses
// pieds. _DecoupeRayon : rayon du disque en fraction de la hauteur de l'écran ; 0 = aucune découpe.
float4 _DecoupeCentre;
float _DecoupeRayon;

half DecoupeSeuil(float2 positionEcran)
{
    const half motif[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint x = (uint)positionEcran.x & 3u;
    uint y = (uint)positionEcran.y & 3u;
    return (half)(motif[y * 4u + x] + 0.5) / (half)16.0;
}

// positionCS : SV_POSITION côté fragment (pixels, profondeur du tampon).
void DonjonDecouper(float4 positionCS)
{
    if (_DecoupeRayon <= 0.0) return;
    // UV d'écran normalisées d'URP (retournement de la plateforme compris) : les mêmes que pour reconstruire la position.
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float profondeur = LinearEyeDepth(positionCS.z, _ZBufferParams);
    // Seulement ce qui est devant le héros (entre lui et la caméra) et au-dessus de ses pieds : le sol qu'il foule et
    // ce qui est derrière lui restent pleins.
    if (profondeur >= _DecoupeCentre.z - 0.35) return;
    #if UNITY_REVERSED_Z
    float zb = positionCS.z;
    #else
    float zb = lerp(UNITY_NEAR_CLIP_VALUE, 1, positionCS.z);
    #endif
    float3 ws = ComputeWorldSpacePosition(uv, zb, UNITY_MATRIX_I_VP);
    if (ws.y <= _DecoupeCentre.w + 0.25) return;
    float2 d = uv - _DecoupeCentre.xy;
    d.x *= _ScreenParams.x / _ScreenParams.y;
    float r = length(d) / _DecoupeRayon;
    // Plein hors du disque, vide au centre, bord en damier sur le dernier tiers du rayon.
    half couverture = saturate((r - 0.65) / 0.35);
    clip(couverture - DecoupeSeuil(positionCS.xy));
}
#endif
