#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Deathless.Jeu;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deathless.Dev.Tournage
{
    /// Les 12 plans de la bande-annonce (structure recommandée du storyboard : ouverture choc, coop, donjon, humour,
    /// crépuscule, nuit, Morgrim, écran final), joués dans la scène Village en Play par code, comme ScenariosParade ou
    /// ScenariosLisibilite : héros local d'une classe donnée (scène rechargée par session), marionnettes des autres
    /// classes (héros non locaux pilotés par HerosEntrees.DeplacementTest / SimulerAction), ennemis posés par
    /// DirecteurVagues.Poser. Sessions : A viking de nuit (plans 1-2), B mage de jour puis crépuscule et nuit 5
    /// (plans 3-9), C paladin de nuit (10), D assassin de nuit 10 (11-12).
    ///
    /// Les réglages de cadrage sont des champs statiques publics : on les ajuste en Play par execute_code entre deux
    /// essais (Tournage.Lancer("4", apercu: true)) sans recompiler.
    public class ScenarioTrailer
    {
        readonly Tournage T;
        CameraTournage Cam => T.Cam;
        HabillageTrailer Hab => T.Hab;
        MixeurTrailer Mix => T.Mix;
        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;
        static GameBalance B => GameBalance.Courant;

        string m_Session = "";
        bool m_FigerPhase;
        float m_TempsFige;
        readonly List<Heros> m_Marionnettes = new List<Heros>();
        readonly List<Squelette> m_Poses = new List<Squelette>();
        ILobby m_LobbyAvant;
        bool m_LobbyRemplace;

        public ScenarioTrailer(Tournage t) { T = t; }

        // ================================================================== Réglages (ajustables en Play)

        public static string ScenePath = "Assets/Scenes/Village.unity";
        // Plan 1
        public static Vector3 P1Heros = new Vector3(-9f, 0f, -9f), P1Regard = new Vector3(-30f, 0f, -30f);
        public static float P1Distance = 1.9f, P1Attaque = 0.55f, P1DelaiCoup = 0.55f;
        public static Vector3 P1CamDebut = new Vector3(4.4f, 1.5f, 0.6f), P1CamFin = new Vector3(3.9f, 1.45f, 0.55f);   // (côté, haut, devant) depuis le squelette, vers le héros
        public static float P1Champ = 36f, P1Vise = 0.4f, P1Hauteur = 1.1f;
        // Plan 2
        public static Vector3 P2Point = new Vector3(-17f, 0f, -21f);
        public static float P2CamDistance = 3.3f, P2CamHaut = 0.25f, P2Champ = 44f, P2CamCote = 0.3f, P2RegardDebut = 0.45f, P2RegardFin = 1.05f;
        // Plan 3
        public static string P3Code = "NYX4EV";
        public static Vector3 P3CamA = new Vector3(-10f, 9f, -24f), P3CamB = new Vector3(-4f, 9f, -24f), P3Regard = new Vector3(4f, 3f, 2f);
        // Jour (session B)
        public static float TempsJour = 45f, TempsNuit = 50f;
        // Plan 4
        public static float P4Depart = 6.5f, P4Arret = 2.6f;
        public static Vector3 P4CamSuivi = new Vector3(0.9f, 1.9f, -3.6f), P4CamRegard = new Vector3(0f, 1.5f, 5f);
        public static Vector3 P4CamArrivee = new Vector3(1.3f, 1.9f, 5.0f);
        // Plan 5
        public static float P5Devant = 1.35f, P5Ouverture = 0.6f;
        public static Vector3 P5CamA = new Vector3(3.0f, 3.4f, 3.3f), P5CamB = new Vector3(3.4f, 4.7f, 6.2f), P5Tas = new Vector3(-2.4f, 0f, 1.4f);
        // Plan 6
        public static float P6Devant = 1.3f;
        public static Vector3 P6Cam = new Vector3(0.55f, 2.15f, -0.75f);   // (côté, haut, devant) depuis l'ancre du comptoir
        public static Vector3 P6Regard = new Vector3(-0.1f, 1.35f, 1.3f);
        public static float P6Champ = 60f, P6RoulisFacteur = 2.5f, P6Rang = 1.2f, P6Ecart = 0.95f, P6Tangage = 16f;
        public static bool P6CameraJeu = false;
        // Plan 7
        public static Vector3 P7Heros = new Vector3(1.1f, 0f, -7.4f), P7Autre = new Vector3(-1.3f, 0f, -7.7f);
        public static Vector3 P7Cam = new Vector3(0.4f, 3.7f, -15.6f), P7Regard = new Vector3(0.0f, 0.8f, -7.5f);
        public static float P7Champ = 36f, P7Gloups = 1.75f, P7Rire = 2.25f;
        // Plan 8
        public static Vector3 P8CamA = new Vector3(-7f, 4.6f, -13.5f), P8CamB = new Vector3(-5.8f, 4.5f, -11.4f), P8Regard = new Vector3(3f, 3.8f, 1.5f);
        public static float P8Decalage = 0.05f;
        // Plan 9
        public static float P9Rayon = 13f, P9Hauteur = 4.2f, P9Angle = 200f, P9Vitesse = 42f, P9Champ = 56f, P9Attente = 2.0f;
        // Plan 10
        public static Vector3 P10Heros = new Vector3(0f, 0f, -11f), P10Regard = new Vector3(0f, 0f, -20f);
        public static Vector3 P10Cam = new Vector3(-4.6f, 1.6f, 1.0f);   // (côté, haut, devant) depuis le paladin
        public static float P10Champ = 40f, P10Avance = 0.05f;
        // Plan 11
        public static Vector3 P11Heros = new Vector3(-5f, 0f, -4f), P11Regard = new Vector3(-30f, 0f, -4f);
        public static float P11Distance = 4.3f, P11AvanceSaut = 0.36f;
        public static Vector3 P11Cam = new Vector3(7.5f, 1.1f, -1.0f);   // (côté, haut, avant) depuis le milieu
        public static float P11Champ = 46f;
        // Plan 12
        public static float P12Rayon = 7.5f, P12Hauteur = 0.2f, P12Angle = 170f, P12Vitesse = 5f, P12Champ = 42f;

        // ================================================================== Aiguillage

        public IEnumerator Jouer(int plan)
        {
            switch (plan)
            {
                case 1: yield return Session("viking", Phase.Nuit, 3); yield return Plan1(); break;
                case 2: yield return Session("viking", Phase.Nuit, 3); yield return Plan2(); break;
                case 3: yield return Session("mage", Phase.Jour, 5); yield return Plan3(); break;
                case 4: yield return Session("mage", Phase.Jour, 5); yield return Plan4(); break;
                case 5: yield return Session("mage", Phase.Jour, 5); yield return Plan5(); break;
                case 6: yield return Session("mage", Phase.Jour, 5); yield return Plan6(); break;
                case 7: yield return Session("mage", Phase.Jour, 5); yield return Plan7(); break;
                case 8: yield return Session("mage", Phase.Jour, 5); yield return Plan8(); break;
                case 9: yield return Session("mage", Phase.Jour, 5); yield return Plan9(); break;
                case 10: yield return Session("paladin", Phase.Nuit, 6); yield return Plan10(); break;
                case 11: yield return Session("assassin", Phase.Nuit, 10); yield return Plan11(); break;
                case 12: yield return Session("assassin", Phase.Nuit, 10); yield return Plan12(); break;
            }
        }

        // ================================================================== Outils

        /// Charge la scène avec la classe voulue si la session change (partie lancée d'office), puis la phase.
        IEnumerator Session(string classe, Phase phase, int nuit)
        {
            string cle = classe + "/" + phase + "/" + nuit;
            if (m_Session == cle && P != null && H != null) yield break;
            if (m_Session.StartsWith(classe + "/") && P != null && H != null && H.Classe != null && H.Classe.Id == classe)
            {
                // Même classe, autre phase (session B : jour puis crépuscule) : rien à recharger.
                m_Session = cle;
                yield break;
            }
            Nettoyer();
            Tournage.Log("session " + cle + " : chargement de la scène");
            Partie.ClasseChoisie = classe;
            Partie.LancerAuChargement = true;
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((P == null || H == null || !P.EnCours) && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            if (P == null || H == null) { Tournage.Log("ÉCHEC : partie non lancée"); yield break; }
            for (int i = 0; i < 5; i++) yield return null;
            Cam.Preparer(P.cameraJeu != null ? P.cameraJeu.GetComponent<Camera>() : Camera.main);
            Hab.BrancherHud();
            H.Invulnerable(1e5f);
            DefenseActive(false);
            P.ForcerPhase(phase, nuit);
            m_TempsFige = phase == Phase.Nuit ? TempsNuit : TempsJour;
            P.Etat.tempsPhase = m_TempsFige;
            m_FigerPhase = true;
            T.StartCoroutine(Figer());
            m_Session = cle;
            yield return Attendre(0.4f);
            Tournage.Log("session prête : " + H.name + ", " + P.Etat.phase + " nuit " + P.Etat.nuit);
        }

        /// Tient la phase au même instant (lumière stable, pas d'alerte ni de passage à la phase suivante).
        IEnumerator Figer()
        {
            var partie = P;
            while (partie != null && partie == P)
            {
                if (m_FigerPhase && (partie.Etat.phase == Phase.Jour || partie.Etat.phase == Phase.Nuit)) partie.Etat.tempsPhase = m_TempsFige;
                yield return null;
            }
        }

        static IEnumerator Attendre(float s) { yield return new WaitForSeconds(s); }

        /// Missiles de Nyxessa : coupés dans les plans à ennemis posés (ils les abattraient), actifs au plan 9.
        static void DefenseActive(bool oui)
        {
            var d = P != null && P.nyxessa != null ? P.nyxessa.GetComponent<DefenseNyxessa>() : null;
            if (d != null) d.enabled = oui;
        }

        /// Retire les gardiens du donjon (ils chargeraient le héros pendant le plan du coffre).
        static void SansGardiens()
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null) return;
            foreach (var s in new List<Squelette>(dv.Vivants)) if (s != null && s.Gardien) s.Desintegrer(true);
        }

        /// Nettoyage des objets posés (marionnettes, ennemis) et du lobby.
        public void Nettoyer()
        {
            foreach (var m in m_Marionnettes) if (m != null) Object.Destroy(m.gameObject);
            m_Marionnettes.Clear();
            foreach (var s in m_Poses) if (s != null) s.Desintegrer(true);
            m_Poses.Clear();
            if (m_LobbyRemplace) { DonneesUI.Lobby = m_LobbyAvant; m_LobbyRemplace = false; }
        }

        static Vector3 Plat(Vector3 v) { v.y = 0f; return v; }

        static Vector3 SurNavMesh(Vector3 p, float rayon = 3f)
        {
            if (NavMesh.SamplePosition(p + Vector3.up * 0.5f, out var hit, rayon, NavMesh.AllAreas)) return hit.position;
            return p;
        }

        /// Place un héros (téléportation) regardant `regard` ; héros local : la caméra du jeu suit.
        static void Placer(Heros h, Vector3 position, Vector3 regard)
        {
            if (h == null) return;
            h.Teleporter(position);
            Vector3 d = Plat(regard - position);
            if (d.sqrMagnitude > 0.001f) h.transform.rotation = Quaternion.LookRotation(d);
            if (h == H && P.cameraJeu != null) { P.cameraJeu.lacet = h.transform.eulerAngles.y; P.cameraJeu.tangage = 12f; }
        }

        /// Oriente le héros local (et la caméra du jeu : les attaques partent vers l'avant de la caméra) vers un point.
        static void Viser(Heros h, Vector3 point)
        {
            if (h == null) return;
            Vector3 d = Plat(point - h.transform.position);
            if (d.sqrMagnitude < 0.001f) return;
            h.transform.rotation = Quaternion.LookRotation(d);
            if (h == H && P.cameraJeu != null) P.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        /// Déplacement imposé (monde, 0-1) : converti en stick relatif à la caméra du jeu.
        static void Marcher(Heros h, Vector3 direction, float force = 1f)
        {
            if (h == null) return;
            var cam = P.cameraJeu;
            Vector3 avant = cam != null ? cam.AvantPlat : Vector3.forward;
            Vector3 droite = Vector3.Cross(Vector3.up, avant);
            Vector3 d = Plat(direction).normalized * force;
            h.Entrees.DeplacementTest = new Vector2(Vector3.Dot(d, droite), Vector3.Dot(d, avant));
        }

        static void Arreter(Heros h) { if (h != null) h.Entrees.DeplacementTest = Vector2.zero; }
        static void Liberer(Heros h) { if (h != null) h.Entrees.DeplacementTest = null; }

        /// Marionnette d'une autre classe : héros non local, sans poste réseau, piloté par code, invulnérable.
        Heros Marionnette(string classeId, Vector3 position, Vector3 regard, string pseudo)
        {
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(classeId) : null;
            if (def == null || def.prefab == null) { Tournage.Log("marionnette impossible : " + classeId); return null; }
            Vector3 d = Plat(regard - position);
            var go = Object.Instantiate(def.prefab, position, d.sqrMagnitude > 0.001f ? Quaternion.LookRotation(d) : Quaternion.identity);
            go.name = "Tournage_" + classeId;
            var h = go.GetComponent<Heros>();
            h.Initialiser(P, new EtatJoueur { id = 90 + m_Marionnettes.Count, nom = pseudo, classeId = classeId, classe = def.nom });
            h.Entrees.DeplacementTest = Vector2.zero;
            h.Invulnerable(1e5f);
            m_Marionnettes.Add(h);
            T.StartCoroutine(RendreRoueAuLocal());
            return h;
        }

        /// Les emotes d'une marionnette la prennent pour le joueur local (roue du HUD) : on rend la roue au vrai héros local.
        IEnumerator RendreRoueAuLocal()
        {
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                if (H != null && H.Emotes != null) DonneesUI.RoueEmotes = H.Emotes;
            }
        }

        Squelette Poser(TypeEnnemi type, Vector3 point, Vector3? regard = null)
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null) return null;
            var s = dv.Poser(type, SurNavMesh(point), false, false);
            if (s == null) return null;
            if (regard.HasValue)
            {
                Vector3 d = Plat(regard.Value - s.transform.position);
                if (d.sqrMagnitude > 0.001f) s.transform.rotation = Quaternion.LookRotation(d);
            }
            m_Poses.Add(s);
            return s;
        }

        static IEnumerator Sortis(IEnumerable<Squelette> liste, float max = 5f)
        {
            float t = 0f;
            bool enCours = true;
            while (enCours && t < max)
            {
                enCours = false;
                foreach (var s in liste) if (s != null && s.EtatCourant == Squelette.Etat.SortieDeTerre) enCours = true;
                t += Time.deltaTime;
                yield return null;
            }
        }

        static Vector3 Nyx => Nyxessa.Instance != null ? Nyxessa.Instance.CentreCristal : new Vector3(0f, 4f, 0f);

        /// Repère d'un point regardé : avant (de `depuis` vers `vers`), droite, haut ; position = o + droite*x + haut*y + avant*z.
        static Vector3 Repere(Vector3 o, Vector3 avant, Vector3 local)
        {
            avant = Plat(avant).normalized;
            Vector3 droite = Vector3.Cross(Vector3.up, avant);
            return o + droite * local.x + Vector3.up * local.y + avant * local.z;
        }

        // ================================================================== Plan 1 : ouverture choc (viking, nuit)

        IEnumerator Plan1()
        {
            var h = H;
            Hab.Hud(false);
            Placer(h, SurNavMesh(P1Heros), P1Regard);
            yield return Attendre(0.3f);
            Vector3 avant = Plat(P1Regard - P1Heros).normalized;
            var s = Poser(TypeEnnemi.Sbire, h.transform.position + avant * P1Distance, h.transform.position);
            if (s == null) { Tournage.Log("plan 1 : sbire non posé"); yield break; }
            yield return Sortis(new[] { s });
            s.Sante.Initialiser(1f);
            if (s.Agent != null) s.Agent.updateRotation = false;
            Viser(h, s.transform.position);
            // Cadrage : depuis le squelette vers le héros, décalé sur le côté, serré.
            Vector3 ps = s.transform.position, ph = h.transform.position;
            Vector3 versHeros = Plat(ph - ps).normalized;
            Vector3 regard = ps + Vector3.up * P1Hauteur + versHeros * P1Vise;
            Cam.Mouvement = CameraTournage.Rail(true,
                new CameraTournage.Cle(0f, Repere(ps, versHeros, P1CamDebut), regard, P1Champ),
                new CameraTournage.Cle(3f, Repere(ps, versHeros, P1CamFin), regard + Vector3.up * 0.1f, P1Champ - 4f));
            Hab.Centre("MOURIR NE VOUS\nARRÊTERA PAS.", 0f, 3.01f, true, "tr-centre--haut");
            Hab.Voile(t => t < 0.42f ? 1f : Mathf.Lerp(0.35f, 0.15f, Mathf.Clamp01((t - 0.42f) / 0.6f)));
            bool tue = false; float tImpact = -1f, tDesint = -1f;
            s.Sante.Tue += _ => tue = true;
            s.Retire += _ => { if (tDesint < 0f) tDesint = T.TempsPlan; };
            bool attaque = false;
            T.SonPic("balayage", P1Attaque + P1DelaiCoup, 0.75f);
            yield return T.Prise(3f, t =>
            {
                if (s != null && !tue)
                {
                    Vector3 vh = Plat(h.transform.position - s.transform.position);
                    if (vh.sqrMagnitude > 0.01f) s.transform.rotation = Quaternion.LookRotation(vh);
                    if (s.Agent != null && s.Agent.enabled && s.Agent.isOnNavMesh) s.Agent.isStopped = true;
                }
                if (!attaque && t >= P1Attaque && s != null) { attaque = true; Viser(h, s.transform.position); h.Entrees.SimulerAction("AttackPrimary"); }
                if (tue && tImpact < 0f)
                {
                    tImpact = t;
                    Cam.Secouer(t, 0.045f, 0.35f);
                    T.Son("boom_b", t, 0.7f, true);
                    T.Ralenti(0.6f);
                    Tournage.Log("plan 1 : coup fatal à " + t.ToString("0.00") + " s");
                }
                if (tDesint > 0f && Mathf.Abs(t - tDesint) < 0.001f) T.Son("impact_lourd", t, 0.35f, true);
            });
            T.Ralenti(1f);
            if (tImpact < 0f) Tournage.Log("plan 1 : le sbire n'a pas été tué pendant la prise");
            if (tDesint > 0f) Tournage.Log("plan 1 : désintégration à " + tDesint.ToString("0.00") + " s");
        }

        // ================================================================== Plan 2 : « Eux non plus » (sortie de terre)

        IEnumerator Plan2()
        {
            var h = H;
            Hab.Hud(false);
            Vector3 p = SurNavMesh(P2Point);
            Vector3 versNyx = Plat(Vector3.zero - p).normalized;
            // Héros hors du cadre (derrière la caméra, loin).
            Placer(h, SurNavMesh(p + versNyx * 14f + Vector3.Cross(Vector3.up, versNyx) * 6f), p);
            yield return Attendre(0.3f);
            Vector3 camPos = p + versNyx * P2CamDistance + Vector3.Cross(Vector3.up, versNyx) * P2CamCote + Vector3.up * P2CamHaut;
            Cam.Mouvement = CameraTournage.Rail(true,
                new CameraTournage.Cle(0f, camPos, p + Vector3.up * P2RegardDebut, P2Champ),
                new CameraTournage.Cle(2f, camPos - versNyx * 0.3f, p + Vector3.up * P2RegardFin, P2Champ - 3f));
            Hab.Centre("EUX NON PLUS.", 0f, 2.01f, true, "tr-centre--haut");
            Hab.Voile(t => 0.12f);
            Squelette s = null;
            T.Son("note_grave", 0f, 0.7f, true);
            yield return T.Prise(2f, t =>
            {
                if (s == null) s = Poser(TypeEnnemi.Sbire, p, camPos);
                if (s != null)
                {
                    Vector3 d = Plat(camPos - s.transform.position);
                    if (d.sqrMagnitude > 0.01f) s.transform.rotation = Quaternion.LookRotation(d);
                    if (s.Agent != null && s.Agent.enabled && s.Agent.isOnNavMesh) s.Agent.isStopped = true;
                }
            });
        }

        // ================================================================== Plan 3 : coop (lobby)

        IEnumerator Plan3()
        {
            var nav = Hab.Navigateur;
            if (nav == null) { Tournage.Log("plan 3 : pas de navigateur"); yield break; }
            var lob = new LobbyTournage { CodeSalon = P3Code, Etat = EtatLobby.Aucun };
            string moi = DonneesUI.Profil != null && !string.IsNullOrEmpty(DonneesUI.Profil.Pseudo) ? DonneesUI.Profil.Pseudo : "Quentin";
            lob.Ajouter("Morgane", "viking", false, true, 0f, 1.35f);
            lob.Ajouter(moi, "mage", true, false, 0f, 2.55f);
            lob.Ajouter("Tibo", "paladin", false, false, 1.15f, 1.8f);
            lob.Ajouter("Lysa", "rodeur", false, false, 1.25f, 2.2f);
            m_LobbyAvant = DonneesUI.Lobby; m_LobbyRemplace = true;
            DonneesUI.Lobby = lob;
            Hab.Hud(true);
            nav.Ouvrir(nav.Lobby);
            yield return null;
            var ecran = nav.Lobby;
            var fCode = typeof(Deathless.UI.Ecrans.EcranLobby).GetField("m_CodeSaisi", BindingFlags.NonPublic | BindingFlags.Instance);
            var mCode = typeof(Deathless.UI.Ecrans.EcranLobby).GetMethod("MajCodeSaisi", BindingFlags.NonPublic | BindingFlags.Instance);
            var racine = Hab.RacineHud;
            Cam.Mouvement = CameraTournage.Rail(true, new CameraTournage.Cle(0f, P3CamA, P3Regard, 50f), new CameraTournage.Cle(3f, P3CamB, P3Regard, 48f));
            Hab.Bas("COOP 4 JOUEURS\nREJOIGNEZ AVEC UN CODE.", 0.15f, 3.01f, "tr-bas--bandeau");
            T.Son("musique_jour", 0f, 0.72f, false, MixeurTrailer.Bus.Musique);
            int pretsAvant = 0; int lettres = -1;
            yield return T.Prise(3f, t =>
            {
                // Saisie du code (0 - 0,9 s), puis connexion au salon.
                int n = Mathf.Clamp(Mathf.FloorToInt(t / 0.13f), 0, P3Code.Length);
                if (t < 1.0f)
                {
                    lob.Etat = EtatLobby.Aucun;
                    if (n != lettres && fCode != null)
                    {
                        lettres = n;
                        fCode.SetValue(ecran, P3Code.Substring(0, n));
                        mCode?.Invoke(ecran, null);
                        if (n > 0) T.Son("clic", t, 0.25f);
                    }
                }
                else
                {
                    if (lob.Etat != EtatLobby.Salon) { lob.Etat = EtatLobby.Salon; T.Son("entree", t, 0.5f); }
                    int prets = lob.MettreA(t);
                    if (prets > pretsAvant) { T.Son("clic", t, 0.55f); pretsAvant = prets; }
                }
                if (racine != null)
                {
                    float u = t / 3f;
                    racine.style.translate = new Translate(new Length(Mathf.Lerp(22f, -22f, u), LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
                    float e = Mathf.Lerp(1.0f, 1.035f, u);
                    racine.style.scale = new Scale(new Vector3(e, e, 1f));
                }
            });
            if (racine != null) { racine.style.translate = StyleKeyword.Null; racine.style.scale = StyleKeyword.Null; }
            if (nav.Sommet == nav.Lobby) nav.Fermer();
            DonneesUI.Lobby = m_LobbyAvant; m_LobbyRemplace = false;
        }

        // ================================================================== Plan 4 : portail vers le donjon

        IEnumerator Plan4()
        {
            var h = H;
            Hab.Hud(false);
            var dj = DonjonJeu.Instance;
            var vc = Object.FindAnyObjectByType<VueCycle>();
            var pv = vc != null ? vc.portail : null;
            float t0 = Time.realtimeSinceStartup;
            while ((dj == null || !dj.Pret) && Time.realtimeSinceStartup - t0 < 10f) { yield return null; dj = DonjonJeu.Instance; }
            if (dj == null || pv == null) { Tournage.Log("plan 4 : pas de donjon ou de portail"); yield break; }
            Vector3 c = pv.Center;
            Vector3 versNyx = Plat(Vector3.zero - c).normalized;
            Vector3 depart = SurNavMesh(new Vector3(c.x, 0f, c.z) + versNyx * P4Depart);
            Placer(h, depart, new Vector3(c.x, depart.y, c.z));
            yield return Attendre(0.5f);
            Cam.Mouvement = CameraTournage.Suivre(h.transform, P4CamSuivi, P4CamRegard, 52f, 5f);
            Hab.Bas("CHAQUE JOUR, UN DONJON DIFFÉRENT.", 0.25f, 4.01f);
            float tPasse = -1f;
            yield return T.Prise(1.9f, t =>
            {
                float d = Vector3.Distance(Plat(h.transform.position), Plat(c));
                if (tPasse < 0f)
                {
                    Viser(h, c);
                    if (d > P4Arret && t < 1.3f) Marcher(h, c - h.transform.position);
                    else
                    {
                        Arreter(h);
                        if (dj.InvitePortail(false, h, out _) != null) { h.Entrees.SimulerAction("Interact"); tPasse = t; Tournage.Log("plan 4 : passage à " + t.ToString("0.00") + " s, " + d.ToString("0.0") + " m du centre"); }
                        else if (t > 1.5f) { dj.Passer(false, h); tPasse = t; Tournage.Log("plan 4 : invite absente, passage forcé"); }
                    }
                }
            });
            // Coupe : arrivée au donjon.
            t0 = Time.realtimeSinceStartup;
            while (!DonjonJeu.AuDonjon(h) && Time.realtimeSinceStartup - t0 < 8f) yield return null;
            if (!DonjonJeu.AuDonjon(h)) Tournage.Log("plan 4 : le héros n'est pas arrivé au donjon");
            Liberer(h); Arreter(h);
            yield return null;
            Mix.CouperJeu();
            Vector3 a = h.transform.position;
            Vector3 f = Plat(h.transform.forward).normalized;
            Vector3 camA = Repere(a, f, P4CamArrivee);
            Cam.Mouvement = CameraTournage.Rail(true,
                new CameraTournage.Cle(1.9f, camA, a + Vector3.up * 1.5f, 55f),
                new CameraTournage.Cle(4f, camA - f * 0.7f + Vector3.up * 0.05f, a + Vector3.up * 1.2f, 52f));
            T.Son("woosh", 1.9f, 0.8f, true);
            yield return T.Prise(2.1f, null, 1.9f);
        }

        // ================================================================== Plan 5 : coffre et or (donjon)

        IEnumerator Plan5()
        {
            var h = H;
            var dj = DonjonJeu.Instance;
            var gen = dj != null ? dj.generateur : null;
            if (gen == null || !dj.Pret) { Tournage.Log("plan 5 : donjon absent"); yield break; }
            int ic = -1;
            for (int i = 0; i < gen.Butins.Length; i++)
                if (gen.Butins[i] != null && gen.Butins[i].butin != Deathless.Donjon.TypeButin.TasOr && !dj.ButinPris(i)) { ic = i; break; }
            if (ic < 0) { Tournage.Log("plan 5 : aucun coffre libre"); yield break; }
            var coffre = gen.Butins[ic];
            Vector3 c = coffre.transform.position;
            Vector3 f = coffre.visuel != null ? coffre.visuel.transform.forward : coffre.transform.forward;
            f = Plat(f).normalized; if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            Vector3 ph = SurNavMesh(c + f * P5Devant, 1.2f);
            Placer(h, ph, c);
            SansGardiens();
            // Tas d'or le plus proche : la marionnette (rôdeur) le ramasse.
            int it = -1; float dt = 9f;
            for (int i = 0; i < gen.Butins.Length; i++)
            {
                var r = gen.Butins[i];
                if (r == null || r.butin != Deathless.Donjon.TypeButin.TasOr || dj.ButinPris(i) || r.visuel == null || !r.visuel.activeSelf) continue;
                float d = Vector3.Distance(r.transform.position, c);
                if (d < dt && Mathf.Abs(r.transform.position.y - c.y) < 1.5f) { dt = d; it = i; }
            }
            Heros m = null; Vector3 tas = Vector3.zero;
            if (it >= 0)
            {
                tas = gen.Butins[it].transform.position;
                Vector3 vers = Plat(c - tas).normalized;
                m = Marionnette("rodeur", SurNavMesh(tas + vers * 2.2f, 1.5f), tas, "Lysa");
            }
            else
            {
                // Mise en place (repli) : aucun tas d'or près du coffre dans ce donjon ; on en rapproche un (repère et visuel).
                for (int i = 0; i < gen.Butins.Length && it < 0; i++)
                {
                    var r = gen.Butins[i];
                    if (r == null || r.butin != Deathless.Donjon.TypeButin.TasOr || dj.ButinPris(i) || r.visuel == null) continue;
                    Vector3 cible = SurNavMesh(Repere(c, f, P5Tas), 2f);
                    Vector3 dep = cible - r.transform.position;
                    bool enfant = r.visuel.transform.IsChildOf(r.transform);
                    r.transform.position += dep;
                    if (!enfant) r.visuel.transform.position += dep;
                    r.visuel.SetActive(true);
                    it = i;
                    Tournage.Log("plan 5 : tas d'or " + i + " rapproché du coffre (mise en place)");
                }
                if (it >= 0)
                {
                    tas = gen.Butins[it].transform.position;
                    Vector3 vers = Plat(c - tas).normalized;
                    m = Marionnette("rodeur", SurNavMesh(tas - vers * 2.2f + Vector3.Cross(Vector3.up, vers) * 0.6f, 1.5f), tas, "Lysa");
                }
            }
            yield return Attendre(0.6f);
            Hab.Hud(true);
            Vector3 regardA = c + Vector3.up * 0.45f;
            Vector3 regardB = it >= 0 ? Vector3.Lerp(c, tas, 0.4f) + Vector3.up * 0.7f : c + Vector3.up * 0.8f;
            Cam.Mouvement = CameraTournage.Rail(true,
                new CameraTournage.Cle(0f, Repere(c, f, P5CamA), regardA, 46f),
                new CameraTournage.Cle(1.7f, Repere(c, f, P5CamA) + Vector3.up * 0.15f, regardA, 46f),
                new CameraTournage.Cle(4f, Repere(c, f, P5CamB), regardB, 56f));
            Hab.Bas("PILLEZ. RAMENEZ L’OR AU VILLAGE.", 0.25f, 4.01f);
            bool ouvert = false, ramasse = false;
            int orAvant = P.JoueurLocal != null ? P.JoueurLocal.orPorte : 0;
            yield return T.Prise(4f, t =>
            {
                if (!ouvert && t >= P5Ouverture)
                {
                    ouvert = true;
                    Viser(h, c);
                    h.Entrees.SimulerAction("Interact");
                    T.Son("sting_or", t + 0.08f, 0.55f, true);
                }
                if (m != null && !ramasse && t >= 1.3f)
                {
                    Marcher(m, tas - m.transform.position, 0.8f);
                    if (Vector3.Distance(Plat(m.transform.position), Plat(tas)) < B.rayonTasOr)
                    {
                        ramasse = true;
                        Arreter(m);
                        // Même effet que DonjonJeu.Ouvrir pour un tas d'or ; l'or va dans le HUD du joueur local (repli :
                        // une marionnette n'est pas un joueur de la partie).
                        var r = gen.Butins[it];
                        if (r.visuel != null) r.visuel.SetActive(false);
                        PieceOr.Jouer(tas + Vector3.up * 0.8f);
                        AudioBank.Jouer(SonsDuJeu.Or, tas + Vector3.up * 0.8f, 0.6f);
                        if (P.JoueurLocal != null) P.JoueurLocal.orPorte += dj.MontantButin(it);
                        T.Son("pieces", t, 0.5f, true);
                    }
                }
            });
            if (m != null) Arreter(m);
            Tournage.Log("plan 5 : or porté " + orAvant + " → " + (P.JoueurLocal != null ? P.JoueurLocal.orPorte : 0));
        }

        // ================================================================== Plan 6 : tournée générale (taverne)

        IEnumerator Plan6()
        {
            var h = H;
            var ancre = GameObject.Find("VillageBlockout/Interieurs/Interieur_Taverne/Ancre_Echange_Taverne");
            if (ancre == null) { Tournage.Log("plan 6 : taverne introuvable"); yield break; }
            Vector3 a = ancre.transform.position; a.y = 0f;
            Vector3 f = Plat(ancre.transform.forward).normalized;
            Vector3 r = Vector3.Cross(Vector3.up, f);
            var dj = DonjonJeu.Instance;
            if (dj != null && P.JoueurLocal != null && P.JoueurLocal.orPorte > 0) dj.Deposer(h.Id);
            Placer(h, SurNavMesh(a + f * P6Rang + r * 0.1f, 0.8f), a);
            var clients = new List<Heros>();
            if (m_Marionnettes.Count > 0) Nettoyer();
            clients.Add(Marionnette("viking", SurNavMesh(a + f * P6Rang + r * P6Ecart, 0.8f), a, "Morgane"));
            clients.Add(Marionnette("rodeur", SurNavMesh(a + f * P6Rang - r * P6Ecart, 0.8f), a, "Lysa"));
            yield return Attendre(0.8f);   // retour du donjon : ambiance du village rétablie
            Hab.Hud(true);
            Vector3 regard = Repere(a, f, P6Regard);
            Vector3 camPos = Repere(a, f, new Vector3(P6Cam.x, P6Cam.y, P6Cam.z));
            if (P6CameraJeu)
            {
                // Vue du joueur : la caméra à l'épaule du jeu, qui tangue d'elle-même (Ivresse) ; roulis appuyé en plus.
                if (P.cameraJeu != null) { P.cameraJeu.lacet = h.transform.eulerAngles.y; P.cameraJeu.tangage = P6Tangage; }
                Cam.Mouvement = CameraTournage.CameraJeu(P.cameraJeu != null ? P.cameraJeu.GetComponent<Camera>() : null);
                Cam.Roulis = t => Ivresse.Roulis * (P6RoulisFacteur - 1f);
            }
            else
            {
                Cam.Mouvement = t => CameraTournage.Pose.Regard(camPos + r * (Ivresse.Balancement * P6RoulisFacteur), regard, P6Champ);
                Cam.Roulis = t => Ivresse.Roulis * P6RoulisFacteur;
            }
            Hab.Bas("TOURNÉE GÉNÉRALE.", 0.45f, 3.01f, "tr-bas--petit");
            bool paye = false;
            P.Etat.orEquipe = Mathf.Max(P.Etat.orEquipe, 150);
            bool emotes = false;
            Liberer(h); Arreter(h);
            foreach (var c in clients) Arreter(c);
            yield return T.Prise(3f, t =>
            {
                if (!paye && t >= 0.25f)
                {
                    paye = true;
                    string msg = P.PayerTaverne(Taverne.Article.Tournee, h.Id, out bool ok);
                    Tournage.Log("plan 6 : " + msg);
                    T.Son("hoquet", 1.25f, 0.55f, true);
                }
                if (!emotes && t >= 0.45f)
                {
                    emotes = true;
                    h.Emotes.Lancer(EmotesHeros.Acclamation);
                    if (clients.Count > 0 && clients[0] != null) clients[0].Emotes.Lancer(EmotesHeros.Acclamation);
                    if (clients.Count > 1 && clients[1] != null) clients[1].Emotes.Lancer(EmotesHeros.Boire);
                }
                // Tous face au comptoir (la caméra est derrière).
                foreach (var hh in new[] { h, clients.Count > 0 ? clients[0] : null, clients.Count > 1 ? clients[1] : null })
                    if (hh != null) hh.transform.rotation = Quaternion.LookRotation(-f);
            });
            Arreter(h);
            foreach (var c in clients) Arreter(c);
        }

        // ================================================================== Plan 7 : emotes sur la place

        IEnumerator Plan7()
        {
            var h = H;
            Nettoyer();
            Ivresse.Arreter();
            if (h.Emotes != null) h.Emotes.Interrompre(true);
            Placer(h, SurNavMesh(P7Heros), P7Cam);
            var autre = Marionnette("assassin", SurNavMesh(P7Autre), P7Cam, "Tibo");
            Liberer(h); Arreter(h);
            yield return Attendre(0.8f);
            Hab.Hud(true);
            Cam.Mouvement = CameraTournage.Rail(true, new CameraTournage.Cle(0f, P7Cam, P7Regard, P7Champ), new CameraTournage.Cle(3f, P7Cam + new Vector3(0f, -0.05f, 0.35f), P7Regard, P7Champ - 1f));
            Hab.Bas("L’HUMOUR EST UNE ARME.", 0.3f, 3.01f);
            bool roue = false, bu = false, mort = false;
            T.Son("gloups", P7Gloups, 0.8f, true);
            T.Son("rire", P7Rire, 0.6f, true);
            yield return T.Prise(3f, t =>
            {
                if (!roue && t >= 0.05f) { roue = true; h.Emotes.TesterRoue(new Vector2(-0.3f, 0f)); }
                if (roue && !bu && t < 0.8f) h.Emotes.TesterRoue(new Vector2(Mathf.Lerp(-0.3f, -1f, Mathf.Clamp01((t - 0.05f) / 0.4f)), 0.05f));
                if (!mort && t >= 0.2f && autre != null) { mort = true; autre.Emotes.Lancer(EmotesHeros.FaireLeMort); }
                if (!bu && t >= 0.8f) { bu = true; h.Emotes.TesterRoue(null); bool ok = h.Emotes.Lancer(EmotesHeros.Boire); if (!ok) Tournage.Log("plan 7 : Boire refusé"); }
                if (t >= 2.96f) Mix.GainMaitre(0f, 0.01f);   // silence d'une image avant le crépuscule
            });
        }

        // ================================================================== Plan 8 : la nuit arrive (crépuscule)

        IEnumerator Plan8()
        {
            var h = H;
            Nettoyer();
            Hab.Hud(false);
            Liberer(h);
            Placer(h, SurNavMesh(new Vector3(-12f, 0f, -21f)), Vector3.zero);
            yield return Attendre(0.3f);
            m_FigerPhase = false;
            P.Etat.tempsPhase = P.Etat.dureePhase - 0.05f;
            float t0 = Time.realtimeSinceStartup;
            while (P.Etat.phase != Phase.Crepuscule && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            if (P8Decalage > 0f) yield return Attendre(P8Decalage);
            Cam.Mouvement = CameraTournage.Rail(false, new CameraTournage.Cle(0f, P8CamA, P8Regard, 44f), new CameraTournage.Cle(2f, P8CamB, P8Regard, 40f));
            Hab.Bas("LA NUIT ARRIVE.", 0.1f, 2.01f);
            Mix.GainMaitre(1f, 0.005f);
            Mix.Couper("musique_jour", 0.03f);
            T.Son("braam", 0f, 0.8f, true);
            if (!T.apercu) Mix.JouerCatalogue(SonsDuJeu.TombeeNuit, T.Video(0f), 0.9f);
            yield return T.Prise(2f, null);
        }

        // ================================================================== Plan 9 : assaut nocturne, Nyxessa riposte

        IEnumerator Plan9()
        {
            var h = H;
            Hab.Hud(false);
            float t0 = Time.realtimeSinceStartup;
            while (P.Etat.phase != Phase.Nuit && Time.realtimeSinceStartup - t0 < 12f) yield return null;
            m_TempsFige = Mathf.Max(P.Etat.tempsPhase, 1f);
            DefenseActive(true);
            var e = P.Etat.nyxessa;
            e.palierMissiles = 5;
            e.stock = GameBalance.AuPalier(B.missilesStockPaliers, 5);
            Placer(h, SurNavMesh(new Vector3(2.5f, 0f, -5.5f)), new Vector3(8f, 0f, -16f));
            // Assaillants posés autour de la place (les vagues réelles arrivent de loin, trop tard pour le plan).
            var poses = new List<Squelette>();
            var necro = Poser(TypeEnnemi.Necromancien, new Vector3(-13f, 0f, -12f), Vector3.zero);
            poses.Add(Poser(TypeEnnemi.Guerrier, new Vector3(8f, 0f, -12f), Vector3.zero));
            poses.Add(Poser(TypeEnnemi.Sbire, new Vector3(-12f, 0f, 5f), Vector3.zero));
            poses.Add(Poser(TypeEnnemi.Sbire, new Vector3(12f, 0f, -3f), Vector3.zero));
            poses.Add(Poser(TypeEnnemi.Guerrier, new Vector3(3f, 0f, 12f), Vector3.zero));
            poses.Add(Poser(TypeEnnemi.Sbire, new Vector3(-6f, 0f, 12f), Vector3.zero));
            poses.Add(Poser(TypeEnnemi.Sbire, new Vector3(10f, 0f, 7f), Vector3.zero));
            yield return Attendre(P9Attente);
            e.stock = GameBalance.AuPalier(B.missilesStockPaliers, 5);
            Cam.Mouvement = CameraTournage.Orbite(() => Vector3.zero, P9Rayon, P9Hauteur, P9Angle, P9Vitesse, new Vector3(0f, 3.0f, 0f), P9Champ);
            Hab.Bas("DÉFENDEZ NYXESSA.", 0.2f, 4.01f);
            Mix.GainBus(MixeurTrailer.Bus.Jeu, 0.55f, 0.05f);
            T.Son("pulsation", 0f, 0.38f, false, MixeurTrailer.Bus.Musique);
            T.Son("musique_nuit", 0f, 0.3f, false, MixeurTrailer.Bus.Musique);
            Vector3[] renforts = { new Vector3(-8f, 0f, -4f), new Vector3(6f, 0f, 8f), new Vector3(9f, 0f, 1f), new Vector3(-3f, 0f, -9f), new Vector3(-9f, 0f, 3f) };
            int posesEnPrise = 0; float prochainTir = 0.3f;
            yield return T.Prise(4f, t =>
            {
                if (posesEnPrise < renforts.Length && t >= 0.25f + posesEnPrise * 0.55f)
                    poses.Add(Poser(posesEnPrise % 2 == 0 ? TypeEnnemi.Sbire : TypeEnnemi.Guerrier, renforts[posesEnPrise++], Vector3.zero));
                if (t >= prochainTir)
                {
                    prochainTir = t + 0.8f;
                    Squelette cible = null; float dmin = 20f;
                    foreach (var s in DirecteurVagues.Instance.Vivants)
                    {
                        if (s == null || !s.Vivant || s.EtatCourant == Squelette.Etat.SortieDeTerre) continue;
                        float d = Vector3.Distance(s.transform.position, h.transform.position);
                        if (d < dmin) { dmin = d; cible = s; }
                    }
                    if (cible != null) { Viser(h, cible.transform.position); h.Entrees.SimulerAction("AttackPrimary"); }
                }
            });
            Mix.Couper("pulsation", 0.25f);
            Mix.GainBus(MixeurTrailer.Bus.Jeu, 1f, 0.1f);
            DefenseActive(false);
        }

        // ================================================================== Plan 10 : parade parfaite (paladin)

        IEnumerator Plan10()
        {
            var h = H;
            var pal = h != null ? h.Classe as ClassePaladin : null;
            if (pal == null || pal.Parade == null) { Tournage.Log("plan 10 : pas de paladin"); yield break; }
            Hab.Hud(false);
            // Le paladin doit pouvoir parer : pas d'invulnérabilité (l'esquive l'intercepterait avant la classe).
            h.Invulnerable(0f);
            bool reussi = false;
            for (int essai = 0; essai < 4 && !reussi; essai++)
            {
                bool enregistre = essai > 0 || false;
                // Essai à blanc d'abord (non enregistré) : la même séquence doit donner une parade parfaite.
                yield return Parade(h, pal, essai >= 1, r => reussi = r);
                if (essai == 0) { Tournage.Log("plan 10 : essai à blanc " + (reussi ? "parfait" : "raté")); reussi = false; }
                else break;
            }
            Mix.Couper("musique_nuit", 0.3f);
            Mix.Couper("boom_a", 0.35f);
        }

        IEnumerator Parade(Heros h, ClassePaladin pal, bool enregistrer, System.Action<bool> resultat)
        {
            h.Sante.Remplir();
            Placer(h, SurNavMesh(P10Heros), P10Regard);
            yield return Attendre(0.3f);
            var g = Poser(TypeEnnemi.Guerrier, h.transform.position + h.transform.forward * 2.2f, h.transform.position);
            if (g == null) { resultat(false); yield break; }
            h.Entrees.DeplacementTest = Vector2.zero;
            h.Entrees.GardeTest = false;
            TelegraphieCoups.Coup c = default;
            float t = 0f;
            while (t < 10f && !(TelegraphieCoups.Prochain(h, out c, 0f) && c.source == g))
            {
                t += Time.deltaTime;
                if (g != null) Viser(h, g.transform.position);
                yield return null;
            }
            if (g == null || c.source != g) { Tournage.Log("plan 10 : aucun coup annoncé"); resultat(false); yield break; }
            int parfaitesAvant = pal.Parade.Parfaites;
            bool garde = false; float tImpactPlan = -1f;
            if (!enregistrer)
            {
                while (c.impact - Time.time > P10Avance) yield return null;
                h.Entrees.GardeTest = true;
                h.Entrees.SimulerAction("AttackSecondary");
                yield return new WaitForSeconds(0.6f);
                h.Entrees.GardeTest = false;
                resultat(pal.Parade.Parfaites > parfaitesAvant);
                if (g != null) g.Desintegrer(true);
                m_Poses.Remove(g);
                yield return Attendre(0.6f);
                yield break;
            }
            Hab.Hud(true);
            Vector3 ph = h.transform.position, f = Plat(g.transform.position - ph).normalized;
            Vector3 camA = Repere(ph, f, P10Cam), regard = ph + f * 1.1f + Vector3.up * 1.2f;
            Cam.Mouvement = CameraTournage.Rail(true, new CameraTournage.Cle(0f, camA, regard, P10Champ),
                new CameraTournage.Cle(3f, Repere(ph, f, P10Cam * 0.85f), regard, P10Champ - 3f));
            Hab.Bas("UNE SECONDE DE TROP ET C’EST FINI.", 0.15f, 3.01f);
            bool silence = false, boum = false;
            yield return T.Prise(3f, tp =>
            {
                float reste = c.impact - Time.time;
                if (!silence && reste < 0.32f) { silence = true; T.Ralenti(0.5f); Mix.GainMaitre(0.08f, 0.12f); }
                if (!garde && reste <= P10Avance) { garde = true; h.Entrees.GardeTest = true; h.Entrees.SimulerAction("AttackSecondary"); }
                if (garde && !boum && (pal.Parade.Parfaites > parfaitesAvant || reste < -0.05f))
                {
                    boum = true; tImpactPlan = tp;
                    Mix.GainMaitre(1f, 0.01f);
                    T.Son("boom_a", tp, 0.9f, true);
                    Cam.Secouer(tp, 0.03f, 0.3f);
                    Tournage.Log("plan 10 : " + (pal.Parade.Parfaites > parfaitesAvant ? "parade parfaite" : "parade " + pal.Parade.Resultat) + " à " + tp.ToString("0.00") + " s");
                }
                if (boum && tp > tImpactPlan + 0.9f) T.Ralenti(1f);
            });
            T.Ralenti(1f);
            h.Entrees.GardeTest = false;
            resultat(pal.Parade.Parfaites > parfaitesAvant);
        }

        // ================================================================== Plan 11 : Morgrim, le Roi des os

        IEnumerator Plan11()
        {
            var h = H;
            Hab.Hud(false);
            var dv = DirecteurVagues.Instance;
            var champ = typeof(DirecteurVagues).GetField("m_PrefabMorgrimChoisi", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dv == null || dv.prefabMorgrimMassue == null || champ == null) { Tournage.Log("plan 11 : Morgrim massue indisponible"); yield break; }
            champ.SetValue(dv, dv.prefabMorgrimMassue);
            Placer(h, SurNavMesh(P11Heros), P11Regard);
            yield return Attendre(0.3f);
            Vector3 avant = Plat(P11Regard - P11Heros).normalized;
            var m = Poser(TypeEnnemi.Golem, h.transform.position + avant * P11Distance, h.transform.position);
            if (m == null) { Tournage.Log("plan 11 : Morgrim non posé"); yield break; }
            yield return Sortis(new[] { m }, 8f);
            m_Sauteur = true;
            T.StartCoroutine(Sauteur(h));
            // Essai à blanc : la première attaque (non enregistrée) vérifie le saut ; la suivante est tournée.
            bool reussi = false;
            for (int essai = 0; essai < 4; essai++)
            {
                bool enr = essai >= 1;
                bool ok = false;
                yield return AttaqueMorgrim(h, m, enr, r => ok = r);
                if (!enr) { Tournage.Log("plan 11 : essai à blanc " + (ok ? "saut réussi" : "touché")); continue; }
                reussi = ok;
                break;
            }
            if (!reussi) Tournage.Log("plan 11 : saut raté pendant la prise");
            m_Sauteur = false;
            m_Poses.Remove(m);
            if (m != null) m.Desintegrer(true);
        }

        bool m_Sauteur;

        /// Le héros saute par-dessus chaque onde du Fracas (0,36 s avant que le front n'arrive sous ses pieds).
        IEnumerator Sauteur(Heros h)
        {
            var vues = new HashSet<int>();
            while (m_Sauteur && h != null)
            {
                foreach (var o in Object.FindObjectsByType<OndeChocLente>(FindObjectsSortMode.None))
                {
                    int id = o.GetInstanceID();
                    if (vues.Contains(id)) continue;
                    vues.Add(id);
                    T.StartCoroutine(SauterOnde(h, o.transform.position, Time.time));
                }
                yield return null;
            }
        }

        IEnumerator SauterOnde(Heros h, Vector3 centre, float depart)
        {
            while (h != null)
            {
                float d = Vector3.Distance(Plat(h.transform.position), Plat(centre));
                float arrivee = d / Mathf.Max(0.1f, B.morgrimMassueFracasOndeVitesse) - (Time.time - depart);
                if (arrivee < -0.5f) yield break;
                if (arrivee <= P11AvanceSaut)
                {
                    h.Entrees.SimulerAction("Jump");
                    Tournage.Log("plan 11 : saut (front dans " + arrivee.ToString("0.00") + " s)");
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator AttaqueMorgrim(Heros h, Squelette m, bool enregistrer, System.Action<bool> resultat)
        {
            // Héros à portée du Fracas (pas de Charge) ; attend le début de la préparation.
            Placer(h, SurNavMesh(P11Heros), m.transform.position);
            h.Sante.Remplir();
            float t = 0f;
            while (m != null && m.EtatCourant == Squelette.Etat.Preparation && t < 6f) { t += Time.deltaTime; yield return null; }
            t = 0f;
            while (h.EnRenverse && t < 6f) { t += Time.deltaTime; yield return null; }
            t = 0f;
            while (m != null && (m.EtatCourant != Squelette.Etat.Preparation || h.EnRenverse) && t < 12f)
            {
                t += Time.deltaTime;
                Viser(h, m.transform.position);
                if (Vector3.Distance(h.transform.position, m.transform.position) > P11Distance + 0.6f) Placer(h, SurNavMesh(P11Heros), m.transform.position);
                yield return null;
            }
            if (m == null || m.EtatCourant != Squelette.Etat.Preparation) { Tournage.Log("plan 11 : Morgrim n'attaque pas"); resultat(false); yield break; }
            var anciennes = new HashSet<int>();
            foreach (var o in Object.FindObjectsByType<OndeChocLente>(FindObjectsSortMode.None)) anciennes.Add(o.GetInstanceID());
            GameObject onde = null; float tOnde = -1f; bool saute = false, renverse = false, passage = false;
            Vector3 centre = Vector3.zero;
            float tSaut = -1f, tAtterri = -1f;
            System.Action<float> chaque = tp =>
            {
                if (onde == null)
                {
                    foreach (var o in Object.FindObjectsByType<OndeChocLente>(FindObjectsSortMode.None))
                    {
                        if (anciennes.Contains(o.GetInstanceID())) continue;
                        onde = o.gameObject; centre = o.transform.position; tOnde = Time.time; break;
                    }
                    if (onde != null && enregistrer) { T.Son("impact_lourd", tp, 0.65f, true); T.Son("boom_c", tp, 0.55f, true); Cam.Secouer(tp, 0.05f, 0.45f); }
                }
                if (onde != null && !saute && !h.AuSol)
                {
                    saute = true; tSaut = tp;
                    if (enregistrer) { Mix.GainMaitre(0.1f, 0.03f); Mix.Couper("boom_a", 0.05f); Mix.Couper("pulsation", 0.05f); }
                }
                if (saute && tAtterri < 0f && h.AuSol && tp > tSaut + 0.2f)
                {
                    tAtterri = tp;
                    if (enregistrer) { Mix.GainMaitre(1f, 0.01f); T.Son("boom_b", tp, 0.75f, true); }
                }
                if (h.EnRenverse && !renverse) { renverse = true; Tournage.Log("plan 11 : renversé à " + tp.ToString("0.00") + " s (onde " + (onde != null ? "vue" : "absente") + ")"); }
                if (onde != null)
                {
                    float d = Vector3.Distance(Plat(h.transform.position), Plat(centre));
                    float front = (Time.time - tOnde) * B.morgrimMassueFracasOndeVitesse;
                    if (Mathf.Abs(d - front) < B.morgrimMassueFracasOndeLargeurBande * 0.5f && !passage) { passage = true; Tournage.Log("plan 11 : front sous les pieds à " + tp.ToString("0.00") + " s, au sol " + h.AuSol + ", d " + d.ToString("0.00")); }
                }
            };
            if (!enregistrer)
            {
                float tt = 0f;
                while (tt < 4f) { chaque(tt); tt += Time.deltaTime; yield return null; }
                resultat(saute && !renverse);
                while (h.EnRenverse) yield return null;
                yield return Attendre(0.5f);
                yield break;
            }
            Vector3 mid = (h.transform.position + m.transform.position) * 0.5f;
            Vector3 f = Plat(m.transform.position - h.transform.position).normalized;
            Vector3 camA = Repere(mid, f, P11Cam), camB = Repere(mid, f, P11Cam + new Vector3(0f, 0.05f, 1.6f));
            Cam.Mouvement = CameraTournage.Rail(false,
                new CameraTournage.Cle(0f, camA, mid + Vector3.up * 1.9f, P11Champ),
                new CameraTournage.Cle(4f, camB, mid + Vector3.up * 1.9f + f * 0.4f, P11Champ));
            Hab.Bas("MORGRIM, LE ROI DES OS.", 0.2f, 4.01f);
            T.Son("boom_a", 0f, 0.35f, true);
            T.Son("pulsation", 0f, 0.3f, false, MixeurTrailer.Bus.Musique);
            yield return T.Prise(4f, tp => chaque(tp));
            Mix.Couper("pulsation", 0.1f);
            Mix.GainMaitre(1f, 0.01f);
            Tournage.Log("plan 11 : saut " + (saute ? "à " + tSaut.ToString("0.00") + " s" : "absent") + ", atterrissage " + tAtterri.ToString("0.00") + (renverse ? ", RENVERSÉ" : ""));
            resultat(saute && !renverse);
        }

        // ================================================================== Plan 12 : écran final

        IEnumerator Plan12()
        {
            var h = H;
            Hab.Hud(false);
            foreach (var s in new List<Squelette>(DirecteurVagues.Instance != null ? DirecteurVagues.Instance.Vivants : new List<Squelette>())) if (s != null) s.Desintegrer(true);
            Placer(h, SurNavMesh(new Vector3(-12f, 0f, 12f)), Vector3.zero);
            yield return Attendre(0.8f);
            Vector3 c = Nyx;
            Cam.Mouvement = CameraTournage.Orbite(() => c, P12Rayon, P12Hauteur, P12Angle, P12Vitesse, new Vector3(0f, -0.9f, 0f), P12Champ, P12Rayon * 0.92f);
            Hab.Voile(t => Mathf.Lerp(0f, 0.45f, Mathf.Clamp01((t - 0.1f) / 0.5f)));
            Hab.Final(0.3f);
            T.NoirTotal = t => Mathf.Clamp01((t - 3.45f) / 0.5f);
            foreach (var nom in new[] { "boom_b", "boom_c", "impact_lourd" }) Mix.Couper(nom, 0.3f);
            T.Son("accord", 0.3f, 1f, true);
            T.Son("braam_final", 0.3f, 0.9f, true);
            T.Son("boom_b", 0.3f, 0.55f, true);
            T.Son("nappe", 0.5f, 0.35f, false, MixeurTrailer.Bus.Musique);
            bool fondu = false;
            yield return T.Prise(4f, t =>
            {
                if (!fondu && t >= 3.4f) { fondu = true; Mix.GainMaitre(0f, 0.55f); }
            });
        }
    }
}
#endif
