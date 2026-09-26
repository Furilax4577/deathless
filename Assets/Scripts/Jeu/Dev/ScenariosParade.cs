using System.Collections;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Vérification en Play de la jauge de parade et de la parade parfaite du paladin (lancée par execute_code :
    /// ScenariosParade.Lancer("parfaite")). Un guerrier est posé devant le héros, un sbire à côté ; quand le coup du
    /// guerrier est annoncé (TelegraphieCoups), la garde est levée `avance` secondes avant l'impact prévu (action simulée,
    /// comme si elle venait d'InputChordResolver). Résultats dans la console (préfixe [Parade]) ; captures facultatives
    /// dans Assets/Screenshots/<nom>.png.
    /// Essais : « jauge » (capture de la jauge à mi-course, sans appui), « normale » (appui 0,2 s avant l'impact),
    /// « parfaite » (appui 0,05 s avant), « tot » (appui 0,45 s avant : trop tôt pour la parade, coup bloqué).
    public class ScenariosParade : MonoBehaviour
    {
        static ScenariosParade s_I;
        public static string Dernier = "";

        public static void Lancer(string essai, string capture = null)
        {
            if (s_I == null) s_I = new GameObject("ScenariosParade").AddComponent<ScenariosParade>();
            s_I.StopAllCoroutines();
            float avance = essai == "parfaite" ? 0.05f : essai == "normale" ? 0.2f : essai == "tot" ? 0.45f : -1f;
            s_I.StartCoroutine(s_I.Essai(essai, avance, capture));
        }

        static void Log(string t) { Dernier = t; Debug.Log("[Parade] " + t); }
        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;

        IEnumerator Essai(string essai, float avance, string capture)
        {
            var h = H;
            var pal = h != null ? h.Classe as ClassePaladin : null;
            if (pal == null || pal.Parade == null) { Log("pas de paladin local"); yield break; }
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var g = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 2.2f);
            var s = DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.0f, 1.3f);
            h.Entrees.DeplacementTest = Vector2.zero;
            h.Entrees.GardeTest = false;
            // Attendre l'annonce du coup du guerrier.
            TelegraphieCoups.Coup c = default;
            float t = 0f;
            while (t < 10f && !(TelegraphieCoups.Prochain(h, out c, 0f) && c.source == g))
            {
                t += Time.deltaTime;
                if (g != null) Viser(h, g.transform);
                yield return null;
            }
            if (c.source != g) { Log(essai + " : aucun coup annoncé"); Finir(h, g, s); yield break; }
            Vector3 posG = g.transform.position, posS = s != null ? s.transform.position : Vector3.zero;
            float pvAvant = h.Sante.Pv;
            if (avance < 0f)
            {
                // Jauge seule : capture à mi-course, sans appui.
                while (c.impact - Time.time > c.Duree * 0.45f) yield return null;
                if (capture != null) DevPartie.Capturer(capture);
                Log("jauge : visible=" + pal.Parade.Visible + " avant impact " + pal.Parade.AvantImpact.ToString("0.00") + " s, fenêtres " + pal.Parade.FenetreParade + " / " + pal.Parade.FenetreParfaite);
                yield return new WaitForSeconds(1f);
                Finir(h, g, s);
                yield break;
            }
            while (c.impact - Time.time > avance) yield return null;
            float reel = c.impact - Time.time;
            h.Entrees.GardeTest = true;
            h.Entrees.SimulerAction("AttackSecondary");
            yield return new WaitForSeconds(0.14f);
            if (capture != null) DevPartie.Capturer(capture);
            yield return new WaitForSeconds(0.5f);
            float depG = g != null ? Vector3.Distance(g.transform.position, posG) : -1f;
            float depS = s != null ? Vector3.Distance(s.transform.position, posS) : -1f;
            Log(essai + " : appui " + reel.ToString("0.000") + " s avant l'impact prévu → issue " + pal.Parade.Resultat
                + " | guerrier " + (g != null ? g.EtatCourant + (g.Statuts != null && g.Statuts.A(TypeStatut.Etourdi) ? " (Étourdi)" : "") + ", déplacé de " + depG.ToString("0.00") + " m" : "?")
                + " | sbire " + (s != null ? s.EtatCourant + (s.Statuts != null && s.Statuts.A(TypeStatut.Etourdi) ? " (Étourdi)" : "") + ", déplacé de " + depS.ToString("0.00") + " m" : "?")
                + " | PV " + pvAvant.ToString("0") + " → " + h.Sante.Pv.ToString("0")
                + " | parfaites " + pal.Parade.Parfaites + ", couverts " + pal.Parade.Couverts);
            yield return new WaitForSeconds(0.6f);
            Finir(h, g, s);
        }

        static void Viser(Heros h, Transform cible)
        {
            Vector3 d = cible.position - h.transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            h.transform.rotation = Quaternion.LookRotation(d);
            if (Partie.Instance.cameraJeu != null) Partie.Instance.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        static void Finir(Heros h, Squelette g, Squelette s)
        {
            if (h != null) { h.Entrees.GardeTest = false; h.Entrees.DeplacementTest = null; }
            if (g != null) g.Desintegrer(true);
            if (s != null) s.Desintegrer(true);
        }
    }
}
