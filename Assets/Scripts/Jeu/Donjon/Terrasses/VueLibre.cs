using UnityEngine;

namespace Deathless.Donjon.Terrasses
{
    /// Marque d'une collision que la caméra du joueur traverse (CameraEpaule.Ignore) : piliers libres et parapets du donjon
    /// en terrasses. La caméra ne se jette pas en avant chaque fois qu'un pilier passe derrière le héros ; ce qui la gêne
    /// est découpé autour du héros (shader Deathless/VertexColorLitDecoupe). Les murs, les terrasses et les plafonds
    /// restent des obstacles pour elle.
    public class VueLibre : MonoBehaviour { }
}
