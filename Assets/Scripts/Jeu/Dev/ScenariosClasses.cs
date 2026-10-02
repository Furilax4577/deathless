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
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosClasses() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosClasses"); }

        static ScenariosClasses s_I;

        public static void Lancer(string nom)
        {
            if (s_I == null) s_I = new GameObject("ScenariosClasses").AddComponent<ScenariosClasses>();
            s_I.StopAllCoroutines();
            EntreesSimulees.ToutRelacher();
            switch (nom)
            {
                case "viking": s_I.StartCoroutine(s_I.Viking()); break;
                case "viking_furie": s_I.StartCoroutine(s_I.VikingFurie()); break;
                case "viking_furie_distant": s_I.StartCoroutine(s_I.VikingFurieDistant()); break;   // marionnette en Furie à côté du viking local   // 03/10/2026 : rage, recharges, Furie (ultime)
                case "mage": s_I.StartCoroutine(s_I.Mage()); break;
                case "mage_kit": s_I.StartCoroutine(s_I.MageKit()); break;   // 01/10/2026 : LB, RB, cône qui ralentit, mana 3/s
                case "mage_visee": s_I.StartCoroutine(s_I.MageVisee()); break;     // 02/10/2026 : visée au sol (grande boule, mur)
                case "rodeur_visee": s_I.StartCoroutine(s_I.RodeurVisee()); break; // 02/10/2026 : visée au sol de la nuée
                case "rodeur": s_I.StartCoroutine(s_I.Rodeur()); break;
                case "assassin": s_I.StartCoroutine(s_I.Assassin()); break;
                case "premierA": s_I.StartCoroutine(s_I.PremierA()); break;
                case "balistique": s_I.StartCoroutine(s_I.Balistique()); break;
                // Lissage du 27/09/2026 (Docs/equilibrage-classes.md) : une capture lissage_*.png par point.
                case "lissage_assassin": s_I.StartCoroutine(s_I.LissageAssassin()); break;
                case "lissage_rodeur": s_I.StartCoroutine(s_I.LissageRodeur()); break;
                case "lissage_viking": s_I.StartCoroutine(s_I.LissageViking()); break;
                case "lissage_paladin": s_I.StartCoroutine(s_I.LissagePaladin()); break;
                case "lissage_squelette": s_I.StartCoroutine(s_I.LissageSquelette()); break;
            }
        }

        /// Coup « du héros local » porté sans passer par sa classe (même chemin que ScenariosVie.Frapper) : la riposte
        /// des squelettes lit InfoDegats.sourceId.
        static void Frapper(Squelette cible, float montant)
        {
            var h = H;
            if (h == null || cible == null || cible.Sante == null) return;
            cible.Sante.Encaisser(new InfoDegats { montant = montant, sourceId = h.Id, equipeSource = Equipe.Heros, point = cible.CentreTete, source = h.gameObject });
        }

        // ================================================================= Lissage du 27/09/2026

        /// Les missiles de Nyxessa (stock de jour) achèveraient les squelettes posés pour ces essais : coupés le temps du
        /// scénario.
        static void Missiles(bool actifs) { var d = FindAnyObjectByType<DefenseNyxessa>(); if (d != null) d.enabled = actifs; }

        /// Assassin : Pas de l'ombre vers un guerrier qui lui fait face (arrivée dans son dos), puis Exécution d'une
        /// cible sous 30 % (achevée net, recharge du bond remise à zéro).
        IEnumerator LissageAssassin()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H; var b = GameBalance.Courant;
            var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 6f, 0f);
            yield return Sortir(new[] { cible });
            Figer(new[] { cible }, 30f);
            cible.transform.rotation = Quaternion.LookRotation(h.transform.position - cible.transform.position);   // il regarde l'assassin
            ViserPoint(cible.transform.position + Vector3.up);
            yield return null;
            Vector3 depart = h.transform.position;
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.1f);
            DevPartie.Capturer("lissage_assassin_bond");
            yield return new WaitForSeconds(0.4f);
            h.Classe.Emplacement(3, out float restant, out float total);
            Vector3 vers = cible.transform.position - h.transform.position; vers.y = 0f;
            Log("assassin : bond de " + Vector3.Distance(depart, h.transform.position).ToString("F1") + " m, à " + vers.magnitude.ToString("F1") + " m de la cible, dans son dos " + Combat.DansLeDos(cible.transform, h.transform.position, b.angleDos)
                + ", face à elle " + (Vector3.Angle(h.transform.forward, vers) < 25f) + ", furtif " + h.Classe.Furtif + ", recharge " + restant.ToString("F1") + "/" + total.ToString("F1") + " s");
            // Exécution : guerrier ramené à 25 % de vie (coup anonyme), coup de dague.
            cible.Sante.Encaisser(new InfoDegats { montant = cible.Sante.pvMax * 0.75f + 1f, equipeSource = Equipe.Ennemis, point = cible.CentreTete });
            float pvAvant = cible.Sante.Pv;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.32f);
            DevPartie.Capturer("lissage_assassin_execution");
            yield return new WaitForSeconds(0.3f);
            h.Classe.Emplacement(3, out restant, out total);
            Log("assassin : exécution sur " + pvAvant.ToString("F0") + " PV (" + Mathf.RoundToInt(pvAvant / 160f * 100f) + " %), cible morte " + (cible == null || cible.Sante.Mort) + ", recharge du bond " + restant.ToString("F1") + " s");
            Missiles(true);
        }

        /// Rôdeur : flèche à pleine charge au corps d'un guerrier libre (Étourdi 1 s), puis nuée sur deux sbires (Ralenti).
        IEnumerator LissageRodeur()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H;
            var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 9f, 0f);
            yield return Sortir(new[] { cible });
            yield return new WaitForSeconds(0.2f);
            ViserPoint(cible.transform.position + Vector3.up);
            EntreesSimulees.Maintenir("rightTrigger", true);
            yield return new WaitForSeconds(1.35f);   // pleine charge (1,2 s)
            ViserPoint(cible.transform.position + cible.Agent.velocity * 0.2f + Vector3.up);
            EntreesSimulees.Maintenir("rightTrigger", false);
            yield return new WaitForSeconds(0.45f);
            bool etourdi = cible != null && cible.Statuts != null && cible.Statuts.A(TypeStatut.Etourdi);
            DevPartie.Capturer("lissage_rodeur_etourdi");
            Log("rôdeur : flèche à pleine charge, étourdi " + etourdi + ", état " + (cible != null ? cible.EtatCourant.ToString() : "?") + ", PV " + (cible != null ? cible.Sante.Pv.ToString("F0") + "/" + cible.Sante.pvMax.ToString("F0") : "?"));
            yield return new WaitForSeconds(1f);
            // Nuée : deux sbires figés à 12 m, Ralenti attendu.
            DevPartie.PlacerHeros(Depart + new Vector3(-3f, 0f, 3f), Loin + new Vector3(-3f, 0f, 3f));
            var groupe = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 12f, -1f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 12f, 1f) };
            yield return Sortir(groupe);
            Figer(groupe, 20f);
            ViserPoint(groupe[0].transform.position);
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Appui("rightTrigger", 0.1f);   // visée au sol (02/10/2026) : RT confirme la nuée
            yield return new WaitForSeconds(1.35f);
            DevPartie.Capturer("lissage_rodeur_nuee_ralenti");
            var s0 = groupe[0];
            Log("rôdeur : nuée, ralenti " + (s0 != null && s0.Statuts != null && s0.Statuts.A(TypeStatut.Ralenti)) + " (" + (s0 != null && s0.Statuts != null ? Mathf.RoundToInt(s0.Statuts.Intensite(TypeStatut.Ralenti) * 100f) : 0) + " %), PV " + (s0 != null ? s0.Sante.Pv.ToString("F0") : "?"));
            Missiles(true);
        }

        /// Viking : rage de départ (plancher 30), rugissement en marchant (couche haute) qui pose Peau de fer, coup reçu
        /// réduit de 35 %, puis remontée de la rage vers le plancher.
        IEnumerator LissageViking()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H;
            var loin = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Guerrier, 8f, -2f), DevPartie.PoserDevant(TypeEnnemi.Guerrier, 8f, 2f) };
            yield return Sortir(loin);
            Figer(loin, 20f);
            Log("viking : rage avant le rugissement " + h.Classe.ValeurJauge.ToString("F0"));
            Vector3 p0 = h.transform.position;
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1.6f);
            yield return new WaitForSeconds(0.1f);
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(1.15f);   // le cri (1,02 s) est passé : statut posé
            DevPartie.Capturer("lissage_viking_peau_de_fer");
            float pv = h.Sante.Pv;
            DevPartie.Blesser(20f);
            yield return new WaitForSeconds(0.2f);
            Log("viking : rugissement en marchant (déplacé de " + Vector3.Distance(p0, h.transform.position).ToString("F1") + " m), Peau de fer " + (h.Statuts != null && h.Statuts.A(TypeStatut.PeauDeFer)) + " (−" + Mathf.RoundToInt((h.Statuts != null ? h.Statuts.Intensite(TypeStatut.PeauDeFer) : 0f) * 100f) + " %), 20 dégâts → " + (pv - h.Sante.Pv).ToString("F1") + " subis, rage " + h.Classe.ValeurJauge.ToString("F0"));
            yield return new WaitForSeconds(7f);
            Log("viking : 7 s plus tard, rage " + h.Classe.ValeurJauge.ToString("F0") + " (plancher " + GameBalance.Courant.rageMin + "), Peau de fer " + (h.Statuts != null && h.Statuts.A(TypeStatut.PeauDeFer)));
            Missiles(true);
        }

        /// Paladin : soin d'aura sur lui et sur un allié factice (héros mage attaché comme marionnette) à 2 m, blessés.
        IEnumerator LissagePaladin()
        {
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H; var p = Partie.Instance;
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver("mage") : null;
            Heros allie = null;
            if (def != null && def.prefab != null)
            {
                var go = Instantiate(def.prefab, h.transform.position + h.transform.right * 2f + h.transform.forward * 1f, Quaternion.LookRotation(-h.transform.right));
                go.name = "Heros_allie_test";
                allie = go.GetComponent<Heros>();
                p.AttacherHerosDistant(allie, "mage", "Allié", 7);
                allie.Sante.Encaisser(new InfoDegats { montant = 50f, equipeSource = Equipe.Ennemis, point = allie.transform.position + Vector3.up });
            }
            DevPartie.Blesser(60f);
            yield return new WaitForSeconds(0.3f);
            float pvP = h.Sante.Pv, pvA = allie != null ? allie.Sante.Pv : 0f;
            float soins = p.JoueurLocal != null ? p.JoueurLocal.score.soinsProdigues : 0f;
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.95f);
            DevPartie.Capturer("lissage_paladin_aura");
            yield return new WaitForSeconds(0.3f);
            Log("paladin : soin d'aura, lui " + pvP.ToString("F0") + " → " + h.Sante.Pv.ToString("F0") + ", allié à 2 m " + pvA.ToString("F0") + " → " + (allie != null ? allie.Sante.Pv.ToString("F0") : "?") + ", soins prodigués +" + ((p.JoueurLocal != null ? p.JoueurLocal.score.soinsProdigues : 0f) - soins).ToString("F0"));
            yield return new WaitForSeconds(1.5f);
            if (allie != null) { p.DetacherHeros(7); Destroy(allie.gameObject); }
        }

        /// Squelette au contact de Nyxessa, frappé deux fois de suite par le héros à moins de 3 m : il se retourne vers lui
        /// (poursuite bornée), puis revient à Nyxessa.
        IEnumerator LissageSquelette()
        {
            Missiles(false);
            var p = Partie.Instance; var h = H; var b = GameBalance.Courant;
            Vector3 nyx = p.nyxessa.transform.position;
            Vector3 dir = Depart - nyx; dir.y = 0f; dir.Normalize();
            DevPartie.PlacerHeros(nyx + dir * 5.4f, nyx);
            var sq = DevPartie.PoserDevant(TypeEnnemi.Sbire, 3f, 0f);
            yield return Sortir(new[] { sq });
            // Il marche vers sa place et frappe Nyxessa (préparation lisible).
            float t = 0f;
            while (t < 4f && sq != null && !sq.SurNyxessa) { t += Time.deltaTime; yield return null; }
            // Le héros se rapproche à moins de 3 m et frappe deux fois (coups anonymes de classe : même chemin que la dague).
            // De côté (pas dans l'axe du pilier de Nyxessa) pour que la caméra voie le squelette se retourner.
            Vector3 derriere = sq.transform.position + dir * 1.3f + Vector3.Cross(Vector3.up, dir) * 1.5f;
            DevPartie.PlacerHeros(derriere, sq.transform.position);
            if (p.cameraJeu != null) p.cameraJeu.lacet = h.transform.eulerAngles.y + 75f;   // vue de trois quarts : le squelette n'est pas caché par le héros
            Log("squelette : au contact de Nyxessa " + sq.SurNyxessa + " (à " + Vector3.Distance(sq.transform.position, nyx).ToString("F1") + " m d'elle), héros à " + Vector3.Distance(h.transform.position, sq.transform.position).ToString("F1") + " m");
            Frapper(sq, 5f);
            yield return new WaitForSeconds(0.4f);
            Frapper(sq, 5f);
            yield return new WaitForSeconds(1.0f);
            Vector3 vers = h.transform.position - sq.transform.position; vers.y = 0f;
            DevPartie.Capturer("lissage_squelette_riposte");
            Log("squelette : après deux coups, riposte " + sq.EnRiposte + ", état " + sq.EtatCourant + ", tourné vers le héros " + (Vector3.Angle(sq.transform.forward, vers) < 45f) + " (" + Vector3.Angle(sq.transform.forward, vers).ToString("F0") + "°)");
            yield return new WaitForSeconds(4f);
            Log("squelette : 4 s plus tard, riposte " + sq.EnRiposte + ", état " + sq.EtatCourant + ", sur Nyxessa " + sq.SurNyxessa + ", à " + Vector3.Distance(sq.transform.position, nyx).ToString("F1") + " m d'elle");
            yield return new WaitForSeconds(0.5f);
            if (sq != null) sq.Desintegrer(true);
            Missiles(true);
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
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(1.1f);
            DevPartie.Capturer("classes_viking_tournante");
            yield return new WaitForSeconds(1.0f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.6f);
            Log("viking : tournante, rage " + H.Classe.ValeurJauge.ToString("F0"));
            // Rugissement : deux guerriers à 8 m, provoqués (gratuit, 03/10/2026).
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
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.1f + 0.79f / 1.2f + 0.25f);
            DevPartie.Capturer("classes_viking_saut");
            yield return new WaitForSeconds(0.8f);
            Log("viking : saut percutant");
        }


        // ================================================================= Viking : rage, recharges, Furie (03/10/2026)

        /// Hits portés par le héros local (hors tics continus) : instants, pour mesurer la cadence.
        static readonly List<float> s_InstantsCoups = new List<float>();
        static Squelette s_CibleMesure;
        static void CompterCoups(Sante cible, InfoDegats info, float reel) { if (H != null && s_CibleMesure != null && cible == s_CibleMesure.Sante && info.sourceId == H.Id && !info.continu && reel > 0f) s_InstantsCoups.Add(Time.time); }

        /// Furie : déclenchée si elle n'est pas en cours (rage remplie puis R3).
        static IEnumerator AssurerFurie(ClasseViking v)
        {
            if (v.EnFurie) yield break;
            v.RemplirJauge();
            yield return new WaitForSeconds(0.25f);
            EntreesSimulees.Appui("rightStickPress", 0.1f);
            yield return new WaitForSeconds(0.4f);
        }

        static string Emp(Heros h, int i) { var e = h.Classe.Emplacement(i, out float r, out float t); return e + (e == EtatEmplacement.Recharge ? " " + r.ToString("F1") + "/" + t.ToString("F0") : ""); }

        /// Cadence : coups portés pendant `duree` s en martelant RT sur une cible figée à PV énormes.
        static IEnumerator Marteler(Squelette cible, float duree, System.Action<float> fin)
        {
            s_InstantsCoups.Clear();
            s_CibleMesure = cible;
            Sante.AnyTouche += CompterCoups;
            float t0 = Time.time;
            while (Time.time - t0 < duree)
            {
                cible.Etourdir(5f);
                EntreesSimulees.Appui("rightTrigger", 0.03f);
                yield return new WaitForSeconds(0.07f);
            }
            Sante.AnyTouche -= CompterCoups;
            float intervalle = s_InstantsCoups.Count > 1 ? (s_InstantsCoups[s_InstantsCoups.Count - 1] - s_InstantsCoups[0]) / (s_InstantsCoups.Count - 1) : 0f;
            fin(intervalle);
        }

        /// Viking : la rage monte en frappant et en encaissant, les compétences sont gratuites (recharge seule), la Furie
        /// ne part pas avant 100, puis Ultimate (R3) : taille, vitesse, cadence, recul mesurés avant et pendant ; sortie à 0.
        IEnumerator VikingFurie()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            yield return new WaitForSeconds(0.5f);
            var h = H; var v = h.Classe as ClasseViking; var b = GameBalance.Courant;
            if (v == null) { Log("viking_furie : le héros local n'est pas un viking"); Missiles(true); yield break; }
            var modele = h.animator.transform;
            Log("furie : départ, rage " + v.ValeurJauge.ToString("F0") + "/" + v.JaugeMax.ToString("F0") + ", vitesse " + v.Vitesse.ToString("F2") + ", échelle modèle " + modele.localScale.y.ToString("F3") + ", capsule " + h.CC.height.ToString("F2") + ", prête " + v.UltimePret);

            // 1. Impossible avant 100.
            EntreesSimulees.Appui("rightStickPress", 0.1f);
            yield return new WaitForSeconds(0.4f);
            Log("furie : R3 à rage " + v.ValeurJauge.ToString("F0") + " → en furie " + v.EnFurie + " (attendu : non)");

            // 2. Rage en frappant : trois sbires figés dans l'arc, deux coups de hache.
            var groupe = new List<Squelette> { DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.0f, -0.8f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.0f, 0f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.0f, 0.8f) };
            yield return Sortir(groupe);
            foreach (var s in groupe) if (s != null) s.Sante.Fixer(100000f, 100000f);
            Figer(groupe, 20f);
            float r0 = v.ValeurJauge;
            Vector3 p0 = groupe[1].transform.position;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.9f);
            Log("furie : un coup de hache sur 3 ennemis : rage " + r0.ToString("F0") + " → " + v.ValeurJauge.ToString("F0") + " (attendu +24), recul de la cible centrale " + Vector3.Distance(p0, groupe[1].transform.position).ToString("F2") + " m (base " + b.hacheRecul + ")");
            float recul0 = Vector3.Distance(p0, groupe[1].transform.position);

            // 3. Rage en encaissant : 20 dégâts, puis un coup énorme plafonné.
            float rb = v.ValeurJauge;
            DevPartie.Blesser(20f);
            yield return new WaitForSeconds(0.2f);
            float apres20 = v.ValeurJauge;
            DevPartie.Blesser(60f);
            yield return new WaitForSeconds(0.2f);
            Log("furie : encaissé 20 dégâts : rage " + rb.ToString("F0") + " → " + apres20.ToString("F0") + " (attendu +10) ; encaissé 60 : → " + v.ValeurJauge.ToString("F0") + " (plafond " + b.rageRecuMax + " par coup)");

            // 4. Compétences gratuites, limitées par leur recharge : LB, RB, LT (tournante) sans toucher à la rage.
            float rc = v.ValeurJauge;
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(2.4f);
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(1.6f);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(1.5f);
            string actif = Emp(h, 1);
            yield return new WaitForSeconds(2.2f);   // 3 s de maintien : s'arrête seule
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.3f);
            Log("furie : rugissement, saut, tournante lancés sans coût ; icônes : tournante pendant « " + actif + " », puis tournante " + Emp(h, 1) + ", rugissement " + Emp(h, 2) + ", saut " + Emp(h, 3) + " ; rage " + rc.ToString("F0") + " → " + v.ValeurJauge.ToString("F0") + " (monte avec les tics, jamais de coût)");
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.5f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.3f);
            Log("furie : tournante relancée pendant sa recharge → action " + v.Occupe + " (attendu : non), état " + Emp(h, 1));

            // 5. Mesures avant la Furie : cadence (6 s de martelage), vitesse, taille.
            foreach (var s in groupe) if (s != null && s.Vivant) s.Sante.Encaisser(new InfoDegats { montant = 1e9f, equipeSource = Equipe.Heros });
            DevPartie.PlacerHeros(Depart, Loin);
            var cible = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 1.6f, 0f);
            yield return Sortir(new List<Squelette> { cible });
            cible.Sante.Fixer(100000f, 100000f);
            float intervalleAvant = 0f;
            yield return Marteler(cible, 6f, i => intervalleAvant = i);
            Log("furie : AVANT — intervalle moyen entre coups " + intervalleAvant.ToString("F3") + " s (hacheIntervalle " + b.hacheIntervalle + "), vitesse " + v.Vitesse.ToString("F2") + ", échelle " + modele.localScale.y.ToString("F3") + ", rage " + v.ValeurJauge.ToString("F0"));
            DevPartie.Capturer("viking_furie_avant");
            yield return new WaitForSeconds(0.4f);
            cible.Sante.Encaisser(new InfoDegats { montant = 1e9f, equipeSource = Equipe.Heros });

            DevPartie.PlacerHeros(Depart, Loin);
            Vector3 d0 = h.transform.position;
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1.5f);
            yield return new WaitForSeconds(1.6f);
            float vitesseAvant = Vector3.Distance(d0, h.transform.position) / 1.5f;
            Log("furie : AVANT — vitesse mesurée " + vitesseAvant.ToString("F2") + " m/s");

            // 6. Rage pleine : prête (jauge, HUD), puis R3.
            DevPartie.PlacerHeros(Depart, Loin);
            v.RemplirJauge();
            yield return new WaitForSeconds(0.5f);
            Log("furie : rage pleine → prête " + v.UltimePret + " (en furie " + v.EnFurie + ")");
            DevPartie.Capturer("viking_furie_prete_hud");
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Appui("rightStickPress", 0.1f);
            yield return new WaitForSeconds(1.2f);
            Log("furie : R3 à rage pleine → en furie " + v.EnFurie + ", échelle modèle " + modele.localScale.y.ToString("F3") + " (cible " + b.furieEchelle + "), capsule " + h.CC.height.ToString("F2") + ", vitesse " + v.Vitesse.ToString("F2") + ", dégâts ×" + v.FacteurDegats.ToString("F2") + ", rage " + v.ValeurJauge.ToString("F0"));
            DevPartie.Capturer("viking_furie_active");
            yield return new WaitForSeconds(0.4f);

            // 7. Pendant la Furie (10 s : chaque mesure la redéclenche si besoin) : recul, cadence, vitesse.
            DevPartie.PlacerHeros(Depart, Loin);
            var sb = DevPartie.PoserDevant(TypeEnnemi.Sbire, 1.8f, 0f);
            yield return Sortir(new List<Squelette> { sb });
            sb.Sante.Fixer(100000f, 100000f);
            sb.Etourdir(10f);
            yield return AssurerFurie(v);
            Vector3 q0 = sb.transform.position;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(1.0f);
            float reculFurie = Vector3.Distance(q0, sb.transform.position);
            Log("furie : PENDANT — recul d'un coup " + reculFurie.ToString("F2") + " m (hors Furie " + recul0.ToString("F2") + " m, ×" + (reculFurie / Mathf.Max(0.01f, recul0)).ToString("F2") + ") ; en furie " + v.EnFurie + ", rage " + v.ValeurJauge.ToString("F0"));
            sb.Sante.Encaisser(new InfoDegats { montant = 1e9f, equipeSource = Equipe.Heros });

            DevPartie.PlacerHeros(Depart, Loin);
            var cible2 = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 1.6f, 0f);
            yield return Sortir(new List<Squelette> { cible2 });
            cible2.Sante.Fixer(100000f, 100000f);
            yield return AssurerFurie(v);
            float rageAvantCoups = v.ValeurJauge;
            float intervalleFurie = 0f;
            yield return Marteler(cible2, 4.5f, i => intervalleFurie = i);
            Log("furie : PENDANT — intervalle moyen entre coups " + intervalleFurie.ToString("F3") + " s (avant " + intervalleAvant.ToString("F3") + ", ×" + (intervalleAvant / Mathf.Max(0.01f, intervalleFurie)).ToString("F2") + " de cadence), rage avant les coups " + rageAvantCoups.ToString("F0") + ", après " + v.ValeurJauge.ToString("F0") + " (elle baisse seule, les coups ne la font pas monter), en furie " + v.EnFurie + ", échelle " + modele.localScale.y.ToString("F3"));
            DevPartie.Capturer("viking_furie_combat");
            yield return new WaitForSeconds(0.3f);
            cible2.Sante.Encaisser(new InfoDegats { montant = 1e9f, equipeSource = Equipe.Heros });

            DevPartie.PlacerHeros(Depart, Loin);
            yield return AssurerFurie(v);
            yield return new WaitForSeconds(1.3f);   // fin du geste de hache (vitesse ×0,25 pendant le coup)
            Vector3 f0 = h.transform.position;
            EntreesSimulees.Stick(new Vector2(0f, 1f), Vector2.zero, 1.5f);
            yield return new WaitForSeconds(1.6f);
            float vitesseFurie = Vector3.Distance(f0, h.transform.position) / 1.5f;
            Log("furie : PENDANT — vitesse mesurée " + vitesseFurie.ToString("F2") + " m/s (avant " + vitesseAvant.ToString("F2") + ", ×" + (vitesseFurie / Mathf.Max(0.01f, vitesseAvant)).ToString("F2") + "), en furie " + v.EnFurie + ", rage " + v.ValeurJauge.ToString("F0"));

            // 8. Sortie : la rage tombe à 0, taille normale.
            float t = 0f;
            while (v.EnFurie && t < 14f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(1.2f);
            Log("furie : SORTIE après " + t.ToString("F1") + " s depuis la mesure ; en furie " + v.EnFurie + ", rage " + v.ValeurJauge.ToString("F0") + ", échelle " + modele.localScale.y.ToString("F3") + ", vitesse " + v.Vitesse.ToString("F2"));
            DevPartie.Capturer("viking_furie_apres");
            Missiles(true);
        }

        /// Réseau (simulé) : un viking marionnette (héros d'un autre poste) reçoit l'état de Furie de son propriétaire
        /// (HerosReseau.m_Furie → ClasseViking.ForcerFurieDistante) : il grossit et rougeoie, sans simuler de bonus. À côté du
        /// viking local resté normal : capture « côte à côte ».
        IEnumerator VikingFurieDistant()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H; var p = Partie.Instance;
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver("viking") : null;
            if (def == null || def.prefab == null) { Log("furie distant : prefab du viking introuvable"); Missiles(true); yield break; }
            var go = Instantiate(def.prefab, h.transform.position + h.transform.right * 1.5f + h.transform.forward * 0.2f, h.transform.rotation);
            go.name = "Heros_allie_viking_test";
            var allie = go.GetComponent<Heros>();
            p.AttacherHerosDistant(allie, "viking", "Allié", 7);
            var va = allie.Classe as ClasseViking;
            var ml = h.animator.transform; var ma = allie.animator.transform;
            yield return new WaitForSeconds(0.8f);
            Log("furie distant : avant, échelle locale " + ml.localScale.y.ToString("F3") + ", marionnette " + ma.localScale.y.ToString("F3") + ", en furie " + va.EnFurie);
            DevPartie.Capturer("viking_furie_cote_a_cote_avant");
            va.ForcerFurieDistante(true);
            yield return new WaitForSeconds(1.0f);
            Log("furie distant : état reçu → marionnette en furie " + va.EnFurie + ", échelle " + ma.localScale.y.ToString("F3") + " (cible " + (0.8f * GameBalance.Courant.furieEchelle).ToString("F3") + "), local " + ml.localScale.y.ToString("F3") + ", capsule marionnette " + allie.CC.height.ToString("F2"));
            DevPartie.Capturer("viking_furie_cote_a_cote");
            yield return new WaitForSeconds(0.8f);
            va.EffetDistant(11, Vector3.zero, Vector3.zero, 0f);   // son d'entrée (E_FurieDebut)
            va.ForcerFurieDistante(false);
            yield return new WaitForSeconds(1.2f);
            Log("furie distant : fin reçue → échelle " + ma.localScale.y.ToString("F3") + ", en furie " + va.EnFurie);
            p.DetacherHeros(7);
            Destroy(go);
            Missiles(true);
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

        /// Kit du mage du 01/10/2026 : grande boule (LB) sur un groupe de 5 sbires figés (dégâts, +1 palier de brûlure, l'un
        /// déjà en brûlure), régénération du mana (3/s), cône (30 DPS, Ralenti), mur de flammes (RB) traversé par une vague
        /// de sbires qui marchent (brûlure, Ralenti). Captures mage_grande_boule.png, mage_mur.png.
        IEnumerator MageKit()
        {
            Missiles(false);
            DevPartie.PlacerHeros(Depart, Loin);
            var h = H; var b = GameBalance.Courant;
            var mage = h.Classe as ClasseMage;
            // 1) Grande boule de feu sur 5 sbires figés à 11-13 m ; le deuxième brûle déjà (palier 1, demi-jauge).
            var groupe = new List<Squelette>
            {
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 11f, 0f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 11.5f, -1.6f),
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 11.5f, 1.6f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 12.8f, -0.8f),
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 12.8f, 0.9f),
            };
            yield return Sortir(groupe);
            Figer(groupe, 25f);
            Brulure.Allumer(groupe[1].Sante, h, 0.5f);
            yield return null;
            var pv = new float[groupe.Count]; var palierAvant = new int[groupe.Count];
            for (int i = 0; i < groupe.Count; i++) { pv[i] = groupe[i].Sante.Pv; palierAvant[i] = PalierBrulure(groupe[i]); }
            h.Classe.RemplirJauge();
            ViserPoint(groupe[0].transform.position + Vector3.up);
            yield return null;
            bool explose = false; Sante direct = null;
            System.Action<ProjectileJeu.Genre, Vector3, Sante, float> suivi = (g, p, s, haut) => { if (g == ProjectileJeu.Genre.GrandeBouleDeFeu) { explose = true; direct = s; } };
            ProjectileJeu.Arrivee += suivi;
            EntreesSimulees.Appui("leftShoulder", 0.1f);
            yield return new WaitForSeconds(0.3f);
            float t0 = Time.time;
            EntreesSimulees.Appui("rightTrigger", 0.1f);   // visée au sol (02/10/2026) : RT confirme
            yield return new WaitForSeconds(0.55f);
            DevPartie.Capturer("mage_grande_boule_lancer");
            while (!explose && Time.time - t0 < 4f) yield return null;
            float vol = Time.time - t0;
            ProjectileJeu.Arrivee -= suivi;
            yield return new WaitForSeconds(0.12f);
            DevPartie.Capturer("mage_grande_boule");
            yield return new WaitForSeconds(0.3f);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < groupe.Count; i++)
            {
                var s = groupe[i];
                sb.Append(" | sbire " + i + (s.Sante == direct ? " (cible)" : "") + " : " + (pv[i] - s.Sante.Pv).ToString("F0") + " dégâts (brûlure comprise), palier " + palierAvant[i] + " → " + PalierBrulure(s)
                    + ", à " + Vector3.Distance(s.transform.position, groupe[0].transform.position).ToString("F1") + " m du premier");
            }
            h.Classe.Emplacement(2, out float restant, out float total);
            Log("mage : grande boule, explosion " + vol.ToString("F2") + " s après l'appui, mana " + h.Classe.ValeurJauge.ToString("F0") + "/" + b.manaMax + ", recharge " + restant.ToString("F1") + "/" + total.ToString("F0") + " s" + sb);
            // 2) Régénération du mana : 2 s sans rien faire.
            float m0 = h.Classe.ValeurJauge;
            yield return new WaitForSeconds(2f);
            Log("mage : mana " + m0.ToString("F1") + " → " + h.Classe.ValeurJauge.ToString("F1") + " en 2 s (" + ((h.Classe.ValeurJauge - m0) / 2f).ToString("F2") + " par seconde, attendu " + b.manaRegen + ")");
            foreach (var s in groupe) if (s != null) Destroy(s.gameObject);
            // 3) Cône maintenu 2 s sur un guerrier figé à 3,5 m : dégâts, mana, Ralenti.
            DevPartie.PlacerHeros(Depart + new Vector3(-3f, 0f, 3f), Loin + new Vector3(-3f, 0f, 3f));
            var proche = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 3.5f, 0f);
            yield return Sortir(new[] { proche });
            Figer(new[] { proche }, 20f);
            ViserPoint(proche.transform.position + Vector3.up);
            h.Classe.RemplirJauge();
            float pvC = proche.Sante.Pv, manaC = h.Classe.ValeurJauge;
            EntreesSimulees.Maintenir("leftTrigger", true);
            float ralenti = 0f;
            for (float t = 0f; t < 2f; t += Time.deltaTime) { if (proche.Statuts != null) ralenti = Mathf.Max(ralenti, proche.Statuts.Intensite(TypeStatut.Ralenti)); yield return null; }
            float pvApres = proche.Sante.Pv, manaApres = h.Classe.ValeurJauge;
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("mage : cône 2 s, " + (pvC - pvApres).ToString("F0") + " dégâts (attendu ~" + (b.coneDegats * 1.75f).ToString("F0") + " + brûlure), mana " + manaC.ToString("F0") + " → " + manaApres.ToString("F0") + " (attendu −" + (b.coneMana * 2f).ToString("F0") + "), Ralenti " + Mathf.RoundToInt(ralenti * 100f) + " %, brûlure palier " + PalierBrulure(proche));
            if (proche != null) Destroy(proche.gameObject);
            yield return new WaitForSeconds(0.5f);
            // 4) Mur de flammes : une vague de 6 sbires libres arrive de 14-15 m ; le mur se pose à 4 m devant le mage.
            DevPartie.PlacerHeros(Depart, Loin);
            var vague = new List<Squelette>();
            for (int i = 0; i < 6; i++) vague.Add(DevPartie.PoserDevant(TypeEnnemi.Sbire, 14f + (i % 2) * 1.5f, -2.5f + i));
            yield return Sortir(vague);
            ViserPoint(h.transform.position + h.transform.forward * 8f);
            h.Classe.RemplirJauge();
            yield return null;
            EntreesSimulees.Appui("rightShoulder", 0.1f);
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Appui("rightTrigger", 0.1f);   // visée au sol (02/10/2026) : RT confirme le mur
            var vuRalenti = new bool[vague.Count]; var palierMax = new int[vague.Count]; var vitMin = new float[vague.Count];
            for (int i = 0; i < vague.Count; i++) vitMin[i] = 99f;
            bool capture = false;
            float debut = Time.time;
            while (Time.time - debut < b.murInstant + b.murDuree)
            {
                for (int i = 0; i < vague.Count; i++)
                {
                    var s = vague[i];
                    if (s == null || s.Statuts == null) continue;
                    if (s.Statuts.A(TypeStatut.Ralenti)) { vuRalenti[i] = true; if (s.Agent != null && s.Agent.enabled) vitMin[i] = Mathf.Min(vitMin[i], s.Agent.velocity.magnitude); }
                    palierMax[i] = Mathf.Max(palierMax[i], PalierBrulure(s));
                }
                if (!capture && Time.time - debut > b.murInstant + 2.2f) { capture = true; DevPartie.Capturer("mage_mur"); }
                yield return null;
            }
            var sm = new System.Text.StringBuilder();
            int touches = 0;
            for (int i = 0; i < vague.Count; i++)
            {
                if (vuRalenti[i] || palierMax[i] > 0) touches++;
                sm.Append(" | sbire " + i + " : ralenti " + vuRalenti[i] + (vitMin[i] < 99f ? " (" + vitMin[i].ToString("F1") + " m/s au plus lent)" : "") + ", palier max " + palierMax[i]);
            }
            Log("mage : mur de flammes à " + (mage != null ? Vector3.Distance(Flat(mage.DernierMurCentre), Flat(h.transform.position)).ToString("F1") : "?") + " m, " + touches + "/" + vague.Count + " sbires pris, mana " + h.Classe.ValeurJauge.ToString("F0") + sm);
            foreach (var s in vague) if (s != null) Destroy(s.gameObject);
            Missiles(true);
        }


        // ================================================================= Visée d'une zone au sol (02/10/2026)

        /// Appui sur une compétence de zone (LB, RB), puis confirmation par RT (ou annulation par LT) : la visée du mage
        /// (grande boule, mur) et du rôdeur (nuée) s'ouvre à l'appui sur la compétence, le sort part à la confirmation.
        static IEnumerator ViseeZone(string controle, float attente = 0.3f)
        {
            EntreesSimulees.Appui(controle, 0.1f);
            yield return new WaitForSeconds(attente);
        }

        /// Point du sol (plan y = `y`) que le centre de l'écran traverse : la vérité terrain de l'indicateur de visée.
        static Vector3 PointSurLePlan(float y)
        {
            var cam = H.CameraJeu;
            Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float t = (y - r.origin.y) / r.direction.y;
            return r.origin + r.direction * t;
        }

        /// Retire les ennemis en place (le Play garde les sorties du jour) et pose le décor d'essai.
        static void Nettoyer()
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null) return;
            foreach (var s in new List<Squelette>(dv.Vivants)) if (s != null) Destroy(s.gameObject);
        }

        /// Mage : visée de la grande boule (cercle de 5 m qui suit le réticule), annulation sans coût (LT), confirmation (RT) :
        /// la boule tombe au centre du cercle ; visée du mur (ligne de 8 m en travers), changement de sort en cours de visée,
        /// annulation par l'esquive et par l'étourdissement, boule de feu primaire et cône inchangés.
        IEnumerator MageVisee()
        {
            Missiles(false);
            Nettoyer();
            var h = H; var b = GameBalance.Courant;
            var mage = h.Classe as ClasseMage;
            DevPartie.PlacerHeros(Depart, Loin);
            yield return null;
            var sol = h.transform.position;
            Vector3 avant = Flat(h.transform.forward).normalized, droite = Flat(h.transform.right).normalized;
            var groupe = new List<Squelette>
            {
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 10f, 0f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 10.6f, -1.4f),
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 10.6f, 1.4f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 12.5f, 0.3f),
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 17f, 4.5f),   // hors du cercle : ne doit rien subir
            };
            yield return Sortir(groupe);
            Figer(groupe, 60f);
            h.Classe.RemplirJauge();
            Vector3 centreGroupe = (groupe[0].transform.position + groupe[1].transform.position + groupe[2].transform.position + groupe[3].transform.position) / 4f;
            ViserPoint(new Vector3(centreGroupe.x, sol.y, centreGroupe.z));
            yield return null;

            // 1) LB : la visée s'ouvre, rien n'est dépensé, le cercle suit le réticule.
            float mana0 = h.Classe.ValeurJauge;
            yield return ViseeZone("leftShoulder");
            h.Classe.Emplacement(2, out float rest, out _);
            Vector3 vrai = PointSurLePlan(sol.y);
            Log("mage visée 1 : visée " + h.Classe.EnVisee + " (sort " + h.Classe.SortVise + "), occupé " + h.Classe.Occupe + ", mana " + mana0.ToString("F0") + " → " + h.Classe.ValeurJauge.ToString("F0")
                + ", recharge " + rest.ToString("F1") + " s, point " + h.Classe.PointVise.ToString("F2") + " (réticule/sol " + vrai.ToString("F2") + ", écart " + Vector3.Distance(Flat(vrai), Flat(h.Classe.PointVise)).ToString("F2") + " m), valide " + h.Classe.PointViseValide
                + ", distance " + Vector3.Distance(Flat(sol), Flat(h.Classe.PointVise)).ToString("F1") + " m");
            DevPartie.Capturer("mage_visee_grande");
            yield return new WaitForSeconds(0.2f);

            // 2) Le point suit la caméra et reste limité à la portée.
            ViserPoint(sol + avant * 6f + droite * 3f);
            yield return new WaitForSeconds(0.15f);
            Log("mage visée 2 : point proche " + Vector3.Distance(Flat(sol), Flat(h.Classe.PointVise)).ToString("F1") + " m (attendu 6,7)");
            ViserPoint(sol + avant * 60f + Vector3.up * 12f);   // vers l'horizon : au plus loin
            yield return new WaitForSeconds(0.15f);
            Log("mage visée 2 : point lointain " + Vector3.Distance(Flat(sol), Flat(h.Classe.PointVise)).ToString("F1") + " m (portée " + b.grandeBoulePortee + ")");
            ViserPoint(new Vector3(centreGroupe.x, sol.y, centreGroupe.z));
            yield return new WaitForSeconds(0.2f);

            // 3) LT annule : ni mana ni recharge, le cône ne part pas.
            EntreesSimulees.Appui("leftTrigger", 0.1f);
            yield return new WaitForSeconds(0.3f);
            h.Classe.Emplacement(2, out rest, out _);
            Log("mage visée 3 : annulée par LT, visée " + h.Classe.EnVisee + ", occupé " + h.Classe.Occupe + ", mana " + h.Classe.ValeurJauge.ToString("F0") + ", recharge " + rest.ToString("F1") + " s (attendu : aucune visée, 100, 0)");
            // LT maintenu longuement après une annulation : le cône ne doit pas partir tant que LT n'est pas relâché.
            yield return ViseeZone("leftShoulder");
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.5f);
            Log("mage visée 3 : LT maintenu après l'annulation, visée " + h.Classe.EnVisee + ", occupé " + h.Classe.Occupe + " (attendu : fermée, libre)");
            EntreesSimulees.Maintenir("leftTrigger", false);
            yield return new WaitForSeconds(0.3f);

            // 3 bis) Changer de sort en visant : RB (mur) puis LB (grande boule) puis annulation.
            yield return ViseeZone("rightShoulder");
            int s1 = h.Classe.SortVise;
            yield return ViseeZone("leftShoulder");
            int s2 = h.Classe.SortVise;
            EntreesSimulees.Appui("leftTrigger", 0.1f);
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Maintenir("leftTrigger", false);
            Log("mage visée 3 : RB puis LB en visant, sorts " + s1 + " puis " + s2 + " (attendu 2 puis 1), annulée " + !h.Classe.EnVisee + ", mana " + h.Classe.ValeurJauge.ToString("F0"));
            yield return new WaitForSeconds(0.3f);

            // 4) LB puis RT : la boule part sur le point, tombe au centre du cercle ; les gros dégâts vont à l'ennemi du cœur.
            yield return ViseeZone("leftShoulder");
            Vector3 cible = h.Classe.PointVise;
            var pv = new float[groupe.Count];
            for (int i = 0; i < groupe.Count; i++) pv[i] = groupe[i].Sante.Pv;
            Vector3 arrivee = Vector3.zero; bool tombee = false;
            System.Action<ProjectileJeu.Genre, Vector3, Sante, float> suivi = (g, p, s, haut) => { if (g == ProjectileJeu.Genre.GrandeBouleDeFeu) { tombee = true; arrivee = p; } };
            ProjectileJeu.Arrivee += suivi;
            float t0 = Time.time;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.2f);
            h.Classe.Emplacement(2, out rest, out float total);
            Log("mage visée 4 : confirmée, visée " + h.Classe.EnVisee + ", mana " + h.Classe.ValeurJauge.ToString("F0") + " (attendu " + (mana0 - b.grandeBouleMana).ToString("F0") + "), recharge " + rest.ToString("F1") + "/" + total.ToString("F0") + " s");
            yield return new WaitForSeconds(0.5f);
            DevPartie.Capturer("mage_visee_grande_vol");
            while (!tombee && Time.time - t0 < 5f) yield return null;
            ProjectileJeu.Arrivee -= suivi;
            yield return new WaitForSeconds(0.1f);
            DevPartie.Capturer("mage_visee_grande_explosion");
            yield return new WaitForSeconds(0.2f);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < groupe.Count; i++) sb.Append(" | sbire " + i + " : " + (pv[i] - groupe[i].Sante.Pv).ToString("F0") + " dégâts, à " + Vector3.Distance(Flat(groupe[i].transform.position), Flat(cible)).ToString("F1") + " m du centre");
            Log("mage visée 4 : explosion " + (Time.time - t0).ToString("F2") + " s après l'appui, tombée " + tombee + " en " + arrivee.ToString("F2") + " (visé " + cible.ToString("F2") + ", écart " + Vector3.Distance(arrivee, cible).ToString("F2") + " m)" + sb);

            // 5) Mur : RB ouvre la visée de la ligne ; changer de sort en visant ; RT confirme, le mur se pose au point visé.
            h.Classe.RemplirJauge();
            yield return new WaitForSeconds(0.3f);
            ViserPoint(sol + avant * 9f + droite * 2f);
            yield return ViseeZone("rightShoulder");
            Log("mage visée 5 : mur, visée " + h.Classe.EnVisee + " (sort " + h.Classe.SortVise + "), axe " + h.Classe.AxeVise.ToString("F2") + " (en travers de la visée), mana " + h.Classe.ValeurJauge.ToString("F0"));
            DevPartie.Capturer("mage_visee_mur");
            Vector3 centreVise = h.Classe.PointVise, axeVise = h.Classe.AxeVise;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(b.murInstant + 0.5f);
            DevPartie.Capturer("mage_visee_mur_pose");
            Log("mage visée 5 : mur posé en " + (mage != null ? mage.DernierMurCentre.ToString("F2") : "?") + " (visé " + centreVise.ToString("F2") + ", écart " + (mage != null ? Vector3.Distance(mage.DernierMurCentre, centreVise).ToString("F2") : "?") + " m), axe " + (mage != null ? mage.DernierMurAxe.ToString("F2") : "?") + " (visé " + axeVise.ToString("F2") + "), mana " + h.Classe.ValeurJauge.ToString("F0") + " (attendu " + (100f - b.murMana).ToString("F0") + ")");
            yield return new WaitForSeconds(b.murDuree);

            // 6) Esquive et étourdissement coupent la visée sans rien dépenser.
            h.Classe.RemplirJauge();
            yield return new WaitForSeconds(1.2f);
            float m1 = h.Classe.ValeurJauge;
            yield return ViseeZone("leftShoulder");
            EntreesSimulees.Appui("buttonEast", 0.1f);
            yield return new WaitForSeconds(0.2f);
            Log("mage visée 6 : esquive pendant la visée, visée " + h.Classe.EnVisee + ", mana " + m1.ToString("F0") + " → " + h.Classe.ValeurJauge.ToString("F0") + ", état " + h.EtatCourant);
            yield return new WaitForSeconds(0.8f);
            yield return ViseeZone("leftShoulder");
            h.Etourdir(0.4f);
            yield return new WaitForSeconds(0.2f);
            Log("mage visée 6 : étourdi pendant la visée, visée " + h.Classe.EnVisee + ", mana " + h.Classe.ValeurJauge.ToString("F0") + ", état " + h.EtatCourant);
            yield return new WaitForSeconds(0.8f);

            // 7) Pas de régression : RT seul lance la boule de feu ; LT maintenu fait le cône ; plus de visée ouverte.
            ViserPoint(new Vector3(centreGroupe.x, sol.y + 1f, centreGroupe.z));
            yield return null;
            bool explose = false;
            System.Action<ProjectileJeu.Genre, Vector3, Sante, float> suiviB = (g, p, s, haut) => { if (g == ProjectileJeu.Genre.BouleDeFeu) explose = true; };
            ProjectileJeu.Arrivee += suiviB;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            float t1 = Time.time;
            while (!explose && Time.time - t1 < 3f) yield return null;
            ProjectileJeu.Arrivee -= suiviB;
            Log("mage visée 7 : boule de feu primaire (RT) lancée et explosée " + explose + " en " + (Time.time - t1).ToString("F2") + " s, visée ouverte " + h.Classe.EnVisee);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.6f);
            Log("mage visée 7 : cône (LT maintenu), occupé " + h.Classe.Occupe + ", visée ouverte " + h.Classe.EnVisee + ", mana " + h.Classe.ValeurJauge.ToString("F0"));
            EntreesSimulees.Maintenir("leftTrigger", false);
            foreach (var s in groupe) if (s != null) Destroy(s.gameObject);
            Missiles(true);
        }

        /// Rôdeur : la nuée de flèches passe par la même visée (cercle de la pluie, thème Chasse) ; annulation sans recharge ;
        /// confirmation : la pluie tombe sur le point visé ; arc bandé (RT) et visée zoomée (LT) inchangés.
        IEnumerator RodeurVisee()
        {
            Missiles(false);
            Nettoyer();
            var h = H; var b = GameBalance.Courant;
            DevPartie.PlacerHeros(Depart, Loin);
            yield return null;
            var sol = h.transform.position;
            var groupe = new List<Squelette>
            {
                DevPartie.PoserDevant(TypeEnnemi.Sbire, 12f, -1.2f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 12f, 1.2f), DevPartie.PoserDevant(TypeEnnemi.Sbire, 13.2f, 0f),
            };
            yield return Sortir(groupe);
            Figer(groupe, 60f);
            Vector3 centreGroupe = (groupe[0].transform.position + groupe[1].transform.position + groupe[2].transform.position) / 3f;
            ViserPoint(new Vector3(centreGroupe.x, sol.y, centreGroupe.z));
            yield return null;
            // 1) LB : visée ouverte, aucune recharge ; LT annule.
            yield return ViseeZone("leftShoulder");
            h.Classe.Emplacement(2, out float rest, out _);
            Log("rôdeur visée 1 : visée " + h.Classe.EnVisee + ", occupé " + h.Classe.Occupe + ", recharge " + rest.ToString("F1") + " s, point " + h.Classe.PointVise.ToString("F2") + " (écart au sol visé " + Vector3.Distance(Flat(PointSurLePlan(sol.y)), Flat(h.Classe.PointVise)).ToString("F2") + " m)");
            DevPartie.Capturer("rodeur_visee_nuee");
            EntreesSimulees.Appui("leftTrigger", 0.1f);
            yield return new WaitForSeconds(0.3f);
            h.Classe.Emplacement(2, out rest, out _);
            Log("rôdeur visée 1 : annulée par LT, visée " + h.Classe.EnVisee + ", recharge " + rest.ToString("F1") + " s (attendu : fermée, 0)");
            // 2) LB, RT : la pluie tombe sur le point visé ; la recharge part à la confirmation.
            yield return ViseeZone("leftShoulder");
            Vector3 cible = h.Classe.PointVise;
            EntreesSimulees.Appui("rightTrigger", 0.1f);
            yield return new WaitForSeconds(0.25f);
            h.Classe.Emplacement(2, out rest, out float total);
            Log("rôdeur visée 2 : confirmée, visée " + h.Classe.EnVisee + ", recharge " + rest.ToString("F1") + "/" + total.ToString("F0") + " s, arc bandé (ne doit pas l'être) " + h.Classe.Emplacement(0, out _, out _));
            yield return new WaitForSeconds(0.9f);
            DevPartie.Capturer("rodeur_visee_nuee_pluie");
            yield return new WaitForSeconds(1.2f);
            var s0 = groupe[0];
            Log("rôdeur visée 2 : ralenti " + (s0 != null && s0.Statuts != null && s0.Statuts.A(TypeStatut.Ralenti)) + ", PV " + (s0 != null ? s0.Sante.Pv.ToString("F0") + "/" + s0.Sante.pvMax.ToString("F0") : "?") + ", point visé " + cible.ToString("F2"));
            // 3) Arc bandé (RT) et visée zoomée (LT) toujours là.
            yield return new WaitForSeconds(0.6f);
            ViserPoint(groupe[0].transform.position + Vector3.up);
            EntreesSimulees.Maintenir("rightTrigger", true);
            yield return new WaitForSeconds(0.5f);
            Log("rôdeur visée 3 : RT maintenu → arc bandé " + h.Classe.Emplacement(0, out _, out _) + ", visée de zone " + h.Classe.EnVisee);
            EntreesSimulees.Maintenir("rightTrigger", false);
            yield return new WaitForSeconds(0.6f);
            EntreesSimulees.Maintenir("leftTrigger", true);
            yield return new WaitForSeconds(0.4f);
            Log("rôdeur visée 3 : LT maintenu → visée zoomée " + h.Classe.Emplacement(1, out _, out _) + ", visée de zone " + h.Classe.EnVisee);
            EntreesSimulees.Maintenir("leftTrigger", false);
            foreach (var s in groupe) if (s != null) Destroy(s.gameObject);
            Missiles(true);
        }



        static int PalierBrulure(Squelette s)
        {
            if (s == null || s.Statuts == null) return 0;
            foreach (var st in s.Statuts.Liste) if (st.type == TypeStatut.Brulure) return st.PalierCourant;
            return 0;
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
            yield return new WaitForSeconds(0.3f);
            EntreesSimulees.Appui("rightTrigger", 0.1f);   // visée au sol (02/10/2026) : RT confirme la nuée
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
