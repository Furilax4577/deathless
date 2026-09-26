using System.Collections;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Morgrim, version Massue (wiki : ennemis.md, Morgrim ; décidé le 26/09/2026) : colosse qui contrôle la zone,
    /// thème Terre, yeux jaune-orangé habituels (Yeux_Squelette.mat, comme le Golem actuel). Trois compétences,
    /// choisies selon la distance à la cible et le nombre de joueurs proches (GameBalance, préfixe morgrimMassue*) :
    /// Fracas (compétence par défaut, reprend le coup de zone du Golem actuel + étourdissement), Tourbillon (dégâts
    /// continus à 360°, punit les groupes serrés) et Charge écrasante (fonce en ligne droite, renverse le premier
    /// joueur touché). Chacune a sa propre recharge, sauf Fracas (cadence de base du Golem).
    public class MorgrimMassue : MorgrimVariant
    {
        enum Competence { Fracas, Tourbillon, Charge }

        Competence m_Competence;
        float m_ProchainTourbillon = -99f, m_ProchaineCharge = -99f;

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            base.Initialiser(stats, multiplicateurPV);
            // Porte d'engagement élargie pour laisser la Charge écrasante se déclencher sur une cible encore loin ;
            // la compétence réellement choisie a sa propre portée (Choisir/Telegraphier ci-dessous).
            m_Stats.portee = Mathf.Max(m_Stats.portee, B.morgrimMassueChargeDistance);
        }

        protected override void CommencerAttaque(Heros cible)
        {
            var b = B;
            m_Competence = Choisir(cible);
            switch (m_Competence)
            {
                case Competence.Tourbillon:
                    m_Stats.preparation = b.morgrimMassueTourbillonPreparation;
                    m_ProchainTourbillon = Time.time + b.morgrimMassueTourbillonRecharge;
                    break;
                case Competence.Charge:
                    m_Stats.preparation = b.morgrimMassueChargePreparation;
                    m_ProchaineCharge = Time.time + b.morgrimMassueChargeRecharge;
                    break;
                default:
                    m_Stats.preparation = b.morgrimMassueFracasPreparation;
                    break;
            }
            base.CommencerAttaque(cible);
            PoserTelegraphie();
        }

        Competence Choisir(Heros cible)
        {
            var b = B;
            int proches = JoueursProches(b.morgrimJoueursProchesRayon);
            if (proches >= 2 && Time.time >= m_ProchainTourbillon) return Competence.Tourbillon;
            float distance = cible != null ? Distance(cible.transform.position) : (P != null && P.nyxessa != null ? Distance(P.nyxessa.transform.position) : 0f);
            if (distance > b.morgrimMassueFracasRayon * 1.4f && Time.time >= m_ProchaineCharge) return Competence.Charge;
            return Competence.Fracas;
        }

        void PoserTelegraphie()
        {
            var b = B;
            Vector3 point = transform.position + Vector3.up * 0.05f;
            switch (m_Competence)
            {
                case Competence.Tourbillon:
                    Telegraphier(point, VfxTheme.Terre, b.morgrimMassueTourbillonRayon, m_Stats.preparation);
                    break;
                case Competence.Charge:
                    Telegraphier(transform.position + transform.forward * (b.morgrimMassueChargeDistance * 0.5f) + Vector3.up * 0.05f,
                        VfxTheme.Terre, b.morgrimMassueChargeDistance * 0.55f, m_Stats.preparation, 26f);
                    break;
                default:
                    Telegraphier(point, VfxTheme.Terre, b.morgrimMassueFracasRayon, m_Stats.preparation);
                    break;
            }
        }

        protected override void Frapper()
        {
            switch (m_Competence)
            {
                case Competence.Tourbillon: StartCoroutine(FaireTourbillon()); break;
                case Competence.Charge: StartCoroutine(FaireCharge()); break;
                default: FaireFracas(); break;
            }
        }

        /// Frappe la boule à pointes au sol : onde de choc en cercle qui étourdit (reprend le coup de zone actuel du
        /// Golem, OndeDeChoc/DirtBurst), + gerbe de gemmes Terre.
        void FaireFracas()
        {
            var b = B;
            Vector3 impact = transform.position + transform.forward * (b.morgrimMassueFracasRayon * 0.35f);
            if (EffetsJeu.Terre != null) DirtBurst.Spawn(impact, EffetsJeu.Terre, 1f);
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabOndeGolem != null)
            {
                var onde = Instantiate(fx.prefabOndeGolem, impact + Vector3.up, Quaternion.LookRotation(transform.forward));
                var o = onde.GetComponent<OndeDeChoc>();
                if (o != null) o.Jouer();
                Destroy(onde, 3f);
            }
            Impact(impact + Vector3.up * 0.2f, VfxTheme.Terre, b.morgrimMassueFracasRayon);
            AudioBank.Jouer(SonsDuJeu.GolemCoup, impact, 1f);
            float r = b.morgrimMassueFracasRayon;
            if (P == null) return;
            foreach (var h in P.TousLesHeros)
            {
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - impact; d.y = 0f;
                if (d.magnitude > r) continue;
                float reel = h.Sante.Encaisser(new InfoDegats
                {
                    montant = b.morgrimMassueFracasDegats, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                    point = h.transform.position + Vector3.up, direction = (h.transform.position - transform.position).normalized
                });
                if (reel > 0f) h.Statuts?.Ajouter(TypeStatut.Etourdi, b.morgrimMassueFracasEtourdi, 1f, OrigineStatut.Ennemi);
            }
            if (P.nyxessa == null) return;
            Vector3 dn = P.nyxessa.transform.position - impact; dn.y = 0f;
            var bo = BouclierNyxessa.Instance;
            if (dn.magnitude > (bo != null && bo.Leve ? RayonContact + 0.8f : r + 1.3f)) return;
            m_DernierCoupNyxessa = Time.time;
            P.nyxessa.Encaisser(new InfoDegats { montant = b.morgrimMassueFracasDegatsNyxessa, equipeSource = Equipe.Ennemis, source = gameObject, point = impact + Vector3.up * 1.5f, direction = transform.forward });
        }

        /// Fait tournoyer la boule à pointes autour de lui : dégâts continus et léger recul pour qui reste dans le rayon.
        IEnumerator FaireTourbillon()
        {
            var b = B;
            AudioBank.Jouer(SonsDuJeu.GolemCoup, transform.position, 1f);
            float duree = b.morgrimMassueTourbillonDuree;
            float t = 0f, prochainEclat = 0f;
            while (t < duree)
            {
                float dt = Time.deltaTime;
                t += dt;
                if (t >= prochainEclat)
                {
                    prochainEclat = t + 0.15f;
                    Impact(transform.position + Vector3.up * 0.15f, VfxTheme.Terre, b.morgrimMassueTourbillonRayon, transform.forward, 360f);
                }
                if (P != null)
                {
                    foreach (var h in P.TousLesHeros)
                    {
                        if (h == null || !h.Vivant) continue;
                        Vector3 d = h.transform.position - transform.position; d.y = 0f;
                        if (d.magnitude > b.morgrimMassueTourbillonRayon) continue;
                        h.Sante.Encaisser(new InfoDegats
                        {
                            montant = b.morgrimMassueTourbillonDegatsParSeconde * dt, equipeSource = Equipe.Ennemis, source = gameObject,
                            point = h.transform.position + Vector3.up, direction = d.normalized, continu = true
                        });
                    }
                }
                yield return null;
            }
        }

        /// Fonce en ligne droite sur sa cible et renverse (Étourdi) le premier joueur touché ; pas de parade en cours
        /// de charge (comme la charge bélier du Paladin), contrairement au reste des coups de Morgrim.
        IEnumerator FaireCharge()
        {
            var b = B;
            AudioBank.Jouer(SonsDuJeu.GolemCoup, transform.position, 1f);
            float duree = Mathf.Max(0.1f, b.morgrimMassueChargeDistance / Mathf.Max(0.1f, b.morgrimMassueChargeVitesse));
            float t = 0f;
            bool touche = false;
            while (t < duree && !touche)
            {
                float dt = Time.deltaTime;
                t += dt;
                if (Agent != null && Agent.enabled) Agent.Move(transform.forward * (b.morgrimMassueChargeVitesse * dt));
                if (P != null)
                {
                    foreach (var h in P.TousLesHeros)
                    {
                        if (h == null || !h.Vivant) continue;
                        Vector3 d = h.transform.position - transform.position; d.y = 0f;
                        if (d.magnitude > b.morgrimMassueChargeLargeur || Vector3.Angle(transform.forward, d) > 70f) continue;
                        float reel = h.Sante.Encaisser(new InfoDegats
                        {
                            montant = b.morgrimMassueChargeDegats, equipeSource = Equipe.Ennemis, source = gameObject,
                            point = h.transform.position + Vector3.up, direction = d.normalized
                        });
                        if (reel > 0f) h.Statuts?.Ajouter(TypeStatut.Etourdi, b.morgrimMassueChargeEtourdi, 1f, OrigineStatut.Ennemi);
                        Impact(h.transform.position, VfxTheme.Terre, 1.2f, transform.forward, 90f);
                        touche = true;
                        break;
                    }
                }
                yield return null;
            }
        }
    }
}
