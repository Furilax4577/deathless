using UnityEngine;

namespace Deathless.Jeu
{
    /// Déplacement d'un CharacterController découpé en sous-pas (02/10/2026, retour de Quentin : les héros flottaient sur
    /// les marches). Le `stepOffset` du contrôleur ne fonctionne que si chaque Move avance de moins que la profondeur
    /// de la marche : à faible cadence (1/15 s à vitesse de sprint = 0,5 m par image) le contrôleur se plantait dans le
    /// nez des marches (0,25 m de haut, 0,30 m de large). Chaque Move est donc borné à `PasMax` mètres à l'horizontale
    /// (au prorata de dt : le déplacement total de l'image est inchangé), ce qui remplace la rampe invisible
    /// `Marches_Rampe` posée au 01/10/2026. Le vertical (gravité, saut) est réparti à égalité sur les sous-pas.
    public static class DeplacementSousPas
    {
        /// Déplacement horizontal maximal d'un appel à Move (m) : sous la moitié de la profondeur d'une marche (0,3 m).
        public const float PasMax = 0.15f;
        /// Garde-fou : un déplacement énorme (téléportation par Move, bug) ne coûte pas plus de 12 appels.
        public const int SousPasMax = 12;

        /// Nombre de sous-pas pour un déplacement donné.
        public static int Nombre(Vector3 delta)
        {
            float h = Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
            return Mathf.Clamp(Mathf.CeilToInt(h / PasMax), 1, SousPasMax);
        }

        /// Équivalent de `cc.Move(delta)` en sous-pas. Les drapeaux sont ceux de tous les sous-pas réunis (un contact
        /// côté ou dessous pendant l'image compte). `collerAuSol` : le personnage était au sol et ne saute pas ; s'il a
        /// perdu le sol en marchant (descente d'une marche : le contrôleur n'a pas de pas descendant, il tomberait de
        /// chaque marche, avec une image en l'air par marche), il est recollé au sol situé à moins de `stepOffset`
        /// dessous (CapsuleCast puis Move vers le bas). Un vrai saut ou un bord plus haut que cela ne sont pas touchés.
        public static CollisionFlags Deplacer(CharacterController cc, Vector3 delta, bool collerAuSol = false)
        {
            int n = Nombre(delta);
            Vector3 d = delta / n;
            bool coller = collerAuSol && delta.y <= 0.0001f;
            CollisionFlags drapeaux = CollisionFlags.None;
            for (int i = 0; i < n; i++)
            {
                var f = cc.Move(d);
                if (coller && (f & CollisionFlags.Below) == 0 && !cc.isGrounded) f |= Recoller(cc);   // à chaque sous-pas : plusieurs marches peuvent se descendre en une image
                drapeaux |= f;
            }
            return drapeaux;
        }

        /// Recolle au sol situé à moins de `stepOffset` sous la capsule (CapsuleCast puis Move vers le bas), sinon ne fait rien.
        static CollisionFlags Recoller(CharacterController cc)
        {
            Vector3 centre = cc.transform.TransformPoint(cc.center);
            float demi = Mathf.Max(0f, cc.height * 0.5f - cc.radius);
            if (Physics.CapsuleCast(centre + Vector3.up * demi, centre - Vector3.up * demi, cc.radius * 0.9f, Vector3.down, out RaycastHit h, cc.stepOffset, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && h.distance > 0.002f && h.normal.y > 0.2f)   // pas un mur ; la normale d'un nez de marche touché par la capsule est oblique
                return cc.Move(Vector3.down * h.distance);
            return CollisionFlags.None;
        }
    }

    /// Pieds au sol (02/10/2026) : le contrôleur est une capsule à fond sphérique (rayon 0,4 m) : sur une marche, il
    /// s'appuie sur le nez de la marche alors que son centre, donc le personnage, est encore à 0,4 m en retrait au-dessus
    /// de la marche du dessous ; les bottes flottent alors de 5 à 25 cm au-dessus de ce qu'elles surplombent. Le modèle
    /// (enfant de la racine) est donc abaissé, à l'écran seulement (capsule, caméra, tirs et ancres de la racine ne
    /// bougent pas), jusqu'au sol situé juste sous la racine : la hauteur visuelle suit ce sol : tout de suite quand il monte
    /// (le modèle ne s'enfonce jamais dans une marche), avec un lissage court (constante de temps de 33 ms) quand il descend,
    /// et le modèle ne descend jamais de plus de 30 cm.
    /// Uniquement au sol (en l'air, décalage nul). Sur le plat, le décalage est nul : la racine est déjà au sol.
    public struct PiedsAuSol
    {
        public const float DecalageMax = 0.30f;
        public const float Raideur = 30f;
        float m_Y;
        bool m_Init;

        /// Décalage vertical (≤ 0, m) à donner au modèle. `racineY` : hauteur de la racine ; `solY` : sol sous la racine.
        public float Maj(float racineY, float solY, bool auSol, float dt)
        {
            float cible = auSol ? Mathf.Clamp(solY, racineY - DecalageMax, racineY) : racineY;
            // Vers le haut (marche qui monte) : tout de suite, le modèle ne s'enfonce jamais dans le sol ; vers le bas : lissé.
            if (!m_Init || cible >= m_Y) { m_Y = cible; m_Init = true; }
            else m_Y = Mathf.Lerp(m_Y, cible, 1f - Mathf.Exp(-Raideur * dt));
            return Mathf.Clamp(m_Y - racineY, -DecalageMax, 0f);
        }

        public void Remettre() { m_Init = false; }
    }
}
