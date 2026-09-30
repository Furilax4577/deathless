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
                case "voleurmage": s_I.StartCoroutine(s_I.VoleurEtMage()); break;
                case "courseesquive": s_I.StartCoroutine(s_I.CourseEsquive()); break;
                case "gardiens": s_I.StartCoroutine(s_I.GardiensAgressifs()); break;
            }
        }

        /// Course et esquive (30/09/2026) : un sbire posé à 14 m doit courir (Court, vitesse > pas de base, Speed > 0,5)
        /// puis repasser au pas près du héros ; ensuite, chance d'esquive forcée à 1 le temps du test, le héros frappe (RT)
        /// face au sbire : on compte les esquives et les états vus.
        IEnumerator CourseEsquive()
        {
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var s = DevPartie.PoserDevant(TypeEnnemi.Sbire, 14f);
            yield return new WaitForSeconds(1.8f);
            float t = 0f, vMax = 0f, speedMax = 0f, vPres = -1f; bool court = false;
            while (t < 6f && s != null && s.Vivant)
            {
                t += Time.deltaTime;
                if (s.Court) court = true;
                vMax = Mathf.Max(vMax, s.Agent.velocity.magnitude);
                if (s.animator != null) speedMax = Mathf.Max(speedMax, s.animator.GetFloat("Speed"));
                float d = Vector3.Distance(s.transform.position, H.transform.position);
                if (d < 3.5f && vPres < 0f) vPres = s.Agent.speed;
                if (t > 0.6f && t < 0.7f) DevPartie.Capturer("v06_squelette_course");
                if (s.EtatCourant == Squelette.Etat.Preparation) break;
                yield return null;
            }
            Log("course : court " + court + ", vitesse max " + vMax.ToString("F2") + " (pas " + GameBalance.Courant.sbire.vitesse + "), Speed max " + speedMax.ToString("F2")
                + ", vitesse demandée près du héros " + vPres.ToString("F2"));
            var b = GameBalance.Courant;
            float chance = b.esquiveChanceSbire;
            b.esquiveChanceSbire = 1f;
            int esquivesAvant = s != null ? s.Esquives : 0; bool vuEsquive = false;
            float pvAvant = s != null ? s.Sante.Pv : 0f;
            for (int i = 0; i < 8 && s != null && s.Vivant; i++)
            {
                Viser(s.transform);
                EntreesSimulees.Appui("rightTrigger", 0.1f);
                float u = 0f;
                while (u < 0.9f) { u += Time.deltaTime; if (s != null && s.EtatCourant == Squelette.Etat.Esquive) { vuEsquive = true; if (u > 0.15f && u < 0.2f) DevPartie.Capturer("v06_squelette_esquive"); } yield return null; }
            }
            b.esquiveChanceSbire = chance;
            Log("esquive : " + (s != null ? s.Esquives - esquivesAvant : -1) + " esquive(s) sur 8 coups (recharge " + b.esquiveEnnemiRecharge + " s), état Esquive vu " + vuEsquive
                + ", PV " + pvAvant.ToString("F0") + " -> " + (s != null ? s.Sante.Pv.ToString("F0") : "mort"));
        }

        /// Gardiens (30/09/2026) : A repère le héros, B (hors détection) est alerté par A. Le héros saute d'un point à
        /// l'autre : on relève le plus long temps sans frapper en poursuite (au-delà de abandonApres, ils ne lâchent pas) ;
        /// puis il part à plus de gardienLaisse de leur poste : ils lâchent et rentrent.
        IEnumerator GardiensAgressifs()
        {
            // Côté dégagé du village (vers +x depuis (0 ; -11)) : A à 8 m (repère le héros), B à 14 m (hors détection,
            // à 6 m de A : alerté).
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(20f, 0f, -11f));
            yield return null;
            var a = DevPartie.PoserDevant(TypeEnnemi.Sbire, 8f);
            var g = DevPartie.PoserDevant(TypeEnnemi.Sbire, 14f);
            if (a != null) a.Garder(a.transform.position);
            if (g != null) g.Garder(g.transform.position);
            var champ = typeof(Squelette).GetField("m_SansFrapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float t = 0f, sansMax = 0f; bool aVu = false, bVu = false; int saut = 0;
            while (t < 10f)
            {
                t += Time.deltaTime;
                if (a != null && a.EtatCourant == Squelette.Etat.Poursuite) { aVu = true; sansMax = Mathf.Max(sansMax, (float)champ.GetValue(a)); }
                if (g != null && g.EtatCourant == Squelette.Etat.Poursuite) { bVu = true; sansMax = Mathf.Max(sansMax, (float)champ.GetValue(g)); }
                if (aVu && t > 3f + saut * 0.9f) { saut++; DevPartie.PlacerHeros(new Vector3(saut % 2 == 0 ? 0f : -5f, 0f, saut % 4 < 2 ? -11f : -8f), new Vector3(20f, 0f, -11f)); }
                yield return null;
            }
            Log("gardiens : A poursuit " + aVu + ", B (alerté) poursuit " + bVu + ", au bout de 10 s A " + (a != null ? a.EtatCourant.ToString() : "?") + ", B " + (g != null ? g.EtatCourant.ToString() : "?")
                + ", plus long temps sans frapper en poursuite " + sansMax.ToString("F1") + " s (abandonApres " + GameBalance.Courant.abandonApres + " s)");
            DevPartie.PlacerHeros(new Vector3(-15f, 0f, -11f), new Vector3(-30f, 0f, -11f));
            yield return new WaitForSeconds(3f);
            Log("héros hors laisse : A " + (a != null ? a.EtatCourant.ToString() : "?") + ", B " + (g != null ? g.EtatCourant.ToString() : "?")
                + ", A à " + (a != null ? Vector3.Distance(a.transform.position, a.Poste).ToString("F1") : "?") + " m de son poste");
        }

        /// Voleur et mage (28/09/2026) : un voleur posé à 12 m doit se ruer sur le joueur (isolé en solo) ; un mage posé à
        /// 6 m doit s'arrêter à distance et tirer un crâne ; on relève leurs états et le nombre de missiles créés.
        IEnumerator VoleurEtMage()
        {
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var v = DevPartie.PoserDevant(TypeEnnemi.Voleur, 12f, -2f);
            var m = DevPartie.PoserDevant(TypeEnnemi.Mage, 6f, 2f);
            Log("posés : voleur " + (v != null ? v.GetType().Name : "null") + ", mage " + (m != null ? m.GetType().Name : "null"));
            yield return new WaitForSeconds(2.5f);
            DevPartie.Capturer("v01_voleur_mage_sortie");
            float t = 0f; bool voleurPoursuit = false; int missiles = 0; float dMin = 999f;
            while (t < 8f)
            {
                t += Time.deltaTime;
                if (v != null && v.Vivant && v.EtatCourant == Squelette.Etat.Poursuite) voleurPoursuit = true;
                if (m != null && m.Vivant) dMin = Mathf.Min(dMin, Vector3.Distance(m.transform.position, H.transform.position));
                missiles = Mathf.Max(missiles, FindObjectsByType<MissileCrane>(FindObjectsSortMode.None).Length);
                if (t > 4f && t < 4.1f) DevPartie.Capturer("v01_voleur_mage_combat");
                yield return null;
            }
            Log("voleur : poursuite " + voleurPoursuit + " (état " + (v != null ? v.EtatCourant.ToString() : "?") + ", vitesse " + (v != null ? v.Agent.speed.ToString("F1") : "?") + ")"
                + " ; mage : distance min " + dMin.ToString("F1") + " m, état " + (m != null ? m.EtatCourant.ToString() : "?") + ", missiles vus " + missiles);
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
