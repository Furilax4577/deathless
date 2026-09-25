using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Boss final de la nuit 12 (décision utilisateur ; le nom « Nécromancien » lui est réservé). Garde ses distances,
    /// tire le missile crâne à taille normale sur le joueur proche (sinon sur Nyxessa) et invoque des sbires qui sortent de
    /// terre autour de lui. Ses yeux brillent en vert Nyxessa (matériau propre posé par Deathless > Jeu).
    public class Necromancien : Squelette
    {
        static readonly int P_Shoot = Animator.StringToHash("Shoot");
        static readonly int P_Cast = Animator.StringToHash("Cast");

        float m_ProchainTir, m_ProchaineInvocation;
        int m_Invoques;
        Sante m_CibleTir;
        float m_TirDans = -1f;

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            var b = GameBalance.Courant;
            var s = new StatsSquelette { pv = b.necroPV, vitesse = b.necroVitesse, degatsJoueur = b.necroDegats, degatsNyxessa = b.necroDegats, intervalle = b.necroIntervalleTir, preparation = 0.5f, portee = b.necroPorteeTir };
            base.Initialiser(s, multiplicateurPV);
            m_ProchainTir = Time.time + 3f;
            m_ProchaineInvocation = Time.time + 6f;
        }

        public int Invoques { get => m_Invoques; set => m_Invoques = Mathf.Max(0, value); }

        protected override void MajMarche(float dt) => MajDistance(dt);
        protected override void MajPoursuite(float dt) => MajDistance(dt);

        void MajDistance(float dt)
        {
            var b = B;
            var j = JoueurProche(b.necroPorteeTir + 4f);
            Sante cible = j != null ? j.Sante : (P != null ? P.nyxessa : null);
            if (cible == null) return;
            Vector3 c = cible.transform.position;
            float d = Distance(c);
            Tourner(c);
            // Garder ses distances : s'approcher au-delà de la distance haute, reculer en deçà de la distance basse.
            if (d > b.necroDistance.y) { Agent.isStopped = false; Agent.speed = b.necroVitesse; Agent.SetDestination(c); }
            else if (d < b.necroDistance.x)
            {
                Vector3 fuite = transform.position + (transform.position - c).normalized * 5f;
                if (NavMesh.SamplePosition(fuite, out var hit, 3f, NavMesh.AllAreas)) { Agent.isStopped = false; Agent.speed = b.necroVitesse * 0.8f; Agent.SetDestination(hit.position); }
            }
            else Agent.isStopped = true;

            if (m_TirDans >= 0f)
            {
                m_TirDans -= dt;
                if (m_TirDans < 0f && m_CibleTir != null && !m_CibleTir.Mort)
                    MissileCrane.Tirer(transform.position + Vector3.up * 1.6f + transform.forward * 0.6f, m_CibleTir, b.necroDegats, b.necroVitesseMissile, 90f, Equipe.Ennemis, gameObject, false);
            }
            if (Time.time >= m_ProchainTir && d <= b.necroPorteeTir)
            {
                m_ProchainTir = Time.time + b.necroIntervalleTir;
                m_CibleTir = cible;
                if (cible == (P != null ? P.nyxessa : null)) m_DernierCoupNyxessa = Time.time;
                m_TirDans = 0.35f;
                if (animator != null) animator.SetTrigger(P_Shoot);
            }
            if (Time.time >= m_ProchaineInvocation)
            {
                m_ProchaineInvocation = Time.time + b.necroIntervalleInvocation;
                if (m_Invoques < b.necroInvoquesMax && DirecteurVagues.Instance != null)
                {
                    if (animator != null) animator.SetTrigger(P_Cast);
                    int n = Mathf.Min(b.necroSbiresParInvocation, b.necroInvoquesMax - m_Invoques);
                    m_Invoques += DirecteurVagues.Instance.Invoquer(this, n);
                    AudioBank.Jouer(SonsDuJeu.Invocation, transform.position, 1f);
                }
            }
        }

        public override void Repousser(Vector3 deplacement, float etourdi, int sourceId) { if (RelaiRepousser(deplacement, etourdi)) return; base.Repousser(deplacement * 0.6f, etourdi, sourceId); }
    }
}
