using System;
using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Point de branchement entre la partie et les écrans de l'agent ui (Docs/ui-v01.md) : implémente IEtatPartie,
    /// IEtatJoueur, IScoreFin et ICommandesPartie en lisant Partie.Etat (aucune logique de jeu ici) et s'enregistre dans
    /// DonneesUI. Au chargement : commandes seules (menu principal) ; partie lancée : toutes les sources (HUD) ; fin :
    /// Phase = Terminee puis PartieTerminee (écran de score). Gère aussi le curseur (caché et verrouillé en jeu).
    public class HudPresenter : MonoBehaviour, IEtatPartie, IEtatJoueur, IEtatJoueurClasse, IScoreFin, ICommandesPartie, IClassesJouables, IEtatEquipe, IEtatMissiles
    {
        public static readonly Color TeintePaladin = new Color32(0xd9, 0xb2, 0x64, 0xff);

        Partie m_Partie;
        Camera m_Camera;
        List<ICompetenceHud> m_Competences;
        List<ILigneScore> m_Lignes;

        Partie P => m_Partie != null ? m_Partie : (m_Partie = Partie.Instance);
        EtatJoueur J => P != null ? P.JoueurLocal : null;
        Heros H => P != null ? P.HerosLocal : null;

        void OnEnable()
        {
            DonneesUI.Enregistrer(null, null, null, this);
            DonneesUI.Personnage = new MenuPersonnage();   // menu du personnage (Tab / Y)
        }

        void Start()
        {
            m_Camera = Camera.main;
            if (DonneesUI.ApercuClasse == null) ApercuClasse.Creer();   // aperçu 3D de l'écran de choix de classe
            if (P == null) return;
            P.PartieLancee += () => { ConstruireEmplacements(); DonneesUI.Enregistrer(this, this, this, this); };
            P.NuitCommencee += n => NuitCommencee?.Invoke(n);
            P.PointCompetenceGagne += n => AudioBank.Jouer2D(SonsDuJeu.PointGagne, 0.7f);
            P.NyxessaTouchee += (d, p) => NyxessaFrappee?.Invoke();
            P.PartieTerminee += () => PartieTerminee?.Invoke();
            m_Lignes = new List<ILigneScore> { new Ligne(this) };
            ConstruireEmplacements();
            if (P.Etat.phase != Jeu.Phase.Attente) DonneesUI.Enregistrer(this, this, this, this);
        }

        /// Emplacements du HUD de la classe du joueur local (RT, LT, LB, RB), noms tirés du catalogue de l'écran de choix.
        void ConstruireEmplacements()
        {
            var classe = ClassesJouables.Trouver(J != null ? J.classeId : Partie.ClasseChoisie) ?? ClassesJouables.Trouver(ClassesJouables.ParDefaut);
            m_Competences = new List<ICompetenceHud>();
            for (int i = 0; i < 4; i++)
            {
                var a = classe != null && classe.Actions.Count > i ? classe.Actions[i] : null;
                string nom = a != null ? a.Nom : null;
                m_Competences.Add(new Competence(a != null ? a.Action : "", nom, Abreviation(nom), this, i));
            }
        }

        static string Abreviation(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return "";
            string n = nom.Trim();
            return n.Length >= 2 ? n.Substring(0, 2) : n;
        }

        IClasseJouable ClasseLocale => ClassesJouables.Trouver(J != null ? J.classeId : Partie.ClasseChoisie);

        void OnDisable()
        {
            if (ReferenceEquals(DonneesUI.Commandes, this)) DonneesUI.Enregistrer(null, null, null, null);
        }

        void LateUpdate()
        {
            // Curseur caché et verrouillé en jeu (carte Gameplay active, partie en cours) ; libre dans les menus.
            var actions = UnityEngine.InputSystem.InputSystem.actions;
            bool jeu = P != null && P.EnCours && actions != null && actions.FindActionMap("Gameplay").enabled;
            var voulu = jeu ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != voulu) Cursor.lockState = voulu;
            if (Cursor.visible == jeu) Cursor.visible = !jeu;
        }

        // ----------------------------------------------------------------- IEtatPartie

        public PhasePartie Phase
        {
            get
            {
                if (P == null) return PhasePartie.Jour;
                switch (P.Etat.phase)
                {
                    case Jeu.Phase.Crepuscule: return PhasePartie.Crepuscule;
                    case Jeu.Phase.Nuit: return PhasePartie.Nuit;
                    case Jeu.Phase.Aube: return PhasePartie.Aube;
                    case Jeu.Phase.Terminee: return PhasePartie.Terminee;
                    default: return PhasePartie.Jour;
                }
            }
        }
        public int NumeroNuit => P != null ? P.Etat.nuit : 1;
        public float TempsRestantPhase => P != null ? P.Etat.TempsRestant : 0f;
        public float VieNyxessa => P != null ? P.Etat.nyxessa.pv : 0f;
        public float VieMaxNyxessa => P != null ? Mathf.Max(1f, P.Etat.nyxessa.pvMax) : 1f;
        /// Bouclier du sorcier (levé la nuit) : solidité restante et encaissement du palier.
        public float Bouclier => BouclierNyxessa.Instance != null ? BouclierNyxessa.Instance.Vie : 0f;
        public float BouclierMax => BouclierNyxessa.Instance != null ? BouclierNyxessa.Instance.VieMax : 0f;
        /// Caisse commune : or des vagues (règle provisoire sans donjon, wiki : deroule).
        public int OrEquipe => P != null ? P.Etat.orEquipe : 0;
        public bool VoteActif => P != null && P.EnCours && P.Etat.phase == Jeu.Phase.Jour;
        // Multijoueur : le vote est compté chez l'hôte (PartieReseau), pour tous les postes.
        static Deathless.Reseau.PartieReseau R => Deathless.Reseau.ReseauJeu.EnPartie ? Deathless.Reseau.PartieReseau.Instance : null;
        public int JoueursPrets => R != null ? R.Prets.Value : P != null ? P.JoueursPrets : 0;
        public int JoueursTotal => R != null ? Mathf.Max(1, R.Joueurs.Value) : P != null ? Mathf.Max(1, P.Etat.joueurs.Count) : 1;
        public float AngleNyxessa
        {
            get
            {
                if (m_Camera == null) m_Camera = Camera.main;
                if (m_Camera == null || P == null || P.nyxessa == null) return 0f;
                Vector3 d = P.nyxessa.transform.position - m_Camera.transform.position; d.y = 0f;
                Vector3 f = m_Camera.transform.forward; f.y = 0f;
                if (d.sqrMagnitude < 0.01f || f.sqrMagnitude < 0.01f) return 0f;
                return Vector3.SignedAngle(f, d, Vector3.up);
            }
        }
        public event Action<int> NuitCommencee;
        public event Action NyxessaFrappee;
        public event Action PartieTerminee;

        // ----------------------------------------------------------------- IEtatMissiles (compteur du HUD)

        /// Missiles de Nyxessa : stock (chez un client, recopié de l'hôte par Partie.SuivreHote, recharge extrapolée).
        public int MissilesDisponibles => P != null ? P.Etat.nyxessa.stock : 0;
        public int MissilesMax => P != null && P.B != null ? GameBalance.AuPalier(P.B.missilesStockPaliers, P.Etat.nyxessa.palierMissiles) : 0;
        public float ChargeProchainMissile
        {
            get
            {
                if (P == null || P.B == null || MissilesDisponibles >= MissilesMax) return 1f;
                float duree = GameBalance.AuPalier(P.B.missileRegenerationPaliers, P.Etat.nyxessa.palierMissiles);
                return duree > 0f ? Mathf.Clamp01(P.Etat.nyxessa.regeneration / duree) : 1f;
            }
        }

        // ----------------------------------------------------------------- IEtatEquipe (multijoueur)

        readonly List<IAllie> m_Allies = new List<IAllie>();

        /// Autres joueurs de la partie réseau (héros apparus, sauf le sien), dans l'ordre d'apparition ; vide en solo.
        public IReadOnlyList<IAllie> Allies
        {
            get
            {
                m_Allies.Clear();
                foreach (var h in Deathless.Reseau.HerosReseau.Tous) if (h != null && !h.IsOwner) m_Allies.Add(h);
                return m_Allies;
            }
        }

        // ----------------------------------------------------------------- IEtatJoueur

        /// Pseudo du joueur local (Options > Jeu, écran du premier lancement).
        public string Nom => DonneesUI.Profil.PseudoDefini ? DonneesUI.Profil.Pseudo : J != null ? J.nom : "Joueur";
        public string Classe => J != null ? J.classe : "Paladin";
        public Color TeinteClasse => ClasseLocale != null ? ClasseLocale.Teinte : TeintePaladin;
        public float Vie => J != null ? J.pv : 0f;
        public float VieMax => J != null ? Mathf.Max(1f, J.pvMax) : 1f;
        public float Endurance => J != null ? J.endurance : 0f;
        public float EnduranceMax => J != null ? Mathf.Max(1f, J.enduranceMax) : 1f;
        public bool EstMort => J != null && J.mort;
        public float TempsAvantReapparition => J != null ? J.reapparitionRestante : 0f;
        public IReadOnlyList<ICompetenceHud> Competences => m_Competences;
        public string InviteInteraction
        {
            get
            {
                var h = P != null ? P.HerosLocal : null;
                if (h == null) return null;
                PointInteraction.Courant(h, out string invite);
                return invite;
            }
        }
        public bool EstPret => J != null && J.pret;

        // ----------------------------------------------------------------- IScoreFin

        public ResultatPartie Resultat => P != null && P.Etat.resultat == Jeu.Resultat.Victoire ? ResultatPartie.Victoire : ResultatPartie.Defaite;
        public int NuitAtteinte => P != null ? P.Etat.nuitAtteinte : 1;
        public float DureeSecondes => P != null ? P.Etat.duree : 0f;
        public int OrTotal
        {
            get
            {
                if (P == null) return 0;
                // Or de l'équipe : la caisse commune, ou au moins la somme de l'or rapporté par les joueurs.
                int rapporte = 0;
                foreach (var j in P.Etat.joueurs) rapporte += j.score.orRapporte;
                return Mathf.Max(P.Etat.orEquipe, rapporte);
            }
        }
        readonly List<ILigneScore> m_LignesReseau = new List<ILigneScore>();

        /// Solo : le joueur local ; multijoueur : tous les joueurs de la partie, tels que l'hôte les tient.
        public IReadOnlyList<ILigneScore> Joueurs
        {
            get
            {
                var r = R;
                if (r == null || r.Scores.Count == 0) return m_Lignes;
                m_LignesReseau.Clear();
                foreach (var s in r.Scores) m_LignesReseau.Add(new LigneReseau(s));
                return m_LignesReseau;
            }
        }

        sealed class LigneReseau : ILigneScore
        {
            readonly Deathless.Reseau.ScoreReseau m_S;
            readonly IClasseJouable m_C;
            public LigneReseau(Deathless.Reseau.ScoreReseau s) { m_S = s; m_C = ClassesJouables.Trouver(s.classeId.ToString()); }
            public string Nom => m_S.pseudo.ToString();
            public string Classe => m_C != null ? m_C.Nom : m_S.classeId.ToString();
            public Color TeinteClasse => m_C != null ? m_C.Teinte : TeintePaladin;
            public bool EstLocal => m_S.clientId == Deathless.Reseau.ReseauJeu.IdLocal;
            public int OrRapporte => m_S.or;
            public int DegatsInfliges => m_S.degats;
            public int EnnemisTues => m_S.tues;
            public int Morts => m_S.morts;
            public int CoupsCritiques => m_S.critiques;
            public int DegatsEvitesNyxessa => m_S.evites;
            public int SoinsProdigues => m_S.soins;
        }
        public bool EstPretLocal => EstPret;

        // ----------------------------------------------------------------- ICommandesPartie

        public void LancerSolo() { if (P != null) P.LancerSolo(ClassesJouables.DerniereJouee); }

        // ----------------------------------------------------------------- IClassesJouables (écran de choix de classe)

        public IReadOnlyList<IClasseJouable> Classes => ClassesJouables.Catalogue;

        /// Lance la partie avec la classe choisie (paladin, mage, rodeur, assassin, viking).
        public void LancerSolo(string classeId) { if (P != null) P.LancerSolo(classeId); }

        // ----------------------------------------------------------------- IEtatJoueurClasse (jauge, furtif)

        public JaugeClasse Jauge => H != null && H.Classe != null ? H.Classe.Jauge : JaugeClasse.Aucune;
        public float ValeurJauge => J != null ? J.jauge : 0f;
        public float JaugeMax => J != null ? Mathf.Max(1f, J.jaugeMax) : 1f;
        public bool Furtif => J != null && J.furtif;
        public void BasculerPret() { if (P != null) P.BasculerPret(J != null ? J.id : 1); }
        public void QuitterPartie() { if (P != null) P.QuitterPartie(); }
        public void QuitterJeu() { if (P != null) P.QuitterJeu(); }

        // ----------------------------------------------------------------- Emplacements et lignes

        class Competence : ICompetenceHud
        {
            readonly HudPresenter m_H; readonly int m_Index;
            public Competence(string action, string nom, string abr, HudPresenter h, int index) { Action = action; Nom = nom; Abreviation = abr; m_H = h; m_Index = index; }
            public string Action { get; }
            public string Nom { get; }
            public Texture2D Icone => null;
            public string Abreviation { get; }
            EtatEmplacement Lire(out float restant, out float total)
            {
                restant = total = 0f;
                var h = m_H.H;
                if (h == null || h.Classe == null) return EtatEmplacement.Indisponible;
                return h.Classe.Emplacement(m_Index, out restant, out total);
            }
            public EtatCompetence Etat
            {
                get
                {
                    var h = m_H.H;
                    var e = Lire(out _, out _);
                    if (e == EtatEmplacement.Vide || h == null || !h.Vivant) return EtatCompetence.Indisponible;
                    switch (e)
                    {
                        case EtatEmplacement.Actif: return EtatCompetence.Active;
                        case EtatEmplacement.Recharge: return EtatCompetence.Recharge;
                        case EtatEmplacement.Indisponible: return EtatCompetence.Indisponible;
                        default: return EtatCompetence.Prete;
                    }
                }
            }
            public float RechargeRestante { get { Lire(out float r, out _); return r; } }
            public float RechargeTotale { get { Lire(out _, out float t); return t; } }
        }

        /// Ligne de score du joueur local : les sept catégories de ILigneScore, lues dans ScoreJoueur.
        class Ligne : ILigneScore
        {
            readonly HudPresenter m_H;
            public Ligne(HudPresenter h) { m_H = h; }
            ScoreJoueur S => m_H.J != null ? m_H.J.score : new ScoreJoueur();
            public string Nom => m_H.Nom;
            public string Classe => m_H.Classe;
            public Color TeinteClasse => m_H.TeinteClasse;
            public bool EstLocal => true;
            public int OrRapporte => S.orRapporte;
            public int DegatsInfliges => Mathf.RoundToInt(S.degatsInfliges);
            public int EnnemisTues => S.ennemisTues;
            public int Morts => S.morts;
            public int CoupsCritiques => S.coupsCritiques;
            public int DegatsEvitesNyxessa => Mathf.RoundToInt(S.degatsEvitesNyxessa);
            public int SoinsProdigues => Mathf.RoundToInt(S.soinsProdigues);
        }
    }
}
