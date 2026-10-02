using System.Text;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Outils de test en Play des boutiques du mécano et du druide, des potions et du crochetage (03/10/2026 ; appelés par
    /// execute_code, préfixe [Boutiques] dans la console). Aucun effet hors Play ; rien n'est sauvegardé.
    ///   ScenariosBoutiques.Etat()                  inventaire, or, invite courante, crochetage en cours
    ///   ScenariosBoutiques.Poser()                 pose les composants sur les ancres (Boutiques.Assurer) : renvoie le nombre de boutiques
    ///   ScenariosBoutiques.Aller("mecano"|"druide") héros devant le vendeur, tourné vers lui (2,4 m ou moins de l'ancre)
    ///   ScenariosBoutiques.Interagir()             touche Interagir au vendeur : ouvre le menu d'achat
    ///   ScenariosBoutiques.Acheter(i)              clic sur la ligne i du menu ouvert (même chemin que le bouton)
    ///   ScenariosBoutiques.Payer("CleBronze")      achat direct par nom d'article (Boutiques.Payer)
    ///   ScenariosBoutiques.Or(n)                   fixe la caisse commune
    ///   ScenariosBoutiques.Donner("PotionSante", n) / Vider()   remplit ou vide l'inventaire du joueur local (test)
    ///   ScenariosBoutiques.Boire("sante"|"mana"|"endurance")  action de la potion, comme la croix directionnelle
    ///   ScenariosBoutiques.Maintenir(vrai)         Interagir maintenu imposé (mini-jeu de crochetage)
    ///   ScenariosBoutiques.Pilote(vrai)            un pilote automatique tient Interagir pour garder l'aiguille dans la zone
    public class ScenariosBoutiques : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosBoutiques() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosBoutiques"); }

        static ScenariosBoutiques s_I;
        public static string Dernier = "";
        static ScenariosBoutiques I { get { if (s_I == null) s_I = new GameObject("ScenariosBoutiques").AddComponent<ScenariosBoutiques>(); return s_I; } }

        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;
        static void Log(string t) { Dernier = t; Debug.Log("[Boutiques] " + t); }

        bool m_Pilote;

        public static string Etat()
        {
            var p = P; var h = H;
            if (p == null) return "pas de partie";
            var sb = new StringBuilder();
            sb.Append(p.Etat.phase).Append(" | caisse ").Append(p.Etat.orEquipe).Append(" | ").Append(Inventaire.Resume(p.JoueurLocal));
            if (h != null)
            {
                sb.Append(" | PV ").Append(h.Sante.Pv.ToString("F0")).Append("/").Append(h.Sante.pvMax.ToString("F0")).Append(" end ").Append(h.Endurance.ToString("F0")).Append("/").Append(h.EnduranceMax.ToString("F0"));
                if (h.Classe != null && h.Classe.JaugeMax > 0f) sb.Append(" jauge ").Append(h.Classe.ValeurJauge.ToString("F0")).Append("/").Append(h.Classe.JaugeMax.ToString("F0"));
                if (h.RegenBoostRestante > 0f) sb.Append(" regen×2 ").Append(h.RegenBoostRestante.ToString("F1")).Append(" s");
                var pi = PointInteraction.Courant(h, out string inv);
                sb.Append(" | invite '").Append(inv ?? "(aucune)").Append("'").Append(pi != null ? " [" + pi.GetType().Name + "]" : "");
            }
            var c = Crochetage.Courant;
            if (c != null) sb.Append(" | crochetage ").Append(c.Serrure).Append(" essai ").Append(c.Essai).Append("/").Append(c.EssaisMax).Append(" aiguille ").Append(c.Aiguille.ToString("F2")).Append(" zone ").Append(c.ZoneCentre.ToString("F2")).Append("±").Append((c.ZoneLargeur * 0.5f).ToString("F2")).Append(" jauge ").Append(c.Progression.ToString("F2")).Append(" '").Append(c.Message).Append("'");
            string msg = DonjonJeu.Instance != null ? DonjonJeu.Instance.Message : "";
            if (!string.IsNullOrEmpty(msg)) sb.Append(" | message '").Append(msg).Append("'");
            return sb.ToString();
        }

        public static int Poser() => Boutiques.Assurer();

        public static string Aller(string ou)
        {
            var h = H;
            if (h == null) return "pas de héros";
            var ancre = ou == "mecano" ? Boutiques.AncreMecano() : Boutiques.AncreDruide();
            if (ancre == null) return "pas d'ancre " + ou;
            Vector3 devant = ancre.transform.forward; devant.y = 0f; devant.Normalize();
            Vector3 p = ancre.transform.position + devant * 1.3f;
            p.y = ancre.transform.position.y - 0.95f;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            DevPartie.PlacerHeros(p + Vector3.up * 0.05f, ancre.transform.position - devant * 0.2f);
            return "héros à " + h.transform.position.ToString("F1") + " (" + ou + ")";
        }

        public static string Interagir()
        {
            var h = H;
            if (h == null) return "pas de héros";
            var pi = PointInteraction.Courant(h, out string inv);
            bool ok = PointInteraction.InteragirIci(h);
            return "interagir sur '" + (inv ?? "(aucune)") + "' → " + ok;
        }

        /// Clic sur la ligne `i` du menu d'achat du vendeur le plus proche (Interagir doit avoir ouvert le menu).
        public static string Acheter(int i)
        {
            var h = H;
            if (h == null) return "pas de héros";
            var pi = PointInteraction.Courant(h, out _) as BoutiqueVillage;
            if (pi == null) return "pas de boutique à portée";
            pi.Acheter(i);
            return pi.Message + (pi.MessageRefus ? " [refus]" : " [ok]") + " | " + Etat();
        }

        public static string Payer(string article)
        {
            if (!System.Enum.TryParse(article, out ArticleBoutique a)) return "article inconnu : " + article;
            var h = H;
            string r = Boutiques.Payer(a, h != null ? h.Id : 1, out bool ok);
            return r + (ok ? " [ok]" : " [refus]") + " | " + Etat();
        }

        public static void Or(int n) { if (P != null) P.Etat.orEquipe = n; }

        public static string Donner(string article, int n)
        {
            if (!System.Enum.TryParse(article, out ArticleBoutique a)) return "article inconnu : " + article;
            var j = P != null ? P.JoueurLocal : null;
            if (j == null) return "pas de joueur";
            int tours = a == ArticleBoutique.KitCrochetage ? Mathf.Max(1, n / Mathf.Max(1, GameBalance.Courant.crochetsParKit)) : n;
            for (int i = 0; i < tours; i++) Inventaire.Ajouter(j, a);
            return Inventaire.Resume(j);
        }

        public static string Vider()
        {
            var j = P != null ? P.JoueurLocal : null;
            if (j == null) return "pas de joueur";
            Inventaire.Detasser(0, j);
            return Inventaire.Resume(j);
        }

        public static string Boire(string potion)
        {
            var h = H;
            if (h == null) return "pas de héros";
            string action = potion == "mana" ? "DrinkPotionMana" : potion == "endurance" ? "DrinkPotionStamina" : "DrinkPotion";
            h.Entrees.SimulerAction(action);
            return action + " → " + Etat();
        }

        public static void Maintenir(bool oui) { var h = H; if (h != null) h.Entrees.InteragirTest = oui; }

        public static void Pilote(bool oui)
        {
            var i = I;
            i.m_Pilote = oui;
            if (!oui) Maintenir(false);
        }

        void Update()
        {
            if (!m_Pilote) return;
            var c = Crochetage.Courant;
            var h = H;
            if (c == null || h == null) { if (h != null) h.Entrees.InteragirTest = false; return; }
            // Garde l'aiguille sous le centre de la zone : monte (maintenu) quand elle est en dessous ou qu'elle ne monte pas assez vite.
            float ecart = c.ZoneCentre - c.Aiguille;
            h.Entrees.InteragirTest = ecart > 0.03f || (Mathf.Abs(ecart) <= 0.03f && c.Aiguille < c.ZoneCentre);
        }
    }
}
