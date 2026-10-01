using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Deathless.Jeu.Dev
{
    /// Banc en jeu des classes (01/10/2026, Docs/equilibrage-classes.md « Banc en jeu ») : compare les cinq classes face à
    /// la même vague déterministe, comme Docs/outils/simulateur_vagues.py, mais dans le vrai jeu. Lancé en Play par
    /// execute_code : BancClasses.Lancer("paladin,viking,mage,rodeur,assassin", "6,12", "moyen,bon", 2) ; mono-cible :
    /// BancClasses.LancerMono(...). Sans effet hors Play (rien n'est créé tant qu'on ne l'appelle pas).
    ///
    /// Chaque passage recharge le village (partie solo neuve de la classe), prépare la nuit N pour un joueur avec une graine
    /// fixe (DirecteurVagues.Preparer : composition, élites, clairières, instants ; boss retirés), puis pose lui-même les
    /// sorties aux mêmes points et aux mêmes instants (graine par squelette : mêmes couloirs, places et pas). Le héros est
    /// immortel (GameBalance.joueurInvincible le temps du banc, remis ensuite) ; Nyxessa garde ses missiles (palier 1), a
    /// des PV hors d'atteinte (les coups reçus sont comptés), et le bouclier du sorcier n'est pas levé (le simulateur ne le
    /// modélise pas). La nuit est prolongée de 60 s pour mesurer le vidage des vagues (simulateur : idem).
    ///
    /// Un bot par classe joue par la manette virtuelle (EntreesSimulees → InputChordResolver) : il ne pilote que la caméra
    /// (lacet, tangage) et les boutons ; aucune méthode de dégâts n'est appelée. Deux profils de visée (« moyen », « bon »)
    /// reprennent les profils du simulateur ; les taux de touche, de tête et de dos sont mesurés, pas supposés.
    /// Mesures (abonnements à Sante.AnyTouche / AnyImmunise et ProjectileJeu.Arrivee) : une ligne par passage dans
    /// Docs/outils/banc_classes_mesures.tsv ; Rapport() agrège et écrit Docs/outils/banc_classes_resultats.md. Console : [Banc].
    public class BancClasses : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static BancClasses() { Deathless.Succes.ServiceSucces.SuspendreDev("BancClasses"); }

        // ================================================================= API (execute_code)

        static BancClasses s_I;
        static GameBalance s_Reglages;
        static bool? s_InvincibleAvant;
        static string s_Etat = "inactif";

        public static readonly string[] Classes = { "paladin", "viking", "mage", "rodeur", "assassin" };

        /// Vagues : classes, nuits et profils séparés par des virgules ; `reps` répétitions (graines repDepart…) ; `vitesse` :
        /// Time.timeScale pendant la nuit.
        public static string Lancer(string classes, string nuits, string profils, int reps = 1, int repDepart = 0, float vitesse = 3f)
        {
            if (!Application.isPlaying) return "hors Play : rien lancé";
            I.StopAllCoroutines();
            I.StartCoroutine(I.Serie(Liste(classes), Entiers(nuits), Liste(profils), reps, repDepart, vitesse, false));
            return "banc lancé";
        }

        /// Mono-cible : un guerrier aux PV hors d'atteinte qui frappe Nyxessa, `dureeMono` s de rotation (sans missiles).
        public static string LancerMono(string classes, string profils, int reps = 1, int repDepart = 0, float vitesse = 3f, float dureeMono = 60f)
        {
            if (!Application.isPlaying) return "hors Play : rien lancé";
            I.m_DureeMono = dureeMono;
            I.StopAllCoroutines();
            I.StartCoroutine(I.Serie(Liste(classes), new[] { 0 }, Liste(profils), reps, repDepart, vitesse, true));
            return "banc mono lancé";
        }

        public static string Etat() => s_Etat;

        public static void Arreter()
        {
            if (s_I != null) s_I.StopAllCoroutines();
            Remettre();
            s_Etat = "arrêté";
        }

        static BancClasses I
        {
            get
            {
                if (s_I == null)
                {
                    var go = new GameObject("BancClasses");
                    DontDestroyOnLoad(go);
                    s_I = go.AddComponent<BancClasses>();
                }
                return s_I;
            }
        }

        static string[] Liste(string s) => s.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        static int[] Entiers(string s) { var l = Liste(s); var r = new int[l.Length]; for (int i = 0; i < l.Length; i++) r[i] = int.Parse(l[i]); return r; }

        /// Réglages de test remis : héros mortel, temps normal, manette relâchée.
        static void Remettre()
        {
            Time.timeScale = 1f;
            EntreesSimulees.ToutRelacher();
            if (s_Reglages != null && s_InvincibleAvant.HasValue) s_Reglages.joueurInvincible = s_InvincibleAvant.Value;
            s_InvincibleAvant = null;
        }

        void OnDisable() { Remettre(); }
        void OnDestroy() { Remettre(); if (s_I == this) s_I = null; }
        void OnApplicationQuit() { Remettre(); }

        static void Log(string t) { Debug.Log("[Banc] " + t); }

        static string Racine => Directory.GetParent(Application.dataPath).FullName;
        public static string FichierMesures => Path.Combine(Racine, "Docs/outils/banc_classes_mesures.tsv");
        public static string FichierResultats => Path.Combine(Racine, "Docs/outils/banc_classes_resultats.md");

        // ================================================================= Série de passages

        float m_DureeMono = 60f;

        IEnumerator Serie(string[] classes, int[] nuits, string[] profils, int reps, int repDepart, float vitesse, bool mono)
        {
            int total = nuits.Length * profils.Length * reps * classes.Length, n = 0;
            foreach (int nuit in nuits)
                foreach (string profil in profils)
                    for (int rep = repDepart; rep < repDepart + reps; rep++)
                        foreach (string classe in classes)
                        {
                            n++;
                            s_Etat = "passage " + n + "/" + total + " : " + classe + (mono ? " mono" : " nuit " + nuit) + " " + profil + " rep " + rep;
                            // Garde-fou (01/10/2026) : un écran autre que le HUD au sommet pendant la mesure (menu, pause,
                            // personnage…) coupe la carte Gameplay et le bot ne joue plus : passage invalidé et relancé.
                            for (int essai = 0; essai < 3; essai++)
                            {
                                yield return Passage(classe, nuit, profil, rep, vitesse, mono);
                                if (m_Invalide == null) break;
                                Log("passage INVALIDÉ (" + m_Invalide + ") : " + classe + (mono ? " mono" : " nuit " + nuit) + " " + profil + " rep " + rep + ", relancé (essai " + (essai + 2) + ")");
                            }
                        }
            Remettre();
            string r = Rapport();
            s_Etat = "terminé (" + total + " passages) ; " + r;
            Log("série terminée : " + total + " passages ; " + r);
        }

        // ================================================================= Un passage

        Mesures M;
        Bot m_Bot;
        Heros m_Heros;
        Partie m_Partie;
        readonly List<Suivi> m_Suivis = new List<Suivi>();
        float m_T0Nuit;          // Time.time au début de la nuit (ou de la mesure mono)
        bool m_Mesure;           // mesure en cours
        float m_Fin;             // durée de la nuit (s, temps de jeu) : au-delà, prolongation
        float m_PorteeCombat;

        class Suivi { public Squelette s; public int vague; public float sortie; public float mort = -1f; public bool aNyxessa; public float dMin = 999f; }

        struct SortiePlan { public float instant; public TypeEnnemi type; public int clairiere; public bool elite; public Vector3 point; public int vague; }

        static int Graine(int nuit, int rep) => 1000 * nuit + 17 * rep + 1;

        string m_Invalide;
        Deathless.UI.Ecrans.NavigateurEcrans m_Nav;

        /// Pendant la mesure : le HUD doit être l'écran au sommet et la carte Gameplay active (sinon le bot ne joue plus).
        void VerifierEcran()
        {
            if (m_Invalide != null) return;
            if (m_Nav == null) m_Nav = FindAnyObjectByType<Deathless.UI.Ecrans.NavigateurEcrans>();
            if (m_Nav != null && m_Nav.Sommet != null && m_Nav.Sommet != m_Nav.Hud) m_Invalide = "écran " + m_Nav.Sommet.GetType().Name + " au sommet à t = " + TempsMesure.ToString("F0") + " s";
            else if (m_Heros != null && m_Heros.Entrees != null && !m_Heros.Entrees.CarteJeuActive) m_Invalide = "carte Gameplay coupée à t = " + TempsMesure.ToString("F0") + " s";
        }

        IEnumerator Passage(string classe, int nuit, string profil, int rep, float vitesse, bool mono)
        {
            m_Invalide = null;
            m_Nav = null;
            Time.timeScale = 1f;
            EntreesSimulees.ToutRelacher();
            // 1) Partie solo neuve de la classe (le village est rechargé : rien ne reste du passage précédent).
            var ancienne = Partie.Instance;
            Partie.ClasseChoisie = classe;
            Partie.LancerAuChargement = true;
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.path : scene.name);
            float attente = Time.realtimeSinceStartup;
            while (Partie.Instance == null || ReferenceEquals(Partie.Instance, ancienne) || Partie.Instance.HerosLocal == null || Partie.Instance.Etat.phase == Phase.Attente)
            {
                if (Time.realtimeSinceStartup - attente > 40f) { Log("ÉCHEC : la partie ne démarre pas (" + classe + ")"); yield break; }
                yield return null;
            }
            for (int i = 0; i < 3; i++) yield return null;
            var p = m_Partie = Partie.Instance;
            var h = m_Heros = p.HerosLocal;
            var b = s_Reglages = GameBalance.Courant;
            if (h.Classe == null || h.Classe.Id != classe) { Log("ÉCHEC : classe " + (h.Classe != null ? h.Classe.Id : "?") + " au lieu de " + classe); yield break; }
            if (!s_InvincibleAvant.HasValue) s_InvincibleAvant = b.joueurInvincible;
            b.joueurInvincible = true;
            // Nyxessa : PV hors d'atteinte (la mesure ne s'arrête pas ; ses coups reçus sont comptés), bouclier jamais levé.
            p.nyxessa.Initialiser(1e7f);
            if (BouclierNyxessa.Instance != null) BouclierNyxessa.Instance.effet = null;
            if (Sorcier.Instance != null && Sorcier.Instance.Sante != null) Sorcier.Instance.Sante.Initialiser(1e7f);
            var dv = DirecteurVagues.Instance;
            Vector3 nyx = p.nyxessa.transform.position;

            M = new Mesures { classe = classe, nuit = nuit, profil = profil, rep = rep, mono = mono, vitesse = vitesse };
            m_Suivis.Clear();
            m_Mesure = false;
            m_PorteeCombat = classe == "mage" ? 30f : classe == "rodeur" ? 40f : 12.5f;
            int graine = Graine(nuit, rep);

            // 2) Plan de la nuit (ou mannequin mono) et poste de départ face à une clairière.
            var plan = new List<SortiePlan>();
            Vector3 dirDepart;
            if (!mono)
            {
                Random.InitState(graine);
                p.ForcerPhase(Phase.Crepuscule, nuit);
                LirePlan(dv, nuit, graine, plan);
                int c0 = p.Etat.vagues.clairieresActives.Count > 0 ? p.Etat.vagues.clairieresActives[0] : 0;
                dirDepart = Plat(dv.clairieres[c0].position - nyx).normalized;
                M.ennemis = plan.Count;
            }
            else
            {
                var d = FindAnyObjectByType<DefenseNyxessa>();
                if (d != null) d.enabled = false;   // simulateur : mono sans missiles
                dirDepart = Plat(dv.clairieres[0].position - nyx).normalized;
            }
            m_Bot = Bot.Creer(classe, this, profil, graine * 31 + System.Array.IndexOf(Classes, classe) * 7 + (profil == "bon" ? 3 : 0));
            m_Bot.Pression = dirDepart;
            Vector3 poste = Sol(nyx + dirDepart * Bot.Poste);
            DevPartie.PlacerHeros(poste, poste + dirDepart * 10f);
            yield return null;

            Abonner(true);
            if (!mono)
            {
                // 3) Crépuscule, puis nuit prolongée de 60 s (mesure du vidage) au temps accéléré.
                while (p.Etat.phase != Phase.Nuit) yield return null;
                m_Fin = b.dureeNuit;
                p.Etat.dureePhase = b.dureeNuit + 60f;
                m_T0Nuit = Time.time;
                m_Mesure = true;
                Time.timeScale = vitesse;
                int k = 0;
                float t = 0f;
                bool aube = false;
                while (true)
                {
                    t = p.Etat.tempsPhase;
                    while (k < plan.Count && plan[k].instant <= t)
                    {
                        var sp = plan[k];
                        Random.InitState(graine * 7919 + 104729 + k);   // couloir, place et pas du squelette : les mêmes pour toutes les classes
                        var sq = dv.Poser(sp.type, sp.point, sp.elite, true);
                        if (sq != null) Suivre(sq, sp.vague, t);
                        k++;
                    }
                    if (!aube && t >= m_Fin) { aube = true; Aube(); }
                    bool vivants = false;
                    foreach (var s in m_Suivis) if (s.mort < 0f && s.s != null && s.s.Vivant) { vivants = true; break; }
                    if (k >= plan.Count && !vivants && t > 1f) break;
                    if (t >= m_Fin + 59.5f || p.Etat.phase != Phase.Nuit) break;
                    yield return null;
                }
                if (!aube) Aube();
                M.duree = Mathf.Min(t, m_Fin + 60f);
            }
            else
            {
                Vector3 pt = Sol(nyx + dirDepart * (b.rayonPlacesNyxessa + 0.3f));   // au contact de Nyxessa (simulateur : mannequin qui la frappe)
                var sq = dv.Poser(TypeEnnemi.Guerrier, pt, false, false);
                sq.Sante.Initialiser(1e6f);
                Suivre(sq, 0, 0f);
                float t0 = Time.time;
                while (sq != null && sq.EtatCourant == Squelette.Etat.SortieDeTerre && Time.time - t0 < 5f) yield return null;
                m_Fin = m_DureeMono;
                m_T0Nuit = Time.time;
                m_Mesure = true;
                Time.timeScale = vitesse;
                while (Time.time - m_T0Nuit < m_DureeMono && sq != null && sq.Vivant) yield return null;
                M.duree = Time.time - m_T0Nuit;
            }
            m_Mesure = false;
            Time.timeScale = 1f;
            EntreesSimulees.ToutRelacher();
            Abonner(false);
            if (m_Invalide != null) { Abonner(false); yield break; }
            M.Clore(m_Suivis, m_Fin);
            Ecrire(M);
            var restes = new StringBuilder();
            foreach (var su in m_Suivis)
                if (su.s != null && su.s.Vivant)
                    restes.Append(" | ").Append(su.s.type).Append(su.s.elite ? " élite" : "").Append(" vague ").Append(su.vague + 1).Append(" à ").Append(Plat(su.s.transform.position - p.nyxessa.transform.position).magnitude.ToString("F1"))
                          .Append(" m de Nyxessa, ").Append(su.s.EtatCourant).Append(" pos ").Append(su.s.transform.position.ToString("F0"));
            Log(M.Resume() + (restes.Length > 0 ? " || restants" + restes : ""));
            yield return null;
        }

        /// Plan préparé par DirecteurVagues (composition, élites, clairières, instants) recopié, boss retirés ; points de
        /// sortie tirés ici avec une graine par sortie, puis la liste du directeur vidée : le banc pose lui-même.
        void LirePlan(DirecteurVagues dv, int nuit, int graine, List<SortiePlan> plan)
        {
            var b = GameBalance.Courant;
            var champ = typeof(DirecteurVagues).GetField("m_AFaire", BindingFlags.NonPublic | BindingFlags.Instance);
            var pointDans = typeof(DirecteurVagues).GetMethod("PointDans", BindingFlags.NonPublic | BindingFlags.Instance);
            var liste = (IList)champ.GetValue(dv);
            var tSortie = liste.GetType().GetGenericArguments()[0];
            var fInstant = tSortie.GetField("instant"); var fType = tSortie.GetField("type"); var fCl = tSortie.GetField("clairiere"); var fElite = tSortie.GetField("elite");
            float[] departs = nuit >= b.nuitQuatreVagues ? b.departsQuatreVagues : b.departsTroisVagues;
            float vit = Mathf.Max(0.01f, b.vitesseCycle);
            int i = 0;
            foreach (var o in liste)
            {
                var sp = new SortiePlan { instant = (float)fInstant.GetValue(o), type = (TypeEnnemi)fType.GetValue(o), clairiere = (int)fCl.GetValue(o), elite = (bool)fElite.GetValue(o) };
                if (DirecteurVagues.EstBoss(sp.type)) continue;
                Random.InitState(graine * 7919 + i);
                sp.point = (Vector3)pointDans.Invoke(dv, new object[] { sp.clairiere });
                for (int w = 0; w < departs.Length; w++) if (sp.instant >= departs[w] / vit - 0.001f) sp.vague = w;
                plan.Add(sp);
                i++;
            }
            liste.Clear();
        }

        void Suivre(Squelette sq, int vague, float t)
        {
            var s = new Suivi { s = sq, vague = vague, sortie = t };
            m_Suivis.Add(s);
            sq.Retire += x => { if (s.mort < 0f && x != null && x.Sante != null && x.Sante.Mort) s.mort = TempsMesure; };
        }

        /// Aube (temps de la nuit écoulé) : survivants comptés, dégâts « de nuit » figés ; la nuit continue 60 s (vidage).
        void Aube()
        {
            int n = 0;
            foreach (var s in m_Suivis) if (s.s != null && s.s.Vivant) n++;
            M.survivantsAube = n;
            M.degatsNuit = M.degats;
        }

        float TempsMesure => Time.time - m_T0Nuit;

        // ================================================================= Mesures (événements existants)

        void Abonner(bool oui)
        {
            Sante.AnyTouche -= OnTouche;
            Sante.AnyImmunise -= OnImmunise;
            ProjectileJeu.Arrivee -= OnArrivee;
            if (oui)
            {
                Sante.AnyTouche += OnTouche;
                Sante.AnyImmunise += OnImmunise;
                ProjectileJeu.Arrivee += OnArrivee;
            }
        }

        bool m_FlecheSalve;
        int m_SalveRestantes;
        float m_SalveJusque;

        /// Le bot vient de lancer la roulade : les flèches suivantes (une salve) sont comptées à part.
        public void NoterSalve() { m_SalveRestantes = GameBalance.Courant.salveFleches; m_SalveJusque = Time.time + 2f; }

        void OnArrivee(ProjectileJeu.Genre g, Vector3 point, Sante s, float sommet)
        {
            if (!m_Mesure) return;
            bool touche = s != null && s.equipe == Equipe.Ennemis;
            switch (g)
            {
                case ProjectileJeu.Genre.Fleche:
                    m_FlecheSalve = m_SalveRestantes > 0 && Time.time < m_SalveJusque;
                    if (m_FlecheSalve) { m_SalveRestantes--; M.salveTirs++; if (touche) M.salveTouches++; }
                    else { M.arcTirs++; if (touche) M.arcTouches++; }
                    break;
                case ProjectileJeu.Genre.Carreau: M.carreauTirs++; if (touche) M.carreauTouches++; break;
                case ProjectileJeu.Genre.BouleDeFeu: M.bouleTirs++; if (touche) M.bouleDirectes++; break;
                case ProjectileJeu.Genre.GrandeBouleDeFeu: M.grandeTirs++; if (touche) M.grandeDirectes++; break;
            }
        }

        void OnTouche(Sante cible, InfoDegats info, float reel)
        {
            if (!m_Mesure || M == null) return;
            var p = m_Partie;
            if (p == null) return;
            if (cible.equipe == Equipe.Ennemis)
            {
                bool heros = m_Heros != null && info.equipeSource == Equipe.Heros && info.sourceId == m_Heros.Id;
                if (!heros) { M.autres += reel; if (cible.Mort) M.tuesAutres++; return; }
                string src = Source(info);
                M.degats += reel;
                if (info.critique) M.crit += reel;
                M.Ajouter(src, reel);
                if (cible.Mort) M.tues++;
                var b = GameBalance.Courant;
                switch (src)
                {
                    case "arc": M.arcDegatsTouches++; if (info.critique) M.arcTetes++; break;
                    case "salve": if (info.critique) M.salveTetes++; break;
                    case "arbalete": if (info.critique) M.carreauTetes++; break;
                    case "dague":
                    {
                        M.dagueCoups++;
                        if (info.execution) M.dagueExecutions++;
                        else
                        {
                            int mult = Mathf.RoundToInt(info.montant / Mathf.Max(0.01f, b.dagueDegats));
                            if (mult == Mathf.RoundToInt(b.critiqueFurtifDos)) { M.dagueDos++; M.dagueFurtif++; }
                            else if (mult == Mathf.RoundToInt(b.critiqueDos)) M.dagueDos++;
                            else if (mult == Mathf.RoundToInt(b.critiqueFurtif)) M.dagueFurtif++;
                        }
                        m_Bot.SurCoupDague(cible);
                        break;
                    }
                }
                return;
            }
            // Coups ennemis sur Nyxessa (ou sur le sorcier, qui prend les coups destinés à Nyxessa sans bouclier).
            bool versNyx = cible == p.nyxessa || (Sorcier.Instance != null && cible == Sorcier.Instance.Sante);
            if (versNyx && info.equipeSource == Equipe.Ennemis)
            {
                M.nyxDegats += reel;
                var att = info.source != null ? info.source.GetComponent<Squelette>() : null;
                if (att != null) foreach (var s in m_Suivis) if (s.s == att) { s.aNyxessa = true; break; }
            }
        }

        void OnImmunise(Sante cible, InfoDegats info)
        {
            if (!m_Mesure || M == null) return;
            if (m_Heros != null && cible == m_Heros.Sante && info.equipeSource == Equipe.Ennemis)
            {
                // Le coup qu'il aurait pris (Peau de fer du viking déduite).
                float m = info.montant;
                var st = m_Heros.Statuts;
                if (st != null && st.A(TypeStatut.PeauDeFer)) m *= 1f - st.Intensite(TypeStatut.PeauDeFer);
                M.recu += m;
                m_Bot.SurCoupRecu(m);
            }
            else if (cible.equipe == Equipe.Ennemis && m_Heros != null && info.sourceId == m_Heros.Id && info.montant > 0f && !info.continu)
                M.annules++;   // squelette en esquive (ou sortant de terre) : coup du héros sans effet
        }

        /// Origine d'un coup du héros : méthode de classe qui a appelé Heros.Frapper (pile d'appels), plus le drapeau de
        /// salve posé par l'arrivée de la flèche (même pile d'appels).
        string Source(InfoDegats info)
        {
            var st = new System.Diagnostics.StackTrace(2, false);
            for (int i = 0; i < st.FrameCount && i < 16; i++)
            {
                var m = st.GetFrame(i).GetMethod();
                if (m == null || m.DeclaringType == null) continue;
                string nom = Extraire(m.Name);
                var t = m.DeclaringType;
                while (t.DeclaringType != null)
                {
                    if (t.Name.StartsWith("<") && !t.Name.StartsWith("<>")) nom = Extraire(t.Name);
                    t = t.DeclaringType;
                }
                string tn = t.Name;
                if (tn == "Sante" || tn == "Heros" || tn == "BancClasses") continue;
                switch (tn + "." + nom)
                {
                    case "ClasseRodeur.TirerFleche": return m_FlecheSalve ? "salve" : "arc";
                    case "ClasseRodeur.Pluie": return "nuee";
                    case "ClasseAssassin.PorterDague": return "dague";
                    case "ClasseAssassin.Tirer": return "arbalete";
                    case "ClasseMage.Toucher": return "boule";
                    case "ClasseMage.ToucherGrande": return "grande_boule";
                    case "ClasseMage.Maj": return "cone";
                    case "Statuts.Bruler": return "brulure";
                    case "ClasseViking.Maj": return info.continu ? "tournante" : "hache";
                    case "ClasseViking.Atterrir": return "saut";
                    case "ClassePaladin.PorterCoup": return "epee";
                    case "ClassePaladin.FinCharge": return "charge";
                    case "ParadeParfaite.Appliquer": return "riposte";
                }
                return tn + "." + nom;
            }
            return "?";
        }

        static string Extraire(string n)
        {
            if (!n.StartsWith("<")) return n;
            int f = n.IndexOf('>');
            return f > 1 ? n.Substring(1, f - 1) : n;
        }

        void Update()
        {
            if (!m_Mesure || M == null || m_Bot == null || m_Partie == null) return;
            if (m_Heros == null) return;
            VerifierEcran();
            M.images++;
            M.dtReel += Time.unscaledDeltaTime;
            M.dtJeuMax = Mathf.Max(M.dtJeuMax, Time.deltaTime);
            // Temps de combat : un squelette sorti de terre à portée de l'arme principale (mêlée : 12,5 m de Nyxessa).
            Vector3 nyx = m_Partie.nyxessa.transform.position;
            Vector3 ref_ = m_PorteeCombat > 13f ? m_Heros.transform.position : nyx;
            bool combat = false;
            foreach (var s in m_Suivis)
                if (s.s != null && s.s.Vivant && s.s.EtatCourant != Squelette.Etat.SortieDeTerre && Plat(s.s.transform.position - ref_).magnitude <= m_PorteeCombat) { combat = true; break; }
            if (combat) M.tempsCombat += Time.deltaTime;
            foreach (var s in m_Suivis) if (s.s != null && s.s.Vivant) s.dMin = Mathf.Min(s.dMin, Plat(s.s.transform.position - nyx).magnitude);
            m_Bot.Tick(Time.deltaTime);
        }

        public IEnumerable<Squelette> Vivants()
        {
            foreach (var s in m_Suivis)
                if (s.s != null && s.s.Vivant && s.s.EtatCourant != Squelette.Etat.SortieDeTerre && s.s.Sante != null && !s.s.Sante.Mort) yield return s.s;
        }

        public Heros Heros => m_Heros;
        public Partie Partie => m_Partie;

        // ================================================================= Écriture

        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        static void Ecrire(Mesures m)
        {
            string f = FichierMesures;
            bool neuf = !File.Exists(f);
            var sb = new StringBuilder();
            if (neuf) sb.AppendLine(Mesures.Entete);
            sb.AppendLine(m.Ligne());
            File.AppendAllText(f, sb.ToString(), new UTF8Encoding(false));
        }

        class Mesures
        {
            public string classe, profil; public int nuit, rep; public bool mono; public float vitesse;
            public int ennemis;
            public float duree, degats, degatsNuit = -1f, crit, autres, nyxDegats, recu, tempsCombat;
            public int tues, tuesAutres, annules, survivantsAube;
            public int arcTirs, arcTouches, arcDegatsTouches, arcTetes, salveTirs, salveTouches, salveTetes, carreauTirs, carreauTouches, carreauTetes;
            public int bouleTirs, bouleDirectes, grandeTirs, grandeDirectes;
            public int dagueCoups, dagueDos, dagueFurtif, dagueExecutions;
            public int images; public float dtReel, dtJeuMax;
            public readonly Dictionary<string, float> sources = new Dictionary<string, float>();
            public readonly Dictionary<string, int> usages = new Dictionary<string, int>();
            public int aNyxessa, bloques;
            public string vidage = "";

            public void Ajouter(string s, float d) { sources.TryGetValue(s, out float v); sources[s] = v + d; }
            public void Usage(string s) { usages.TryGetValue(s, out int v); usages[s] = v + 1; }

            public void Clore(List<Suivi> suivis, float fin)
            {
                if (degatsNuit < 0f) degatsNuit = degats;
                aNyxessa = 0;
                foreach (var s in suivis) if (s.aNyxessa) aNyxessa++;
                if (mono) { vidage = "-"; return; }
                var parties = new List<string>();
                for (int w = 0; w < 4; w++)
                {
                    // Un squelette resté à plus de 30 m de Nyxessa (bloqué en route) ne compte pas : « * ».
                    float debut = float.MaxValue, der = 0f; bool vue = false, toutes = true, bloque = false;
                    foreach (var s in suivis)
                    {
                        if (s.vague != w) continue;
                        vue = true;
                        debut = Mathf.Min(debut, s.sortie);
                        if (s.mort < 0f && s.dMin > 30f) { bloque = true; bloques++; continue; }
                        if (s.mort < 0f) toutes = false; else der = Mathf.Max(der, s.mort);
                    }
                    if (!vue) continue;
                    parties.Add(toutes ? (der - debut).ToString("F0", C) + (bloque ? "*" : "") : "—");
                }
                vidage = string.Join("/", parties);
            }

            public const string Entete = "date\tmono\tnuit\tprofil\trep\tclasse\tvitesse\tennemis\tduree\tdegats\tdegatsNuit\tcrit\tdpsNuit\tdpsCombat\ttempsCombat\ttues\ttuesMissiles\tdegatsMissiles\tsurvivantsAube\tvidage\taNyxessa\tnyxDegats\trecu\tannules"
                + "\tbloques\tarcTirs\tarcTouches\tarcTetes\tsalveTirs\tsalveTouches\tsalveTetes\tcarreauTirs\tcarreauTouches\tcarreauTetes\tbouleTirs\tbouleDirectes\tgrandeTirs\tgrandeDirectes\tdagueCoups\tdagueDos\tdagueFurtif\tdagueExecutions\tsources\tusages\tfps\tdtJeuMax";

            static string F(float v) => v.ToString("0.###", C);

            public string Ligne()
            {
                float dpsNuit = mono ? degats / Mathf.Max(1f, duree) : degatsNuit / Mathf.Max(1f, GameBalance.Courant.dureeNuit);
                float dpsCombat = degats / Mathf.Max(0.01f, tempsCombat);
                var src = new List<string>(); foreach (var kv in sources) src.Add(kv.Key + "=" + F(kv.Value));
                var us = new List<string>(); foreach (var kv in usages) us.Add(kv.Key + "=" + kv.Value);
                string[] c =
                {
                    System.DateTime.Now.ToString("yyyy-MM-dd HH:mm", C), mono ? "1" : "0", nuit.ToString(C), profil, rep.ToString(C), classe, F(vitesse), ennemis.ToString(C), F(duree),
                    F(degats), F(degatsNuit), F(crit), F(dpsNuit), F(dpsCombat), F(tempsCombat), tues.ToString(C), tuesAutres.ToString(C), F(autres), survivantsAube.ToString(C), vidage,
                    aNyxessa.ToString(C), F(nyxDegats), F(recu), annules.ToString(C), bloques.ToString(C),
                    arcTirs.ToString(C), arcTouches.ToString(C), arcTetes.ToString(C), salveTirs.ToString(C), salveTouches.ToString(C), salveTetes.ToString(C),
                    carreauTirs.ToString(C), carreauTouches.ToString(C), carreauTetes.ToString(C), bouleTirs.ToString(C), bouleDirectes.ToString(C), grandeTirs.ToString(C), grandeDirectes.ToString(C),
                    dagueCoups.ToString(C), dagueDos.ToString(C), dagueFurtif.ToString(C), dagueExecutions.ToString(C), string.Join(";", src), string.Join(";", us),
                    F(images / Mathf.Max(0.01f, dtReel)), F(dtJeuMax)
                };
                return string.Join("\t", c);
            }

            public string Resume()
            {
                var sb = new StringBuilder();
                sb.Append(classe).Append(mono ? " mono" : " nuit " + nuit).Append(" ").Append(profil).Append(" rep ").Append(rep).Append(" ×").Append(F(vitesse));
                if (mono) sb.Append(" : DPS mono ").Append((degats / Mathf.Max(1f, duree)).ToString("F1", C));
                else sb.Append(" : DPS de nuit ").Append((degatsNuit / GameBalance.Courant.dureeNuit).ToString("F1", C)).Append(", en combat ").Append((degats / Mathf.Max(0.01f, tempsCombat)).ToString("F1", C))
                        .Append(", tués ").Append(tues).Append("+").Append(tuesAutres).Append(" missiles / ").Append(ennemis).Append(", vidage ").Append(vidage).Append(", survivants à l'aube ").Append(survivantsAube)
                        .Append(", à Nyxessa ").Append(aNyxessa).Append(" (").Append(nyxDegats.ToString("F0", C)).Append(")");
                sb.Append(", crit ").Append(Mathf.RoundToInt(100f * crit / Mathf.Max(1f, degats))).Append(" %, reçus ").Append(recu.ToString("F0", C)).Append(", annulés ").Append(annules);
                if (arcTirs > 0) sb.Append(", arc ").Append(arcTouches).Append("/").Append(arcTirs).Append(" tête ").Append(arcTetes);
                if (salveTirs > 0) sb.Append(", salve ").Append(salveTouches).Append("/").Append(salveTirs).Append(" tête ").Append(salveTetes);
                if (carreauTirs > 0) sb.Append(", carreaux ").Append(carreauTouches).Append("/").Append(carreauTirs).Append(" tête ").Append(carreauTetes);
                if (bouleTirs > 0) sb.Append(", boules directes ").Append(bouleDirectes).Append("/").Append(bouleTirs);
                if (grandeTirs > 0) sb.Append(", grandes ").Append(grandeDirectes).Append("/").Append(grandeTirs);
                if (dagueCoups > 0) sb.Append(", dague ").Append(dagueCoups).Append(" dos ").Append(dagueDos).Append(" furtif ").Append(dagueFurtif).Append(" exéc ").Append(dagueExecutions);
                sb.Append(" | sources");
                foreach (var kv in sources) sb.Append(" ").Append(kv.Key).Append(" ").Append(kv.Value.ToString("F0", C));
                sb.Append(" | usages");
                foreach (var kv in usages) sb.Append(" ").Append(kv.Key).Append(" ").Append(kv.Value);
                sb.Append(" | ").Append((images / Mathf.Max(0.01f, dtReel)).ToString("F0", C)).Append(" img/s, dt max ").Append(dtJeuMax.ToString("F3", C));
                return sb.ToString();
            }
        }

        public void Usage(string s) { if (m_Mesure && M != null) M.Usage(s); }

        // ================================================================= Rapport (agrégation du TSV)

        static readonly string[] NomsClasses = { "Paladin", "Viking", "Mage", "Rôdeur", "Assassin" };

        /// Lit Docs/outils/banc_classes_mesures.tsv et écrit Docs/outils/banc_classes_resultats.md (moyennes des répétitions,
        /// indices rapportés à la moyenne des cinq classes de la même répétition, plage min–max). Renvoie un court bilan.
        public static string Rapport()
        {
            string f = FichierMesures;
            if (!File.Exists(f)) return "pas de mesures";
            var lignes = File.ReadAllLines(f, Encoding.UTF8);
            if (lignes.Length < 2) return "pas de mesures";
            var entete = lignes[0].Split('\t');
            var idx = new Dictionary<string, int>();
            for (int i = 0; i < entete.Length; i++) idx[entete[i]] = i;
            var rangs = new List<string[]>();
            for (int i = 1; i < lignes.Length; i++) { var c = lignes[i].Split('\t'); if (c.Length >= entete.Length - 2) rangs.Add(c); }
            // Dernière mesure par (mono, nuit, profil, rep, classe) : un passage relancé remplace l'ancien.
            var der = new Dictionary<string, string[]>();
            var vitesses = new HashSet<string>();
            foreach (var c in rangs)
            {
                string cle = c[idx["mono"]] + "|" + c[idx["nuit"]] + "|" + c[idx["profil"]] + "|" + c[idx["rep"]] + "|" + c[idx["classe"]] + "|" + c[idx["vitesse"]];
                der[cle] = c;
                vitesses.Add(c[idx["vitesse"]]);
            }
            System.Func<string[], string, float> num = (c, k) => float.Parse(c[idx[k]], C);
            var groupes = new SortedDictionary<string, List<string[]>>();
            foreach (var kv in der)
            {
                var c = kv.Value;
                string g = c[idx["mono"]] + "|" + c[idx["nuit"]].PadLeft(2, '0') + "|" + c[idx["profil"]] + "|" + c[idx["vitesse"]];
                if (!groupes.TryGetValue(g, out var l)) groupes[g] = l = new List<string[]>();
                l.Add(c);
            }
            var md = new StringBuilder();
            md.AppendLine("# Banc en jeu des classes : résultats");
            md.AppendLine();
            md.AppendLine("Écrit par `Assets/Scripts/Jeu/Dev/BancClasses.cs` (`BancClasses.Rapport()`) à partir de `Docs/outils/banc_classes_mesures.tsv` (une ligne par passage). Méthode et lecture : `Docs/equilibrage-classes.md`, section « Banc en jeu (01/10/2026) ». Indices : rapport à la moyenne des cinq classes de la même répétition (même vague), moyenne des répétitions (plage min–max entre crochets).");
            md.AppendLine();
            int nGroupes = 0;
            foreach (var g in groupes)
            {
                var parts = g.Key.Split('|');
                bool mono = parts[0] == "1";
                string nuit = parts[1].TrimStart('0'), profil = parts[2], vit = parts[3];
                // Répétitions complètes (cinq classes).
                var parRep = new SortedDictionary<string, Dictionary<string, string[]>>();
                foreach (var c in g.Value)
                {
                    if (!parRep.TryGetValue(c[idx["rep"]], out var d)) parRep[c[idx["rep"]]] = d = new Dictionary<string, string[]>();
                    d[c[idx["classe"]]] = c;
                }
                var stats = new Dictionary<string, List<float[]>>();   // classe → par rep : valeurs
                var indices = new Dictionary<string, List<float>>();
                int repsCompletes = 0;
                foreach (var r in parRep)
                {
                    if (r.Value.Count < Classes.Length) continue;
                    repsCompletes++;
                    float moy = 0f;
                    foreach (var cl in Classes) moy += num(r.Value[cl], "dpsNuit");
                    moy /= Classes.Length;
                    foreach (var cl in Classes)
                    {
                        if (!indices.TryGetValue(cl, out var li)) indices[cl] = li = new List<float>();
                        li.Add(num(r.Value[cl], "dpsNuit") / Mathf.Max(0.01f, moy));
                    }
                }
                if (repsCompletes == 0) continue;
                nGroupes++;
                md.AppendLine(mono ? "## Mono-cible, profil " + profil + " (×" + vit + ", " + repsCompletes + " répétition(s))"
                                   : "## Nuit " + nuit + ", profil " + profil + " (×" + vit + ", " + repsCompletes + " répétition(s))");
                md.AppendLine();
                if (mono) md.AppendLine("| Classe | DPS mono | **Indice mono** | Part critiques | Arc touche / tête | Arbalète touche / tête | Boules directes | Dague dos / furtif | Coups annulés (esquives) | Origine des dégâts |");
                else md.AppendLine("| Classe | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Tués (+ missiles) | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus | Arc touche / tête | Salve touche / tête | Arbalète touche / tête | Boules directes | Dague dos / furtif / exéc. | Coups annulés | Origine des dégâts |");
                md.AppendLine(mono ? "|---|---|---|---|---|---|---|---|---|---|" : "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                for (int ci = 0; ci < Classes.Length; ci++)
                {
                    string cl = Classes[ci];
                    var lr = new List<string[]>();
                    foreach (var r in parRep) if (r.Value.Count >= Classes.Length) lr.Add(r.Value[cl]);
                    System.Func<string, float> moyK = k => { float s = 0f; foreach (var c in lr) s += num(c, k); return s / lr.Count; };
                    System.Func<string, string, string> taux = (a, b2) => { float A = moyK(a), Bv = moyK(b2); return Bv > 0f ? Mathf.RoundToInt(100f * A / Bv) + " %" : "—"; };
                    var li = indices[cl];
                    float im = 0f, imin = 9f, imax = 0f; foreach (var x in li) { im += x; imin = Mathf.Min(imin, x); imax = Mathf.Max(imax, x); }
                    im /= li.Count;
                    string ind = "**" + im.ToString("0.00", C) + "**" + (li.Count > 1 ? " [" + imin.ToString("0.00", C) + "–" + imax.ToString("0.00", C) + "]" : "");
                    float deg = moyK("degats");
                    string crit = Mathf.RoundToInt(100f * moyK("crit") / Mathf.Max(1f, deg)) + " %";
                    string arc = moyK("arcTirs") > 0 ? taux("arcTouches", "arcTirs") + " / " + taux("arcTetes", "arcTouches") : "—";
                    string salve = moyK("salveTirs") > 0 ? taux("salveTouches", "salveTirs") + " / " + taux("salveTetes", "salveTouches") : "—";
                    string carreau = moyK("carreauTirs") > 0 ? taux("carreauTouches", "carreauTirs") + " / " + taux("carreauTetes", "carreauTouches") : "—";
                    string boule = moyK("bouleTirs") > 0 ? taux("bouleDirectes", "bouleTirs") : "—";
                    string dague = moyK("dagueCoups") > 0 ? taux("dagueDos", "dagueCoups") + " / " + taux("dagueFurtif", "dagueCoups") + (mono ? "" : " / " + taux("dagueExecutions", "dagueCoups")) : "—";
                    // Origine : moyenne des sources.
                    var src = new Dictionary<string, float>(); float tot = 0f;
                    foreach (var c in lr)
                        foreach (var e in c[idx["sources"]].Split(';'))
                        {
                            var kv = e.Split('='); if (kv.Length != 2) continue;
                            float v = float.Parse(kv[1], C); src.TryGetValue(kv[0], out float a); src[kv[0]] = a + v; tot += v;
                        }
                    var ls = new List<KeyValuePair<string, float>>(src); ls.Sort((a, b2) => b2.Value.CompareTo(a.Value));
                    var origine = new List<string>(); foreach (var kv in ls) origine.Add(kv.Key.Replace('_', ' ') + " " + Mathf.RoundToInt(100f * kv.Value / Mathf.Max(1f, tot)) + " %");
                    if (mono)
                        md.AppendLine("| " + NomsClasses[ci] + " | " + moyK("dpsNuit").ToString("0.0", C) + " | " + ind + " | " + crit + " | " + arc + " | " + carreau + " | " + boule + " | " + dague + " | " + moyK("annules").ToString("0", C) + " | " + string.Join(", ", origine) + " |");
                    else
                    {
                        var vid = new List<string>(); foreach (var c in lr) vid.Add(c[idx["vidage"]]);
                        md.AppendLine("| " + NomsClasses[ci] + " | " + moyK("dpsCombat").ToString("0.0", C) + " | " + moyK("dpsNuit").ToString("0.0", C) + " | " + ind + " | " + crit
                            + " | " + moyK("tues").ToString("0.#", C) + " (+" + moyK("tuesMissiles").ToString("0.#", C) + ") | " + string.Join(" ; ", vid) + " | " + moyK("aNyxessa").ToString("0.#", C)
                            + " | " + moyK("nyxDegats").ToString("0", C) + " | " + moyK("survivantsAube").ToString("0.#", C) + " | " + moyK("recu").ToString("0", C)
                            + " | " + arc + " | " + salve + " | " + carreau + " | " + boule + " | " + dague + " | " + moyK("annules").ToString("0", C) + " | " + string.Join(", ", origine) + " |");
                    }
                }
                md.AppendLine();
            }
            File.WriteAllText(FichierResultats, md.ToString(), new UTF8Encoding(false));
            Log("rapport écrit (" + nGroupes + " tableaux) :\n" + md);
            return nGroupes + " tableau(x) écrits dans Docs/outils/banc_classes_resultats.md";
        }

        // ================================================================= Outils

        public static Vector3 Plat(Vector3 v) { v.y = 0f; return v; }

        public static Vector3 Sol(Vector3 p)
        {
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) return hit.position;
            return p;
        }
    }
}
