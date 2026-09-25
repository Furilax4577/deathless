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
    public class HudPresenter : MonoBehaviour, IEtatPartie, IEtatJoueur, IScoreFin, ICommandesPartie, IClassesJouables
    {
        public static readonly Color TeintePaladin = new Color32(0xd9, 0xb2, 0x64, 0xff);

        Partie m_Partie;
        Camera m_Camera;
        List<ICompetenceHud> m_Competences;
        List<ILigneScore> m_Lignes;

        Partie P => m_Partie != null ? m_Partie : (m_Partie = Partie.Instance);
        EtatJoueur J => P != null ? P.JoueurLocal : null;
        Heros H => P != null ? P.HerosLocal : null;

        void OnEnable() { DonneesUI.Enregistrer(null, null, null, this); }

        void Start()
        {
            m_Camera = Camera.main;
            if (P == null) return;
            P.PartieLancee += () => DonneesUI.Enregistrer(this, this, this, this);
            P.NuitCommencee += n => NuitCommencee?.Invoke(n);
            P.NyxessaTouchee += (d, p) => NyxessaFrappee?.Invoke();
            P.PartieTerminee += () => PartieTerminee?.Invoke();
            if (P.Etat.phase != Jeu.Phase.Attente) DonneesUI.Enregistrer(this, this, this, this);
            m_Competences = new List<ICompetenceHud>
            {
                new Competence("Gameplay/AttackPrimary", "Frappe à l'épée", "Ép", this, 0),
                new Competence("Gameplay/AttackSecondary", "Garde et parade", "Ga", this, 1),
                new Competence("Gameplay/Skill1", "Charge bélier", "Ch", this, 2),
                new Competence("Gameplay/Skill2", "Soin sur soi", "So", this, 3),
            };
            m_Lignes = new List<ILigneScore> { new Ligne(this) };
        }

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
        public float Bouclier => 0f;       // pas de bouclier du sorcier dans la 0.1
        public float BouclierMax => 0f;
        public int OrEquipe => 0;          // pas de donjon dans la 0.1
        public bool VoteActif => P != null && P.EnCours && P.Etat.phase == Jeu.Phase.Jour;
        public int JoueursPrets => P != null ? P.JoueursPrets : 0;
        public int JoueursTotal => P != null ? Mathf.Max(1, P.Etat.joueurs.Count) : 1;
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

        // ----------------------------------------------------------------- IEtatJoueur

        public string Nom => J != null ? J.nom : "Joueur";
        public string Classe => J != null ? J.classe : "Paladin";
        public Color TeinteClasse => TeintePaladin;
        public float Vie => J != null ? J.pv : 0f;
        public float VieMax => J != null ? Mathf.Max(1f, J.pvMax) : 1f;
        public float Endurance => J != null ? J.endurance : 0f;
        public float EnduranceMax => J != null ? Mathf.Max(1f, J.enduranceMax) : 1f;
        public bool EstMort => J != null && J.mort;
        public float TempsAvantReapparition => J != null ? J.reapparitionRestante : 0f;
        public IReadOnlyList<ICompetenceHud> Competences => m_Competences;
        public string InviteInteraction => null;
        public bool EstPret => J != null && J.pret;

        // ----------------------------------------------------------------- IScoreFin

        public ResultatPartie Resultat => P != null && P.Etat.resultat == Jeu.Resultat.Victoire ? ResultatPartie.Victoire : ResultatPartie.Defaite;
        public int NuitAtteinte => P != null ? P.Etat.nuitAtteinte : 1;
        public float DureeSecondes => P != null ? P.Etat.duree : 0f;
        public int OrTotal => 0;
        public IReadOnlyList<ILigneScore> Joueurs => m_Lignes;
        public bool EstPretLocal => EstPret;

        // ----------------------------------------------------------------- ICommandesPartie

        public void LancerSolo() { if (P != null) P.LancerSolo(); }

        // ----------------------------------------------------------------- IClassesJouables (écran de choix de classe)

        public IReadOnlyList<IClasseJouable> Classes => ClassesJouables.Catalogue;

        /// Seul le Paladin est jouable pour l'instant : quelle que soit la classe choisie, la partie lance le Paladin
        /// (l'agent jeu branchera les autres classes ici).
        public void LancerSolo(string classeId) => LancerSolo();
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
            public EtatCompetence Etat
            {
                get
                {
                    var h = m_H.H;
                    if (h == null || !h.Vivant) return EtatCompetence.Indisponible;
                    var b = GameBalance.Courant;
                    switch (m_Index)
                    {
                        case 0: return h.ActionCourante == Heros.Action.Attaque ? EtatCompetence.Active : EtatCompetence.Prete;
                        case 1: return h.EnGarde ? EtatCompetence.Active : h.Endurance <= 0f ? EtatCompetence.Indisponible : EtatCompetence.Prete;
                        case 2: return h.ActionCourante == Heros.Action.Charge || h.ActionCourante == Heros.Action.ChargeAnticipation ? EtatCompetence.Active : h.RechargeCharge > 0f ? EtatCompetence.Recharge : EtatCompetence.Prete;
                        default: return h.ActionCourante == Heros.Action.Soin ? EtatCompetence.Active : h.RechargeSoin > 0f ? EtatCompetence.Recharge : EtatCompetence.Prete;
                    }
                }
            }
            public float RechargeRestante { get { var h = m_H.H; return h == null ? 0f : m_Index == 2 ? h.RechargeCharge : m_Index == 3 ? h.RechargeSoin : 0f; } }
            public float RechargeTotale { get { var b = GameBalance.Courant; return m_Index == 2 ? b.chargeRecharge : m_Index == 3 ? b.soinRecharge : 0f; } }
        }

        /// Ligne de score du joueur local : les sept catégories de ILigneScore, lues dans ScoreJoueur.
        class Ligne : ILigneScore
        {
            readonly HudPresenter m_H;
            public Ligne(HudPresenter h) { m_H = h; }
            ScoreJoueur S => m_H.J != null ? m_H.J.score : new ScoreJoueur();
            public string Nom => m_H.Nom;
            public string Classe => m_H.Classe;
            public Color TeinteClasse => TeintePaladin;
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
