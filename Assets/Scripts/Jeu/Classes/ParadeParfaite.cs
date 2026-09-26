using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Jauge de parade et parade parfaite du paladin local (Wiki classe-paladin.md, « Garde et parade » ; réseau :
    /// Docs/reseau.md, « Parade parfaite »). Utilisée par ClassePaladin, qui garde la main sur l'action et l'animation.
    ///
    /// - La jauge suit le coup parable annoncé le plus proche de son impact (TelegraphieCoups) : curseur vers l'impact,
    ///   fenêtre de parade (GameBalance.paradeFenetre) et fenêtre parfaite (paradeParfaiteFenetre) avant l'impact.
    /// - Jugement chez le propriétaire, sur l'impact prévu qu'il connaît : un appui (LT) dans la fenêtre parfaite lance le
    ///   coup de bouclier ; un appui dans la fenêtre de parade pare le coup quand il arrive (ClassePaladin.Intercepter).
    /// - Effets (repousse, étourdissement) appliqués par l'autorité : ici en solo et chez l'hôte (Appliquer) ; un client
    ///   les demande à l'hôte (HerosReseau.ParadeParfaiteRpc), qui les valide avec une tolérance (Demande).
    public sealed class ParadeParfaite : IJaugeParade
    {
        /// Temps pendant lequel l'issue d'un coup reste affichée par la jauge (s).
        public const float AffichageResultat = 0.45f;

        readonly Heros m_H;
        TelegraphieCoups.Coup m_Coup;
        bool m_Suivi;
        float m_Appui = -1f;
        ResultatParade m_Resultat;
        float m_ResultatDepuis = -99f;
        // Parade parfaite en cours : le coup de cette source est couvert (paré) quand il arrive.
        Squelette m_ParfaiteSource;
        float m_ParfaiteDepuis = -99f;

        static GameBalance B => GameBalance.Courant;

        public ParadeParfaite(Heros h) { m_H = h; }

        /// Tests : parades parfaites lancées et coups couverts par elles.
        public int Parfaites { get; private set; }
        public int Couverts { get; private set; }

        // ----------------------------------------------------------------- Suivi du coup (chaque image)

        public void Maj()
        {
            float t = Time.time;
            bool montreIssue = m_Suivi && m_Resultat != ResultatParade.Aucun && t - m_ResultatDepuis < AffichageResultat * 0.7f;
            if (TelegraphieCoups.Prochain(m_H, out var c))
            {
                bool autre = !m_Suivi || c.source != m_Coup.source || Mathf.Abs(c.impact - m_Coup.impact) > 0.05f;
                if (autre && !montreIssue) Suivre(c);
            }
            else if (m_Suivi && m_Resultat == ResultatParade.Aucun && (!TelegraphieCoups.Actif(m_Coup) || t > m_Coup.impact + 0.15f))
                m_Suivi = false;   // coup annulé (ennemi étourdi, mort) ou passé sans issue connue
            else if (m_Suivi && m_Resultat != ResultatParade.Aucun && t - m_ResultatDepuis >= AffichageResultat)
                m_Suivi = false;
        }

        void Suivre(TelegraphieCoups.Coup c)
        {
            m_Coup = c;
            m_Suivi = true;
            m_Appui = -1f;
            m_Resultat = ResultatParade.Aucun;
            m_ResultatDepuis = -99f;
        }

        /// Appui sur la garde (LT) : noté sur la jauge (repère de l'appui).
        public void NoterAppui()
        {
            if (!m_Suivi || m_Appui >= 0f || m_Resultat != ResultatParade.Aucun) return;
            float a = m_Coup.impact - Time.time;
            if (a >= -B.paradeParfaiteGrace) m_Appui = Mathf.Max(0f, a);
        }

        /// Appui dans la fenêtre parfaite du coup le plus proche, attaquant devant (`avant`, demi-angle de la garde) ?
        public bool TenterParfaite(Vector3 avant, out Squelette source, out float avance)
        {
            source = null;
            avance = 0f;
            var b = B;
            if (!TelegraphieCoups.Prochain(m_H, out var c, b.paradeParfaiteGrace)) return false;
            avance = c.impact - Time.time;
            if (avance > b.paradeParfaiteFenetre || avance < -b.paradeParfaiteGrace) return false;
            Vector3 vers = c.source.transform.position - m_H.transform.position; vers.y = 0f;
            avant.y = 0f;
            if (vers.sqrMagnitude > 0.01f && avant.sqrMagnitude > 0.01f && Vector3.Angle(avant, vers) > b.gardeDemiAngle) return false;
            source = c.source;
            return true;
        }

        /// La parade parfaite part : le coup de `source` sera paré à son arrivée, l'issue est montrée tout de suite.
        public void Commencer(Squelette source)
        {
            m_ParfaiteSource = source;
            m_ParfaiteDepuis = Time.time;
            Parfaites++;
            if (m_Suivi && source == m_Coup.source) { m_Resultat = ResultatParade.Parfaite; m_ResultatDepuis = Time.time; }
        }

        /// Le coup de `source` arrive-t-il pendant une parade parfaite lancée contre lui ? (paré sans condition)
        public bool Couvre(GameObject source)
        {
            if (source == null || m_ParfaiteSource == null || m_ParfaiteSource.gameObject != source) return false;
            if (Time.time - m_ParfaiteDepuis > B.paradeParfaiteFenetre + B.paradeParfaiteGrace + TelegraphieCoups.Retenue) return false;
            Couverts++;
            return true;
        }

        /// Parade normale : l'appui (Time.time `gardeDepuis`) tombe-t-il dans la fenêtre de parade ? Jugé sur l'instant où
        /// le coup arrive (règle d'avant la jauge) ou sur l'impact prévu du coup de `source` (ce que montre la jauge) :
        /// l'un ou l'autre suffit, pour qu'un léger écart du réseau entre les deux ne coûte pas une parade.
        public bool DansFenetre(GameObject source, float gardeDepuis)
        {
            if (Dans(Time.time - gardeDepuis)) return true;
            return TelegraphieCoups.ImpactPrevu(source, m_H, out float impact) && Dans(impact - gardeDepuis);
        }

        static bool Dans(float avance) => avance >= -B.paradeParfaiteGrace && avance <= B.paradeFenetre;

        /// Issue d'un coup reçu (bloqué, paré, touché) : montrée par la jauge si c'est le coup suivi.
        public void Noter(GameObject source, ResultatParade r)
        {
            if (!m_Suivi || source == null || m_Coup.source == null || m_Coup.source.gameObject != source) return;
            if (m_Resultat == ResultatParade.Parfaite) return;   // déjà montrée à l'appui
            m_Resultat = r;
            m_ResultatDepuis = Time.time;
        }

        // ----------------------------------------------------------------- IJaugeParade

        public bool Visible => m_Suivi && m_H != null && m_H.Vivant
            && (m_Resultat != ResultatParade.Aucun ? Time.time - m_ResultatDepuis < AffichageResultat : Time.time <= m_Coup.impact + 0.15f);
        public float AvantImpact => m_Suivi ? m_Coup.impact - Time.time : 0f;
        public float Duree => Mathf.Max(0.2f, B.paradeJaugeDuree);
        public float FenetreParade => B.paradeFenetre;
        public float FenetreParfaite => B.paradeParfaiteFenetre;
        public float Appui => m_Appui;
        public ResultatParade Resultat => m_Resultat;
        public float DepuisResultat => Time.time - m_ResultatDepuis;

        // ----------------------------------------------------------------- Effets (autorité)

        static readonly Dictionary<Heros, float> s_DerniereValidee = new Dictionary<Heros, float>();

        /// Hôte : un client demande une parade parfaite contre `attaquant`. Validée si ce squelette a bien annoncé un coup
        /// contre ce héros, et si la demande arrive entre le début de sa préparation et son impact + aller-retour + marge
        /// (le client a jugé sur l'impact qu'il voyait, décalé de la latence) ; au plus une toutes les 0,3 s.
        public static void Demande(Heros h, Squelette attaquant, Vector3 direction, float rtt, float avance)
        {
            var p = h != null ? h.Partie : null;
            string refus = null;
            float impact = 0f, duree = 0f;
            rtt = Mathf.Clamp(rtt, 0f, 0.6f);
            if (h == null || !h.Vivant || !(h.Classe is ClassePaladin)) refus = "héros indisponible";
            else if (attaquant == null || !attaquant.Vivant) refus = "attaquant absent";
            else if (!attaquant.CoupAnnonceSur(h, out impact, out duree)) refus = "aucun coup annoncé contre lui";
            else if (Time.time < impact - duree - 0.05f || Time.time > impact + rtt + B.paradeParfaiteToleranceReseau)
                refus = "hors délai (" + (Time.time - impact).ToString("+0.00;-0.00") + " s de l'impact, aller-retour " + rtt.ToString("0.00") + " s)";
            else if (s_DerniereValidee.TryGetValue(h, out float d) && Time.time - d < 0.3f) refus = "trop rapprochée";
            if (refus != null)
            {
                if (p != null) p.Journal("Parade parfaite refusée (" + refus + ")");
                return;
            }
            s_DerniereValidee[h] = Time.time;
            Appliquer(h, direction, attaquant, "demandée par le client, appui " + avance.ToString("0.00") + " s avant l'impact prévu, reçue à "
                + (Time.time - impact).ToString("+0.00;-0.00") + " s de l'impact de l'hôte");
        }

        /// Autorité : coup de bouclier du paladin `h` vers `direction`. Les ennemis dans le cône (paradeParfaitePortee,
        /// paradeParfaiteDemiAngle) et l'attaquant, s'il est proche, sont repoussés et étourdis (statut Étourdi).
        public static int Appliquer(Heros h, Vector3 direction, Squelette attaquant, string note = null)
        {
            if (h == null) return 0;
            var b = B;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = h.transform.forward;
            direction.Normalize();
            Vector3 o = h.transform.position;
            var touches = new List<Squelette>();
            foreach (var s in Combat.Ennemis(o, direction, b.paradeParfaitePortee, b.paradeParfaiteDemiAngle))
            {
                var sq = s != null ? s.GetComponent<Squelette>() : null;
                if (sq != null && sq.Vivant && !touches.Contains(sq)) touches.Add(sq);
            }
            if (attaquant != null && attaquant.Vivant && !touches.Contains(attaquant))
            {
                Vector3 d = attaquant.transform.position - o; d.y = 0f;
                if (d.magnitude <= b.paradeParfaitePortee + 1.2f) touches.Add(attaquant);
            }
            foreach (var sq in touches)
            {
                Vector3 d = sq.transform.position - o; d.y = 0f;
                Vector3 sens = d.sqrMagnitude > 0.04f ? Vector3.Slerp(direction, d.normalized, 0.6f).normalized : direction;
                sq.Repousser(sens * b.paradeParfaiteRepousse, b.paradeParfaiteEtourdi, h.Id);
            }
            if (h.Partie != null)
                h.Partie.Journal("Parade parfaite : " + touches.Count + " repoussé(s) et étourdi(s)" + (note != null ? " (" + note + ")" : ""));
            return touches.Count;
        }

        // ----------------------------------------------------------------- Visuel (tous les postes)

        /// Éclat renforcé du coup de bouclier : éclat de parade sur le bouclier, gerbe de gemmes or et blanc (thème
        /// Sacre) dans le cône de la repousse, lumière moyenne. Couleurs de la palette, jamais en dur.
        public static void Eclat(Vector3 point, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.Normalize();
            if (ParadeEclat.Instance != null) ParadeEclat.Instance.Jouer(point, direction);
            var mat = EffetsJeu.Gemmes;
            if (mat == null) return;
            if (ParadeEclat.Instance == null) ParadeEclat.Eclat(point, direction, mat);
            Color baseC = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Base, new Color(0.722f, 0.565f, 0.227f));
            Color vif = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Vif, new Color(0.910f, 0.784f, 0.447f));
            Color coeur = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Coeur, new Color(0.957f, 0.886f, 0.659f));
            var g = GemmesVolantes.Creer("ParadeParfaite", mat, 80, true);
            float demi = B.paradeParfaiteDemiAngle;
            // Gerbe en éventail au ras du bouclier, qui file dans le cône de la repousse.
            for (int i = 0; i < 13; i++)
            {
                float a = Mathf.Lerp(-demi, demi, i / 12f) + Random.Range(-4f, 4f);
                Vector3 d = Quaternion.Euler(0f, a, 0f) * direction;
                Vector3 v = d * Random.Range(5.5f, 7.5f) + Vector3.up * Random.Range(0.2f, 0.9f);
                Color c = i % 3 == 0 ? coeur * 1.5f : Color.Lerp(vif * 1.2f, baseC, Random.value * 0.6f);
                g.Emettre(point + d * 0.15f, v, Random.Range(0.05f, 0.08f), 0.32f, c, 0f, 7f, 0.02f, 0.25f, new Vector3(0.5f, 0.5f, 1.8f));
            }
            // Arc au sol au bout de la portée : le cône touché se lit d'un coup d'œil.
            Vector3 sol = new Vector3(point.x, point.y - 1.1f, point.z);
            for (int i = 0; i < 11; i++)
            {
                float a = Mathf.Lerp(-demi, demi, i / 10f);
                Vector3 d = Quaternion.Euler(0f, a, 0f) * direction;
                g.Emettre(sol + d * 0.6f, d * 4.5f, 0.045f, 0.3f, vif * 1.1f, 0f, 9f, 0.02f, 0.35f, default, 0.03f);
            }
            VfxLumiere.Eclat(point, VfxTheme.Sacre, VfxTailleLumiere.Moyenne, 0.05f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reinitialiser() => s_DerniereValidee.Clear();
    }
}
