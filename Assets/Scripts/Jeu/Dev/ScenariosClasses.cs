using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Scénarios de vérification des classes en Play (ScenariosClasses.Lancer("viking") depuis execute_code). Gestes par
    /// la manette virtuelle (EntreesSimulees → InputChordResolver), résultats dans la console (préfixe [Test]), captures
    /// dans Assets/Screenshots/classes_*.png.
    public class ScenariosClasses : MonoBehaviour
    {
        static ScenariosClasses s_I;

        public static void Lancer(string nom)
        {
            if (s_I == null) s_I = new GameObject("ScenariosClasses").AddComponent<ScenariosClasses>();
            s_I.StopAllCoroutines();
            EntreesSimulees.ToutRelacher();
            switch (nom)
            {
                case "viking": s_I.StartCoroutine(s_I.Viking()); break;
                case "mage": s_I.StartCoroutine(s_I.Mage()); break;
                case "rodeur": s_I.StartCoroutine(s_I.Rodeur()); break;
                case "assassin": s_I.StartCoroutine(s_I.Assassin()); break;
                case "premierA": s_I.StartCoroutine(s_I.PremierA()); break;
                case "balistique": s_I.StartCoroutine(s_I.Balistique()); break;
            }
        }

        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;
        static void Log(string t) { Debug.Log("[Test] " + t + " | " + DevPartie.Etat()); }

        // Position de départ commune : le couloir sud-ouest, face à la forêt (dégagé).
        static readonly Vector3 Depart = new Vector3(-9f, 0f, -9f), Loin = new Vector3(-30f, 0f, -30f);

        /// Oriente le héros et la caméra pour que le réticule passe par `p`.
        static void ViserPoint(Vector3 p)
        {
            var h = H; var cam = Partie.Instance.cameraJeu; var b = GameBalance.Courant;
            Vector3 d = p - h.transform.position; d.y = 0f;
            h.transform.rotation = Quaternion.LookRotation(d);
            cam.lacet = h.transform.eulerAngles.y;
            // Même placement que CameraEpaule (pivot, épaule, recul) : on itère pour que le centre de l'écran passe par p.
            for (int i = 0; i < 6; i++)
            {
                Quaternion rot = Quaternion.Euler(cam.tangage, cam.lacet, 0f);
                Vector3 pivot = h.transform.position + Vector3.up * b.cameraHauteur;
                Vector3 pos = pivot + rot * Vector3.right * b.cameraEpaule + rot * Vector3.back * b.cameraDistance;
                Vector3 v = p - pos;
                cam.lacet = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                cam.tangage = -Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg;
            }
        }

        static IEnumerator Sortir(IEnumerable<Squelette> liste)
        {
            float t = 0f;
            bool enCours = true;
            while (enCours && t < 4f)
            {
                enCours = false;
                foreach (var s in liste) if (s != null && s.EtatCourant == Squelette.Etat.SortieDeTerre) enCours = true;
                t += Time.deltaTime;
                yield return null;
            }
        }

        static void Figer(IEnumerable<Squelette> liste, float duree) { foreach (var s in liste) if (s != null && s.Vivant) s.Etourdir(duree); }

        // ================================================================= Premier appui A après la souris (menu)

        IEnumerator PremierA()
        {
            var submit = UnityEngine.InputSystem.InputSystem.actions.FindAction("UI/Submit");
            int vus = 0;
            System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> compter = c => vus++;
            submit.performed += compter;
            EntreesSimulees.Appui("buttonSouth", 0.01f);   // crée la manette (premier état)
            yield return new WaitForSeconds(0.5f);
            // Mouvement de souris, puis A une seconde plus tard.
            var m = UnityEngine.InputSystem.Mouse.current;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(m, new UnityEngine.InputSystem.LowLevel.MouseState { position = new Vector2(420, 300), delta = new Vector2(30, 5) });
            yield return new WaitForSeconds(1f);
            string avant = Deathless.UI.InputDeviceWatcher.Current.ToString();
            EntreesSimulees.Appui("buttonSouth", 0.2f);
            for (int i = 0; i < 20; i++) yield return null;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            Log("premier A après la souris : Submit vu " + vus + " fois, appareil " + avant + " → " + Deathless.UI.InputDeviceWatcher.Current + ", sélection " + (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name : "aucune"));
            submit.performed -= compter;
        }

        // ================================================================= Viking

        IEnumerator Viking()
        {
            DevPartie.PlacerHeros(Depart, Loin);
            var groupe = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.4f, -1f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.6f, 0f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.4f, 1f) };
            yield return Sortir(groupe);
            Figer(groupe, 20f);
            for (int i = 0; i < 2; i++)
            {
                EntreesSimulees.Appui("rightTrigger", 0.1f);
                yield return new WaitForSeconds(0.55f);
                if (i == 0) DevPartie.Capturer("classes_viking_hache");
                yield return new WaitForSeconds(0.7f);
            }
            Log("viking : hache, rage " + H.Classe.ValeurJauge.ToString("F0"));
            H.Classe.RemplirJauge();
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(1.1f);
            DevPartie.Capturer("classes_viking_tournante");
            yield return new WaitForSeconds(1.0f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.6f);
            Log("viking : tournante, rage " + H.Classe.ValeurJauge.ToString("F0"));
            // Rugissement : deux guerriers à 8 m, provoqués.
            H.Classe.RemplirJauge();
            var loin = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Guerrier, 8f, -2f), DevPartie.PoserDevant(TypeEnnemi.Guerrier, 8f, 2f) };
            yield return Sortir(loin);
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(0.95f);
            DevPartie.Capturer("classes_viking_rugissement");
            yield return new WaitForSeconds(0.8f);
            Log("viking : rugissement");
            // Saut percutant vers le groupe.
            yield return new WaitForSeconds(0.6f);
            DevPartie.PlacerHeros(Depart + new Vector3(-3f, 0f, 3f), Loin + new Vector3(-3f, 0f, 3f));
            var cibleSaut = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 5.5f, -0.8f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 5.8f, 0.8f) };
            yield return Sortir(cibleSaut);
            Figer(cibleSaut, 10f);
            H.Classe.RemplirJauge();
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.1f + 0.79f / 1.2f + 0.25f);
            DevPartie.Capturer("classes_viking_saut");
            yield return new WaitForSeconds(0.8f);
            Log("viking : saut percutant");
        }

        // ================================================================= Mage

        IEnumerator Mage()
        {
            DevPartie.PlacerHeros(Depart, Loin);
            var groupe = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 9f, -0.8f), DevPartie.PoserDevant(TypeEnnemi.Guerrier, 9.5f, 0.6f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 10f, 0f) };
            yield return Sortir(groupe);
            Figer(groupe, 20f);
            ViserPoint(groupe[1].transform.position + Vector3.up * 1f);
            yield return null;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.5f);
            DevPartie.Capturer("classes_mage_boule");
            yield return new WaitForSeconds(0.35f);
            DevPartie.Capturer("classes_mage_explosion");
            yield return new WaitForSeconds(1.2f);
            DevPartie.Capturer("classes_mage_brulure");
            Log("mage : boule, mana " + H.Classe.ValeurJauge.ToString("F0"));
            // Cône maintenu, de près.
            var proche = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Guerrier, 3.5f, 0f) };
            yield return Sortir(proche);
            Figer(proche, 20f);
            ViserPoint(proche[0].transform.position + Vector3.up);
            H.Classe.RemplirJauge();
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(1.4f);
            DevPartie.Capturer("classes_mage_cone");
            yield return new WaitForSeconds(1.2f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.3f);
            Log("mage : cône, mana " + H.Classe.ValeurJauge.ToString("F0"));
        }

        // ================================================================= Rôdeur

        IEnumerator Rodeur()
        {
            DevPartie.PlacerHeros(Depart, Loin);
            var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 12f, 0f);
            yield return Sortir(new[] { cible });
            Figer(new[] { cible }, 25f);
            yield return new WaitForSeconds(0.2f);
            ViserPoint(cible.CentreTete);
            // Viser (LT maintenu), puis bander pendant la visée (RT maintenu), relâcher RT pour tirer.
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.2f);
            EntreesSimulees.Maintenir("rightTrigger", true);
            yield return new WaitForSeconds(0.6f);
            DevPartie.Capturer("classes_rodeur_bander");
            yield return new WaitForSeconds(0.75f);
            DevPartie.Capturer("classes_rodeur_pret");
            EntreesSimulees.Maintenir("rightTrigger", false);
            yield return new WaitForSeconds(0.15f);
            DevPartie.Capturer("classes_rodeur_tir");
            yield return new WaitForSeconds(0.6f);
            Log("rôdeur : tir chargé à la tête");
            // Tir au corps, charge faible.
            ViserPoint(cible.transform.position + Vector3.up * 0.9f);
            EntreesSimulees.Maintenir("rightTrigger", true);
            yield return new WaitForSeconds(0.4f);
            EntreesSimulees.Maintenir("rightTrigger", false);
            yield return new WaitForSeconds(0.8f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("rôdeur : tir faible au corps");
            // Nuée sur un groupe.
            var groupe = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 14f, -1f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 14f, 1f) };
            yield return Sortir(groupe);
            Figer(groupe, 20f);
            ViserPoint(groupe[0].transform.position);
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(1.1f);
            DevPartie.Capturer("classes_rodeur_nuee");
            yield return new WaitForSeconds(1.2f);
            Log("rôdeur : nuée");
            // Roulade arrière et salve.
            ViserPoint(H.transform.position + H.transform.forward * 10f + Vector3.up);
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.35f);
            DevPartie.Capturer("classes_rodeur_roulade");
            yield return new WaitForSeconds(0.8f);
            Log("rôdeur : roulade arrière");
        }

        // ================================================================= Assassin

        // ================================================================= Balistique (flèches et carreaux)

        /// Rôdeur : tirs à la tête d'un guerrier figé à 15, 30 et 45 m, tir rapide (0,25 s) puis chargé (1,3 s) ; assassin :
        /// carreau à 30 et 45 m. Journal : touché ou non, écart vertical de l'impact au centre de la tête, flèche la plus haute.
        IEnumerator Balistique()
        {
            string classe = Partie.ClasseChoisie;
            ProjectileJeu.Genre dernierGenre = ProjectileJeu.Genre.Fleche;
            Vector3 dernierPoint = Vector3.zero; Sante derniereCible = null; float dernierSommet = 0f; bool arrive = false;
            System.Action<ProjectileJeu.Genre, Vector3, Sante, float> suivi = (g, p, s, h) => { if (arrive) return; arrive = true; dernierGenre = g; dernierPoint = p; derniereCible = s; dernierSommet = h; };
            ProjectileJeu.Arrivee += suivi;
            var distances = new[] { 15f, 30f, 45f };
            var tenues = classe == "rodeur" ? new[] { 0.25f, 1.3f } : new[] { 0.05f };
            int n = 0;
            foreach (float d in distances)
            {
                foreach (float tenue in tenues)
                {
                    DevPartie.PlacerHeros(Depart, Loin);
                    var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, d, (n % 3 - 1) * 3f);
                    n++;
                    if (cible == null) continue;
                    yield return Sortir(new[] { cible });
                    Figer(new[] { cible }, 30f);
                    yield return new WaitForSeconds(0.2f);
                    Vector3 tete = cible.CentreTete;
                    float pv = cible.Sante.Pv;
                    ViserPoint(tete);
                    arrive = false;
                    if (classe == "rodeur")
                    {
                        EntreesSimulees.Maintenir("leftTrigger", true);
                        yield return new WaitForSeconds(0.1f);
                        EntreesSimulees.Maintenir("rightTrigger", true);
                        yield return new WaitForSeconds(tenue);
                        ViserPoint(tete);
                        EntreesSimulees.Maintenir("rightTrigger", false);
                        yield return new WaitForSeconds(0.05f);
                        EntreesSimulees.Maintenir("leftTrigger", false);
                    }
                    else
                    {
                        EntreesSimulees.Maintenir("leftTrigger", true);
                        yield return new WaitForSeconds(0.6f);
                        ViserPoint(tete);
                        yield return new WaitForSeconds(0.2f);
                        EntreesSimulees.Appui("rightTrigger", 0.1f);
                    }
                    float t = 0f;
                    while (!arrive && t < 4f) { t += Time.deltaTime; yield return null; }
                    EntreesSimulees.ToutRelacher();
                    bool touche = derniereCible == cible.Sante;
                    Log("balistique " + classe + " " + d + " m, tenue " + tenue + " s : " + (touche ? "touché (" + (pv - cible.Sante.Pv).ToString("F0") + " dégâts)" : "manqué")
                        + ", impact à " + (dernierPoint.y - tete.y).ToString("+0.00;-0.00") + " m de la tête (vertical), à " + Vector3.Distance(Flat(dernierPoint), Flat(H.transform.position)).ToString("F1") + " m, sommet +" + dernierSommet.ToString("F2") + " m");
                    yield return new WaitForSeconds(0.5f);
                    if (cible != null) Destroy(cible.gameObject);
                    yield return new WaitForSeconds(classe == "rodeur" ? 0.3f : 6.2f);
                }
            }
            ProjectileJeu.Arrivee -= suivi;
            Log("balistique terminée");
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        IEnumerator Assassin()
        {
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H;
            // Un guerrier de dos, 7 m devant.
            var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 7f, 0f);
            yield return Sortir(new[] { cible });
            Figer(new[] { cible }, 30f);
            cible.transform.rotation = Quaternion.LookRotation(cible.transform.position - h.transform.position);
            // Approche en marche discrète.
            ViserPoint(cible.transform.position + Vector3.up);
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1.55f);
            yield return new WaitForSeconds(0.9f);
            DevPartie.Capturer("classes_assassin_furtif");
            yield return new WaitForSeconds(0.9f);
            ViserPoint(cible.transform.position + Vector3.up);
            Log("assassin : approche, furtif " + h.Classe.Furtif + ", distance " + Vector3.Distance(h.transform.position, cible.transform.position).ToString("F1"));
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.3f);
            DevPartie.Capturer("classes_assassin_meilleur_critique");
            yield return new WaitForSeconds(0.8f);
            Log("assassin : coup de dague");
            // Arbalète : tir à la tête d'un guerrier à 10 m.
            DevPartie.PlacerHeros(Depart, Loin);
            var tir = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 10f, 2f);
            yield return Sortir(new[] { tir });
            Figer(new[] { tir }, 20f);
            ViserPoint(tir.CentreTete);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.5f);
            ViserPoint(tir.CentreTete);
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.2f);
            DevPartie.Capturer("classes_assassin_arbalete");
            yield return new WaitForSeconds(0.6f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("assassin : carreau");
            // Grenade fumigène, puis furtif dans la fumée malgré le combat.
            yield return new WaitForSeconds(0.4f);
            ViserPoint(h.transform.position + h.transform.forward * 5f);
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(1.9f);
            DevPartie.Capturer("classes_assassin_fumee");
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1.2f);
            yield return new WaitForSeconds(1.3f);
            Log("assassin : dans la fumée, furtif " + h.Classe.Furtif + " (dans la fumée : " + ClasseAssassin.DansLaFumee(h.transform.position) + ")");
            yield return new WaitForSeconds(4f);
            // Repérage : un sbire face à l'assassin furtif, à 5 m.
            DevPartie.PlacerHeros(Depart, Loin);
            var guetteur = DevPartie.PoserDevant(TypeEnnemi.Sbire, 5f, 0f);
            yield return Sortir(new[] { guetteur });
            guetteur.transform.rotation = Quaternion.LookRotation(h.transform.position - guetteur.transform.position);
            EntreesSimulees.Stick(new Vector2(0f, 0.3f), Vector2.zero, 1.5f);
            yield return new WaitForSeconds(1.6f);
            Log("assassin : face à un sbire à 5 m, furtif " + h.Classe.Furtif);
        }
    }
}
