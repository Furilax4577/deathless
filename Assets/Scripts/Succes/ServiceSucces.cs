using System;
using System.Collections.Generic;
using Deathless.Jeu;
using Deathless.Reseau;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Succes
{
    /// Succès du joueur local (Wiki/pages/succes.md ; implémentation du 01/10/2026). Le jeu signale ses faits par les
    /// méthodes ci-dessous (appels d'une ligne posés dans le code de jeu) ; SuiviSucces écoute en plus les événements de
    /// Partie et sonde ce qui n'a pas d'événement (caisse, eau, brûlures). La plateforme (ISuccesPlateforme) garde l'état :
    /// SuccesLocal maintenant, Steam plus tard.
    ///
    /// Multijoueur : un succès d'équipe est décidé par l'autorité (hôte, ou ce poste en solo) puis diffusé à tous les
    /// postes présents (Equipe → PartieReseau.SuccesEquipe) ; un succès personnel est compté sur le poste du joueur. Quand
    /// seul l'hôte voit le fait (butin accordé, ennemi tué, soin d'aura), il l'envoie au poste du joueur (PourJoueur →
    /// PartieReseau.FaitPersonnel).
    ///
    /// Garde-fou : rien n'est débloqué ni compté pendant les scénarios de dev et le banc (SuspendreDev, appelé par chaque
    /// classe de Deathless.Jeu.Dev à sa première utilisation, jusqu'à la fin du Play) ni dans une partie de test
    /// (MarquerPartieTest : réglages de développement de GameBalance, Partie.ForcerPhase / ForcerFin). `AutoriserTests`
    /// lève ce garde-fou pour vérifier les succès eux-mêmes (outil de dev, jamais posé par le jeu).
    public static class ServiceSucces
    {
        static ISuccesPlateforme s_Plateforme;
        static VueListe s_Vue;
        static string s_SuspenduDev, s_PartieTest;

        public static ISuccesPlateforme Plateforme => s_Plateforme;
        /// Outil de vérification : lève le garde-fou des scénarios et des parties de test (Play en cours seulement).
        public static bool AutoriserTests;
        /// Raison pour laquelle les succès sont suspendus en ce moment (null : ils comptent).
        public static string Suspension => AutoriserTests ? null : s_SuspenduDev ?? s_PartieTest;

        public static event Action<DefSucces> Debloque;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReinitialiserStatiques()
        {
            s_Plateforme = null;
            s_Vue = null;
            s_SuspenduDev = s_PartieTest = null;
            AutoriserTests = false;
            Debloque = null;
            NouvellePartie();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Demarrer()
        {
            if (s_Plateforme != null) return;
            s_Plateforme = new SuccesLocal(Profil());
            s_Vue = new VueListe();
            DonneesUI.Succes = s_Vue;
            var go = new GameObject("SuiviSucces");
            go.hideFlags = HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SuiviSucces>();
        }

        /// Profil de test (`-deathless-profil=…`, lu comme LobbyReseau) : un fichier de succès par profil.
        static string Profil()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.StartsWith("-deathless-profil=")) return a.Substring("-deathless-profil=".Length);
            return "";
        }

        // ================================================================= Garde-fou

        /// Scénarios de dev, banc des classes, tournage : plus rien ne compte jusqu'à la fin du Play.
        public static void SuspendreDev(string raison)
        {
            if (s_SuspenduDev != null) return;
            s_SuspenduDev = raison;
            Debug.Log("[Succès] suspendus : " + raison + " (outil de dev)");
        }

        /// Partie de test (réglages de développement, phase ou fin forcée) : plus rien ne compte jusqu'à la partie suivante.
        public static void MarquerPartieTest(string raison)
        {
            if (s_PartieTest != null) return;
            s_PartieTest = raison;
            Debug.Log("[Succès] partie de test (" + raison + ") : rien ne sera débloqué");
        }

        /// Nouvelle partie (Partie.PartieLancee) : état par partie remis à zéro.
        internal static void NouvellePartie()
        {
            s_PartieTest = null;
            s_Cloues.Clear();
            s_SerieFurtive = 0;
            s_Executions.Clear();
            s_IvresseVerres = 0;
            s_MorgrimMort = -99f;
        }

        // ================================================================= Déblocage et statistiques

        /// Débloque un succès sur ce poste. Vrai s'il vient d'être débloqué.
        public static bool Debloquer(string id)
        {
            var def = CatalogueSucces.Trouver(id);
            if (def == null || s_Plateforme == null) { if (def == null) Debug.LogWarning("[Succès] inconnu : " + id); return false; }
            if (Suspension != null) { Journal("ignoré (" + Suspension + ") : " + id); return false; }
            if (!s_Plateforme.Debloquer(id)) return false;
            s_Plateforme.Stocker();
            Debug.Log("[Succès] débloqué : " + def.Id + " « " + def.Nom + " »");
            Debloque?.Invoke(def);
            s_Vue?.Signaler(def);
            return true;
        }

        public static bool EstDebloque(string id) => s_Plateforme != null && s_Plateforme.EstDebloque(id, out _);

        /// Ajoute `n` à une statistique (succès à cumul).
        public static void Ajouter(string stat, int n = 1)
        {
            if (s_Plateforme == null || n == 0 || string.IsNullOrEmpty(stat)) return;
            if (Suspension != null) return;
            s_Plateforme.ReglerStat(stat, s_Plateforme.LireStat(stat) + n);
            Evaluer(stat);
        }

        /// Ajoute des bits à une statistique masque (classes jouées, versions de Morgrim).
        public static void AjouterBits(string stat, int bits)
        {
            if (s_Plateforme == null || bits == 0 || Suspension != null) return;
            s_Plateforme.ReglerStat(stat, s_Plateforme.LireStat(stat) | bits);
            Evaluer(stat);
        }

        static void Evaluer(string stat)
        {
            foreach (var d in CatalogueSucces.Tous)
                if (d.Stat == stat && !EstDebloque(d.Id) && Progression(d) >= d.Seuil) Debloquer(d.Id);
        }

        public static int Progression(DefSucces d)
        {
            if (s_Plateforme == null || d == null || string.IsNullOrEmpty(d.Stat)) return EstDebloque(d?.Id) ? 1 : 0;
            int v = s_Plateforme.LireStat(d.Stat);
            return d.Masque ? CatalogueSucces.Bits(v) : v;
        }

        /// Pousse ce qui a changé (fin de nuit, retour au menu, sortie du jeu).
        public static void Stocker() => s_Plateforme?.Stocker();

        // ================================================================= Équipe (l'autorité décide) et routage

        static Partie P => Partie.Instance;
        static bool Autorite => !Partie.ClientReseau;

        /// Autorité : fait d'équipe (succès `ACH_…` ou fait composé « NUIT », « MORGRIM ») appliqué ici puis diffusé à tous
        /// les postes présents. Ignoré chez un client (il le recevra de l'hôte).
        public static void Equipe(string fait, int valeur = 0, Vector3 point = default)
        {
            if (!Autorite || string.IsNullOrEmpty(fait)) return;
            if (Suspension != null) { Journal("fait d'équipe ignoré (" + Suspension + ") : " + fait); return; }
            AppliquerEquipe(fait, valeur, point);
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.SuccesEquipe(fait, valeur, point);
        }

        /// Client : fait d'équipe reçu de l'hôte (PartieReseau).
        public static void RecevoirEquipe(string fait, int valeur, Vector3 point) => AppliquerEquipe(fait, valeur, point);

        static void AppliquerEquipe(string fait, int valeur, Vector3 point)
        {
            Journal("fait d'équipe : " + fait + (valeur != 0 ? " " + valeur : ""));
            switch (fait)
            {
                case FaitNuit:
                    // Nuit tenue (valeur : numéro de la nuit) : première aube, et la classe de ce poste pour Touche-à-tout.
                    Debloquer("ACH_NUIT_1");
                    var j = P != null ? P.JoueurLocal : null;
                    if (j != null) AjouterBits(CatalogueSucces.StatClasses, CatalogueSucces.BitClasse(j.classeId));
                    break;
                case FaitMorgrim:
                    // Morgrim vaincu (valeur : bit de sa version, point : son cadavre).
                    Debloquer("ACH_MORGRIM");
                    AjouterBits(CatalogueSucces.StatMorgrim, valeur);
                    s_MorgrimMort = Time.time;
                    s_MorgrimPoint = point;
                    break;
                default:
                    if (fait.StartsWith("ACH_")) Debloquer(fait);
                    break;
            }
        }

        public const string FaitNuit = "NUIT", FaitMorgrim = "MORGRIM";

        /// Autorité : fait personnel du joueur `joueurId` (« ACH_… » ou « STAT_… » + valeur), appliqué ici si c'est le
        /// joueur de ce poste, sinon envoyé à son poste.
        public static void PourJoueur(int joueurId, string fait, int valeur = 1)
        {
            var p = P;
            if (p == null || joueurId <= 0) return;
            var local = p.JoueurLocal;
            if (local != null && local.id == joueurId) { RecevoirPersonnel(fait, valeur); return; }
            if (ReseauJeu.EnPartie && ReseauJeu.Autorite) PartieReseau.Instance?.FaitPersonnel((ulong)(joueurId - 1), fait, valeur);
        }

        /// Fait personnel reçu de l'hôte (ou appliqué ici).
        public static void RecevoirPersonnel(string fait, int valeur)
        {
            if (string.IsNullOrEmpty(fait)) return;
            if (fait.StartsWith("STAT_")) Ajouter(fait, valeur);
            else Debloquer(fait);
        }

        static bool EstLocal(Heros h) => h != null && !h.Distant && P != null && h == P.HerosLocal;

        /// Coup qui achève (estimation chez un client, dont l'ennemi est tenu par l'hôte : le coup porte au moins les PV
        /// restants vus ici ; exact en solo et chez l'hôte).
        public static bool Acheve(Sante s, float pvAvant, float reel) => s != null && pvAvant > 0f && (s.Mort || reel >= pvAvant - 0.01f);

        // ================================================================= Faits du jeu (autorité)

        /// Autorité : un squelette meurt (Squelette.OnTue). Morgrim (version, cri), boss après une longue nuit, voleur tué
        /// par sa proie, missile de Nyxessa qui sauve un joueur.
        public static void EnnemiTue(Squelette sq, InfoDegats info)
        {
            if (sq == null || !Autorite) return;
            var p = P;
            if (sq.type == TypeEnnemi.Golem || sq.type == TypeEnnemi.Necromancien)
            {
                if (SuiviSucces.AubeRetenue > 60f) Equipe("ACH_NUIT_LONGUE");
                if (sq.type == TypeEnnemi.Golem)
                {
                    int bit = sq is MorgrimMassue ? 1 : sq is MorgrimMartache ? 2 : 0;
                    Equipe(FaitMorgrim, bit, sq.transform.position);
                    if (sq is MorgrimVariant mv && !mv.ACrie) Equipe("ACH_MORGRIM_MUET");
                }
            }
            if (sq is Voleur v && v.Proie != null && info.sourceId > 0 && v.Proie.Id == info.sourceId)
                PourJoueur(info.sourceId, "ACH_VOLEUR_VOLE");
            // Missile de Nyxessa (équipe Relique, à distance ; le renvoi du bouclier n'est pas à distance) qui tue l'ennemi
            // en train de frapper un joueur à moins de 10 % de vie.
            if (info.equipeSource == Jeu.Equipe.Relique && info.aDistance)
            {
                var h = sq.CibleJoueur;
                if (h != null && h.Vivant && h.Sante != null && h.Sante.Ratio < 0.1f
                    && (sq.EnPreparation || Vector3.Distance(h.transform.position, sq.transform.position) < 3.5f))
                    PourJoueur(h.Id, "ACH_SAUVE_PAR_NYX");
            }
        }

        /// Autorité : un éclat de Nyxar se brise (0 : couronne, 1 : grimoire).
        public static void EclatBrise(int index)
        {
            if (!Autorite) return;
            if (index == 0) { SuiviSucces.CouronneBrisee = true; Equipe("ACH_ECLAT_COURONNE"); }
            else
            {
                Equipe("ACH_ECLAT_GRIMOIRE");
                if (!SuiviSucces.CouronneBrisee) Equipe("ACH_ECLATS_INVERSE");
            }
        }

        /// Autorité : soin d'aura du paladin `p` qui a soigné `allies` alliés (ClassePaladin.SoignerAllies).
        public static void SoinAura(Heros p, int allies)
        {
            if (p != null && allies >= 3) PourJoueur(p.Id, "ACH_SOIN_TRIPLE");
        }

        /// Autorité : butin accordé (coffre ou grand coffre) au joueur `joueurId`.
        public static void CoffreOuvert(int joueurId) => PourJoueur(joueurId, CatalogueSucces.StatCoffres, 1);

        /// Autorité : sac d'un autre joueur ramassé par `joueurId`.
        public static void SacAllie(int joueurId) => PourJoueur(joueurId, "ACH_SAC_ALLIE");

        // ================================================================= Faits du joueur local

        /// Retour dans la partie après une coupure (même code, même classe : Partie.LancerReseau).
        public static void Reconnexion() => Debloquer("ACH_RECONNEXION");

        /// Hôte : réseau perdu, la partie continue en solo (Partie.ContinuerSeul) ; la prochaine nuit tenue débloque.
        public static void HoteSeul() => SuiviSucces.HoteSeul = true;

        /// Portail de retour du donjon passé (DonjonJeu.Passer) : moins de 3 s avant la fermeture du donjon (crépuscule).
        public static void PortailRetour()
        {
            var p = P;
            if (p != null && p.Etat.phase == Phase.Jour && p.Etat.TempsRestant < 3f) Debloquer("ACH_JUSTE_A_TEMPS");
        }

        /// Parade parfaite du paladin local (ParadeParfaite.Commencer).
        public static void ParadeParfaite(Heros h) { if (EstLocal(h)) Ajouter(CatalogueSucces.StatParades); }

        /// Viking local : nombre d'ennemis différents touchés par la tournante en cours.
        public static void Tournante(Heros h, int touches) { if (touches >= 10 && EstLocal(h)) Debloquer("ACH_TOURNANTE_10"); }

        /// Mage local : ennemis tués par une grande boule de feu.
        public static void GrandeBoule(Heros h, int tues) { if (tues >= 6 && EstLocal(h)) Debloquer("ACH_GRANDE_BOULE_6"); }

        /// Mage local : ennemis différents entrés dans le mur de flammes posé.
        public static void MurDeFlammes(Heros h, int traverses) { if (traverses >= 20 && EstLocal(h)) Debloquer("ACH_MUR_20"); }

        static readonly Dictionary<Squelette, float> s_Cloues = new Dictionary<Squelette, float>();

        /// Rôdeur local : flèche arrivée sur `sq` (tête, achevé, étourdissement de pleine charge).
        public static void FlecheRodeur(Heros h, Squelette sq, bool tete, bool acheve, float etourdi)
        {
            if (!EstLocal(h)) return;
            if (tete) Ajouter(CatalogueSucces.StatTetes);
            if (sq == null) return;
            if (acheve)
            {
                if (s_Cloues.TryGetValue(sq, out float jusqua) && Time.time <= jusqua) Debloquer("ACH_CLOUE");
                s_Cloues.Remove(sq);
            }
            else if (etourdi > 0f && sq.elite) s_Cloues[sq] = Time.time + etourdi;
        }

        static int s_SerieFurtive;
        static readonly Queue<float> s_Executions = new Queue<float>();

        /// Assassin local : coup de dague porté (dos, furtif au coup, exécution, ennemi achevé).
        public static void Dague(Heros h, bool dos, bool furtif, bool execution, bool acheve)
        {
            if (!EstLocal(h)) return;
            if (dos) Ajouter(CatalogueSucces.StatDos);
            if (execution)
            {
                s_Executions.Enqueue(Time.time);
                while (s_Executions.Count > 0 && Time.time - s_Executions.Peek() > 10f) s_Executions.Dequeue();
                if (s_Executions.Count >= 4) Debloquer("ACH_EXECUTIONS_4");
            }
            if (!furtif) { s_SerieFurtive = 0; return; }
            if (acheve && ++s_SerieFurtive >= 5) Debloquer("ACH_FURTIF_5");
        }

        /// Assassin local repéré ou touché : la série furtive est rompue.
        public static void FurtifRompu(Heros h) { if (EstLocal(h)) s_SerieFurtive = 0; }

        static int s_IvresseVerres;

        /// Joueur local : un verre (bière ou tournée). « Ivresse maximale » : trois verres de suite sans dessoûler.
        public static void Boire(bool dejaIvre)
        {
            s_IvresseVerres = dejaIvre ? s_IvresseVerres + 1 : 1;
            if (s_IvresseVerres >= 3) Debloquer("ACH_IVRESSE");
        }

        static float s_MorgrimMort = -99f;
        static Vector3 s_MorgrimPoint;

        /// Joueur local : emote lancée ; près du cadavre de Morgrim (6 m) dans les 30 s qui suivent sa chute.
        public static void Emote(Heros h)
        {
            if (!EstLocal(h) || Time.time - s_MorgrimMort > 30f) return;
            Vector3 d = h.transform.position - s_MorgrimPoint; d.y = 0f;
            if (d.magnitude <= 6f) Debloquer("ACH_EMOTE_MORGRIM");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void Journal(string texte) => Debug.Log("[Succès] " + texte);

        // ================================================================= Vue (écran Succès, bannière)

        sealed class Vue : ISuccesVue
        {
            readonly DefSucces d;
            public Vue(DefSucces def) { d = def; }
            public string Id => d.Id;
            public string Nom => d.Nom;
            public string Description => d.Description;
            public string Categorie => d.Categorie;
            public bool Cache => d.Cache;
            public bool Debloque => EstDebloque(d.Id);
            public DateTime? Date => s_Plateforme != null && s_Plateforme.EstDebloque(d.Id, out var t) ? t : (DateTime?)null;
            public int Progression => Math.Min(ServiceSucces.Progression(d), d.Seuil);
            public int Seuil => d.Stat != null ? d.Seuil : 1;
            public string Icone => d.Icone;
        }

        sealed class VueListe : IListeSucces
        {
            readonly List<ISuccesVue> m_Tous = new List<ISuccesVue>();
            readonly Dictionary<string, ISuccesVue> m_Index = new Dictionary<string, ISuccesVue>();
            public VueListe()
            {
                foreach (var d in CatalogueSucces.Tous) { var v = new Vue(d); m_Tous.Add(v); m_Index[d.Id] = v; }
            }
            public IReadOnlyList<ISuccesVue> Tous => m_Tous;
            public int NombreDebloques { get { int n = 0; foreach (var v in m_Tous) if (v.Debloque) n++; return n; } }
            public event Action<ISuccesVue> Debloque;
            public void Signaler(DefSucces d) { if (m_Index.TryGetValue(d.Id, out var v)) Debloque?.Invoke(v); }
        }
    }
}
