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
    ///   -deathless-achat : achète le palier 2 des missiles à la relique (l'hôte décide)
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
            c.m_Achat = Drapeau("deathless-achat");
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
                if (m_Achat && Achat(p, m_EnPartieDepuis)) { }
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

        bool m_Competences, m_Achat;
        int m_Or;
        int m_EtapeAchat;

        /// Test des achats à la relique (-deathless-achat) : vers 6 s, le héros va près de Nyxessa, ouvre le menu (touche
        /// Interagir) et achète le palier 2 des missiles ; l'hôte décide, le journal donne sa réponse et les paliers.
        bool Achat(Partie p, float t)
        {
            var h = p.HerosLocal;
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
                if (j != null) t += " | moi pv " + j.pv.ToString("F0") + (j.mort ? " MORT " + j.reapparitionRestante.ToString("F0") + " s" : "") + (j.pret ? " prêt" : "") + " tués " + j.score.ennemisTues + " dégâts " + j.score.degatsInfliges.ToString("F0") + " or " + j.score.orRapporte;
                var so = Sorcier.Instance;
                if (so != null) t += " | sorcier " + so.EtatCourant;
                var bo = BouclierNyxessa.Instance;
                if (bo != null && bo.Leve) t += " bouclier " + bo.Vie.ToString("F0");
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
