using UnityEngine;

namespace Deathless.Jeu
{
    /// Base commune aux deux versions de Morgrim, le mini-boss de la nuit 10 (wiki : ennemis.md, section Morgrim ;
    /// décidé le 26/09/2026). L'hôte choisit seul la compétence et ses effets (Docs/reseau.md) ; les autres postes ne
    /// tournent pas cette IA (le composant `Squelette` d'une marionnette est désactivé), mais reçoivent quand même la
    /// télégraphie et l'impact au sol par un chemin léger (`EnnemiReseau.DiffuserEffetMorgrim`, un octet de thème et
    /// une forme, pas un RPC par gemme) : c'est purement visuel, comme le reste des effets de squelette.
    /// Outils communs aux sous-classes : compte des joueurs proches (choix de compétence) et télégraphie / impact au
    /// sol dans le langage gemmes commun (MorgrimEffets : AnneauGemmes pour la préparation, EclatGemmes pour le coup).
    public abstract class MorgrimVariant : Golem
    {
        /// Joueurs vivants à moins de `rayon` (m, horizontal) : sert au choix de compétence (zone vs cible isolée).
        protected int JoueursProches(float rayon)
        {
            if (P == null) return 0;
            int n = 0;
            foreach (var h in P.TousLesHeros)
            {
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - transform.position; d.y = 0f;
                if (d.magnitude <= rayon) n++;
            }
            return n;
        }

        protected bool BouclierLeve => BouclierNyxessa.Instance != null && BouclierNyxessa.Instance.Leve;

        /// Télégraphie au sol pendant la préparation (cercle ou cône de gemmes, langage commun VFX) : posée devant le
        /// personnage, orientée avec lui, détruite avec la préparation. Hôte seulement (appelée depuis CommencerAttaque) :
        /// diffusée aux autres postes par `EnnemiReseau.DiffuserEffetMorgrim`.
        protected void Telegraphier(Vector3 point, VfxTheme theme, float rayon, float duree, float angleDeg = 360f)
        {
            JouerTelegraphie(point, theme, rayon, duree, angleDeg);
            if (m_Reseau != null && m_Reseau.IsSpawned && m_Reseau.IsServer)
                m_Reseau.DiffuserEffetMorgrim(true, (byte)theme, rayon, duree, angleDeg);
        }

        /// Gerbe d'impact (coup qui part), même langage : boule (360°) ou gerbe dirigée dans un cône. Hôte seulement
        /// (appelée depuis Frapper) : diffusée comme la télégraphie.
        protected void Impact(Vector3 point, VfxTheme theme, float rayon, Vector3? direction = null, float coneDeg = 360f)
        {
            JouerImpact(point, theme, rayon, direction, coneDeg);
            if (m_Reseau != null && m_Reseau.IsSpawned && m_Reseau.IsServer)
                m_Reseau.DiffuserEffetMorgrim(false, (byte)theme, rayon, 0f, coneDeg);
        }

        /// Client (marionnette) : rejoue une télégraphie ou un impact reçus de l'hôte, devant le personnage comme
        /// chez lui (transform répliqué par NetworkTransform, léger décalage de latence accepté).
        public void RejouerEffetDistant(bool telegraphie, VfxTheme theme, float rayon, float duree, float angleDeg)
        {
            Vector3 point = transform.position + Vector3.up * 0.05f;
            if (telegraphie) JouerTelegraphie(point, theme, rayon, duree, angleDeg);
            else JouerImpact(point, theme, rayon, transform.forward, angleDeg);
        }

        void JouerTelegraphie(Vector3 point, VfxTheme theme, float rayon, float duree, float angleDeg)
        {
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat == null) return;
            MorgrimEffets.MateriauGemmes = mat;
            var go = MorgrimEffets.AnneauGemmes(point, theme, Mathf.Max(0.3f, rayon), Mathf.Max(0.1f, duree), angleDeg, transform.forward, "Telegraphie_" + name);
            go.transform.SetParent(transform, true);
            Object.Destroy(go, duree + 0.3f);
        }

        void JouerImpact(Vector3 point, VfxTheme theme, float rayon, Vector3? direction, float coneDeg)
        {
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat == null) return;
            MorgrimEffets.MateriauGemmes = mat;
            var go = MorgrimEffets.EclatGemmes(point, theme, Mathf.Max(0.3f, rayon), direction, coneDeg, "Impact_" + name);
            Object.Destroy(go, 1f);
        }
    }
}
