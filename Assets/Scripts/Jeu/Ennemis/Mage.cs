using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Mage squelette (wiki : ennemis.md, Mage ; 28/09/2026) : tireur fragile qui garde ses distances et lance le missile
    /// crâne à taille normale (le même que Nyxar) sur le joueur le plus proche à portée, sinon sur Nyxessa. Version
    /// simple de la garde-distance de Necromancien.MajDistance : ni invocation ni téléportation. Il n'a pas de coup de
    /// mêlée : `preparation` est l'incantation avant le départ du crâne, `intervalle` le temps entre deux tirs, `portee`
    /// la portée de tir (GameBalance.mage) ; la bande de distance tenue est GameBalance.mageDistanceTir.
    public class Mage : Squelette
    {
        static readonly int P_Shoot = Animator.StringToHash("Shoot");

        float m_ProchainTir;
        Sante m_CibleTir;
        float m_TirDans = -1f;

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            base.Initialiser(stats, multiplicateurPV);
            m_ProchainTir = Time.time + 1.5f;
        }

        protected override void MajMarche(float dt) => MajDistance(dt);
        protected override void MajPoursuite(float dt) => MajDistance(dt);

        void MajDistance(float dt)
        {
            var b = B;
            // Rugissement du viking : le provocateur devient la cible, comme pour les autres squelettes.
            var j = Provoque ? Provocateur : JoueurProche(m_Stats.portee + 3f);
            Sante cible = j != null ? j.Sante : (P != null ? P.nyxessa : null);
            if (cible == null) return;
            Vector3 c = cible.transform.position;
            float d = Distance(c);
            Tourner(c);
            // Garder ses distances : approcher au-delà de la distance haute, reculer en deçà de la basse, sinon s'arrêter.
            if (d > b.mageDistanceTir.y) { Agent.isStopped = false; Poursuivre(c); }
            else if (d < b.mageDistanceTir.x)
            {
                Vector3 fuite = transform.position + (transform.position - c).normalized * 4f;
                if (NavMesh.SamplePosition(fuite, out var hit, 3f, NavMesh.AllAreas)) { Agent.isStopped = false; Agent.SetDestination(hit.position); OublierDestination(); }
            }
            else Agent.isStopped = true;

            // Crâne en incantation : il part à la fin de la préparation si la cible vit encore.
            if (m_TirDans >= 0f)
            {
                m_TirDans -= dt;
                if (m_TirDans < 0f && m_CibleTir != null && !m_CibleTir.Mort)
                {
                    MissileCrane.Tirer(transform.position + Vector3.up * 1.5f + transform.forward * 0.5f, m_CibleTir, m_Stats.degatsJoueur, b.mageVitesseMissile, 90f, Equipe.Ennemis, gameObject, false);
                    AudioBank.Jouer(SonsDuJeu.MageSqueletteTir, transform.position + Vector3.up, 0.7f, 0.1f);
                }
            }
            if (Time.time >= m_ProchainTir && d <= m_Stats.portee)
            {
                m_ProchainTir = Time.time + m_Stats.intervalle;
                m_CibleTir = cible;
                if (P != null && cible == P.nyxessa) m_DernierCoupNyxessa = Time.time;
                m_TirDans = Mathf.Max(0.1f, m_Stats.preparation);
                m_SansFrapper = 0f;
                if (animator != null) animator.SetTrigger(P_Shoot);
                AudioBank.Jouer(SonsDuJeu.MageSqueletteIncantation, transform.position + Vector3.up, 0.6f, 0.1f);
            }
        }
    }
}
