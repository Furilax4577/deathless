using UnityEngine;

namespace Deathless.Jeu
{
    /// Visée d'une zone au sol (02/10/2026, décision de Quentin : « pour le mage, il doit voir où le sort va tomber ; clic
    /// droit annule, clic gauche confirme ; mécanique à reprendre avec l'archer »). Base commune du mage (grande boule de
    /// feu, mur de flammes) et du rôdeur (nuée de flèches) : l'appui sur la compétence ne lance plus le sort, il ouvre la
    /// visée ; un indicateur de zone (ZoneVisee, à la taille réelle de la zone d'effet, couleur du thème du sort) suit le
    /// point que le réticule vise, limité à la portée du sort ; confirmer (RT, clic gauche) lance le sort sur ce point,
    /// annuler (LT, clic droit, ou une esquive) referme la visée sans rien coûter. Rien ne part sur le réseau pendant la
    /// visée : seul le poste qui vise voit l'indicateur ; à la confirmation, le sort suit le chemin habituel de sa classe
    /// (tir, effet diffusé, dégâts appliqués par le poste du héros puis par l'hôte).
    ///
    /// Un objet par classe (ClasseHeros.Visiere) ; les règles d'entrée et d'interruption sont dans ClasseHeros
    /// (CommencerVisee, ViseeSurAction, ViseeMaj), cette classe tient l'état et l'indicateur.
    public class VisiereZone
    {
        /// La visée est ouverte.
        public bool Actif { get; private set; }
        /// Identifiant du sort visé (numéro choisi par la classe).
        public int Sort { get; private set; }
        /// Centre de la zone, au sol.
        public Vector3 Point { get; private set; }
        /// Axe de la zone : pour une ligne (mur), sa direction ; en travers de la ligne héros → point.
        public Vector3 Axe { get; private set; } = Vector3.forward;
        /// Le point est sur du sol : faux (vide), le sort ne peut pas partir là.
        public bool Valide { get; private set; }
        /// Distance horizontale héros → point (m), pour les journaux de test.
        public float Distance { get; private set; }

        float m_Portee, m_Min;
        bool m_Ligne;
        ZoneVisee m_Zone;

        /// Ouvre la visée. `a` : rayon (cercle) ou longueur (ligne), `b` : largeur (ligne).
        public void Commencer(Heros h, int sort, ZoneVisee.Forme forme, VfxTheme theme, float a, float b, float portee)
        {
            Terminer(false);
            Actif = true;
            Sort = sort;
            m_Portee = portee;
            m_Min = GameBalance.Courant.viseeZoneDistanceMin;
            m_Ligne = forme == ZoneVisee.Forme.Ligne;
            var gemmes = EffetsJeu.Gemmes;
            m_Zone = gemmes != null ? ZoneVisee.Creer(gemmes, theme, forme, a, b) : null;
            if (m_Zone != null)
            {
                m_Zone.proprietaire = h;
                var ignorer = h.transform;
                m_Zone.ignorerCollider = c => Combat.IgnorerPourSol(c, ignorer);
            }
            Maj(h);
        }

        /// Chaque image : le point visé, ramené au sol et à la portée, et l'indicateur qui le suit.
        public void Maj(Heros h)
        {
            if (!Actif || h == null) return;
            Valide = Combat.PointViseSol(h.CameraJeu, h.transform, m_Portee, m_Min, out Vector3 p);
            Point = p;
            Vector3 v = p - h.transform.position; v.y = 0f;
            Distance = v.magnitude;
            Vector3 dir = Distance > 0.05f ? v / Distance : h.AvantCamera;
            dir.y = 0f;
            Axe = m_Ligne ? Vector3.Cross(Vector3.up, dir.normalized).normalized : dir.normalized;
            if (m_Zone != null)
            {
                m_Zone.Valide = Valide;
                m_Zone.Placer(Point, Axe);
            }
        }

        /// Referme la visée : `confirme` vrai, la zone se resserre (le sort part) ; faux, elle s'éteint.
        public void Terminer(bool confirme)
        {
            if (m_Zone != null) m_Zone.Fermer(confirme);
            m_Zone = null;
            Actif = false;
        }
    }
}
