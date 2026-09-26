using System;
using Deathless.Jeu;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Tests du réseau sans fenêtre (Docs/reseau.md) : un poste piloté par la ligne de commande, typiquement un client
    /// construit lancé en -batchmode -nographics. Rejoint un salon (adresse IP ou code), prend une classe, se déclare
    /// prêt, puis, en partie, fait marcher, sprinter, sauter, esquiver et attaquer son héros en rond ; il journalise ce
    /// qu'il voit (salon, héros des autres et leur position) et quitte au bout de la durée donnée.
    ///   -deathless-rejoindre=127.0.0.1[:7777] | -deathless-code=ABC123 | -deathless-heberger
    ///   -deathless-pseudo=Bot -deathless-classe=mage -deathless-duree=60 [-deathless-profil=client2] [-deathless-direct]
    ///   -deathless-attendre=2 : ne se déclare prêt qu'une fois 2 joueurs dans le salon (hôte construit)
    ///   -deathless-or=250 : hôte ou solo, caisse commune remplie au lancement (test des achats)
    ///   -deathless-taverne : paie une tournée à la taverne (l'hôte décide, tous ivres)
    ///   -deathless-achat : achète le palier 2 des missiles à la relique (l'hôte décide)
    ///   -deathless-donjon=retour|rester : entre au donjon par le portail, prend un tas d'or et un coffre, puis revient
    ///     par le portail de retour (or versé à la caisse) ou reste (rappel par Nyxessa au crépuscule)
    ///   -deathless-capture=dossier : avec -deathless-donjon, images PNG de la caméra du jeu aux étapes des portails
    ///     (rendues hors écran : en -batchmode sans -nographics, rien ne s'affiche et aucune fenêtre ne prend le focus)
    ///   -deathless-competences : le héros enchaîne toutes ses compétences (effets vus par les autres postes)
    ///   -deathless-solo : partie solo lancée aussitôt avec la classe donnée (vérification du build : caméra, héros)
    ///   [-deathless-quitter-salon=5] : quitte le salon 5 s après y être entré (test de la classe libérée), sans se déclarer prêt
    public class ClientAutomatique : MonoBehaviour
    {
        string m_Adresse, m_Code, m_Classe;
        bool m_Heberger, m_Solo;
        float m_Duree = 60f;
        float m_QuitterSalon = -1f, m_DansSalon;
        float m_Depuis, m_EnPartieDepuis = -1f, m_EnErreur, m_ProchainJournal, m_ProchaineAction;
        bool m_Demande, m_ClasseDemandee, m_PretDemande;
        int m_Action;
        EtatLobby m_EtatVu = (EtatLobby)(-1);

        static string Arg(string nom)
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.StartsWith("-" + nom + "=")) return a.Substring(nom.Length + 2);
            return null;
        }

        static bool Drapeau(string nom) => Array.IndexOf(Environment.GetCommandLineArgs(), "-" + nom) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Demarrer()
        {
            var adresse = Arg("deathless-rejoindre");
            var code = Arg("deathless-code");
            bool heberger = Drapeau("deathless-heberger");
            bool solo = Drapeau("deathless-solo");
            if (adresse == null && code == null && !heberger && !solo) return;
            var go = new GameObject("ClientAutomatique");
            DontDestroyOnLoad(go);
            var c = go.AddComponent<ClientAutomatique>();
            c.m_Adresse = adresse;
            c.m_Code = code;
            c.m_Heberger = heberger;
            c.m_Solo = solo;
            c.m_Competences = Drapeau("deathless-competences");
            c.m_Achat = Drapeau("deathless-achat") || Drapeau("deathless-taverne");
            c.m_Taverne = Drapeau("deathless-taverne");
            c.m_Donjon = Arg("deathless-donjon");
            c.m_DossierCaptures = Arg("deathless-capture");
            if (int.TryParse(Arg("deathless-or"), out var or)) c.m_Or = or;
            if (int.TryParse(Arg("deathless-attendre"), out var att)) c.m_Attendre = att;
            c.m_Classe = Arg("deathless-classe") ?? "mage";
            LobbyReseau.PseudoForce = Arg("deathless-pseudo") ?? "Bot";
            LobbyReseau.ClasseForcee = c.m_Classe;
            if (float.TryParse(Arg("deathless-quitter-salon"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var q)) c.m_QuitterSalon = q;
            if (float.TryParse(Arg("deathless-duree"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) c.m_Duree = d;
            Application.runInBackground = true;
            ReseauJeu.Journal("client automatique : " + (solo ? "solo" : heberger ? "héberge" : adresse != null ? "rejoint " + adresse : "rejoint le code " + code)
                + ", pseudo " + LobbyReseau.PseudoForce + ", classe " + c.m_Classe + ", " + c.m_Duree + " s");
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            m_Depuis += dt;
            var r = ReseauJeu.Instance;
            var lobby = r != null ? r.Lobby : null;
            if (lobby == null) return;

            if (!m_Demande && m_Depuis > 1f)
            {
                m_Demande = true;
                if (m_Solo) ClassesJouables.Lancer(m_Classe);
                else if (m_Heberger) lobby.CreerSalon();
                else if (m_Adresse != null) lobby.RejoindreParAdresse(m_Adresse);
                else lobby.Rejoindre(m_Code);
            }
            if (lobby.Etat != m_EtatVu)
            {
                m_EtatVu = lobby.Etat;
                ReseauJeu.Journal("[auto] lobby : " + m_EtatVu + (string.IsNullOrEmpty(lobby.Message) ? "" : " (" + lobby.Message + ")")
                    + (string.IsNullOrEmpty(lobby.CodeSalon) ? "" : " code " + lobby.CodeSalon));
            }

            // Hôte parti ou connexion perdue : on ne reste pas bloqué (le poste de test se ferme au bout de 8 s d'erreur).
            m_EnErreur = lobby.Etat == EtatLobby.Erreur ? m_EnErreur + dt : 0f;
            if (m_EnErreur > 8f) { ReseauJeu.Journal("[auto] lobby en erreur (" + lobby.Message + "), on quitte"); enabled = false; Invoke(nameof(Fermer), 1f); return; }

            var salon = SalonReseau.Instance;
            if (lobby.Etat == EtatLobby.Salon && salon != null)
            {
                var moi = Moi(lobby);
                m_DansSalon += dt;
                if (m_QuitterSalon >= 0f && m_DansSalon >= m_QuitterSalon)
                {
                    ReseauJeu.Journal("[auto] quitte le salon (test)");
                    enabled = false;
                    lobby.Quitter();
                    Invoke(nameof(Fermer), 3f);
                    return;
                }
                if (moi != null && moi.ClasseId != m_Classe && !m_ClasseDemandee)
                {
                    m_ClasseDemandee = true;
                    bool ok = lobby.ChoisirClasse(m_Classe);
                    ReseauJeu.Journal("[auto] demande la classe " + m_Classe + (ok ? "" : " (prise par " + LobbyOutils.PrisePar(lobby, m_Classe) + ")"));
                }
                if (m_QuitterSalon < 0f && moi != null && !moi.Pret && lobby.Joueurs.Count >= m_Attendre && !m_PretDemande && !string.IsNullOrEmpty(moi.ClasseId) && (moi.ClasseId == m_Classe || m_ClasseDemandee))
                {
                    m_PretDemande = true;
                    lobby.BasculerPret();
                    ReseauJeu.Journal("[auto] prêt (" + moi.Pseudo + ", " + moi.ClasseId + ")");
                }
            }

            var p = Partie.Instance;
            if (p != null && p.EnCours && p.HerosLocal != null)
            {
                if (m_EnPartieDepuis < 0f)
                {
                    m_EnPartieDepuis = 0f;
                    ReseauJeu.Journal("[auto] en partie avec " + p.HerosLocal.name);
                    if (m_Or > 0 && ReseauJeu.Autorite) { p.Etat.orEquipe = m_Or; ReseauJeu.Journal("[auto] caisse commune : " + m_Or + " or (test)"); }
                }
                m_EnPartieDepuis += dt;
                if (m_Donjon != null && Donjon(p, p.HerosLocal, m_EnPartieDepuis)) { }
                else if (m_Achat && Achat(p, m_EnPartieDepuis)) { }
                else Piloter(p.HerosLocal, m_EnPartieDepuis);
                if (m_EnPartieDepuis >= m_Duree)
                {
                    ReseauJeu.Journal("[auto] fin du test, on quitte");
                    enabled = false;
                    p.QuitterPartie();
                    Invoke(nameof(Fermer), 1.5f);
                    return;
                }
            }
            if (Time.unscaledTime >= m_ProchainJournal)
            {
                m_ProchainJournal = Time.unscaledTime + 2f;
                Journaliser(lobby, p);
            }
            if (m_EnPartieDepuis < 0f && m_Depuis > m_Duree + 60f) { ReseauJeu.Journal("[auto] la partie n'a pas commencé, abandon"); enabled = false; lobby.Quitter(); Invoke(nameof(Fermer), 1.5f); }
        }

        void Fermer()
        {
#if UNITY_EDITOR
            // Éditeur en -batchmode (client de test) : on ferme l'éditeur ; éditeur interactif : on s'arrête seulement.
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
            else Destroy(gameObject);
#else
            Application.Quit();
#endif
        }

        static IJoueurLobby Moi(ILobby l)
        {
            foreach (var j in l.Joueurs) if (j.EstLocal) return j;
            return null;
        }

        /// En rond (un tour en 12 s), sprint une seconde sur trois ; toutes les 1,5 s : saut, esquive, attaque, à tour de rôle.
        bool m_PretEnvoye;
        int m_PhaseVue = -1;

        void Piloter(Heros h, float t)
        {
            var p = Partie.Instance;
            // Changement de phase : journal ; le jour, un vote « prêt » (une fois par jour).
            if ((int)p.Etat.phase != m_PhaseVue)
            {
                m_PhaseVue = (int)p.Etat.phase;
                m_PretEnvoye = false;
                ReseauJeu.Journal("[auto] phase " + p.Etat.phase + " nuit " + p.Etat.nuit);
            }
            if (p.Etat.phase == Phase.Jour && !m_PretEnvoye && t > 3f) { m_PretEnvoye = true; h.Entrees.SimulerAction("Ready"); ReseauJeu.Journal("[auto] vote prêt"); }
            // Un squelette à portée : on va vers lui, on le vise (caméra) et on attaque.
            Squelette cible = null; float dmin = 30f;
            var dv = DirecteurVagues.Instance;
            if (dv != null) foreach (var s in dv.Vivants) { if (s == null || !s.Vivant || s.Sante.Mort) continue; float d = Vector3.Distance(s.transform.position, h.transform.position); if (d < dmin) { dmin = d; cible = s; } }
            if (cible != null && h.CameraEpaule != null)
            {
                Vector3 v = cible.transform.position - h.transform.position; v.y = 0f;
                h.CameraEpaule.lacet = Quaternion.LookRotation(v).eulerAngles.y;
                h.Entrees.DeplacementTest = dmin > 5f ? new Vector2(0f, 1f) : Vector2.zero;
                h.Entrees.SprintTest = false;
                if (t >= m_ProchaineAction) { m_ProchaineAction = t + 1.0f; h.Entrees.SimulerAction("AttackPrimary"); }
                return;
            }
            float a = t * Mathf.PI * 2f / 12f;
            h.Entrees.DeplacementTest = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            h.Entrees.SprintTest = Mathf.Repeat(t, 3f) < 1f;
            h.Entrees.GardeTest = m_Competences && t < m_GardeJusqua;
            h.Entrees.AttaqueTest = m_Competences && t < m_AttaqueJusqua;
            if (t >= m_ProchaineAction)
            {
                m_ProchaineAction = t + 1.5f;
                if (!m_Competences)
                {
                    string action = (m_Action++ % 3) switch { 0 => "Jump", 1 => "Dodge", _ => "AttackPrimary" };
                    h.Entrees.SimulerAction(action);
                    return;
                }
                // Toutes les compétences, à tour de rôle (jauge remplie : rage, mana), pour les effets vus par les autres.
                if (h.Classe != null) h.Classe.RemplirJauge();
                int k = m_Action++ % 8;
                switch (k)
                {
                    case 0: h.Entrees.SimulerAction("Jump"); break;
                    case 1: h.Entrees.SimulerAction("Dodge"); break;
                    case 2: case 7: h.Entrees.SimulerAction("AttackPrimary"); m_AttaqueJusqua = t + 1.3f; break;
                    case 3: h.Entrees.SimulerAction("Skill1"); m_ProchaineAction = t + 2.2f; break;
                    case 4: h.Entrees.SimulerAction("Skill2"); m_ProchaineAction = t + 2.2f; break;
                    case 5: h.Entrees.SimulerAction("AttackSecondary"); m_GardeJusqua = t + 2.5f; m_ProchaineAction = t + 3f; break;
                    case 6:
                        // Rôdeur : viser (LT) puis bander pendant la visée (RT) et relâcher.
                        if (h.Classe is ClasseRodeur) { m_GardeJusqua = t + 1.6f; m_AttaqueJusqua = t + 1.3f; }
                        else { m_AttaqueJusqua = t + 1.3f; h.Entrees.SimulerAction("AttackPrimary"); }
                        break;
                }
                ReseauJeu.Journal("[auto] compétence " + k);
            }
        }

        bool m_Competences, m_Achat, m_Taverne;

        /// Test de la taverne (-deathless-taverne) : vers 6 s, le héros va au comptoir, ouvre le menu et paie une tournée ;
        /// l'hôte décide, tous les joueurs sont ivres (le journal donne la réponse et l'ivresse).
        bool Taverne(Partie p, Heros h, float t)
        {
            var comptoir = GameObject.Find("VillageBlockout/Interieurs/Interieur_Taverne/Ancre_Echange_Taverne");
            if (m_EtapeAchat == 0 && t > 6f)
            {
                m_EtapeAchat = 1;
                h.Entrees.DeplacementTest = Vector2.zero;
                if (comptoir == null) { ReseauJeu.Journal("[auto] pas de taverne"); m_EtapeAchat = 3; return false; }
                Vector3 pos = comptoir.transform.position + comptoir.transform.forward * 1.1f; pos.y = comptoir.transform.position.y - 0.85f;
                h.Teleporter(pos);
                return true;
            }
            if (m_EtapeAchat == 1 && t > 7.5f)
            {
                m_EtapeAchat = 2;
                PointInteraction.Courant(h, out string invite);
                bool ok = PointInteraction.InteragirIci(h);
                var tv = comptoir != null ? comptoir.GetComponent<Deathless.Jeu.Taverne>() : null;
                ReseauJeu.Journal("[auto] taverne : invite « " + invite + " », menu ouvert : " + ok + ", or " + p.Etat.orEquipe);
                if (tv != null) { tv.Acheter(2); ReseauJeu.Journal("[auto] tournée demandée : " + tv.Message); }
                return true;
            }
            if (m_EtapeAchat == 2 && t > 10f)
            {
                m_EtapeAchat = 3;
                ReseauJeu.Journal("[auto] après la tournée : or " + p.Etat.orEquipe + ", ivresse " + Ivresse.Force.ToString("F2") + ", message « " + (comptoir != null ? comptoir.GetComponent<Deathless.Jeu.Taverne>()?.Message : "") + " »");
            }
            return m_EtapeAchat < 3;
        }
        int m_Or;
        int m_EtapeAchat;

        // ------------------------------------------------------------ Donjon
        string m_Donjon, m_DonjonMessage = "", m_DossierCaptures;
        int m_EtapeDonjon, m_Captures;

        /// Tourne le héros (et sa caméra) vers `direction`.
        static void Regarder(Partie p, Heros h, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            h.transform.rotation = Quaternion.LookRotation(direction);
            if (p.cameraJeu != null && p.cameraJeu.cible == h.transform) p.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        /// -deathless-capture : image de la caméra du jeu (1280 x 720, PNG), rendue dans une texture (pas d'écran en
        /// -batchmode) ; l'interface (UI Toolkit) n'y figure pas.
        void Capturer(string nom)
        {
            if (string.IsNullOrEmpty(m_DossierCaptures)) return;
            var p = Partie.Instance;
            var cam = p != null && p.cameraJeu != null ? p.cameraJeu.GetComponent<Camera>() : Camera.main;
            if (cam == null) { ReseauJeu.Journal("[auto] capture " + nom + " : pas de caméra"); return; }
            try
            {
                const int L = 1280, H = 720;
                var rt = RenderTexture.GetTemporary(L, H, 24, RenderTextureFormat.ARGB32);
                var avant = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = avant;
                var actif = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(L, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, L, H), 0, 0);
                tex.Apply();
                RenderTexture.active = actif;
                RenderTexture.ReleaseTemporary(rt);
                System.IO.Directory.CreateDirectory(m_DossierCaptures);
                string chemin = System.IO.Path.Combine(m_DossierCaptures, nom + ".png");
                System.IO.File.WriteAllBytes(chemin, tex.EncodeToPNG());
                Destroy(tex);
                m_Captures++;
                ReseauJeu.Journal("[auto] capture " + chemin);
            }
            catch (Exception e) { ReseauJeu.Journal("[auto] capture " + nom + " impossible : " + e.Message); }
        }

        /// Test du donjon (-deathless-donjon) : entrée par le portail du village, un tas d'or, un coffre (l'hôte décide),
        /// puis retour par le portail (dépôt) ou attente du rappel. Le journal donne l'état vu par ce poste.
        bool Donjon(Partie p, Heros h, float t)
        {
            var dj = DonjonJeu.Instance;
            if (dj == null) { if (m_EtapeDonjon == 0) { m_EtapeDonjon = 99; ReseauJeu.Journal("[auto] donjon : absent"); } return false; }
            h.Entrees.DeplacementTest = Vector2.zero;
            h.Entrees.SprintTest = false;
            var g = dj.generateur;
            string etat = "graine " + dj.GraineCourante + ", pris " + dj.Pris + ", or porté " + (p.JoueurLocal != null ? p.JoueurLocal.orPorte : -1) + ", caisse " + p.Etat.orEquipe + ", au donjon " + DonjonJeu.AuDonjon(h) + ", phase " + p.Etat.phase;
            if (dj.Message != m_DonjonMessage) { m_DonjonMessage = dj.Message; if (m_DonjonMessage != "") ReseauJeu.Journal("[auto] donjon, message : " + m_DonjonMessage + " (" + etat + ")"); }
            if (m_EtapeDonjon == 0 && t > 5f && dj.Pret && p.Etat.phase == Phase.Jour)
            {
                m_EtapeDonjon = 1;
                var pv = FindAnyObjectByType<VueCycle>().portail;
                Vector3 n = p.nyxessa.transform.position - pv.Center; n.y = 0f;
                // Devant le portail (à 1,8 m du centre : la touche Interagir porte à 3 m), tourné vers lui.
                h.Teleporter(new Vector3(pv.Center.x, 0.1f, pv.Center.z) + n.normalized * 1.8f);
                Regarder(p, h, -n);
                ReseauJeu.Journal("[auto] donjon : devant le portail du village (" + etat + ")");
            }
            else if (m_EtapeDonjon == 1 && t > 6.5f)
            {
                // On n'entre plus en marchant dedans : touche Interagir (Quentin, 26/09/2026).
                m_EtapeDonjon = 11;
                Capturer("portail_village_invite");
                PointInteraction.Courant(h, out string invite);
                bool ok = PointInteraction.InteragirIci(h);
                ReseauJeu.Journal("[auto] donjon : invite « " + invite + " », interaction " + ok + ", en transit " + h.EnTransit);
            }
            else if (m_EtapeDonjon == 11 && t > 6.8f)
            {
                m_EtapeDonjon = 12;
                Capturer("portail_village_passage");
            }
            else if (m_EtapeDonjon == 12 && t > 9f)
            {
                m_EtapeDonjon = 2;
                Capturer("portail_donjon_arrivee");
                ReseauJeu.Journal("[auto] donjon : après le portail, position " + h.transform.position.ToString("F1") + " (" + etat + ")");
                for (int i = 0; i < g.Butins.Length; i++)
                    if (g.Butins[i].butin == Deathless.Donjon.TypeButin.TasOr && !dj.ButinPris(i)) { h.Teleporter(g.Butins[i].transform.position + Vector3.up * 0.1f); ReseauJeu.Journal("[auto] donjon : sur le tas d'or " + i); break; }
            }
            else if (m_EtapeDonjon == 2 && t > 11.5f)
            {
                m_EtapeDonjon = 3;
                for (int i = 0; i < g.Butins.Length; i++)
                    if (g.Butins[i].butin != Deathless.Donjon.TypeButin.TasOr && !dj.ButinPris(i))
                    {
                        var r = g.Butins[i].transform;
                        h.Teleporter(r.position + r.forward * 1.4f + Vector3.up * 0.1f);
                        Regarder(p, h, -r.forward);
                        ReseauJeu.Journal("[auto] donjon : devant le coffre " + i + " (" + etat + ")");
                        break;
                    }
            }
            else if (m_EtapeDonjon == 3 && t > 13f)
            {
                m_EtapeDonjon = 4;
                Capturer("coffre_cadenas");
                PointInteraction.Courant(h, out string invite);
                bool ok = PointInteraction.InteragirIci(h);
                ReseauJeu.Journal("[auto] donjon : invite « " + invite + " », interaction " + ok);
            }
            else if (m_EtapeDonjon == 4 && t > 14f)
            {
                m_EtapeDonjon = 5;
                ReseauJeu.Journal("[auto] donjon : butin (" + etat + ")");
                // Devant le portail de retour, tourné vers lui (même avec « rester » : capture du portail).
                var ret = g.PortailRetour.transform;
                h.Teleporter(ret.position + ret.forward * 2.2f + Vector3.up * 0.1f);
                Regarder(p, h, -ret.forward);
                ReseauJeu.Journal("[auto] donjon : devant le portail de retour, visuel " + (g.VisuelPortailRetour != null ? "gemmes" : g.PortailRetour.visuel != null ? g.PortailRetour.visuel.name : "aucun"));
            }
            else if (m_EtapeDonjon == 5 && t > 15.5f)
            {
                m_EtapeDonjon = 51;
                Capturer("portail_retour_donjon");
                PointInteraction.Courant(h, out string invite);
                bool ok = m_Donjon == "retour" && PointInteraction.InteragirIci(h);
                ReseauJeu.Journal("[auto] donjon : invite « " + invite + " », interaction " + ok);
            }
            else if (m_EtapeDonjon == 51 && t > 15.8f)
            {
                m_EtapeDonjon = 52;
                if (m_Donjon == "retour") Capturer("portail_retour_passage");
            }
            else if (m_EtapeDonjon == 52 && t > 18f)
            {
                m_EtapeDonjon = 6;
                if (m_Donjon == "retour") Capturer("portail_village_sortie");
                ReseauJeu.Journal("[auto] donjon : " + (m_Donjon == "retour" ? "après le retour" : "on attend le rappel") + " (" + etat + ")");
            }
            else if (m_EtapeDonjon == 6 && p.Etat.phase != Phase.Jour)
            {
                m_EtapeDonjon = 7;
                ReseauJeu.Journal("[auto] donjon : phase " + p.Etat.phase + ", position " + h.transform.position.ToString("F1") + " (" + etat + ")");
            }
            return m_EtapeDonjon != 7;   // étapes 11, 12, 51, 52 : intermédiaires
        }

        /// Test des achats à la relique (-deathless-achat) : vers 6 s, le héros va près de Nyxessa, ouvre le menu (touche
        /// Interagir) et achète le palier 2 des missiles ; l'hôte décide, le journal donne sa réponse et les paliers.
        bool Achat(Partie p, float t)
        {
            var h = p.HerosLocal;
            if (m_Taverne) return Taverne(p, h, t);
            if (m_EtapeAchat == 0 && t > 6f)
            {
                m_EtapeAchat = 1;
                h.Entrees.DeplacementTest = Vector2.zero;
                h.Teleporter(p.nyxessa.transform.position + new Vector3(0f, 0.1f, -7.5f));
                PartieReseau.ReponseAchat += m => ReseauJeu.Journal("[auto] réponse de l'hôte : " + m);
                return true;
            }
            if (m_EtapeAchat == 1 && t > 7f)
            {
                m_EtapeAchat = 2;
                PointInteraction.Courant(h, out string invite);
                bool ok = PointInteraction.InteragirIci(h);
                ReseauJeu.Journal("[auto] invite « " + invite + " », menu ouvert : " + ok + ", or " + p.Etat.orEquipe + ", paliers " + p.Etat.nyxessa.palierMissiles + "/" + p.Etat.nyxessa.palierBouclier);
                var a = p.nyxessa.GetComponent<AchatRelique>();
                if (a != null) { a.Acheter(0); ReseauJeu.Journal("[auto] achat des missiles demandé : " + a.Message); }
                return true;
            }
            if (m_EtapeAchat == 2 && t > 10f)
            {
                m_EtapeAchat = 3;
                ReseauJeu.Journal("[auto] après l'achat : or " + p.Etat.orEquipe + ", paliers " + p.Etat.nyxessa.palierMissiles + "/" + p.Etat.nyxessa.palierBouclier);
            }
            return m_EtapeAchat < 3;
        }
        int m_Attendre = 1;
        float m_GardeJusqua, m_AttaqueJusqua;

        void Journaliser(ILobby l, Partie p)
        {
            var t = "[auto] " + l.Etat + ", joueurs :";
            foreach (var j in l.Joueurs) t += " " + j.Pseudo + "/" + j.ClasseId + (j.Pret ? "/prêt" : "") + (j.EstHote ? "/hôte" : "") + (j.EstLocal ? "/moi" : "");
            if (p != null && p.HerosLocal != null)
            {
                var pos = p.HerosLocal.transform.position;
                t += " | mon héros " + V(pos) + (p.HerosLocal.AuSol ? " au sol" : " en l'air");
                if (Physics.Raycast(new Vector3(pos.x, 60f, pos.z), Vector3.down, out var sol, 400f, ~0, QueryTriggerInteraction.Ignore))
                    t += ", sol " + sol.collider.name + " à " + sol.point.y.ToString("F1");
                else t += ", pas de sol sous le héros";
                foreach (var hr in HerosReseau.Tous) if (hr != null && !hr.IsOwner) t += " | effets reçus de " + hr.Pseudo + " : " + hr.EffetsRecus + " (" + string.Join(",", hr.EffetsVus) + ")";
                if (p.cameraJeu != null) t += " | " + p.cameraJeu.Diagnostic() + (p.cameraJeu.cible == p.HerosLocal.transform ? " (héros local)" : " (PAS le héros local)");
            }
            if (p != null && p.Etat.phase != Phase.Attente)
            {
                var dv = DirecteurVagues.Instance;
                t += " | " + p.Etat.phase + " nuit " + p.Etat.nuit + " t=" + p.Etat.tempsPhase.ToString("F0") + " nyx " + (p.nyxessa != null ? p.nyxessa.Pv.ToString("F0") : "?")
                    + " or " + p.Etat.orEquipe + " squelettes " + (dv != null ? dv.Vivants.Count : 0);
                var j = p.JoueurLocal;
                if (j != null) t += " | moi pv " + j.pv.ToString("F0") + (j.mort ? " MORT " + j.reapparitionRestante.ToString("F0") + " s" : "") + (j.pret ? " prêt" : "") + " points " + j.pointsCompetence + " tués " + j.score.ennemisTues + " dégâts " + j.score.degatsInfliges.ToString("F0") + " or " + j.score.orRapporte;
                var so = Sorcier.Instance;
                if (so != null) t += " | sorcier " + so.EtatCourant;
                var bo = BouclierNyxessa.Instance;
                if (bo != null && bo.Leve) t += " bouclier " + bo.Vie.ToString("F0");
                // Statuts : ennemis affectés vus par ce poste, listes reçues de l'hôte, demandes envoyées, statuts du héros local.
                int affectes = 0;
                var parType = new System.Collections.Generic.SortedDictionary<string, int>();
                foreach (var st in Statuts.Actifs)
                {
                    if (st == null || st.Nombre == 0 || st.GetComponent<Squelette>() == null) continue;
                    affectes++;
                    foreach (var s in st.Liste) { parType.TryGetValue(s.type.ToString(), out int c); parType[s.type.ToString()] = c + 1; }
                }
                t += " | statuts : " + affectes + " ennemis";
                foreach (var kv in parType) t += " " + kv.Key + "x" + kv.Value;
                t += ", listes reçues " + Statuts.ListesRecues + ", demandes " + Statuts.DemandesEnvoyees;
                if (p.HerosLocal != null && p.HerosLocal.Statuts != null)
                    foreach (var s in p.HerosLocal.Statuts.Liste) t += ", moi " + s.type + (s.predit ? " (prédit)" : "") + " " + s.Restant.ToString("F1") + " s";
            }
            if (p != null && p.Etat.phase == Phase.Terminee && DonneesUI.Score != null)
            {
                var sc = DonneesUI.Score;
                t += " | SCORE " + sc.Resultat + " nuit " + sc.NuitAtteinte + " or " + sc.OrTotal + " :";
                foreach (var ls in sc.Joueurs) t += " [" + ls.Nom + " " + ls.Classe + (ls.EstLocal ? " moi" : "") + " or " + ls.OrRapporte + " dégâts " + ls.DegatsInfliges + " tués " + ls.EnnemisTues + " morts " + ls.Morts + "]";
            }
            foreach (var h in HerosReseau.Tous)
                if (h != null && !h.IsOwner) t += " | " + h.Pseudo + " (" + h.ClasseId + ") " + V(h.transform.position) + " vie " + h.Vie.ToString("F0") + "/" + h.VieMax.ToString("F0");
            ReseauJeu.Journal(t);
        }

        static string V(Vector3 v) => "(" + v.x.ToString("F1") + ", " + v.y.ToString("F1") + ", " + v.z.ToString("F1") + ")";
    }
}
