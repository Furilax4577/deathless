using System;
using System.Collections.Generic;
using Deathless.Reseau;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deathless.Jeu
{
    /// Autorité de la partie (un par scène) : seule horloge du jeu (jour 120 s, crépuscule 5 s, nuit 120 s, aube 5 s),
    /// vote « prêt », victoire à l'aube de la nuit 12, défaite quand Nyxessa est détruite, morts et réapparitions, score.
    /// Tout l'état qui fait foi est dans `Etat` (données pures) ; les vues (ambiance, sons, HUD) s'abonnent aux événements.
    /// Multijoueur (Docs/reseau.md) : chaque poste simule son propre héros ; l'hôte fait apparaître les héros de tous
    /// (ApparaitreHerosReseau) et fera autorité sur le monde (ReseauJeu.Autorite) ; les héros des autres postes sont des
    /// marionnettes (Heros.Distant) tenues hors de Etat.joueurs.
    [DefaultExecutionOrder(-50)]
    public class Partie : MonoBehaviour
    {
        public static Partie Instance { get; private set; }
        /// Rejouer : la scène rechargée relance aussitôt une partie.
        public static bool LancerAuChargement;

        [Header("Réglages")]
        public GameBalance reglages;
        [Header("Scène")]
        public GameObject prefabHeros;
        public Sante nyxessa;
        public Transform[] pointsReapparition;
        public Transform pointDepart;
        public CameraEpaule cameraJeu;

        public EtatPartie Etat { get; private set; } = new EtatPartie();
        public GameBalance B => reglages != null ? reglages : GameBalance.Courant;
        public bool EnCours => Etat.phase != Phase.Attente && Etat.phase != Phase.Terminee && !m_Chute;

        readonly Dictionary<int, Heros> m_Heros = new Dictionary<int, Heros>();
        readonly Dictionary<int, EtatJoueur> m_Distants = new Dictionary<int, EtatJoueur>();
        public Heros HerosLocal { get; private set; }
        public EtatJoueur JoueurLocal => Etat.joueurs.Count > 0 ? Etat.joueurs[0] : null;
        bool m_Chute;          // Nyxessa détruite, écran de score imminent
        float m_ChuteDepuis;
        bool m_AlerteDonnee;

        // ----------------------------------------------------------------- Événements (vues)
        public event Action<Phase, Phase> PhaseChangee;     // ancienne, nouvelle
        public event Action<int> NuitCommencee;
        public event Action AlerteNuit;
        public event Action<float, Vector3> NyxessaTouchee;
        public event Action NyxessaDetruite;
        public event Action PartieLancee;
        public event Action PartieTerminee;
        public event Action<int, bool> PretChange;           // joueur, prêt
        public event Action<int> JoueurMort;
        public event Action<int> JoueurReapparu;

        void Awake()
        {
            Instance = this;
            if (reglages != null) GameBalance.Courant = reglages;
            ReseauJeu.Assurer();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (nyxessa != null)
            {
                nyxessa.equipe = Equipe.Relique;
                nyxessa.Initialiser(B.nyxessaPV);
                nyxessa.Touche += OnNyxessaTouchee;
                nyxessa.Tue += _ => OnNyxessaDetruite();
            }
            Etat.nyxessa.pvMax = B.nyxessaPV;
            Etat.nyxessa.pv = B.nyxessaPV;
            if (ReseauJeu.EnPartie)
            {
                // Partie réseau : le village vient d'être chargé par l'hôte ; la partie commence quand le héros de ce
                // poste apparaît (HerosReseau → LancerReseau).
                LancerAuChargement = false;
                Journal("Village chargé en mode réseau (" + (ReseauJeu.Autorite ? "hôte" : "client") + "), attente des héros");
            }
            else if (LancerAuChargement || B.lancerDirectement)
            {
                LancerAuChargement = false;
                if (B.lancerDirectement && !string.IsNullOrEmpty(B.classeDeTest)) ClasseChoisie = B.classeDeTest;
                StartCoroutine(LancerApresUneImage());
            }
        }

        // Une image d'attente : toutes les vues (vagues, ambiance, HUD) se sont abonnées dans leur Start.
        System.Collections.IEnumerator LancerApresUneImage()
        {
            yield return null;
            LancerSolo();
        }

        // ----------------------------------------------------------------- Commandes (IPartieCommandes)

        /// Classe du joueur local (choix de classe ; gardée pour « Rejouer »).
        public static string ClasseChoisie = "paladin";

        /// Menu principal > Solo : lance la partie avec la dernière classe choisie.
        public void LancerSolo() => LancerSolo(ClasseChoisie);

        /// Crée le héros de la classe `classeId` et lance la partie au jour qui précède la nuit de départ.
        public void LancerSolo(string classeId)
        {
            if (Etat.phase != Phase.Attente) return;
            var def = Definition(ref classeId);
            var prefab = def != null && def.prefab != null ? def.prefab : prefabHeros;
            Heros h = null;
            if (prefab != null)
            {
                Vector3 p = pointDepart != null ? pointDepart.position : PointReapparition(Vector3.zero);
                Quaternion r = pointDepart != null ? pointDepart.rotation : Quaternion.LookRotation(-new Vector3(p.x, 0f, p.z).normalized);
                var go = Instantiate(prefab, p, r);
                go.name = "Heros_" + classeId;
                h = go.GetComponent<Heros>();
            }
            Commencer(NouveauJoueur(1, "Joueur", classeId, def), h);
        }

        ClassesJeu.ClasseDef Definition(ref string classeId)
        {
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(classeId) : null;
            if (def == null || def.prefab == null) { classeId = "paladin"; def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(classeId) : null; }
            return def;
        }

        EtatJoueur NouveauJoueur(int id, string nom, string classeId, ClassesJeu.ClasseDef def)
        {
            var b = B;
            return new EtatJoueur { id = id, nom = nom, classe = def != null ? def.nom : "Paladin", classeId = classeId, enduranceMax = b.endurance, endurance = b.endurance };
        }

        /// Le joueur local et son héros entrent en jeu : la partie commence (jour qui précède la nuit de départ).
        void Commencer(EtatJoueur j, Heros h)
        {
            var b = B;
            ClasseChoisie = j.classeId;
            Etat.joueurs.Add(j);
            if (h != null)
            {
                h.Initialiser(this, j);
                h.EcrireEtat(j);
                m_Heros[j.id] = h;
                HerosLocal = h;
                if (cameraJeu != null) cameraJeu.Suivre(h.transform);
            }
            Etat.nuit = Mathf.Clamp(b.nuitDeDepart, 1, b.nuitsPourGagner);
            Etat.duree = 0f;
            Journal("Partie lancée (" + j.classeId + ", nuit de départ " + Etat.nuit + ", vitesse ×" + b.vitesseCycle + ")");
            PartieLancee?.Invoke();
            Passer(b.commencerALaNuit ? Phase.Crepuscule : Phase.Jour);
        }

        // ----------------------------------------------------------------- Multijoueur (Docs/reseau.md)

        /// Identifiant de joueur d'un poste réseau (1 pour l'hôte, comme en solo).
        public static int IdJoueur(ulong clientId) => (int)clientId + 1;

        /// Héros des autres postes (multijoueur ; vide en solo), pour la colonne du HUD.
        public IEnumerable<Heros> HerosDistants { get { foreach (var id in m_Distants.Keys) if (m_Heros.TryGetValue(id, out var h) && h != null) yield return h; } }

        /// Hôte, village chargé chez tous : le héros de chaque joueur du salon (sa classe) apparaît près de Nyxessa,
        /// côte à côte au point de départ ; chaque poste prend le contrôle du sien.
        public void ApparaitreHerosReseau()
        {
            var nm = ReseauJeu.Instance != null ? ReseauJeu.Instance.Reseau : null;
            if (nm == null || !nm.IsServer) return;
            var joueurs = new List<JoueurSalon>(ReseauJeu.JoueursPartie);
            Vector3 p0 = pointDepart != null ? pointDepart.position : PointReapparition(Vector3.zero);
            Quaternion r = pointDepart != null ? pointDepart.rotation : Quaternion.LookRotation(-new Vector3(p0.x, 0f, p0.z).normalized);
            Vector3 cote = r * Vector3.right;
            for (int i = 0; i < joueurs.Count; i++)
            {
                var js = joueurs[i];
                if (!nm.ConnectedClients.ContainsKey(js.clientId)) continue;
                string classeId = js.classeId.ToString();
                var def = Definition(ref classeId);
                if (def == null || def.prefab == null || def.prefab.GetComponent<Unity.Netcode.NetworkObject>() == null)
                {
                    Debug.LogError("[Réseau] préfab réseau manquant pour la classe " + classeId);
                    continue;
                }
                Vector3 p = p0 + cote * ((i - (joueurs.Count - 1) * 0.5f) * 1.8f);
                var go = Instantiate(def.prefab, p, r);
                var hr = go.GetComponent<HerosReseau>();
                hr.NomJoueur.Value = js.pseudo;
                hr.Classe.Value = new Unity.Collections.FixedString32Bytes(classeId);
                go.GetComponent<Unity.Netcode.NetworkObject>().SpawnAsPlayerObject(js.clientId, true);
                ReseauJeu.Journal("héros de " + js.pseudo + " (" + classeId + ") apparu pour le client " + js.clientId);
            }
        }

        /// Le héros de ce poste est apparu (réseau) : la partie commence ici.
        public void LancerReseau(Heros h, string classeId, string pseudo, ulong clientId)
        {
            if (Etat.phase != Phase.Attente || h == null) return;
            var def = Definition(ref classeId);
            Commencer(NouveauJoueur(IdJoueur(clientId), pseudo, classeId, def), h);
        }

        /// Héros d'un autre poste : marionnette (position et animations par le réseau), hors de Etat.joueurs.
        public void AttacherHerosDistant(Heros h, string classeId, string pseudo, ulong clientId)
        {
            if (h == null) return;
            var def = Definition(ref classeId);
            var j = NouveauJoueur(IdJoueur(clientId), pseudo, classeId, def);
            h.DevenirDistant();
            h.Initialiser(this, j);
            m_Heros[j.id] = h;
            m_Distants[j.id] = j;
            Journal("Héros distant : " + pseudo + " (" + classeId + ")");
        }

        public void DetacherHeros(ulong clientId)
        {
            int id = IdJoueur(clientId);
            if (m_Distants.Remove(id)) m_Heros.Remove(id);
        }

        /// Hôte : un joueur a quitté la partie (son héros disparaît avec lui).
        public void JoueurParti(ulong clientId)
        {
            Journal("Joueur " + IdJoueur(clientId) + " a quitté la partie");
            DetacherHeros(clientId);
        }

        /// Vote « prêt » (jour) ou Rejouer (écran de score).
        public void BasculerPret(int joueurId = 1)
        {
            var j = Joueur(joueurId);
            if (j == null) return;
            if (Etat.phase == Phase.Terminee)
            {
                j.pret = !j.pret;
                PretChange?.Invoke(joueurId, j.pret);
                if (TousPrets()) Recharger(true);
                return;
            }
            if (Etat.phase != Phase.Jour) return;
            j.pret = !j.pret;
            AudioBank.Jouer2D(j.pret ? SonsDuJeu.Pret : SonsDuJeu.PretAnnule);
            PretChange?.Invoke(joueurId, j.pret);
            if (TousPrets() && !Etat.comptePret)
            {
                Etat.comptePret = true;
                // Le jour est écourté : il reste le compte à rebours (5 s).
                float reste = Mathf.Min(Etat.TempsRestant, B.compteAReboursPret);
                Etat.tempsPhase = Etat.dureePhase - reste;
                AudioBank.Jouer2D(SonsDuJeu.TousPrets);
            }
            else if (!TousPrets() && Etat.comptePret)
            {
                // Vote annulé pendant le compte à rebours : le jour reprend son cours normal (temps restant gardé).
                Etat.comptePret = false;
            }
        }

        public void QuitterPartie()
        {
            // Multijoueur : on quitte d'abord la session (l'hôte la ferme pour tous), puis retour au menu en solo.
            if (ReseauJeu.Actif && ReseauJeu.Instance.Lobby != null) ReseauJeu.Instance.Lobby.Quitter();
            Recharger(false);
        }

        public void QuitterJeu()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Recharger(bool relancer)
        {
            LancerAuChargement = relancer;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().path : SceneManager.GetActiveScene().name);
        }

        bool TousPrets()
        {
            if (Etat.joueurs.Count == 0) return false;
            foreach (var j in Etat.joueurs) if (!j.pret) return false;
            return true;
        }

        public int JoueursPrets { get { int n = 0; foreach (var j in Etat.joueurs) if (j.pret) n++; return n; } }

        // ----------------------------------------------------------------- Horloge

        void Update()
        {
            if (Etat.phase == Phase.Attente || Etat.phase == Phase.Terminee) return;
            float dt = Time.deltaTime;
            if (m_Chute)
            {
                m_ChuteDepuis += dt;
                if (m_ChuteDepuis >= B.delaiScoreDefaite) Terminer(Resultat.Defaite);
                return;
            }
            Etat.duree += dt;
            Etat.tempsPhase += dt;

            // Alerte avant la nuit (wiki : 15 s avant le crépuscule).
            if (Etat.phase == Phase.Jour && !m_AlerteDonnee && Etat.TempsRestant <= B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle) && Etat.dureePhase > B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle))
            {
                m_AlerteDonnee = true;
                AudioBank.Jouer2D(SonsDuJeu.AlerteNuit);
                AlerteNuit?.Invoke();
            }

            MettreAJourJoueurs(dt);

            if (Etat.tempsPhase >= Etat.dureePhase)
            {
                switch (Etat.phase)
                {
                    case Phase.Jour: Passer(Phase.Crepuscule); break;
                    case Phase.Crepuscule: Passer(Phase.Nuit); break;
                    case Phase.Nuit: Passer(Phase.Aube); break;
                    case Phase.Aube:
                        if (Etat.nuit >= B.nuitsPourGagner) Terminer(Resultat.Victoire);
                        else { Etat.nuit++; Passer(Phase.Jour); }
                        break;
                }
            }
        }

        void Passer(Phase nouvelle)
        {
            var ancienne = Etat.phase;
            Etat.phase = nouvelle;
            Etat.tempsPhase = 0f;
            Etat.dureePhase = B.Duree(nouvelle);
            if (nouvelle == Phase.Jour)
            {
                m_AlerteDonnee = false;
                Etat.comptePret = false;
                foreach (var j in Etat.joueurs) j.pret = false;
            }
            if (nouvelle == Phase.Aube)
            {
                // Wiki : un joueur mort revient au début de la nouvelle journée, même si son délai n'est pas écoulé.
                foreach (var j in Etat.joueurs) if (j.mort) Reapparaitre(j);
                if (B.nyxessaRegenAube > 0f && nyxessa != null) nyxessa.Soigner(nyxessa.pvMax * B.nyxessaRegenAube);
            }
            Journal("Phase " + nouvelle + " (nuit " + Etat.nuit + ", " + Etat.dureePhase.ToString("F1") + " s)");
            PhaseChangee?.Invoke(ancienne, nouvelle);
            if (nouvelle == Phase.Nuit) NuitCommencee?.Invoke(Etat.nuit);
        }

        void Terminer(Resultat r)
        {
            if (Etat.phase == Phase.Terminee) return;
            Etat.resultat = r;
            Etat.nuitAtteinte = r == Resultat.Victoire ? B.nuitsPourGagner : Etat.nuit;
            foreach (var j in Etat.joueurs) j.pret = false;
            var ancienne = Etat.phase;
            Etat.phase = Phase.Terminee;
            m_Chute = false;
            if (r == Resultat.Victoire) AudioBank.Jouer2D(SonsDuJeu.Victoire);
            Journal("Fin de partie : " + r + " (nuit " + Etat.nuitAtteinte + ", " + Etat.duree.ToString("F0") + " s)");
            PhaseChangee?.Invoke(ancienne, Phase.Terminee);
            PartieTerminee?.Invoke();
        }

        /// Test : fin forcée.
        public void ForcerFin(Resultat r) => Terminer(r);

        /// Test : saute à une phase (nuit donnée).
        public void ForcerPhase(Phase p, int nuit)
        {
            Etat.nuit = Mathf.Clamp(nuit, 1, B.nuitsPourGagner);
            Passer(p);
        }

        // ----------------------------------------------------------------- Nyxessa

        void OnNyxessaTouchee(InfoDegats info, float reel)
        {
            Etat.nyxessa.pv = nyxessa.Pv;
            Etat.nyxessa.dernierCoup = Time.time;
            NyxessaTouchee?.Invoke(reel, info.point);
        }

        void OnNyxessaDetruite()
        {
            if (!EnCours) return;
            Etat.nyxessa.pv = 0f;
            Etat.nyxessa.detruite = true;
            m_Chute = true;
            m_ChuteDepuis = 0f;
            Journal("Nyxessa est détruite (nuit " + Etat.nuit + ")");
            NyxessaDetruite?.Invoke();
        }

        // ----------------------------------------------------------------- Joueurs

        public EtatJoueur Joueur(int id)
        {
            foreach (var j in Etat.joueurs) if (j.id == id) return j;
            return null;
        }

        public Heros HerosDe(int id) => m_Heros.TryGetValue(id, out var h) ? h : null;
        public IEnumerable<Heros> TousLesHeros => m_Heros.Values;

        void MettreAJourJoueurs(float dt)
        {
            foreach (var j in Etat.joueurs)
            {
                var h = HerosDe(j.id);
                if (h != null) h.EcrireEtat(j);
                if (!j.mort) continue;
                j.reapparitionRestante = Mathf.Max(0f, j.reapparitionRestante - dt);
                if (j.reapparitionRestante <= 0f && nyxessa != null && !nyxessa.Mort) Reapparaitre(j);
            }
        }

        /// Appelé par le héros à sa mort.
        public void SignalerMort(int joueurId)
        {
            var j = Joueur(joueurId);
            if (j == null || j.mort) return;
            j.mort = true;
            // Wiki : chaque mort allonge le délai (8 s + 4 s par mort précédente dans la partie).
            j.reapparitionRestante = B.DelaiReapparition(j.score.morts);
            j.score.morts++;
            Journal("Joueur " + joueurId + " mort (" + j.score.morts + "e mort, réapparition dans " + j.reapparitionRestante.ToString("F0") + " s)");
            JoueurMort?.Invoke(joueurId);
        }

        void Reapparaitre(EtatJoueur j)
        {
            if (!j.mort) return;
            j.mort = false;
            j.reapparitionRestante = 0f;
            var h = HerosDe(j.id);
            if (h != null) h.Reapparaitre(PointReapparition(h.transform.position));
            Journal("Joueur " + j.id + " réapparaît");
            JoueurReapparu?.Invoke(j.id);
        }

        /// Point de réapparition libre le plus proche de la position donnée (autour de Nyxessa).
        public Vector3 PointReapparition(Vector3 depuis)
        {
            if (pointsReapparition == null || pointsReapparition.Length == 0) return new Vector3(0f, 0f, -10f);
            Transform meilleur = null;
            float d = float.MaxValue;
            foreach (var t in pointsReapparition)
            {
                if (t == null) continue;
                bool occupe = Physics.CheckSphere(t.position + Vector3.up, 0.6f, ~0, QueryTriggerInteraction.Ignore);
                float dd = (t.position - depuis).sqrMagnitude + (occupe ? 10000f : 0f);
                if (dd < d) { d = dd; meilleur = t; }
            }
            return meilleur != null ? meilleur.position : Vector3.zero;
        }

        // ----------------------------------------------------------------- Score

        public void CompterDegats(int joueurId, float montant, bool critique)
        {
            var j = Joueur(joueurId);
            if (j == null) return;
            j.score.degatsInfliges += montant;
            if (critique) j.score.coupsCritiques++;
        }

        public void CompterTue(int joueurId)
        {
            var j = Joueur(joueurId);
            if (j != null) j.score.ennemisTues++;
        }

        public void CompterDegatsEvites(int joueurId, float montant)
        {
            var j = Joueur(joueurId);
            if (j != null && montant > 0f) j.score.degatsEvitesNyxessa += montant;
        }

        public void CompterSoins(int joueurId, float montant)
        {
            var j = Joueur(joueurId);
            if (j != null && montant > 0f) j.score.soinsProdigues += montant;
        }

        public void Journal(string texte)
        {
            if (B.journal) Debug.Log("[Partie " + Etat.duree.ToString("F1") + " s] " + texte);
        }
    }
}
