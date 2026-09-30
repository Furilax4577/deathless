using UnityEngine;

namespace Deathless.Jeu
{
    /// Mini-boss de la nuit 10 (décision utilisateur) : Golem squelette (rig Large, hache `Skeleton_Golem_Axe_Large`).
    /// Lent, très résistant ; gros coup de zone devant lui, long à préparer : parable (garde levée au bon moment) et
    /// esquivable (roulade hors du cercle). N'est pas repoussé par la charge bélier et n'est étourdi qu'à moitié.
    public class Golem : Squelette
    {
        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            var b = GameBalance.Courant;
            var s = new StatsSquelette
            {
                pv = b.golemPV, vitesse = b.golemVitesse, degatsJoueur = b.golemDegatsJoueur, degatsNyxessa = b.golemDegatsNyxessa,
                intervalle = b.golemIntervalle, preparation = b.golemPreparation, portee = b.golemRayonCoup
            };
            base.Initialiser(s, multiplicateurPV);
            Agent.radius = 0.9f;
        }

        public override bool Repoussable => false;
        public override float FacteurEtourdissement => 0.5f;

        public override void Etourdir(float duree, int sourceId = 0) { if (RelaiEtourdir(duree, sourceId)) return; base.Etourdir(duree * FacteurEtourdissement, sourceId); }

        public override void Repousser(Vector3 deplacement, float etourdi, int sourceId) => Etourdir(etourdi, sourceId);

        Vector3 PointImpact => transform.position + transform.forward * (m_Stats.portee * 0.6f);

        protected override void Frapper()
        {
            Vector3 impact = PointImpact;
            // Terre, onde brève et son : joués ici et rejoués chez les clients (EffetsBoss, 30/09/2026 ; avant, seul
            // l'hôte les voyait).
            EffetsBoss.Diffuser(this, EffetBoss.OndeGolem, impact);
            float r = m_Stats.portee;
            if (P != null)
            {
                var tous = P.TousLesHeros;
                for (int i = 0; i < tous.Count; i++)
                {
                    var h = tous[i];
                    if (h == null || !h.Vivant) continue;
                    Vector3 d = h.transform.position - impact; d.y = 0f;
                    if (d.magnitude > r) continue;
                    h.Sante.Encaisser(new InfoDegats
                    {
                        montant = m_Stats.degatsJoueur, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                        point = h.transform.position + Vector3.up, direction = (h.transform.position - transform.position).normalized
                    });
                }
                if (P.nyxessa != null)
                {
                    Vector3 dn = P.nyxessa.transform.position - impact; dn.y = 0f;
                    var bo = BouclierNyxessa.Instance;
                    if (dn.magnitude <= (bo != null && bo.Leve ? RayonContact + 0.8f : r + 1.3f))
                    {
                        m_DernierCoupNyxessa = Time.time;
                        P.nyxessa.Encaisser(new InfoDegats { montant = m_Stats.degatsNyxessa, equipeSource = Equipe.Ennemis, source = gameObject, point = impact + Vector3.up * 1.5f, direction = transform.forward });
                    }
                }
            }
        }

        protected override void CommencerAttaque(Heros cible)
        {
            base.CommencerAttaque(cible);
            // Le coup de zone touche aussi Nyxessa si elle est dans le cercle : on garde la cible choisie pour l'orientation.
        }
    }
}
