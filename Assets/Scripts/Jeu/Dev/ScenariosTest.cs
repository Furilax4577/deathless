using System.Collections;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Scénarios de vérification en Play (lancés par execute_code : ScenariosTest.Lancer("combat")). Les gestes passent
    /// par la manette virtuelle (EntreesSimulees → DeathlessControls → InputChordResolver) ; les résultats vont dans la
    /// console (préfixe [Test]) et les captures dans Assets/Screenshots/v01_*.png.
    public class ScenariosTest : MonoBehaviour
    {
        static ScenariosTest s_I;
        public static string Dernier = "";

        public static void Lancer(string nom)
        {
            if (s_I == null) s_I = new GameObject("ScenariosTest").AddComponent<ScenariosTest>();
            s_I.StopAllCoroutines();
            EntreesSimulees.ToutRelacher();
            switch (nom)
            {
                case "combat": s_I.StartCoroutine(s_I.Combat()); break;
                case "charge": s_I.StartCoroutine(s_I.Charge()); break;
                case "mort": s_I.StartCoroutine(s_I.Mort()); break;
                case "parade": s_I.StartCoroutine(s_I.Parade()); break;
                case "golem": s_I.StartCoroutine(s_I.GolemTest()); break;
                case "mouvement": s_I.StartCoroutine(s_I.Mouvement()); break;
            }
        }

        static void Log(string t) { Dernier = t; Debug.Log("[Test] " + t + " | " + DevPartie.Etat()); }
        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;

        static void Viser(Transform cible)
        {
            var h = H;
            if (h == null || cible == null) return;
            Vector3 d = cible.position - h.transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            h.transform.rotation = Quaternion.LookRotation(d);
            Partie.Instance.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        IEnumerator Combat()
        {
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            var s = DevPartie.PoserDevant(TypeEnnemi.Sbire, 3.6f, 0.8f);
            Log("sbire posé");
            yield return new WaitForSeconds(0.9f);
            DevPartie.Capturer("v01_sortie_de_terre");
            yield return new WaitForSeconds(1.2f);
            Partie.Instance.cameraJeu.lacet += 25f;
            for (int i = 0; i < 6 && s != null && s.Vivant; i++)
            {
                Viser(s.transform);
                EntreesSimulees.Appui("rightTrigger", 0.1f);
                yield return new WaitForSeconds(0.42f);
                if (i == 1) DevPartie.Capturer("v01_combat_epee");
                yield return new WaitForSeconds(0.4f);
            }
            Log(s == null || !s.Vivant ? "sbire tué à l'épée" : "sbire encore vivant");
            yield return new WaitForSeconds(0.9f);
            DevPartie.Capturer("v01_desintegration");
            // Garde levée puis soin.
            var g = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 3f);
            yield return new WaitForSeconds(2.2f);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.8f);
            DevPartie.Capturer("v01_garde");
            yield return new WaitForSeconds(2.5f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("garde tenue face au guerrier");
            DevPartie.Blesser(60f);
            EntreesSimulees.Appui("rightShoulder", 0.1f);   // RB : soin sur soi
            yield return new WaitForSeconds(0.85f);
            DevPartie.Capturer("v01_soin");
            yield return new WaitForSeconds(0.6f);
            Log("soin sur soi");
        }

        IEnumerator Parade()
        {
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            var g = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 2.2f);
            yield return new WaitForSeconds(2.2f);
            // Attendre la préparation du coup, puis lever la garde juste avant l'impact.
            float t = 0f;
            while (g != null && g.EtatCourant != Squelette.Etat.Preparation && t < 6f) { t += Time.deltaTime; Viser(g.transform); yield return null; }
            yield return new WaitForSeconds(GameBalance.Courant.guerrier.preparation - 0.15f);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.2f);
            DevPartie.Capturer("v01_parade");
            yield return new WaitForSeconds(0.6f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("parade : guerrier " + (g != null ? g.EtatCourant.ToString() : "?"));
        }

        IEnumerator Charge()
        {
            DevPartie.PlacerHeros(new Vector3(-9f, 0f, -9f), new Vector3(-30f, 0f, -30f));
            yield return null;
            var a = DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.6f, 0.2f);
            var b = DevPartie.PoserDevant(TypeEnnemi.Sbire, 4.2f, -0.3f);
            var c = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 7.4f, 0f);
            // Ils sortent de terre puis avancent vers Nyxessa : on les fige en les étourdissant (hors score).
            float t0 = 0f;
            while (t0 < 4f && ((a != null && a.EtatCourant == Squelette.Etat.SortieDeTerre) || (c != null && c.EtatCourant == Squelette.Etat.SortieDeTerre))) { t0 += Time.deltaTime; yield return null; }
            foreach (var s in new[] { a, b, c }) if (s != null) s.Etourdir(4f);
            Viser(c != null ? c.transform : null);
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Appui("leftShoulder", 0.1f);   // LB : charge bélier (après le court délai d'accord)
            yield return new WaitForSeconds(0.5f);
            DevPartie.Capturer("v01_charge_elan");
            yield return new WaitForSeconds(0.45f);
            DevPartie.Capturer("v01_charge_impact");
            yield return new WaitForSeconds(0.5f);
            Log("charge : " + (c != null ? c.EtatCourant.ToString() : "?"));
        }

        IEnumerator Mort()
        {
            DevPartie.Blesser(1000f);
            yield return new WaitForSeconds(1.4f);
            DevPartie.Capturer("v01_mort_dissolution");
            yield return new WaitForSeconds(0.8f);
            DevPartie.Capturer("v01_mort_energie");
            Log("mort");
            var j = Partie.Instance.JoueurLocal;
            while (j.mort) yield return null;
            yield return new WaitForSeconds(0.6f);
            DevPartie.Capturer("v01_reapparition");
            yield return new WaitForSeconds(1.2f);
            Log("réapparu");
        }

        IEnumerator Mouvement()
        {
            var h = H;
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -9.5f), new Vector3(0f, 0f, -40f));
            yield return new WaitForSeconds(0.3f);
            // Saut (A) : hauteur maximale atteinte.
            float y0 = h.transform.position.y, ymax = y0;
            EntreesSimulees.Appui("buttonSouth", 0.12f);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { ymax = Mathf.Max(ymax, h.transform.position.y); if (t > 0.3f && t < 0.35f) DevPartie.Capturer("v01_saut"); yield return null; }
            Log("saut : " + (ymax - y0).ToString("F2") + " m");
            // Sprint (L3 maintenu + stick) contre marche : distance en 1 s.
            Vector3 p0 = h.transform.position;
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1f);
            yield return new WaitForSeconds(1.05f);
            float marche = Vector3.Distance(p0, h.transform.position);
            yield return new WaitForSeconds(0.3f);
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -9.5f), new Vector3(0f, 0f, -40f));
            yield return new WaitForSeconds(0.2f);
            p0 = h.transform.position;
            float e0 = h.Endurance;
            EntreesSimulees.Maintenir("leftStickPress", true);
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1f);
            yield return new WaitForSeconds(1.05f);
            EntreesSimulees.Maintenir("leftStickPress", false);
            Log("marche " + marche.ToString("F1") + " m/s, sprint " + Vector3.Distance(p0, h.transform.position).ToString("F1") + " m/s, endurance " + e0.ToString("F0") + " → " + h.Endurance.ToString("F0"));
            // Esquive (B) : distance.
            yield return new WaitForSeconds(0.5f);
            p0 = h.transform.position;
            EntreesSimulees.Appui("buttonEast", 0.1f);
            yield return new WaitForSeconds(0.2f);
            DevPartie.Capturer("v01_esquive");
            yield return new WaitForSeconds(0.4f);
            Log("esquive : " + Vector3.Distance(p0, h.transform.position).ToString("F1") + " m");
        }

        IEnumerator GolemTest()
        {
            DevPartie.PlacerHeros(new Vector3(-9f, 0f, -9f), new Vector3(-30f, 0f, -30f));
            var g = DevPartie.PoserDevant(TypeEnnemi.Golem, 5f);
            yield return new WaitForSeconds(3f);
            float t = 0f;
            while (g != null && g.EtatCourant != Squelette.Etat.Preparation && t < 10f) { t += Time.deltaTime; Viser(g.transform); yield return null; }
            yield return new WaitForSeconds(0.6f);
            DevPartie.Capturer("v01_golem_preparation");
            yield return new WaitForSeconds(GameBalance.Courant.golemPreparation - 0.6f - 0.15f);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.4f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("golem : coup paré ?");
            t = 0f;
            while (g != null && g.EtatCourant != Squelette.Etat.Preparation && t < 10f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(GameBalance.Courant.golemPreparation - 0.3f);
            EntreesSimulees.Stick(new Vector2(-1f, 0f), Vector2.zero, 0.3f);
            EntreesSimulees.Appui("buttonEast", 0.1f);   // B : esquive
            yield return new WaitForSeconds(0.2f);
            DevPartie.Capturer("v01_golem_esquive");
            yield return new WaitForSeconds(1f);
            Log("golem : coup esquivé ?");
        }
    }
}
